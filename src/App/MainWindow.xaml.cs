using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using EtherSandbox.Views;

namespace EtherSandbox;

public sealed partial class MainWindow : Window
{
    private bool _isDark = false;

    /// <summary>
    /// Guards against re-entrancy between <see cref="NavView_SelectionChanged"/> (which
    /// drives the Frame) and <see cref="ContentFrame_Navigated"/> (which syncs the
    /// NavigationView selection back after any navigation, including ones started from a
    /// Home page card rather than the pane).
    /// </summary>
    private bool _isSyncingSelection = false;

    public MainWindow()
    {
        this.InitializeComponent();
        ResizeToDesignCanvas();
        BuildNavigation();
        ContentFrame.Navigate(ComponentCatalog.Home.PageType);
    }

    /// <summary>Design canvas the sandbox mirrors, in effective (logical) pixels.</summary>
    private const double DesignCanvasWidth = 1500;
    private const double DesignCanvasHeight = 900;

    // DllImport rather than the source-generated LibraryImport: the latter requires
    // AllowUnsafeBlocks, which is not worth turning on project-wide for one call.
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint hwnd);

    /// <summary>
    /// Sizes the window so the canvas measures <see cref="DesignCanvasWidth"/> x
    /// <see cref="DesignCanvasHeight"/> in effective pixels on any display scale.
    /// </summary>
    /// <remarks>
    /// AppWindow works in physical pixels, so passing the design size straight through shrinks
    /// the canvas on a scaled display — at 250% a 1500x900 call leaves roughly 587x325 of usable
    /// canvas. Scaling by the window's DPI keeps the design size honest.
    ///
    /// ResizeClient rather than Resize: Resize sets the outer window size, so the title bar and
    /// borders would come out of the canvas instead of surrounding it.
    /// </remarks>
    private void ResizeToDesignCanvas()
    {
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var dpi = GetDpiForWindow(handle);
        var scale = dpi == 0 ? 1d : dpi / 96d;   // 96 DPI is the unscaled baseline

        AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(
            (int)Math.Round(DesignCanvasWidth * scale),
            (int)Math.Round(DesignCanvasHeight * scale)));
    }

    /// <summary>
    /// Builds the NavigationView's menu items from <see cref="ComponentCatalog"/>: Home
    /// first, then each category as a header followed by its component items, then any
    /// top-level leaves (currently just AI Design Language). This is the only place that
    /// reads the catalog to build the pane — nothing here is hand-authored per page.
    /// </summary>
    private void BuildNavigation()
    {
        var homeItem = new NavigationViewItem
        {
            Content = ComponentCatalog.Home.Name,
            Tag = ComponentCatalog.Home.PageType,
            Icon = new SymbolIcon(Symbol.Home),
        };
        NavView.MenuItems.Add(homeItem);

        foreach (var node in ComponentCatalog.Nodes)
        {
            switch (node)
            {
                case CatalogCategory category:
                    NavView.MenuItems.Add(new NavigationViewItemHeader
                    {
                        // Uppercase + a top margin does the heavy lifting: "FOUNDATIONS" no
                        // longer reads like the item "Colors" below it. Content stays a STRING
                        // — the header's default template collapses a UIElement Content to 0x0
                        // (verified), so a styled TextBlock there renders invisibly. FontSize /
                        // CharacterSpacing are set on the instance; the default template's
                        // ContentPresenter template-binds them. Foreground uses TextBrand (not
                        // left to the template) so headers read as brand blue in both themes.
                        Content = category.Header.ToUpperInvariant(),
                        FontSize = 11,
                        CharacterSpacing = 80,
                        Margin = new Thickness(0, 12, 0, 0),
                        Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextBrand"],
                    });
                    foreach (var entry in category.Items)
                    {
                        NavView.MenuItems.Add(new NavigationViewItem
                        {
                            Content = BuildNavContent(entry),
                            Tag = entry.PageType,
                        });
                    }
                    break;

                case CatalogLeaf leaf:
                    NavView.MenuItems.Add(new NavigationViewItem
                    {
                        Content = BuildNavContent(leaf.Entry),
                        Tag = leaf.Entry.PageType,
                    });
                    break;
            }
        }

        NavView.SelectedItem = homeItem;
    }

    /// <summary>
    /// A nav item's content: just the name, or the name plus an UPDATED badge for a reworked
    /// component. The TextBlock sets no font or foreground so it inherits the nav item's own —
    /// including the selected-state colour — exactly like the plain-string items.
    /// </summary>
    private static object BuildNavContent(ComponentEntry entry)
    {
        if (!entry.IsUpdated) return entry.Name;

        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
        };
        panel.Children.Add(new TextBlock { Text = entry.Name, VerticalAlignment = VerticalAlignment.Center });
        panel.Children.Add(new EtherSandbox.Controls.UpdatedBadge());
        return panel;
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (_isSyncingSelection) return;
        if (args.SelectedItem is NavigationViewItem { Tag: Type pageType } &&
            ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }

    /// <summary>
    /// Keeps the pane selection in sync with the Frame regardless of what triggered the
    /// navigation — the NavigationView itself, or a card on the Home page.
    /// </summary>
    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        foreach (var item in NavView.MenuItems)
        {
            if (item is NavigationViewItem { Tag: Type pageType } navItem && pageType == e.SourcePageType)
            {
                if (!ReferenceEquals(NavView.SelectedItem, navItem))
                {
                    _isSyncingSelection = true;
                    NavView.SelectedItem = navItem;
                    _isSyncingSelection = false;
                }
                break;
            }
        }
    }

    /// <summary>
    /// Flips the whole shell between Light and Dark. Setting <see cref="RootGrid"/>'s
    /// RequestedTheme is the single lever: it re-resolves every {ThemeResource} in the
    /// tree — including the NavigationView pane items (NavigationViewItemForeground) and
    /// RootGrid's own BackgroundCanvas — so no brush needs poking by hand.
    /// </summary>
    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        _isDark = !_isDark;
        RootGrid.RequestedTheme = _isDark ? ElementTheme.Dark : ElementTheme.Light;
        ThemeToggle.Content = _isDark ? "🌙 Dark" : "☀ Light";
    }

    private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new Exception($"Navigation failed to page '{e.SourcePageType.FullName}': {e.Exception.Message}");
    }
}

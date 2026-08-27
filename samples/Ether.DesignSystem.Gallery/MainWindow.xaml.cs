using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using EtherSandbox.Views;

namespace EtherSandbox;

public sealed partial class MainWindow : Window
{
    private bool _isDark;
    private readonly bool _isGallerySmoke = string.Equals(
        Environment.GetEnvironmentVariable("ETHER_GALLERY_SMOKE"),
        "1",
        StringComparison.Ordinal);
    private List<Type>? _gallerySmokePages;
    private List<GallerySmokeRound>? _gallerySmokeRounds;
    private GallerySmokeRound? _gallerySmokeCurrentRound;
    private int _gallerySmokeNextPageIndex;
    private bool _gallerySmokeCompleted;

    private GalleryRtlProof? _gallerySmokeRtl;

    /// <summary>
    /// Guards against re-entrancy between <see cref="NavView_SelectionChanged"/> (which
    /// drives the Frame) and <see cref="ContentFrame_Navigated"/> (which syncs the
    /// NavigationView selection back after any navigation, including ones started from a
    /// Home page card rather than the pane).
    /// </summary>
    private bool _isSyncingSelection;

    private sealed class GallerySmokeRound
    {
        public required string Theme { get; init; }
        public required string BackgroundCanvasColor { get; init; }
        public int PageCount { get; set; }
        public required int TotalPageCount { get; init; }
        public required string?[] PageTypes { get; init; }
    }

    private sealed class GalleryRtlProof
    {
        public required string OsLayoutDirection { get; init; }
        public required bool ForcedRtlInherited { get; init; }
    }

    public MainWindow()
    {
        this.InitializeComponent();
        ApplyOsLayoutDirection();
        ResizeToDesignCanvas();
        BuildNavigation();
        ContentFrame.Navigate(ComponentCatalog.Home.PageType);

        if (_isGallerySmoke)
        {
            RootGrid.Loaded += RootGrid_LoadedForGallerySmoke;
        }
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
            Content = ComponentCatalog.Home.DisplayName,
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
                        Content = category.DisplayHeader.ToUpperInvariant(),
                        FontSize = 11,
                        CharacterSpacing = 80,
                        Margin = new Thickness(0, 12, 0, 0),
                        Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextBrand"],
                    });
                    foreach (var entry in category.Items)
                    {
                        var item = new NavigationViewItem
                        {
                            Content = BuildNavContent(entry),
                            Tag = entry.PageType,
                        };
                        AutomationProperties.SetName(item, entry.DisplayName);
                        NavView.MenuItems.Add(item);
                    }
                    break;

                case CatalogLeaf leaf:
                    {
                        var item = new NavigationViewItem
                        {
                            Content = BuildNavContent(leaf.Entry),
                            Tag = leaf.Entry.PageType,
                        };
                        AutomationProperties.SetName(item, leaf.Entry.DisplayName);
                        NavView.MenuItems.Add(item);
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unhandled catalog node '{node.GetType().FullName}'.");
            }
        }

        NavView.SelectedItem = homeItem;
    }

    /// <summary>
    /// A nav item's content: just the name, or the name plus an UPDATED and/or PRIMITIVE TOKEN
    /// badge. The TextBlock sets no font or foreground so it inherits the nav item's own —
    /// including the selected-state colour — exactly like the plain-string items.
    /// </summary>
    private static object BuildNavContent(ComponentEntry entry)
    {
        if (!entry.IsUpdated && !entry.IsPrimitive) return entry.DisplayName;

        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
        };
        panel.Children.Add(new TextBlock { Text = entry.DisplayName, VerticalAlignment = VerticalAlignment.Center });
        if (entry.IsUpdated) panel.Children.Add(new EtherSandbox.Controls.UpdatedBadge());
        if (entry.IsPrimitive) panel.Children.Add(new EtherSandbox.Controls.PrimitiveBadge());
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
        ThemeToggle.Content = _isDark
            ? GalleryStrings.Get("GalleryMainWindow.ThemeDark.Content", "🌙 Dark")
            : GalleryStrings.Get("GalleryMainWindow002.Content", "☀ Light");
    }

    private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        if (_isGallerySmoke)
        {
            CompleteGallerySmoke(false, $"Navigation failed to page '{e.SourcePageType.FullName}': {e.Exception.Message}");
            return;
        }

        throw new InvalidOperationException($"Navigation failed to page '{e.SourcePageType.FullName}': {e.Exception.Message}");
    }

    private async void RootGrid_LoadedForGallerySmoke(object sender, RoutedEventArgs e)
    {
        RootGrid.Loaded -= RootGrid_LoadedForGallerySmoke;

        try
        {
            _gallerySmokePages = GetGallerySmokePages();
            _gallerySmokeRounds = new List<GallerySmokeRound>();
            ContentFrame.Navigated += ContentFrame_NavigatedForGallerySmoke;
            await StartGallerySmokeRoundAsync(ElementTheme.Light);
        }
        catch (Exception exception)
        {
            CompleteGallerySmoke(false, exception.ToString());
        }
    }

    private static List<Type> GetGallerySmokePages()
    {
        var pageTypes = new List<Type> { ComponentCatalog.Home.PageType };
        foreach (var node in ComponentCatalog.Nodes)
        {
            switch (node)
            {
                case CatalogCategory category:
                    pageTypes.AddRange(category.Items.Select(entry => entry.PageType));
                    break;
                case CatalogLeaf leaf:
                    pageTypes.Add(leaf.Entry.PageType);
                    break;
                default:
                    throw new InvalidOperationException($"Unhandled catalog node '{node.GetType().FullName}'.");
            }
        }

        return pageTypes;
    }

    private void NavigateNextGallerySmokePage()
    {
        if (_gallerySmokePages is null)
        {
            CompleteGallerySmoke(false, "Gallery smoke pages were not initialized.");
            return;
        }

        if (_gallerySmokeNextPageIndex >= _gallerySmokePages.Count)
        {
            CompleteGallerySmokeRound();
            return;
        }

        var nextPage = _gallerySmokePages[_gallerySmokeNextPageIndex];
        if (ContentFrame.CurrentSourcePageType == nextPage)
        {
            _gallerySmokeNextPageIndex++;
            QueueNextGallerySmokePage();
            return;
        }

        if (!ContentFrame.Navigate(nextPage))
        {
            CompleteGallerySmoke(false, $"Frame refused navigation to page '{nextPage.FullName}'.");
        }
    }

    private void ContentFrame_NavigatedForGallerySmoke(object sender, NavigationEventArgs e)
    {
        if (_gallerySmokePages is null || _gallerySmokeNextPageIndex >= _gallerySmokePages.Count)
        {
            CompleteGallerySmoke(false, "Gallery smoke received an unexpected navigation event.");
            return;
        }

        var expectedPage = _gallerySmokePages[_gallerySmokeNextPageIndex];
        if (e.SourcePageType != expectedPage)
        {
            CompleteGallerySmoke(false, $"Expected page '{expectedPage.FullName}', but navigated to '{e.SourcePageType.FullName}'.");
            return;
        }

        _gallerySmokeNextPageIndex++;
        QueueNextGallerySmokePage();
    }

    private void QueueNextGallerySmokePage()
    {
        if (!DispatcherQueue.TryEnqueue(NavigateNextGallerySmokePage))
        {
            CompleteGallerySmoke(false, "The UI dispatcher rejected the next Gallery smoke navigation.");
        }
    }

    private async Task StartGallerySmokeRoundAsync(ElementTheme theme)
    {
        if (_gallerySmokePages is null || _gallerySmokeRounds is null)
        {
            CompleteGallerySmoke(false, "Gallery smoke round state was not initialized.");
            return;
        }

        _gallerySmokeNextPageIndex = 0;
        RootGrid.RequestedTheme = theme;
        try
        {
            await WaitForAppliedThemeAsync(RootGrid, theme);
            if (RootGrid.Background is not Microsoft.UI.Xaml.Media.SolidColorBrush brush)
            {
                CompleteGallerySmoke(false, "RootGrid BackgroundCanvas did not resolve to a SolidColorBrush.");
                return;
            }

            _gallerySmokeCurrentRound = new GallerySmokeRound
            {
                Theme = theme.ToString(),
                BackgroundCanvasColor = $"#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}",
                TotalPageCount = _gallerySmokePages.Count,
                PageTypes = _gallerySmokePages.Select(page => page.FullName).ToArray(),
            };
            NavigateNextGallerySmokePage();
        }
        catch (Exception exception)
        {
            CompleteGallerySmoke(false, exception.Message);
        }
    }

    private void CompleteGallerySmokeRound()
    {
        if (_gallerySmokeCurrentRound is null || _gallerySmokeRounds is null)
        {
            CompleteGallerySmoke(false, "Gallery smoke completed a page traversal without an active theme round.");
            return;
        }

        _gallerySmokeCurrentRound.PageCount = _gallerySmokeNextPageIndex;
        _gallerySmokeRounds.Add(_gallerySmokeCurrentRound);
        _gallerySmokeCurrentRound = null;

        if (_gallerySmokeRounds.Count == 1)
        {
            _ = StartGallerySmokeRoundAsync(ElementTheme.Dark);
            return;
        }

        if (!TryProveGalleryRtlInheritance(out var rtlError))
        {
            CompleteGallerySmoke(false, rtlError);
            return;
        }

        CompleteGallerySmoke(true, null);
    }

    private void ApplyOsLayoutDirection()
    {
        if (CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft)
        {
            RootGrid.FlowDirection = FlowDirection.RightToLeft;
        }
    }

    private bool TryProveGalleryRtlInheritance(out string? error)
    {
        error = null;
        var previous = RootGrid.FlowDirection;
        var osDirection = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? "RTL" : "LTR";

        RootGrid.FlowDirection = FlowDirection.RightToLeft;
        RootGrid.UpdateLayout();
        NavView.UpdateLayout();
        ContentFrame.UpdateLayout();

        var inherited = NavView.FlowDirection == FlowDirection.RightToLeft &&
            ContentFrame.FlowDirection == FlowDirection.RightToLeft;
        _gallerySmokeRtl = new GalleryRtlProof
        {
            OsLayoutDirection = osDirection,
            ForcedRtlInherited = inherited,
        };

        RootGrid.FlowDirection = previous;
        RootGrid.UpdateLayout();

        if (!inherited)
        {
            error = "Gallery RootGrid RightToLeft did not inherit to NavigationView and ContentFrame.";
            return false;
        }

        return true;
    }

    private static async Task WaitForAppliedThemeAsync(FrameworkElement themeRoot, ElementTheme requestedTheme)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (themeRoot.ActualTheme != requestedTheme && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }

        if (themeRoot.ActualTheme != requestedTheme)
        {
            throw new InvalidOperationException(
                $"Theme application timed out. Requested '{requestedTheme}', actual '{themeRoot.ActualTheme}'.");
        }
    }

    private void CompleteGallerySmoke(bool succeeded, string? message)
    {
        if (_gallerySmokeCompleted)
        {
            return;
        }

        _gallerySmokeCompleted = true;
        ContentFrame.Navigated -= ContentFrame_NavigatedForGallerySmoke;

        var resultPath = Environment.GetEnvironmentVariable("ETHER_GALLERY_SMOKE_RESULT_PATH");
        var result = JsonSerializer.Serialize(new
        {
            marker = "ETHER_GALLERY_SMOKE",
            outcome = succeeded ? "success" : "failure",
            pageCount = _gallerySmokeRounds?.Sum(round => round.PageCount) ?? _gallerySmokeNextPageIndex,
            totalPageCount = (_gallerySmokePages?.Count ?? 0) * 2,
            pageTypes = _gallerySmokePages?.Select(page => page.FullName).ToArray() ?? Array.Empty<string?>(),
            rounds = _gallerySmokeRounds?.Select(round => new
            {
                theme = round.Theme,
                pageCount = round.PageCount,
                totalPageCount = round.TotalPageCount,
                pageTypes = round.PageTypes,
                backgroundCanvasColor = round.BackgroundCanvasColor,
            }).ToArray() ?? Array.Empty<object>(),
            rtl = _gallerySmokeRtl is null ? null : new
            {
                osLayoutDirection = _gallerySmokeRtl.OsLayoutDirection,
                forcedRtlInherited = _gallerySmokeRtl.ForcedRtlInherited,
            },
            message,
        });

        if (!string.IsNullOrWhiteSpace(resultPath))
        {
            File.WriteAllText(resultPath, result);
        }

        Console.WriteLine(result);
        Close();
        Application.Current.Exit();
    }
}

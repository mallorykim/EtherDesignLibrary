using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace Ether.DesignSystem.Controls;

/// <summary>
/// Right-aligned masthead caption bar with optional icon slots. Caption
/// buttons invoke host <see cref="Window.Close"/> (via <c>WM_CLOSE</c>) and
/// <see cref="OverlappedPresenter"/> commands. Hosts resolve the window from
/// <see cref="UIElement.XamlRoot"/> so the control is packable.
/// </summary>
/// <remarks>
/// Optional icon visibility is driven through VisualStateManager groups
/// (<c>SettingsIconStates</c>, <c>SearchIconStates</c>, <c>MenuIconStates</c>,
/// <c>ChevronStates</c>). The caption buttons carry Segoe Fluent Icons font glyphs
/// as their Content; the maximize/restore glyph swap uses <c>WindowStates</c> to
/// change that Content directly, and EtherMastheadCaptionButtonStyle's own
/// VisualStateManager recolors the glyph and its hover/pressed wash on
/// PointerOver/Pressed. Caption click handling stays code-driven. Gallery
/// specimens set <see cref="EnableWindowCommands"/> to false and use an internal
/// preview override for their static maximize/restore samples. The menu, search,
/// settings, and chevron icon slots are decorative affordances; place an
/// interactive control beside the masthead for those actions.
/// </remarks>
[TemplatePart(Name = MenuIconSlotPart, Type = typeof(FrameworkElement))]
[TemplatePart(Name = SearchIconSlotPart, Type = typeof(FrameworkElement))]
[TemplatePart(Name = SettingsButtonPart, Type = typeof(FrameworkElement))]
[TemplatePart(Name = ChevronSlotPart, Type = typeof(FrameworkElement))]
[TemplatePart(Name = MinimizeButtonPart, Type = typeof(EtherButton))]
[TemplatePart(Name = MaximizeRestoreButtonPart, Type = typeof(EtherButton))]
[TemplatePart(Name = CloseButtonPart, Type = typeof(EtherButton))]
[TemplateVisualState(GroupName = SettingsIconStatesGroup, Name = SettingsVisibleState)]
[TemplateVisualState(GroupName = SettingsIconStatesGroup, Name = SettingsCollapsedState)]
[TemplateVisualState(GroupName = SearchIconStatesGroup, Name = SearchVisibleState)]
[TemplateVisualState(GroupName = SearchIconStatesGroup, Name = SearchCollapsedState)]
[TemplateVisualState(GroupName = MenuIconStatesGroup, Name = MenuVisibleState)]
[TemplateVisualState(GroupName = MenuIconStatesGroup, Name = MenuCollapsedState)]
[TemplateVisualState(GroupName = ChevronStatesGroup, Name = ChevronVisibleState)]
[TemplateVisualState(GroupName = ChevronStatesGroup, Name = ChevronCollapsedState)]
[TemplateVisualState(GroupName = WindowStatesGroup, Name = WindowRestoredState)]
[TemplateVisualState(GroupName = WindowStatesGroup, Name = WindowMaximizedState)]
public sealed class EtherMasthead : Control
{
    private const string MenuIconSlotPart = "MenuIconSlot";
    private const string SearchIconSlotPart = "SearchIconSlot";
    private const string SettingsButtonPart = "SettingsButton";
    private const string ChevronSlotPart = "ChevronSlot";
    private const string MinimizeButtonPart = "MinimizeButton";
    private const string MaximizeRestoreButtonPart = "MaximizeRestoreButton";
    private const string CloseButtonPart = "CloseButton";
    private const string SettingsIconStatesGroup = "SettingsIconStates";
    private const string SettingsVisibleState = "SettingsVisible";
    private const string SettingsCollapsedState = "SettingsCollapsed";
    private const string SearchIconStatesGroup = "SearchIconStates";
    private const string SearchVisibleState = "SearchVisible";
    private const string SearchCollapsedState = "SearchCollapsed";
    private const string MenuIconStatesGroup = "MenuIconStates";
    private const string MenuVisibleState = "MenuVisible";
    private const string MenuCollapsedState = "MenuCollapsed";
    private const string ChevronStatesGroup = "ChevronStates";
    private const string ChevronVisibleState = "ChevronVisible";
    private const string ChevronCollapsedState = "ChevronCollapsed";
    private const string WindowStatesGroup = "WindowStates";
    private const string WindowRestoredState = "WindowRestored";
    private const string WindowMaximizedState = "WindowMaximized";

    private const uint WmClose = 0x0010;

    private AppWindow? _appWindow;
    private EtherButton? _minimizeButton;
    private EtherButton? _maximizeRestoreButton;
    private EtherButton? _closeButton;

    /// <summary>
    /// Initializes a new instance of the <see cref="EtherMasthead"/> class.
    /// </summary>
    public EtherMasthead()
    {
        DefaultStyleKey = typeof(EtherMasthead);
        Loaded += EtherMasthead_Loaded;
        Unloaded += EtherMasthead_Unloaded;
    }

    /// <summary>Raised before an enabled caption button invokes its host-window command.</summary>
    public event EventHandler<MastheadActionInvokedEventArgs>? ActionInvoked;

    /// <summary>Identifies the <see cref="ShowSettings"/> dependency property. Registered and effective default is true.</summary>
    public static readonly DependencyProperty ShowSettingsProperty =
        DependencyProperty.Register(
            nameof(ShowSettings),
            typeof(bool),
            typeof(EtherMasthead),
            new PropertyMetadata(true, OnOptionalIconPropertyChanged));

    /// <summary>Gets or sets whether the settings icon slot is shown. Default is true.</summary>
    public bool ShowSettings
    {
        get => (bool)GetValue(ShowSettingsProperty);
        set => SetValue(ShowSettingsProperty, value);
    }

    /// <summary>Identifies the <see cref="ShowSearch"/> dependency property. Registered and effective default is false.</summary>
    public static readonly DependencyProperty ShowSearchProperty =
        DependencyProperty.Register(
            nameof(ShowSearch),
            typeof(bool),
            typeof(EtherMasthead),
            new PropertyMetadata(false, OnOptionalIconPropertyChanged));

    /// <summary>Gets or sets whether the search icon slot is shown. Default is false.</summary>
    public bool ShowSearch
    {
        get => (bool)GetValue(ShowSearchProperty);
        set => SetValue(ShowSearchProperty, value);
    }

    /// <summary>Identifies the <see cref="ShowMenuIcon"/> dependency property. Registered and effective default is false.</summary>
    public static readonly DependencyProperty ShowMenuIconProperty =
        DependencyProperty.Register(
            nameof(ShowMenuIcon),
            typeof(bool),
            typeof(EtherMasthead),
            new PropertyMetadata(false, OnOptionalIconPropertyChanged));

    /// <summary>Gets or sets whether the menu icon slot is shown. Default is false.</summary>
    public bool ShowMenuIcon
    {
        get => (bool)GetValue(ShowMenuIconProperty);
        set => SetValue(ShowMenuIconProperty, value);
    }

    /// <summary>Identifies the <see cref="ShowChevron"/> dependency property. Registered and effective default is false.</summary>
    public static readonly DependencyProperty ShowChevronProperty =
        DependencyProperty.Register(
            nameof(ShowChevron),
            typeof(bool),
            typeof(EtherMasthead),
            new PropertyMetadata(false, OnOptionalIconPropertyChanged));

    /// <summary>Gets or sets whether the chevron icon slot is shown. Default is false.</summary>
    public bool ShowChevron
    {
        get => (bool)GetValue(ShowChevronProperty);
        set => SetValue(ShowChevronProperty, value);
    }

    /// <summary>Identifies the Gallery-only preview window-state dependency property.</summary>
    internal static readonly DependencyProperty PreviewIsMaximizedProperty =
        DependencyProperty.Register(
            nameof(PreviewIsMaximized),
            typeof(bool?),
            typeof(EtherMasthead),
            new PropertyMetadata(null, OnPreviewWindowStateChanged));

    /// <summary>Gets or sets the Gallery-only maximize/restore glyph override.</summary>
    internal bool? PreviewIsMaximized
    {
        get => (bool?)GetValue(PreviewIsMaximizedProperty);
        set => SetValue(PreviewIsMaximizedProperty, value);
    }

    /// <summary>Identifies the <see cref="EnableWindowCommands"/> dependency property. Registered and effective default is true.</summary>
    public static readonly DependencyProperty EnableWindowCommandsProperty =
        DependencyProperty.Register(
            nameof(EnableWindowCommands),
            typeof(bool),
            typeof(EtherMasthead),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether caption buttons invoke host window commands. Default is true.
    /// Gallery specimens set this to false so showcase clicks do not minimize
    /// or close the sandbox window.
    /// </summary>
    public bool EnableWindowCommands
    {
        get => (bool)GetValue(EnableWindowCommandsProperty);
        set => SetValue(EnableWindowCommandsProperty, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        DetachCaptionButtons();

        _minimizeButton = GetTemplateChild(MinimizeButtonPart) as EtherButton;
        _maximizeRestoreButton = GetTemplateChild(MaximizeRestoreButtonPart) as EtherButton;
        _closeButton = GetTemplateChild(CloseButtonPart) as EtherButton;

        AttachCaptionButtons();
        UpdateOptionalIconStates();
        UpdateWindowState();
    }

    private static void OnOptionalIconPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EtherMasthead masthead)
        {
            masthead.UpdateOptionalIconStates();
        }
    }

    private static void OnPreviewWindowStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EtherMasthead masthead)
        {
            masthead.UpdateWindowState();
        }
    }

    private void EtherMasthead_Loaded(object sender, RoutedEventArgs e)
    {
        AttachHostAppWindow();
        UpdateOptionalIconStates();
        UpdateWindowState();
    }

    private void EtherMasthead_Unloaded(object sender, RoutedEventArgs e)
    {
        DetachHostAppWindow();
    }

    private void AttachHostAppWindow()
    {
        var appWindow = GetHostAppWindow();
        if (_appWindow == appWindow)
        {
            UpdateWindowState();
            return;
        }

        DetachHostAppWindow();
        _appWindow = appWindow;
        if (_appWindow is not null)
        {
            _appWindow.Changed += AppWindow_Changed;
        }
    }

    private void DetachHostAppWindow()
    {
        if (_appWindow is null)
        {
            return;
        }

        _appWindow.Changed -= AppWindow_Changed;
        _appWindow = null;
    }

    private AppWindow? GetHostAppWindow()
    {
        if (XamlRoot?.ContentIslandEnvironment is not { } environment)
        {
            return null;
        }

        return AppWindow.GetFromWindowId(environment.AppWindowId);
    }

    private OverlappedPresenter? GetPresenter()
        => GetHostAppWindow()?.Presenter as OverlappedPresenter;

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        DispatcherQueue.TryEnqueue(UpdateWindowState);
    }

    private void UpdateOptionalIconStates()
    {
        VisualStateManager.GoToState(this, ShowSettings ? SettingsVisibleState : SettingsCollapsedState, false);
        VisualStateManager.GoToState(this, ShowSearch ? SearchVisibleState : SearchCollapsedState, false);
        VisualStateManager.GoToState(this, ShowMenuIcon ? MenuVisibleState : MenuCollapsedState, false);
        VisualStateManager.GoToState(this, ShowChevron ? ChevronVisibleState : ChevronCollapsedState, false);
    }

    private void UpdateWindowState()
    {
        var isMaximized = PreviewIsMaximized ?? (GetPresenter()?.State == OverlappedPresenterState.Maximized);
        VisualStateManager.GoToState(this, isMaximized ? WindowMaximizedState : WindowRestoredState, false);

        if (_maximizeRestoreButton is not null)
        {
            AutomationProperties.SetName(_maximizeRestoreButton, isMaximized ? "Restore window" : "Maximize window");
        }
    }

    private void AttachCaptionButtons()
    {
        if (_minimizeButton is not null)
        {
            _minimizeButton.Click += MinimizeButton_Click;
        }

        if (_maximizeRestoreButton is not null)
        {
            _maximizeRestoreButton.Click += MaximizeRestoreButton_Click;
        }

        if (_closeButton is not null)
        {
            _closeButton.Click += CloseButton_Click;
        }
    }

    private void DetachCaptionButtons()
    {
        if (_minimizeButton is not null)
        {
            _minimizeButton.Click -= MinimizeButton_Click;
        }

        if (_maximizeRestoreButton is not null)
        {
            _maximizeRestoreButton.Click -= MaximizeRestoreButton_Click;
        }

        if (_closeButton is not null)
        {
            _closeButton.Click -= CloseButton_Click;
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnableWindowCommands)
        {
            return;
        }

        ActionInvoked?.Invoke(this, new MastheadActionInvokedEventArgs(MastheadAction.Minimize));
        GetPresenter()?.Minimize();
    }

    private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnableWindowCommands)
        {
            if (PreviewIsMaximized.HasValue)
            {
                PreviewIsMaximized = !PreviewIsMaximized.Value;
            }

            return;
        }

        var presenter = GetPresenter();
        var isMaximized = PreviewIsMaximized ?? (presenter?.State == OverlappedPresenterState.Maximized);
        ActionInvoked?.Invoke(this, new MastheadActionInvokedEventArgs(MastheadAction.MaximizeRestore));

        if (presenter is null)
            return;

        if (isMaximized)
        {
            presenter.Restore();
        }
        else
        {
            presenter.Maximize();
        }

        UpdateWindowState();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnableWindowCommands)
        {
            return;
        }

        ActionInvoked?.Invoke(this, new MastheadActionInvokedEventArgs(MastheadAction.Close));
        CloseHostWindow();
    }

    private void CloseHostWindow()
    {
        if (XamlRoot?.ContentIslandEnvironment is not { } environment)
        {
            return;
        }

        var hwnd = Win32Interop.GetWindowFromWindowId(environment.AppWindowId);
        if (hwnd == 0)
        {
            return;
        }

        _ = PostMessage(hwnd, WmClose, 0, 0);
    }

    // DllImport rather than the source-generated LibraryImport: the latter requires
    // AllowUnsafeBlocks, which is not worth turning on in the packable Controls
    // assembly for one call.
    [DllImport("user32.dll", EntryPoint = "PostMessageW", ExactSpelling = true)]
    private static extern int PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);
}

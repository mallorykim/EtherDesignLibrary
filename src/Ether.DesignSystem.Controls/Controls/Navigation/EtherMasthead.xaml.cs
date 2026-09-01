using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

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
/// <c>ChevronStates</c>). Maximize/restore glyph swap uses <c>WindowStates</c>.
/// Caption click handling stays code-driven. Gallery specimens set
/// <see cref="EnableWindowCommands"/> to false and use an internal preview override for
/// their static maximize/restore samples. The menu, search, settings, and chevron icon slots
/// are decorative affordances; place an interactive control beside the masthead for those
/// actions.
/// </remarks>
[TemplatePart(Name = MenuIconSlotPart, Type = typeof(FrameworkElement))]
[TemplatePart(Name = SearchIconSlotPart, Type = typeof(FrameworkElement))]
[TemplatePart(Name = SettingsButtonPart, Type = typeof(FrameworkElement))]
[TemplatePart(Name = ChevronSlotPart, Type = typeof(FrameworkElement))]
[TemplatePart(Name = MinimizeButtonPart, Type = typeof(EtherButton))]
[TemplatePart(Name = MaximizeRestoreButtonPart, Type = typeof(EtherButton))]
[TemplatePart(Name = CloseButtonPart, Type = typeof(EtherButton))]
[TemplatePart(Name = MaximizeIconPart, Type = typeof(FrameworkElement))]
[TemplatePart(Name = RestoreIconPart, Type = typeof(FrameworkElement))]
[TemplatePart(Name = MinimizeIconPart, Type = typeof(Shape))]
[TemplatePart(Name = RestoreIconBackPart, Type = typeof(Shape))]
[TemplatePart(Name = RestoreIconFrontPart, Type = typeof(Shape))]
[TemplatePart(Name = CloseIconStroke1Part, Type = typeof(Shape))]
[TemplatePart(Name = CloseIconStroke2Part, Type = typeof(Shape))]
[TemplatePart(Name = IconForegroundDefaultSwatchPart, Type = typeof(Shape))]
[TemplatePart(Name = IconForegroundHoverSwatchPart, Type = typeof(Shape))]
[TemplatePart(Name = IconForegroundPressedSwatchPart, Type = typeof(Shape))]
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
    private const string MaximizeIconPart = "MaximizeIcon";
    private const string RestoreIconPart = "RestoreIcon";
    private const string MinimizeIconPart = "MinimizeIcon";
    private const string RestoreIconBackPart = "RestoreIconBack";
    private const string RestoreIconFrontPart = "RestoreIconFront";
    private const string CloseIconStroke1Part = "CloseIconStroke1";
    private const string CloseIconStroke2Part = "CloseIconStroke2";
    private const string IconForegroundDefaultSwatchPart = "IconForegroundDefaultSwatch";
    private const string IconForegroundHoverSwatchPart = "IconForegroundHoverSwatch";
    private const string IconForegroundPressedSwatchPart = "IconForegroundPressedSwatch";
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
    private Shape? _minimizeIcon;
    private Shape? _maximizeIcon;
    private Shape? _restoreIconBack;
    private Shape? _restoreIconFront;
    private Shape? _closeIconStroke1;
    private Shape? _closeIconStroke2;
    private Shape? _iconForegroundDefaultSwatch;
    private Shape? _iconForegroundHoverSwatch;
    private Shape? _iconForegroundPressedSwatch;

    /// <summary>
    /// Initializes a new instance of the <see cref="EtherMasthead"/> class.
    /// </summary>
    public EtherMasthead()
    {
        DefaultStyleKey = typeof(EtherMasthead);
        Loaded += EtherMasthead_Loaded;
        Unloaded += EtherMasthead_Unloaded;
        ActualThemeChanged += EtherMasthead_ActualThemeChanged;
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
        _minimizeIcon = GetTemplateChild(MinimizeIconPart) as Shape;
        _maximizeIcon = GetTemplateChild(MaximizeIconPart) as Shape;
        _restoreIconBack = GetTemplateChild(RestoreIconBackPart) as Shape;
        _restoreIconFront = GetTemplateChild(RestoreIconFrontPart) as Shape;
        _closeIconStroke1 = GetTemplateChild(CloseIconStroke1Part) as Shape;
        _closeIconStroke2 = GetTemplateChild(CloseIconStroke2Part) as Shape;
        _iconForegroundDefaultSwatch = GetTemplateChild(IconForegroundDefaultSwatchPart) as Shape;
        _iconForegroundHoverSwatch = GetTemplateChild(IconForegroundHoverSwatchPart) as Shape;
        _iconForegroundPressedSwatch = GetTemplateChild(IconForegroundPressedSwatchPart) as Shape;

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
            AttachCaptionIconPointerEvents(_minimizeButton);
        }

        if (_maximizeRestoreButton is not null)
        {
            _maximizeRestoreButton.Click += MaximizeRestoreButton_Click;
            AttachCaptionIconPointerEvents(_maximizeRestoreButton);
        }

        if (_closeButton is not null)
        {
            _closeButton.Click += CloseButton_Click;
            AttachCaptionIconPointerEvents(_closeButton);
        }
    }

    private void DetachCaptionButtons()
    {
        if (_minimizeButton is not null)
        {
            _minimizeButton.Click -= MinimizeButton_Click;
            DetachCaptionIconPointerEvents(_minimizeButton);
        }

        if (_maximizeRestoreButton is not null)
        {
            _maximizeRestoreButton.Click -= MaximizeRestoreButton_Click;
            DetachCaptionIconPointerEvents(_maximizeRestoreButton);
        }

        if (_closeButton is not null)
        {
            _closeButton.Click -= CloseButton_Click;
            DetachCaptionIconPointerEvents(_closeButton);
        }
    }

    private void AttachCaptionIconPointerEvents(EtherButton button)
    {
        button.PointerEntered += CaptionButton_PointerEntered;
        button.PointerExited += CaptionButton_PointerExited;
        button.PointerPressed += CaptionButton_PointerPressed;
        button.PointerReleased += CaptionButton_PointerReleased;
        button.PointerCanceled += CaptionButton_PointerCanceledOrCaptureLost;
        button.PointerCaptureLost += CaptionButton_PointerCanceledOrCaptureLost;
    }

    private void DetachCaptionIconPointerEvents(EtherButton button)
    {
        button.PointerEntered -= CaptionButton_PointerEntered;
        button.PointerExited -= CaptionButton_PointerExited;
        button.PointerPressed -= CaptionButton_PointerPressed;
        button.PointerReleased -= CaptionButton_PointerReleased;
        button.PointerCanceled -= CaptionButton_PointerCanceledOrCaptureLost;
        button.PointerCaptureLost -= CaptionButton_PointerCanceledOrCaptureLost;
    }

    // The caption icons (Minimize/Maximize/Restore/Close) are plain Shape content hosted through
    // EtherMastheadCaptionButtonStyle's ContentPresenter. Shape.Fill/Stroke does not participate
    // in WinUI's Foreground property-value-inheritance the way TextElement/Control.Foreground
    // does, and a VisualState.Setter can only reach named parts within the SAME ControlTemplate
    // that declares the PointerOver/Pressed state — not into a sibling control's own template or
    // into that control's Content. So the caption button's already-working Bg.Background
    // Hover/Pressed repaint (a same-template VisualState.Setter) cannot be mirrored onto the icon
    // color the same way; these pointer handlers keep the icon's paired foreground in sync
    // instead.
    //
    // (An attempt to replace this with a pure-XAML "Setter Target="Foreground"" — no ElementName,
    // intended to target the templated Button's own Foreground property from
    // EtherMastheadCaptionButtonStyle's PointerOver/Pressed states, paired with an ElementName
    // Binding on the icon Shapes — compiled fine but threw a runtime
    // System.Runtime.InteropServices.COMException (0x800F1000) out of
    // VisualStateManager.GoToState the moment PointerOver actually needed to apply, on this WinUI
    // 3 version. Reverted; see the ActualThemeChanged handler below for how the resulting
    // stale-color-after-hover-then-theme-switch bug is fixed within this approach instead.)
    //
    // IconForegroundDefaultSwatch/HoverSwatch/PressedSwatch are hidden elements whose Fill is
    // bound via {ThemeResource EtherMastheadIconForegroundBrush/HoverBrush/PressedBrush} in XAML,
    // so reading their already-resolved Fill here picks up the correct brush for the current
    // Light/Dark/HighContrast theme without re-implementing WinUI's theme-resolution logic. Every
    // transition below explicitly assigns Fill/Stroke from one of these swatches (never
    // ClearValue): once an icon's Fill/Stroke has been set procedurally even once, it stops
    // tracking its original {ThemeResource} XAML expression, so ClearValue would fall back to the
    // Shape's bare default (no brush, an invisible icon) instead of the themed default color.
    private void CaptionButton_PointerEntered(object sender, PointerRoutedEventArgs e) =>
        SetCaptionIconColor(sender as EtherButton, _iconForegroundHoverSwatch?.Fill);

    private void CaptionButton_PointerExited(object sender, PointerRoutedEventArgs e) =>
        SetCaptionIconColor(sender as EtherButton, _iconForegroundDefaultSwatch?.Fill);

    private void CaptionButton_PointerPressed(object sender, PointerRoutedEventArgs e) =>
        SetCaptionIconColor(sender as EtherButton, _iconForegroundPressedSwatch?.Fill);

    private void CaptionButton_PointerReleased(object sender, PointerRoutedEventArgs e) =>
        SetCaptionIconColor(sender as EtherButton, _iconForegroundHoverSwatch?.Fill);

    private void CaptionButton_PointerCanceledOrCaptureLost(object sender, PointerRoutedEventArgs e) =>
        SetCaptionIconColor(sender as EtherButton, _iconForegroundDefaultSwatch?.Fill);

    // Fixes the theme-staleness regression: since Fill/Stroke are procedural local-value
    // assignments (see the big comment above — ClearValue is not an option), a Fill/Stroke set
    // while hovering Light and left untouched will keep showing Light's hover color even after
    // the app switches to Dark, until the next real hover re-synces it. The swatch elements
    // themselves are NOT affected by this — their Fill stays a live {ThemeResource ...} that
    // WinUI automatically re-resolves on theme change — so on ActualThemeChanged we just need to
    // re-push each button's CURRENT CommonStates state (Normal/PointerOver/Pressed/Disabled),
    // which WinUI's ButtonBase already tracks for us, back onto its icon(s) using the
    // now-freshly-resolved swatch for that state. This covers every caption button whether or not
    // it is presently hovered/pressed, and requires no extra state bookkeeping of our own.
    private void EtherMasthead_ActualThemeChanged(FrameworkElement sender, object args)
    {
        // The swatch elements' Fill is a {ThemeResource ...} binding. Measured directly (an
        // earlier attempt called UpdateLayout() right here first): at the instant
        // ActualThemeChanged fires, the swatches have NOT actually re-resolved their Fill to the
        // new theme yet — reading them synchronously here reads the OLD theme's color, so
        // "refreshing" from them just reassigns the same stale value onto the icon. Deferring one
        // dispatcher tick (same pattern as AppWindow_Changed below) lets the resource requery that
        // ActualThemeChanged itself kicks off actually land before this reads the swatches.
        DispatcherQueue.TryEnqueue(RefreshAllCaptionIconColorsForCurrentState);
    }

    private void RefreshAllCaptionIconColorsForCurrentState()
    {
        RefreshCaptionIconColorForCurrentState(_minimizeButton);
        RefreshCaptionIconColorForCurrentState(_maximizeRestoreButton);
        RefreshCaptionIconColorForCurrentState(_closeButton);
    }

    private void RefreshCaptionIconColorForCurrentState(EtherButton? button)
    {
        if (button is null ||
            VisualTreeHelper.GetChildrenCount(button) < 1 ||
            VisualTreeHelper.GetChild(button, 0) is not FrameworkElement templateRoot)
        {
            return;
        }

        var commonStates = VisualStateManager.GetVisualStateGroups(templateRoot)
            .OfType<VisualStateGroup>()
            .FirstOrDefault(group => group.Name == "CommonStates");
        var stateName = commonStates?.CurrentState?.Name;

        var swatch = stateName switch
        {
            "PointerOver" => _iconForegroundHoverSwatch,
            "Pressed" => _iconForegroundPressedSwatch,
            _ => _iconForegroundDefaultSwatch,
        };

        SetCaptionIconColor(button, swatch?.Fill);
    }

    private void SetCaptionIconColor(EtherButton? button, Brush? color)
    {
        if (color is null)
        {
            return;
        }

        if (ReferenceEquals(button, _minimizeButton))
        {
            if (_minimizeIcon is not null)
            {
                _minimizeIcon.Fill = color;
            }
        }
        else if (ReferenceEquals(button, _maximizeRestoreButton))
        {
            if (_maximizeIcon is not null)
            {
                _maximizeIcon.Stroke = color;
            }

            if (_restoreIconBack is not null)
            {
                _restoreIconBack.Stroke = color;
            }

            if (_restoreIconFront is not null)
            {
                _restoreIconFront.Stroke = color;
            }
        }
        else if (ReferenceEquals(button, _closeButton))
        {
            if (_closeIconStroke1 is not null)
            {
                _closeIconStroke1.Stroke = color;
            }

            if (_closeIconStroke2 is not null)
            {
                _closeIconStroke2.Stroke = color;
            }
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

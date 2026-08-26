using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace EtherSandbox.Controls;

/// <summary>
/// A templated <see cref="ComboBox"/>. The default visual is the compact Ether
/// dropdown via <c>DefaultEtherDropdownStyle</c>.
/// </summary>
/// <remarks>
/// Derives from <see cref="ComboBox"/> rather than restyling one through attached behaviours,
/// so template parts are reached with <see cref="Control.GetTemplateChild"/> instead of walking
/// the visual tree on every open, and the content-hugging width is produced by
/// <see cref="MeasureOverride"/> instead of assigning <see cref="FrameworkElement.Width"/>
/// (which would pin a local value and stop the control participating in layout).
///
/// Two ComboBox behaviours the template has to work around, both verified by inspecting the
/// live visual tree:
/// <list type="bullet">
/// <item>The part named "ContentPresenter" is framework-owned: ComboBox clears its Content while
/// the menu is open, and renaming it away silently breaks the control's own DropDownStates
/// transitions. It is kept, hidden, and the selection is shown through "TriggerText" instead.</item>
/// <item>The default items panel is CarouselPanel, which centres the menu on the selected item and
/// takes scrolling over itself, leaving the host ScrollViewer with a zero extent and no scroll
/// bar. The template swaps in a StackPanel.</item>
/// </list>
/// Native ComboBox drives CommonStates, FocusStates, and DropDownStates. Default visual
/// setters live on <c>DefaultEtherDropdownStyle</c>.
/// </remarks>
[TemplatePart(Name = TriggerTextPart, Type = typeof(TextBlock))]
[TemplatePart(Name = ArrowPart, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PopupPart, Type = typeof(Popup))]
[TemplatePart(Name = PopupBorderPart, Type = typeof(Border))]
[TemplatePart(Name = MenuScrollViewerPart, Type = typeof(ScrollViewer))]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = NormalState)]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = PointerOverState)]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = PressedState)]
[TemplateVisualState(GroupName = CommonStatesGroup, Name = DisabledState)]
[TemplateVisualState(GroupName = FocusStatesGroup, Name = FocusedState)]
[TemplateVisualState(GroupName = FocusStatesGroup, Name = UnfocusedState)]
[TemplateVisualState(GroupName = DropDownStatesGroup, Name = OpenedState)]
[TemplateVisualState(GroupName = DropDownStatesGroup, Name = ClosedState)]
public sealed class EtherDropdown : ComboBox
{
    private const string TriggerTextPart = "TriggerText";
    private const string ArrowPart = "Arrow";
    private const string PopupPart = "Popup";
    private const string PopupBorderPart = "PopupBorder";
    private const string MenuScrollViewerPart = "ScrollViewer";
    private const string CommonStatesGroup = "CommonStates";
    private const string NormalState = "Normal";
    private const string PointerOverState = "PointerOver";
    private const string PressedState = "Pressed";
    private const string DisabledState = "Disabled";
    private const string FocusStatesGroup = "FocusStates";
    private const string FocusedState = "Focused";
    private const string UnfocusedState = "Unfocused";
    private const string DropDownStatesGroup = "DropDownStates";
    private const string OpenedState = "Opened";
    private const string ClosedState = "Closed";

    /// <summary>Fallback for the trailing icon's width when the template part is unavailable.</summary>
    private const double FallbackArrowWidth = 16;

    private TextBlock? _triggerText;
    private FrameworkElement? _arrow;
    private Popup? _popup;
    private Border? _popupBorder;
    private ScrollViewer? _menuScrollViewer;

    private double? _longestItemWidth;

    /// <summary>
    /// Initializes a new instance of the <see cref="EtherDropdown"/> class.
    /// </summary>
    public EtherDropdown()
    {
        DefaultStyleKey = typeof(EtherDropdown);
        SelectionChanged += (_, _) => UpdateTriggerText();
        SizeChanged += (_, _) => UpdateMenuLayout();

        // Width is derived from the items, so it goes stale whenever they change. Items covers
        // both direct children and an ItemsSource projection; ItemsSource itself is watched
        // separately because swapping the whole collection does not raise VectorChanged.
        Items.VectorChanged += OnItemsVectorChanged;
        RegisterPropertyChangedCallback(ItemsSourceProperty, (_, _) => InvalidateItemMetrics());
    }

    /// <summary>Identifies the <see cref="MaxVisibleItems"/> dependency property.</summary>
    public static readonly DependencyProperty MaxVisibleItemsProperty =
        DependencyProperty.Register(
            nameof(MaxVisibleItems),
            typeof(int),
            typeof(EtherDropdown),
            new PropertyMetadata(0));

    /// <summary>Items shown before the menu starts scrolling. Zero or less leaves it unbounded.</summary>
    public int MaxVisibleItems
    {
        get => (int)GetValue(MaxVisibleItemsProperty);
        set => SetValue(MaxVisibleItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="MenuGap"/> dependency property.</summary>
    public static readonly DependencyProperty MenuGapProperty =
        DependencyProperty.Register(
            nameof(MenuGap),
            typeof(double),
            typeof(EtherDropdown),
            new PropertyMetadata(0d));

    /// <summary>Gap between the trigger and the menu.</summary>
    public double MenuGap
    {
        get => (double)GetValue(MenuGapProperty);
        set => SetValue(MenuGapProperty, value);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _triggerText = GetTemplateChild(TriggerTextPart) as TextBlock;
        _arrow = GetTemplateChild(ArrowPart) as FrameworkElement;
        _popup = GetTemplateChild(PopupPart) as Popup;
        _popupBorder = GetTemplateChild(PopupBorderPart) as Border;
        _menuScrollViewer = GetTemplateChild(MenuScrollViewerPart) as ScrollViewer;

        InvalidateItemMetrics();
        UpdateTriggerText();
    }

    private void OnItemsVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args) =>
        InvalidateItemMetrics();

    private void InvalidateItemMetrics()
    {
        _longestItemWidth = null;
        InvalidateMeasure();
        UpdateTriggerText();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var baseSize = base.MeasureOverride(availableSize);

        var arrowWidth = _arrow?.ActualWidth > 0 ? _arrow.ActualWidth
            : _arrow?.Width > 0 ? _arrow.Width
            : FallbackArrowWidth;

        var hugged = MeasureLongestItemWidth() + Padding.Left + Padding.Right + arrowWidth;
        var width = Math.Max(MinWidth, Math.Ceiling(hugged));

        if (!double.IsInfinity(availableSize.Width))
            width = Math.Min(width, Math.Max(MinWidth, availableSize.Width));

        return new Size(width, baseSize.Height);
    }

    /// <summary>Widest item label, measured with the trigger's own text settings.</summary>
    private double MeasureLongestItemWidth()
    {
        if (_longestItemWidth is { } cached)
            return cached;

        var widest = 0d;
        foreach (var item in Items)
        {
            var text = ContentOf(item)?.ToString();
            if (string.IsNullOrEmpty(text))
                continue;

            var measurer = CreateTextProbe(text);
            measurer.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            widest = Math.Max(widest, measurer.DesiredSize.Width);
        }

        _longestItemWidth = widest;
        return widest;
    }

    /// <summary>
    /// Mirrors the trigger's own text settings so the measurement matches what is rendered,
    /// rather than restating font values that live in the template.
    /// </summary>
    private TextBlock CreateTextProbe(string text) => new()
    {
        Text = text,
        FontFamily = _triggerText?.FontFamily ?? FontFamily,
        FontSize = _triggerText?.FontSize ?? FontSize,
        FontWeight = _triggerText?.FontWeight ?? FontWeights.Medium,
        CharacterSpacing = _triggerText?.CharacterSpacing ?? CharacterSpacing,
        TextWrapping = TextWrapping.NoWrap
    };

    private static object? ContentOf(object? item) =>
        item is ComboBoxItem container ? container.Content : item;

    /// <summary>
    /// ComboBox clears both SelectionBoxItem and its "ContentPresenter" part while the menu is
    /// open, so neither can drive the trigger's text. TriggerText is ours alone.
    /// </summary>
    private void UpdateTriggerText()
    {
        if (_triggerText is null)
            return;

        _triggerText.Text = ContentOf(SelectedItem)?.ToString() ?? string.Empty;
    }

    protected override void OnDropDownOpened(object e)
    {
        base.OnDropDownOpened(e);
        UpdateMenuLayout();

        if (_popup?.Child is FrameworkElement popupContent)
        {
            // The menu settles over more than one layout pass as its items realise, so keep
            // correcting until it closes. SizeChanged rather than LayoutUpdated: LayoutUpdated
            // also fires for the pass our own offset write causes, which turns this into an
            // infinite reposition loop and throws LayoutCycleException.
            popupContent.SizeChanged += OnPopupContentSizeChanged;
        }
    }

    protected override void OnDropDownClosed(object e)
    {
        base.OnDropDownClosed(e);

        if (_popup?.Child is FrameworkElement popupContent)
            popupContent.SizeChanged -= OnPopupContentSizeChanged;
    }

    private void OnPopupContentSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsDropDownOpen)
            UpdateMenuLayout();
    }

    private void UpdateMenuLayout()
    {
        if (!IsDropDownOpen)
            return;

        ApplyMenuWidth();
        ApplyMaxVisibleHeight();
        PositionMenu();
    }

    /// <summary>Keeps the menu the same width as the trigger.</summary>
    private void ApplyMenuWidth()
    {
        if (_popupBorder is null || ActualWidth <= 0)
            return;

        if (Math.Abs(_popupBorder.Width - ActualWidth) > 0.5)
            _popupBorder.Width = ActualWidth;
    }

    private void ApplyMaxVisibleHeight()
    {
        if (_menuScrollViewer is null || MaxVisibleItems <= 0 || Items.Count <= MaxVisibleItems)
        {
            if (_menuScrollViewer is not null && MaxVisibleItems <= 0)
                _menuScrollViewer.MaxHeight = double.PositiveInfinity;
            return;
        }

        // Force layout so realised containers can be measured on this very open rather than
        // only from the second one onwards.
        _menuScrollViewer.UpdateLayout();

        var spacing = (ItemsPanelRoot as StackPanel)?.Spacing ?? 0;
        var maxHeight = MeasureVisibleItemsHeight(MaxVisibleItems, spacing);
        if (maxHeight <= 0)
            return;

        // Only write a genuinely different value: this also runs while the menu is on screen,
        // and re-assigning an unchanged MaxHeight would re-invalidate layout for nothing.
        if (Math.Abs(_menuScrollViewer.MaxHeight - maxHeight) > 0.5)
            _menuScrollViewer.MaxHeight = maxHeight;
    }

    /// <summary>
    /// Height of the first <paramref name="count"/> items plus the gaps between them. Sums the
    /// realised containers individually so items of differing heights stay exact; only the
    /// not-yet-realised case falls back to assuming a uniform height.
    /// </summary>
    private double MeasureVisibleItemsHeight(int count, double spacing)
    {
        var total = 0d;
        var counted = 0;

        for (var index = 0; index < Items.Count && counted < count; index++)
        {
            if (ContainerFromIndex(index) is FrameworkElement { ActualHeight: > 0 } container)
            {
                total += container.ActualHeight;
                counted++;
            }
        }

        if (counted == count)
            return total + (spacing * (count - 1));

        var probeHeight = MeasureProbeItemHeight();
        return probeHeight <= 0 ? 0 : (probeHeight * count) + (spacing * (count - 1));
    }

    /// <summary>Measures a throwaway container carrying the real item style.</summary>
    private double MeasureProbeItemHeight()
    {
        var probe = new ComboBoxItem
        {
            Content = Items.Count > 0 ? ContentOf(Items[0]) : "Ag",
            Style = ItemContainerStyle
        };

        probe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return probe.DesiredSize.Height;
    }

    /// <summary>
    /// Places the menu directly under the trigger, flipping above it when there is not enough
    /// room below.
    /// </summary>
    /// <remarks>
    /// Moves the popup's content with a RenderTransform rather than Popup.VerticalOffset:
    /// ComboBox manages the Popup part's own offset for its selected-item-centred placement, so
    /// writes to Offset get overwritten by the framework's placement pass. A RenderTransform on
    /// the content is outside anything ComboBox inspects, so it sticks.
    /// </remarks>
    private void PositionMenu()
    {
        if (_popup?.Child is not FrameworkElement popupContent ||
            XamlRoot?.Content is not FrameworkElement rootContent)
        {
            return;
        }

        _popup.ShouldConstrainToRootBounds = false;

        if (popupContent.RenderTransform is not TranslateTransform transform)
        {
            transform = new TranslateTransform();
            popupContent.RenderTransform = transform;
        }

        // Zero first so the position measured next is the untransformed layout position WinUI
        // just placed the content at, not wherever the previous correction left it.
        transform.X = 0;
        transform.Y = 0;

        // Placement is a delta between two points, so the frame only has to be common to both.
        var triggerTopLeft = TransformToVisual(rootContent).TransformPoint(new Point(0, 0));
        var layoutPosition = popupContent.TransformToVisual(rootContent).TransformPoint(new Point(0, 0));

        // Measure the menu itself, not popupContent: ComboBox wraps the popup's content in a
        // Canvas that spans the whole window, so its height is the window's and would make
        // every menu look too tall to fit below.
        var menu = _popupBorder ?? popupContent;
        var menuHeight = menu.ActualHeight > 0 ? menu.ActualHeight : menu.DesiredSize.Height;

        // Whether it fits, though, is a question about the window — and rootContent's
        // ActualHeight is not the window's (it is laid out to its own content, which can end
        // exactly at the trigger and make every menu look like it overflows). Ask XamlRoot for
        // the real content size and take the trigger's position in that same window frame.
        var windowHeight = XamlRoot.Size.Height;
        var triggerTopInWindow = TransformToVisual(null).TransformPoint(new Point(0, 0)).Y;
        var spaceBelow = windowHeight - (triggerTopInWindow + ActualHeight) - MenuGap;
        var spaceAbove = triggerTopInWindow - MenuGap;

        var flipAbove = menuHeight > spaceBelow && spaceAbove > spaceBelow;
        var targetY = flipAbove
            ? triggerTopLeft.Y - MenuGap - menuHeight
            : triggerTopLeft.Y + ActualHeight + MenuGap;

        transform.X = triggerTopLeft.X - layoutPosition.X;
        transform.Y = targetY - layoutPosition.Y;
    }
}

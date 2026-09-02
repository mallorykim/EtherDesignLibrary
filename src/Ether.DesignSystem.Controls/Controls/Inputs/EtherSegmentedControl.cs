using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ether.DesignSystem.Controls;

/// <summary>
/// A templated <see cref="ContentControl"/> host for segmented radio items. The default
/// visual is the Figma track via <c>DefaultEtherSegmentedControlStyle</c>.
/// </summary>
/// <remarks>
/// Call sites may still set <c>Style="{StaticResource EtherSegmentedTrack}"</c>; that key
/// is an alias of the default style. Segment item chrome stays on the keyed
/// <c>EtherSegment</c> <see cref="RadioButton"/> style. Put inline segments in
/// <see cref="EtherSegmentPanel"/> so the track can stretch with the parent and keep
/// equal-width slots. For data-backed segments, use <see cref="ItemsSource"/> with
/// <see cref="ItemTemplate"/> or <see cref="DisplayMemberPath"/>. The data-backed path
/// generates <see cref="EtherSegmentRadioButton"/> children inside the same panel and
/// keeps <see cref="SelectedIndex"/>, <see cref="SelectedItem"/>, and
/// <see cref="SelectedValue"/> synchronized. Inline segments keep the existing
/// <see cref="FrameworkElement.Tag"/>/Content value contract. Native radio grouping and
/// <see cref="EtherSegmentRadioButton"/> layer opacity remain the interaction model; the
/// host does not add visual-state groups. An optional <see cref="SelectionCommand"/>
/// gives the <see cref="ItemsSource"/>-generated path an action hook: it fires only for a
/// user-initiated pick (never for a programmatic <see cref="SelectedIndex"/>/
/// <see cref="SelectedItem"/>/<see cref="SelectedValue"/> assignment), mirroring how a
/// <see cref="Microsoft.UI.Xaml.Controls.Primitives.ButtonBase.Command"/> fires on Click,
/// not on a programmatic state change.
/// </remarks>
[TemplatePart(Name = TrackSurfacePartName, Type = typeof(Border))]
public class EtherSegmentedControl : ContentControl
{
    private const string TrackSurfacePartName = "TrackSurface";
    private readonly List<RadioButton> _segments = [];
    private object? _inlineContent;
    private INotifyCollectionChanged? _itemsSourceCollection;
    private bool _managingItemsSourceContent;
    private bool _synchronizingSelection;

    /// <summary>Identifies the <see cref="ItemsSource"/> dependency property. Registered default is <see langword="null"/>.</summary>
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource),
        typeof(object),
        typeof(EtherSegmentedControl),
        new PropertyMetadata(null, OnItemsSourceChanged));

    /// <summary>Identifies the <see cref="ItemTemplate"/> dependency property. Registered default is <see langword="null"/>.</summary>
    public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
        nameof(ItemTemplate),
        typeof(DataTemplate),
        typeof(EtherSegmentedControl),
        new PropertyMetadata(null, OnItemPresentationChanged));

    /// <summary>Identifies the <see cref="DisplayMemberPath"/> dependency property. Registered default is <see langword="null"/>.</summary>
    public static readonly DependencyProperty DisplayMemberPathProperty = DependencyProperty.Register(
        nameof(DisplayMemberPath),
        typeof(string),
        typeof(EtherSegmentedControl),
        new PropertyMetadata(null, OnItemPresentationChanged));

    /// <summary>Identifies the <see cref="SelectedIndex"/> dependency property. Registered default is -1.</summary>
    public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
        nameof(SelectedIndex),
        typeof(int),
        typeof(EtherSegmentedControl),
        new PropertyMetadata(-1, OnSelectedIndexChanged));

    /// <summary>Identifies the <see cref="SelectedItem"/> dependency property. Registered default is <see langword="null"/>.</summary>
    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
        nameof(SelectedItem),
        typeof(object),
        typeof(EtherSegmentedControl),
        new PropertyMetadata(null, OnSelectedItemChanged));

    /// <summary>Identifies the <see cref="SelectedValue"/> dependency property. Registered default is <see langword="null"/>.</summary>
    public static readonly DependencyProperty SelectedValueProperty = DependencyProperty.Register(
        nameof(SelectedValue),
        typeof(object),
        typeof(EtherSegmentedControl),
        new PropertyMetadata(null, OnSelectedValueChanged));

    /// <summary>Identifies the <see cref="SelectionCommand"/> dependency property. Registered default is <see langword="null"/>.</summary>
    public static readonly DependencyProperty SelectionCommandProperty = DependencyProperty.Register(
        nameof(SelectionCommand),
        typeof(ICommand),
        typeof(EtherSegmentedControl),
        new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="SelectionCommandParameter"/> dependency property. Registered default is <see langword="null"/>.</summary>
    public static readonly DependencyProperty SelectionCommandParameterProperty = DependencyProperty.Register(
        nameof(SelectionCommandParameter),
        typeof(object),
        typeof(EtherSegmentedControl),
        new PropertyMetadata(null));

    /// <summary>Gets or sets the collection used to generate the segment items. Default is <see langword="null"/>; null selects the inline-content path.</summary>
    public object? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Gets or sets the template used to display each item from <see cref="ItemsSource"/>. Default is <see langword="null"/>.</summary>
    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the property path used to display each item when <see cref="ItemTemplate"/>
    /// is not set. Default is <see langword="null"/>.
    /// </summary>
    public string? DisplayMemberPath
    {
        get => (string?)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    /// <summary>Gets or sets the zero-based index of the selected segment, or -1 when none is selected. Default is -1.</summary>
    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>Gets or sets the selected segment item. Default is <see langword="null"/>.</summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// Gets or sets the selected segment's <see cref="FrameworkElement.Tag"/>, or its
    /// Content when Tag is null. For data-backed segments this is the item from
    /// <see cref="ItemsSource"/>. Default is <see langword="null"/>.
    /// </summary>
    public object? SelectedValue
    {
        get => GetValue(SelectedValueProperty);
        set => SetValue(SelectedValueProperty, value);
    }

    /// <summary>
    /// Gets or sets the command invoked when the <b>user</b> selects a segment. Default is
    /// <see langword="null"/>. Fires only for a user-initiated pick (the segment's own
    /// <see cref="Microsoft.UI.Xaml.Controls.Primitives.ToggleButton.Checked"/> event, the same
    /// path a pointer click or keyboard selection drives) — never for a programmatic
    /// <see cref="SelectedIndex"/>, <see cref="SelectedItem"/>, or <see cref="SelectedValue"/>
    /// assignment, and never while a previous selection is still being synchronized. This
    /// mirrors <see cref="Microsoft.UI.Xaml.Controls.Primitives.ButtonBase.Command"/>, which
    /// fires on Click, not on a programmatic state change. Execution is guarded by
    /// <see cref="ICommand.CanExecute(object?)"/>.
    /// </summary>
    public ICommand? SelectionCommand
    {
        get => (ICommand?)GetValue(SelectionCommandProperty);
        set => SetValue(SelectionCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the parameter passed to <see cref="SelectionCommand"/>. Default is
    /// <see langword="null"/>, in which case the newly selected <see cref="SelectedValue"/> is
    /// passed instead.
    /// </summary>
    public object? SelectionCommandParameter
    {
        get => GetValue(SelectionCommandParameterProperty);
        set => SetValue(SelectionCommandParameterProperty, value);
    }

    /// <summary>Raised after the selected segment value changes.</summary>
    public event EventHandler<SegmentedSelectionChangedEventArgs>? SelectionChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="EtherSegmentedControl"/> class.
    /// </summary>
    public EtherSegmentedControl()
    {
        DefaultStyleKey = typeof(EtherSegmentedControl);
        Loaded += OnLoaded;
        RegisterPropertyChangedCallback(ContentProperty, (_, _) =>
        {
            if (!_managingItemsSourceContent && ItemsSource is null)
            {
                _inlineContent = Content;
                WireSegments();
            }
        });
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        WireSegments();
    }

    private void OnLoaded(object sender, RoutedEventArgs args) => WireSegments();

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (EtherSegmentedControl)d;
        control.DetachItemsSourceCollection();

        if (e.NewValue is INotifyCollectionChanged collection)
        {
            control._itemsSourceCollection = collection;
            collection.CollectionChanged += control.OnItemsSourceCollectionChanged;
        }

        if (e.OldValue is null && !control._managingItemsSourceContent)
            control._inlineContent = control.Content;

        if (e.NewValue is null)
        {
            control.SetInlineContent();
            control.WireSegments();
        }
        else
        {
            control.RebuildGeneratedContent();
        }
    }

    private static void OnItemPresentationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (EtherSegmentedControl)d;
        if (control.ItemsSource is not null)
            control.RebuildGeneratedContent();
    }

    private static void OnSelectedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (EtherSegmentedControl)d;
        if (!control._synchronizingSelection)
            control.SelectSegmentForValue(e.NewValue);

        control.SelectionChanged?.Invoke(
            control,
            new SegmentedSelectionChangedEventArgs(e.OldValue, e.NewValue));
    }

    private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (EtherSegmentedControl)d;
        if (!control._synchronizingSelection)
            control.SelectSegmentAtIndex((int)e.NewValue);
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (EtherSegmentedControl)d;
        if (!control._synchronizingSelection)
            control.SelectSegmentForValue(e.NewValue);
    }

    private void RebuildGeneratedContent()
    {
        if (ItemsSource is null)
            return;

        var selectedValue = SelectedValue;
        var selectedItem = SelectedItem;
        var selectedIndex = SelectedIndex;
        var panel = new EtherSegmentPanel();

        if (ItemsSource is IEnumerable items)
        {
            foreach (var item in items)
                panel.Children.Add(CreateGeneratedSegment(item));
        }

        _managingItemsSourceContent = true;
        try
        {
            SetValue(ContentProperty, panel);
        }
        finally
        {
            _managingItemsSourceContent = false;
        }

        WireSegments(restoreSelection: false);
        if (selectedValue is not null)
            SelectSegmentForValue(selectedValue);
        else if (selectedItem is not null)
            SelectSegmentForValue(selectedItem);
        else if (selectedIndex >= 0)
            SelectSegmentAtIndex(selectedIndex);
        else
            SelectFirstCheckedSegment();
    }

    private EtherSegmentRadioButton CreateGeneratedSegment(object? item)
    {
        var segment = new EtherSegmentRadioButton
        {
            Tag = item,
            ContentTemplate = ItemTemplate,
            Style = ResolveSegmentStyle(),
        };

        segment.Content = ItemTemplate is not null
            ? item
            : ResolveDisplayMember(item);
        return segment;
    }

    // ItemsSource-generated segments need the same EtherSegment chrome that inline call sites apply
    // explicitly via Style="{StaticResource EtherSegment}". Resolve that keyed Style from the element
    // resource scope (this control, up its parent chain, then Application), searching MergedDictionaries
    // recursively - ResourceDictionary.TryGetValue does not descend merged dictionaries on its own (the
    // same reason EtherTabItem.FindStyle recurses by hand). Done in code, with no default style / no
    // XAML change, so it cannot perturb the control's XAML compilation.
    private Style? ResolveSegmentStyle()
    {
        for (FrameworkElement? current = this; current is not null; current = current.Parent as FrameworkElement)
        {
            if (FindStyleInDictionary(current.Resources, "EtherSegment") is { } scoped)
                return scoped;
        }

        if (Application.Current?.Resources is { } appResources &&
            FindStyleInDictionary(appResources, "EtherSegment") is { } appStyle)
        {
            return appStyle;
        }

        return null;
    }

    private static Style? FindStyleInDictionary(ResourceDictionary dictionary, string key)
    {
        if (dictionary.TryGetValue(key, out var value) && value is Style style)
            return style;

        foreach (var merged in dictionary.MergedDictionaries)
        {
            if (FindStyleInDictionary(merged, key) is { } nested)
                return nested;
        }

        return null;
    }

    private object? ResolveDisplayMember(object? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(DisplayMemberPath))
            return item;

        object? current = item;
        foreach (var memberName in DisplayMemberPath.Split('.'))
        {
            if (current is null)
                return null;

            var property = current.GetType().GetProperty(
                memberName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            current = property?.GetValue(current);
        }

        return current;
    }

    private void SetInlineContent()
    {
        _managingItemsSourceContent = true;
        try
        {
            SetValue(ContentProperty, _inlineContent);
        }
        finally
        {
            _managingItemsSourceContent = false;
        }
    }

    private void WireSegments(bool restoreSelection = true)
    {
        foreach (var segment in _segments)
            segment.Checked -= OnSegmentChecked;

        _segments.Clear();
        AddSegments(Content, _segments);

        foreach (var segment in _segments)
            segment.Checked += OnSegmentChecked;

        if (restoreSelection)
            SynchronizeSelectionFromProperties();
    }

    private void SynchronizeSelectionFromProperties()
    {
        if (SelectedIndex >= 0)
        {
            SelectSegmentAtIndex(SelectedIndex);
            return;
        }

        if (SelectedItem is not null)
        {
            SelectSegmentForValue(SelectedItem);
            return;
        }

        if (SelectedValue is not null)
        {
            SelectSegmentForValue(SelectedValue);
            return;
        }

        SelectFirstCheckedSegment();
    }

    private static void AddSegments(object? content, ICollection<RadioButton> target)
    {
        switch (content)
        {
            case RadioButton segment:
                target.Add(segment);
                break;
            case Panel panel:
                foreach (var child in panel.Children)
                    AddSegments(child, target);
                break;
            case ContentControl contentControl:
                AddSegments(contentControl.Content, target);
                break;
        }
    }

    private void SelectFirstCheckedSegment()
    {
        var selected = _segments.FirstOrDefault(segment => segment.IsChecked == true);
        if (selected is not null)
            SetSelectedValueFromSegment(selected);
    }

    private void SelectSegmentForValue(object? value)
    {
        var selected = value is null
            ? null
            : _segments.FirstOrDefault(segment => Equals(GetSegmentValue(segment), value));
        ApplySelection(selected);
    }

    private void SelectSegmentAtIndex(int index)
    {
        ApplySelection(index >= 0 && index < _segments.Count ? _segments[index] : null);
    }

    private void OnSegmentChecked(object sender, RoutedEventArgs e)
    {
        if (!_synchronizingSelection && sender is RadioButton segment)
            SetSelectedValueFromSegment(segment, userInitiated: true);
    }

    private void SetSelectedValueFromSegment(RadioButton segment, bool userInitiated = false)
        => ApplySelection(segment, userInitiated);

    private void ApplySelection(RadioButton? selected, bool userInitiated = false)
    {
        var selectedIndex = selected is null ? -1 : _segments.IndexOf(selected);
        var selectedValue = selected is null ? null : GetSegmentValue(selected);

        _synchronizingSelection = true;
        try
        {
            foreach (var segment in _segments)
                segment.IsChecked = ReferenceEquals(segment, selected);

            SetValue(SelectedIndexProperty, selectedIndex);
            SetValue(SelectedItemProperty, selectedValue);
            SetValue(SelectedValueProperty, selectedValue);
        }
        finally
        {
            _synchronizingSelection = false;
        }

        // Only a real user pick (never a programmatic SelectedIndex/SelectedItem/SelectedValue
        // assignment, and never the ItemsSource rebuild's selection restore) invokes the command -
        // see the userInitiated=true call in OnSegmentChecked above, the sole caller that passes it.
        if (userInitiated)
            InvokeSelectionCommand(selectedValue);
    }

    private void InvokeSelectionCommand(object? selectedValue)
    {
        if (SelectionCommand is not { } command)
            return;

        var parameter = SelectionCommandParameter ?? selectedValue;
        if (command.CanExecute(parameter))
            command.Execute(parameter);
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => RebuildGeneratedContent();

    private void DetachItemsSourceCollection()
    {
        if (_itemsSourceCollection is not null)
            _itemsSourceCollection.CollectionChanged -= OnItemsSourceCollectionChanged;

        _itemsSourceCollection = null;
    }

    private static object? GetSegmentValue(RadioButton segment)
        => segment.Tag ?? segment.Content;
}

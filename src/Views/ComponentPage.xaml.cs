using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace EtherSandbox.Views;

/// <summary>
/// Shared chrome for every component showcase page: title, one-line description,
/// an INTERACTIVE section and an optional STATES section. See the class remarks
/// in ComponentPage.xaml for the reasoning behind the optional STATES content.
/// </summary>
public sealed partial class ComponentPage : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(ComponentPage), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(ComponentPage), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty InteractiveContentProperty =
        DependencyProperty.Register(nameof(InteractiveContent), typeof(object), typeof(ComponentPage), new PropertyMetadata(null));

    public static readonly DependencyProperty StatesContentProperty =
        DependencyProperty.Register(nameof(StatesContent), typeof(object), typeof(ComponentPage), new PropertyMetadata(null));

    /// <summary>Header for the second card. Defaults to "STATES"; pages whose second section
    /// shows something else (e.g. the Progress Bar's variants) can set "TYPES", "VARIANTS", etc.</summary>
    public static readonly DependencyProperty StatesTitleProperty =
        DependencyProperty.Register(nameof(StatesTitle), typeof(string), typeof(ComponentPage), new PropertyMetadata("STATES"));

    /// <summary>Set true on pages whose component has a disabled state, to surface a "Disabled"
    /// checkbox in the INTERACTIVE header that disables the live specimen so it can be inspected.</summary>
    public static readonly DependencyProperty HasDisabledToggleProperty =
        DependencyProperty.Register(nameof(HasDisabledToggle), typeof(bool), typeof(ComponentPage), new PropertyMetadata(false));

    /// <summary>Optional page-specific controls (e.g. the Button page's "Trailing icon" checkbox)
    /// shown in the INTERACTIVE header beside the Disabled checkbox. They stay enabled when the
    /// specimen is disabled, since they live outside the disable-able host.</summary>
    public static readonly DependencyProperty InteractiveControlsProperty =
        DependencyProperty.Register(nameof(InteractiveControls), typeof(object), typeof(ComponentPage), new PropertyMetadata(null));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public object InteractiveContent
    {
        get => GetValue(InteractiveContentProperty);
        set => SetValue(InteractiveContentProperty, value);
    }

    public object StatesContent
    {
        get => GetValue(StatesContentProperty);
        set => SetValue(StatesContentProperty, value);
    }

    public string StatesTitle
    {
        get => (string)GetValue(StatesTitleProperty);
        set => SetValue(StatesTitleProperty, value);
    }

    public bool HasDisabledToggle
    {
        get => (bool)GetValue(HasDisabledToggleProperty);
        set => SetValue(HasDisabledToggleProperty, value);
    }

    public object InteractiveControls
    {
        get => GetValue(InteractiveControlsProperty);
        set => SetValue(InteractiveControlsProperty, value);
    }

    public ComponentPage()
    {
        this.InitializeComponent();

        // Show the UPDATED / PRIMITIVE TOKEN badges when the hosting page is flagged in the
        // catalog. Resolved from the hosting Page's type (walked up the tree) so the catalog
        // stays the single source — the page itself never hard-codes the flag.
        this.Loaded += (_, _) =>
        {
            DependencyObject? node = this;
            while (node is not null and not Page)
                node = VisualTreeHelper.GetParent(node);

            if (node is Page page)
            {
                if (ComponentCatalog.IsUpdated(page.GetType()))
                    UpdatedBadgeElement.Visibility = Visibility.Visible;
                if (ComponentCatalog.IsPrimitive(page.GetType()))
                    PrimitiveBadgeElement.Visibility = Visibility.Visible;
            }
        };
    }

    /// <summary>Hides the STATES card when a page has no distinct state swatches to show.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Compiled x:Bind invokes this public instance method; changing it to static would break the binding contract.")]
    public Visibility ConvertHasStates(object? content) => content is null ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>Visible when true (used for the opt-in Disabled checkbox).</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Compiled x:Bind invokes this public instance method; changing it to static would break the binding contract.")]
    public Visibility BoolToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Disable / re-enable the live specimen from the Disabled checkbox. IsEnabled on the
    /// host ContentControl cascades to the specimen subtree, showing its disabled visual state.
    /// When InteractiveContent is a <see cref="ControlExample"/>, only the specimen host is
    /// disabled so SOURCE/Copy stay usable.</summary>
    private void OnDisabledToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox check)
            return;

        var enabled = !(check.IsChecked ?? false);
        if (InteractiveContent is ControlExample example)
        {
            if (InteractiveHost is not null)
                InteractiveHost.IsEnabled = true;
            example.IsExampleEnabled = enabled;
            return;
        }

        if (InteractiveHost is not null)
            InteractiveHost.IsEnabled = enabled;
    }
}

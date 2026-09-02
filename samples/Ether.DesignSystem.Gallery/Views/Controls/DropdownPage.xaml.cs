using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Controls;

public sealed partial class DropdownPage : Page, INotifyPropertyChanged
{
    public string SpecimenXaml { get; } =
        """
        <controls:EtherDropdown SelectedIndex="0">
            <ComboBoxItem Content="10 Minutes"/>
            <ComboBoxItem Content="30 Minutes"/>
            <ComboBoxItem Content="1 Hour"/>
        </controls:EtherDropdown>

        <!-- Data-bound (ItemsSource): -->
        <controls:EtherDropdown ItemsSource="{x:Bind DurationOptions}"
                                DisplayMemberPath="Label"
                                SelectedValuePath="Id"
                                SelectedValue="{x:Bind SelectedDurationId, Mode=TwoWay}"/>
        """;

    /// <summary>Backing collection for the "Data-bound (ItemsSource)" specimen below — a plain
    /// <see cref="ObservableCollection{T}"/>, no MVVM package required.</summary>
    public ObservableCollection<DurationOption> DurationOptions { get; } = new()
    {
        new DurationOption("10m", "10 Minutes"),
        new DurationOption("30m", "30 Minutes"),
        new DurationOption("1h", "1 Hour"),
    };

    private string? _selectedDurationId;

    /// <summary>Selected value for the data-bound specimen's <c>SelectedValue</c> TwoWay binding
    /// (resolved through <c>SelectedValuePath="Id"</c>). The setter updates
    /// <see cref="LiveExample"/>'s OUTPUT text directly, proving the TwoWay binding really writes
    /// back into page state.</summary>
    public string? SelectedDurationId
    {
        get => _selectedDurationId;
        set
        {
            if (_selectedDurationId == value)
                return;

            _selectedDurationId = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDurationId)));

            if (LiveExample is null)
                return;

            var label = DurationOptions.FirstOrDefault(option => option.Id == value)?.Label;
            LiveExample.OutputText = string.IsNullOrEmpty(label)
                ? GalleryStrings.Get("GalleryOutput.SelectedEmpty", "Selected: —")
                : GalleryStrings.Format("GalleryOutput.Selected", "Selected: {0}", label);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DropdownPage()
    {
        // Set the backing field directly (not the property setter) so the initial x:Bind read,
        // wired up inside InitializeComponent(), sees the default selection without the setter
        // touching LiveExample before it exists.
        _selectedDurationId = DurationOptions[0].Id;
        this.InitializeComponent();
        this.Loaded += (_, _) =>
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (DropdownHoverState is not null)
                    VisualStateManager.GoToState(DropdownHoverState, "PointerOver", false);
                if (DropdownPressedState is not null)
                    VisualStateManager.GoToState(DropdownPressedState, "Pressed", false);
                if (DropdownOpenState is not null)
                    VisualStateManager.GoToState(DropdownOpenState, "Opened", false);
            });
        };
    }

    private void InteractiveDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LiveExample is null)
            return;

        var content = (sender as ComboBox)?.SelectedItem is ComboBoxItem item
            ? item.Content?.ToString()
            : null;
        LiveExample.OutputText = string.IsNullOrEmpty(content)
            ? GalleryStrings.Get("GalleryOutput.SelectedEmpty", "Selected: —")
            : GalleryStrings.Format("GalleryOutput.Selected", "Selected: {0}", content);
    }
}

/// <summary>Gallery-only data item for the Dropdown "Data-bound (ItemsSource)" specimen.
/// <c>Id</c>/<c>Label</c> are regular settable properties (not <c>init</c>) — WinUI's generated
/// <c>XamlTypeInfo</c> requires settable properties and fails with CS8852 against init-only
/// ones, so this cannot be a positional record.</summary>
public sealed class DurationOption
{
    public DurationOption(string id, string label)
    {
        Id = id;
        Label = label;
    }

    public string Id { get; set; }

    public string Label { get; set; }
}

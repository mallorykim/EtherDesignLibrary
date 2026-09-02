using System.Collections.ObjectModel;
using System.ComponentModel;
using Ether.DesignSystem.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Controls;

public sealed partial class SegmentedControlPage : Page, INotifyPropertyChanged
{
    public string SpecimenXaml { get; } =
        """
        <controls:EtherSegmentedControl SelectedValue="list">
            <controls:EtherSegmentPanel>
                <controls:EtherSegmentRadioButton GroupName="SegmentedTwo" Content="List" Tag="list"
                                         Style="{StaticResource EtherSegment}"
                                         IsChecked="True"/>
                <controls:EtherSegmentRadioButton GroupName="SegmentedTwo" Content="Grid" Tag="grid"
                                         Style="{StaticResource EtherSegment}"/>
            </controls:EtherSegmentPanel>
        </controls:EtherSegmentedControl>

        <!-- Data-bound (ItemsSource): -->
        <controls:EtherSegmentedControl ItemsSource="{x:Bind Periods}"
                                        DisplayMemberPath="Label"
                                        SelectedItem="{x:Bind SelectedPeriod, Mode=TwoWay}"/>
        """;

    /// <summary>Backing collection for the "Data-bound (ItemsSource)" specimen below — a plain
    /// <see cref="ObservableCollection{T}"/>, no MVVM package required.</summary>
    public ObservableCollection<PeriodItem> Periods { get; } = new()
    {
        new PeriodItem("Today"),
        new PeriodItem("This Week"),
        new PeriodItem("This Month"),
    };

    private PeriodItem? _selectedPeriod;

    /// <summary>Selected item for the data-bound specimen's <c>SelectedItem</c> TwoWay binding.
    /// The setter updates <see cref="LiveExample"/>'s OUTPUT text directly, proving the TwoWay
    /// binding really writes back into page state (not just a one-way display).</summary>
    public PeriodItem? SelectedPeriod
    {
        get => _selectedPeriod;
        set
        {
            if (Equals(_selectedPeriod, value))
                return;

            _selectedPeriod = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedPeriod)));

            if (LiveExample is not null)
                LiveExample.OutputText = GalleryStrings.Format("GalleryOutput.Selected", "Selected: {0}", value?.Label ?? "(none)");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // The STATES preview swatches force their look via HandRadioButton.PreviewState in XAML
    // (HandRadioButton is a thin Gallery-only subclass of the shipped
    // Ether.DesignSystem.Controls.EtherSegmentRadioButton), so no code-behind visual-state
    // poking is needed here.
    public SegmentedControlPage()
    {
        // Set the backing field directly (not the property setter) so the initial x:Bind read,
        // wired up inside InitializeComponent(), sees the default selection without the setter
        // touching LiveExample before it exists.
        _selectedPeriod = Periods[0];
        this.InitializeComponent();
    }

    private void InteractiveSegmentedControl_SelectionChanged(object sender, SegmentedSelectionChangedEventArgs e)
    {
        if (LiveExample is not null)
            LiveExample.OutputText = GalleryStrings.Format("GalleryOutput.Selected", "Selected: {0}", e.NewValue ?? "(none)");
    }
}

/// <summary>Gallery-only data item for the SegmentedControl "Data-bound (ItemsSource)" specimen.
/// <c>Label</c> is a regular settable property (not <c>init</c>) — WinUI's generated
/// <c>XamlTypeInfo</c> requires a settable property and fails with CS8852 against an
/// init-only one, so this cannot be a positional record.</summary>
public sealed class PeriodItem
{
    public PeriodItem(string label) => Label = label;

    public string Label { get; set; }
}

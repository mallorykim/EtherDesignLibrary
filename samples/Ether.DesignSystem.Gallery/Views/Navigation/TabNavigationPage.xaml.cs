using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Navigation;

public sealed partial class TabNavigationPage : Page, INotifyPropertyChanged
{
    public string SpecimenXaml { get; } =
        """
        <controls:EtherTabNavigation SelectedIndex="0" SelectionChanged="TabNavigation_SelectionChanged">
            <controls:EtherTabItem Content="Overview" Icon="Home"/>
            <controls:EtherTabItem Content="Details" Icon="Document"/>
            <controls:EtherTabItem Content="History" Icon="Clock"/>
        </controls:EtherTabNavigation>

        <!-- Data-bound (ItemsSource) - note: no per-item Icon on this path, see getting-started.md §6.5 -->
        <controls:EtherTabNavigation ItemsSource="{x:Bind Sections}"
                                     SelectedIndex="{x:Bind SelectedSectionIndex, Mode=TwoWay}"/>
        """;

    /// <summary>Backing collection for the "Data-bound (ItemsSource)" specimen below — a plain
    /// <see cref="ObservableCollection{T}"/>, no MVVM package required.</summary>
    public ObservableCollection<string> Sections { get; } = new() { "Dashboard", "Reports", "Team" };

    private int _selectedSectionIndex;

    /// <summary>Selected index for the data-bound specimen's <c>SelectedIndex</c> TwoWay binding.
    /// The setter updates <see cref="LiveExample"/>'s OUTPUT text directly, proving the TwoWay
    /// binding really writes back into page state.</summary>
    public int SelectedSectionIndex
    {
        get => _selectedSectionIndex;
        set
        {
            if (_selectedSectionIndex == value)
                return;

            _selectedSectionIndex = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedSectionIndex)));

            if (LiveExample is not null && value >= 0 && value < Sections.Count)
                LiveExample.OutputText = GalleryStrings.Format("GalleryOutput.Selected", "Selected: {0}", Sections[value]);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public TabNavigationPage()
    {
        InitializeComponent();
    }

    private void TabNavigation_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView { SelectedItem: Ether.DesignSystem.Controls.EtherTabItem { Content: { } content } })
            LiveExample.OutputText = GalleryStrings.Format("GalleryOutput.Selected", "Selected: {0}", content);
    }
}

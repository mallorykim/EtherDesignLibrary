using Microsoft.UI.Xaml;

namespace Ether.DesignSystem.ConsumerFixtures.Unpackaged;

public sealed partial class MainWindow : Window
{
    internal FrameworkElement ThemeRoot => RootGrid;
    internal Microsoft.UI.Xaml.Controls.Image SvgProofImage => SvgProof;
    internal EtherSandbox.Controls.EtherProgressBar DefaultProgressBarProofControl => DefaultProgressBarProof;
    internal EtherSandbox.Controls.EtherProgressBar ProgressBarProofControl => ProgressBarProof;
    internal EtherSandbox.Controls.EtherButton DefaultButtonProofControl => DefaultButtonProof;
    internal EtherSandbox.Controls.EtherButton ButtonProofControl => ButtonProof;
    internal EtherSandbox.Controls.EtherButton SecondaryButtonProofControl => SecondaryButtonProof;
    internal EtherSandbox.Controls.EtherCheckbox DefaultCheckboxProofControl => DefaultCheckboxProof;
    internal EtherSandbox.Controls.EtherCheckbox CheckboxProofControl => CheckboxProof;
    internal EtherSandbox.Controls.EtherRadioButton DefaultRadioButtonProofControl => DefaultRadioButtonProof;
    internal EtherSandbox.Controls.EtherRadioButton RadioButtonProofControl => RadioButtonProof;
    internal EtherSandbox.Controls.EtherInput DefaultInputProofControl => DefaultInputProof;
    internal EtherSandbox.Controls.EtherInput InputProofControl => InputProof;
    internal EtherSandbox.Controls.EtherDropdown DefaultDropdownProofControl => DefaultDropdownProof;
    internal EtherSandbox.Controls.EtherDropdown DropdownProofControl => DropdownProof;
    internal EtherSandbox.Controls.EtherSegmentedControl DefaultSegmentedControlProofControl => DefaultSegmentedControlProof;
    internal EtherSandbox.Controls.EtherSegmentedControl SegmentedControlProofControl => SegmentedControlProof;
    internal EtherSandbox.Controls.EtherIntelligenceButton DefaultIntelligenceButtonProofControl => DefaultIntelligenceButtonProof;
    internal EtherSandbox.Controls.EtherIntelligenceButton IntelligenceButtonProofControl => IntelligenceButtonProof;
    internal EtherSandbox.Controls.EtherSteeringBar DefaultSteeringBarProofControl => DefaultSteeringBarProof;
    internal EtherSandbox.Controls.EtherSteeringBar SteeringBarProofControl => SteeringBarProof;

    public MainWindow()
    {
        InitializeComponent();
    }
}

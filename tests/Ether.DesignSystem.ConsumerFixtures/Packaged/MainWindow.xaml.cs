using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace Ether.DesignSystem.ConsumerFixtures.Packaged;

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
    internal EtherSandbox.Controls.EtherSlider DefaultSliderProofControl => DefaultSliderProof;
    internal EtherSandbox.Controls.EtherSlider SliderProofControl => SliderProof;
    internal EtherSandbox.Controls.EtherMasthead DefaultMastheadProofControl => DefaultMastheadProof;
    internal EtherSandbox.Controls.EtherMasthead MastheadProofControl => MastheadProof;
    internal Microsoft.UI.Xaml.Controls.ToggleSwitch BareToggleSwitchProofControl => BareToggleSwitchProof;
    internal Microsoft.UI.Xaml.Controls.ToggleSwitch DefaultToggleSwitchProofControl => DefaultToggleSwitchProof;
    internal Microsoft.UI.Xaml.Controls.ToggleSwitch ToggleSwitchProofControl => ToggleSwitchProof;
    internal Microsoft.UI.Xaml.Controls.Primitives.ScrollBar ScrollBarProofControl => ScrollBarProof;
    internal Microsoft.UI.Xaml.Controls.ScrollViewer ScrollViewerProofControl => ScrollViewerProof;

    public MainWindow()
    {
        InitializeComponent();
        SizeClientToLogicalPixels(960, 800);
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint hwnd);

    private void SizeClientToLogicalPixels(int width, int height)
    {
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var dpi = GetDpiForWindow(handle);
        var scale = dpi == 0 ? 1d : dpi / 96d;
        AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(
            (int)Math.Round(width * scale),
            (int)Math.Round(height * scale)));
    }
}

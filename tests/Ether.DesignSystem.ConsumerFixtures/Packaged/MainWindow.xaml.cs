using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ether.DesignSystem.ConsumerFixtures.Packaged;

public sealed partial class MainWindow : Window
{
    internal FrameworkElement ThemeRoot => RootGrid;
    internal Microsoft.UI.Xaml.Controls.Image SvgProofImage => SvgProof;
    internal Ether.DesignSystem.Controls.EtherProgressBar DefaultProgressBarProofControl => DefaultProgressBarProof;
    internal Ether.DesignSystem.Controls.EtherProgressBar ProgressBarProofControl => ProgressBarProof;
    internal Ether.DesignSystem.Controls.EtherButton DefaultButtonProofControl => DefaultButtonProof;
    internal Ether.DesignSystem.Controls.EtherButton ButtonProofControl => ButtonProof;
    internal Ether.DesignSystem.Controls.EtherButton SecondaryButtonProofControl => SecondaryButtonProof;
    internal Ether.DesignSystem.Controls.EtherCheckbox DefaultCheckboxProofControl => DefaultCheckboxProof;
    internal Ether.DesignSystem.Controls.EtherCheckbox CheckboxProofControl => CheckboxProof;
    internal Ether.DesignSystem.Controls.EtherRadioButton DefaultRadioButtonProofControl => DefaultRadioButtonProof;
    internal Ether.DesignSystem.Controls.EtherRadioButton RadioButtonProofControl => RadioButtonProof;
    internal Ether.DesignSystem.Controls.EtherInput DefaultInputProofControl => DefaultInputProof;
    internal Ether.DesignSystem.Controls.EtherInput InputProofControl => InputProof;
    internal Ether.DesignSystem.Controls.EtherDropdown DefaultDropdownProofControl => DefaultDropdownProof;
    internal Ether.DesignSystem.Controls.EtherDropdown DropdownProofControl => DropdownProof;
    internal Ether.DesignSystem.Controls.EtherSegmentedControl DefaultSegmentedControlProofControl => DefaultSegmentedControlProof;
    internal Ether.DesignSystem.Controls.EtherSegmentedControl SegmentedControlProofControl => SegmentedControlProof;
    internal Ether.DesignSystem.Controls.EtherIntelligenceButton DefaultIntelligenceButtonProofControl => DefaultIntelligenceButtonProof;
    internal Ether.DesignSystem.Controls.EtherIntelligenceButton IntelligenceButtonProofControl => IntelligenceButtonProof;
    internal Ether.DesignSystem.Controls.EtherSteeringBar DefaultSteeringBarProofControl => DefaultSteeringBarProof;
    internal Ether.DesignSystem.Controls.EtherSteeringBar SteeringBarProofControl => SteeringBarProof;
    internal Ether.DesignSystem.Controls.EtherSlider DefaultSliderProofControl => DefaultSliderProof;
    internal Ether.DesignSystem.Controls.EtherSlider SliderProofControl => SliderProof;
    internal Ether.DesignSystem.Controls.EtherMasthead DefaultMastheadProofControl => DefaultMastheadProof;
    internal Ether.DesignSystem.Controls.EtherMasthead MastheadProofControl => MastheadProof;
    internal Microsoft.UI.Xaml.Controls.ToggleSwitch BareToggleSwitchProofControl => BareToggleSwitchProof;
    internal Microsoft.UI.Xaml.Controls.ToggleSwitch DefaultToggleSwitchProofControl => DefaultToggleSwitchProof;
    internal Microsoft.UI.Xaml.Controls.ToggleSwitch ToggleSwitchProofControl => ToggleSwitchProof;
    internal Microsoft.UI.Xaml.Controls.Primitives.ScrollBar ScrollBarProofControl => ScrollBarProof;
    internal Microsoft.UI.Xaml.Controls.ScrollViewer ScrollViewerProofControl => ScrollViewerProof;
    internal TextBlock StatusTextProof => StatusText;

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

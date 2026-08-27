using Microsoft.UI.Xaml;

namespace Ether.DesignSystem.ConsumerFixtures.Unpackaged;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        VerifyPackageResources();
        var window = new MainWindow();
        _window = window;
        window.Activate();

        if (string.Equals(Environment.GetEnvironmentVariable("ETHER_CONSUMER_SMOKE"), "1", StringComparison.Ordinal))
        {
            try
            {
                var result = await RuntimeVerification.VerifyAsync(
                    window.ThemeRoot,
                    window.SvgProofImage,
                    window.DefaultProgressBarProofControl,
                    window.ProgressBarProofControl,
                    window.DefaultButtonProofControl,
                    window.ButtonProofControl,
                    window.SecondaryButtonProofControl,
                    window.DefaultCheckboxProofControl,
                    window.CheckboxProofControl,
                    window.DefaultRadioButtonProofControl,
                    window.RadioButtonProofControl,
                    window.DefaultInputProofControl,
                    window.InputProofControl,
                    window.DefaultDropdownProofControl,
                    window.DropdownProofControl,
                    window.DefaultSegmentedControlProofControl,
                    window.SegmentedControlProofControl,
                    window.DefaultIntelligenceButtonProofControl,
                    window.IntelligenceButtonProofControl,
                    window.DefaultSteeringBarProofControl,
                    window.SteeringBarProofControl,
                    window.DefaultSliderProofControl,
                    window.SliderProofControl,
                    window.DefaultMastheadProofControl,
                    window.MastheadProofControl,
                    window.BareToggleSwitchProofControl,
                    window.DefaultToggleSwitchProofControl,
                    window.ToggleSwitchProofControl,
                    window.ScrollBarProofControl,
                    window.ScrollViewerProofControl);
                RuntimeVerification.WriteMarker(true, result);
                window.Close();
                Exit();
            }
            catch (Exception exception)
            {
                RuntimeVerification.WriteMarker(false, exception: exception);
                window.Close();
                Exit();
            }
        }
    }

    private static void VerifyPackageResources()
    {
        if (!Current.Resources.TryGetValue("Spacing8", out _))
        {
            throw new InvalidOperationException(
                "Ether Foundation resource 'Spacing8' was not loaded from the packaged DesignSystem.xaml merge graph.");
        }

        if (!Current.Resources.TryGetValue("EtherButtonPrimary", out _))
        {
            throw new InvalidOperationException(
                "Ether control style 'EtherButtonPrimary' was not loaded from the packaged DesignSystem.xaml merge graph.");
        }
    }
}

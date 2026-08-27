using EtherSandbox.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Views.Controls;

public sealed partial class IntelligenceButtonPage : Page
{
    public string SpecimenXaml { get; } =
        """
        <controls:EtherIntelligenceButton Content="What's this mean?" />
        """;

    public IntelligenceButtonPage()
    {
        this.InitializeComponent();
        this.Loaded += (_, _) =>
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (IntelligenceHoverState is not null)
                    VisualStateManager.GoToState(IntelligenceHoverState, "PointerOver", false);
                if (IntelligencePressedState is not null)
                    VisualStateManager.GoToState(IntelligencePressedState, "Pressed", false);
            });
        };
    }

    private void IntelligenceButton_Click(object sender, RoutedEventArgs e)
    {
        if (LiveExample is not null && sender is EtherIntelligenceButton button)
            LiveExample.OutputText = GalleryStrings.Format("GalleryOutput.Clicked", "Clicked: {0}", button.Content);
    }
}

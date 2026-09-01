using Ether.DesignSystem.Controls;
using Microsoft.UI.Xaml;

namespace Ether.DesignSystem.Controls;

/// <summary>Gallery-only forced visual-state host for Tab Navigation specimens.</summary>
public class HandTabItem : EtherTabItem
{
    public enum TabPreview { Default, Selected, Hover, Pressed }

    public static readonly DependencyProperty PreviewStateProperty = DependencyProperty.Register(
        nameof(PreviewState), typeof(TabPreview), typeof(HandTabItem),
        new PropertyMetadata(TabPreview.Default, (d, _) => ((HandTabItem)d).ApplyPreviewState()));

    public TabPreview PreviewState
    {
        get => (TabPreview)GetValue(PreviewStateProperty);
        set => SetValue(PreviewStateProperty, value);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        ApplyPreviewState();
    }

    private void ApplyPreviewState()
    {
        if (XamlRoot is null) return;
        VisualStateManager.GoToState(this, PreviewState switch
        {
            TabPreview.Selected => "Selected",
            TabPreview.Hover => "PointerOver",
            TabPreview.Pressed => "Pressed",
            _ => "Normal",
        }, false);
    }
}

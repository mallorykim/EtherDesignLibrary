using System.ComponentModel;
using Microsoft.UI.Xaml;

namespace Ether.DesignSystem.Controls.Resources;

/// <summary>
/// Compiled resource dictionary that supplies the keyed EtherSwitch style and resources.
/// Consumers apply the <c>EtherSwitch</c> resource key to a stock ToggleSwitch; this is not a
/// ToggleSwitch control type. Public only because WinUI XAML resolves compiled resource
/// dictionaries through public metadata; not a supported consumer API.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed partial class EtherSwitchResources : ResourceDictionary
{
    public EtherSwitchResources()
    {
        this.InitializeComponent();
    }
}

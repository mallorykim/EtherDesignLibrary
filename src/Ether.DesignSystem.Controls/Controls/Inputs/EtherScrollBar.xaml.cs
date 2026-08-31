using System.ComponentModel;
using Microsoft.UI.Xaml;

namespace Ether.DesignSystem.Controls.Resources;

/// <summary>
/// Compiled resource dictionary that supplies the Ether ScrollBar resources.
/// Consumers merge the design-system dictionary and consume its resource keys; this is not a
/// ScrollBar control type. Public only because WinUI XAML resolves compiled resource
/// dictionaries through public metadata; not a supported consumer API.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed partial class EtherScrollBarResources : ResourceDictionary
{
    public EtherScrollBarResources()
    {
        this.InitializeComponent();
    }
}

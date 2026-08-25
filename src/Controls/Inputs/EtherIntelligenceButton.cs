using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Controls;

/// <summary>The Ether intelligence button — a templated Button with a hand cursor, matching
/// EtherButton's convention (see Controls/EtherButton.cs). No behavior changes beyond the
/// visual template and cursor; see Controls/EtherIntelligenceButton.xaml.</summary>
public sealed class EtherIntelligenceButton : Button
{
    public EtherIntelligenceButton()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }
}

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace Ether.DesignSystem.Controls;

/// <summary>
/// Public only because WinUI XAML resource dictionaries resolve <c>using:</c> types through
/// public metadata. It is template implementation support, not a supported design-system
/// control contract; consumers should use the named Ether controls instead.
/// </summary>
public class HandContentControl : ContentControl
{
    public HandContentControl()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }
}

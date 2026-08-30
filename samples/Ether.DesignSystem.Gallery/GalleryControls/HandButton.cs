using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace Ether.DesignSystem.Controls;

public class HandButton : Button
{
    public HandButton()
    {
        this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }
}

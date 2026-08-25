using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace EtherSandbox.Controls;

public class HandContentControl : ContentControl
{
    public HandContentControl()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }
}

namespace Ether.DesignSystem.Controls;

/// <summary>Identifies a caption action raised by <see cref="EtherMasthead.ActionInvoked"/>.</summary>
public enum MastheadAction
{
    /// <summary>Minimizes the host window.</summary>
    Minimize,

    /// <summary>Maximizes or restores the host window.</summary>
    MaximizeRestore,

    /// <summary>Closes the host window.</summary>
    Close
}

/// <summary>Provides data for the <see cref="EtherMasthead.ActionInvoked"/> event.</summary>
public sealed class MastheadActionInvokedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of the <see cref="MastheadActionInvokedEventArgs"/> class.</summary>
    public MastheadActionInvokedEventArgs(MastheadAction action)
    {
        Action = action;
    }

    /// <summary>Gets the caption action that was invoked.</summary>
    public MastheadAction Action { get; }
}

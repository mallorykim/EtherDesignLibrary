namespace Ether.DesignSystem.Controls;

/// <summary>Provides the old and new values for <see cref="EtherSegmentedControl.SelectionChanged"/>.</summary>
public sealed class SegmentedSelectionChangedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of the event arguments.</summary>
    public SegmentedSelectionChangedEventArgs(object? oldValue, object? newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>Gets the previously selected segment value.</summary>
    public object? OldValue { get; }

    /// <summary>Gets the newly selected segment value.</summary>
    public object? NewValue { get; }
}

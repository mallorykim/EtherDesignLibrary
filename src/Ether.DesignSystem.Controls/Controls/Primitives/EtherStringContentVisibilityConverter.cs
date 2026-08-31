using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Ether.DesignSystem.Controls.Primitives;

/// <summary>
/// Internal-template converter that selects the text or arbitrary-content presentation path.
/// </summary>
/// <remarks>
/// This type is public only because WinUI resolves types referenced by compiled resource
/// dictionaries through public XAML metadata. It is template support, not a general consumer
/// conversion utility. The owning bindings are one-way visibility bindings, so reverse
/// conversion is deliberately unsupported.
/// </remarks>
public sealed class EtherStringContentVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool valueAsBoolean)
        {
            var showWhenTrue = !string.Equals(parameter as string, "False", StringComparison.Ordinal);
            return valueAsBoolean == showWhenTrue ? Visibility.Visible : Visibility.Collapsed;
        }

        var showString = string.Equals(parameter as string, "String", StringComparison.Ordinal);
        return value is string == showString ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Always throws because every template binding that uses this converter is one-way.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException("EtherStringContentVisibilityConverter supports one-way template bindings only.");
}

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace EtherSandbox.Controls;

/// <summary>
/// Maps content type to visibility for paired text and arbitrary-content template paths.
/// </summary>
/// <remarks>
/// Use <c>String</c> as <see cref="IValueConverter.Convert"/>'s parameter to show only
/// string content; any other parameter shows only non-string content. When the source value
/// is a <see cref="bool"/>, use <c>False</c> to show only false values; any other parameter
/// shows only true values.
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

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

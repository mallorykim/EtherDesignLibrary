using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace EtherSandbox.Converters;

/// <summary>True → Visible, false → Collapsed. Used to bind an element's visibility to a
/// bool (e.g. the Home cards' UPDATED badge to <see cref="ComponentEntry.IsUpdated"/>).</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility.Visible;
}

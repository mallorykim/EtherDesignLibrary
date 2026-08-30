using System;
using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace Ether.DesignSystem.Controls.Converters;

/// <summary>Multiplies a double by the ConverterParameter (a double). Used to derive the
/// button label's optical-centering nudge from its FontSize, so the nudge scales with size
/// (see the RenderTransform on the label in EtherButton.xaml).</summary>
public sealed class MultiplyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        double v = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
        double factor = parameter is null ? 1.0 : System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
        return v * factor;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

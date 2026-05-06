using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Big_Screen_Launcher_Neorizon.Converters;

public sealed class BooleanToThicknessConverter : IValueConverter
{
    public double TrueValue { get; set; } = 4;
    public double FalseValue { get; set; } = 1;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var thickness = value is true ? TrueValue : FalseValue;
        return new Avalonia.Thickness(thickness);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Big_Screen_Launcher_Neorizon.Converters;

public sealed class BooleanToBrushConverter : IValueConverter
{
    public IBrush? TrueBrush { get; set; }
    public IBrush? FalseBrush { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? TrueBrush ?? Brushes.Transparent : FalseBrush ?? Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

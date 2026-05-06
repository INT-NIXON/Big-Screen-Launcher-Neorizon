using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Big_Screen_Launcher_Neorizon.Converters;

public sealed class StringToImageConverter : IValueConverter
{
    private static readonly HttpClient HttpClient = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
        {
            return null!;
        }

        System.Diagnostics.Debug.WriteLine($"[CoverConv] convert: {path}");

        try
        {
            if (path.StartsWith("avares://", StringComparison.OrdinalIgnoreCase))
                return new Bitmap(AssetLoader.Open(new Uri(path)));

            if (File.Exists(path))
                return new Bitmap(path);

            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var response = HttpClient.GetAsync(path).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                using var stream = response.Content.ReadAsStream();
                var bmp = new Bitmap(stream);
                System.Diagnostics.Debug.WriteLine($"[CoverConv] HTTP OK size={bmp.Size}");
                return bmp;
            }

            System.Diagnostics.Debug.WriteLine($"[CoverConv] unsupported path: {path}");
            return null!;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CoverConv] error: {ex.Message}");
            return null!;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

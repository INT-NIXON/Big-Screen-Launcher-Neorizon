#if WPF
namespace PCL.Core.Utils.Exts;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

public static class UiExtension {
    public static bool IsVisibleInWindow(this FrameworkElement element, Window mainWindow) {
        if (!element.IsVisible) return false;

        try {
            var transform = element.TransformToAncestor(mainWindow);
            var bounds = transform.TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
            var windowRect = new Rect(0, 0, mainWindow.ActualWidth, mainWindow.ActualHeight);
            return windowRect.IntersectsWith(bounds);
        } catch (InvalidOperationException) {
            return false;
        }
    }

    public static bool IsTextTrimmed(this TextBlock textBlock) {
        if (textBlock.TextTrimming == TextTrimming.None) return false;

        try {
            var formattedText = new FormattedText(
                textBlock.Text,
                System.Globalization.CultureInfo.CurrentCulture,
                textBlock.FlowDirection,
                new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
                textBlock.FontSize,
                textBlock.Foreground,
                VisualTreeHelper.GetDpi(textBlock).PixelsPerDip
                );

            return formattedText.Width > textBlock.ActualWidth;
        } catch (Exception) {
            return false;
        }
    }
}
#endif

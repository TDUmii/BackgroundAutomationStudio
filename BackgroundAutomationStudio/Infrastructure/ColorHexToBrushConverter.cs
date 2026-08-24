using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using BackgroundAutomationStudio.Models;

namespace BackgroundAutomationStudio.Infrastructure;

public sealed class ColorHexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ColorScanAction.TryParseColor(value?.ToString(), out var red, out var green, out var blue)
            ? new SolidColorBrush(Color.FromRgb(red, green, blue))
            : Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

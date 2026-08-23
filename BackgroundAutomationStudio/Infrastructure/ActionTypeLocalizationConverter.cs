using System.Globalization;
using System.Windows.Data;
using BackgroundAutomationStudio.Services;

namespace BackgroundAutomationStudio.Infrastructure;

public sealed class ActionTypeLocalizationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value?.ToString() switch
    {
        "Click" => LocalizationService.Get("Click"), "Right click" => LocalizationService.Get("RightClick"), "Double click" => LocalizationService.Get("DoubleClick"),
        "Type text" => LocalizationService.Get("TypeText"), "Key press" => LocalizationService.Get("KeyPress"), "Wait" => LocalizationService.Get("Wait"), _ => value?.ToString() ?? string.Empty
    };
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

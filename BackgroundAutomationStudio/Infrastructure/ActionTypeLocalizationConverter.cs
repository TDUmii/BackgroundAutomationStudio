using System.Globalization;
using System.Windows.Data;
using BackgroundAutomationStudio.Services;

namespace BackgroundAutomationStudio.Infrastructure;

public sealed class ActionTypeLocalizationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value?.ToString() switch
    {
        "Click" => LocalizationService.Get("Click"), "Right Click" => LocalizationService.Get("RightClick"), "Double Click" => LocalizationService.Get("DoubleClick"), "Drag" => LocalizationService.Get("Drag"), "Scroll" => LocalizationService.Get("Scroll"), "Move Pointer" => LocalizationService.Get("MovePointer"),
        "Call Function" => LocalizationService.Get("CallFunction"), "Type Text" => LocalizationService.Get("TypeText"), "Key Press" => LocalizationService.Get("KeyPress"), "Hold Key" => LocalizationService.Get("KeyHold"), "Wait" => LocalizationService.Get("Wait"), "Wait for Image" => LocalizationService.Get("WaitForImage"), "Click Image" => LocalizationService.Get("ClickImage"), "Wait for Color" => LocalizationService.Get("WaitForColor"), "Click Color" => LocalizationService.Get("ClickColor"), _ => value?.ToString() ?? string.Empty
    };
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

using System.Globalization;
using System.Windows.Data;

namespace BackgroundAutomationStudio.Infrastructure;

public sealed class OneBasedIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is int index ? index + 1 : value;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BackgroundAutomationStudio.Infrastructure;

public sealed class EmptyCollectionToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value switch
        {
            int count => count == 0 ? Visibility.Visible : Visibility.Collapsed,
            IEnumerable items => !items.Cast<object>().Any() ? Visibility.Visible : Visibility.Collapsed,
            _ => Visibility.Collapsed
        };
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

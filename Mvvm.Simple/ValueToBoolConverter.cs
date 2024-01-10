using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Mvvm.Simple
{
    internal class ValueToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (ReferenceEquals(value, parameter)) return true;
            if (value == null) return parameter == null;
            if ($"{value}" == $"{parameter}" && $"{value}" != value.GetType().FullName) return true;

            return (value.GetType() == parameter.GetType()) ? value.Equals(parameter) :
                value.Equals(System.Convert.ChangeType(parameter, value.GetType()));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || parameter == null) return Binding.DoNothing;

            if (!(bool)value) return Binding.DoNothing;
            var result = (parameter.GetType() == targetType)
                ? parameter
                : System.Convert.ChangeType(parameter, targetType);
            return result;
        }
    }
}

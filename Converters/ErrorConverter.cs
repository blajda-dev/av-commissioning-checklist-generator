using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace CommissioningChecklistGenerator.Converters
{
    public class ErrorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value is the Validation.Errors collection
            if (value is ReadOnlyObservableCollection<ValidationError> errors && errors.Count > 0)
            {
                // We return the actual string content of the first error
                return errors[0].ErrorContent;
            }

            // Return null or string.Empty when there's no error. 
            // ToolTip/TextBlock will handle this gracefully without logging Error 17.
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}

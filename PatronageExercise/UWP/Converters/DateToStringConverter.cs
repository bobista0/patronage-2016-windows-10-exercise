using System;
using Windows.UI.Xaml.Data;

namespace UWP.Converters
{
    public class DateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string convertedDate = ((DateTime)value).ToString();
            return convertedDate;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

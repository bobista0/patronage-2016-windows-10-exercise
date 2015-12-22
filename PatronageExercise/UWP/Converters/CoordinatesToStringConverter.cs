using System;
using Windows.UI.Xaml.Data;

namespace UWP.Converters
{
    public class CoordinatesToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string convertedCoordinate = string.Empty;

            if(value != null)
            {
                var coordinate = (double[])value;
                convertedCoordinate = coordinate[0].ToString() + "," + coordinate[1].ToString() + coordinate[2].ToString();
            }
            else
            {
                convertedCoordinate = "-";
            }

            return convertedCoordinate;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using Windows.UI.Xaml.Data;

namespace UWP.Converters
{
    public class SizeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var converterSize = string.Empty;

            if (value != null)
            {
                var unit = string.Empty;
                var size = value as ulong?;

                if (size != null)
                {
                    size = size / 1024;
                    if (size < 1024)
                    {
                        unit = "KB";
                    }
                    else if (size >= 1024)
                    {
                        size = size / 1024;
                        unit = "MB";
                    }
                    converterSize = string.Format("{0:F2}{1}", size, unit);
                }
            }
            else
            {
                converterSize = "-";
            }

            return converterSize;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using Windows.UI.Xaml.Media.Imaging;

namespace UWP.Models
{
    public class DetailPhoto
    {
        public BitmapImage Source { get; set; }
        public ulong Size { get; set; }
        public DateTime Date { get; set; }
        public double[] Latitude { get; set; }
        public double[] Longitude { get; set; }
    }
}

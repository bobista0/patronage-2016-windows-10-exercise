using System;
using Windows.UI.Xaml.Media.Imaging;

namespace UWP.Models
{
    public class Photo
    {
        public string Name { get; set; }
        public BitmapImage Thumbnail { get; set; }
        public BitmapImage Source { get; set; }
        public ulong Size { get; set; }
        public DateTime Date { get; set; }
        public double[] Latitude { get; set; }
        public double[] Longitude { get; set; }
    }
}

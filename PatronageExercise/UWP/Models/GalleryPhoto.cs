using Windows.UI.Xaml.Media.Imaging;

namespace UWP.Models
{
    public class GalleryPhoto
    {
        public uint Index { get; set; }
        public string Name { get; set; }
        public BitmapImage Thumbnail { get; set; }
    }
}

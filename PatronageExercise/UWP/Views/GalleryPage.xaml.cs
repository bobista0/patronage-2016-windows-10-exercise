using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using UWP.Models;
using UWP.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace UWP.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GalleryPage : Page, INotifyPropertyChanged
    {
        #region FIELDS
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region CONSTRUCTORS
        public GalleryPage()
        {
            InitializeComponent();

            CheckCameraAvailability();
            Loaded += GalleryPage_Loaded;
        }

        private async void GalleryPage_Loaded(object sender, RoutedEventArgs e)
        {
            await DisplayGallery();
        }
        #endregion

        #region PROPERTIES
        private List<GalleryPhoto> photoCollection;
        public List<GalleryPhoto> PhotoCollection
        {
            get { return photoCollection; }
            set
            {
                if (value != photoCollection)
                {
                    photoCollection = value;
                    OnPropertyChanged("PhotoCollection");
                }
            }
        }

        private bool _isCameraDeviceAvailable = false;
        public bool IsCameraDeviceAvailable
        {
            get { return _isCameraDeviceAvailable; }
            set
            {
                if (value != _isCameraDeviceAvailable)
                {
                    _isCameraDeviceAvailable = value;
                    OnPropertyChanged("IsCameraDeviceAvailable");
                }
            }
        }
        #endregion

        #region METHODS
        private async Task DisplayGallery()
        {
            PhotoCollection = await PhotoCameraService.Instance.LoadAndGetGallery();
        }

        private async void CheckCameraAvailability()
        {
            IsCameraDeviceAvailable = await PhotoCameraService.Instance.IsCameraAvailable();
        }

        private async void OnCapturePhotoAppBarButtonClick(object sender, RoutedEventArgs e)
        {
            await PhotoCameraService.Instance.CaptureAndSavePhoto();
        }

        private async void OnRefreshAppBarButtonClick(object sender, RoutedEventArgs e)
        {
            CheckCameraAvailability();
            await DisplayGallery();
        }

        private void OnPropertyChanged(string name)
        {
            if (name == null || name == string.Empty) { return; }

            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private void OnGridViewItemClick(object sender, ItemClickEventArgs e)
        {
            if (e == null) { return; }

            var clickedPhotoIndex = (e.ClickedItem as GalleryPhoto).Index;
            PhotoCameraService.Instance.SetFileIndexToClickedItem(clickedPhotoIndex);

            if (Frame != null)
            {
                Frame.Navigate(typeof(DetailPage));
            }
        }
        #endregion
    }
}

using System;
using System.ComponentModel;
using UWP.Models;
using UWP.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace UWP.Views
{
    public sealed partial class DetailPage : Page, INotifyPropertyChanged
    {
        #region FIELDS
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region CONSTRUCTORS
        public DetailPage()
        {
            InitializeComponent();

            DisplayPhoto();
            CheckCameraAvailability();
        }
        #endregion

        #region PROPERTIES
        private BitmapImage _loadedPhoto;
        public BitmapImage LoadedPhoto
        {
            get { return _loadedPhoto; }
            set
            {
                if (value != _loadedPhoto)
                {
                    _loadedPhoto = value;
                    OnPropertyChanged("LoadedPhoto");
                }
            }
        }

        private ulong _size;
        public ulong Size
        {
            get { return _size; }
            set
            {
                if (value != _size)
                {
                    _size = value;
                    OnPropertyChanged("Size");
                }
            }
        }

        private DateTime _date;
        public DateTime Date
        {
            get { return _date; }
            set
            {
                if (value != _date)
                {
                    _date = value;
                    OnPropertyChanged("Date");
                }
            }
        }

        private double[] _latitude;
        public double[] Latitude
        {
            get { return _latitude; }
            set
            {
                if (value != _latitude)
                {
                    _latitude = value;
                    OnPropertyChanged("Latitude");

                }
            }
        }

        private double[] _longitude;
        public double[] Longitude
        {
            get { return _longitude; }
            set
            {
                if (value != _longitude)
                {
                    _longitude = value;
                    OnPropertyChanged("Longitude");
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

        private async void DisplayPhoto()
        {
            var photo = await PhotoCameraService.Instance.LoadAndGetPhoto();

            if (photo != null)
                SetPhoto(photo);
        }

        private void SetPhoto(Photo photo)
        {
            LoadedPhoto = photo.Source;
            Size = photo.Size;
            Date = photo.Date;
            Latitude = photo.Latitude;
            Longitude = photo.Longitude;
        }

        private async void CheckCameraAvailability()
        {
            IsCameraDeviceAvailable = await PhotoCameraService.Instance.IsCameraAvailable();
        }

        private void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private void OnDisplayedPhotoTapped(object sender, TappedRoutedEventArgs e)
        {
            DisplayPhoto();
        }

        private void OnCapturePhotoAppBarButtonClick(object sender, RoutedEventArgs e)
        {
            PhotoCameraService.Instance.CaptureAndSavePhoto();
        }

        private void OnRefreshAppBarButtonClick(object sender, RoutedEventArgs e)
        {
            CheckCameraAvailability();
        }

        private void OnGalleryAppBarButtonClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(GalleryPage));
        }
        #endregion
    }
}

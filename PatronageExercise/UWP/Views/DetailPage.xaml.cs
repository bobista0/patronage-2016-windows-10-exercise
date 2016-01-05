using System;
using System.Collections.Generic;
using System.ComponentModel;
using UWP.Models;
using UWP.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
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
        #endregion

        #region METHODS
        private async void DisplayPhoto()
        {
            var photo = await PhotoCameraService.Instance.LoadAndGetPhoto();

            if (photo != null)
                SetPhoto(photo);
        }

        private void SetPhoto(DetailPhoto photo)
        {
            LoadedPhoto = photo.Source;
            Size = photo.Size;
            Date = photo.Date;
            Latitude = photo.Latitude;
            Longitude = photo.Longitude;
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

        private void OnGalleryAppBarButtonClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(GalleryPage));
        }

        private void OnShareAppBarButtonClick(object sender, RoutedEventArgs e)
        {
            var dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;
            DataTransferManager.ShowShareUI();
        }

        private async void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var deferral = args.Request.GetDeferral();
            args.Request.Data.SetText("Hello World!");
            args.Request.Data.Properties.Title = "Share Example";
            args.Request.Data.Properties.Description = "trololol olol ol o lolo";

            try
            {
                var thumbnail = await PhotoCameraService.Instance.GetThumbnailOfCurrentPhoto();
                args.Request.Data.Properties.Thumbnail = thumbnail;
                var photoFile = PhotoCameraService.Instance.GetCurrentPhotoFile();
                List<StorageFile> files = new List<StorageFile>();
                files.Add(photoFile);
                args.Request.Data.SetStorageItems(files);
                var bitmap = PhotoCameraService.Instance.GetCurrentPhoto();
                args.Request.Data.SetBitmap(bitmap);
                args.Request.Data.SetData("id1", photoFile);

            }
            finally
            {
                deferral.Complete();
            }
        }
        #endregion
    }
}

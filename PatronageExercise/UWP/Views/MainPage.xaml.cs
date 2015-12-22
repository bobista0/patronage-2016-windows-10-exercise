using System;
using System.ComponentModel;
using UWP.Models;
using UWP.Services;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace UWP
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        #region FIELDS
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region CONSTRUCTORS
        public MainPage()
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
        private void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private async void DisplayPhoto()
        {
            await PhotoDisplayService.Instance.LoadPhoto();
            var photo = PhotoDisplayService.Instance.GetPhoto();

            LoadedPhoto = photo.Source;
            DisplayPhotoInfo(photo);
        }

        private void DisplayPhotoInfo(Photo photo)
        {
            Size = photo.Size;
            Date = photo.Date;
            Latitude = photo.Latitude;
            Longitude = photo.Longitude;
        }

        private void OnDisplayedPhotoTapped(object sender, TappedRoutedEventArgs e)
        {
            DisplayPhoto();
        }
        #endregion
    }
}

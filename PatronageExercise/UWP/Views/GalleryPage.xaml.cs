using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UWP.Models;
using UWP.Services;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace UWP.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GalleryPage : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public GalleryPage()
        {
            this.InitializeComponent();

            DisplayGallery();
        }

        private List<Photo> photoCollection;
        public List<Photo> PhotoCollection
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

        private async void DisplayGallery()
        {
            var gallery = await PhotoCameraService.Instance.LoadAndGetGallery();

            if (gallery != null)
                PhotoCollection = gallery;
        }

        private void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private void OnGridViewItemClick(object sender, ItemClickEventArgs e)
        {

        }
    }
}

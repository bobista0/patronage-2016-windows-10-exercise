using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace UWP
{
    public sealed partial class MainPage : Page
    {
        #region PRIVATE FIELDS
        private static IReadOnlyList<StorageFile> _files;
        #endregion

        #region CONSTRUCTORS
        public MainPage()
        {
            InitializeComponent();

            LoadPhoto();
        }
        #endregion

        #region PROPERTIES
        #endregion

        #region METHODS
        public static async void GetFiles()
        {
            var folderPath = KnownFolders.PicturesLibrary;
            _files = await folderPath.GetFilesAsync(CommonFileQuery.DefaultQuery, 0, 10);
        }

        private async void LoadPhoto()
        {
            if (_files == null)
                return;

            if (_files.Count == 0)
            {
                ShowErrorMessage("The picture library is empty!");
                return;
            }

            for (int i = 0; i < _files.Count; i++)
            {
                var file = _files[i];
                if (HasPhotoExtension(file))
                {
                    using (var fileStream = await file.OpenReadAsync())
                    {
                        var photo = new BitmapImage();
                        photo.SetSource(fileStream);
                        DisplayedPhoto.Source = photo;
                    }

                    break;
                }
            }
        }

        private bool HasPhotoExtension(IStorageItem item)
        {
            return Path.GetExtension(item.Name).Equals(".jpg", StringComparison.CurrentCultureIgnoreCase)
                || Path.GetExtension(item.Name).Equals(".jpeg", StringComparison.CurrentCultureIgnoreCase)
                || Path.GetExtension(item.Name).Equals(".png", StringComparison.CurrentCultureIgnoreCase)
                || Path.GetExtension(item.Name).Equals(".bmp", StringComparison.CurrentCultureIgnoreCase);
        }

        private void ShowErrorMessage(string message)
        {
            var textBlock = new TextBlock()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Colors.White),
                Text = message
            };

            var border = new Border()
            {
                Width = 300,
                Height = 300,
                Background = new SolidColorBrush(Colors.Red),
                Child = textBlock
            };

            LayoutRoot.Children.Add(border);
        }
        #endregion
    }
}

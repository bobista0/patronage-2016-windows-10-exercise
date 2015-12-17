using System;
using System.Collections.Generic;
using System.IO;
using UWP.Services;
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
            try
            {
                var folderPath = KnownFolders.PicturesLibrary;
                _files = await folderPath.GetFilesAsync(CommonFileQuery.DefaultQuery, 0, 10);
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowMessageService.Instance.ShowMessage(ex.Message);
            }
            catch (Exception ex)
            {
                ShowMessageService.Instance.ShowMessage(ex.Message);
            }
        }

        private async void LoadPhoto()
        {
            if (_files == null)
                return;

            if (_files.Count == 0)
            {
                ShowMessageService.Instance.ShowMessage("The picture library is empty!");

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

                        return;
                    }
                }
            }

            ShowMessageService.Instance.ShowMessage("There is no photo file to display!");
        }

        private bool HasPhotoExtension(IStorageItem item)
        {
            return HasSpecificExtension(item, ".jpg", ".jpeg", ".png", ".tif", ".bmp");
        }

        private bool HasSpecificExtension(IStorageItem item, params string[] extensions)
        {
            var result = false;

            foreach (var extension in extensions)
            {
                if (result = Path.GetExtension(item.Name).Equals(extension, StringComparison.CurrentCultureIgnoreCase))
                    break;
            }

            return result;
        }
        #endregion
    }
}

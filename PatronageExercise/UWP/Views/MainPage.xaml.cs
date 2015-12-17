using System;
using System.Collections.Generic;
using System.IO;
using UWP.Services;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace UWP
{
    public sealed partial class MainPage : Page
    {
        #region PRIVATE FIELDS
        private StorageFile _currentPhoto;
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
            if (_files == null || _files.Count == 0)
            {
                ShowMessageService.Instance.ShowMessage("The picture library is empty!");
            }
            else
            {
                foreach (var file in _files)
                {
                    if (ExtensionCheckService.Instance.HasPhotoExtension(file))
                    {
                        if (_currentPhoto == file)
                            continue;

                        try
                        {
                            using (var fileStream = await file.OpenReadAsync())
                            {
                                var photo = new BitmapImage();
                                photo.SetSource(fileStream);
                                DisplayedPhoto.Source = photo;
                                _currentPhoto = file;
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            ShowMessageService.Instance.ShowMessage(ex.Message);
                        }
                    }
                }

                ShowMessageService.Instance.ShowMessage("There is no photo file to display!");
            }

            return;
        }

        private void OnDisplayedPhotoTapped(object sender, TappedRoutedEventArgs e)
        {
            LoadPhoto();
        }
        #endregion
    }
}

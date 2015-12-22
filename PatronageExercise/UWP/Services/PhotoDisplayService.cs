using ExifLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UWP.Models;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace UWP.Services
{
    public sealed class PhotoDisplayService
    {
        #region LAZY-SINGLETON IMPLEMENTATION
        private static readonly Lazy<PhotoDisplayService> lazy = new Lazy<PhotoDisplayService>(() => new PhotoDisplayService());
        public static PhotoDisplayService Instance
        {
            get { return lazy.Value; }
        }
        private PhotoDisplayService() { }
        #endregion

        #region FIELDS
        private IReadOnlyList<StorageFile> _files;
        private BitmapImage _photo;
        private ulong _size;
        private DateTime _date;
        private double[] _latitude;
        private double[] _longitude;
        private int _fileIndex;
        #endregion

        #region METHODS
        private async Task GetPhotoFromFile(StorageFile file)
        {
            try
            {
                _photo = new BitmapImage();

                using (var fileStream = await file.OpenAsync(FileAccessMode.Read))
                {
                    GetMetaDataInfoFromPhoto(file, fileStream);

                    if (ExtensionCheckService.Instance.HasSpecificExtension(file, ".jpg"))
                    {
                        var clonedStream = fileStream.CloneStream();
                        using (var reader = new ExifReader(clonedStream.AsStreamForRead()))
                        {
                            try
                            {
                                GetExifInfoFromPhoto(reader);
                            }
                            catch { }
                        }
                        clonedStream.Dispose();
                    }
                    else
                    {
                        _latitude = null;
                        _longitude = null;
                    }

                    await _photo.SetSourceAsync(fileStream);
                }

            }
            catch (Exception ex)
            {
                ShowMessageService.Instance.ShowMessageWithApplicationExit(ex.Message);
            }
        }

        private void GetMetaDataInfoFromPhoto(StorageFile file, IRandomAccessStream fileStream)
        {
            ulong size;
            size = fileStream.Size;
            _size = size;

            DateTime date;
            date = file.DateCreated.DateTime;
            _date = date;
        }

        private void GetExifInfoFromPhoto(ExifReader reader)
        {
            double[] latitude;
            reader.GetTagValue(ExifTags.GPSLatitude, out latitude);
            _latitude = (double[])latitude.Clone();

            double[] longitude;
            reader.GetTagValue(ExifTags.GPSLongitude, out longitude);
            _longitude = (double[])longitude.Clone();
        }

        private async Task CheckIfFilesExist()
        {
            if (_files == null)
                await GetFiles();
            if (_files.Count == 0)
                ShowMessageService.Instance.ShowMessageWithApplicationExit("The picture library is empty!");
        }

        private void SetPhotoInfo(Photo photo)
        {
            photo.Source = _photo;
            photo.Size = _size;
            photo.Date = _date;
            photo.Latitude = _latitude;
            photo.Longitude = _longitude;
        }

        public async Task LoadPhoto()
        {
            await CheckIfFilesExist();

            if (_fileIndex < _files.Count)
            {
                for (int i = _fileIndex; i < _files.Count; i++)
                {
                    _fileIndex = (_fileIndex + 1) % _files.Count;
                    var file = _files[i];
                    if (ExtensionCheckService.Instance.HasPhotoExtension(file))
                    {
                        await GetPhotoFromFile(file);
                        break;
                    }
                }
            }
        }

        public Photo GetPhoto()
        {
            var photo = new Photo();
            SetPhotoInfo(photo);
            return photo;
        }

        public async Task GetFiles()
        {
            try
            {
                var folderPath = KnownFolders.PicturesLibrary;
                _files = await folderPath.GetFilesAsync(CommonFileQuery.DefaultQuery, 0, 10);
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowMessageService.Instance.ShowMessageWithApplicationExit(ex.Message);
            }
            catch (Exception ex)
            {
                ShowMessageService.Instance.ShowMessageWithApplicationExit(ex.Message);
            }
        }
        #endregion
    }
}

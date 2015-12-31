using ExifLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UWP.Interfaces;
using UWP.Models;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.UI.Xaml.Media.Imaging;

//TODO: sprawdzić czyjest wszedzie 'var'.
//TODO: przerobić wcięcia, ewentualnie oddzielić kod
//TODO: zrefaktoryzować powtórzenia
//TODO: zrobić porządek w zmiennych zwracanych -> albo 'result' albo 'nazwa_zwracanego_obiektu'
namespace UWP.Services
{
    public sealed class PhotoCameraService : IPhotoCameraService
    {
        #region LAZY-SINGLETON IMPLEMENTATION
        private static readonly Lazy<PhotoCameraService> lazy = new Lazy<PhotoCameraService>(() => new PhotoCameraService());
        public static PhotoCameraService Instance { get { return lazy.Value; } }
        private PhotoCameraService() { _deviceFamilyInfo = AnalyticsInfo.VersionInfo.DeviceFamily; }
        #endregion

        #region PUBLIC FIELDS
        #endregion

        #region PRIVATE FIELDS
        private IReadOnlyList<StorageFile> _files;
        private BitmapImage _photo;
        private ulong _size;
        private DateTime _date;
        private double[] _latitude;
        private double[] _longitude;
        private int _fileIndex;
        private readonly string _deviceFamilyInfo;
        #endregion

        #region PUBLIC METHODS
        public async void CaptureAndSavePhoto()
        {
            var capturedPhoto = await CapturePhoto();
            if (capturedPhoto != null)
                await SavePhoto(capturedPhoto);
        }
        public async Task<bool> IsCameraAvailable()
        {
            return await IsCameraDeviceExist();
        }
        public async Task<Photo> LoadAndGetPhoto()
        {
            Photo result = null;

            await LoadPhoto();
            if (_photo != null)
            {
                result = new Photo();
                SetPhotoInfo(result);
            }

            return result;
        }
        public async void GetFiles()
        {
            try
            {
                var folderPath = KnownFolders.PicturesLibrary;
                _files = await folderPath.GetFilesAsync(CommonFileQuery.DefaultQuery, 0, 10);
            }
            catch (Exception ex)
            {
                ShowMessageService.Instance.ShowMessageWithApplicationExit(ex.Message);
            }
        }
        public string GetDeviceFamilyInfo()
        {
            return _deviceFamilyInfo;
        }
        #endregion

        #region PRIVATE METHODS
        private async Task<bool> IsCameraDeviceExist()
        {
            var result = false;

            try
            {
                var cameraDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                if (cameraDevices.Count > 0)
                {
                    foreach (var cameraDevice in cameraDevices)
                    {
                        if (cameraDevice.IsEnabled)
                        {
                            result = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessageService.Instance.ShowMessage(ex.ToString());
            }

            return result;
        }
        private async Task LoadPhoto()
        {
            CheckIfFilesExist();

            if (_fileIndex < _files.Count)
            {
                for (int i = _fileIndex; i < _files.Count; i++)
                {
                    _fileIndex = (_fileIndex + 1) % _files.Count;
                    var file = _files[i];
                    if (ExtensionCheckService.Instance.HasGraphicExtension(file))
                    {
                        await GetPhotoFromFile(file);
                        break;
                    }
                }
            }
        }
        private void CheckIfFilesExist()
        {
            if (_files == null)
                GetFiles();

            if (_files.Count == 0)
                ShowMessageService.Instance.ShowMessageWithApplicationExit("The picture library is empty!");
        }
        private async Task GetPhotoFromFile(StorageFile file)
        {
            try
            {
                _photo = new BitmapImage();

                using (var fileStream = await file.OpenAsync(FileAccessMode.Read))
                {
                    GetMetaDataInfoFromPhoto(file, fileStream);

                    if (ExtensionCheckService.Instance.HasPhotoExtension(file))
                    {
                        var clonedStream = fileStream.CloneStream();
                        using (var reader = new ExifReader(clonedStream.AsStreamForRead()))
                        {
                            try
                            {
                                GetExifInfoFromPhoto(reader);
                            }
                            catch (Exception ex)
                            {
                                ShowMessageService.Instance.ShowMessage(ex.Message);
                            }
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
            _latitude = (latitude != null) ? (double[])latitude.Clone() : null;

            double[] longitude;
            reader.GetTagValue(ExifTags.GPSLongitude, out longitude);
            _longitude = (longitude != null) ? (double[])longitude.Clone() : null;
        }
        private void SetPhotoInfo(Photo photo)
        {
            photo.Source = _photo;
            photo.Size = _size;
            photo.Date = _date;
            photo.Latitude = _latitude;
            photo.Longitude = _longitude;
        }
        private async Task<StorageFile> CapturePhoto()
        {
            StorageFile photo = null;
            try
            {
                var captureUI = new CameraCaptureUI();
                captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
                captureUI.PhotoSettings.MaxResolution = CameraCaptureUIMaxPhotoResolution.Large3M;
                captureUI.PhotoSettings.AllowCropping = true;
                captureUI.PhotoSettings.CroppedSizeInPixels = new Size();
                photo = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);
                await photo.RenameAsync(GetDefaultPhotoName());
            }
            catch (Exception ex)
            {
                ShowMessageService.Instance.ShowMessage(ex.ToString());
            }
            return photo;
        }
        private async Task SavePhoto(StorageFile capturedPhoto)
        {
            var deviceFamily = GetDeviceFamilyInfo();
            switch (deviceFamily)
            {
                case "Windows.Desktop":
                    await SavePhotoForWindowsDesktop(capturedPhoto);
                    break;
                case "Windows.Mobile":
                    await SavePhotoForWindowsMobile(capturedPhoto);
                    break;
                default:
                    ShowMessageService.Instance.ShowMessage("Oops! Is this potato?");
                    break;
            }
        }
        private async Task SavePhotoForWindowsDesktop(StorageFile capturedPhoto)
        {
            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeChoices.Add("JPEG (*.jpg;*.jpeg;*.jpe;*.jfif)", new List<string> { ".jpeg", ".jpg" });
            picker.SuggestedSaveFile = capturedPhoto;
            var file = await picker.PickSaveFileAsync();
            var storageFile = file;
            if (storageFile != null)
            {
                try
                {
                    await capturedPhoto.MoveAndReplaceAsync(file);
                }
                catch (Exception ex)
                {
                    ShowMessageService.Instance.ShowMessage(ex.ToString());
                }
            }
        }
        private async Task SavePhotoForWindowsMobile(StorageFile capturedPhoto)
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            folderPicker.FileTypeFilter.Add(".jpg");
            var folder = await folderPicker.PickSingleFolderAsync();
            var storageFolder = folder;
            if (storageFolder != null)
            {
                try
                {
                    await capturedPhoto.MoveAsync(storageFolder);
                }
                catch (Exception ex)
                {
                    ShowMessageService.Instance.ShowMessage(ex.ToString());
                }
            }
        }
        private string GetDefaultPhotoName()
        {
            var result = new StringBuilder();

            var year = DateTime.Now.Year.ToString();
            var month = DateTime.Now.Month.ToString();
            var day = DateTime.Now.Day.ToString();
            var hour = DateTime.Now.Hour.ToString();
            var minute = DateTime.Now.Minute.ToString();
            var second = DateTime.Now.Second.ToString();

            result.Append("UWP")
                  .Append($"_{year}{month}{day}")
                  .Append($"_{hour}_{minute}_{second}")
                  .Append(".jpg");

            return result.ToString();
        }
        #endregion
    }
}

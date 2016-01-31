using ExifLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UWP.Interfaces;
using UWP.Models;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.UI.Xaml.Media.Imaging;

//TODO: sprawdzić czy jest wszedzie lokalnie 'var'.
//TODO: przerobić wcięcia, ewentualnie oddzielić kod
//TODO: zrefaktoryzować powtórzenia
//TODO: zrobić porządek w zmiennych zwracanych ->  'result' w public, 'nazwa_zwracanego_obiektu' w prywatnych
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
        private readonly string _deviceFamilyInfo;
        private IReadOnlyList<StorageFile> _photoFiles;
        private BitmapImage _photo;
        private ulong _size;
        private DateTime _date;
        private double[] _latitude;
        private double[] _longitude;
        private int _fileIndex;
        #endregion

        #region PUBLIC METHODS
        public async Task CaptureAndSavePhoto()
        {
            var capturedPhoto = await CapturePhoto();
            if (capturedPhoto != null)
            {
                await SavePhoto(capturedPhoto);
            }
        }
        public async Task<bool> IsCameraAvailable()
        {
            return await IsCameraDeviceExist();
        }
        public async Task<DetailPhoto> LoadAndGetPhoto()
        {
            DetailPhoto result = null;

            await LoadPhoto();

            result = new DetailPhoto();
            if (result != null)
            {
                SetPhotoInfo(result);
            }

            return result;
        }
        public async Task<List<GalleryPhoto>> LoadAndGetGallery()
        {
            List<GalleryPhoto> result;

            result = await LoadGallery();

            return result;
        }
        public void LoadFiles()
        {
            try
            {
                var folderPath = KnownFolders.PicturesLibrary;
                List<StorageFile> photoFiles = new List<StorageFile>();
                if (folderPath != null || photoFiles != null)
                {
                    LoadPhotoFilesFromPicturesLibraryFolders(photoFiles, folderPath);
                    _photoFiles = photoFiles;
                }
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
        public void SetFileIndexToClickedItem(uint clickedPhotoIndex)
        {
            _fileIndex = Convert.ToInt32(clickedPhotoIndex);
        }
        public RandomAccessStreamReference GetCurrentPhoto()
        {
            RandomAccessStreamReference currentPhotoStreamReference = null;

            var currentPhotoIndex = _fileIndex - 1;

            if (_photoFiles != null && _photoFiles.Count > 0)
            {
                var currentPhotoFile = _photoFiles[currentPhotoIndex];

                if (currentPhotoFile != null)
                {
                    currentPhotoStreamReference = RandomAccessStreamReference.CreateFromFile(currentPhotoFile);
                }
            }

            return currentPhotoStreamReference;
        }
        public StorageFile GetCurrentPhotoFile()
        {
            StorageFile currentPhotoFile = null;

            var currentPhotoIndex = _fileIndex - 1;

            if (_photoFiles != null && _photoFiles.Count > 0)
            {
                currentPhotoFile = _photoFiles[currentPhotoIndex];
            }

            return currentPhotoFile;
        }
        public async Task<RandomAccessStreamReference> GetThumbnailOfCurrentPhoto()
        {
            RandomAccessStreamReference currentPhotoThumbnailStreamReference = null;

            var currentPhotoIndex = _fileIndex - 1;

            if (_photoFiles != null && _photoFiles.Count > 0)
            {
                var currentPhotoFile = _photoFiles[currentPhotoIndex];

                if (currentPhotoFile != null)
                {
                    StorageItemThumbnail thumbnail = null;
                    thumbnail = await currentPhotoFile.GetThumbnailAsync(ThumbnailMode.PicturesView);

                    if (thumbnail != null)
                    {
                        currentPhotoThumbnailStreamReference = RandomAccessStreamReference.CreateFromStream(thumbnail);
                    }
                }
            }

            return currentPhotoThumbnailStreamReference;
        }
        #endregion

        #region PRIVATE METHODS
        private async Task<bool> IsCameraDeviceExist()
        {
            var result = false;

            try
            {
                var cameraDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                if (cameraDevices != null || cameraDevices.Count != 0)
                {
                    var device = cameraDevices.FirstOrDefault(x => x.IsEnabled == true);
                    result = (device == null) ? false : true;
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

            if (_photoFiles != null)
            {
                if (_fileIndex < _photoFiles.Count)
                {
                    var file = _photoFiles[_fileIndex];

                    if (file != null)
                    {
                        await OpenPhotoFileStreamAndLoadSources(file);
                    }

                    _fileIndex = (_fileIndex + 1) % _photoFiles.Count;
                }
            }
        }
        private void CheckIfFilesExist()
        {
            if (_photoFiles == null)
            {
                LoadFiles();
            }
            else if (_photoFiles.Count == 0)
            {
                ShowMessageService.Instance.ShowMessage("The picture library is empty!");
            }
        }
        private async Task OpenPhotoFileStreamAndLoadSources(StorageFile file)
        {
            if (file == null) { return; }

            try
            {
                using (var fileStream = await file.OpenAsync(FileAccessMode.Read))
                {
                    if (fileStream != null)
                    {
                        LoadMetaDataInfoFromPhotoAndSetToServiceFields(file, fileStream);
                        await SetPhotoSourceFromFileStream(fileStream);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessageService.Instance.ShowMessage(ex.Message);
            }
        }
        private void LoadMetaDataInfoFromPhotoAndSetToServiceFields(StorageFile file, IRandomAccessStream fileStream)
        {
            if (file == null || fileStream == null) { return; }

            _size = GetSizeInfoFromPhoto(fileStream);
            _date = GetDataCreatedInfoFromPhoto(file);
            _latitude = null;
            _longitude = null;

            if (ExtensionCheckService.Instance.HasPhotoExtension(file))
            {
                var clonedStream = fileStream.CloneStream();
                if (clonedStream != null)
                {
                    using (var reader = new ExifReader(clonedStream.AsStreamForRead()))
                    {
                        try
                        {
                            if (reader != null)
                            {
                                SetGpsLatitudeAndLongitudeInfoFromPhoto(reader);
                            }
                        }
                        catch (Exception ex)
                        {
                            ShowMessageService.Instance.ShowMessage(ex.Message);
                        }
                    }
                    clonedStream.Dispose();
                }
            }
        }
        private async Task SetPhotoSourceFromFileStream(IRandomAccessStream fileStream)
        {
            if (fileStream == null) { return; }

            _photo = new BitmapImage();
            if (_photo != null)
            {
                await _photo.SetSourceAsync(fileStream);
            }
        }
        private ulong GetSizeInfoFromPhoto(IRandomAccessStream fileStream)
        {
            ulong result;
            _size = result = ulong.MinValue;

            if (fileStream != null)
            {
                result = fileStream.Size;
            }

            return result;
        }
        private DateTime GetDataCreatedInfoFromPhoto(StorageFile file)
        {
            DateTime result;
            _date = result = DateTime.MinValue;

            if (file != null)
            {
                result = file.DateCreated.DateTime;
            }

            return result;
        }
        private void SetGpsLatitudeAndLongitudeInfoFromPhoto(ExifReader reader)
        {
            if (reader == null) { return; }

            double[] latitude;
            reader.GetTagValue(ExifTags.GPSLatitude, out latitude);
            _latitude = (latitude != null) ? (double[])latitude.Clone() : null;

            double[] longitude;
            reader.GetTagValue(ExifTags.GPSLongitude, out longitude);
            _longitude = (longitude != null) ? (double[])longitude.Clone() : null;
        }
        private void SetPhotoInfo(DetailPhoto photo)
        {
            if (photo == null) { return; }

            if (_photo != null)
            {
                photo.Source = _photo;
                photo.Size = _size;
                photo.Date = _date;
                photo.Latitude = _latitude;
                photo.Longitude = _longitude;
            }
        }
        private async Task<StorageFile> CapturePhoto()
        {
            StorageFile photo = null;
            try
            {
                var captureUI = new CameraCaptureUI();
                if (captureUI != null)
                {
                    captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
                    captureUI.PhotoSettings.MaxResolution = CameraCaptureUIMaxPhotoResolution.Large3M;
                    captureUI.PhotoSettings.AllowCropping = true;
                    captureUI.PhotoSettings.CroppedSizeInPixels = new Size();
                    photo = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);
                }

                if (photo != null)
                {
                    await photo.RenameAsync(GetDefaultPhotoName());
                }
            }
            catch (Exception ex)
            {
                ShowMessageService.Instance.ShowMessage(ex.ToString());
            }

            return photo;
        }
        private async Task SavePhoto(StorageFile capturedPhoto)
        {
            if (capturedPhoto == null) { return; }

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

            var newStorageFiles = new List<StorageFile>(_photoFiles);
            if (newStorageFiles != null)
            {
                newStorageFiles.Add(capturedPhoto);
                _photoFiles = newStorageFiles;
            }
        }
        private async Task SavePhotoForWindowsDesktop(StorageFile capturedPhoto)
        {
            if (capturedPhoto == null) { return; }

            var picker = new FileSavePicker();
            if (picker != null)
            {
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeChoices.Add("JPEG (*.jpg;*.jpeg;*.jpe;*.jfif)", new List<string> { ".jpeg", ".jpg" });
                picker.SuggestedSaveFile = capturedPhoto;

                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    var storageFile = file;
                    try
                    {
                        if (storageFile != null)
                        {
                            await capturedPhoto.MoveAndReplaceAsync(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowMessageService.Instance.ShowMessage(ex.ToString());
                    }
                }
            }
        }
        private async Task SavePhotoForWindowsMobile(StorageFile capturedPhoto)
        {
            if (capturedPhoto == null) { return; }

            var folderPicker = new FolderPicker();
            if (folderPicker != null)
            {
                folderPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                folderPicker.FileTypeFilter.Add(".jpg");

                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    var storageFolder = folder;
                    try
                    {
                        if (storageFolder != null)
                        {
                            await capturedPhoto.MoveAsync(storageFolder);
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowMessageService.Instance.ShowMessage(ex.ToString());
                    }
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

            if (result != null)
            {
                result.Append("UWP").Append($"_{year}{month}{day}").Append($"_{hour}_{minute}_{second}").Append(".jpg");
            }

            return result.ToString();
        }
        private async void LoadPhotoFilesFromPicturesLibraryFolders(List<StorageFile> photoFiles, StorageFolder storageFolder)
        {
            if (photoFiles == null || storageFolder == null) { return; }

            IReadOnlyList<StorageFolder> folders = null;
            await Task.Run(async () =>
            {
                folders = await storageFolder.GetFoldersAsync();
            });

            IReadOnlyList<StorageFile> files = null;
            await Task.Run(async () =>
            {
                files = await storageFolder.GetFilesAsync();
            });

            if (folders != null || files != null)
            {
                if (folders.Count > 0)
                {
                    foreach (var folder in folders)
                    {
                        await Task.Run(() =>
                        {
                            LoadPhotoFilesFromPicturesLibraryFolders(photoFiles, folder);
                        });
                    }
                }

                if (files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        if (ExtensionCheckService.Instance.HasGraphicExtension(file))
                        {
                            photoFiles.Add(file);
                        }
                    }
                }
            }
        }
        private async Task<List<GalleryPhoto>> LoadGallery()
        {
            List<GalleryPhoto> photoGallery = null;

            if (_photoFiles != null)
            {
                if (_photoFiles.Count > 0)
                {
                    photoGallery = new List<GalleryPhoto>();
                    if (photoGallery != null)
                    {
                        for (int i = 0; i < _photoFiles.Count; i++)
                        {
                            StorageFile photoFile = _photoFiles[i];
                            await OpenPhotoThumbnailFileStreamAndLoadToPhotoGallery(photoGallery, photoFile, i);
                        }
                    }
                }
            }

            return photoGallery;
        }
        private async Task OpenPhotoThumbnailFileStreamAndLoadToPhotoGallery(List<GalleryPhoto> photoGallery, StorageFile photoFile, int index)
        {
            if (photoGallery == null || photoFile == null) { return; }

            try
            {
                var photo = new GalleryPhoto();
                if (photo != null)
                {
                    photo.Index = Convert.ToUInt32(index);
                    photo.Name = photoFile.Name;

                    using (var fileStream = await photoFile.GetThumbnailAsync(ThumbnailMode.PicturesView))
                    {
                        photo.Thumbnail = new BitmapImage();

                        if (fileStream != null && photo.Thumbnail != null)
                        {
                            await photo.Thumbnail.SetSourceAsync(fileStream);
                        }
                    }
                }

                photoGallery.Add(photo);
            }
            catch (Exception ex)
            {
                ShowMessageService.Instance.ShowMessage(ex.ToString());
            }
        }
        #endregion
    }
}

﻿using ExifLib;
using System;
using System.Collections.Generic;
using System.IO;
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
                await SavePhoto(capturedPhoto);
        }
        public async Task<bool> IsCameraAvailable()
        {
            return await IsCameraDeviceExist();
        }
        public async Task<DetailPhoto> LoadAndGetPhoto()
        {
            DetailPhoto result = null;

            await LoadPhoto();
            if (_photo != null)
            {
                result = new DetailPhoto();
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
                List<StorageFile> photoFiles = null;
                if (photoFiles == null)
                {
                    photoFiles = new List<StorageFile>();
                    LoadPhotoFilesFromPicturesLibraryFolders(photoFiles, folderPath);
                }
                _photoFiles = photoFiles;
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
            _fileIndex = (int)clickedPhotoIndex;
        }
        public RandomAccessStreamReference GetCurrentPhoto()
        {
            var currentPhotoIndex = _fileIndex - 1;
            var currentPhotoFile = _photoFiles[currentPhotoIndex];
            var currentPhotoStreamReference = RandomAccessStreamReference.CreateFromFile(currentPhotoFile);

            return currentPhotoStreamReference;
        }
        public StorageFile GetCurrentPhotoFile()
        {
            var currentPhotoIndex = _fileIndex - 1;
            var currentPhotoFile = _photoFiles[currentPhotoIndex];

            return currentPhotoFile;
        }
        public async Task<RandomAccessStreamReference> GetThumbnailOfCurrentPhoto()
        {
            var currentPhotoIndex = _fileIndex - 1;
            var currentPhotoFile = _photoFiles[currentPhotoIndex];
            var thumbnail = await currentPhotoFile.GetThumbnailAsync(ThumbnailMode.PicturesView);
            var currentPhotoThumbnailStreamReference = RandomAccessStreamReference.CreateFromStream(thumbnail);

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
                if (cameraDevices.Count > 0)
                {
                    foreach (var cameraDevice in cameraDevices)
                    {
                        if (cameraDevice.IsEnabled)
                        {
                            result = true;
                            break;
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

            if (_fileIndex < _photoFiles.Count)
            {
                var file = _photoFiles[_fileIndex];
                await OpenPhotoFileStreamAndLoadSources(file);
                _fileIndex = (_fileIndex + 1) % _photoFiles.Count;
            }
        }
        private void CheckIfFilesExist()
        {
            if (_photoFiles == null)
                LoadFiles();
            else if(_photoFiles.Count == 0)
                ShowMessageService.Instance.ShowMessage("The picture library is empty!");
        }
        private async Task OpenPhotoFileStreamAndLoadSources(StorageFile file)
        {
            try
            {
                using (var fileStream = await file.OpenAsync(FileAccessMode.Read))
                {
                    LoadMetaDataInfoFromPhotoAndSetToServiceFields(file, fileStream);
                    await SetPhotoSourceFromFileStream(fileStream);
                }
            }
            catch (Exception ex)
            {
                ShowMessageService.Instance.ShowMessage(ex.Message);
            }
        }
        private void LoadMetaDataInfoFromPhotoAndSetToServiceFields(StorageFile file, IRandomAccessStream fileStream)
        {
            _size = GetSizeInfoFromPhoto(fileStream);
            _date = GetDataCreatedInfoFromPhoto(file);
            _latitude = null;
            _longitude = null;

            if (ExtensionCheckService.Instance.HasPhotoExtension(file))
            {
                var clonedStream = fileStream.CloneStream();
                using (var reader = new ExifReader(clonedStream.AsStreamForRead()))
                {
                    try
                    {
                        SetGpsLatitudeAndLongitudeInfoFromPhoto(reader);
                    }
                    catch (Exception ex)
                    {
                        ShowMessageService.Instance.ShowMessage(ex.Message);
                    }
                }
                clonedStream.Dispose();
            }
        }
        private async Task SetPhotoSourceFromFileStream(IRandomAccessStream fileStream)
        {
            _photo = new BitmapImage();
            await _photo.SetSourceAsync(fileStream);
        }
        private ulong GetSizeInfoFromPhoto(IRandomAccessStream fileStream)
        {
            ulong result;

            _size = result = ulong.MinValue;
            result = fileStream.Size;

            return result;
        }
        private DateTime GetDataCreatedInfoFromPhoto(StorageFile file)
        {
            DateTime result;

            _date = result = DateTime.MinValue;
            result = file.DateCreated.DateTime;

            return result;
        }
        private void SetGpsLatitudeAndLongitudeInfoFromPhoto(ExifReader reader)
        {
            double[] latitude;
            reader.GetTagValue(ExifTags.GPSLatitude, out latitude);
            _latitude = (latitude != null) ? (double[])latitude.Clone() : null;

            double[] longitude;
            reader.GetTagValue(ExifTags.GPSLongitude, out longitude);
            _longitude = (longitude != null) ? (double[])longitude.Clone() : null;
        }
        private void SetPhotoInfo(DetailPhoto photo)
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
            List<StorageFile> newStorageFiles = new List<StorageFile>(_photoFiles);
            newStorageFiles.Add(capturedPhoto);
            _photoFiles = newStorageFiles;
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
        private async void LoadPhotoFilesFromPicturesLibraryFolders(List<StorageFile> photoFiles, StorageFolder storageFolder)
        {
            var folders = await storageFolder.GetFoldersAsync();
            var files = await storageFolder.GetFilesAsync();
            if (folders.Count > 0)
            {
                foreach (var folder in folders)
                {
                    if (photoFiles != null)
                    {
                        await Task.Run(() =>
                        {
                            LoadPhotoFilesFromPicturesLibraryFolders(photoFiles, folder);
                        });
                    }
                }
            }
            if (files.Count > 0)
            {
                foreach (var file in files)
                {
                    if (ExtensionCheckService.Instance.HasGraphicExtension(file) && photoFiles != null)
                        photoFiles.Add(file);
                }
            }
        }
        private async Task<List<GalleryPhoto>> LoadGallery()
        {
            CheckIfFilesExist();

            List<GalleryPhoto> photoGallery = null;
            if (_photoFiles != null)
            {
                photoGallery = new List<GalleryPhoto>();
                for(int i = 0; i < _photoFiles.Count; i++)
                {
                    StorageFile photoFile = _photoFiles[i];
                    await OpenPhotoThumbnailFileStreamAndLoadToPhotoGallery(photoGallery, photoFile, i);
                }
            }

            return photoGallery;
        }
        private async Task OpenPhotoThumbnailFileStreamAndLoadToPhotoGallery(List<GalleryPhoto> photoGallery, StorageFile photoFile, int index)
        {
            try
            {
                var photo = new GalleryPhoto();
                photo.Index = (uint)index;
                photo.Name = photoFile.Name;
                using (var fileStream = await photoFile.GetThumbnailAsync(ThumbnailMode.PicturesView))
                {
                    photo.Thumbnail = new BitmapImage();
                    await photo.Thumbnail.SetSourceAsync(fileStream);
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

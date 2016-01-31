using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UWP.Models;
using Windows.Storage;
using Windows.Storage.Streams;

namespace UWP.Interfaces
{
    public interface IPhotoCameraService
    {
        Task<GalleryPhoto> CaptureAndSavePhoto();
        Task<bool> IsCameraAvailable();
        Task<DetailPhoto> LoadAndGetPhoto();
        Task<ObservableCollection<GalleryPhoto>> LoadAndGetGallery();
        void LoadFiles();
        string GetDeviceFamilyInfo();
        void SetFileIndexToClickedItem(uint clickedPhotoIndex);
        RandomAccessStreamReference GetCurrentPhoto();
        StorageFile GetCurrentPhotoFile();
        Task<RandomAccessStreamReference> GetThumbnailOfCurrentPhoto();
    }
}

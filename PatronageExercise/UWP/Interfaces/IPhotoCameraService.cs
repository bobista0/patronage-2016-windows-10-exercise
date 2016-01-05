using System.Threading.Tasks;
using UWP.Models;

namespace UWP.Interfaces
{
    public interface IPhotoCameraService
    {
        Task CaptureAndSavePhoto();
        void LoadFiles();
        Task<bool> IsCameraAvailable();
        Task<DetailPhoto> LoadAndGetPhoto();
    }
}

using System.Threading.Tasks;
using UWP.Models;

namespace UWP.Interfaces
{
    public interface IPhotoCameraService
    {
        void CaptureAndSavePhoto();
        void GetFiles();
        Task<bool> IsCameraAvailable();
        Task<Photo> LoadAndGetPhoto();
    }
}

using System;
using System.IO;
using Windows.Storage;

namespace UWP.Services
{
    public sealed class ExtensionCheckService
    {
        #region LAZY-SINGLETON IMPLEMENTATION
        private static readonly Lazy<ExtensionCheckService> lazy = new Lazy<ExtensionCheckService>(() => new ExtensionCheckService());
        public static ExtensionCheckService Instance { get { return lazy.Value; } }
        private ExtensionCheckService() { }
        #endregion

        #region METHODS
        public bool HasGraphicExtension(IStorageItem item)
        {
            return HasSpecificExtension(item, ".png", ".tif", ".bmp")
                || HasPhotoExtension(item);
        }
        public bool HasPhotoExtension(IStorageItem item)
        {
            return HasSpecificExtension(item, ".jpg", ".jpeg");
        }
        public bool HasSpecificExtension(IStorageItem item, params string[] extensions)
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

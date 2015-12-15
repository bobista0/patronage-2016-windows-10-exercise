using Windows.UI.Xaml.Controls;

namespace UWP
{
    public sealed partial class MainPage : Page
    {
        #region PRIVATE FIELDS
        #endregion

        #region CONSTRUCTORS
        public MainPage()
        {
            this.InitializeComponent();
        }
        #endregion

        #region PROPERTIES
        private bool _isGalleryEmpty = true;
        public bool IsGalleryEmpty
        {
            get { return _isGalleryEmpty; }
            set { _isGalleryEmpty = value; }
        }
        #endregion


        #region METHODS
        #endregion
    }
}

using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;

namespace UWP.Services
{
    public sealed class ShowMessageService
    {
        #region LAZY-SINGLETON IMPLEMENTATION
        private static readonly Lazy<ShowMessageService> lazy = new Lazy<ShowMessageService>(() => new ShowMessageService());
        public static ShowMessageService Instance { get { return lazy.Value; } }
        private ShowMessageService() { }
        #endregion

        #region PUBLIC METHODS
        public async void ShowMessage(string message, UICommandInvokedHandler handler = null)
        {
            if (message == null || message == string.Empty) { return; }

            var messageDialog = new MessageDialog(message);
            if (messageDialog != null)
            {
                messageDialog.Commands.Add(new UICommand("OK", handler));
                messageDialog.DefaultCommandIndex = 0;
                await messageDialog.ShowAsync();
            }
        }
        public void ShowMessageWithApplicationExit(string message)
        {
            if (message == null || message == string.Empty) { return; }

            ShowMessage(message, new UICommandInvokedHandler(this.CommandInvokeHandler));
        }
        #endregion

        #region PRIVATE METHODS
        private void CommandInvokeHandler(IUICommand command)
        {
            Application.Current.Exit();
        }
        #endregion
    }
}

using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;

namespace UWP.Services
{
    public sealed class ShowMessageService
    {
        #region LAZY-SINGLETON IMPLEMENTATION
        private static readonly Lazy<ShowMessageService> lazy = new Lazy<ShowMessageService>(() => new ShowMessageService());
        public static ShowMessageService Instance
        {
            get { return lazy.Value; }
        }
        private ShowMessageService() { }
        #endregion

        #region METHODS
        public async void ShowMessage(string message)
        {
            var messageDialog = new MessageDialog(message);
            messageDialog.Commands.Add(new UICommand("OK", new UICommandInvokedHandler(this.CommandInvokeHandler)));
            await messageDialog.ShowAsync();
        }

        private void CommandInvokeHandler(IUICommand command)
        {
            Application.Current.Exit();
        }
        #endregion
    }
}

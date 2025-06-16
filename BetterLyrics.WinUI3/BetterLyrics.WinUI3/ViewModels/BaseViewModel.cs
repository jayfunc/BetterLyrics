using System.Runtime.CompilerServices;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class BaseViewModel : ObservableRecipient
    {
        private protected readonly ISettingsService _settingsService;

        private protected readonly DispatcherQueue _dispatcherQueue =
            DispatcherQueue.GetForCurrentThread();

        public BaseViewModel(ISettingsService settingsService)
        {
            IsActive = true;
            _settingsService = settingsService;
        }
    }
}

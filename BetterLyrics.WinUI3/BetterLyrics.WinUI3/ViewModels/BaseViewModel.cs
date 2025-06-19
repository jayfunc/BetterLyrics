using System;
using System.Runtime.CompilerServices;
using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class BaseViewModel : ObservableRecipient, IDisposable
    {
        private protected readonly ISettingsService _settingsService;

        private protected readonly DispatcherQueue _dispatcherQueue =
            DispatcherQueue.GetForCurrentThread();

        public BaseViewModel(ISettingsService settingsService)
        {
            IsActive = true;
            _settingsService = settingsService;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}

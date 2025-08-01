// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using System;
using System.Runtime.InteropServices;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class BaseViewModel : ObservableRecipient
    {
        private protected readonly DispatcherQueue _dispatcherQueue;
        private protected readonly DispatcherQueueTimer _dispatcherQueueTimer;
        private protected readonly ISettingsService _settingsService;

        public BaseViewModel(ISettingsService settingsService)
        {
            IsActive = true;

            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _dispatcherQueueTimer = _dispatcherQueue.CreateTimer();
            _settingsService = settingsService;
        }
    }
}

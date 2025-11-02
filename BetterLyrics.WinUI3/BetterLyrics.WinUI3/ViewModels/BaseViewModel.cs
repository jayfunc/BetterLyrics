// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using System;
using System.Runtime.InteropServices;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class BaseViewModel : ObservableRecipient
    {
        private protected readonly DispatcherQueue _dispatcherQueue;

        public BaseViewModel()
        {
            IsActive = true;

            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }
    }
}

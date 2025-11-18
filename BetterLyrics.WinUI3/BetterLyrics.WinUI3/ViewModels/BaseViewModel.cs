// 2025/6/23 by Zhe Fang

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

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

        public DispatcherQueue GetCurrentDispatcherQueue() => _dispatcherQueue;
    }
}

using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Services.AppLifecycleService
{
    public interface IAppLifecycleService
    {
        IRelayCommand RestartAppCommand { get; }
        IRelayCommand ExitAppCommand { get; }
    }
}

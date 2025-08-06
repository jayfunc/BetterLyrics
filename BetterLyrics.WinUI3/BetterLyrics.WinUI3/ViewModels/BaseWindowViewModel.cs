using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class BaseWindowViewModel(ISettingsService settingsService) : BaseViewModel(settingsService)
    {
    }
}

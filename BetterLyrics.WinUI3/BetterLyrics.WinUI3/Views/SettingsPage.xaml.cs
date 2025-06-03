using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using ABI.System;
using Windows.System;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using System.Diagnostics;
using WinRT.Interop;
using Windows.Storage.Pickers;
using BetterLyrics.WinUI3.Models;
using Microsoft.Windows.ApplicationModel.Resources;
using BetterLyrics.WinUI3.Services.Settings;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page {

        public SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

        public SettingsPage() {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetService<SettingsViewModel>();
        }

    }
}

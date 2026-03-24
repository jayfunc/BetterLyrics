using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models
{
    public partial class LyricsCardStyleItem : ObservableObject
    {
        public string DisplayText { get; set; }
        public string StyleKey { get; set; }
        public DataTemplate CardDataTemplate => (DataTemplate)App.Current.Resources[StyleKey];
        public LyricsCardData CardData => LyricsCardDataExtensions.DemoLyricsCardData;
        [ObservableProperty] public partial bool IsChecked { get; set; } = false;
        [ObservableProperty] public partial bool IsExpanded { get; set; } = true;

    }
}

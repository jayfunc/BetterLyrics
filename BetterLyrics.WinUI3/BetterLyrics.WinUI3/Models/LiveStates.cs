using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Models
{
    public partial class LiveStates : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial LyricsWindowStatus LyricsWindowStatus { get; set; }

        /// <summary>
        /// 在需要暂时禁用监听歌词窗口位置大小变化时使用
        /// </summary>
        public bool IsLyricsWindowStatusRefreshing { get; set; } = false;

        public LiveStates()
        {
            LyricsWindowStatus = new LyricsWindowStatus();
        }
    }
}

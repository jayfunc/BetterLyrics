using CommunityToolkit.Mvvm.ComponentModel;

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

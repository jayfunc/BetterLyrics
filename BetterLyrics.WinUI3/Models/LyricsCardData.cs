using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Lyrics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;

namespace BetterLyrics.WinUI3.Models
{
    public partial class LyricsCardData : ObservableObject
    {
        [ObservableProperty] public partial string Title { get; set; } = "";
        [ObservableProperty] public partial string Artist { get; set; } = "";

        [ObservableProperty] public partial ImageSource? CoverImage { get; set; }
        [ObservableProperty] public partial AppColor? AccentCoverColor { get; set; }

        [ObservableProperty] public partial List<LyricsLine> Lyrics { get; set; } = new();

    }
}

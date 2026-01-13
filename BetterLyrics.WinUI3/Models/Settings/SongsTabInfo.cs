using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.ViewModels;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class SongsTabInfo : BaseViewModel
    {
        public string Name { get; set; } = "";

        public string Icon { get; set; } = "";

        public CommonSongProperty FilterProperty { get; set; } = CommonSongProperty.Title;

        public string FilterValue { get; set; } = "";

        public bool IsDefault => Icon == "\uE8A9";

        public SongsTabInfo()
        {
        }
    }
}
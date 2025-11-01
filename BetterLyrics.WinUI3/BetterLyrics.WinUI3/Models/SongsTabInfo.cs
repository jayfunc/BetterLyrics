using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.Models
{
    public partial class SongsTabInfo : BaseViewModel
    {
        public string Name { get; set; }

        public string Icon { get; set; }

        public bool IsClosable { get; set; }

        [ObservableProperty]
        public partial bool IsStarred { get; set; }

        public CommonSongProperty FilterProperty { get; set; }

        public string FilterValue { get; set; }

        public SongsTabInfo()
        {
            Name = string.Empty;
            Icon = string.Empty;
            IsClosable = true;
            IsStarred = false;
            FilterProperty = CommonSongProperty.Title;
            FilterValue = string.Empty;
        }

        public SongsTabInfo(string name, string icon, bool isClosable, bool isStarred, CommonSongProperty filterProperty, string filterValue)
        {
            Name = name;
            Icon = icon;
            IsClosable = isClosable;
            IsStarred = isStarred;
            FilterProperty = filterProperty;
            FilterValue = filterValue;
        }
    }
}
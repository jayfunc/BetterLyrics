using BetterLyrics.WinUI3.Enums;

namespace BetterLyrics.WinUI3.Models
{
    public class SongsTabInfo
    {
        public string Name { get; set; }

        public string Icon { get; set; }

        public bool IsClosable { get; set; }

        public CommonSongProperty FilterProperty { get; set; }

        public string FilterValue { get; set; }

        public SongsTabInfo()
        {
            Name = string.Empty;
            Icon = string.Empty;
            IsClosable = true;
            FilterProperty = CommonSongProperty.Title;
            FilterValue = string.Empty;
        }

        public SongsTabInfo(string name, string icon, bool isClosable, CommonSongProperty filterProperty, string filterValue)
        {
            Name = name;
            Icon = icon;
            IsClosable = isClosable;
            FilterProperty = filterProperty;
            FilterValue = filterValue;
        }
    }
}

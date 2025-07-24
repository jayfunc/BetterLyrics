using BetterLyrics.WinUI3.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.TemplateSelector;

public class SongOrderTemplateSelector : DataTemplateSelector
{
    public DataTemplate ByTitleTemplate { get; set; }
    public DataTemplate ByAlbumTemplate { get; set; }
    public DataTemplate ByArtistTemplate { get; set; }

    public SongOrderType SongOrderType { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return SongOrderType switch
        {
            SongOrderType.Title => ByTitleTemplate,
            SongOrderType.Album => ByAlbumTemplate,
            SongOrderType.Artist => ByArtistTemplate,
            _ => ByTitleTemplate
        };
    }
}
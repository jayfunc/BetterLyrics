using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Services.LocalizationService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class ComponentTypeExtensions
    {
        extension(ComponentType type)
        {
            public SolidColorBrush GetSolidColorBrush() => type switch
            {
                ComponentType.AlbumArt => new SolidColorBrush(Colors.CornflowerBlue),
                ComponentType.Lyrics => new SolidColorBrush(Colors.MediumSeaGreen),
                ComponentType.SongInfo => new SolidColorBrush(Colors.Orange),
                _ => new SolidColorBrush(Colors.Transparent)
            };

            public string GetDisplayName()
            {
                var localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();
                return localizationService.GetLocalizedString($"LayoutEditorControlComponent{type}");
            }
        }
    }
}

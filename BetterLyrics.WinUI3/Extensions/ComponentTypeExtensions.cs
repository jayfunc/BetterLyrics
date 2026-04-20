using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Services.LocalizationService;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class ComponentTypeExtensions
    {
        extension(ComponentType type)
        {
            public string GetDisplayName()
            {
                var localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();
                return localizationService.GetLocalizedString($"LayoutEditorControlComponent{type}");
            }
        }
    }
}

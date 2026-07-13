using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Core.Extensions;

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
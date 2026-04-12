using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Services.LocalizationService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
namespace BetterLyrics.WinUI3.Models
{
    public class ToolboxItem
    {
        public ComponentType ComponentType { get; set; }

        public SolidColorBrush ColorBrush => ComponentType.GetSolidColorBrush();

        public string DisplayName => ComponentType.GetDisplayName();
    }
}

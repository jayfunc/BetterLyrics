using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using Microsoft.UI.Xaml.Media;
namespace BetterLyrics.WinUI3.Models
{
    public class ToolboxItem
    {
        public ComponentType ComponentType { get; set; }
        public string DisplayName => ComponentType.GetDisplayName();
    }
}

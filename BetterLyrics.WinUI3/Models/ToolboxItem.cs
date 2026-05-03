using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
namespace BetterLyrics.WinUI3.Models
{
    public class ToolboxItem
    {
        public ComponentType ComponentType { get; set; }
        public string DisplayName => ComponentType.GetDisplayName();
    }
}

using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;

namespace BetterLyrics.Core.Models;

public class ToolboxItem
{
    public ComponentType ComponentType { get; set; }
    public string DisplayName => ComponentType.GetDisplayName();
}
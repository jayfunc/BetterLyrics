using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Models;

public class SettingsNavMenuItem
{
    public string Label { get; set; }
    public string Glyph { get; set; }
    public SettingsSection Section { get; set; }
}
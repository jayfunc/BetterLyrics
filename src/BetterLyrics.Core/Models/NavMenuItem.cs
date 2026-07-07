using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Models;

public class NavMenuItem
{
    public string Label { get; set; }
    public string Glyph { get; set; }
    public SettingsSection Section { get; set; }
}
using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Models;

public class SettingSearchItem
{
    public string Uid { get; set; }
    public string Title { get; set; }
    public string Path { get; set; }
    public SettingsSection Section { get; set; }
    public Enum? Subsection { get; set; }
    public object TargetParameter { get; set; }
}

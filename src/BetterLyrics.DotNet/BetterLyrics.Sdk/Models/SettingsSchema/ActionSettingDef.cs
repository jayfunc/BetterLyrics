namespace BetterLyrics.Sdk.Models.SettingsSchema;

public class ActionSettingDef : SettingDef
{
    public string ButtonText { get; set; }
    public Action<string> Action { get; set; }
}
namespace BetterLyrics.Core.Models.SettingsSchema
{
    public class ActionSettingDef : SettingDef
    {
        public string ButtonText { get; set; }
        public Action<Dictionary<string, object>> Action { get; set; }
    }
}

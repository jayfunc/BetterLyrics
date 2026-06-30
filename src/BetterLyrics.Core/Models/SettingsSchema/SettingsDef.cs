namespace BetterLyrics.Core.Models.SettingsSchema
{
    public abstract class SettingDef
    {
        public string Key { get; set; }
        public string Header { get; set; }
        public string Description { get; set; }
        public object Value { get; set; }
    }
}

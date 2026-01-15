namespace BetterLyrics.Core.Models.SettingsSchema
{
    public class NumberSettingDef : SettingDef
    {
        public double Min { get; set; } = 0;
        public double Max { get; set; } = 100;
        public double Step { get; set; } = 1;
    }

}

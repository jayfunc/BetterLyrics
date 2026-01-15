using BetterLyrics.Core.Abstractions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.SettingsSchema;
using System.Text;

namespace BetterLyrics.Plugins.AI.Local.NMT
{
    public class Plugin : PluginBase<Config>, IAIService
    {
        public override string Name => "Local NMT Service";
        public override string Description => "Provide NMT service for other plugins";

        public async Task<string> ChatAsync(string systemPrompt, string userPrompt)
        {
            return "";
        }

        public override IEnumerable<SettingDef> GetSettings()
        {
            yield return SettingBuilder.Text(() => NMT.Config.ModelPath, Context.Localizer);
        }

        protected override async Task OnInitializeAsync()
        {
            if (string.IsNullOrEmpty(NMT.Config.ModelPath))
            {
                return;
            }
        }

        protected override async ValueTask OnShutdownAsync()
        {
        }
    }
}

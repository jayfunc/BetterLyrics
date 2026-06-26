using BetterLyrics.Core.Abstractions.Plugins;
using BetterLyrics.Core.Interfaces.Plugins;

namespace BetterLyrics.Plugins.AI.Local.NMT
{
    public class Plugin : PluginBase<Config>, IAIService
    {
        public override string Title { get; set; } = "Local NMT";

        public async Task<string> ChatAsync(string systemPrompt, string userPrompt)
        {
            return "";
        }

        protected override async Task OnInitializeAsync()
        {
            if (string.IsNullOrEmpty(Config.ModelPath))
            {
                return;
            }
        }

        protected override async Task OnShutdownAsync()
        {
        }
    }
}

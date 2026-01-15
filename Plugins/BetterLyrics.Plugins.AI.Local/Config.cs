using BetterLyrics.Core.Abstractions;

namespace BetterLyrics.Plugins.AI.Local
{
    public class Config : PluginConfigBase
    {
        public string ModelPath
        {
            get => Get("");
            set => Set(value);
        }
        public int ContextSize
        {
            get => Get(2048);
            set => Set(value);
        }
        public int Seed
        {
            get => Get(42);
            set => Set(value);
        }
        public int Threads
        {
            get => Get(4);
            set => Set(value);
        }
    }
}

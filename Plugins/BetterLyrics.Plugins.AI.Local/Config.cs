using BetterLyrics.Core.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace BetterLyrics.Plugins.AI.Local
{
    public class Config : PluginConfigBase
    {
        [Display(Name = "Model Path", Description = "Input model absolute uri here")]
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

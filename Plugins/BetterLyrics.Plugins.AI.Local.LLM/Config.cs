using BetterLyrics.Core.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace BetterLyrics.Plugins.AI.Local.LLM
{
    public class Config : PluginConfigBase
    {
        [Display(Name = "Model Path", Description = "Input model absolute uri here")]
        public string ModelPath
        {
            get => Get("");
            set => Set(value);
        }

        [Display(Name = "Context Size", Description = "Set the context size for the model")]
        public int ContextSize
        {
            get => Get(2048);
            set => Set(value);
        }

        [Display(Name = "GPU Layer Count", Description = "Set the number of layers to offload to GPU. Set 0 to use CPU only.")]
        public int GpuLayerCount
        {
            get => Get(0);
            set => Set(value);
        }

        [Display(Name = "Threads", Description = "Set the number of threads to use for inference")]
        public int Threads
        {
            get => Get(4);
            set => Set(value);
        }
    }
}

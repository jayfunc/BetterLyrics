using BetterLyrics.Core;
using BetterLyrics.Core.Interfaces;
using BetterLyrics.Core.Interfaces.Services;
using LLama;
using LLama.Common;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.Text;
using Windows.Services.Maps;

namespace BetterLyrics.Plugins.AI.Local
{
    public class Plugin : PluginBase, IAIService
    {
        private IPluginContext? _pluginContext;

        private LLamaWeights? _model;
        private LLamaContext? _llamaContext;
        private ChatSession? _session;

        public override string Name => "Local AI Service";
        public override string Description => "Provide AI service for other plugins";

        public async Task<string> ChatAsync(string systemPrompt, string userPrompt)
        {
            if (_session == null) return "Error: Session was not initialized";

            return await Task.Run(async () =>
            {
                try
                {
                    _session.History.Messages.Clear();

                    if (!string.IsNullOrWhiteSpace(systemPrompt))
                    {
                        _session.History.AddMessage(AuthorRole.System, systemPrompt);
                    }

                    var inferenceParams = new InferenceParams()
                    {
                        AntiPrompts = new List<string> { "<|im_end|>", "<|endoftext|>" }
                    };

                    StringBuilder sb = new StringBuilder();

                    await foreach (var token in _session.ChatAsync(
                        new ChatHistory.Message(AuthorRole.User, userPrompt),
                        inferenceParams))
                    {
                        sb.Append(token);
                    }

                    return sb.ToString();
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            });
        }

        public override void OnLoad(IPluginContext context)
        {
            _pluginContext = context;

            var modelPath = Directory.GetFiles(context.PluginDirectory, "*.gguf").FirstOrDefault();

            if (string.IsNullOrEmpty(modelPath))
            {
                return;
            }

            try
            {
                var parameters = new ModelParams(modelPath)
                {
                    ContextSize = 2048, // 上下文长度，根据内存调整
                    GpuLayerCount = 99, // 0 = 纯CPU，设为 20+ 可以通过显卡加速（需安装 Cuda 后端）
                    Threads = 4 // CPU 线程数
                };

                _model = LLamaWeights.LoadFromFile(parameters);
                _llamaContext = _model.CreateContext(parameters);
                var executor = new InteractiveExecutor(_llamaContext);
                _session = new ChatSession(executor);
                _session.WithHistoryTransform(new ChatMLHistoryTransform());
            }
            catch (Exception ex)
            {
            }
        }

        public override void OnUnload()
        {
            _llamaContext?.Dispose();
            _model?.Dispose();
        }
    }
}

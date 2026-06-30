using BetterLyrics.Core.Abstractions.Plugins;
using BetterLyrics.Core.Interfaces.Plugins;
using LLama;
using LLama.Common;
using System.Text;

namespace BetterLyrics.Plugins.AI.Local.LLM
{
    public class Plugin : PluginBase<Config>, IAIService
    {
        private LLamaWeights? _model;
        private LLamaContext? _llamaContext;
        private ChatSession? _session;

        public override string Title { get; set; } = "Local LLM";

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

        protected override async Task OnInitializeAsync()
        {
            if (string.IsNullOrEmpty(Config.ModelPath))
            {
                return;
            }

            try
            {
                var parameters = new ModelParams(Config.ModelPath)
                {
                    ContextSize = (uint?)Config.ContextSize,
                    GpuLayerCount = 99,
                    Threads = Config.Threads
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

        protected override async Task OnShutdownAsync()
        {
            _llamaContext?.Dispose();
            _model?.Dispose();
        }
    }
}

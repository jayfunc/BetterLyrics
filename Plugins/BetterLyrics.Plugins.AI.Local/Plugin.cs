using BetterLyrics.Core.Interfaces;
using BetterLyrics.Core.Interfaces.Services;
using LLama;
using LLama.Common;
using System.Reflection;
using System.Text;

namespace BetterLyrics.Plugins.AI.Local
{
    public class Plugin : IPlugin, IAIService
    {
        private LLamaWeights? _model;
        private ModelParams? _parameters;
        private string? _modelPath;

        public string Id => "jayfunc.ai";
        public string Name => "Local AI Service";
        public string Description => "";
        public string Author => "jayfunc";
        public string Version => "1.0.0.0";
        public DateTime LastUpdated => new DateTime(2026, 1, 12);

        public async Task<string> ChatAsync(string systemPrompt, string userPrompt)
        {
            if (_model == null) return "错误：本地模型未加载。请检查插件目录下是否有 .gguf 文件。";

            return await Task.Run(async () =>
            {
                try
                {
                    if (_parameters == null)
                    {
                        return "错误：模型参数未正确配置。";
                    }

                    // 1. 创建上下文
                    using var context = _model.CreateContext(_parameters);

                    // 2. 创建聊天执行器 (InteractiveExecutor 适合聊天)
                    var executor = new InteractiveExecutor(context);

                    // 3. 创建会话
                    var session = new ChatSession(executor);

                    // 4. 设置系统提示词 (System Prompt)
                    // LLamaSharp 的 ChatSession 处理 System Prompt 稍微有点技巧，通常直接作为第一次输入的一部分或者使用特定的 Prompt 模板
                    // 这里做一个简单的处理：

                    var fullPrompt = $"{systemPrompt}\n\n用户: {userPrompt}\n助手:";

                    // 5. 运行推理
                    StringBuilder responseBuilder = new StringBuilder();

                    // Transform: 将输出流转换为文本
                    // InferenceParams: 设置采样参数 (Temperature 等)
                    var inferenceParams = new InferenceParams()
                    {
                        //Temperature = 0.6f,
                        AntiPrompts = new List<string> { "用户:" } // 防止模型自问自答
                    };

                    await foreach (var text in session.ChatAsync(new ChatHistory.Message(AuthorRole.User, userPrompt), inferenceParams: inferenceParams))
                    {
                        responseBuilder.Append(text);
                    }

                    return responseBuilder.ToString();
                }
                catch (Exception ex)
                {
                    return $"推理出错: {ex.Message}";
                }
            });
        }

        public void OnLoad(IPluginContext context)
        {
            _modelPath = Directory.GetFiles(context.PluginDirectory, "*.gguf").FirstOrDefault();

            if (string.IsNullOrEmpty(_modelPath))
            {
                System.Diagnostics.Debug.WriteLine("【LocalAI】未找到 .gguf 模型文件，请下载模型放入插件目录！");
                return;
            }

            try
            {
                // 2. 配置模型参数
                _parameters = new ModelParams(_modelPath)
                {
                    ContextSize = 2048, // 上下文长度，根据内存调整
                    GpuLayerCount = 0,  // 0 = 纯CPU，设为 20+ 可以通过显卡加速（需安装 Cuda 后端）
                    Threads = 4         // CPU 线程数
                };

                // 3. 加载模型 (这一步比较耗时，实际开发建议放在后台线程或第一次调用时懒加载)
                _model = LLamaWeights.LoadFromFile(_parameters);

                System.Diagnostics.Debug.WriteLine($"【LocalAI】模型加载成功: {_modelPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"【LocalAI】模型加载失败: {ex.Message}");
            }
        }

        public void OnUnload()
        {
            _model?.Dispose();
        }
    }
}

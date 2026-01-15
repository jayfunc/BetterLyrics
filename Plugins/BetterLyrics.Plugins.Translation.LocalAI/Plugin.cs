using BetterLyrics.Core.Abstractions;
using BetterLyrics.Core.Interfaces.Features;
using BetterLyrics.Core.Interfaces.Services;
using System.Text;
using System.Text.RegularExpressions;

namespace BetterLyrics.Plugins.Translation.LocalAI
{
    public class Plugin : PluginBase<Config>, ILyricsTranslator
    {
        private IAIService? _aiService;

        public override string Name => "Local AI translator";
        public override string Description => "Translate lyrics into your language via local AI plugin";

        public async Task<string?> GetTranslationAsync(string text, string targetLangCode)
        {
            if (_aiService == null || string.IsNullOrWhiteSpace(text)) return text;

            string langName = ConvertLangCodeToName(targetLangCode);

            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var translatedLines = new string[lines.Length];

            var inputBuilder = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                string content = string.IsNullOrWhiteSpace(lines[i]) ? "(EMPTY)" : lines[i];
                inputBuilder.AppendLine($"[L{i}] {content}");
            }

            string prompt = $"""
                Task: Translate lyrics to {langName}.
                Rules:
                1. Keep the "[Ln]" tags at the start of every line UNCHANGED.
                2. Translate the text after the tag.
                3. If the line is "(EMPTY)", output "[Ln] " (empty).
                4. Do not merge lines. Do not change the order.
                5. Output strictly in the format: "[Ln] Translation".
                """;

            try
            {
                var result = await _aiService.ChatAsync(prompt, inputBuilder.ToString());

                ParseAndFill(result, lines, translatedLines);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Translation Failed: {ex.Message}");
            }

            for (int i = 0; i < lines.Length; i++)
            {
                if (translatedLines[i] == null)
                {
                    translatedLines[i] = lines[i];
                }
            }

            return string.Join("\n", translatedLines);
        }

        private void ParseAndFill(string? aiOutput, string[] originalLines, string[] resultArray)
        {
            if (string.IsNullOrWhiteSpace(aiOutput)) return;

            var resultLines = aiOutput.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in resultLines)
            {
                var match = Regex.Match(line, @"^\[L(\d+)\]\s*(.*)");

                if (match.Success)
                {
                    if (int.TryParse(match.Groups[1].Value, out int index))
                    {
                        if (index >= 0 && index < resultArray.Length)
                        {
                            string content = match.Groups[2].Value.Trim();

                            if (content == "(EMPTY)" || string.IsNullOrWhiteSpace(content))
                            {
                                resultArray[index] = "";
                            }
                            else
                            {
                                resultArray[index] = content.Trim('"');
                            }
                        }
                    }
                }
            }
        }

        protected override async Task OnInitializeAsync()
        {
            _aiService = Context.AIService;
        }

        private string ConvertLangCodeToName(string code)
        {
            return code.ToLower() switch
            {
                "zh" or "zh-cn" or "chs" => "Simplified Chinese",
                "zh-tw" or "cht" => "Traditional Chinese",
                "en" => "English",
                "ja" or "jp" => "Japanese",
                "ko" or "kr" => "Korean",
                "fr" => "French",
                "de" => "German",
                "es" => "Spanish",
                "ru" => "Russian",
                _ => code
            };
        }
    }
}

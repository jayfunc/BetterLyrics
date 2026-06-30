using LLama.Abstractions;
using LLama.Common;
using System.Text;

namespace BetterLyrics.Plugins.AI.Local.LLM
{
    public class ChatMLHistoryTransform : IHistoryTransform
    {
        public string HistoryToText(ChatHistory history)
        {
            var sb = new StringBuilder();

            foreach (var message in history.Messages)
            {
                string roleName = message.AuthorRole switch
                {
                    AuthorRole.User => "user",
                    AuthorRole.System => "system",
                    AuthorRole.Assistant => "assistant",
                    _ => "user"
                };

                // 格式：<|im_start|>role\nContent<|im_end|>\n
                sb.Append($"<|im_start|>{roleName}\n{message.Content}<|im_end|>\n");
            }

            sb.Append("<|im_start|>assistant\n");

            return sb.ToString();
        }

        public ChatHistory TextToHistory(AuthorRole role, string text)
        {
            var history = new ChatHistory();
            history.AddMessage(role, text);
            return history;
        }

        public IHistoryTransform Clone()
        {
            return new ChatMLHistoryTransform();
        }
    }
}

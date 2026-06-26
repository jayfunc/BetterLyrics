namespace BetterLyrics.Core.Interfaces.Plugins
{
    public interface IAIService
    {
        Task<string> ChatAsync(string systemPrompt, string userPrompt);
    }
}

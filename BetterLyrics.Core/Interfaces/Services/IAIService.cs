namespace BetterLyrics.Core.Interfaces.Services
{
    public interface IAIService
    {
        Task<string> ChatAsync(string systemPrompt, string userPrompt);
    }
}

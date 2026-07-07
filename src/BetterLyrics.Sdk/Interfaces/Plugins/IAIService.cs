namespace BetterLyrics.Sdk.Interfaces.Plugins;

public interface IAIService
{
    Task<string> ChatAsync(string systemPrompt, string userPrompt);
}
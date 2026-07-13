namespace BetterLyrics.Core.Interfaces.Providers;

public interface IAssetReaderProvider
{
    Task<List<string>> ReadAllLinesAsync(string assetFilename);
    Task<string> ReadAllTextAsync(string assetFilename);
    Task<Stream> GetAssetStreamAsync(string assetFilename);
}


using BetterLyrics.Core.Interfaces.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace BetterLyrics.Avalonia.Providers;

public class AssetReaderProvider : IAssetReaderProvider
{
    private Uri FormatAssetUri(string assetFilename)
    {
        if (string.IsNullOrWhiteSpace(assetFilename))
        {
            throw new ArgumentException("Filename cannot be null or whitespace", nameof(assetFilename));
        }

        if (assetFilename.StartsWith("avares://", StringComparison.OrdinalIgnoreCase))
        {
            return new Uri(assetFilename);
        }

        string normalizedPath = assetFilename.Replace('\\', '/').TrimStart('/');

        if (!normalizedPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = $"Assets/{normalizedPath}";
        }

        string? assemblyName = GetType().Assembly.GetName().Name;

        return new Uri($"avares://{assemblyName}/{normalizedPath}");
    }

    public Task<Stream> GetAssetStreamAsync(string assetFilename)
    {
        var uri = FormatAssetUri(assetFilename);

        var stream = AssetLoader.Open(uri);
        return Task.FromResult(stream);
    }

    public async Task<List<string>> ReadAllLinesAsync(string assetFilename)
    {
        var uri = FormatAssetUri(assetFilename);

        using var stream = AssetLoader.Open(uri);
        using var reader = new StreamReader(stream);

        var lines = new List<string>();
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            lines.Add(line);
        }

        return lines;
    }

    public async Task<string> ReadAllTextAsync(string assetFilename)
    {
        var uri = FormatAssetUri(assetFilename);

        using var stream = AssetLoader.Open(uri);
        using var reader = new StreamReader(stream);

        return await reader.ReadToEndAsync();
    }
}
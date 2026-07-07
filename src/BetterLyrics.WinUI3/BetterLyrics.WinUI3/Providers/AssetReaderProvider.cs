using BetterLyrics.Core.Interfaces.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace BetterLyrics.WinUI3.Providers;

public class AssetReaderProvider : IAssetReaderProvider
{
    private static string FormatAssetPath(string assetFilename)
    {
        if (string.IsNullOrWhiteSpace(assetFilename))
        {
            throw new ArgumentException("Filename cannot be null or whitespace", nameof(assetFilename));
        }

        if (assetFilename.StartsWith("ms-appx://", StringComparison.OrdinalIgnoreCase))
        {
            return assetFilename;
        }

        string normalizedPath = assetFilename.Replace('\\', '/').TrimStart('/');

        if (!normalizedPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = $"Assets/{normalizedPath}";
        }

        return $"ms-appx:///{normalizedPath}";
    }

    public async Task<Stream> GetAssetStreamAsync(string assetFilename)
    {
        var uri = new Uri(FormatAssetPath(assetFilename));
        StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);

        return await file.OpenStreamForReadAsync();
    }

    public async Task<List<string>> ReadAllLinesAsync(string assetFilename)
    {
        var uri = new Uri(FormatAssetPath(assetFilename));
        StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);

        var lines = await FileIO.ReadLinesAsync(file);
        return new List<string>(lines);
    }

    public async Task<string> ReadAllTextAsync(string assetFilename)
    {
        var uri = new Uri(FormatAssetPath(assetFilename));
        StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);

        return await FileIO.ReadTextAsync(file);
    }
}
using System;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Memory;

namespace BetterLyrics.Avalonia.Providers;

public class UniversalMemoryReaderProvider : IUniversalMemoryReaderProvider
{
    public MemoryReaderConfig Config { get; set; }
    public event Action<double, double>? OnProgressChanged;
    public void Start()
    {
        throw new NotImplementedException();
    }

    public void Stop()
    {
        throw new NotImplementedException();
    }
}
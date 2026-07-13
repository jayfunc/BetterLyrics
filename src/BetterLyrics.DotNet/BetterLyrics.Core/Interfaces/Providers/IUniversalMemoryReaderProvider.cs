using BetterLyrics.Core.Models.Memory;

namespace BetterLyrics.Core.Interfaces.Providers;

public interface IUniversalMemoryReaderProvider
{
    MemoryReaderConfig Config { get; set; }
    event Action<double, double>? OnProgressChanged;
    void Start();
    void Stop();
}
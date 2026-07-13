namespace BetterLyrics.Core.Models.Memory;

/// <summary>
///     内存读取器的配置对象
/// </summary>
public class MemoryReaderConfig
{
    public string ProcessName { get; set; } = string.Empty;
    public bool Is64Bit { get; set; } = false;
    public MemoryAddressDefinition CurrentTime { get; set; } = new();
    public MemoryAddressDefinition TotalDuration { get; set; } = new();
}
using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Models.Memory;

/// <summary>
///     定义如何读取一个具体的数值（地址、偏移、类型）
/// </summary>
public class MemoryAddressDefinition
{
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    ///     模块基址的初始偏移量
    /// </summary>
    public int BaseOffset { get; set; }

    /// <summary>
    ///     多级指针偏移量列表。
    ///     <para>如果是直接读取（一级），留空或 null 即可。</para>
    ///     <para>如果是多级指针，按顺序填入偏移。逻辑为：读取指针->加偏移->读取指针...->加最终偏移->读取数值</para>
    /// </summary>
    public int[]? PointerOffsets { get; set; }

    /// <summary>
    ///     读取的数据类型
    /// </summary>
    public MemoryValueType ValueType { get; set; } = MemoryValueType.Double;

    /// <summary>
    ///     单位转换系数。例如：读取到的是毫秒(15000)，需要转为秒，则设为 0.001。默认为 1.0 (不转换)。
    /// </summary>
    public double UnitScale { get; set; } = 1.0;
}
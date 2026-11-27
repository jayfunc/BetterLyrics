namespace BetterLyrics.Core.Enums;

/// <summary>
/// 规则的匹配方式。
/// 枚举顺序同时被界面的下拉框使用，请勿随意调整。
/// </summary>
public enum MediaFieldRuleKind
{
    /// <summary>
    /// 模板：用 "{字段}" 描述文本结构，例如 "{artist} - {title}"。
    /// 命中后把各个槽位写回同名字段。
    /// </summary>
    Template = 0,

    /// <summary>
    /// 正则提取：用命名分组 (?&lt;artist&gt;...) 指定要写回的字段。
    /// </summary>
    RegexExtract = 1,

    /// <summary>
    /// 正则替换：对整个字段做替换，支持 $1 / ${name}。
    /// </summary>
    RegexReplace = 2
}

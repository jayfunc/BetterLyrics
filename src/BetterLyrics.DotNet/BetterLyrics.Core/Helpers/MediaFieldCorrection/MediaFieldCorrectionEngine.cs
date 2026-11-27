using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Helpers.MediaFieldCorrection;

/// <summary>
/// 纠正后的歌曲信息。
/// </summary>
public readonly record struct MediaFieldCorrectionResult(string? Title, string? Artist, string? Album);

/// <summary>
/// 执行用户写的字段纠正规则。
/// 支持两种写法：
/// 1. 模板："{artist} - {title}"，用占位符描述文本结构；
/// 2. 正则：命名分组提取，或整体替换。
/// </summary>
public static class MediaFieldCorrectionEngine
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

    private const RegexOptions _matchOptions =
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

    private static readonly ConcurrentDictionary<string, TemplateDefinition> _templateCache =
        new(StringComparer.Ordinal);

    private static readonly ConcurrentDictionary<string, Regex> _regexCache =
        new(StringComparer.Ordinal);

    /// <summary>
    /// 常见的同义符号族：用户只需要输入其中一种，程序自动兼容其余写法。
    /// </summary>
    private static readonly Dictionary<char, char[]> _characterFamilies = BuildCharacterFamilies();

    /// <summary>
    /// 依次执行启用的规则，返回纠正后的字段；任何单条规则出错都不会影响其他规则。
    /// </summary>
    public static MediaFieldCorrectionResult Apply(
        IEnumerable<MediaFieldMappingRule>? rules, string? title, string? artist, string? album)
    {
        if (rules == null) return new MediaFieldCorrectionResult(title, artist, album);

        // 规则集合可能在用户编辑时被修改（界面线程），这里先做快照；
        // 快照失败说明正好在修改，直接返回原始值，绝不影响播放流程。
        List<MediaFieldMappingRule> snapshot;
        try
        {
            snapshot = rules.ToList();
        }
        catch (InvalidOperationException)
        {
            return new MediaFieldCorrectionResult(title, artist, album);
        }

        var state = new FieldState(title, artist, album);

        foreach (var rule in snapshot)
        {
            ApplyRuleSafe(state, rule);
        }

        return new MediaFieldCorrectionResult(state.Title, state.Artist, state.Album);
    }

    /// <summary>
    /// 只执行一条规则（用于规则编辑器的实时试算）。
    /// </summary>
    public static MediaFieldCorrectionResult Apply(
        MediaFieldMappingRule? rule, string? title, string? artist, string? album)
    {
        var state = new FieldState(title, artist, album);
        ApplyRuleSafe(state, rule);
        return new MediaFieldCorrectionResult(state.Title, state.Artist, state.Album);
    }

    /// <summary>
    /// 校验图案能否使用；编辑界面据此提示用户。
    /// </summary>
    public static bool IsValidPattern(MediaFieldRuleKind kind, string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return false;

        if (kind == MediaFieldRuleKind.Template) return HasSlot(pattern);

        try
        {
            _ = new Regex(pattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);
        }
        catch (ArgumentException)
        {
            return false;
        }

        return true;
    }

    public static bool HasSlot(string? pattern) =>
        pattern != null && pattern.Contains('{') && pattern.Contains('}');

    private static void ApplyRuleSafe(FieldState state, MediaFieldMappingRule? rule)
    {
        if (rule == null || !rule.IsEnabled || string.IsNullOrWhiteSpace(rule.Pattern)) return;

        try
        {
            switch (rule.Kind)
            {
                case MediaFieldRuleKind.Template:
                    ApplyTemplate(state, rule);
                    break;
                case MediaFieldRuleKind.RegexExtract:
                    ApplyRegexExtract(state, rule);
                    break;
                case MediaFieldRuleKind.RegexReplace:
                    ApplyRegexReplace(state, rule);
                    break;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            // 用户写的图案匹配过久时放弃这一条规则
        }
        catch (ArgumentException)
        {
            // 非法图案（正则语法错误等）直接跳过
        }
    }

    private static void ApplyTemplate(FieldState state, MediaFieldMappingRule rule)
    {
        var value = state.Get(rule.Source);
        if (string.IsNullOrWhiteSpace(value)) return;

        var definition = _templateCache.GetOrAdd(rule.Pattern, static p => BuildTemplate(p));
        if (definition.Slots.Length == 0) return;

        var match = definition.Regex.Match(value);
        if (!match.Success) return;

        for (var i = 0; i < definition.Slots.Length; i++)
        {
            var field = definition.Slots[i];
            if (field is null) continue;

            var group = match.Groups["g" + i];
            if (!group.Success) continue;

            var text = group.Value.Trim();
            if (text.Length == 0) continue;

            state.Set(field.Value, text);
        }
    }

    private static void ApplyRegexExtract(FieldState state, MediaFieldMappingRule rule)
    {
        var value = state.Get(rule.Source);
        if (string.IsNullOrWhiteSpace(value)) return;

        var regex = _regexCache.GetOrAdd(rule.Pattern,
            static p => new Regex(p, _matchOptions, RegexTimeout));

        var match = regex.Match(value);
        if (!match.Success) return;

        var captured = false;

        foreach (var groupName in regex.GetGroupNames())
        {
            if (int.TryParse(groupName, out _)) continue;
            if (!TryGetField(groupName, out var field)) continue;

            var group = match.Groups[groupName];
            if (!group.Success) continue;

            var text = group.Value.Trim();
            if (text.Length == 0) continue;

            state.Set(field, text);
            captured = true;
        }

        // 没有命名分组时，退化为「取匹配到的那一段」
        if (captured) return;

        var matched = match.Value.Trim();
        if (matched.Length > 0) state.Set(rule.Source, matched);
    }

    private static void ApplyRegexReplace(FieldState state, MediaFieldMappingRule rule)
    {
        var value = state.Get(rule.Source);
        if (string.IsNullOrWhiteSpace(value)) return;

        var result = Regex.Replace(value, rule.Pattern, rule.Replacement ?? string.Empty,
            _matchOptions, RegexTimeout).Trim();

        // 不允许把字段清空，避免一条错误规则让歌曲信息消失
        if (result.Length > 0) state.Set(rule.Source, result);
    }

    /// <summary>
    /// 把模板转换为正则：占位符变成命名分组，最后一个占位符使用贪婪匹配。
    /// </summary>
    private static TemplateDefinition BuildTemplate(string template)
    {
        var parts = new List<(bool IsSlot, string Text, MediaField? Field)>();
        var index = 0;

        while (index < template.Length)
        {
            var c = template[index];

            if (c == '{')
            {
                var end = template.IndexOf('}', index + 1);
                if (end > index + 1)
                {
                    var name = template[(index + 1)..end].Trim();
                    parts.Add((true, name, TryGetField(name, out var resolved) ? resolved : null));
                    index = end + 1;
                    continue;
                }
            }

            if (c == '*')
            {
                // * 表示「任意剩余内容」，相当于正则里的 .*
                parts.Add((false, ".*", null));
                index++;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                while (index < template.Length && char.IsWhiteSpace(template[index])) index++;
                parts.Add((false, @"\s+", null));
                continue;
            }

            parts.Add((false, BuildLiteralToken(c), null));
            index++;
        }

        var builder = new StringBuilder("^");
        var slots = new List<MediaField?>();
        var slotCount = 0;

        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (!part.IsSlot)
            {
                builder.Append(part.Text);
                continue;
            }

            var groupName = "g" + slotCount++;
            slots.Add(part.Field);

            // 末尾的占位符负责吃掉剩余内容
            builder.Append(i == parts.Count - 1
                ? $"(?<{groupName}>.+)"
                : $"(?<{groupName}>.+?)");
        }

        builder.Append('$');

        return new TemplateDefinition(
            new Regex(builder.ToString(), _matchOptions, RegexTimeout), [.. slots]);
    }

    private static string BuildLiteralToken(char c)
    {
        if (_characterFamilies.TryGetValue(c, out var family))
        {
            var builder = new StringBuilder("[");
            foreach (var item in family) builder.Append(Regex.Escape(item.ToString()));
            builder.Append(']');
            return builder.ToString();
        }

        return Regex.Escape(c.ToString());
    }

    /// <summary>
    /// 占位符名称：支持中英文写法，未识别的名称表示丢弃该部分。
    /// </summary>
    private static bool TryGetField(string? name, out MediaField field)
    {
        field = default;
        if (string.IsNullOrWhiteSpace(name)) return false;

        switch (name.Trim().ToLowerInvariant())
        {
            case "title" or "name" or "song" or "标题" or "歌曲" or "歌曲名" or "曲名":
                field = MediaField.Title;
                return true;
            case "artist" or "artists" or "歌手" or "艺人" or "演出者":
                field = MediaField.Artist;
                return true;
            case "album" or "专辑" or "專輯":
                field = MediaField.Album;
                return true;
            default:
                return false;
        }
    }

    private static Dictionary<char, char[]> BuildCharacterFamilies()
    {
        var map = new Dictionary<char, char[]>();

        // 短横线输入时兼容所有横线写法；长破折号输入时只匹配长破折号，
        // 避免 Apple Music 的「歌手 — 专辑 - Single」被 "-" 二次拆开
        char[] hyphenLike = ['-', '‐', '‑', '−'];
        char[] longDashLike = ['—', '–', '－'];
        var allDashes = hyphenLike.Concat(longDashLike).ToArray();

        foreach (var character in hyphenLike) map[character] = allDashes;
        foreach (var character in longDashLike) map[character] = longDashLike;

        string[] groups =
        [
            "/／⁄",
            "|｜¦",
            ":：",
            ";；",
            ",，、",
            "·・•‧",
            "。．",
            "~～",
            "&＆",
            "+＋",
            "×✕"
        ];

        foreach (var group in groups)
        {
            var characters = group.ToCharArray();
            foreach (var character in characters)
            {
                map[character] = characters;
            }
        }

        return map;
    }

    private sealed record TemplateDefinition(Regex Regex, MediaField?[] Slots);

    private sealed class FieldState(string? title, string? artist, string? album)
    {
        public string? Title { get; private set; } = title;
        public string? Artist { get; private set; } = artist;
        public string? Album { get; private set; } = album;

        public string? Get(MediaField field) => field switch
        {
            MediaField.Title => Title,
            MediaField.Artist => Artist,
            MediaField.Album => Album,
            _ => null
        };

        public void Set(MediaField field, string? value)
        {
            switch (field)
            {
                case MediaField.Title:
                    Title = value;
                    break;
                case MediaField.Artist:
                    Artist = value;
                    break;
                case MediaField.Album:
                    Album = value;
                    break;
            }
        }
    }
}

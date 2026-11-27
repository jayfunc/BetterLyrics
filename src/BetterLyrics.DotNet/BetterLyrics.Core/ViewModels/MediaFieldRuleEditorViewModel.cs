using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers.MediaFieldCorrection;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// 规则编辑器的展示模型。
/// 编辑的是一份副本，点击确定后才会写回；图案变化时用示例数据实时试算。
/// </summary>
public partial class MediaFieldRuleEditorViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;
    private readonly MediaFieldCorrectionResult _sample;

    public MediaFieldMappingRule Rule { get; }

    public MediaFieldRuleEditorViewModel(MediaFieldMappingRule? rule,
        ILocalizationService localizationService, MediaFieldCorrectionResult sample)
    {
        _localizationService = localizationService;
        _sample = sample;

        Rule = rule?.Clone() ?? new MediaFieldMappingRule();
        Rule.PropertyChanged += (_, _) => RaiseComputedProperties();

        UpdatePreview();
    }

    public string Summary => MediaFieldRuleDescriber.Describe(Rule, _localizationService);

    public int KindIndex
    {
        get => (int)Rule.Kind;
        set
        {
            if (value < 0 || value == (int)Rule.Kind) return;
            Rule.Kind = (MediaFieldRuleKind)value;
            OnPropertyChanged();
        }
    }

    public int SourceIndex
    {
        get => (int)Rule.Source;
        set
        {
            if (value < 0 || value == (int)Rule.Source) return;
            Rule.Source = (MediaField)value;
            OnPropertyChanged();
        }
    }

    public string Pattern
    {
        get => Rule.Pattern;
        set
        {
            if (Rule.Pattern == value) return;
            Rule.Pattern = value;
            OnPropertyChanged();
        }
    }

    public string Replacement
    {
        get => Rule.Replacement;
        set
        {
            if (Rule.Replacement == value) return;
            Rule.Replacement = value;
            OnPropertyChanged();
        }
    }

    public bool NeedsReplacement => Rule.Kind == MediaFieldRuleKind.RegexReplace;

    public string PatternPlaceholder => Rule.Kind switch
    {
        MediaFieldRuleKind.Template => "{artist} - {title}",
        MediaFieldRuleKind.RegexExtract => @"(?<artist>.+?) - (?<title>.+)",
        _ => @"\s*-\s*(?:Single|EP)$"
    };

    public string Hint => Rule.Kind switch
    {
        MediaFieldRuleKind.Template => _localizationService.GetLocalizedString("MediaFieldRuleHintTemplate"),
        MediaFieldRuleKind.RegexExtract => _localizationService.GetLocalizedString("MediaFieldRuleHintRegexExtract"),
        _ => _localizationService.GetLocalizedString("MediaFieldRuleHintRegexReplace")
    };

    public bool CanSave => MediaFieldCorrectionEngine.IsValidPattern(Rule.Kind, Rule.Pattern);

    public bool HasValidationError => !CanSave && !string.IsNullOrEmpty(Rule.Pattern);

    /// <summary>
    /// 用示例数据实时试算，格式：字段: 原值 → 结果。
    /// </summary>
    public string PreviewText { get; private set; } = string.Empty;

    private void UpdatePreview()
    {
        var result = MediaFieldCorrectionEngine.Apply(
            Rule, _sample.Title, _sample.Artist, _sample.Album);

        var rows = new List<string>();
        AppendRow(rows, MediaField.Title, _sample.Title, result.Title);
        AppendRow(rows, MediaField.Artist, _sample.Artist, result.Artist);
        AppendRow(rows, MediaField.Album, _sample.Album, result.Album);

        PreviewText = rows.Count > 0
            ? string.Join(Environment.NewLine, rows)
            : _localizationService.GetLocalizedString("MediaFieldPreviewNoChange");
    }

    private void AppendRow(List<string> rows, MediaField field, string? raw, string? corrected)
    {
        if (string.Equals(raw ?? string.Empty, corrected ?? string.Empty, StringComparison.Ordinal)) return;

        var label = MediaFieldRuleDescriber.GetFieldName(field, _localizationService);
        rows.Add($"{label}: {raw} → {corrected}");
    }

    private void RaiseComputedProperties()
    {
        UpdatePreview();

        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(NeedsReplacement));
        OnPropertyChanged(nameof(PatternPlaceholder));
        OnPropertyChanged(nameof(Hint));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(HasValidationError));
        OnPropertyChanged(nameof(PreviewText));
    }
}

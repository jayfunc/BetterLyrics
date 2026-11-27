using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers.MediaFieldCorrection;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// 规则列表中的一项：显示图案原文 + 一句概括。
/// </summary>
public partial class MediaFieldRuleItemViewModel : ObservableObject
{
    private readonly ILocalizationService _localizationService;

    public MediaFieldMappingRule Rule { get; }

    public MediaFieldRuleItemViewModel(MediaFieldMappingRule rule, ILocalizationService localizationService)
    {
        Rule = rule;
        _localizationService = localizationService;

        Rule.PropertyChanged += Rule_PropertyChanged;
    }

    /// <summary>
    /// 图案原文：模板直接显示，正则用 /pattern/ 或 s/pattern/replacement/ 的写法。
    /// </summary>
    public string PatternText => Rule.Kind switch
    {
        MediaFieldRuleKind.Template => Rule.Pattern,
        MediaFieldRuleKind.RegexExtract => $"/{Rule.Pattern}/",
        _ => $"s/{Rule.Pattern}/{Rule.Replacement}/"
    };

    public string Summary => MediaFieldRuleDescriber.Describe(Rule, _localizationService);

    public bool IsEnabled
    {
        get => Rule.IsEnabled;
        set
        {
            if (Rule.IsEnabled == value) return;
            Rule.IsEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 停止监听规则变化，避免列表重建后旧对象无法释放。
    /// </summary>
    public void Detach()
    {
        Rule.PropertyChanged -= Rule_PropertyChanged;
    }

    private void Rule_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(PatternText));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(IsEnabled));
    }
}

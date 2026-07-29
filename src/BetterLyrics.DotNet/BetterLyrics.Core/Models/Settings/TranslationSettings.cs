using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class TranslationSettings : ObservableRecipient
{
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsLibreTranslateEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial string LibreTranslateServer { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsTranslationEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial string SelectedTargetLanguageCode { get; set; } =
        LanguageHelper.GetDefaultTargetTranslationLanguageCode();

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsMandarinRomanizationEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsCantoneseRomanizationEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsJapaneseRomanizationEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsKoreanRomanizationEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial ChineseConversion ChineseConversion { get; set; } = ChineseConversion.Unspecified;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsFilterEnabled { get; set; } = false;
}
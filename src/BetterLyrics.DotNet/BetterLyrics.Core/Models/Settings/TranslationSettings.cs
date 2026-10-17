using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Messages.AppSettings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace BetterLyrics.Core.Models.Settings;

public partial class TranslationSettings : ObservableObject
{
    [ObservableProperty]
    public partial bool IsLibreTranslateEnabled { get; set; } = false;

    [ObservableProperty]
    public partial string LibreTranslateServer { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsTranslationEnabled { get; set; } = true;

    [ObservableProperty]
    public partial string SelectedTargetLanguageCode { get; set; } =
        LanguageHelper.GetDefaultTargetTranslationLanguageCode();

    [ObservableProperty]
    public partial bool IsMandarinRomanizationEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsCantoneseRomanizationEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsJapaneseRomanizationEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsKoreanRomanizationEnabled { get; set; } = false;

    [ObservableProperty]
    public partial ChineseConversion ChineseConversion { get; set; } = ChineseConversion.Unspecified;

    [ObservableProperty]
    public partial bool IsFilterEnabled { get; set; } = false;

    partial void OnIsLibreTranslateEnabledChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new TranslationSettingsChangedMessage(nameof(IsLibreTranslateEnabled)));

    partial void OnLibreTranslateServerChanged(string value) =>
        WeakReferenceMessenger.Default.Send(new TranslationSettingsChangedMessage(nameof(LibreTranslateServer)));

    partial void OnIsTranslationEnabledChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new TranslationSettingsChangedMessage(nameof(IsTranslationEnabled)));

    partial void OnSelectedTargetLanguageCodeChanged(string value) =>
        WeakReferenceMessenger.Default.Send(new TranslationSettingsChangedMessage(nameof(SelectedTargetLanguageCode)));

    partial void OnIsMandarinRomanizationEnabledChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new TranslationSettingsChangedMessage(nameof(IsMandarinRomanizationEnabled)));

    partial void OnIsCantoneseRomanizationEnabledChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new TranslationSettingsChangedMessage(nameof(IsCantoneseRomanizationEnabled)));

    partial void OnIsJapaneseRomanizationEnabledChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new TranslationSettingsChangedMessage(nameof(IsJapaneseRomanizationEnabled)));

    partial void OnIsKoreanRomanizationEnabledChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new TranslationSettingsChangedMessage(nameof(IsKoreanRomanizationEnabled)));

    partial void OnChineseConversionChanged(ChineseConversion value) =>
        WeakReferenceMessenger.Default.Send(new TranslationSettingsChangedMessage(nameof(ChineseConversion)));

    partial void OnIsFilterEnabledChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new TranslationSettingsChangedMessage(nameof(IsFilterEnabled)));
}
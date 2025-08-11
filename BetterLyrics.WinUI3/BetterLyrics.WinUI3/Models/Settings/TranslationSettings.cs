using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class TranslationSettings : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLibreTranslateEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string LibreTranslateServer { get; set; } = string.Empty;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsTranslationEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool ShowTranslationOnly { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int SelectedTargetLanguageIndex { get; set; } = LanguageHelper.GetDefaultTargetLanguageIndex();

        public TranslationSettings() { }
    }
}

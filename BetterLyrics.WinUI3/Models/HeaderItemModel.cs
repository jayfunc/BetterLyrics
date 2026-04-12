using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models
{
    public partial class HeaderItemModel : ObservableObject
    {
        public int Index { get; set; }
        public string Definition { get; set; }

        public bool CanDelete { get; set; }

        public bool IsAuto => Definition.Equals("Auto", StringComparison.OrdinalIgnoreCase);
        public string ToggleText
        {
            get
            {
                var localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();
                return IsAuto ? localizationService.GetLocalizedString("SetTo1Star") : localizationService.GetLocalizedString("SetToAuto");
            }
        }
        public string ToggleIcon => IsAuto ? "\uE71A" : "\uE743";

        public LayoutEditorControlViewModel Parent { get; set; }

        public double BaseSize { get; set; }
        [ObservableProperty] public partial double ItemSize { get; set; } = 40;
    }
}

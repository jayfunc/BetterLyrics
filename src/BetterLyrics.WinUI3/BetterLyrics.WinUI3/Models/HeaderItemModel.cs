using System;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.WinUI3.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.WinUI3.Models;

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
            return IsAuto
                ? localizationService.GetLocalizedString("SetTo1Star")
                : localizationService.GetLocalizedString("SetToAuto");
        }
    }

    public string ToggleIcon => IsAuto ? "\uE71A" : "\uE743";

    public LayoutEditorControl Parent { get; set; }

    public double BaseSize { get; set; }
    [ObservableProperty] public partial double ItemSize { get; set; } = 40;
    [ObservableProperty] public partial double FollowingSpacing { get; set; }
}
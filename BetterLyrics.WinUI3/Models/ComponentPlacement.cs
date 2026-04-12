using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Services.LocalizationService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BetterLyrics.WinUI3.Models
{
    public partial class ComponentPlacement : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial ComponentType ComponentType { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int Row { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int Column { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int RowSpan { get; set; } = 1;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int ColumnSpan { get; set; } = 1;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double MarginLeft { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double MarginTop { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double MarginRight { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double MarginBottom { get; set; } = 0;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Stretch;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Stretch;

        [JsonIgnore] public string DisplayName => ComponentType.GetDisplayName();
        [JsonIgnore] public SolidColorBrush ColorBrush => ComponentType.GetSolidColorBrush();
    }
}

using System.Text.Json.Serialization;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Models.Domain;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models;

public partial class ComponentPlacement : ObservableRecipient, ICloneable
{
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial ComponentType ComponentType { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int Row { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int Column { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int RowSpan { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int ColumnSpan { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial double MarginLeft { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial double MarginTop { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial double MarginRight { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial double MarginBottom { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial AppHorizontalAlignment HorizontalAlignment { get; set; } = AppHorizontalAlignment.Stretch;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial AppVerticalAlignment VerticalAlignment { get; set; } = AppVerticalAlignment.Stretch;

    [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)]
    [ObservableProperty]
    public partial double Width { get; set; } = double.NaN;

    [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)]
    [ObservableProperty]
    public partial double Height { get; set; } = double.NaN;

    [JsonIgnore] public string DisplayName => ComponentType.GetDisplayName();

    public object Clone()
    {
        return new ComponentPlacement
        {
            ComponentType = ComponentType,

            Row = Row,
            Column = Column,
            RowSpan = RowSpan,
            ColumnSpan = ColumnSpan,

            MarginLeft = MarginLeft,
            MarginTop = MarginTop,
            MarginRight = MarginRight,
            MarginBottom = MarginBottom,

            HorizontalAlignment = HorizontalAlignment,
            VerticalAlignment = VerticalAlignment,

            Width = Width,
            Height = Height
        };
    }
}
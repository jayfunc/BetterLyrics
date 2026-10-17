using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Models.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace BetterLyrics.Core.Models;

public partial class ComponentPlacement : ObservableObject, ICloneable
{
    [ObservableProperty]
    public partial ComponentType ComponentType { get; set; }

    [ObservableProperty]
    public partial int Row { get; set; } = 0;

    [ObservableProperty]
    public partial int Column { get; set; } = 0;

    [ObservableProperty]
    public partial int RowSpan { get; set; } = 1;

    [ObservableProperty]
    public partial int ColumnSpan { get; set; } = 1;

    [ObservableProperty]
    public partial double MarginLeft { get; set; } = 0;

    [ObservableProperty]
    public partial double MarginTop { get; set; } = 0;

    [ObservableProperty]
    public partial double MarginRight { get; set; } = 0;

    [ObservableProperty]
    public partial double MarginBottom { get; set; } = 0;

    [ObservableProperty]
    public partial AppHorizontalAlignment HorizontalAlignment { get; set; } = AppHorizontalAlignment.Stretch;

    [ObservableProperty]
    public partial AppVerticalAlignment VerticalAlignment { get; set; } = AppVerticalAlignment.Stretch;

    [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)]
    [ObservableProperty]
    public partial double Width { get; set; } = double.NaN;

    [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)]
    [ObservableProperty]
    public partial double Height { get; set; } = double.NaN;

    [ObservableProperty]
    public partial double PaddingLeft { get; set; } = 0;

    [ObservableProperty]
    public partial double PaddingTop { get; set; } = 0;

    [ObservableProperty]
    public partial double PaddingRight { get; set; } = 0;

    [ObservableProperty]
    public partial double PaddingBottom { get; set; } = 0;

    [JsonIgnore] public string DisplayName => ComponentType.GetDisplayName();

    partial void OnComponentTypeChanged(ComponentType value)
    {
        if (value == ComponentType.PlaybackControl)
        {
            PaddingLeft = 16;
            PaddingTop = 16;
            PaddingRight = 16;
            PaddingBottom = 16;
        }
    }

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

            PaddingLeft = PaddingLeft,
            PaddingTop = PaddingTop,
            PaddingRight = PaddingRight,
            PaddingBottom = PaddingBottom,

            HorizontalAlignment = HorizontalAlignment,
            VerticalAlignment = VerticalAlignment,

            Width = Width,
            Height = Height
        };
    }
}
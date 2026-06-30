using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Models.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace BetterLyrics.Core.Models
{
    public partial class ComponentPlacement : ObservableRecipient, ICloneable
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial ComponentType ComponentType { get; set; }
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int Row { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int Column { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int RowSpan { get; set; } = 1;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int ColumnSpan { get; set; } = 1;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double MarginLeft { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double MarginTop { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double MarginRight { get; set; } = 0;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial double MarginBottom { get; set; } = 0;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial AppHorizontalAlignment HorizontalAlignment { get; set; } = AppHorizontalAlignment.Stretch;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial AppVerticalAlignment VerticalAlignment { get; set; } = AppVerticalAlignment.Stretch;

        [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)][ObservableProperty] public partial double Width { get; set; } = double.NaN;
        [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)][ObservableProperty] public partial double Height { get; set; } = double.NaN;

        [JsonIgnore] public string DisplayName => ComponentType.GetDisplayName();

        public object Clone()
        {
            return new ComponentPlacement
            {
                ComponentType = this.ComponentType,

                Row = this.Row,
                Column = this.Column,
                RowSpan = this.RowSpan,
                ColumnSpan = this.ColumnSpan,

                MarginLeft = this.MarginLeft,
                MarginTop = this.MarginTop,
                MarginRight = this.MarginRight,
                MarginBottom = this.MarginBottom,

                HorizontalAlignment = this.HorizontalAlignment,
                VerticalAlignment = this.VerticalAlignment,

                Width = this.Width,
                Height = this.Height,
            };
        }
    }
}

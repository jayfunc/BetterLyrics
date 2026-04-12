using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class CanvasItemControl : UserControl
    {
        public ComponentPlacement Placement { get; }

        public CanvasItemControl(ComponentPlacement placement, bool isSelected)
        {
            this.InitializeComponent();
            Placement = placement;

            TitleBlock.Text = placement.DisplayName;
            MainBorder.Background = placement.ComponentType.GetSolidColorBrush();
            MainBorder.Tag = placement;

            if (isSelected)
            {
                MainBorder.BorderBrush = new SolidColorBrush(ColorHelper.GetHarmoniousColor(Placement.ComponentType.GetSolidColorBrush().Color));
                MainBorder.BorderThickness = new Thickness(3);
                MainBorder.Opacity = 1.0;

                RightHandle.Visibility = Visibility.Visible;
                BottomHandle.Visibility = Visibility.Visible;
                CornerHandle.Visibility = Visibility.Visible;
            }
            else
            {
                MainBorder.BorderBrush = null;
                MainBorder.BorderThickness = new Thickness(0);
                MainBorder.Opacity = 0.6;

                RightHandle.Visibility = Visibility.Collapsed;
                BottomHandle.Visibility = Visibility.Collapsed;
                CornerHandle.Visibility = Visibility.Collapsed;
            }
        }
    }
}
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.LocalizationService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class CanvasItemControl : UserControl
    {
        private readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

        public ComponentPlacement Placement { get; }

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    UpdateSelectionVisuals();
                }
            }
        }

        public CanvasItemControl(ComponentPlacement placement, bool isSelected)
        {
            this.InitializeComponent();
            Placement = placement;

            MainBorder.Tag = placement;

            MainBorder.HorizontalAlignment = placement.HorizontalAlignment;
            MainBorder.VerticalAlignment = placement.VerticalAlignment;
            MainBorder.Margin = new Thickness(placement.MarginLeft, placement.MarginTop, placement.MarginRight, placement.MarginBottom);

            MainBorder.Width = placement.Width;
            MainBorder.Height = placement.Height;

            var mockupGrid = new Grid
            {
                HorizontalAlignment = placement.HorizontalAlignment,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            mockupGrid.Children.Add(MockupHelper.GenerateMockupContent(this, placement.ComponentType, placement.HorizontalAlignment, placement.DisplayName));
            ContentHost.Children.Add(mockupGrid);

            _isSelected = isSelected;
            UpdateSelectionVisuals();
        }

        private void UpdateSelectionVisuals()
        {
            Color accentColor = Colors.DodgerBlue;
            if (Resources.TryGetValue("SystemAccentColor", out var resColor) && resColor is Color c)
            {
                accentColor = c;
            }
            var highlightBrush = new SolidColorBrush(accentColor);
            var defaultBorderBrush = BrushHelper.GetThemeBrush(this, "CardStrokeColorDefaultBrush");

            if (_isSelected)
            {
                MainBorder.BorderBrush = highlightBrush;
                MainBorder.Opacity = 1.0;
                SpanHighlightBorder.Stroke = highlightBrush;
                SpanHighlightBorder.Fill = new SolidColorBrush(accentColor) { Opacity = 0.1 };
                SpanHighlightBorder.Visibility = Visibility.Visible;
                RightHandle.Visibility = Visibility.Visible;
                BottomHandle.Visibility = Visibility.Visible;
                CornerHandle.Visibility = Visibility.Visible;
                RightHandle.BorderBrush = highlightBrush;
                BottomHandle.BorderBrush = highlightBrush;
                CornerHandle.BorderBrush = highlightBrush;
            }
            else
            {
                MainBorder.BorderBrush = defaultBorderBrush;
                MainBorder.Opacity = 0.8;
                SpanHighlightBorder.Visibility = Visibility.Collapsed;
                RightHandle.Visibility = Visibility.Collapsed;
                BottomHandle.Visibility = Visibility.Collapsed;
                CornerHandle.Visibility = Visibility.Collapsed;
            }
        }
    }
}
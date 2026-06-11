using BetterLyrics.WinUI3.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    [ContentProperty(Name = "PanelContent")]
    public sealed partial class FloatSidePanel : UserControl
    {
        public static readonly DependencyProperty PanelContentProperty =
            DependencyProperty.Register(nameof(PanelContent), typeof(object), typeof(FloatSidePanel), new PropertyMetadata(null));

        public object PanelContent
        {
            get => GetValue(PanelContentProperty);
            set => SetValue(PanelContentProperty, value);
        }

        public static readonly DependencyProperty PlacementProperty =
            DependencyProperty.Register(
                nameof(Placement),
                typeof(SidePanelPlacement),
                typeof(FloatSidePanel),
                new PropertyMetadata(SidePanelPlacement.Bottom, OnPlacementChanged));

        public SidePanelPlacement Placement
        {
            get => (SidePanelPlacement)GetValue(PlacementProperty);
            set => SetValue(PlacementProperty, value);
        }

        public FloatSidePanel()
        {
            this.InitializeComponent();
        }

        private static void OnPlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FloatSidePanel panel && e.NewValue is SidePanelPlacement placement)
            {
                panel.PanelGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
                panel.PanelGrid.VerticalAlignment = VerticalAlignment.Stretch;

                switch (placement)
                {
                    case SidePanelPlacement.Right:
                        panel.PanelGrid.HorizontalAlignment = HorizontalAlignment.Right;
                        panel.PanelGrid.BorderThickness = new Thickness(1, 0, 0, 0);
                        break;
                    case SidePanelPlacement.Left:
                        panel.PanelGrid.HorizontalAlignment = HorizontalAlignment.Left;
                        panel.PanelGrid.BorderThickness = new Thickness(0, 0, 1, 0);
                        break;
                    case SidePanelPlacement.Top:
                        panel.PanelGrid.VerticalAlignment = VerticalAlignment.Top;
                        panel.PanelGrid.BorderThickness = new Thickness(0, 0, 0, 1);
                        break;
                    case SidePanelPlacement.Bottom:
                        panel.PanelGrid.VerticalAlignment = VerticalAlignment.Bottom;
                        panel.PanelGrid.BorderThickness = new Thickness(0, 1, 0, 0);
                        break;
                }
            }
        }

        public void Show()
        {
            RootContainer.Visibility = Visibility.Visible;
            PanelGrid.UpdateLayout();

            double width = PanelGrid.ActualWidth;
            double height = PanelGrid.ActualHeight;

            PanelTransform.X = 0;
            PanelTransform.Y = 0;

            switch (Placement)
            {
                case SidePanelPlacement.Right: PanelTransform.X = width; break;
                case SidePanelPlacement.Left: PanelTransform.X = -width; break;
                case SidePanelPlacement.Bottom: PanelTransform.Y = height; break;
                case SidePanelPlacement.Top: PanelTransform.Y = -height; break;
            }

            EnterStoryboard.Begin();
        }

        public void Hide()
        {
            double width = PanelGrid.ActualWidth;
            double height = PanelGrid.ActualHeight;

            ExitSlideAnimationX.To = 0;
            ExitSlideAnimationY.To = 0;

            switch (Placement)
            {
                case SidePanelPlacement.Right: ExitSlideAnimationX.To = width; break;
                case SidePanelPlacement.Left: ExitSlideAnimationX.To = -width; break;
                case SidePanelPlacement.Bottom: ExitSlideAnimationY.To = height; break;
                case SidePanelPlacement.Top: ExitSlideAnimationY.To = -height; break;
            }

            ExitStoryboard.Begin();
        }

        private void ExitStoryboard_Completed(object sender, object e)
        {
            RootContainer.Visibility = Visibility.Collapsed;
        }

        private void Mask_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Hide();
        }
    }
}

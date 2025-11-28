using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Controls
{
    public class DragDeltaEventArgs : EventArgs
    {
        public double HorizontalChange { get; }
        public double VerticalChange { get; }

        public DragDeltaEventArgs(double hChange, double vChange)
        {
            HorizontalChange = hChange;
            VerticalChange = vChange;
        }
    }

    public sealed partial class Dragger : UserControl
    {
        public event EventHandler DragStarted;
        public event EventHandler<DragDeltaEventArgs> DragDelta;
        public event EventHandler DragCompleted;

        private bool _isDragging = false;
        private Point _lastPoint;

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(Dragger),
                new PropertyMetadata(Orientation.Vertical, OnOrientationChanged));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (Dragger)d;
            control.UpdateVisuals();
        }

        public Dragger()
        {
            this.InitializeComponent();
            this.Loaded += (s, e) => UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (Orientation == Orientation.Vertical)
            {
                this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);

                this.Width = 16;
                this.Height = double.NaN; // Auto

                if (HitArea != null && HandlePill != null)
                {
                    HitArea.Width = 16;
                    HitArea.Height = double.NaN;

                    HandlePill.Width = 8;
                    HandlePill.Height = 32;
                }
            }
            else
            {
                this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);

                this.Height = 16;
                this.Width = double.NaN; // Auto

                if (HitArea != null && HandlePill != null)
                {
                    HitArea.Height = 16;
                    HitArea.Width = double.NaN;

                    HandlePill.Height = 8;
                    HandlePill.Width = 32;
                }
            }
        }

        private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
        }

        private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
        }

        private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var element = sender as UIElement;
            if (element.CapturePointer(e.Pointer))
            {
                _isDragging = true;
                _lastPoint = e.GetCurrentPoint(this.XamlRoot.Content).Position;
                DragStarted?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Grid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                var currentPoint = e.GetCurrentPoint(this.XamlRoot.Content).Position;
                double dx = currentPoint.X - _lastPoint.X;
                double dy = currentPoint.Y - _lastPoint.Y;

                if (dx != 0 || dy != 0)
                {
                    DragDelta?.Invoke(this, new DragDeltaEventArgs(dx, dy));
                    _lastPoint = currentPoint;
                }
            }
        }

        private void Grid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                var element = sender as UIElement;
                _isDragging = false;
                element.ReleasePointerCapture(e.Pointer);
                DragCompleted?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
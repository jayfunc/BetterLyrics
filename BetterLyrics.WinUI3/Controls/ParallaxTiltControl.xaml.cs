using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    [ContentProperty(Name = "Child")]
    public sealed partial class ParallaxTiltControl : UserControl
    {
        public static readonly DependencyProperty ChildProperty =
            DependencyProperty.Register("Child", typeof(UIElement), typeof(ParallaxTiltControl), new PropertyMetadata(null));

        public UIElement Child
        {
            get => (UIElement)GetValue(ChildProperty);
            set => SetValue(ChildProperty, value);
        }

        // 最大倾斜角度（3D）
        public static readonly DependencyProperty MaxTiltAngleProperty =
            DependencyProperty.Register("MaxTiltAngle", typeof(double), typeof(ParallaxTiltControl), new PropertyMetadata(15.0));

        public double MaxTiltAngle
        {
            get => (double)GetValue(MaxTiltAngleProperty);
            set => SetValue(MaxTiltAngleProperty, value);
        }

        // 视差位移距离（浮动效果）
        public static readonly DependencyProperty ParallaxDepthProperty =
            DependencyProperty.Register("ParallaxDepth", typeof(double), typeof(ParallaxTiltControl), new PropertyMetadata(10.0));

        public double ParallaxDepth
        {
            get => (double)GetValue(ParallaxDepthProperty);
            set => SetValue(ParallaxDepthProperty, value);
        }

        private double _targetRotationX = 0;
        private double _targetRotationY = 0;
        private double _targetTranslateX = 0;
        private double _targetTranslateY = 0;

        private double _currentRotationX = 0;
        private double _currentRotationY = 0;
        private double _currentTranslateX = 0;
        private double _currentTranslateY = 0;

        private bool _isPointerInside = false;

        public ParallaxTiltControl()
        {
            this.InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering += OnRendering;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
        }

        private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _isPointerInside = true;
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isPointerInside) return;

            var pointerPos = e.GetCurrentPoint(RootGrid).Position;
            var width = RootGrid.ActualWidth;
            var height = RootGrid.ActualHeight;

            if (width == 0 || height == 0) return;

            // 计算归一化坐标（-1 到 1）
            // 左上角为（-1, -1），右下角为（1, 1），中心点为（0, 0）
            double normalizedX = (pointerPos.X - width / 2) / (width / 2);
            double normalizedY = (pointerPos.Y - height / 2) / (height / 2);

            // 鼠标压向哪一侧，哪一侧就向屏幕内部倾斜
            _targetRotationY = -normalizedX * MaxTiltAngle;
            _targetRotationX = normalizedY * MaxTiltAngle;

            // 内部元素反向轻微移动，营造悬浮视差感
            _targetTranslateX = -normalizedX * ParallaxDepth;
            _targetTranslateY = -normalizedY * ParallaxDepth;
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isPointerInside = false;

            // 鼠标离开时恢复原位
            _targetRotationX = 0;
            _targetRotationY = 0;
            _targetTranslateX = 0;
            _targetTranslateY = 0;
        }

        private void OnRendering(object? sender, object e)
        {
            // Lerp（线性插值）因子：数值越小越平滑但跟随越慢，数值越大越敏捷
            const double lerpFactor = 0.15;

            // 只有当存在数值差异时才进行计算，节省性能
            if (Math.Abs(_targetRotationX - _currentRotationX) > 0.01 ||
                Math.Abs(_targetRotationY - _currentRotationY) > 0.01)
            {
                _currentRotationX += (_targetRotationX - _currentRotationX) * lerpFactor;
                _currentRotationY += (_targetRotationY - _currentRotationY) * lerpFactor;
                _currentTranslateX += (_targetTranslateX - _currentTranslateX) * lerpFactor;
                _currentTranslateY += (_targetTranslateY - _currentTranslateY) * lerpFactor;

                // 应用到 XAML 节点
                TiltProjection.RotationX = _currentRotationX;
                TiltProjection.RotationY = _currentRotationY;
                ParallaxTransform.TranslateX = _currentTranslateX;
                ParallaxTransform.TranslateY = _currentTranslateY;
            }
        }
    }
}

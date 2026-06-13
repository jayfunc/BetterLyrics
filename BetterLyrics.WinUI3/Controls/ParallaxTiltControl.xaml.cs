using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using System;
using BetterLyrics.WinUI3.Effects;

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

        public static readonly DependencyProperty IsParallaxEnabledProperty =
            DependencyProperty.Register("IsParallaxEnabled", typeof(bool), typeof(ParallaxTiltControl), new PropertyMetadata(true));

        public bool IsParallaxEnabled
        {
            get => (bool)GetValue(IsParallaxEnabledProperty);
            set => SetValue(IsParallaxEnabledProperty, value);
        }

        public ParallaxTiltEffect? ParallaxContext { get; set; }

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

        private void OnRendering(object? sender, object e)
        {
            if (ParallaxContext == null) return;

            if (IsParallaxEnabled)
            {
                TiltProjection.RotationX = ParallaxContext.CurrentRotationX;
                TiltProjection.RotationY = ParallaxContext.CurrentRotationY;
                ParallaxTransform.TranslateX = ParallaxContext.CurrentTranslateX;
                ParallaxTransform.TranslateY = ParallaxContext.CurrentTranslateY;
            }
            else
            {
                if (TiltProjection.RotationX != 0 || ParallaxTransform.TranslateX != 0)
                {
                    TiltProjection.RotationX = 0;
                    TiltProjection.RotationY = 0;
                    ParallaxTransform.TranslateX = 0;
                    ParallaxTransform.TranslateY = 0;
                }
            }
        }
    }
}
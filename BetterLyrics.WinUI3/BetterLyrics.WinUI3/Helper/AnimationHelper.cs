using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace BetterLyrics.WinUI3.Helper {

    /// <summary>
    /// Edited based on: https://stackoverflow.com/a/25236507/11048731
    /// </summary>
    public class AnimationHelper : DependencyObject {
        public static int GetAnimationDuration(DependencyObject obj) {
            return (int)obj.GetValue(AnimationDurationProperty);
        }

        public static void SetAnimationDuration(DependencyObject obj, int value) {
            obj.SetValue(AnimationDurationProperty, value);
        }

        // Using a DependencyProperty as the backing store for AnimationDuration.
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.RegisterAttached("AnimationDuration", typeof(int),
            typeof(AnimationHelper), new PropertyMetadata(0,
            OnAnimationDurationChanged));

        private static void OnAnimationDurationChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e) {
            FrameworkElement element = d as FrameworkElement;

            var ms = (int)e.NewValue;

            if (ms < 0) return;

            var key = "LyricsLineCharGradientInTextBlock";
            foreach (var timeline in (element.Resources[key] as Storyboard).Children) {
                foreach (var keyFrame in (timeline as DoubleAnimationUsingKeyFrames).KeyFrames) {
                    (keyFrame as LinearDoubleKeyFrame).KeyTime =
                        KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(ms));
                }
            }
        }
    }

}

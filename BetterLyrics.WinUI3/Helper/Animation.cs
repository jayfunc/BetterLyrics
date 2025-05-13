using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public class Animation
    {
        public static async Task Fade(UIElement textBlock, double to, int duration = 300)
        {
            // Fade out animation
            var fade = new DoubleAnimation
            {
                To = to,
                Duration = TimeSpan.FromMilliseconds(duration),
                EnableDependentAnimation = true
            };
            var storyboard = new Storyboard();
            Storyboard.SetTarget(fade, textBlock);
            Storyboard.SetTargetProperty(fade, "Opacity");
            storyboard.Children.Add(fade);

            // Start fade out animation
            storyboard.Begin();
            await Task.Delay(duration);
        }
    }
}

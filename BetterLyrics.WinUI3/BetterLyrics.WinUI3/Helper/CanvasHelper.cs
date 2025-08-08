using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace BetterLyrics.WinUI3.Helper
{
    public class CanvasHelper
    {
        public static CanvasLinearGradientBrush CreateHorizontalFillBrush(
            ICanvasAnimatedControl control,
            List<(double position, double opacity)> stops,
            double startX,
            double width
        )
        {
            return new CanvasLinearGradientBrush(control, stops.Select(stops => new CanvasGradientStop
            {
                Position = (float)stops.position,
                Color = Color.FromArgb((byte)(stops.opacity * 255), 128, 128, 128),
            }).ToArray())
            {
                StartPoint = new Vector2((float)startX, 0),
                EndPoint = new Vector2((float)(startX + width), 0),
            };
        }
    }
}

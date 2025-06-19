using System;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.ViewModels;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace BetterLyrics.WinUI3.Rendering
{
    public partial class BaseRendererViewModel(ISettingsService settingsService)
        : BaseViewModel(settingsService)
    {
        public TimeSpan TotalTime { get; set; } = TimeSpan.Zero;
        public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;

        public virtual void Calculate(
            ICanvasAnimatedControl control,
            CanvasAnimatedUpdateEventArgs args
        )
        {
            TotalTime += args.Timing.ElapsedTime;
            ElapsedTime = args.Timing.ElapsedTime;
        }
    }
}

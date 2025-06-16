using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Renderer
{
    public sealed partial class DesktopLyricsRenderer : UserControl
    {
        public DesktopLyricsRendererViewModel ViewModel =
            Ioc.Default.GetRequiredService<DesktopLyricsRendererViewModel>();

        public DesktopLyricsRenderer()
        {
            InitializeComponent();
        }

        private void LyricsCanvas_Draw(
            Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender,
            Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedDrawEventArgs args
        )
        {
            ViewModel.Draw(sender, args.DrawingSession);
        }

        private void LyricsCanvas_Update(
            Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender,
            Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedUpdateEventArgs args
        )
        {
            ViewModel.Calculate(sender, args);
        }
    }
}

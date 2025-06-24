// 2025/6/23 by Zhe Fang

using System.Diagnostics;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Renderer
{
    /// <summary>
    /// Defines the <see cref="LyricsRenderer" />
    /// </summary>
    public sealed partial class LyricsRenderer : UserControl
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LyricsRenderer"/> class.
        /// </summary>
        public LyricsRenderer()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<LyricsRendererViewModel>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the ViewModel
        /// </summary>
        public LyricsRendererViewModel ViewModel { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// The LyricsCanvas_Draw
        /// </summary>
        /// <param name="sender">The sender<see cref="Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl"/></param>
        /// <param name="args">The args<see cref="Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedDrawEventArgs"/></param>
        private void LyricsCanvas_Draw(
            Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender,
            Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedDrawEventArgs args
        )
        {
            ViewModel.Draw(sender, args.DrawingSession);
        }

        /// <summary>
        /// The LyricsCanvas_Update
        /// </summary>
        /// <param name="sender">The sender<see cref="Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl"/></param>
        /// <param name="args">The args<see cref="Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedUpdateEventArgs"/></param>
        private void LyricsCanvas_Update(
            Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender,
            Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedUpdateEventArgs args
        )
        {
            ViewModel.Update(sender, args);
        }

        #endregion
    }
}

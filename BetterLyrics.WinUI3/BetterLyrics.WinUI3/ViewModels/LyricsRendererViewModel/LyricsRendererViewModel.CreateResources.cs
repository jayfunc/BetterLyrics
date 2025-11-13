using Microsoft.Extensions.Logging;

namespace BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel
{
    public partial class LyricsRendererViewModel
    {
        public void CreateResources(Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            _logger.LogInformation("Creating resources... Reason: {Reason}", args.Reason);
            switch (args.Reason)
            {
                case Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesReason.FirstTime:
                    _isDeviceChanged = true;
                    break;
                case Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesReason.NewDevice:
                    _isDeviceChanged = true;
                    break;
                case Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesReason.DpiChanged:
                    break;
                default:
                    break;
            }
        }
    }
}

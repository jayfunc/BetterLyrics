using BetterLyrics.WinUI3.Helper;
using CommunityToolkit.WinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel
{
    public partial class LyricsRendererViewModel
    {
        private PixelShaderEffect? _effect;

        public async Task CreateResourcesAsync(Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedControl control)
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/effect.bin"));
            IBuffer buffer = await FileIO.ReadBufferAsync(file);
            var bytes = buffer.ToArray();
            _effect = new PixelShaderEffect(bytes);
            _effect.Properties["Width"] = Convert.ToSingle(control.ConvertDipsToPixels((float)control.Size.Width, CanvasDpiRounding.Round));
            _effect.Properties["Height"] = Convert.ToSingle(control.ConvertDipsToPixels((float)control.Size.Height, CanvasDpiRounding.Round));
            _effect.Properties["color1"] = Colors.Black.ToVector3RGB();
            _effect.Properties["color2"] = Colors.Black.ToVector3RGB();
            _effect.Properties["color3"] = Colors.Black.ToVector3RGB();
            _effect.Properties["color4"] = Colors.Black.ToVector3RGB();
            _effect.Properties["EnableLightWave"] = false;
        }
    }
}

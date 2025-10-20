using CommunityToolkit.WinUI;
using Microsoft.Graphics.Canvas.Effects;
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

        public async Task CreateResourcesAsync()
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/effect.bin"));
            IBuffer buffer = await FileIO.ReadBufferAsync(file);
            var bytes = buffer.ToArray();
            _effect = new PixelShaderEffect(bytes);
            _effect.Properties["EnableLightWave"] = false;
        }
    }
}

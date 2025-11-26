using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Shaders;
using ComputeSharp.D2D1.WinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using System;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel
{
    public partial class LyricsRendererViewModel
    {
        private PixelShaderEffect? _fluidEffect;
        private PixelShaderEffect<SnowEffect>? _snowEffect;
        private PixelShaderEffect<FogEffect>? _fogEffect;
        private PixelShaderEffect<RaindropEffect>? _raindropEffect;

        private void DisposeFluidEffect()
        {
            _fluidEffect?.Dispose();
            _fluidEffect = null;
        }

        private async void RecreateFluidEffect(ICanvasAnimatedControl control)
        {
            DisposeFluidEffect();

            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/FluidEffect.bin"));
            IBuffer buffer = await FileIO.ReadBufferAsync(file);
            var bytes = buffer.ToArray();
            _fluidEffect = new PixelShaderEffect(bytes);
            _fluidEffect.Properties["Width"] = (float)control.ConvertDipsToPixels((float)control.Size.Width, CanvasDpiRounding.Round);
            _fluidEffect.Properties["Height"] = (float)control.ConvertDipsToPixels((float)control.Size.Height, CanvasDpiRounding.Round);
            _fluidEffect.Properties["color1"] = _albumArtAccentColor1Transition.Value.ToVector3RGB();
            _fluidEffect.Properties["color2"] = _albumArtAccentColor2Transition.Value.ToVector3RGB();
            _fluidEffect.Properties["color3"] = _albumArtAccentColor3Transition.Value.ToVector3RGB();
            _fluidEffect.Properties["color4"] = _albumArtAccentColor4Transition.Value.ToVector3RGB();
            _fluidEffect.Properties["EnableLightWave"] = false;
        }

        private void DisposeSnowEffect()
        {
            _snowEffect?.Dispose();
            _snowEffect = null;
        }

        private void RecreateSnowEffect()
        {
            DisposeSnowEffect();
            _snowEffect = new();
        }

        private void DisposeFogEffect()
        {
            _fogEffect?.Dispose();
            _fogEffect = null;
        }

        private void RecreateFogEffect()
        {
            DisposeFogEffect();
            _fogEffect = new();
        }
    }
}

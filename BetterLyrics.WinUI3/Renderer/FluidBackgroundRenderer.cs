using BetterLyrics.WinUI3.Extensions;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;

namespace BetterLyrics.WinUI3.Renderer
{
    public partial class FluidBackgroundRenderer : BreathingRendererBase, IDisposable
    {
        private PixelShaderEffect? _fluidEffect;
        private float _timeAccumulator = 0f;
        private Vector3 _c1, _c2, _c3, _c4;

        public bool IsEnabled { get; set; } = false;
        public double Opacity { get; set; } = 1.0;

        public bool EnableLightWave { get; set; } = true;

        public async Task LoadResourcesAsync()
        {
            Dispose();

            try
            {
                var uri = new Uri("ms-appx:///Assets/FluidEffect.bin");
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);

                using (var stream = await file.OpenReadAsync())
                {
                    var buffer = new Windows.Storage.Streams.Buffer((uint)stream.Size);
                    await stream.ReadAsync(buffer, (uint)stream.Size, Windows.Storage.Streams.InputStreamOptions.None);
                    byte[] bytes = buffer.ToArray();

                    _fluidEffect = new PixelShaderEffect(bytes);

                    _fluidEffect.Properties["EnableLightWave"] = EnableLightWave;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FluidRenderer] Load Failed: {ex.Message}");
                _fluidEffect = null;
            }
        }

        public void UpdateColors(Color c1, Color c2, Color c3, Color c4)
        {
            _c1 = c1.ToVector3RGB();
            _c2 = c2.ToVector3RGB();
            _c3 = c3.ToVector3RGB();
            _c4 = c4.ToVector3RGB();
        }

        public void Update(TimeSpan deltaTime, float bassEnergy, int breathingIntensity)
        {
            if (_fluidEffect == null || !IsEnabled) return;

            base.UpdateBreathing(bassEnergy, breathingIntensity);

            _timeAccumulator += (float)deltaTime.TotalSeconds;

            _fluidEffect?.Properties["iTime"] = _timeAccumulator;

            _fluidEffect?.Properties["color1"] = _c1;
            _fluidEffect?.Properties["color2"] = _c2;
            _fluidEffect?.Properties["color3"] = _c3;
            _fluidEffect?.Properties["color4"] = _c4;

            _fluidEffect?.Properties["EnableLightWave"] = EnableLightWave;
        }

        public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds, bool isBreathingEffectEnabled)
        {
            if (_fluidEffect == null || !IsEnabled || Opacity <= 0) return;

            float pixelWidth = control.ConvertDipsToPixels((float)control.Size.Width, CanvasDpiRounding.Round);
            float pixelHeight = control.ConvertDipsToPixels((float)control.Size.Height, CanvasDpiRounding.Round);

            _fluidEffect.Properties["Width"] = pixelWidth;
            _fluidEffect.Properties["Height"] = pixelHeight;

            var center = new Vector2((float)control.Size.Width / 2, (float)control.Size.Height / 2);

            ApplyBreathingTransform(ds, center, isBreathingEffectEnabled);

            if (Opacity >= 1.0)
            {
                ds.DrawImage(_fluidEffect);
            }
            else
            {
                using (var opacityEffect = new OpacityEffect
                {
                    Source = _fluidEffect,
                    Opacity = (float)Opacity
                })
                {
                    ds.DrawImage(opacityEffect);
                }
            }

            ResetTransform(ds, isBreathingEffectEnabled);
        }

        public void Dispose()
        {
            _fluidEffect?.Dispose();
            _fluidEffect = null;
        }

    }
}

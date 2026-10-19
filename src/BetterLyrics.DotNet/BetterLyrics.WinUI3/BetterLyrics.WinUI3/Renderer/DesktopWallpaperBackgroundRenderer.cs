using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI;

namespace BetterLyrics.WinUI3.Renderer;

public class DesktopWallpaperBackgroundRenderer : IDisposable
{
    private int _blurAmount;
    private int _loadVersion;
    private bool _needsCacheUpdate;
    private string? _requestedPath;
    private CanvasBitmap? _sourceBitmap;
    private CanvasRenderTarget? _targetCache;

    public bool IsEnabled { get; set; } = false;
    public int Opacity { get; set; } = 100;

    public bool HasWallpaper => _sourceBitmap != null;

    public int BlurAmount
    {
        get => _blurAmount;
        set
        {
            if (_blurAmount == value) return;

            _blurAmount = value;
            _needsCacheUpdate = true;
        }
    }

    public void Dispose()
    {
        _loadVersion++;
        _requestedPath = null;

        _sourceBitmap?.Dispose();
        _targetCache?.Dispose();

        _sourceBitmap = null;
        _targetCache = null;
    }

    public void Reset()
    {
        _loadVersion++;
        _requestedPath = null;
        _needsCacheUpdate = true;

        _sourceBitmap?.Dispose();
        _targetCache?.Dispose();

        _sourceBitmap = null;
        _targetCache = null;
    }

    public void SetWallpaperPath(ICanvasAnimatedControl control, string? path)
    {
        path = string.IsNullOrWhiteSpace(path) ? null : path;

        if (path == _requestedPath) return;

        _requestedPath = path;
        var version = ++_loadVersion;

        if (path == null)
        {
            _sourceBitmap?.Dispose();
            _targetCache?.Dispose();
            _sourceBitmap = null;
            _targetCache = null;
            return;
        }

        _ = LoadWallpaperAsync(control, path, version);
    }

    public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds)
    {
        if (!IsEnabled || Opacity <= 0) return;

        EnsureCachedLayer(control);
        if (_targetCache == null) return;

        var sourceRect = new Rect(0, 0, _targetCache.Size.Width, _targetCache.Size.Height);
        ds.DrawImage(_targetCache, 0, 0, sourceRect, Opacity / 100.0f);
    }

    private async Task LoadWallpaperAsync(ICanvasAnimatedControl control, string path, int version)
    {
        CanvasBitmap? bitmap = null;

        try
        {
            var bytes = await File.ReadAllBytesAsync(path);

            using var stream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
            {
                writer.WriteBytes(bytes);
                await writer.StoreAsync();
            }

            stream.Seek(0);
            bitmap = await CanvasBitmap.LoadAsync(control, stream);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadWallpaperAsync: {ex}");
            bitmap?.Dispose();

            // Allow a later poll to retry the same path (for example if the
            // wallpaper file is temporarily locked by Explorer).
            if (version == _loadVersion) _requestedPath = null;
            return;
        }

        if (version != _loadVersion)
        {
            bitmap.Dispose();
            return;
        }

        _sourceBitmap?.Dispose();
        _targetCache?.Dispose();
        _targetCache = null;

        _sourceBitmap = bitmap;
        _needsCacheUpdate = true;
    }

    private void EnsureCachedLayer(ICanvasAnimatedControl control)
    {
        if (_sourceBitmap == null)
        {
            _targetCache?.Dispose();
            _targetCache = null;
            return;
        }

        var size = control.Size;
        var deviceMismatch = _targetCache != null && _targetCache.Device != control.Device;

        if (!_needsCacheUpdate &&
            _targetCache != null &&
            !deviceMismatch &&
            _targetCache.Size.Width == size.Width &&
            _targetCache.Size.Height == size.Height)
        {
            return;
        }

        if (size.Width <= 0 || size.Height <= 0) return;

        _targetCache?.Dispose();
        _targetCache = new CanvasRenderTarget(control, (float)size.Width, (float)size.Height, control.Dpi);

        var imageWidth = _sourceBitmap.Size.Width;
        var imageHeight = _sourceBitmap.Size.Height;
        if (imageWidth <= 0 || imageHeight <= 0) return;

        var scale = (float)Math.Max((float)size.Width / imageWidth, (float)size.Height / imageHeight);
        var offsetX = (float)(((float)size.Width - imageWidth * scale) / 2);
        var offsetY = (float)(((float)size.Height - imageHeight * scale) / 2);
        var transform = Matrix3x2.CreateScale(scale) * Matrix3x2.CreateTranslation(offsetX, offsetY);

        using var drawingSession = _targetCache.CreateDrawingSession();
        drawingSession.Clear(Color.FromArgb(0, 0, 0, 0));

        using var transformEffect = new Transform2DEffect
        {
            Source = _sourceBitmap,
            TransformMatrix = transform,
            InterpolationMode = CanvasImageInterpolation.HighQualityCubic
        };

        if (BlurAmount > 0)
        {
            using var blurEffect = new GaussianBlurEffect
            {
                Source = transformEffect,
                BlurAmount = BlurAmount,
                BorderMode = EffectBorderMode.Hard
            };

            drawingSession.DrawImage(blurEffect);
        }
        else
        {
            drawingSession.DrawImage(transformEffect);
        }

        _needsCacheUpdate = false;
    }
}

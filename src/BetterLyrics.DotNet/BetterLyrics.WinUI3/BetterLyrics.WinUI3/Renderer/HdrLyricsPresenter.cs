using BetterLyrics.WinUI3.Shaders;
using ComputeSharp.D2D1.WinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Display;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Graphics.DirectX;

namespace BetterLyrics.WinUI3.Renderer;

internal sealed class HdrLyricsPresenter : IDisposable
{
    // FP16 scRGB defines 1.0 as 80 nits on an HDR display.
    private const float ScRgbWhiteNits = 80;
    private const DirectXPixelFormat HdrFormat = DirectXPixelFormat.R16G16B16A16Float;

    private readonly object _gate = new();
    private readonly CanvasSwapChainPanel _panel;
    private readonly DispatcherQueue _dispatcher;
    private readonly DisplayInformation _display;
    private readonly Action<ICanvasAnimatedControl, CanvasDrawingSession, CanvasTimingInformation, float> _drawScene;
    private (float WhiteScale, float HighlightGain) _levels;
    private CanvasSwapChain? _swapChain;
    private CanvasSwapChain? _panelSwapChain;
    private CanvasRenderTarget? _scene;
    private PixelShaderEffect<ScRgbOutputEffect>? _outputEffect;
    private bool _attachPending;
    private bool _attached;
    private bool _failed;
    private bool _disposed;

    private HdrLyricsPresenter(CanvasSwapChainPanel panel, DisplayInformation display,
        Action<ICanvasAnimatedControl, CanvasDrawingSession, CanvasTimingInformation, float> drawScene)
    {
        _panel = panel;
        _dispatcher = panel.DispatcherQueue;
        _display = display;
        _drawScene = drawScene;
        ReadDisplayLevels();
        _display.AdvancedColorInfoChanged += OnDisplayChanged;
    }

    public static HdrLyricsPresenter? TryCreate(CanvasSwapChainPanel panel, WindowId windowId,
        Action<ICanvasAnimatedControl, CanvasDrawingSession, CanvasTimingInformation, float> drawScene)
    {
        try
        {
            return new HdrLyricsPresenter(panel, DisplayInformation.CreateForWindowId(windowId), drawScene);
        }
        catch (Exception error) when (IsGraphicsError(error))
        {
            Debug.WriteLine(error);
            return null;
        }
    }

    public bool TryDraw(ICanvasAnimatedControl control, CanvasTimingInformation timing, bool enabled)
    {
        lock (_gate)
        {
            if (_disposed) return false;
            if (!enabled || _levels.HighlightGain <= 1)
            {
                ReleaseSurface();
                _failed = false;
                return false;
            }

            var size = control.Size;
            if (_failed || size.Width <= 0 || size.Height <= 0) return false;

            try
            {
                EnsureSurface(control, (float)size.Width, (float)size.Height);

                using (var scene = _scene!.CreateDrawingSession())
                {
                    scene.Clear(Colors.Transparent);
                    scene.EffectBufferPrecision = CanvasBufferPrecision.Precision16Float;
                    _drawScene(control, scene, timing, _levels.HighlightGain);
                }

                _outputEffect!.ConstantBuffer = new ScRgbOutputEffect(_levels.WhiteScale);
                using (var output = _swapChain!.CreateDrawingSession(Colors.Transparent))
                {
                    output.EffectBufferPrecision = CanvasBufferPrecision.Precision16Float;
                    output.DrawImage(_outputEffect);
                }

                _swapChain.Present(0);
                if (!_attached && !_attachPending)
                    _attachPending = _dispatcher.TryEnqueue(AttachSurface);

                return _attached;
            }
            catch (Exception error) when (IsGraphicsError(error))
            {
                Debug.WriteLine(error);
                _failed = true;
                ReleaseSurface();
                return false;
            }
        }
    }

    private void EnsureSurface(ICanvasAnimatedControl control, float width, float height)
    {
        var dpi = control.Dpi;
        if (_scene != null && _scene.Dpi == dpi &&
            _scene.SizeInPixels.Width == control.ConvertDipsToPixels(width, CanvasDpiRounding.Round) &&
            _scene.SizeInPixels.Height == control.ConvertDipsToPixels(height, CanvasDpiRounding.Round))
            return;

        if (_swapChain == null)
            _swapChain = new CanvasSwapChain(control, width, height, dpi, HdrFormat, 2, CanvasAlphaMode.Premultiplied);
        else
            _swapChain.ResizeBuffers(width, height, dpi);

        _outputEffect?.Dispose();
        _outputEffect = null;
        _scene?.Dispose();
        _scene = new CanvasRenderTarget(control, width, height, dpi, HdrFormat, CanvasAlphaMode.Premultiplied);
        _outputEffect = new PixelShaderEffect<ScRgbOutputEffect>();
        _outputEffect.Sources[0] = _scene;
    }

    private void AttachSurface()
    {
        lock (_gate)
        {
            _attachPending = false;
            if (_disposed || _swapChain == null || _levels.HighlightGain <= 1) return;

            try
            {
                _panel.SwapChain = _swapChain;
                _panelSwapChain = _swapChain;
                _panel.Visibility = Visibility.Visible;
                _attached = true;
            }
            catch (Exception error) when (IsGraphicsError(error))
            {
                Debug.WriteLine(error);
                _failed = true;
                ReleaseSurface();
            }
        }
    }

    private void OnDisplayChanged(DisplayInformation sender, object args)
    {
        _dispatcher.TryEnqueue(() =>
        {
            lock (_gate)
            {
                if (_disposed) return;
                ReadDisplayLevels();
                _failed = false;
                if (_levels.HighlightGain <= 1) ReleaseSurface();
            }
        });
    }

    private void ReadDisplayLevels()
    {
        _levels = default;
        try
        {
            var color = _display.GetAdvancedColorInfo();
            if (color.CurrentAdvancedColorKind != DisplayAdvancedColorKind.HighDynamicRange) return;

            var white = color.SdrWhiteLevelInNits;
            var peak = color.MaxLuminanceInNits;
            if (!double.IsFinite(white) || !double.IsFinite(peak) || white <= 0 || peak <= white) return;

            _levels = ((float)(white / ScRgbWhiteNits), (float)(peak / white));
        }
        catch (Exception error) when (IsGraphicsError(error))
        {
            Debug.WriteLine(error);
        }
    }

    public void Reset()
    {
        lock (_gate)
        {
            _failed = false;
            ReleaseSurface();
        }
    }

    private void ReleaseSurface()
    {
        _outputEffect?.Dispose();
        _outputEffect = null;
        _scene?.Dispose();
        _scene = null;

        var retired = _swapChain;
        _swapChain = null;
        _attached = false;
        if (retired == null) return;

        if (!_dispatcher.TryEnqueue(() =>
            {
                lock (_gate)
                {
                    try
                    {
                        if (ReferenceEquals(_panelSwapChain, retired))
                        {
                            _panel.Visibility = Visibility.Collapsed;
                            _panel.SwapChain = null;
                            _panelSwapChain = null;
                        }
                    }
                    finally
                    {
                        retired.Dispose();
                    }
                }
            }))
            retired.Dispose();
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            _display.AdvancedColorInfoChanged -= OnDisplayChanged;
            _panel.Visibility = Visibility.Collapsed;
            _panel.SwapChain = null;
            _panelSwapChain = null;
            ReleaseSurface();
        }
    }

    private static bool IsGraphicsError(Exception error) =>
        error is COMException or ArgumentException or InvalidOperationException or NotSupportedException;
}

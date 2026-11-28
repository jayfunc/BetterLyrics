using BetterLyrics.Core.Interfaces.Providers;
using Microsoft.Graphics.Canvas;
using SpoutDx.Net.Interop;
using System;
using Vanara.PInvoke;
using Vortice.Direct3D11;
using WinRT;

namespace BetterLyrics.WinUI3.Providers;

/// <summary>
/// Singleton Provider
/// Co-author:
/// 1) <see href="https://github.com/cnbluefire" />
/// 2) <see href="https://github.com/Raspberry-Monster" />
/// </summary>
public partial class SpoutTextureProvider : ISpoutTextureProvider
{
    private static readonly Guid DxgiInterfaceAccessGuid = new("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1");
    private static readonly object SendGate = new();
    private bool _isDisposed;
    private IntPtr _devicePointer;
    private SpoutSender? _sender;

    public string SenderName { get; private set; } = "BetterLyrics (Disabled)";

    public void Close()
    {
        if (_isDisposed) return;

        _sender?.Dispose();
        _sender = null;
        _devicePointer = IntPtr.Zero;

        _isDisposed = true;
    }

    public void Initialize(object device, string senderName)
    {
        if (device is not CanvasDevice canvasDevice) return;

        var deviceObject = canvasDevice.As<IWinRTObject>();
        HRESULT result = deviceObject.NativeObject.TryAs(DxgiInterfaceAccessGuid, out var pointer);

        if (result == HRESULT.S_OK)
        {
            using var access = new IDirect3DDxgiInterfaceAccess(pointer);
            using var d3dDevice = access.GetInterface<ID3D11Device>();

            // 同一设备重复初始化（CreateResources 可能多次触发）时复用 sender，
            // 避免频繁释放驱动仍在使用的共享纹理资源。
            if (_sender != null && _devicePointer == d3dDevice.NativePointer)
            {
                _sender.Name = senderName;
                SenderName = senderName;
                _isDisposed = false;
                return;
            }

            _sender?.Dispose();
            _sender = new SpoutSender(d3dDevice.NativePointer)
            {
                Name = senderName
            };
            _devicePointer = d3dDevice.NativePointer;
            SenderName = senderName;
            _isDisposed = false;
        }
    }

    public void SendTexture(object renderTarget)
    {
        if (_isDisposed || _sender == null || renderTarget == null) return;

        var canvasRenderTarget = (CanvasRenderTarget?)renderTarget;

        if (canvasRenderTarget == null) return;

        HRESULT success = canvasRenderTarget.As<IWinRTObject>().NativeObject.TryAs(DxgiInterfaceAccessGuid, out var pointer);

        if (success == HRESULT.S_OK)
        {
            using var access = new IDirect3DDxgiInterfaceAccess(pointer);
            using var texture = access.GetInterface<ID3D11Texture2D>();

            // 跨设备的共享纹理会让部分显卡驱动在内部工作线程上崩溃
            if (texture.Device.NativePointer != _devicePointer) return;

            // 多个画布可能共用同一 D3D 设备，串行发送避免并发访问驱动共享资源
            lock (SendGate)
            {
                _sender.SendTexture(texture.NativePointer);
            }
        }
    }
}
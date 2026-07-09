using System;
using BetterLyrics.Core.Interfaces.Providers;
using Microsoft.Graphics.Canvas;
using SpoutDx.Net.Interop;
using Vanara.PInvoke;
using Vortice.Direct3D11;
using WinRT;

namespace BetterLyrics.WinUI3.Providers;

/// <summary>
///     Co-author:
///     1) <see href="https://github.com/cnbluefire" />
///     2) <see href="https://github.com/Raspberry-Monster" />
/// </summary>
public partial class SpoutTextureProvider : ISpoutTextureProvider
{
    private static readonly Guid DxgiInterfaceAccessGuid = new("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1");
    private bool _isDisposed;
    private SpoutSender? _sender;

    public string SenderName { get; private set; } = "BetterLyrics (Disabled)";

    public void Close()
    {
        if (_isDisposed) return;

        _sender?.Dispose();
        _sender = null;

        _isDisposed = true;
    }

    public void Initialize(object device, string senderName)
    {
        if (device == null) return;

        var canvasDevice = (CanvasDevice?)device;

        if (canvasDevice == null) return;

        var deviceObject = canvasDevice.As<IWinRTObject>();
        HRESULT result = deviceObject.NativeObject.TryAs(DxgiInterfaceAccessGuid, out var pointer);

        if (result == HRESULT.S_OK)
        {
            using var access = new IDirect3DDxgiInterfaceAccess(pointer);
            using var d3dDevice = access.GetInterface<ID3D11Device>();

            _sender = new SpoutSender(d3dDevice.NativePointer)
            {
                Name = senderName
            };
            SenderName = senderName;
        }
    }

    public void SendTexture(object renderTarget)
    {
        if (_sender == null || renderTarget == null) return;

        var canvasRenderTarget = (CanvasRenderTarget?)renderTarget;

        if (canvasRenderTarget == null) return;

        HRESULT success = canvasRenderTarget.As<IWinRTObject>().NativeObject.TryAs(DxgiInterfaceAccessGuid, out var pointer);

        if (success == HRESULT.S_OK)
        {
            using var access = new IDirect3DDxgiInterfaceAccess(pointer);
            using var texture = access.GetInterface<ID3D11Texture2D>();
            _sender.SendTexture(texture.NativePointer);
        }
    }
}
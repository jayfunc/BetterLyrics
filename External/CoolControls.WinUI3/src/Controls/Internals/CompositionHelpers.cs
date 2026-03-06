using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi;
using WinRT;
using WinRT.Interop;


namespace CoolControls.WinUI3.Controls.Internals
{
    internal static class CompositionHelpers
    {
        internal static async Task<IDirect3DSurface?> CaptureToDirect3DSurfaceAsync(CompositionGraphicsDevice graphicsDevice, Visual visual, Vector2 size, float dpi)
        {
            if (visual == null) return null;

            var canvasSurfaceInterface = await CaptureAsync(graphicsDevice, visual, size, dpi);

            if (canvasSurfaceInterface == null) return null;

            var canvasSurface = ((CompositionDrawingSurface)canvasSurfaceInterface);

            return CopySurface(CanvasComposition.GetCanvasDevice(graphicsDevice), canvasSurface);
        }

        internal static async Task<ICompositionSurface?> CaptureAsync(CompositionGraphicsDevice graphicsDevice, Visual visual, Vector2 size, float dpi)
        {
            if (visual == null) return null;

            var captureSize = new SizeInt32((int)(Math.Ceiling(size.X)), (int)(Math.Ceiling(size.Y)));
            var captureVisual = visual;

            SpriteVisual? spriteVisual = null;
            CompositionSurfaceBrush? surfaceBrush = null;
            CompositionVisualSurface? visualSurface = null;

            try
            {
                if (dpi > 96)
                {
                    var scale = dpi / 96;
                    captureSize = new SizeInt32((int)(size.X * scale), (int)(size.Y * scale));

                    visualSurface = visual.Compositor.CreateVisualSurface();
                    visualSurface.SourceVisual = visual;
                    visualSurface.SourceSize = size;

                    surfaceBrush = visual.Compositor.CreateSurfaceBrush(visualSurface);
                    surfaceBrush.HorizontalAlignmentRatio = 0;
                    surfaceBrush.VerticalAlignmentRatio = 0;
                    surfaceBrush.Stretch = CompositionStretch.None;
                    surfaceBrush.Scale = new Vector2(scale, scale);

                    spriteVisual = visual.Compositor.CreateSpriteVisual();
                    spriteVisual.Size = size * 2;
                    spriteVisual.Brush = surfaceBrush;

                    captureVisual = spriteVisual;
                }

                return await graphicsDevice.CaptureAsync(
                    captureVisual,
                    captureSize,
                    Microsoft.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    Microsoft.Graphics.DirectX.DirectXAlphaMode.Premultiplied,
                    0);
            }
            finally
            {
                if (spriteVisual != null)
                {
                    spriteVisual.Brush = null;
                    spriteVisual.Dispose();
                }

                if (surfaceBrush != null)
                {
                    surfaceBrush.Surface = null;
                    surfaceBrush.Dispose();
                }

                visualSurface?.Dispose();
            }
        }

        internal static unsafe IDirect3DSurface CopySurface(IDirect3DDevice direct3DDevice, CompositionDrawingSurface surface)
        {
            var surfaceSize = surface.SizeInt32;
            var interop = surface.As<ICompositionDrawingSurfaceInterop2>();

            var dxgiDeviceAccess = direct3DDevice.As<Windows.Win32.System.WinRT.Direct3D11.IDirect3DDxgiInterfaceAccess>();

            var guid = typeof(ID3D11Device).GUID;
            dxgiDeviceAccess.GetInterface(&guid, out var value);
            var d3d11Device = (ID3D11Device)value;

            var surfaceDesc = new D3D11_TEXTURE2D_DESC()
            {
                Width = (uint)surfaceSize.Width,
                Height = (uint)surfaceSize.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Windows.Win32.Graphics.Dxgi.Common.DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                SampleDesc = new Windows.Win32.Graphics.Dxgi.Common.DXGI_SAMPLE_DESC()
                {
                    Count = 1,
                    Quality = 0,
                },
                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                BindFlags = D3D11_BIND_FLAG.D3D11_BIND_UNORDERED_ACCESS | D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE,
                CPUAccessFlags = D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ,
                MiscFlags = D3D11_RESOURCE_MISC_FLAG.D3D11_RESOURCE_MISC_SHARED,
            };

            d3d11Device.CreateTexture2D(&surfaceDesc, ppTexture2D: out var texture2d);

            interop.CopySurface(texture2d, 0, 0);

            Windows.Win32.PInvoke.CreateDirect3D11SurfaceFromDXGISurface(
                (IDXGISurface)texture2d,
                out var _graphicsSurface).ThrowOnFailure();

            var ptr = Marshal.GetIUnknownForObject(_graphicsSurface);

            using (var objRef = ObjectReference<IUnknownVftbl>.Attach(ref ptr))
            {
                return objRef.AsInterface<IDirect3DSurface>();
            }
        }



        [Guid("2D6355C2-AD57-4EAE-92E4-4C3EFF65D578"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport()]
        internal interface ICompositionDrawingSurfaceInterop
        {
            /// <summary>Initiates drawing on the surface.</summary>
            /// <param name="updateRect">
            /// <para>Type: <b>const RECT*</b> The section of the surface to update. The update rectangle must be within the boundaries of the surface. Specifying nullptr indicates the entire surface should be updated.</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop-begindraw#parameters">Read more on docs.microsoft.com</see>.</para>
            /// </param>
            /// <param name="iid">
            /// <para>Type: <b>REFIID</b> The identifier of the interface to retrieve.</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop-begindraw#parameters">Read more on docs.microsoft.com</see>.</para>
            /// </param>
            /// <param name="updateObject">
            /// <para>Type: <b>void**</b> Receives an interface pointer of the type specified in the iid parameter. This parameter must not be NULL.</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop-begindraw#parameters">Read more on docs.microsoft.com</see>.</para>
            /// </param>
            /// <param name="updateOffset">
            /// <para>Type: <b>POINT*</b> The offset into the surface where the application should draw updated content. This offset will reference the upper left corner of the update rectangle.</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop-begindraw#parameters">Read more on docs.microsoft.com</see>.</para>
            /// </param>
            /// <returns>
            /// <para>Type: <b>HRESULT</b> If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.</para>
            /// </returns>
            /// <remarks>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop-begindraw">Learn more about this API from docs.microsoft.com</see>.</para>
            /// </remarks>
            unsafe void BeginDraw([Optional] Windows.Win32.Foundation.RECT* updateRect, global::System.Guid* iid, [MarshalAs(UnmanagedType.IUnknown)] out object updateObject, global::System.Drawing.Point* updateOffset);

            /// <summary>Marks the end of drawing on the surface object.</summary>
            /// <returns>
            /// <para>Type: <b>HRESULT</b> If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.</para>
            /// </returns>
            /// <remarks>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop-enddraw">Learn more about this API from docs.microsoft.com</see>.</para>
            /// </remarks>
            void EndDraw();

            /// <summary>Changes the size of the surface.</summary>
            /// <param name="sizePixels">
            /// <para>Type: <b>SIZE</b> Width and height of the surface in pixels.</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop-resize#parameters">Read more on docs.microsoft.com</see>.</para>
            /// </param>
            /// <returns>
            /// <para>Type: <b>HRESULT</b> If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.</para>
            /// </returns>
            /// <remarks>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop-resize">Learn more about this API from docs.microsoft.com</see>.</para>
            /// </remarks>
            void Resize(Windows.Win32.Foundation.SIZE sizePixels);

            /// <summary>Scrolls a rectangular area of the logical surface.</summary>
            /// <param name="scrollRect">
            /// <para>Type: <b>const RECT*</b> The rectangular area of the surface to be scrolled, relative to the upper-left corner of the surface. If this parameter is NULL, the entire surface is scrolled.</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop-scroll#parameters">Read more on docs.microsoft.com</see>.</para>
            /// </param>
            /// <param name="clipRect">
            /// <para>Type: <b>const RECT*</b> The clipRect clips the destination (scrollRect after offset) of the scroll. The only bitmap content that will be scrolled are those that remain inside the clip rectangle after the scroll is completed.</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop-scroll#parameters">Read more on docs.microsoft.com</see>.</para>
            /// </param>
            /// <param name="offsetX">
            /// <para>Type: <b>int</b> The amount of horizontal scrolling, in pixels. Use positive values to scroll right, and negative values to scroll left.</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop-scroll#parameters">Read more on docs.microsoft.com</see>.</para>
            /// </param>
            /// <param name="offsetY">
            /// <para>Type: <b>int</b> The amount of vertical scrolling, in pixels. Use positive values to scroll down, and negative values to scroll up.</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop-scroll#parameters">Read more on docs.microsoft.com</see>.</para>
            /// </param>
            /// <returns>
            /// <para>Type: <b>HRESULT</b> If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.</para>
            /// </returns>
            /// <remarks>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop-scroll">Learn more about this API from docs.microsoft.com</see>.</para>
            /// </remarks>
            unsafe void Scroll([Optional] Windows.Win32.Foundation.RECT* scrollRect, [Optional] Windows.Win32.Foundation.RECT* clipRect, int offsetX, int offsetY);

            /// <summary>Resumes drawing on the surface object.</summary>
            /// <returns>
            /// <para>Type: <b>HRESULT</b> If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.</para>
            /// </returns>
            /// <remarks>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop-resumedraw">Learn more about this API from docs.microsoft.com</see>.</para>
            /// </remarks>
            void ResumeDraw();

            /// <summary>Suspends drawing on the surface object.</summary>
            /// <returns>
            /// <para>Type: <b>HRESULT</b> If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.</para>
            /// </returns>
            /// <remarks>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop-suspenddraw">Learn more about this API from docs.microsoft.com</see>.</para>
            /// </remarks>
            void SuspendDraw();
        }

        [Guid("D4B71A65-3052-4ABE-9183-E98DE02A41A9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport()]
        internal interface ICompositionDrawingSurfaceInterop2 : Windows.Win32.System.WinRT.Composition.ICompositionDrawingSurfaceInterop
        {
            unsafe new void BeginDraw([Optional] Windows.Win32.Foundation.RECT* updateRect, global::System.Guid* iid, [MarshalAs(UnmanagedType.IUnknown)] out object updateObject, global::System.Drawing.Point* updateOffset);

            new void EndDraw();

            new void Resize(Windows.Win32.Foundation.SIZE sizePixels);

            unsafe new void Scroll([Optional] Windows.Win32.Foundation.RECT* scrollRect, [Optional] Windows.Win32.Foundation.RECT* clipRect, int offsetX, int offsetY);

            new void ResumeDraw();

            new void SuspendDraw();

            /// <summary>Reads back the contents of a composition drawing surface (or a composition virtual drawing surface).</summary>
            /// <param name="destinationResource">
            /// <para>Type: **[IUnknown](../unknwn/nn-unknwn-iunknown.md)\*** Represents the Direct3D texture that will receive the copy. You must have created this resource on the same Direct3D device as the one associated with the [CompositionGraphicsDevice](/uwp/api/Windows.UI.Composition.CompositionGraphicsDevice) that was used to create the source composition drawing surface (or composition virtual drawing surface).</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop2-copysurface#parameters">Read more on docs.microsoft.com</see>.</para>
            /// </param>
            /// <param name="destinationOffsetX">
            /// <para>Type: **int** The x-coordinate of an offset (into *destinationResource*) where the copy will be performed. No pixels above or to the left of this offset are changed by the copy operation. The argument can't be negative.</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop2-copysurface#parameters">Read more on docs.microsoft.com</see>.</para>
            /// </param>
            /// <param name="destinationOffsetY">
            /// <para>Type: **int** The y-coordinate of an offset (into *destinationResource*) where the copy will be performed. No pixels above or to the left of this offset are changed by the copy operation. The argument can't be negative.</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop2-copysurface#parameters">Read more on docs.microsoft.com</see>.</para>
            /// </param>
            /// <param name="sourceRectangle">
            /// <para>Type: **const [RECT](../windef/ns-windef-rect.md)\*** An optional pointer to a constant **RECT** representing the rectangle on the source surface to copy out. The rectangle can't exceed the bounds of the source surface. In order to have enough room to receive the requested pixels, the destination resource must have at least as many pixels as the *destinationOffsetX* and *Y* parameters plus the width/height of this rectangle. If this parameter is null, then the entire source surface is copied (and the source surface size is used to validate the size of the destination resource).</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop2-copysurface#parameters">Read more on docs.microsoft.com</see>.</para>
            /// </param>
            /// <returns>
            /// <para>Type: **[HRESULT](/windows/win32/com/structure-of-com-error-codes)** **S_OK** if successful, otherwise returns an [HRESULT](/windows/win32/com/structure-of-com-error-codes) error code indicating the reason for failure. Also see [COM Error Codes (UI, Audio, DirectX, Codec)](/windows/win32/com/com-error-codes-10).</para>
            /// </returns>
            /// <remarks>
            /// <para>To create a Direct2D or a Direct3D surface for use with [Windows.UI.Composition](/uwp/api/windows.ui.composition), you use the [composition drawing surface interoperation](./index.md) interfaces. You can use the **CopySurface** method to read back the contents of a composition drawing surface (or a composition virtual drawing surface). **CopySurface** is a synchronous and instantaneous copy from one part of video memory to another; you don't need to call **Commit**. For any given composition drawing surface (or composition virtual drawing surface), your application can query for [ICompositionDrawingSurfaceInterop2](./nn-windows-ui-composition-interop-icompositiondrawingsurfaceinterop2.md), and call **CopySurface** on that interface. You can call **CopySurface** only when there are no pending updates to any surfaces belonging to the same [CompositionGraphicsDevice](/uwp/api/windows.ui.composition.compositiongraphicsdevice) as the source surface ([ICompositionDrawingSurfaceInterop::BeginDraw](./nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop-begindraw.md) has the same requirement). It's also illegal to call **CopySurface** on a non-virtual composition drawing surface that has never been updated, as its pixel contents are undefined. For virtual surfaces, since they are sparsely allocated, it's possible to specify a source rectangle that intersects uninitialized regions of the surface. In that case, the call is legal, but the result of the copy for those uninitialized regions is undefined. > [!NOTE] > This interface is available on Windows 10, version 1903 (10.0; Build 18362), but it is not defined in the `windows.ui.composition.interop.h` header file for that version of the Windows Software Development Kit (SDK). If you first obtain a pointer to an [ICompositionDrawingSurfaceInterop](./nn-windows-ui-composition-interop-icompositiondrawingsurfaceinterop.md) interface, you can then query that (via [QueryInterface](../unknwn/nf-unknwn-iunknown-queryinterface(refiid_void).md)) for a pointer to an [ICompositionDrawingSurfaceInterop2](./nn-windows-ui-composition-interop-icompositiondrawingsurfaceinterop2.md) interface.</para>
            /// <para><see href="https://learn.microsoft.com/windows/win32/api/windows.ui.composition.interop/nf-windows-ui-composition-interop-icompositiondrawingsurfaceinterop2-copysurface#">Read more on docs.microsoft.com</see>.</para>
            /// </remarks>
            unsafe void CopySurface([MarshalAs(UnmanagedType.IUnknown)] object destinationResource, int destinationOffsetX, int destinationOffsetY, [Optional] Windows.Win32.Foundation.RECT* sourceRectangle);
        }
    }
}

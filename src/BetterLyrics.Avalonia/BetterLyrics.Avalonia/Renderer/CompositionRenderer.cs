using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace BetterLyrics.Avalonia.Renderer;

public partial class CompositionRenderer : IDisposable
{
    // 💡 替换原本 Win2D 的离屏渲染目标组件
    private RenderTargetBitmap? _renderTarget;
    private double _lastDpi = 1.0;

    public void Dispose()
    {
        _renderTarget?.Dispose();
        _renderTarget = null;
    }

    /// <summary>
    /// 将一序列绘图动作录制并离屏输出为跨平台兼容的 RenderTargetBitmap
    /// </summary>
    /// <param name="size">逻辑尺寸 (DIPs)</param>
    /// <param name="dpi">当前窗口/显示器的标度因子 (例如 1.0, 1.5, 2.0)</param>
    /// <param name="clearColor">清空画布所使用的底色 (通常是透明色 Colors.Transparent)</param>
    /// <param name="drawCommands">在渲染线程被顺序消费的自绘 lambda 表达式组合</param>
    public RenderTargetBitmap Render(
        Size size,
        double dpi,
        Color clearColor,
        Action<DrawingContext> drawCommands)
    {
        // 💡 显式将设备独立像素(DIP)乘以 DPI，转换为底层的物理像素尺寸，防止高分屏下歌词发虚
        var pixelWidth = (int)Math.Round(size.Width * dpi);
        var pixelHeight = (int)Math.Round(size.Height * dpi);

        // 防止异常无效尺寸造成离屏缓冲区崩溃
        pixelWidth = Math.Max(1, pixelWidth);
        pixelHeight = Math.Max(1, pixelHeight);

        // 如果检测到容器物理尺寸变动或者系统 DPI 缩放发生阶跃，则重新向系统申请位图表面
        if (_renderTarget == null ||
            _renderTarget.PixelSize.Width != pixelWidth ||
            _renderTarget.PixelSize.Height != pixelHeight ||
            Math.Abs(_lastDpi - dpi) > 0.01)
        {
            _renderTarget?.Dispose();

            // 💡 建立带有高分屏DPI补偿的渲染目标位图，内部像素格式自动对齐全平台最优(通常是等效的 B8G8R8A8)
            _renderTarget = new RenderTargetBitmap(
                new PixelSize(pixelWidth, pixelHeight),
                new Vector(96 * dpi, 96 * dpi));

            _lastDpi = dpi;
        }

        // 💡 开启位图对应的离屏绘图上下文生命周期作用域
        using (var context = _renderTarget.CreateDrawingContext())
        {
            // 如果清空色包含颜色（非透明），则先给底层图层铺一层背景
            if (clearColor != Colors.Transparent)
            {
                var bgBrush = new SolidColorBrush(clearColor);
                context.DrawRectangle(bgBrush, null, new Rect(0, 0, size.Width, size.Height));
            }

            // 执行外层塞进来的链式渲染指令（如：自绘歌词、频谱、雪花效果等）
            drawCommands?.Invoke(context);
        }

        return _renderTarget;
    }
}
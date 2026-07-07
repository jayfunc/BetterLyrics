using Avalonia;
using Avalonia.Controls;

// ⚠️ 如果你安装了 Avalonia.Gif 库来支持动图，请取消注释以下命名空间：
// using Avalonia.Gif;

namespace BetterLyrics.Avalonia.Helpers;

public static class GifHelper
{
    // 1. 注册 Avalonia 附加属性
    public static readonly AttachedProperty<bool> IsPlayingProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, Image, bool>(
            "IsPlaying", true);

    // 2. 静态构造函数中全局拦截属性改变
    static GifHelper()
    {
        IsPlayingProperty.Changed.AddClassHandler<Image>(OnIsPlayingChanged);
    }

    public static bool GetIsPlaying(AvaloniaObject obj)
    {
        return obj.GetValue(IsPlayingProperty);
    }

    public static void SetIsPlaying(AvaloniaObject obj, bool value)
    {
        obj.SetValue(IsPlayingProperty, value);
    }

    private static void OnIsPlayingChanged(Image image, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is bool isPlaying)
        {
            UpdatePlayback(image, isPlaying);
        }
    }

    private static void UpdatePlayback(Image image, bool isPlaying)
    {
        // 💡 迁移指南：
        // Avalonia 原生的 Image/Bitmap 没有 ImageOpened 事件，也不支持 .Play() / .Stop()。
        //
        // 若使用第三方包（例如 `Avalonia.Gif`），你可以通过提取它的 Animator 来实现相同的逻辑：

        /*
        var animator = ImageBehavior.GetAnimator(image);
        if (animator != null)
        {
            if (isPlaying)
                animator.Play();
            else
                animator.Pause();
        }
        */
    }
}
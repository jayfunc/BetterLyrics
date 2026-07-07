using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Avalonia.Controls; // 使用新的控件命名空间
using CommunityToolkit.Mvvm.DependencyInjection;
// using FluentAvalonia.UI.Controls; // 如果你用到了 FluentAvalonia 的 PersonPicture，请取消注释这行

namespace BetterLyrics.Avalonia.Helpers;

public static class AppExtensions
{
    // 1. 注册 Avalonia 附加属性
    public static readonly AttachedProperty<string?> AumidProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, AvaloniaObject, string?>("Aumid");

    private static readonly IProgramProvider _programProvider =
        Ioc.Default.GetRequiredService<IProgramProvider>();

    // 2. 在静态构造函数中统一监听属性变化
    static AppExtensions()
    {
        AumidProperty.Changed.AddClassHandler<AvaloniaObject>(OnAumidChanged);
    }

    public static string? GetAumid(AvaloniaObject obj)
    {
        return obj.GetValue(AumidProperty);
    }

    public static void SetAumid(AvaloniaObject obj, string? value)
    {
        obj.SetValue(AumidProperty, value);
    }

    private static async void OnAumidChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
    {
        var aumid = e.NewValue as string;

        if (string.IsNullOrWhiteSpace(aumid)) return;

        if (d is Image imageControl)
        {
            imageControl.Source = null;

            try
            {
                var bytes = await _programProvider.GetIconByAumidAsync(aumid);

                if (GetAumid(imageControl) == aumid && bytes != null)
                {
                    // 3. Avalonia 的图片转换逻辑，直接利用 MemoryStream 构建 Bitmap
                    using var ms = new MemoryStream(bytes);
                    imageControl.Source = new Bitmap(ms);
                }
            }
            catch
            {
                // 忽略异常
            }
        }
        else if (d is TextBlock textBlock)
        {
            textBlock.Text = aumid;

            try
            {
                var name = await _programProvider.GetDisplayNameByAumidAsync(aumid);

                if (GetAumid(textBlock) == aumid) textBlock.Text = name ?? aumid;
            }
            catch
            {
                textBlock.Text = aumid;
            }
        }
        else if (d is PropertyRow propertyRow)
        {
            propertyRow.Value = aumid;

            try
            {
                var name = await _programProvider.GetDisplayNameByAumidAsync(aumid);

                if (GetAumid(propertyRow) == aumid) propertyRow.Value = name ?? aumid;
            }
            catch
            {
                propertyRow.Value = aumid;
            }
        }

        // 4. PersonPicture 不是 Avalonia 原生控件。如果你在项目中引入了 FluentAvalonia，
        // 可以把下面的代码块取消注释。
        /*
        else if (d is PersonPicture personPicture)
        {
            personPicture.DisplayName = aumid;

            try
            {
                var name = await _programProvider.GetDisplayNameByAumidAsync(aumid);

                if (GetAumid(personPicture) == aumid) personPicture.DisplayName = name ?? aumid;
            }
            catch
            {
                personPicture.DisplayName = aumid;
            }
        }
        */
    }
}
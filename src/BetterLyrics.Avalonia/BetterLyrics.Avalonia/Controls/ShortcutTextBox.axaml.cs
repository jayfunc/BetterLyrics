using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Avalonia.Controls;

public partial class ShortcutTextBox : UserControl
{
    public static readonly StyledProperty<List<string>> ShortcutProperty =
        AvaloniaProperty.Register<ShortcutTextBox, List<string>>(nameof(Shortcut), new List<string>());

    private readonly IGlobalToastProvider _globalToastProvider =
        Ioc.Default.GetRequiredService<IGlobalToastProvider>();

    private readonly ILocalizationService _localizationService =
        Ioc.Default.GetRequiredService<ILocalizationService>();

    public ShortcutTextBox()
    {
        InitializeComponent();
    }

    public List<string> Shortcut
    {
        get => GetValue(ShortcutProperty);
        set => SetValue(ShortcutProperty, value);
    }

    private void TextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        // 阻止默认的文本输入行为
        e.Handled = true;

        var shortcut = new List<string>();

        // 在 Avalonia 中，直接通过 KeyModifiers 即可获取所有修饰键状态，Meta 对应 Win 键
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) shortcut.Add("Ctrl");
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) shortcut.Add("Shift");
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt)) shortcut.Add("Alt");
        if (e.KeyModifiers.HasFlag(KeyModifiers.Meta)) shortcut.Add("Win");

        // 判断当前按下的键是否纯粹是修饰键本身
        bool isModifierKey = e.Key is Key.LeftCtrl or Key.RightCtrl
                                   or Key.LeftShift or Key.RightShift
                                   or Key.LeftAlt or Key.RightAlt
                                   or Key.LWin or Key.RWin;

        if (!isModifierKey && e.Key != Key.None)
        {
            shortcut.Add(e.Key.ToString());
        }

        Shortcut = shortcut;
        UpdateTextBox();
    }

    private void UpdateTextBox()
    {
        if (TextBox != null)
        {
            TextBox.Text = Shortcut != null ? string.Join(" + ", Shortcut) : string.Empty;
        }
    }

    private void TextBox_Loaded(object? sender, RoutedEventArgs e)
    {
        UpdateTextBox();
    }

    private void ClearButton_Click(object? sender, RoutedEventArgs e)
    {
        Shortcut = new List<string>();
        UpdateTextBox();
    }

    private void CheckButton_Click(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
        //var registered = GlobalHotKeyHook.IsHotKeyRegistered(Shortcut);
        //if (registered)
        //{
        //    _globalToastProvider.Show("SettingsPageShortcutRegSuccessInfo", null, MessageSeverity.Success);
        //}
        //else
        //{
        //    _globalToastProvider.Show("SettingsPageShortcutRegFailInfo", null, MessageSeverity.Error);
        //}
    }

    // 推荐添加：监听外部绑定带来的属性改变，同步更新 UI
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ShortcutProperty)
        {
            UpdateTextBox();
        }
    }
}
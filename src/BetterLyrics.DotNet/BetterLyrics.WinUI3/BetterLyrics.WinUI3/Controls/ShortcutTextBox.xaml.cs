using System.Collections.Generic;
using Windows.System;
using Windows.UI.Core;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.WinUI3.Hooks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class ShortcutTextBox : UserControl
{
    public static readonly DependencyProperty ShortcutProperty =
        DependencyProperty.Register(nameof(Shortcut), typeof(List<string>), typeof(ShortcutTextBox),
            new PropertyMetadata(default));

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
        get => (List<string>)GetValue(ShortcutProperty);
        set => SetValue(ShortcutProperty, value);
    }

    private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        List<string> shortcut = [];

        var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
            .HasFlag(CoreVirtualKeyStates.Down);
        var shift = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
            .HasFlag(CoreVirtualKeyStates.Down);
        var alt = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu)
            .HasFlag(CoreVirtualKeyStates.Down);
        var win = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.LeftWindows)
                      .HasFlag(CoreVirtualKeyStates.Down) ||
                  InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.RightWindows)
                      .HasFlag(CoreVirtualKeyStates.Down);

        if (ctrl) shortcut.Add("Ctrl");

        if (shift) shortcut.Add("Shift");

        if (alt) shortcut.Add("Alt");

        if (win) shortcut.Add("Win");

        if (e.Key != VirtualKey.Control &&
            e.Key != VirtualKey.Shift &&
            e.Key != VirtualKey.Menu &&
            e.Key != VirtualKey.LeftWindows &&
            e.Key != VirtualKey.RightWindows)
            shortcut.Add(e.Key.ToString());

        Shortcut = shortcut;

        UpdateTextBox();
    }

    private void UpdateTextBox()
    {
        TextBox.Text = string.Join(" + ", Shortcut);
    }

    private void TextBox_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateTextBox();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        Shortcut = [];
        UpdateTextBox();
    }

    private void CheckButton_Click(object sender, RoutedEventArgs e)
    {
        var registered = GlobalHotKeyHook.IsHotKeyRegistered(Shortcut);
        if (registered)
            _globalToastProvider.Show("SettingsPageShortcutRegSuccessInfo", null, MessageSeverity.Success);
        else
            _globalToastProvider.Show("SettingsPageShortcutRegFailInfo", null, MessageSeverity.Error);
    }
}
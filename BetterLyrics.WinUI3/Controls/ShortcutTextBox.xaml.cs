using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class ShortcutTextBox : UserControl
    {
        private readonly ILocalizationService _localizationService =
            Ioc.Default.GetRequiredService<ILocalizationService>();

        public ShortcutTextBox()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ShortcutProperty =
            DependencyProperty.Register(nameof(Shortcut), typeof(List<string>), typeof(ShortcutTextBox),
                new PropertyMetadata(default));

        public List<string> Shortcut
        {
            get => (List<string>)GetValue(ShortcutProperty);
            set => SetValue(ShortcutProperty, value);
        }

        private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            List<string> shortcut = [];

            bool ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
                .HasFlag(CoreVirtualKeyStates.Down);
            bool shift = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
                .HasFlag(CoreVirtualKeyStates.Down);
            bool alt = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu)
                .HasFlag(CoreVirtualKeyStates.Down);
            bool win = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.LeftWindows)
                           .HasFlag(CoreVirtualKeyStates.Down) ||
                       InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.RightWindows)
                           .HasFlag(CoreVirtualKeyStates.Down);

            if (ctrl)
            {
                shortcut.Add("Ctrl");
            }

            if (shift)
            {
                shortcut.Add("Shift");
            }

            if (alt)
            {
                shortcut.Add("Alt");
            }

            if (win)
            {
                shortcut.Add("Win");
            }

            if (e.Key != Windows.System.VirtualKey.Control &&
                e.Key != Windows.System.VirtualKey.Shift &&
                e.Key != Windows.System.VirtualKey.Menu &&
                e.Key != Windows.System.VirtualKey.LeftWindows &&
                e.Key != Windows.System.VirtualKey.RightWindows)
            {
                shortcut.Add(e.Key.ToString());
            }

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
            bool registered = GlobalHotKeyHook.IsHotKeyRegistered(Shortcut);
            if (registered)
            {
                GlobalToastManager.Show("SettingsPageShortcutRegSuccessInfo", null, MessageSeverity.Success);
            }
            else
            {
                GlobalToastManager.Show("SettingsPageShortcutRegFailInfo", null, MessageSeverity.Error);
            }
        }
    }
}
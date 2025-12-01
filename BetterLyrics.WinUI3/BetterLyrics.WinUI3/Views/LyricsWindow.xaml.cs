// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Services.LiveStatesService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Vanara.PInvoke;
using WinUIEx.Messaging;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class LyricsWindow : Window
    {
        private readonly ILiveStatesService _liveStatesService = Ioc.Default.GetRequiredService<ILiveStatesService>();

        private readonly WindowMessageMonitor _wmm;

        public LyricsWindowViewModel ViewModel { get; private set; } = Ioc.Default.GetRequiredService<LyricsWindowViewModel>();

        public LyricsWindow()
        {
            this.InitializeComponent();

            _wmm = new WindowMessageMonitor(this);
            _wmm.WindowMessageReceived += Wmm_WindowMessageReceived;

            this.Init("LyricsPageTitle", TitleBarHeightOption.Collapsed, BackdropType.Transparent);

            AppWindow.Changed += AppWindow_Changed;
            AppWindow.Closing += AppWindow_Closing;
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            ViewModel.ExitOrClose();
            args.Cancel = true;
        }

        public void SetTitleBarArea(TitleBarArea titleBarArea)
        {
            switch (titleBarArea)
            {
                case TitleBarArea.None:
                    SetTitleBar(PlaceholderGrid);
                    break;
                case TitleBarArea.Top:
                    SetTitleBar(TopCommandGrid);
                    break;
                case TitleBarArea.Whole:
                    SetTitleBar(RootGrid);
                    break;
                default:
                    break;
            }
        }

        private void Wmm_WindowMessageReceived(object? sender, WindowMessageEventArgs e)
        {
            if (e.Message.MessageId == (uint)User32.WindowMessage.WM_HOTKEY)
            {
                int id = (int)e.Message.WParam;
                GlobalHotKeyHook.TryInvokeAction(id);
            }
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (_liveStatesService.LiveStates.IsLyricsWindowStatusRefreshing)
            {
                return;
            }

            if (args.DidPositionChange || args.DidSizeChange)
            {
                var size = AppWindow.Size;
                var rect = AppWindow.Position;

                if (rect.X < 0 && rect.Y < 0 && rect.X + size.Width < 0 && rect.Y + size.Height < 0)
                {
                    return;
                }
                else
                {
                    _liveStatesService.LiveStates.LyricsWindowStatus.WindowBounds = new Windows.Foundation.Rect(rect.X, rect.Y, size.Width, size.Height);
                }
            }
        }

        private void TopCommandGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.TopCommandGridOpacity = 1f;
        }

        private void TopCommandGrid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.TopCommandGridOpacity = 0f;
        }

        private void MusicGalleryButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHook.OpenOrShowWindow<MusicGalleryWindow>();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ExitOrClose();
        }

        private void LyricsWindowSwitchButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHook.OpenOrShowWindow<LyricsWindowSwitchWindow>();
        }

        private void SettingsWindowButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHook.OpenOrShowWindow<SettingsWindow>();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHook.MinimizeWindow<LyricsWindow>();
        }
    }
}

using BetterLyrics.WinUI3.Controls;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Vanara.PInvoke;
using WinRT.Interop;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ToastOverlayWindow : Window
    {
        private OverlayInputHelper? _overlayInputHelper;
        public InAppNotificationStack Stack => NotificationStack;

        public ToastOverlayWindow()
        {
            this.InitializeComponent();

            this.Init(titleBarHeightOption: TitleBarHeightOption.Collapsed, backdropType: BackdropType.Transparent);
            this.SetWindowStyle(WindowStyle.Popup | WindowStyle.Visible);
            AppWindow.IsShownInSwitchers = false;
            WindowHook.SetIsClickThrough(this, true);
        }

        public void ShowOverlay(Microsoft.UI.Windowing.DisplayArea displayArea)
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            var appWindow = this.AppWindow;

            appWindow.MoveAndResize(displayArea.OuterBounds);

            // 显示窗口不抢占焦点
            User32.ShowWindow(hWnd, ShowWindowCommand.SW_SHOWNOACTIVATE);

            WindowHook.SetIsAlwaysOnTop(this, true);
        }

        private void NotificationStack_Loaded(object sender, RoutedEventArgs e)
        {
            NotificationStack.Notifications.CollectionChanged += (o, a) =>
            {
                if (a.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    if (NotificationStack.Notifications.Count == 0)
                    {
                        this.Hide();
                    }
                }
            };
            _overlayInputHelper = new(this)
            {
                OnInteractiveAreaMoved = (args) =>
                {
                    this.SetIsClickThrough(!args.Elements.Contains(NotificationStack));
                }
            };
            _overlayInputHelper.Register(RootGrid);
            _overlayInputHelper.Register(NotificationStack);
            _overlayInputHelper.Start();
        }

        private void NotificationStack_Unloaded(object sender, RoutedEventArgs e)
        {
            _overlayInputHelper = null;
        }
    }
}

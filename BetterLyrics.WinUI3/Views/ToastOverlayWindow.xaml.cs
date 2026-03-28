using BetterLyrics.WinUI3.Controls;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Settings;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using Vanara.PInvoke;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ToastOverlayWindow : Window,
        IRecipient<PropertyChangedMessage<ElementTheme>>
    {
        private OverlayInputHelper? _overlayInputHelper;
        public InAppNotificationStack Stack => NotificationStack;

        public ToastOverlayWindow()
        {
            this.InitializeComponent();
            WeakReferenceMessenger.Default.RegisterAll(this);
            this.Init(titleBarHeightOption: TitleBarHeightOption.Collapsed, backdropType: BackdropType.Transparent);
            this.SetIsBorderless(true);
            AppWindow.IsShownInSwitchers = false;
            WindowHook.SetIsClickThrough(this, true);
            this.SyncTheme();
        }

        public void Init(DisplayArea displayArea, int targetWidth = 592)
        {
            var targetRect = displayArea.OuterBounds;
            var xMargin = (int)((targetRect.Width - targetWidth) / 2.0);
            xMargin = Math.Max(xMargin, 0);
            targetRect.X += xMargin;
            targetRect.Width -= xMargin * 2;
            this.AppWindow.MoveAndResize(targetRect);
            this.SetIsAlwaysOnTop(true);
            this.Hide();
        }

        public void StartOverlayInputHelper()
        {
            _overlayInputHelper?.Start();
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
                        _overlayInputHelper?.Stop();
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
        }

        private void NotificationStack_Unloaded(object sender, RoutedEventArgs e)
        {
            _overlayInputHelper = null;
        }

        public void Receive(PropertyChangedMessage<ElementTheme> message)
        {
            if (message.Sender is GeneralSettings)
            {
                if (message.PropertyName == nameof(GeneralSettings.AppTheme))
                {
                    this.SyncTheme();
                }
            }
        }

    }
}

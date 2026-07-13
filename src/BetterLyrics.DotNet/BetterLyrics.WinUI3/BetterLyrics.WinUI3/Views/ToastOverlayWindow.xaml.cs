using System;
using System.Collections.Specialized;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Controls;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helpers;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views;

/// <summary>
///     An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ToastOverlayWindow : Window,
    IRecipient<PropertyChangedMessage<AppTheme>>
{
    private readonly IWindowManagerProvider _windowManagerProvider =
        Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    private OverlayInputHelper? _overlayInputHelper;

    public ToastOverlayWindow()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.RegisterAll(this);
        this.Init("ToastOverlayTitle", titleBarHeightOption: TitleBarHeightOption.Collapsed,
            backdropType: BackdropType.Transparent);
        _windowManagerProvider.SetIsBorderless(this, true);
        AppWindow.IsShownInSwitchers = false;
        _windowManagerProvider.SetIsClickThrough(this, true);
        this.SyncTheme();
    }

    public InAppNotificationStack Stack => NotificationStack;

    public void Receive(PropertyChangedMessage<AppTheme> message)
    {
        if (message.Sender is GeneralSettings)
            if (message.PropertyName == nameof(GeneralSettings.AppTheme))
                this.SyncTheme();
    }

    public void Init(DisplayArea displayArea, int targetWidth = 592)
    {
        var targetRect = displayArea.OuterBounds;
        var xMargin = (int)((targetRect.Width - targetWidth) / 2.0);
        xMargin = Math.Max(xMargin, 0);
        targetRect.X += xMargin;
        targetRect.Width -= xMargin * 2;
        AppWindow.MoveAndResize(targetRect);
        this.SetIsAlwaysOnTop(true);
        this.Hide();
    }

    public void StartOverlayInputHelper()
    {
        _overlayInputHelper?.Start();
    }

    private void NotificationStack_Loaded(object sender, RoutedEventArgs e)
    {
        NotificationStack.Notifications.CollectionChanged += (_, a) =>
        {
            if (a.Action == NotifyCollectionChangedAction.Remove)
                if (NotificationStack.Notifications.Count == 0)
                {
                    this.Hide();
                    _overlayInputHelper?.Stop();
                }
        };
        _overlayInputHelper = new OverlayInputHelper(this)
        {
            OnInteractiveAreaMoved = args =>
            {
                _windowManagerProvider.SetIsClickThrough(this, !args.Elements.Contains(NotificationStack));
            }
        };
        _overlayInputHelper.Register(RootGrid);
        _overlayInputHelper.Register(NotificationStack);
    }

    private void NotificationStack_Unloaded(object sender, RoutedEventArgs e)
    {
        _overlayInputHelper = null;
    }
}
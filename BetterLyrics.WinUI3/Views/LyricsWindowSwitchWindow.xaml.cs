using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LyricsWindowSwitchWindow : Window,
        IRecipient<PropertyChangedMessage<ElementTheme>>
    {
        public LyricsWindowSwitchWindowViewModel ViewModel { get; private set; } = Ioc.Default.GetRequiredService<LyricsWindowSwitchWindowViewModel>();

        public LyricsWindowSwitchWindow()
        {
            InitializeComponent();
            WeakReferenceMessenger.Default.RegisterAll(this);

            this.Init(title: "LyricsWindowSwitchWindowTitle", titleBarHeightOption: TitleBarHeightOption.Collapsed, backdropType: BackdropType.Transparent);
            this.SyncTheme();

            this.CenterOnScreen();
            this.SetWindowStyle(WindowStyle.Popup | WindowStyle.Visible);
            AppWindow.IsShownInSwitchers = false;
            this.SetIsAlwaysOnTop(true);
            SetTitleBar(PlaceholderGrid);

            AppWindow.Changed += AppWindow_Changed;
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (args.DidVisibilityChange)
            {
                if (sender.IsVisible)
                {
                    ViewModel.RootGridOpacity = 1;
                }
            }
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

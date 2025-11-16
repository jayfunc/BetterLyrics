using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
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
    public sealed partial class LyricsWindowSwitchWindow : Window
    {
        public LyricsWindowSwitchWindowViewModel ViewModel { get; private set; } = Ioc.Default.GetRequiredService<LyricsWindowSwitchWindowViewModel>();

        public LyricsWindowSwitchWindow()
        {
            InitializeComponent();

            this.Init("LyricsWindowSwitchWindowTitle", TitleBarHeightOption.Collapsed, BackdropType.Transparent);

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
    }
}

using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
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

            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Collapsed;
            this.CenterOnScreen();
            this.SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(Enums.BackdropType.Transparent);
            this.SetWindowStyle(WindowStyle.Popup | WindowStyle.Visible);
            AppWindow.IsShownInSwitchers = false;
            this.SetIsAlwaysOnTop(true);
            SetTitleBar(PlaceholderGrid);

            AppWindow.Changed += AppWindow_Changed;
        }

        private void AppWindow_Changed(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
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

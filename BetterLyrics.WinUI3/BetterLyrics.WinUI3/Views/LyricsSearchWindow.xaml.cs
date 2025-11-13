using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.ResourceService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LyricsSearchWindow : Window
    {
        private readonly IResourceService _resourceService = Ioc.Default.GetRequiredService<IResourceService>();

        public LyricsSearchWindow()
        {
            InitializeComponent();

            Title = _resourceService.GetLocalizedString("LyricsSearchPageTitle");
            AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
            AppWindow.SetIcons();

            ExtendsContentIntoTitleBar = true;

            AppWindow.Closing += AppWindow_Closing;

            SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(Enums.BackdropType.Transparent);
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            WindowHelper.CloseWindow<LyricsSearchWindow>();
        }

    }
}

using BetterLyrics.Core.Enums;
using BetterLyrics.Core.ViewModels;
using BetterLyrics.WinUI3.Extensions;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using System.Collections.Generic;
using Vanara.PInvoke;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LyricsWindowSwitchWindow : BaseWindow
{
    public LyricsWindowSwitchWindowViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<LyricsWindowSwitchWindowViewModel>();

    protected override IEnumerable<object> GetAdditionalTrackedViewModels() => [ViewModel];

    public LyricsWindowSwitchWindow()
    {
        InitializeComponent();

        this.Init("LyricsWindowSwitchWindowTitle", titleBarHeightOption: TitleBarHeightOption.Collapsed, backdropType: BackdropType.Transparent);

        SetTitleBar(PlaceholderGrid);
        this.CenterOnScreen();
        _windowManagerProvider.SetIsBorderless(this, true);
        AppWindow.IsShownInSwitchers = false;
        this.SetIsAlwaysOnTop(true);

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
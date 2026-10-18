using BetterLyrics.WinUI3.Extensions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class StatsDashboardWindow : BaseWindow
{
    public StatsDashboardWindow()
    {
        InitializeComponent();
        this.Init("SettingsPageStats");
    }
}
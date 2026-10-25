using BetterLyrics.Core.Enums;
using BetterLyrics.WinUI3.Extensions;
using Microsoft.UI.Windowing;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class SystemTrayWindow : BaseWindow
    {
        public SystemTrayWindow()
        {
            InitializeComponent();
            this.Init("AppSettingsControlSystemTray/Text", titleBarHeightOption: TitleBarHeightOption.Collapsed, backdropType: BackdropType.Transparent);
            _windowManagerProvider.SetIsBorderless(this, true);
            _windowManagerProvider.SetIsClickThrough(this, true);
            AppWindow.IsShownInSwitchers = false;
        }
    }
}

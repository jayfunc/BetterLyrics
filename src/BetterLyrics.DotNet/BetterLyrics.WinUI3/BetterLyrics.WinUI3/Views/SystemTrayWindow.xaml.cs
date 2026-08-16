using WinUIEx;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class SystemTrayWindow : WindowEx
    {
        public SystemTrayWindow()
        {
            InitializeComponent();
            
            // 将窗口移出屏幕可视范围，并从 Alt+Tab 和任务栏中隐藏
            // 这样既能激活窗口，又不会让用户看到它
            this.IsShownInSwitchers = false;
            this.MoveAndResize(-10000, -10000, 10, 10);
        }
    }
}

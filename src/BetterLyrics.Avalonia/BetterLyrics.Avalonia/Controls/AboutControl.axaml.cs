using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Avalonia.Controls;

public partial class AboutControl : UserControl
{
    public AboutControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<AboutControlViewModel>();
    }

    // 在 Avalonia 中，如果 DataContext 声明为可空，可以用 ! 抑制警告
    public AboutControlViewModel ViewModel => (AboutControlViewModel)DataContext!;

    private void AlipayWeChatPayButton_Click(object? sender, RoutedEventArgs e)
    {
        // 提示: 如果在 axaml 中已经使用了 <Button.Flyout> 标签，Avalonia 会自动接管点击弹出逻辑。
        // 这里的代码块用于展示如何在后台通过代码手动弹出 Attached Flyout。
        if (sender is Control control)
        {
            FlyoutBase.ShowAttachedFlyout(control);
        }
    }

    private void QQGroupButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control control)
        {
            FlyoutBase.ShowAttachedFlyout(control);
        }
    }
}
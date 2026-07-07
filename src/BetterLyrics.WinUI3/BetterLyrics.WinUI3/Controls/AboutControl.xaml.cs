using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class AboutControl : UserControl
{
    public AboutControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<AboutControlViewModel>();
    }

    public AboutControlViewModel ViewModel => (AboutControlViewModel)DataContext;

    private void AlipayWeChatPayButton_Click(object sender, RoutedEventArgs e)
    {
        AlipayWeChatPayFlyout.ShowAt(AlipayWeChatPayButton);
    }

    private void QQGroupButton_Click(object sender, RoutedEventArgs e)
    {
        QQGroupFlyout.ShowAt(QQGroupButton);
    }
}
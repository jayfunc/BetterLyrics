using Avalonia.Controls;
using Avalonia.Interactivity;
using BetterLyrics.Avalonia.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Avalonia.Controls;

public partial class AboutControl : UserControl
{
    public AboutControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<AboutControlViewModel>();
    }

    private void AlipayWeChatPayButton_Click(object sender, RoutedEventArgs e)
    {
        //AlipayWeChatPayFlyout.ShowAt(AlipayWeChatPayButton);
    }

    private void QQGroupButton_Click(object sender, RoutedEventArgs e)
    {
        //QQGroupFlyout.ShowAt(QQGroupButton);
    }
}
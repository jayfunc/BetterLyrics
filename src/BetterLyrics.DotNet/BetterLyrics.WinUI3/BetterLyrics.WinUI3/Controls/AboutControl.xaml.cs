using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class AboutControl : UserControl
{
    private double _creditsOffset = 0;
    private bool _isCreditsHovered = false;
    private bool _creditsInitialized = false;

    public AboutControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<AboutControlViewModel>();
    }

    public AboutControlViewModel ViewModel => (AboutControlViewModel)DataContext;

    private void PlayCreditsButton_Click(object sender, RoutedEventArgs e)
    {
        CreditsTransform?.Y = 8000; // Move off-screen to prevent flash on next open
        CrtOverlay.Visibility = Visibility.Visible;
        CrtTurnOnStoryboard.Begin();
    }

    private void CloseCreditsButton_Click(object sender, RoutedEventArgs e)
    {
        CrtTurnOffStoryboard.Begin();
    }

    private void CrtTurnOnStoryboard_Completed(object sender, object e)
    {
        _creditsInitialized = false;
        Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += CompositionTarget_Rendering;
    }

    private void CrtTurnOffStoryboard_Completed(object sender, object e)
    {
        Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= CompositionTarget_Rendering;
        CrtOverlay.Visibility = Visibility.Collapsed;
    }

    private void CompositionTarget_Rendering(object sender, object e)
    {
        if (CreditsScrollViewer == null || CreditsPanel == null) return;
        
        double containerHeight = CreditsScrollViewer.ActualHeight;
        double panelHeight = CreditsPanel.ActualHeight;

        if (containerHeight == 0 || panelHeight == 0) return;

        if (!_creditsInitialized)
        {
            _creditsOffset = containerHeight;
            _creditsInitialized = true;
        }

        if (!_isCreditsHovered)
        {
            _creditsOffset -= 0.8; // Vertical scroll speed
            double targetOffset = (containerHeight / 2) - panelHeight + 250;
            if (_creditsOffset <= targetOffset)
            {
                _creditsOffset = targetOffset; // Stop rolling
            }
            if (CreditsTransform != null) CreditsTransform.Y = _creditsOffset;
        }
    }

    private void Credits_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) => _isCreditsHovered = true;
    private void Credits_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) => _isCreditsHovered = false;

    private void AlipayWeChatPayButton_Click(object sender, RoutedEventArgs e)
    {
        AlipayWeChatPayFlyout.ShowAt(AlipayWeChatPayButton);
    }

    private void QQGroupButton_Click(object sender, RoutedEventArgs e)
    {
        QQGroupFlyout.ShowAt(QQGroupButton);
    }
}
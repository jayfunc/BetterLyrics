using BetterLyrics.Core.Enums;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class AppSettingsControl : UserControl, IRecipient<PropertyChangedMessage<bool>>
{
    public AppSettingsControlViewModel ViewModel => (AppSettingsControlViewModel)DataContext;

    public AppSettingsControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<AppSettingsControlViewModel>();
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message.Sender == ViewModel && message.PropertyName == nameof(ViewModel.IsDeepLinkRequested))
        {
            if (this.IsLoaded)
            {
                CheckAndProcessDeepLink();
            }
        }
    }

    private void CheckAndProcessDeepLink()
    {
        if (ViewModel.IsDeepLinkRequested)
        {
            ViewModel.IsDeepLinkRequested = false;
            foreach (NavigationViewItem item in ConfigNavView.MenuItems.Cast<NavigationViewItem>())
            {
                if ((AppSettingsSection)item.Tag == ViewModel.SelectedAppSettingsSection)
                {
                    ConfigNavView.SelectedItem = item;
                    break;
                }
            }
        }
    }

    private void ConfigNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is AppSettingsSection appSettingsSection)
        {
            ViewModel.SelectedAppSettingsSection = appSettingsSection;
        }
    }

    private void UserControl_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        CheckAndProcessDeepLink();

        if (ConfigNavView.SelectedItem == null)
        {
            AppearanceNavViewItem.IsSelected = true;
        }
    }
}

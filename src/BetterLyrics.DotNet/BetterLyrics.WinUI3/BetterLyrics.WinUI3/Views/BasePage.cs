using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Views;

public partial class BasePage : Page
{
    protected BasePage()
    {
        Loaded += BasePage_Loaded;
        Unloaded += BasePage_Unloaded;
    }

    protected virtual void Initialize() { }
    protected virtual void Cleanup() { }

    private void BasePage_Loaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.RegisterAll(this);

        if (DataContext is ITransientViewModel && DataContext is ObservableRecipient observableRecipient)
        {
            observableRecipient.IsActive = true;
        }

        Initialize();
    }

    private void BasePage_Unloaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);

        if (DataContext is ITransientViewModel && DataContext is ObservableRecipient observableRecipient)
        {
            observableRecipient.IsActive = false;
        }

        MemoryLeakDetector.Track(this);

        if (DataContext is ITransientViewModel)
        {
            MemoryLeakDetector.Track(DataContext);
        }

        Cleanup();

        MemoryLeakDetector.ScheduleCheck();
    }
}

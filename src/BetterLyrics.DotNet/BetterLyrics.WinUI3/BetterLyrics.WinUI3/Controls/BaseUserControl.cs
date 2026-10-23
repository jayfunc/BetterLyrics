using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Controls;

public partial class BaseUserControl : UserControl
{
    protected BaseUserControl()
    {
        Loaded += BaseUserControl_Loaded;
        Unloaded += BaseUserControl_Unloaded;
    }

    protected virtual void Initialize() { }
    protected virtual void Cleanup() { }

    private void BaseUserControl_Loaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.RegisterAll(this);

        if (DataContext is ITransientViewModel && DataContext is ObservableRecipient observableRecipient)
        {
            observableRecipient.IsActive = true;
        }

        Initialize();
    }

    private void BaseUserControl_Unloaded(object sender, RoutedEventArgs e)
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

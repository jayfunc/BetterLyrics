using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;

namespace BetterLyrics.WinUI3.Helpers;

public static class FrameworkElementLifecycleHelper
{
    public static void HandleLoaded(FrameworkElement element, Action onLoadedCustom, Action bindControls)
    {
        WeakReferenceMessenger.Default.RegisterAll(element);

        if (element.DataContext is ITransientViewModel && element.DataContext is ObservableRecipient observableRecipient)
        {
            observableRecipient.IsActive = true;
        }

        onLoadedCustom?.Invoke();

        bindControls?.Invoke();
    }

    public static void HandleUnloaded(FrameworkElement element, RoutedEventHandler loadedHandler, RoutedEventHandler unloadedHandler, Action onUnloadedCustom, Action unbindControls)
    {
        element.Loaded -= loadedHandler;
        element.Unloaded -= unloadedHandler;

        WeakReferenceMessenger.Default.UnregisterAll(element);

        if (element.DataContext is ITransientViewModel && element.DataContext is ObservableRecipient observableRecipient)
        {
            observableRecipient.IsActive = false;
        }

        MemoryLeakDetector.Track(element);

        if (element.DataContext is ITransientViewModel)
        {
            MemoryLeakDetector.Track(element.DataContext);
        }

        onUnloadedCustom?.Invoke();

        unbindControls?.Invoke();
    }
}

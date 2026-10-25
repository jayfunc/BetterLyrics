using BetterLyrics.WinUI3.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Views;

public abstract partial class BasePage : Page
{
    protected BasePage()
    {
        Loaded += BasePage_Loaded;
        Unloaded += BasePage_Unloaded;
    }

    protected abstract void BindGeneratedControls();
    protected abstract void UnbindGeneratedControls();

    protected virtual void OnLoaded() { }
    protected virtual void OnUnloaded() { }

    private void BasePage_Loaded(object sender, RoutedEventArgs e)
    {
        FrameworkElementLifecycleHelper.HandleLoaded(this, OnLoaded, BindGeneratedControls);
    }

    private void BasePage_Unloaded(object sender, RoutedEventArgs e)
    {
        FrameworkElementLifecycleHelper.HandleUnloaded(this, BasePage_Loaded, BasePage_Unloaded, OnUnloaded, UnbindGeneratedControls);
    }
}

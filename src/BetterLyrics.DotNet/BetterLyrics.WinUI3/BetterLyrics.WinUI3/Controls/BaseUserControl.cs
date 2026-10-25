using BetterLyrics.WinUI3.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Controls;

public abstract partial class BaseUserControl : UserControl
{
    protected BaseUserControl()
    {
        Loaded += BaseUserControl_Loaded;
        Unloaded += BaseUserControl_Unloaded;
    }

    protected abstract void BindGeneratedControls();
    protected abstract void UnbindGeneratedControls();

    protected virtual void OnLoaded() { }
    protected virtual void OnUnloaded() { }

    private void BaseUserControl_Loaded(object sender, RoutedEventArgs e)
    {
        FrameworkElementLifecycleHelper.HandleLoaded(this, OnLoaded, BindGeneratedControls);
    }

    private void BaseUserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        FrameworkElementLifecycleHelper.HandleUnloaded(this, BaseUserControl_Loaded, BaseUserControl_Unloaded, OnUnloaded, UnbindGeneratedControls);
    }
}

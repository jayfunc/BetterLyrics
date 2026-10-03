// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using BetterLyrics.WinUI3.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<SettingsPageViewModel>();
    }

    public SettingsPageViewModel ViewModel => (SettingsPageViewModel)DataContext;

    private void AutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is AutoSuggestBox box && !string.IsNullOrWhiteSpace(box.Text))
        {
            if (ViewModel.FilteredSettings.Count > 0)
            {
                box.IsSuggestionListOpen = true;
            }
        }
    }

    private async void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is SettingSearchItem item)
        {
            if (item == SettingSearchItemExtensions.LoadingPlaceholder || item == SettingSearchItemExtensions.NoResultsPlaceholder)
            {
                return;
            }

            ViewModel.NavigateToSettingSearchItem(item);

            // Show loading bar and wait for panels to become visible and layout to update
            LoadingOverlay.Visibility = Visibility.Visible;
            LoadingBar.IsIndeterminate = true;
            
            try
            {
                await Task.Delay(500);
            
                var element = FindChildByHeader(this, item.Title);
                if (element != null)
                {
                    element.StartBringIntoView();
                    element.Focus(FocusState.Keyboard);
                }
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                LoadingBar.IsIndeterminate = false;
            }
        }
    }

    private FrameworkElement? FindChildByHeader(DependencyObject parent, string header)
    {
        if (parent == null) return null;
        
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            
            if (child is SettingsCard card && card.Header is string cardHeader && cardHeader == header)
            {
                return card;
            }
            if (child is SettingsExpander expander && expander.Header is string expHeader && expHeader == header)
            {
                return expander;
            }
            if (child is TextBlock tb && tb.Text == header)
            {
                return tb;
            }

            var result = FindChildByHeader(child, header);
            if (result != null) return result;
        }
        return null;
    }


}

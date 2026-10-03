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
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
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
            
                bool wasHiddenExpanded = ExpandHiddenExpanders(this, item.Title);
                if (wasHiddenExpanded)
                {
                    // Wait for the expander to generate its visual tree and animate
                    await Task.Delay(300);
                }

                var element = FindChildByHeader(this, item.Title);
                if (element != null)
                {
                    bool wasExpanded = ExpandParentExpanders(element);
                    if (wasExpanded)
                    {
                        // Wait for the expander's layout and animation to finish so scrolling is accurate
                        await Task.Delay(300);
                    }

                    element.StartBringIntoView();
                    HighlightControl(element);
                }
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                LoadingBar.IsIndeterminate = false;
            }
        }
    }

    private bool ExpandHiddenExpanders(DependencyObject parent, string header)
    {
        if (parent == null) return false;
        bool expanded = false;

        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is SettingsExpander expander && !expander.IsExpanded)
            {
                foreach (var logicalItem in expander.Items)
                {
                    if (CheckLogicalMatch(logicalItem, header))
                    {
                        expander.IsExpanded = true;
                        expanded = true;
                        break;
                    }
                }
            }
            else if (child is Expander stdExpander && !stdExpander.IsExpanded && stdExpander.Content != null)
            {
                if (CheckLogicalMatch(stdExpander.Content, header))
                {
                    stdExpander.IsExpanded = true;
                    expanded = true;
                }
            }

            if (ExpandHiddenExpanders(child, header))
            {
                expanded = true;
            }
        }
        return expanded;
    }

    private bool CheckLogicalMatch(object item, string header)
    {
        if (item == null) return false;

        if (item is SettingsCard card && card.Header is string cardHeader && cardHeader == header) return true;
        if (item is SettingsExpander expander && expander.Header is string expHeader && expHeader == header) return true;
        if (item is TextBlock tb && tb.Text == header) return true;

        if (item is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (CheckLogicalMatch(child, header)) return true;
            }
        }
        else if (item is Expander standardExpander && standardExpander.Content != null)
        {
            if (CheckLogicalMatch(standardExpander.Content, header)) return true;
        }
        else if (item is SettingsExpander se)
        {
            foreach (var subItem in se.Items)
            {
                if (CheckLogicalMatch(subItem, header)) return true;
            }
        }
        else if (item is ContentControl cc && cc.Content != null)
        {
            if (CheckLogicalMatch(cc.Content, header)) return true;
        }
        else if (item is ItemsControl ic)
        {
            foreach (var childItem in ic.Items)
            {
                if (CheckLogicalMatch(childItem, header)) return true;
            }
        }
        return false;
    }

    private bool ExpandParentExpanders(DependencyObject element)
    {
        bool expandedAny = false;
        var parent = VisualTreeHelper.GetParent(element);
        while (parent != null)
        {
            if (parent is SettingsExpander settingsExpander && !settingsExpander.IsExpanded)
            {
                settingsExpander.IsExpanded = true;
                expandedAny = true;
            }
            else if (parent is Expander expander && !expander.IsExpanded)
            {
                expander.IsExpanded = true;
                expandedAny = true;
            }
            parent = VisualTreeHelper.GetParent(parent);
        }
        return expandedAny;
    }

    private void HighlightControl(FrameworkElement element)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        var overlayVisual = compositor.CreateSpriteVisual();
        overlayVisual.Size = new System.Numerics.Vector2((float)element.ActualWidth, (float)element.ActualHeight);
        
        var accentColor = (Windows.UI.Color)Application.Current.Resources["SystemAccentColor"];
        // Use a semi-transparent color for the overlay tint
        var highlightColor = Windows.UI.Color.FromArgb(80, accentColor.R, accentColor.G, accentColor.B);
        overlayVisual.Brush = compositor.CreateColorBrush(highlightColor);
        
        // Add rounded corners clip
        float radius = 4.0f; // Default WinUI corner radius
        if (element is Control control && control.CornerRadius.TopLeft > 0)
        {
            radius = (float)control.CornerRadius.TopLeft;
        }
        else if (element.GetType().Name.Contains("SettingsCard") || element.GetType().Name.Contains("SettingsExpander"))
        {
            // Settings items typically use 4 or 8 in Win11
            radius = 8.0f; 
        }

        var geometry = compositor.CreateRoundedRectangleGeometry();
        geometry.Size = overlayVisual.Size;
        geometry.CornerRadius = new System.Numerics.Vector2(radius, radius);
        overlayVisual.Clip = compositor.CreateGeometricClip(geometry);

        // Start with opacity 0
        overlayVisual.Opacity = 0.0f;

        // Insert the visual into the element's visual tree
        ElementCompositionPreview.SetElementChildVisual(element, overlayVisual);

        // Update the size if the element is resized during animation
        element.SizeChanged += Element_SizeChanged;

        void Element_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var newSize = new System.Numerics.Vector2((float)e.NewSize.Width, (float)e.NewSize.Height);
            overlayVisual.Size = newSize;
            if (overlayVisual.Clip is CompositionGeometricClip geoClip && geoClip.Geometry is CompositionRoundedRectangleGeometry roundGeo)
            {
                roundGeo.Size = newSize;
            }
        }

        var animation = compositor.CreateScalarKeyFrameAnimation();
        animation.InsertKeyFrame(0.0f, 0.0f);
        animation.InsertKeyFrame(0.5f, 1.0f); // Reach full opacity at half duration
        animation.InsertKeyFrame(1.0f, 0.0f); // Fade out
        animation.Duration = TimeSpan.FromSeconds(1.6);
        animation.IterationBehavior = AnimationIterationBehavior.Count;
        animation.IterationCount = 2;

        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        overlayVisual.StartAnimation("Opacity", animation);
        batch.Completed += (s, e) =>
        {
            element.SizeChanged -= Element_SizeChanged;
            ElementCompositionPreview.SetElementChildVisual(element, null);
            overlayVisual.Dispose();
        };
        batch.End();
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

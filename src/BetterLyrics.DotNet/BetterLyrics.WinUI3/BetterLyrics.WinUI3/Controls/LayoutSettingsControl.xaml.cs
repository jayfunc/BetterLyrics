using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.Json;
using Windows.Storage;
using BetterLyrics.Core.Collections;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.Serialization;
using BetterLyrics.WinUI3.Helpers;
using BetterLyrics.WinUI3.Providers;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class LayoutSettingsControl : UserControl
{
    public static readonly DependencyProperty LayoutProfilesProperty =
        DependencyProperty.Register(nameof(LayoutProfiles), typeof(FullyObservableCollection<LayoutProfile>),
            typeof(LayoutSettingsControl), new PropertyMetadata(default, OnDependencyPropertyChanged));

    private static readonly DependencyProperty SelectedLayoutProfileProperty =
        DependencyProperty.Register(nameof(SelectedLayoutProfile), typeof(LayoutProfile),
            typeof(LayoutSettingsControl), new PropertyMetadata(default));

    private static readonly DependencyProperty EditingLayoutProfileProperty =
        DependencyProperty.Register(nameof(EditingLayoutProfile), typeof(LayoutProfile),
            typeof(LayoutSettingsControl), new PropertyMetadata(default));

    public static readonly DependencyProperty LyricsWindowStatusProperty =
        DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus),
            typeof(LayoutSettingsControl), new PropertyMetadata(default, OnDependencyPropertyChanged));

    private readonly IGlobalToastProvider _globalToastProvider =
        Ioc.Default.GetRequiredService<IGlobalToastProvider>();

    private readonly ILocalizationService _localizationService =
        Ioc.Default.GetRequiredService<ILocalizationService>();

    private readonly IFilePickerProvider _filePickerProvider =
        Ioc.Default.GetRequiredService<IFilePickerProvider>();

    private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

    private bool _isSyncing;
    private bool _isUpdatingSelection;

    public LayoutSettingsControl()
    {
        InitializeComponent();
    }

    public ObservableCollection<LayoutProfile> SystemProfiles { get; } = new();
    public ObservableCollection<LayoutProfile> CustomProfiles { get; } = new();

    public FullyObservableCollection<LayoutProfile> LayoutProfiles
    {
        get => (FullyObservableCollection<LayoutProfile>)GetValue(LayoutProfilesProperty);
        set => SetValue(LayoutProfilesProperty, value);
    }

    private LayoutProfile SelectedLayoutProfile
    {
        get => (LayoutProfile)GetValue(SelectedLayoutProfileProperty);
        set => SetValue(SelectedLayoutProfileProperty, value);
    }

    private LayoutProfile EditingLayoutProfile
    {
        get => (LayoutProfile)GetValue(EditingLayoutProfileProperty);
        set => SetValue(EditingLayoutProfileProperty, value);
    }

    public LyricsWindowStatus LyricsWindowStatus
    {
        get => (LyricsWindowStatus)GetValue(LyricsWindowStatusProperty);
        set => SetValue(LyricsWindowStatusProperty, value);
    }

    private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LayoutSettingsControl control)
        {
            if (e.Property == LyricsWindowStatusProperty)
            {
                var newStatus = e.NewValue as LyricsWindowStatus;
                if (newStatus != null)
                    control.SelectedLayoutProfile =
                        control.LayoutProfiles?.FirstOrDefault(x => x.Id == newStatus.LayoutProfileId);
            }
            else if (e.Property == LayoutProfilesProperty)
            {
                if (e.OldValue is INotifyCollectionChanged oldList)
                    oldList.CollectionChanged -= control.LayoutProfiles_CollectionChanged;

                if (e.NewValue is INotifyCollectionChanged newList)
                    newList.CollectionChanged += control.LayoutProfiles_CollectionChanged;

                control.RefreshGroupedProfiles();
            }
        }
    }

    private void LayoutProfiles_CollectionChanged(object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        if (_isSyncing) return;
        RefreshGroupedProfiles();
    }

    private void RefreshGroupedProfiles()
    {
        if (LayoutProfiles == null) return;

        var currentSelection = SelectedLayoutProfile;

        SystemProfiles.Clear();
        CustomProfiles.Clear();

        foreach (var profile in LayoutProfiles)
            if (profile.Mode == NowPlayingLayoutMode.Custom)
                CustomProfiles.Add(profile);
            else
                SystemProfiles.Add(profile);

        if (currentSelection != null)
        {
            _isUpdatingSelection = true;

            if (currentSelection.Mode == NowPlayingLayoutMode.Custom)
            {
                if (CustomProfilesListView != null)
                    CustomProfilesListView.SelectedItem = currentSelection;
            }
            else
            {
                if (SystemProfilesListView != null)
                    SystemProfilesListView.SelectedItem = currentSelection;
            }

            _isUpdatingSelection = false;
        }
    }

    private void SystemProfilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingSelection) return;

        if (sender is ListView listView && listView.SelectedItem != null)
        {
            _isUpdatingSelection = true;

            if (CustomProfilesListView != null) CustomProfilesListView.SelectedItem = null;
            UpdateSelectedProfile((LayoutProfile)listView.SelectedItem);

            _isUpdatingSelection = false;
        }
    }

    private void CustomProfilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingSelection) return;

        if (sender is ListView listView && listView.SelectedItem != null)
        {
            _isUpdatingSelection = true;

            if (SystemProfilesListView != null) SystemProfilesListView.SelectedItem = null;
            UpdateSelectedProfile((LayoutProfile)listView.SelectedItem);

            _isUpdatingSelection = false;
        }
    }

    private void UpdateSelectedProfile(LayoutProfile profile)
    {
        SelectedLayoutProfile = profile;
        if (LyricsWindowStatus != null) LyricsWindowStatus.LayoutProfileId = profile.Id;
    }

    private void CustomProfilesListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        if (LayoutProfiles == null) return;

        _isSyncing = true;

        var systemItems = LayoutProfiles.Where(p => p.Mode != NowPlayingLayoutMode.Custom).ToList();

        LayoutProfiles.Clear();

        foreach (var p in systemItems) LayoutProfiles.Add(p);
        foreach (var p in CustomProfiles) LayoutProfiles.Add(p);

        LayoutProfiles.Refresh();

        _isSyncing = false;
    }

    private void ConfigButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement button && button.DataContext is LayoutProfile clickedProfile)
        {
            EditingLayoutProfile = clickedProfile;
            ConfigPanel.Show();
        }
    }

    private void LayoutProfilesListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        LayoutProfiles.Refresh();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        ConfigPanel.Hide();
        EditingLayoutProfile = null;
    }

    private void DeleteMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuFlyoutItem)
            if (menuFlyoutItem.DataContext is LayoutProfile data)
                _settingsService.AppSettings.LayoutProfiles.Remove(data);
    }

    private void MenuBarItemFlyout_Opened(object sender, object e)
    {
        var menuFlyout = (MenuFlyout)sender;
        var menuFlyoutSubItem = (MenuFlyoutSubItem)menuFlyout.Items.Last();
        var layoutProfile = (LayoutProfile)menuFlyoutSubItem.DataContext;
        var status =
            _settingsService.AppSettings.WindowBoundsRecords.Where(x => x.LayoutProfileId == layoutProfile.Id);

        menuFlyoutSubItem.Items.Clear();
        foreach (var item in status)
            menuFlyoutSubItem.Items.Add(new MenuFlyoutItem { Text = $"{item.Name} ({item.MonitorDeviceName})" });

        if (!status.Any())
            menuFlyoutSubItem.Items.Add(new MenuFlyoutItem
                { Text = _localizationService.GetLocalizedString("LayoutSettingsControlNoLyricsWindow") });

        var deleteMenuFlyoutItem = (MenuFlyoutItem)menuFlyout.Items[2];
        deleteMenuFlyoutItem.IsEnabled = !status.Any();
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        string[] fileTypeFilter = [".json"];
        string? filePath;
        if (Parent is FlyoutPresenter)
            (_, filePath) = await _filePickerProvider.PickSingleFileAsync(fileTypeFilter, WindowType.NowPlayingWindow);
        else
            (_, filePath) = await _filePickerProvider.PickSingleFileAsync(fileTypeFilter, WindowType.SettingsWindow);

        if (filePath != null)
        {
            var json = await File.ReadAllTextAsync(filePath);
            var data = JsonSerializer.Deserialize(json,
                SourceGenerationContext.Default.LayoutProfile);
            if (data != null)
            {
                data.Id = Guid.NewGuid(); // Ensure the imported profile has a unique ID
                _settingsService.AppSettings.LayoutProfiles.Add(data);
                _globalToastProvider.Show("ImportSettingsSuccess", null, MessageSeverity.Success);
            }
        }
    }

    private void CopyMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement frameworkElement)
            if (frameworkElement.DataContext is LayoutProfile data)
            {
                var clonedData = (LayoutProfile)data.Clone();
                _settingsService.AppSettings.LayoutProfiles.Add(clonedData);
            }
    }

    private async void ExportMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement frameworkElement)
            if (frameworkElement.DataContext is LayoutProfile data)
            {
                IDictionary<string, IList<string>> fileTypeChoices = new Dictionary<string, IList<string>>
                {
                    { "JSON", new List<string> { ".json" } }
                };
                var suggestedFileName = $"BetterLyrics_LayoutProfile_{data.Name}.json";
                string? filePath;
                if (Parent is FlyoutPresenter)
                    (_, filePath) = await _filePickerProvider.PickSaveFileAsync(
                        fileTypeChoices,
                        suggestedFileName, WindowType.NowPlayingWindow);
                else
                    (_, filePath) = await _filePickerProvider.PickSaveFileAsync(
                        fileTypeChoices,
                        suggestedFileName, WindowType.SettingsWindow);

                if (filePath != null)
                {
                    var clonedData = (LayoutProfile)data.Clone();
                    var json = JsonSerializer.Serialize(clonedData,
                        SourceGenerationContext.Default.LayoutProfile);
                    await File.WriteAllTextAsync(filePath, json);
                    _globalToastProvider.Show("ExportSettingsSuccess", null, MessageSeverity.Success);
                }
            }
    }

    private void RootLayoutSettings_Loaded(object sender, RoutedEventArgs e)
    {
        if (CreateFromTemplatesMenuFlyout.Items.Count == 0)
            foreach (var mode in Enum.GetValues<NowPlayingLayoutMode>())
                if (mode != NowPlayingLayoutMode.Custom)
                {
                    var item = new MenuFlyoutItem
                        { Text = _localizationService.GetLocalizedString($"{mode}Layout"), Tag = mode };
                    item.Click += CreateFromTemplateMenuFlyoutItem_Click;
                    CreateFromTemplatesMenuFlyout.Items.Add(item);
                }
    }

    private void CreateFromTemplateMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is NowPlayingLayoutMode mode)
        {
            var newProfile = new LayoutProfile(mode)
            {
                Mode = NowPlayingLayoutMode.Custom // Set to Custom so that it can be edited
            };
            _settingsService.AppSettings.LayoutProfiles.Add(newProfile);
        }
    }
}
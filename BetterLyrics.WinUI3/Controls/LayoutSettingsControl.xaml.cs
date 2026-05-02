using BetterLyrics.WinUI3.Collections;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Serialization;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.Storage;

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class LayoutSettingsControl : UserControl
    {
        private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        private readonly ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

        public ObservableCollection<LayoutProfile> SystemProfiles { get; } = new();
        public ObservableCollection<LayoutProfile> CustomProfiles { get; } = new();

        private bool _isSyncing = false;
        private bool _isUpdatingSelection = false;

        public static readonly DependencyProperty LayoutProfilesProperty =
            DependencyProperty.Register(nameof(LayoutProfiles), typeof(FullyObservableCollection<LayoutProfile>), typeof(LayoutSettingsControl), new PropertyMetadata(default, OnDependencyPropertyChanged));

        public FullyObservableCollection<LayoutProfile> LayoutProfiles
        {
            get => (FullyObservableCollection<LayoutProfile>)GetValue(LayoutProfilesProperty);
            set => SetValue(LayoutProfilesProperty, value);
        }

        private static readonly DependencyProperty SelectedLayoutProfileProperty =
            DependencyProperty.Register(nameof(SelectedLayoutProfile), typeof(LayoutProfile), typeof(LayoutSettingsControl), new PropertyMetadata(default));

        private LayoutProfile SelectedLayoutProfile
        {
            get => (LayoutProfile)GetValue(SelectedLayoutProfileProperty);
            set => SetValue(SelectedLayoutProfileProperty, value);
        }

        private static readonly DependencyProperty EditingLayoutProfileProperty =
            DependencyProperty.Register(nameof(EditingLayoutProfile), typeof(LayoutProfile), typeof(LayoutSettingsControl), new PropertyMetadata(default));

        private LayoutProfile EditingLayoutProfile
        {
            get => (LayoutProfile)GetValue(EditingLayoutProfileProperty);
            set => SetValue(EditingLayoutProfileProperty, value);
        }

        public static readonly DependencyProperty LyricsWindowStatusProperty =
            DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus), typeof(LayoutSettingsControl), new PropertyMetadata(default, OnDependencyPropertyChanged));

        public LyricsWindowStatus LyricsWindowStatus
        {
            get => (LyricsWindowStatus)GetValue(LyricsWindowStatusProperty);
            set => SetValue(LyricsWindowStatusProperty, value);
        }

        private static readonly DependencyProperty SwitchPresenterValueProperty =
            DependencyProperty.Register(nameof(SwitchPresenterValue), typeof(int), typeof(LayoutSettingsControl), new PropertyMetadata(0));

        private int SwitchPresenterValue
        {
            get => (int)GetValue(SwitchPresenterValueProperty);
            set => SetValue(SwitchPresenterValueProperty, value);
        }

        public LayoutSettingsControl()
        {
            InitializeComponent();
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LayoutSettingsControl control)
            {
                if (e.Property == LyricsWindowStatusProperty)
                {
                    var newStatus = e.NewValue as LyricsWindowStatus;
                    if (newStatus != null)
                    {
                        control.SelectedLayoutProfile = control.LayoutProfiles?.FirstOrDefault(x => x.Id == newStatus.LayoutProfileId);
                    }
                }
                else if (e.Property == LayoutProfilesProperty)
                {
                    if (e.OldValue is System.Collections.Specialized.INotifyCollectionChanged oldList)
                        oldList.CollectionChanged -= control.LayoutProfiles_CollectionChanged;

                    if (e.NewValue is System.Collections.Specialized.INotifyCollectionChanged newList)
                        newList.CollectionChanged += control.LayoutProfiles_CollectionChanged;

                    control.RefreshGroupedProfiles();
                }
            }
        }

        private void LayoutProfiles_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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
            {
                if (profile.Mode == NowPlayingLayoutMode.Custom)
                    CustomProfiles.Add(profile);
                else
                    SystemProfiles.Add(profile);
            }

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
            if (LyricsWindowStatus != null)
            {
                LyricsWindowStatus.LayoutProfileId = profile.Id;
            }
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
                SwitchPresenterValue = 1;
            }
        }

        private void LayoutProfilesListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            LayoutProfiles.Refresh();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchPresenterValue = 0;
            EditingLayoutProfile = null;
        }

        private void DeleteMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuFlyoutItem)
            {
                if (menuFlyoutItem.DataContext is LayoutProfile data)
                {
                    _settingsService.AppSettings.LayoutProfiles.Remove(data);
                }
            }
        }

        private void MenuBarItemFlyout_Opened(object sender, object e)
        {
            var menuFlyout = (MenuFlyout)sender;
            var menuFlyoutSubItem = (MenuFlyoutSubItem)menuFlyout.Items.Last();
            var layoutProfile = (LayoutProfile)menuFlyoutSubItem.DataContext;
            var status = _settingsService.AppSettings.WindowBoundsRecords.Where(x => x.LayoutProfileId == layoutProfile.Id);

            menuFlyoutSubItem.Items.Clear();
            foreach (var item in status)
            {
                menuFlyoutSubItem.Items.Add(new MenuFlyoutItem() { Text = $"{item.Name} ({item.MonitorDeviceName})" });
            }
            if (!status.Any())
            {
                menuFlyoutSubItem.Items.Add(new MenuFlyoutItem() { Text = _localizationService.GetLocalizedString("LayoutSettingsControlNoLyricsWindow") });
            }

            var deleteMenuFlyoutItem = (MenuFlyoutItem)menuFlyout.Items[2];
            deleteMenuFlyoutItem.IsEnabled = !status.Any();
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            string[] fileTypeFilter = [".json"];
            StorageFile? file;
            if (this.Parent is FlyoutPresenter)
            {
                file = await PickerHelper.PickSingleFileAsync<NowPlayingWindow>(fileTypeFilter);
            }
            else
            {
                file = await PickerHelper.PickSingleFileAsync<SettingsWindow>(fileTypeFilter);
            }
            if (file != null)
            {
                var json = File.ReadAllText(file.Path);
                var data = System.Text.Json.JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LayoutProfile);
                if (data != null)
                {
                    data.Id = System.Guid.NewGuid(); // Ensure the imported profile has a unique ID
                    _settingsService.AppSettings.LayoutProfiles.Add(data);
                    GlobalToastManager.Show("ImportSettingsSuccess", null, InfoBarSeverity.Success);
                }
            }
        }

        private void CopyMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement)
            {
                if (frameworkElement.DataContext is LayoutProfile data)
                {
                    var clonedData = (LayoutProfile)data.Clone();
                    _settingsService.AppSettings.LayoutProfiles.Add(clonedData);
                }
            }
        }

        private async void ExportMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement)
            {
                if (frameworkElement.DataContext is LayoutProfile data)
                {
                    IDictionary<string, IList<string>> fileTypeChoices = new Dictionary<string, IList<string>>()
                    {
                        { "JSON", new List<string>() { ".json" } }
                    };
                    var suggestedFileName = $"BetterLyrics_LayoutProfile_{data.Name}.json";
                    StorageFile? file;
                    if (this.Parent is FlyoutPresenter)
                    {
                        file = await PickerHelper.PickSaveFileAsync<NowPlayingWindow>(fileTypeChoices, suggestedFileName);
                    }
                    else
                    {
                        file = await PickerHelper.PickSaveFileAsync<SettingsWindow>(fileTypeChoices, suggestedFileName);
                    }
                    if (file != null)
                    {
                        var clonedData = (LayoutProfile)data.Clone();
                        var json = System.Text.Json.JsonSerializer.Serialize(clonedData, SourceGenerationContext.Default.LayoutProfile);
                        File.WriteAllText(file.Path, json);
                        GlobalToastManager.Show("ExportSettingsSuccess", null, InfoBarSeverity.Success);
                    }
                }
            }
        }

        private void RootLayoutSettings_Loaded(object sender, RoutedEventArgs e)
        {
            if (CreateFromTemplatesMenuFlyout.Items.Count == 0)
            {
                foreach (var mode in Enum.GetValues<NowPlayingLayoutMode>().Cast<NowPlayingLayoutMode>())
                {
                    if (mode != NowPlayingLayoutMode.Custom)
                    {
                        var item = new MenuFlyoutItem() { Text = _localizationService.GetLocalizedString($"{mode}Layout"), Tag = mode };
                        item.Click += CreateFromTemplateMenuFlyoutItem_Click;
                        CreateFromTemplatesMenuFlyout.Items.Add(item);
                    }
                }
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
}
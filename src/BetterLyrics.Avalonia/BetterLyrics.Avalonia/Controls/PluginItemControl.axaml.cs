using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Sdk.Models.SettingsSchema;
using CommunityToolkit.Mvvm.DependencyInjection;
using FluentAvalonia.UI.Controls;
using System;
using System.Collections.Generic;

namespace BetterLyrics.Avalonia.Controls;

public partial class PluginItemControl : UserControl
{
    public static readonly StyledProperty<PluginInfo?> PluginInfoProperty =
        AvaloniaProperty.Register<PluginItemControl, PluginInfo?>(nameof(PluginInfo));

    private readonly IPluginService _pluginService = Ioc.Default.GetRequiredService<IPluginService>();

    public PluginItemControl()
    {
        InitializeComponent();
    }

    public PluginInfo? PluginInfo
    {
        get => GetValue(PluginInfoProperty);
        set => SetValue(PluginInfoProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PluginInfoProperty) RebuildItems();
    }

    private void RebuildItems()
    {
        RootExpander.Items?.Clear();
        if (PluginInfo is { IsEnabled: true, Plugin: not null })
        {
            foreach (var kvp in PluginInfo.Plugin.GetSettingDefDict())
            {
                if (CreateSettingCard(kvp) is { } card) RootExpander.Items?.Add(card);
            }
        }
    }

    private FASettingsExpander? CreateSettingCard(KeyValuePair<string, SettingDef> kvp)
    {
        var def = kvp.Value;
        Control? inputControl = def switch
        {
            TextSettingDef txt => new TextBox { Text = def.Value?.ToString(), MinWidth = 200 }.Init(tb => tb.TextChanged += (s, e) => UpdateSetting(def.Key, tb.Text)),
            BoolSettingDef b => new ToggleSwitch { IsChecked = (bool)(def.Value ?? false) }.Init(ts => ts.IsCheckedChanged += (s, e) => UpdateSetting(def.Key, ts.IsChecked)),
            ChoiceSettingDef ch => new ComboBox { ItemsSource = ch.Options, MinWidth = 150, SelectedItem = def.Value }.Init(cb => cb.SelectionChanged += (s, e) => UpdateSetting(def.Key, cb.SelectedItem)),
            NumberSettingDef num => new NumericUpDown { Value = (decimal?)Convert.ToDouble(def.Value ?? 0), Minimum = (decimal)num.Min, Maximum = (decimal)num.Max, MinWidth = 120 }.Init(nb => nb.ValueChanged += (s, e) => UpdateSetting(def.Key, nb.Value)),
            ActionSettingDef act => new Button { Content = act.ButtonText }.Init(btn => btn.Click += (s, e) => act.Action?.Invoke(act.Key)),
            _ => null
        };

        return inputControl == null ? null : new FASettingsExpander { Header = def.Header, Description = def.Description, Footer = inputControl };
    }

    private void UpdateSetting(string key, object? value) => _pluginService.SetSettingItem(PluginInfo!.Id, key, value!);

    private void UninstallClick(object? sender, RoutedEventArgs e) => UninstallClicked?.Invoke(this, e);

    private void ToggleSwitch_Toggled(object? sender, RoutedEventArgs e) => RebuildItems();

    public event EventHandler<RoutedEventArgs>? UninstallClicked;
}

// 辅助扩展方法，让构造代码更简洁
public static class ControlExtensions
{
    public static T Init<T>(this T control, Action<T> action) where T : Control
    {
        action(control);
        return control;
    }
}
using System;
using System.Collections.Generic;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Sdk.Models.SettingsSchema;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TextBox = Microsoft.UI.Xaml.Controls.TextBox;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class PluginItemControl : UserControl
{
    public static readonly DependencyProperty PluginInfoProperty =
        DependencyProperty.Register(nameof(PluginInfo), typeof(PluginInfo), typeof(PluginItemControl),
            new PropertyMetadata(null, OnPluginInfoChanged));

    private readonly IPluginService _pluginService;

    public PluginItemControl()
    {
        InitializeComponent();
        _pluginService = Ioc.Default.GetRequiredService<IPluginService>();
    }

    public PluginInfo PluginInfo
    {
        get => (PluginInfo)GetValue(PluginInfoProperty);
        set => SetValue(PluginInfoProperty, value);
    }

    public event RoutedEventHandler? UninstallClicked;

    private static void OnPluginInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PluginItemControl control && e.NewValue is PluginInfo) control.RebuildItems();
    }

    private void RebuildItems()
    {
        RootExpander.Items.Clear();

        if (PluginInfo == null) return;

        if (PluginInfo.IsEnabled)
        {
            var configDict = PluginInfo.Plugin?.GetSettingDefDict();
            if (configDict == null) return;

            foreach (var kvp in configDict)
            {
                var dynamicCard = CreateSettingCard(kvp);
                if (dynamicCard != null) RootExpander.Items.Add(dynamicCard);
            }
        }
    }

    private SettingsCard? CreateSettingCard(KeyValuePair<string, SettingDef> kvp)
    {
        var def = kvp.Value;
        var currentVal = def.Value;

        FrameworkElement? inputControl = null;

        switch (def)
        {
            case TextSettingDef txt:
                var textBox = new TextBox
                {
                    Text = currentVal?.ToString() ?? "",
                    MinWidth = 200
                };
                textBox.TextChanged += (s, e) => UpdateSetting(def.Key, textBox.Text);
                inputControl = textBox;
                break;

            case BoolSettingDef b:
                var toggle = new ToggleSwitch { IsOn = Convert.ToBoolean(currentVal) };
                toggle.Toggled += (s, e) => UpdateSetting(def.Key, toggle.IsOn);
                inputControl = toggle;
                break;

            case ChoiceSettingDef ch:
                var combo = new ComboBox { ItemsSource = ch.Options, MinWidth = 150 };
                combo.SelectedItem = currentVal?.ToString();
                if (combo.SelectedItem == null && ch.Options.Count > 0) combo.SelectedIndex = 0;

                combo.SelectionChanged += (s, e) => UpdateSetting(def.Key, combo.SelectedItem);
                inputControl = combo;
                break;

            case NumberSettingDef num:
                var numBox = new NumberBox
                {
                    Value = Convert.ToDouble(currentVal),
                    Minimum = num.Min,
                    Maximum = num.Max,
                    SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                    MinWidth = 120
                };
                numBox.ValueChanged += (s, e) => UpdateSetting(def.Key, numBox.Value);
                inputControl = numBox;
                break;

            case ActionSettingDef act:
                var btn = new Button { Content = act.ButtonText };
                btn.Click += (s, e) => act.Action?.Invoke(act.Key);
                inputControl = btn;
                break;
        }

        if (inputControl == null) return null;

        return new SettingsCard
        {
            Header = def.Header,
            Description = def.Description,
            Content = inputControl
        };
    }

    private void UpdateSetting(string key, object value)
    {
        _pluginService.SetSettingItem(PluginInfo.Id, key, value);
    }

    private void UninstallClick(object sender, RoutedEventArgs e)
    {
        UninstallClicked?.Invoke(this, e);
    }

    private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        RebuildItems();
    }
}
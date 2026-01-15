using BetterLyrics.Core.Models.SettingsSchema;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.PluginService;
using CommunityToolkit.Mvvm.DependencyInjection;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using TextBox = Microsoft.UI.Xaml.Controls.TextBox;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class PluginItemControl : UserControl
    {
        private readonly IPluginService _pluginService;

        public static readonly DependencyProperty PluginInfoProperty =
            DependencyProperty.Register(nameof(PluginInfo), typeof(PluginInfo), typeof(PluginItemControl), new PropertyMetadata(null, OnPluginInfoChanged));

        public PluginInfo PluginInfo
        {
            get => (PluginInfo)GetValue(PluginInfoProperty);
            set => SetValue(PluginInfoProperty, value);
        }

        public event RoutedEventHandler? UninstallClicked;

        public PluginItemControl()
        {
            InitializeComponent();
            _pluginService = Ioc.Default.GetRequiredService<IPluginService>();
        }

        private static void OnPluginInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PluginItemControl control && e.NewValue is PluginInfo)
            {
                control.RebuildItems();
            }
        }

        private void RebuildItems()
        {
            RootExpander.Items.Clear();

            if (PluginInfo == null) return;

            var enableSwitch = new ToggleSwitch
            {
                IsOn = PluginInfo.IsEnabled
            };

            enableSwitch.Toggled += async (s, e) =>
            {
                enableSwitch.IsEnabled = false;

                try
                {
                    await _pluginService.TogglePluginAsync(PluginInfo.Id, enableSwitch.IsOn);
                    PluginInfo.IsEnabled = enableSwitch.IsOn;
                    RebuildItems();
                }
                catch (Exception ex)
                {
                    enableSwitch.IsOn = !enableSwitch.IsOn;
                }
                finally
                {
                    enableSwitch.IsEnabled = true;
                }
            };

            var enableCard = new SettingsCard
            {
                Header = "Enable Plugin",
                Description = "Toggle to enable or disable this plugin.",
                Content = enableSwitch
            };
            RootExpander.Items.Add(enableCard);

            if (PluginInfo.IsEnabled && PluginInfo.SettingsDefinitions != null)
            {
                foreach (var def in PluginInfo.SettingsDefinitions)
                {
                    var dynamicCard = CreateSettingCard(def);
                    if (dynamicCard != null)
                    {
                        RootExpander.Items.Add(dynamicCard);
                    }
                }
            }

            var uninstallBtn = new Button { Content = "Uninstall" };
            uninstallBtn.Click += (s, e) => UninstallClicked?.Invoke(this, e);

            var uninstallCard = new SettingsCard
            {
                Header = "Uninstall",
                Description = "Remove this plugin and its data.",
                Content = uninstallBtn,
                IsClickEnabled = true,
                ActionIconToolTip = "Uninstall"
            };

            uninstallCard.Click += (s, e) => UninstallClicked?.Invoke(this, e);

            RootExpander.Items.Add(uninstallCard);
        }

        private SettingsCard? CreateSettingCard(SettingDef def)
        {
            var currentVal = PluginInfo.GetSetting<object>(def.Key, def.DefaultValue);

            FrameworkElement inputControl = null;

            switch (def)
            {
                case TextSettingDef txt:
                    var textBox = new TextBox
                    {
                        Text = currentVal?.ToString() ?? "",
                        MinWidth = 200,
                    };
                    textBox.TextChanged += (s, e) => UpdateSetting(def.Key, textBox.Text);
                    inputControl = textBox;
                    break;

                case BoolSettingDef b:
                    var toggle = new ToggleSwitch { IsOn = System.Convert.ToBoolean(currentVal) };
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
                        Value = System.Convert.ToDouble(currentVal),
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
                    btn.Click += (s, e) => act.Action?.Invoke(PluginInfo.Settings);
                    inputControl = btn;
                    break;
            }

            if (inputControl == null) return null;

            return new SettingsCard
            {
                Header = def.Label,
                Description = def.Description,
                Content = inputControl
            };
        }

        private void UpdateSetting(string key, object value)
        {
            if (PluginInfo != null)
            {
                PluginInfo.Settings[key] = value;
            }
        }

    }
}

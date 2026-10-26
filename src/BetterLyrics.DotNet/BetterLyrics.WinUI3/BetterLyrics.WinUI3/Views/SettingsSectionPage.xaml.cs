using BetterLyrics.Core.Enums;
using BetterLyrics.WinUI3.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace BetterLyrics.WinUI3.Views;

public sealed partial class SettingsSectionPage : BasePage
{
    public SettingsSection Section { get; private set; }

    public SettingsSectionPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is not SettingsSection section)
        {
            throw new ArgumentException("A settings section is required.", nameof(e));
        }

        Section = section;
        ContentRoot.Children.Add(CreateSectionControl(section));
        base.OnNavigatedTo(e);
    }

    protected override void BindGeneratedControls()
    {
    }

    protected override void UnbindGeneratedControls()
    {
    }

    private static Control CreateSectionControl(SettingsSection section) => section switch
    {
        SettingsSection.App => new AppSettingsControl(),
        SettingsSection.LyricsWindowMgr => new LyricsWindowManagerControl(),
        SettingsSection.MediaLib => new MediaSettingsControl(),
        SettingsSection.PlaybackLib => new PlaybackSettingsControl(),
        SettingsSection.Plugins => new PluginManagerControl(),
        SettingsSection.About => new AboutControl(),
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, "Unknown settings section.")
    };
}

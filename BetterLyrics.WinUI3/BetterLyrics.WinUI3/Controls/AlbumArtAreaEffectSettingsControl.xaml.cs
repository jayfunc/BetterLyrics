using BetterLyrics.WinUI3.Models.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class AlbumArtAreaEffectSettingsControl : UserControl
{
    public static readonly DependencyProperty AlbumArtAreaEffectSettingsProperty =
    DependencyProperty.Register(nameof(AlbumArtAreaEffectSettings), typeof(AlbumArtAreaEffectSettings), typeof(AlbumArtAreaEffectSettingsControl), new PropertyMetadata(default));

    public AlbumArtAreaEffectSettings AlbumArtAreaEffectSettings
    {
        get => (AlbumArtAreaEffectSettings)GetValue(AlbumArtAreaEffectSettingsProperty);
        set => SetValue(AlbumArtAreaEffectSettingsProperty, value);
    }

    public AlbumArtAreaEffectSettingsControl()
    {
        InitializeComponent();
    }
}

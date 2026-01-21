using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.LocalizationService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class FontPicker : UserControl
    {
        private readonly ILocalizationService _localizationService;

        public FontPicker()
        {
            this.InitializeComponent();
            _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();
        }

        #region Dependency Properties
        public string SelectedFontId
        {
            get => (string)GetValue(SelectedFontIdProperty);
            set => SetValue(SelectedFontIdProperty, value);
        }

        public static readonly DependencyProperty SelectedFontIdProperty =
            DependencyProperty.Register(nameof(SelectedFontId), typeof(string), typeof(FontPicker),
                new PropertyMetadata(null, OnSelectedFontIdChanged));

        private static void OnSelectedFontIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FontPicker)d;
            control.UpdateDisplay((string)e.NewValue);
        }
        #endregion

        private async void UpdateDisplay(string fontId)
        {
            if (string.IsNullOrEmpty(fontId))
            {
                SelectedLocalizedText.Text = "N/A";
                SelectedRawText.Text = "";
                return;
            }

            var fonts = await FontHelper.GetSystemFontFamiliesAsync();
            var match = fonts.FirstOrDefault(f => f.FontFamily == fontId);

            if (match != null)
            {
                SelectedLocalizedText.Text = match.LocalizedFontFamily;
                SelectedRawText.Text = match.FontFamily;
                SelectedLocalizedText.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily(match.FontFamily);
            }
            else
            {
                SelectedLocalizedText.Text = fontId;
                SelectedRawText.Text = "Unknown";
            }
        }

        private async void TriggerButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FontPickerDialog(SelectedFontId);
            dialog.PrimaryButtonText = _localizationService.GetLocalizedString("Cancel");
            dialog.XamlRoot = this.XamlRoot;
            await dialog.ShowAsync();

            if (!string.IsNullOrEmpty(dialog.SelectedFontId))
            {
                this.SelectedFontId = dialog.SelectedFontId;
            }
        }
    }
}
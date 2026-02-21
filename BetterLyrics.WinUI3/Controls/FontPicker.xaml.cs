using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.LocalizationService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                new PropertyMetadata(string.Empty, OnSelectedFontIdChanged));

        private static void OnSelectedFontIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FontPicker)d;
            _ = control.UpdateDisplayAsync((string)e.NewValue);
        }

        #endregion

        private async Task UpdateDisplayAsync(string fontIdString)
        {
            SelectedLocalizedText.Text = string.Empty;
            SelectedLocalizedText.Inlines.Clear();

            if (string.IsNullOrWhiteSpace(fontIdString))
            {
                SelectedLocalizedText.Text = "N/A";
                SelectedRawText.Text = "";
                return;
            }

            var fontIds = fontIdString
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

            var fonts = await FontHelper.GetSystemFontFamiliesAsync();
            var matchedFonts = fontIds.Select(id => fonts.FirstOrDefault(f => f.FontFamily == id)).Where(f => f != null).ToList();

            if (matchedFonts.Any())
            {
                SelectedRawText.Text = string.Join(", ", matchedFonts.Select(f => f.FontFamily));

                for (int i = 0; i < matchedFonts.Count; i++)
                {
                    var f = matchedFonts[i];

                    var fontRun = new Microsoft.UI.Xaml.Documents.Run
                    {
                        Text = f.LocalizedFontFamily,
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily(f.FontFamily)
                    };
                    SelectedLocalizedText.Inlines.Add(fontRun);

                    if (i < matchedFonts.Count - 1)
                    {
                        var separatorRun = new Microsoft.UI.Xaml.Documents.Run
                        {
                            Text = ", "
                        };
                        SelectedLocalizedText.Inlines.Add(separatorRun);
                    }
                }
            }
            else
            {
                SelectedLocalizedText.Text = fontIdString;
                SelectedRawText.Text = "Unknown";
            }
        }

        private async void TriggerButton_Click(object sender, RoutedEventArgs e)
        {
            var currentFontsList = string.IsNullOrWhiteSpace(SelectedFontId)
                ? new List<string>()
                : SelectedFontId.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .ToList();

            var dialog = new FontPickerDialog(currentFontsList)
            {
                XamlRoot = this.XamlRoot,
                PrimaryButtonText = _localizationService.GetLocalizedString("Confirm"),
                CloseButtonText = _localizationService.GetLocalizedString("Cancel")
            };

            var result = await dialog.ShowAsync(ContentDialogPlacement.Popup);

            if (result == ContentDialogResult.Primary)
            {
                this.SelectedFontId = string.Join(", ", dialog.SelectedFontIds);
            }
        }
    }
}
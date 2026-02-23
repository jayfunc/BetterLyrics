using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
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
            if (string.IsNullOrWhiteSpace(fontIdString))
            {
                SelectedFontsItemsControl.ItemsSource = new List<ExtendedFontFamily>
                {
                    new ExtendedFontFamily
                    {
                        LocalizedFontFamily = "Segoe UI",
                        FontFamily = "Segoe UI"
                    }
                };
                return;
            }

            var fontIds = fontIdString
                .Split([','], StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

            var fonts = await FontHelper.GetSystemFontFamiliesAsync();
            var displayItems = new List<ExtendedFontFamily>();

            if (fontIds.Count != 0)
            {
                foreach (var fontId in fontIds)
                {
                    var matchedFont = fonts.FirstOrDefault(f => f.FontFamily == fontId);

                    if (matchedFont != null)
                    {
                        displayItems.Add(matchedFont);
                    }
                    else
                    {
                        displayItems.Add(new ExtendedFontFamily
                        {
                            LocalizedFontFamily = fontId,
                            FontFamily = fontId
                        });
                    }
                }
            }
            else
            {
                displayItems.Add(new ExtendedFontFamily
                {
                    LocalizedFontFamily = fontIdString,
                    FontFamily = "Unknown"
                });
            }

            SelectedFontsItemsControl.ItemsSource = displayItems;
        }

        private async void TriggerButton_Click(object sender, RoutedEventArgs e)
        {
            var currentFontsList = string.IsNullOrWhiteSpace(SelectedFontId)
                ? new List<string>()
                : SelectedFontId
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
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
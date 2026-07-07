using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.WinUI3.Helpers;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class FontPicker : UserControl
{
    public static readonly DependencyProperty SelectedFontIdProperty =
        DependencyProperty.Register(nameof(SelectedFontId), typeof(string), typeof(FontPicker),
            new PropertyMetadata(string.Empty, OnSelectedFontIdChanged));

    public static readonly DependencyProperty AllowMultipleSelectionProperty =
        DependencyProperty.Register(nameof(AllowMultipleSelection), typeof(bool), typeof(FontPicker),
            new PropertyMetadata(true, OnAllowMultipleSelectionChanged));

    private readonly ILocalizationService _localizationService;

    public FontPicker()
    {
        InitializeComponent();
        _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

        LoadingTextBlock.Text = _localizationService.GetLocalizedString("Loading");
    }

    public string SelectedFontId
    {
        get => (string)GetValue(SelectedFontIdProperty);
        set => SetValue(SelectedFontIdProperty, value);
    }

    public bool AllowMultipleSelection
    {
        get => (bool)GetValue(AllowMultipleSelectionProperty);
        set => SetValue(AllowMultipleSelectionProperty, value);
    }

    private static void OnSelectedFontIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (FontPicker)d;
        _ = control.UpdateDisplayAsync((string)e.NewValue);
    }

    private static void OnAllowMultipleSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (FontPicker)d;
        var isMultiAllowed = (bool)e.NewValue;

        if (!isMultiAllowed && !string.IsNullOrWhiteSpace(control.SelectedFontId))
        {
            var fontIds = control.SelectedFontId.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (fontIds.Length > 1) control.SelectedFontId = fontIds[0].Trim();
        }
    }

    private void SetLoadingState(bool isLoading)
    {
        if (isLoading)
        {
            SelectedFontsItemsControl.Visibility = Visibility.Collapsed;
            LoadingPanel.Visibility = Visibility.Visible;
            LoadingRing.IsActive = true;

            TriggerButton.IsEnabled = false;
        }
        else
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            LoadingRing.IsActive = false;
            SelectedFontsItemsControl.Visibility = Visibility.Visible;
            TriggerButton.IsEnabled = true;
        }
    }

    private async Task UpdateDisplayAsync(string fontIdString)
    {
        SetLoadingState(true);

        try
        {
            if (string.IsNullOrWhiteSpace(fontIdString))
            {
                SelectedFontsItemsControl.ItemsSource = new List<ExtendedFontFamily>
                {
                    new()
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

            var idsToProcess = AllowMultipleSelection ? fontIds : fontIds.Take(1);

            if (fontIds.Count != 0)
                foreach (var fontId in idsToProcess)
                {
                    var matchedFont = fonts.FirstOrDefault(f => f.FontFamily == fontId);

                    if (matchedFont != null)
                        displayItems.Add(matchedFont);
                    else
                        displayItems.Add(new ExtendedFontFamily
                        {
                            LocalizedFontFamily = fontId,
                            FontFamily = fontId
                        });
                }
            else
                displayItems.Add(new ExtendedFontFamily
                {
                    LocalizedFontFamily = fontIdString,
                    FontFamily = "Unknown"
                });

            SelectedFontsItemsControl.ItemsSource = displayItems;
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    private async void TriggerButton_Click(object sender, RoutedEventArgs e)
    {
        var currentFontsList = string.IsNullOrWhiteSpace(SelectedFontId)
            ? new List<string>()
            : SelectedFontId
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

        var dialog = new FontPickerDialog(currentFontsList, AllowMultipleSelection)
        {
            XamlRoot = XamlRoot,
            PrimaryButtonText = _localizationService.GetLocalizedString("Confirm"),
            CloseButtonText = _localizationService.GetLocalizedString("Cancel")
        };

        var result = await dialog.ShowAsync(ContentDialogPlacement.Popup);

        if (result == ContentDialogResult.Primary) SelectedFontId = string.Join(", ", dialog.SelectedFontIds);
    }
}
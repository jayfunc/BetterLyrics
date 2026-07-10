using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Media;
using global::Avalonia.Interactivity;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using CommunityToolkit.Mvvm.DependencyInjection;
using FluentAvalonia.UI.Controls;
namespace BetterLyrics.Avalonia.Controls;

public partial class FontPicker : UserControl
{
    public static readonly StyledProperty<string> SelectedFontIdProperty =
        AvaloniaProperty.Register<FontPicker, string>(nameof(SelectedFontId), string.Empty);

    public static readonly StyledProperty<bool> AllowMultipleSelectionProperty =
        AvaloniaProperty.Register<FontPicker, bool>(nameof(AllowMultipleSelection), true);

    private readonly ILocalizationService _localizationService;
    private List<ExtendedFontFamily> _allFonts = new();

    public FontPicker()
    {
        InitializeComponent();
        _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

        LoadingTextBlock.Text = _localizationService.GetLocalizedString("Loading");
        
        SelectedFontIdProperty.Changed.AddClassHandler<FontPicker>((s, e) => s.OnSelectedFontIdChanged(e));
        AllowMultipleSelectionProperty.Changed.AddClassHandler<FontPicker>((s, e) => s.OnAllowMultipleSelectionChanged(e));
    }

    public string SelectedFontId
    {
        get => GetValue(SelectedFontIdProperty);
        set => SetValue(SelectedFontIdProperty, value);
    }

    public bool AllowMultipleSelection
    {
        get => GetValue(AllowMultipleSelectionProperty);
        set => SetValue(AllowMultipleSelectionProperty, value);
    }

    private void OnSelectedFontIdChanged(AvaloniaPropertyChangedEventArgs e)
    {
        _ = UpdateDisplayAsync((string)e.NewValue);
    }

    private void OnAllowMultipleSelectionChanged(AvaloniaPropertyChangedEventArgs e)
    {
        var isMultiAllowed = (bool)e.NewValue;

        if (!isMultiAllowed && !string.IsNullOrWhiteSpace(SelectedFontId))
        {
            var fontIds = SelectedFontId.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (fontIds.Length > 1) SelectedFontId = fontIds[0].Trim();
        }
        
        FontListBox.SelectionMode = isMultiAllowed ? SelectionMode.Multiple | SelectionMode.Toggle : SelectionMode.Single;
    }

    private void SetLoadingState(bool isLoading)
    {
        if (isLoading)
        {
            SelectedFontsItemsControl.IsVisible = false;
            LoadingPanel.IsVisible = true;
            TriggerButton.IsEnabled = false;
        }
        else
        {
            LoadingPanel.IsVisible = false;
            SelectedFontsItemsControl.IsVisible = true;
            TriggerButton.IsEnabled = true;
        }
    }
    
    private Task<List<ExtendedFontFamily>> GetSystemFontsAsync()
    {
        return Task.Run(() =>
        {
            var fonts = FontManager.Current.SystemFonts;
            return fonts.Select(f => new ExtendedFontFamily
            {
                FontFamily = f.Name,
                LocalizedFontFamily = f.Name
            }).OrderBy(f => f.FontFamily).ToList();
        });
    }

    private async Task UpdateDisplayAsync(string fontIdString)
    {
        SetLoadingState(true);

        try
        {
            if (_allFonts.Count == 0)
            {
                _allFonts = await GetSystemFontsAsync();
                FontListBox.ItemsSource = _allFonts;
            }

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
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

            var displayItems = new List<ExtendedFontFamily>();
            var idsToProcess = AllowMultipleSelection ? fontIds : fontIds.Take(1);

            if (fontIds.Count != 0)
            {
                foreach (var fontId in idsToProcess)
                {
                    var matchedFont = _allFonts.FirstOrDefault(f => f.FontFamily == fontId);
                    if (matchedFont != null)
                        displayItems.Add(matchedFont);
                    else
                        displayItems.Add(new ExtendedFontFamily
                        {
                            LocalizedFontFamily = fontId,
                            FontFamily = fontId
                        });
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
            
            // Sync selection to ListBox
            FontListBox.SelectionChanged -= FontListBox_SelectionChanged;
            FontListBox.SelectedItems.Clear();
            foreach (var item in displayItems)
            {
                var match = _allFonts.FirstOrDefault(f => f.FontFamily == item.FontFamily);
                if (match != null)
                {
                    FontListBox.SelectedItems.Add(match);
                }
            }
            FontListBox.SelectionChanged += FontListBox_SelectionChanged;
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    private void TriggerButton_Click(object? sender, RoutedEventArgs e)
    {
        // Flyout is opened automatically via XAML Button.Flyout.
        if (_allFonts.Count == 0)
        {
            _ = UpdateDisplayAsync(SelectedFontId);
        }
    }

    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var text = SearchBox.Text?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(text))
        {
            FontListBox.ItemsSource = _allFonts;
        }
        else
        {
            FontListBox.ItemsSource = _allFonts.Where(f => f.FontFamily.ToLowerInvariant().Contains(text) || 
                                                           f.LocalizedFontFamily.ToLowerInvariant().Contains(text)).ToList();
        }
    }

    private void FontListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selectedFonts = FontListBox.SelectedItems.Cast<ExtendedFontFamily>().Select(f => f.FontFamily);
        SelectedFontId = string.Join(", ", selectedFonts);
    }
}

using BetterLyrics.Core.Models;
using BetterLyrics.WinUI3.Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class FontPickerDialog : ContentDialog
    {
        private ObservableCollection<ExtendedFontFamily> _filteredFonts = new();
        private ObservableCollection<ExtendedFontFamily> _selectedFonts = new();
        private List<ExtendedFontFamily> _allFontsReference;

        public List<string> SelectedFontIds { get; private set; } = new();

        public bool AllowMultipleSelection { get; private set; }

        public FontPickerDialog(List<string> currentFontIds, bool allowMultipleSelection = true)
        {
            this.InitializeComponent();
            AllowMultipleSelection = allowMultipleSelection;
            SelectedFontsListView.ItemsSource = _selectedFonts;
            _ = InitializeFontsAsync(currentFontIds);
        }

        private async Task InitializeFontsAsync(List<string> currentFontIds)
        {
            _allFontsReference = await FontHelper.GetSystemFontFamiliesAsync();
            foreach (var font in _allFontsReference)
            {
                _filteredFonts.Add(font);
            }
            FontListView.ItemsSource = _filteredFonts;

            if (currentFontIds != null && currentFontIds.Count != 0)
            {
                var idsToProcess = AllowMultipleSelection ? currentFontIds : currentFontIds.Take(1);

                foreach (var id in idsToProcess)
                {
                    var match = _allFontsReference.FirstOrDefault(f => f.FontFamily == id);
                    if (match != null)
                    {
                        _selectedFonts.Add(match);
                    }
                    else
                    {
                        _selectedFonts.Add(new ExtendedFontFamily { IsExistedInSystem = false, FontFamily = id, LocalizedFontFamily = id });
                    }
                }
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_allFontsReference == null) return;
            string query = SearchBox.Text.Trim().ToLower();

            _filteredFonts.Clear();
            var result = string.IsNullOrEmpty(query)
                ? _allFontsReference
                : _allFontsReference.Where(f =>
                    f.LocalizedFontFamily.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                    f.FontFamily.Contains(query, StringComparison.CurrentCultureIgnoreCase));

            foreach (var item in result) _filteredFonts.Add(item);
        }

        private void FontListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FontListView.SelectedItem is ExtendedFontFamily selected)
            {
                if (!AllowMultipleSelection)
                {
                    _selectedFonts.Clear();
                }

                if (!_selectedFonts.Contains(selected))
                {
                    _selectedFonts.Add(selected);
                }

                FontListView.SelectedItem = null;
            }
        }

        private void RemoveFont_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ExtendedFontFamily fontToRemove)
            {
                _selectedFonts.Remove(fontToRemove);
            }
        }

        private void FontListView_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            SelectedFontIds = _selectedFonts.Select(f => f.FontFamily).ToList();
        }
    }
}
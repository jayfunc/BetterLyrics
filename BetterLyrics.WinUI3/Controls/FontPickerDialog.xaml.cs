using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class FontPickerDialog : ContentDialog
    {
        private bool _isInitializing = false;
        private ObservableCollection<ExtendedFontFamily> _filteredFonts = new();
        private List<ExtendedFontFamily> _allFontsReference;

        public string SelectedFontId { get; private set; }

        public FontPickerDialog(string currentFontId)
        {
            this.InitializeComponent();
            InitializeFonts(currentFontId);
        }

        private async void InitializeFonts(string currentFontId)
        {
            _allFontsReference = await FontHelper.GetSystemFontFamiliesAsync();
            foreach (var font in _allFontsReference)
            {
                _filteredFonts.Add(font);
            }
            FontListView.ItemsSource = _filteredFonts;

            if (!string.IsNullOrEmpty(currentFontId))
            {
                var match = _allFontsReference.FirstOrDefault(f => f.FontFamily == currentFontId);
                if (match != null)
                {
                    _isInitializing = true;
                    FontListView.SelectedItem = match;
                    FontListView.ScrollIntoView(FontListView.SelectedItem, ScrollIntoViewAlignment.Leading);
                    _isInitializing = false;
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
            if (_isInitializing) return;

            if (FontListView.SelectedItem is ExtendedFontFamily selected)
            {
                SelectedFontId = selected.FontFamily;
                this.Hide();
            }
        }

        private void FontListView_Loaded(object sender, RoutedEventArgs e)
        {
            if (FontListView.SelectedItem != null)
            {
                FontListView.ScrollIntoView(FontListView.SelectedItem, ScrollIntoViewAlignment.Leading);
            }
        }

    }
}

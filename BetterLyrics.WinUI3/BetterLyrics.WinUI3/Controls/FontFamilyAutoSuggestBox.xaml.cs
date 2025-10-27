using BetterLyrics.WinUI3.Helper;
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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class FontFamilyAutoSuggestBox : UserControl
    {
        private List<string> SystemFontNames { get; set; } = [.. FontHelper.SystemFontFamilies];

        public FontFamilyAutoSuggestBox()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty SelectedFontFamilyProperty =
            DependencyProperty.Register(nameof(SelectedFontFamily), typeof(string), typeof(FontFamilyAutoSuggestBox), new PropertyMetadata(default));

        public string SelectedFontFamily
        {
            get => (string)GetValue(SelectedFontFamilyProperty);
            set => SetValue(SelectedFontFamilyProperty, value);
        }


        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            SelectedFontFamily = args.SelectedItem.ToString() ?? "";
        }

        private void UpdateAutoSuggestBoxItemsSource()
        {
            var suitableItems = new List<string>();
            var splitText = AutoSuggestBox.Text.ToLower().Split(" ");
            foreach (var fontFamily in SystemFontNames)
            {
                var found = splitText.All((key) =>
                {
                    return fontFamily.ToLower().Contains(key);
                });
                if (found)
                {
                    suitableItems.Add(fontFamily);
                }
            }
            if (suitableItems.Count == 0)
            {
                suitableItems.Add("N/A");
            }
            AutoSuggestBox.ItemsSource = suitableItems.Order();
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Since selecting an item will also change the text,
            // only listen to changes caused by user entering text.
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                UpdateAutoSuggestBoxItemsSource();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SystemFontNames = [.. FontHelper.SystemFontFamilies];
        }

        private void AutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdateAutoSuggestBoxItemsSource();
        }

        private void AutoSuggestBox_LostFocus(object sender, RoutedEventArgs e)
        {
            AutoSuggestBox.Text = SelectedFontFamily;
        }
    }
}

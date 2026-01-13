using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class FontFamilyAutoSuggestBox : UserControl
    {
        private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

        //private List<ExtendedFontFamily> FontFamilies { get; set; } = [];
        private List<string> FontFamilies { get; set; } = [];

        public FontFamilyAutoSuggestBox()
        {
            InitializeComponent();
            RefreshFontFamilies();
        }

        public static readonly DependencyProperty SelectedFontFamilyProperty =
            DependencyProperty.Register(nameof(SelectedFontFamily), typeof(string), typeof(FontFamilyAutoSuggestBox), new PropertyMetadata(default));

        public string SelectedFontFamily
        {
            get => (string)GetValue(SelectedFontFamilyProperty);
            set => SetValue(SelectedFontFamilyProperty, value);
        }

        private void RefreshFontFamilies()
        {
            //Task.Run(() =>
            //{
            //    var fontFamilies = FontHelper.SystemFontFamilies.Select(x => new ExtendedFontFamily()
            //    {
            //        FontFamily = x,
            //        LocalizedFontFamily = FontHelper.GetLocalizedFontFamilyName(x, _settingsService.AppSettings.GeneralSettings.LanguageCode)
            //    }).OrderBy(x => x.LocalizedFontFamily).ToList();
            //    DispatcherQueue.TryEnqueue(() =>
            //    {
            //        FontFamilies = fontFamilies;
            //    });
            //});
            FontFamilies = FontHelper.GetSystemFontFamilies();
        }

        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is ExtendedFontFamily extendedFontFamily)
            {
                SelectedFontFamily = extendedFontFamily.FontFamily;
            }
            else
            {
                SelectedFontFamily = args.SelectedItem.ToString() ?? "";
            }
        }

        private void UpdateAutoSuggestBoxItemsSource(string? query = null)
        {
            query ??= AutoSuggestBox.Text;

            //var suitableItems = new List<ExtendedFontFamily>();
            var suitableItems = new List<string>();
            var splitText = query.ToLower().Split(" ");
            foreach (var fontFamily in FontFamilies)
            {
                bool found = splitText.All((key) =>
                {
                    //return fontFamily.FontFamily.ToLower().Contains(key) || fontFamily.LocalizedFontFamily.ToLower().Contains(key);
                    return fontFamily.ToLower().Contains(key);
                });
                if (found)
                {
                    suitableItems.Add(fontFamily);
                }
            }
            if (suitableItems.Count == 0)
            {
                //suitableItems.Add(new ExtendedFontFamily()
                //{
                //    FontFamily = "",
                //    LocalizedFontFamily = "N/A"
                //});
                suitableItems.Add("N/A");
            }
            //AutoSuggestBox.ItemsSource = suitableItems.OrderBy(x => x.LocalizedFontFamily);
            AutoSuggestBox.ItemsSource = suitableItems.OrderBy(x => x);
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Since selecting an item will also change the text,
            // only listen to changes caused by user entering text.
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                UpdateAutoSuggestBoxItemsSource();
            }

            SelectedLocalizedFontFamilyTextBlock.Text = FontHelper.GetLocalizedFontFamilyName(SelectedFontFamily, _settingsService.AppSettings.GeneralSettings.LanguageCode);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RefreshFontFamilies();
        }

        private void AutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdateAutoSuggestBoxItemsSource("");
        }

        private void AutoSuggestBox_LostFocus(object sender, RoutedEventArgs e)
        {
            AutoSuggestBox.Text = SelectedFontFamily;
        }
    }
}

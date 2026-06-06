using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class ExternalLinkButton : HyperlinkButton
    {
        public ExternalLinkButton()
        {
            this.Loaded += ExternalLinkButton_Loaded;
        }

        private void ExternalLinkButton_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateContent();
        }

        private void UpdateContent()
        {
            FrameworkElement? element = null;
            if (Content is string textContent)
            {
                element = new TextBlock
                {
                    Text = textContent,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
            else if (Content is FrameworkElement frameworkElement)
            {
                element = frameworkElement;
                if (element.Tag?.ToString() == "ExternalLinkButtonPanel")
                {
                    return; // Already wrapped, no need to update
                }
            }

            var panel = new Grid { ColumnSpacing = 6, Tag = "ExternalLinkButtonPanel" };
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            panel.Children.Add(element);
            Grid.SetColumn(element, 0);

            var fontIcon = new FontIcon
            {
                FontFamily = (FontFamily)Application.Current.Resources["SegoeFluentIcons"],
                Glyph = "\uE8A7",
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            panel.Children.Add(fontIcon);
            Grid.SetColumn(fontIcon, 1);

            this.Content = panel;
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            UpdateContent();
        }
    }
}

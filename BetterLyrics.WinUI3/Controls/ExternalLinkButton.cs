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
            if (Content is string textContent)
            {
                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                };
                panel.Children.Add(new TextBlock { Text = textContent, VerticalAlignment = VerticalAlignment.Center });
                panel.Children.Add(new FontIcon
                {
                    FontFamily = (FontFamily)Application.Current.Resources["SegoeFluentIcons"],
                    Glyph = "\uE8A7",
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                });
                this.Content = panel;
            }
        }
    }
}

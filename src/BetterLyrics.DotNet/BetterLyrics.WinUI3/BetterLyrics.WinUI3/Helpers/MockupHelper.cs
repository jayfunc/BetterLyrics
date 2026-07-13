using BetterLyrics.Core.Enums;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BetterLyrics.WinUI3.Helpers;

public static class MockupHelper
{
    public static FrameworkElement GenerateMockupContent(FrameworkElement frameworkElement, ComponentType type,
        HorizontalAlignment align, string displayName)
    {
        var primaryTextBrush = BrushHelper.GetThemeBrush(frameworkElement, "TextFillColorPrimaryBrush");
        var secondaryTextBrush = BrushHelper.GetThemeBrush(frameworkElement, "TextFillColorSecondaryBrush");
        var tertiaryTextBrush = BrushHelper.GetThemeBrush(frameworkElement, "TextFillColorTertiaryBrush");
        var bgBrush = BrushHelper.GetThemeBrush(frameworkElement, "CardBackgroundFillColorDefaultBrush");
        var borderBrush = BrushHelper.GetThemeBrush(frameworkElement, "CardStrokeColorDefaultBrush");

        var contentAlign = align == HorizontalAlignment.Stretch ? HorizontalAlignment.Left : align;

        FrameworkElement? innerContent;

        switch (type)
        {
            case ComponentType.AlbumArt:
                innerContent = new Border
                {
                    Width = 100,
                    Height = 100,
                    Background = bgBrush,
                    BorderBrush = borderBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Child = new FontIcon { Glyph = "\uE93C", FontSize = 48, Foreground = secondaryTextBrush }
                };
                break;

            case ComponentType.SongTitle:
            case ComponentType.SongArtist:
            case ComponentType.SongAlbum:
                var textGrid = new Grid { Width = 400 };

                if (type == ComponentType.SongTitle)
                    textGrid.Children.Add(new TextBlock
                    {
                        Text = $"[{displayName}] BetterLyrics",
                        FontSize = 24,
                        FontWeight = FontWeights.Bold,
                        Foreground = primaryTextBrush,
                        HorizontalAlignment = contentAlign
                    });
                else if (type == ComponentType.SongArtist)
                    textGrid.Children.Add(new TextBlock
                    {
                        Text = $"[{displayName}] Zhe Fang",
                        FontSize = 14,
                        Foreground = secondaryTextBrush,
                        HorizontalAlignment = contentAlign
                    });
                else if (type == ComponentType.SongAlbum)
                    textGrid.Children.Add(new TextBlock
                    {
                        Text = $"[{displayName}] JayFunc Labs",
                        FontSize = 14,
                        Foreground = tertiaryTextBrush,
                        HorizontalAlignment = contentAlign
                    });
                innerContent = textGrid;
                break;

            case ComponentType.Lyrics:
                var lyricsStack = new StackPanel
                    { Spacing = 12, HorizontalAlignment = HorizontalAlignment.Center, Width = 500 };
                var textAlign = align == HorizontalAlignment.Left ? TextAlignment.Left :
                    align == HorizontalAlignment.Right ? TextAlignment.Right : TextAlignment.Center;

                lyricsStack.Children.Add(new TextBlock
                {
                    Text = $"[{displayName}]", FontSize = 18, Foreground = tertiaryTextBrush, TextAlignment = textAlign
                });
                lyricsStack.Children.Add(new TextBlock
                {
                    Text = $"{Core.Constants.App.SloganEN}", FontSize = 18, Foreground = tertiaryTextBrush,
                    TextAlignment = textAlign
                });
                lyricsStack.Children.Add(new TextBlock
                {
                    Text = $"{Core.Constants.App.SloganJP}", FontSize = 20, Foreground = secondaryTextBrush,
                    TextAlignment = textAlign
                });
                lyricsStack.Children.Add(new TextBlock
                {
                    Text = $"{Core.Constants.App.SloganCN}", FontSize = 22, Foreground = primaryTextBrush,
                    TextAlignment = textAlign
                });
                lyricsStack.Children.Add(new TextBlock
                {
                    Text = $"{Core.Constants.App.SloganJP}", FontSize = 20, Foreground = secondaryTextBrush,
                    TextAlignment = textAlign
                });
                lyricsStack.Children.Add(new TextBlock
                {
                    Text = $"{Core.Constants.App.SloganEN}", FontSize = 18, Foreground = tertiaryTextBrush,
                    TextAlignment = textAlign
                });
                innerContent = lyricsStack;
                break;

            case ComponentType.LyricsCard:
                var cardBorder = new Border
                {
                    Background = bgBrush,
                    BorderBrush = borderBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16, 12, 16, 12),
                    Width = 300,
                    HorizontalAlignment = contentAlign
                };
                var cardStack = new StackPanel { Spacing = 8 };
                cardStack.Children.Add(new TextBlock
                {
                    Text = $"[{displayName}]「{Core.Constants.App.SloganCN}」", FontSize = 16,
                    FontWeight = FontWeights.Bold, Foreground = primaryTextBrush,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                cardStack.Children.Add(new TextBlock
                {
                    Text = $"[{displayName}] BetterLyrics", FontSize = 10, Foreground = secondaryTextBrush,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                cardBorder.Child = cardStack;
                innerContent = cardBorder;
                break;

            default:
                innerContent = new TextBlock
                {
                    Text = $"[{displayName}]",
                    Foreground = secondaryTextBrush,
                    FontSize = 14,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                break;
        }

        var viewbox = new Viewbox
        {
            Child = innerContent,
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.Both
        };

        return viewbox;
    }
}
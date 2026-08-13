using BetterLyrics.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class FileNamePatternControl : UserControl
{
    private static ILocalizationService _localizationService = Ioc.Default.GetRequiredService<ILocalizationService>();

    public static readonly DependencyProperty PatternTextProperty = DependencyProperty.Register(
        nameof(PatternText),
        typeof(string),
        typeof(FileNamePatternControl),
        new PropertyMetadata(string.Empty, OnPatternTextChanged));

    public string PatternText
    {
        get => (string)GetValue(PatternTextProperty);
        set => SetValue(PatternTextProperty, value);
    }

    public FileNamePatternControl()
    {
        InitializeComponent();
        this.Loaded += (s, e) => UpdateExamplePreview(PatternText);
    }

    private static void OnPatternTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FileNamePatternControl control)
        {
            control.UpdateExamplePreview(e.NewValue as string);
        }
    }

    private void UpdateExamplePreview(string? pattern)
    {
        if (ExamplePreviewTextBlock == null) return;

        ExamplePreviewTextBlock.Inlines.Clear();

        if (string.IsNullOrWhiteSpace(pattern))
        {
            ExamplePreviewTextBlock.Text = _localizationService.GetLocalizedString("FileNamePatternControlNoPatternProvided");
            return;
        }

        ExamplePreviewTextBlock.Text = string.Empty; // clear previous text

        var regex = new System.Text.RegularExpressions.Regex(@"\{.*?\}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var matches = regex.Matches(pattern);
        int lastIndex = 0;

        var accentBrush = Application.Current.Resources["AccentTextFillColorPrimaryBrush"] as Microsoft.UI.Xaml.Media.SolidColorBrush;
        var secondaryBrush = Application.Current.Resources["TextFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.SolidColorBrush;

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Index > lastIndex)
            {
                ExamplePreviewTextBlock.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = pattern.Substring(lastIndex, match.Index - lastIndex) });
            }

            var key = match.Value.ToLowerInvariant();
            var isSupported = key is "{artist}" or "{title}" or "{album}";

            var replacementText = key switch
            {
                "{artist}" => "Coldplay",
                "{title}" => "Yellow",
                "{album}" => "Parachutes",
                _ => "(any)"
            };

            var run = new Microsoft.UI.Xaml.Documents.Run { Text = replacementText };
            if (isSupported && accentBrush != null)
            {
                run.Foreground = accentBrush;
                run.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
            }
            else if (!isSupported && secondaryBrush != null)
            {
                run.Foreground = secondaryBrush;
            }
            ExamplePreviewTextBlock.Inlines.Add(run);

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < pattern.Length)
        {
            ExamplePreviewTextBlock.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = pattern.Substring(lastIndex) });
        }
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
    }
}

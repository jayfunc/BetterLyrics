using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Controls;

/// <summary>
/// A <see cref="PasswordBox"/> replacement whose reveal button is always available and sits inside
/// the text box border, like the built-in one.
/// The built-in reveal button of <see cref="PasswordBox"/> is only shown once, when the box gets
/// focus for the first time and text is entered; it never comes back after a refocus, which makes
/// prefilled secrets impossible to reveal.
/// </summary>
public sealed partial class RevealPasswordBox : UserControl
{
    private const double RevealButtonSpace = 32;

    public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register(
        nameof(Password), typeof(string), typeof(RevealPasswordBox),
        new PropertyMetadata(string.Empty, OnPasswordChanged));

    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(
        nameof(PlaceholderText), typeof(string), typeof(RevealPasswordBox),
        new PropertyMetadata(string.Empty, OnPlaceholderTextChanged));

    private bool _isUpdating;
    private bool _hasReservedButtonSpace;

    public RevealPasswordBox()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        UpdateRevealButtonState();
    }

    public event RoutedEventHandler? PasswordChanged;

    public string Password
    {
        get => (string)GetValue(PasswordProperty);
        set => SetValue(PasswordProperty, value);
    }

    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((RevealPasswordBox)d).ApplyPassword(e.NewValue as string ?? string.Empty);
    }

    private static void OnPlaceholderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((RevealPasswordBox)d).InputBox.PlaceholderText = e.NewValue as string ?? string.Empty;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_hasReservedButtonSpace) return;

        // Keep the text clear of the overlaid reveal button without overriding the theme's padding.
        var padding = InputBox.Padding;
        InputBox.Padding = new Thickness(padding.Left, padding.Top, padding.Right + RevealButtonSpace,
            padding.Bottom);
        _hasReservedButtonSpace = true;
    }

    private void ApplyPassword(string value)
    {
        if (InputBox.Password != value)
        {
            _isUpdating = true;
            InputBox.Password = value;
            _isUpdating = false;
        }

        UpdateRevealButtonState();
    }

    private void InputBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdating) return;

        SetValue(PasswordProperty, InputBox.Password);
        PasswordChanged?.Invoke(this, e);
        UpdateRevealButtonState();
    }

    private void RevealButton_Click(object sender, RoutedEventArgs e)
    {
        var reveal = InputBox.PasswordRevealMode != PasswordRevealMode.Visible;

        InputBox.PasswordRevealMode = reveal ? PasswordRevealMode.Visible : PasswordRevealMode.Hidden;
        RevealIcon.Glyph = reveal ? "\uED1A" : "\uE7B3";
    }

    private void UpdateRevealButtonState()
    {
        RevealButton.IsEnabled = InputBox.Password.Length > 0;
    }
}

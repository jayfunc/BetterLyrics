using BetterLyrics.WinUI3.Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class PropertyRow : UserControl
    {
        public PropertyRow()
        {
            this.InitializeComponent();
        }

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(string), typeof(PropertyRow), new PropertyMetadata(string.Empty));

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(string), typeof(PropertyRow), new PropertyMetadata(string.Empty));

        public string Link
        {
            get => (string)GetValue(LinkProperty);
            set => SetValue(LinkProperty, value);
        }
        public static readonly DependencyProperty LinkProperty =
            DependencyProperty.Register(nameof(Link), typeof(string), typeof(PropertyRow), new PropertyMetadata(string.Empty));

        public string Unit
        {
            get => (string)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }
        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register(nameof(Unit), typeof(string), typeof(PropertyRow), new PropertyMetadata(string.Empty));

        private Visibility TextVisibility => Link == string.Empty ? Visibility.Visible : Visibility.Collapsed;
        private Visibility LinkVisibility => Link != string.Empty ? Visibility.Visible : Visibility.Collapsed;

        private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Value))
            {
                CopyButton.Opacity = 1;
            }
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            CopyButton.Opacity = 0;
        }

        private void OnCopyClicked(object sender, RoutedEventArgs e)
        {
            var targetValue = string.IsNullOrEmpty(Link) ? Value : Link;

            if (string.IsNullOrEmpty(targetValue)) return;

            try
            {
                DataPackage dataPackage = new DataPackage();
                dataPackage.SetText(targetValue);
                Clipboard.SetContent(dataPackage);
            }
            catch (Exception ex)
            {
                GlobalToastManager.Show("Error", ex.Message, InfoBarSeverity.Error);
                return;
            }

            CheckIcon.Opacity = 1;
            CopyIcon.Opacity = 0;

            this.DispatcherQueue.TryEnqueue(async () =>
            {
                await Task.Delay(1000);

                CheckIcon.Opacity = 0;
                CopyIcon.Opacity = 1;
            });
        }

        private async void OnLinkClicked(object sender, RoutedEventArgs e)
        {
            Uri.TryCreate(Link, UriKind.Absolute, out var uri);
            if (uri != null)
            {
                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                {
                    await Windows.System.Launcher.LaunchUriAsync(uri);
                }
                else if (uri.Scheme == Uri.UriSchemeFile)
                {
                    await LauncherHelper.SelectAndShowFileAsync(uri.LocalPath);
                }
            }
        }
    }
}

using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;

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
            if (string.IsNullOrEmpty(Value)) return;

            try
            {
                DataPackage dataPackage = new DataPackage();
                dataPackage.SetText(Value);
                Clipboard.SetContent(dataPackage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Copy failed: {ex.Message}");
                return;
            }

            CheckIcon.Opacity = 1;
            CopyIcon.Opacity = 0;

            this.DispatcherQueue.TryEnqueue(async () =>
            {
                await Task.Delay(1500);

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
                    await LauncherHelper.SelectAndShowFile(uri.LocalPath);
                }
            }
        }
    }
}

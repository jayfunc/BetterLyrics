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
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class ContributorCard : UserControl
    {
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(string), typeof(ContributorCard), new PropertyMetadata(string.Empty));

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(ContributorCard), new PropertyMetadata(string.Empty));

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty AvatarSourceProperty =
            DependencyProperty.Register(nameof(AvatarSource), typeof(string), typeof(ContributorCard), new PropertyMetadata(string.Empty));

        public string AvatarSource
        {
            get => (string)GetValue(AvatarSourceProperty);
            set => SetValue(AvatarSourceProperty, value);
        }

        public static readonly DependencyProperty BadgesProperty =
            DependencyProperty.Register(nameof(Badges), typeof(string), typeof(ContributorCard), new PropertyMetadata(string.Empty));

        public string Badges
        {
            get => (string)GetValue(BadgesProperty);
            set => SetValue(BadgesProperty, value);
        }

        public ContributorCard()
        {
            InitializeComponent();
        }
    }
}

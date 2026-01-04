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
    public sealed partial class PatronControl : UserControl
    {
        public string PatronName
        {
            get { return (string)GetValue(PatronNameProperty); }
            set { SetValue(PatronNameProperty, value); }
        }

        public static readonly DependencyProperty PatronNameProperty =
            DependencyProperty.Register(nameof(PatronName), typeof(string), typeof(PatronControl), new PropertyMetadata(""));

        public string Date
        {
            get { return (string)GetValue(DateProperty); }
            set { SetValue(DateProperty, value); }
        }

        public static readonly DependencyProperty DateProperty =
            DependencyProperty.Register(nameof(Date), typeof(string), typeof(PatronControl), new PropertyMetadata(""));

        public PatronControl()
        {
            InitializeComponent();
        }
    }
}

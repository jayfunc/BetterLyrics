using BetterLyrics.WinUI3.Helper;
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
    public sealed partial class DragVisualControl : UserControl
    {
        public DragVisualControl(string displayName, Brush background, double width, double height)
        {
            this.InitializeComponent();
            TitleBlock.Text = displayName;
            RootBorder.Background = background;
            RootBorder.BorderBrush = new SolidColorBrush(ColorHelper.GetHarmoniousColor(((SolidColorBrush)background).Color));
            this.Width = width;
            this.Height = height;
        }
    }
}

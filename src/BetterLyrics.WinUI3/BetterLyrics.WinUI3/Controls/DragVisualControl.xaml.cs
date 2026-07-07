using BetterLyrics.Core.Helpers;
using BetterLyrics.WinUI3.Extensions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class DragVisualControl : UserControl
{
    public DragVisualControl(string displayName, Brush? background, double width, double height)
    {
        InitializeComponent();
        TitleBlock.Text = displayName;
        RootBorder.Background = background;
        if (background is SolidColorBrush solidColorBrush)
            RootBorder.BorderBrush =
                new SolidColorBrush(
                    ColorExtensions.FromAppColor(
                        ColorHelper.GetHarmoniousColor(solidColorBrush.Color.ToAppColor())));

        Width = width;
        Height = height;
    }
}
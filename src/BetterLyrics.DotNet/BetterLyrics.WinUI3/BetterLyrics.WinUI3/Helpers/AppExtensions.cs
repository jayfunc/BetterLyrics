using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.WinUI3.Controls;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Providers;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace BetterLyrics.WinUI3.Helpers;

public static class AppExtensions
{
    public static readonly DependencyProperty AumidProperty =
        DependencyProperty.RegisterAttached(
            "Aumid",
            typeof(string),
            typeof(AppExtensions),
            new PropertyMetadata(null, OnAumidChanged));

    private static readonly IProgramProvider _programProvider =
        Ioc.Default.GetRequiredService<IProgramProvider>();

    public static string GetAumid(DependencyObject obj)
    {
        return (string)obj.GetValue(AumidProperty);
    }

    public static void SetAumid(DependencyObject obj, string value)
    {
        obj.SetValue(AumidProperty, value);
    }

    private static async void OnAumidChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var aumid = e.NewValue as string;

        if (string.IsNullOrWhiteSpace(aumid)) return;

        if (d is Image imageControl)
        {
            imageControl.Source = null;

            try
            {
                var bytes = await _programProvider.GetIconByAumidAsync(aumid);

                if (GetAumid(imageControl) == aumid && bytes != null)
                    imageControl.Source = BitmapImageExtensions.FromByteArray(bytes);
            }
            catch
            {
            }
        }

        else if (d is TextBlock textBlock)
        {
            textBlock.Text = aumid;

            try
            {
                var name = await _programProvider.GetDisplayNameByAumidAsync(aumid);

                if (GetAumid(textBlock) == aumid) textBlock.Text = name ?? aumid;
            }
            catch
            {
                textBlock.Text = aumid;
            }
        }

        else if (d is PropertyRow propertyRow)
        {
            propertyRow.Value = aumid;

            try
            {
                var name = await _programProvider.GetDisplayNameByAumidAsync(aumid);

                if (GetAumid(propertyRow) == aumid) propertyRow.Value = name ?? aumid;
            }
            catch
            {
                propertyRow.Value = aumid;
            }
        }

        else if (d is PersonPicture personPicture)
        {
            personPicture.DisplayName = aumid;

            try
            {
                var name = await _programProvider.GetDisplayNameByAumidAsync(aumid);

                if (GetAumid(personPicture) == aumid) personPicture.DisplayName = name ?? aumid;
            }
            catch
            {
                personPicture.DisplayName = aumid;
            }
        }
    }
}
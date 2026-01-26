using BetterLyrics.WinUI3.Controls;
using BetterLyrics.WinUI3.Hooks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace BetterLyrics.WinUI3.Helper
{
    public static class AppExtensions
    {
        public static readonly DependencyProperty AumidProperty =
            DependencyProperty.RegisterAttached(
                "Aumid",
                typeof(string),
                typeof(AppExtensions),
                new PropertyMetadata(null, OnAumidChanged));

        public static string GetAumid(DependencyObject obj) => (string)obj.GetValue(AumidProperty);
        public static void SetAumid(DependencyObject obj, string value) => obj.SetValue(AumidProperty, value);

        private static async void OnAumidChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            string? aumid = e.NewValue as string;

            if (string.IsNullOrWhiteSpace(aumid)) return;

            if (d is Image imageControl)
            {
                imageControl.Source = new BitmapImage(new Uri(PathHelper.UnknownPlayerLogoPath));

                try
                {
                    var icon = await AppHook.GetIconByAumidAsync(aumid, imageControl.DispatcherQueue);

                    if (GetAumid(imageControl) == aumid)
                    {
                        imageControl.Source = icon;
                    }
                }
                catch { }
            }

            else if (d is TextBlock textBlock)
            {
                textBlock.Text = aumid;

                try
                {
                    var name = await AppHook.GetDisplayNameByAumidAsync(aumid);

                    if (GetAumid(textBlock) == aumid)
                    {
                        textBlock.Text = name ?? aumid;
                    }
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
                    var name = await AppHook.GetDisplayNameByAumidAsync(aumid);

                    if (GetAumid(propertyRow) == aumid)
                    {
                        propertyRow.Value = name ?? aumid;
                    }
                }
                catch
                {
                    propertyRow.Value = aumid;
                }
            }
        }
    }
}

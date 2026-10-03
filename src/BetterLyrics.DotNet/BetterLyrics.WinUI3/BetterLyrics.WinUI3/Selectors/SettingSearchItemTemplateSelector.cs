using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Selectors;

public partial class SettingSearchItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? NormalTemplate { get; set; }
    public DataTemplate? LoadingTemplate { get; set; }
    public DataTemplate? NotFoundTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        if (item is SettingSearchItem searchItem)
        {
            if (searchItem == SettingSearchItemExtensions.LoadingPlaceholder) return LoadingTemplate;
            if (searchItem == SettingSearchItemExtensions.NoResultsPlaceholder) return NotFoundTemplate;
            return NormalTemplate;
        }
        return base.SelectTemplateCore(item);
    }
}

using BetterLyrics.Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.Avalonia.Services
{
    public class LocalizationService : ILocalizationService
    {
        public string GetLocalizedString(string id)
        {
            return Strings.Resources.ResourceManager.GetString(id) ?? id;
        }
    }
}

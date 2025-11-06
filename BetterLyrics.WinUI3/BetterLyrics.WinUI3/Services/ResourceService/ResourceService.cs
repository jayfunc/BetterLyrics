using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.ResourceService
{
    public class ResourceService : IResourceService
    {
        private readonly ResourceLoader _resourceLoader = new();

        public string GetLocalizedString(string id)
        {
            return _resourceLoader.GetString(id);
        }
    }
}

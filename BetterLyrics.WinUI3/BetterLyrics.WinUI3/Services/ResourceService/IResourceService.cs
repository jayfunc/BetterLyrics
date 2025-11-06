using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.ResourceService
{
    public interface IResourceService
    {
        string GetLocalizedString(string id);
    }
}

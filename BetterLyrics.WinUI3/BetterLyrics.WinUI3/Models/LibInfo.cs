using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models
{
    public class LibInfo
    {
        public string Name { get; set; }
        public string Url => $"https://www.nuget.org/packages/{Name}/";
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models
{
    public partial class LanguageInfo : ObservableObject
    {
        [ObservableProperty]
        public partial string Code { get; set; }
        [ObservableProperty]
        public partial string Name { get; set; }

        public LanguageInfo(string code, string name)
        {
            Code = code;
            Name = name;
        }
    }
}

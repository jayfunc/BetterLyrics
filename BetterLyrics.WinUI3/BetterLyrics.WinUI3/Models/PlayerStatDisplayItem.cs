using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models
{
    public class PlayerStatDisplayItem
    {
        public string PlayerId { get; set; }
        public int PlayCount { get; set; }

        // 这就是 XAML 中 Rectangle Width 绑定的属性
        public double DisplayWidth { get; set; }
    }
}

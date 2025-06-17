using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models
{
    public enum TitleBarType
    {
        Compact,
        Extended,
    }

    public static class TitleBarTypeExtensions
    {
        public static double GetHeight(this TitleBarType titleBarType)
        {
            return titleBarType switch
            {
                TitleBarType.Compact => 32.0,
                TitleBarType.Extended => 48.0,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(titleBarType),
                    titleBarType,
                    null
                ),
            };
        }
    }
}

using BetterLyrics.WinUI3.Helper;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models
{
    public class PlayerStatDisplayItem
    {
        public string PlayerId { get; set; }
        public int PlayCount { get; set; }

        public string PlayerName => PlayerIdHelper.GetDisplayName(PlayerId);
    }
}

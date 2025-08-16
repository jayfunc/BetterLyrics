using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class PictureInPictureModeSettings : BaseModeSettings
    {
        public PictureInPictureModeSettings()
        {
            LyricsDisplayType = Enums.LyricsDisplayType.SplitView;
        }
    }
}

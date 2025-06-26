// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models
{
    /// <summary>
    /// Defines the <see cref="CharTiming" />
    /// </summary>
    public class CharTiming
    {
        #region Properties

        /// <summary>
        /// Gets or sets the EndMs
        /// </summary>
        public int EndMs { get; set; }

        /// <summary>
        /// Gets or sets the StartMs
        /// </summary>
        public int StartMs { get; set; }

        public string Text { get; set; } = string.Empty;

        public int StartIndex { get; set; }

        #endregion
    }
}

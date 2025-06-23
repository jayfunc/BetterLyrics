// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ude;

namespace BetterLyrics.WinUI3.Helper
{
    /// <summary>
    /// Defines the <see cref="FileHelper" />
    /// </summary>
    public class FileHelper
    {
        #region Methods

        /// <summary>
        /// The GetEncoding
        /// </summary>
        /// <param name="filename">The filename<see cref="string"/></param>
        /// <returns>The <see cref="Encoding"/></returns>
        public static Encoding GetEncoding(string filename)
        {
            var bytes = File.ReadAllBytes(filename);
            var cdet = new CharsetDetector();
            cdet.Feed(bytes, 0, bytes.Length);
            cdet.DataEnd();
            var encoding = cdet.Charset;
            if (encoding == null)
            {
                return Encoding.UTF8;
            }
            return Encoding.GetEncoding(encoding);
        }

        #endregion
    }
}

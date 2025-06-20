using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ude;

namespace BetterLyrics.WinUI3.Enums
{
    public class FileHelper
    {
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
    }
}

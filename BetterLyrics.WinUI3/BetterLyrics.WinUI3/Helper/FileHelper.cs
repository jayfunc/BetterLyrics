// 2025/6/23 by Zhe Fang

using System.IO;
using System.Text;
using Ude;

namespace BetterLyrics.WinUI3.Helper
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

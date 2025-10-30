using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace BetterLyrics.WinUI3.Helper
    {
        public class DirectoryHelper
        {
            /// <summary>
            /// 递归查找指定文件夹下所有文件（包括子文件夹）。
            /// </summary>
            /// <param name="folderPath">要查找的文件夹路径</param>
            /// <returns>所有文件的完整路径列表</returns>
            public static List<string> GetAllFiles(string folderPath, string searchPattern = "*")
            {
                var files = new List<string>();
                if (!Directory.Exists(folderPath))
                    return files;

                try
                {
                    files.AddRange(Directory.GetFiles(folderPath, searchPattern));
                    foreach (var dir in Directory.GetDirectories(folderPath))
                    {
                        files.AddRange(GetAllFiles(dir, searchPattern));
                    }
                }
                catch (Exception)
                {
                    // 可根据需要处理异常，如权限不足等
                }
                return files;
            }

            public static void DeleteAllFiles(string folderPath)
            {
                if (!Directory.Exists(folderPath))
                {
                    return;
                }

                DirectoryInfo di = new DirectoryInfo(folderPath);

                try
                {
                    foreach (FileInfo file in di.GetFiles())
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception ex) { }
                    }
                }
                catch (Exception) { }
            }
        }
    }
}

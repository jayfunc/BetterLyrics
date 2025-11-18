using NTextCat.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class StringExtensions
    {
        private static readonly string[] _splitter =
        [
            ";"
            ,
            ","
            ,
            "/"
            ,
            "；"
            ,
            "、"
            ,
            "，"
        ];

        extension(string str)
        {
            public string[] SplitByCommonSplitter()
            {
                var splitter = _splitter.FirstOrDefault(str.Contains);
                if (splitter != null)
                {
                    return str.Split(splitter);
                }
                else
                {
                    return [str];
                }
            }
        }
    }
}

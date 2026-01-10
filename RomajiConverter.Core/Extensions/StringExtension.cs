using System;
using System.Linq;

namespace RomajiConverter.Core.Extensions
{
    public static class StringExtension
    {
        public static string[] LineToUnits(this string str)
        {
            return str.Split(new[] { ' ', '　' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
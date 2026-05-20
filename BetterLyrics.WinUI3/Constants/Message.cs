using System;
using System.Collections.Generic;
using System.Text;
using Vanara.PInvoke;

namespace BetterLyrics.WinUI3.Constants
{
    public static class Message
    {
        public const uint WM_APPBAR_CALLBACK = User32.WM_USER + 2000;
    }
}

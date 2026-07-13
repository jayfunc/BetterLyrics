using System;
using System.Drawing;

namespace BetterLyrics.WinUI3.Models;

public class TrackedWindowInfo
{
    public string ClassName { get; set; }
    public Rectangle LastRect { get; set; }
    public DateTime LastSeen { get; set; }
}
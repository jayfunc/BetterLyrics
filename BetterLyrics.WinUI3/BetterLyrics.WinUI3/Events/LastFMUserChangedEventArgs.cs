using Hqub.Lastfm.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Events
{
    public class LastFMUserChangedEventArgs : EventArgs
    {
        public User? User { get; set; }
        public LastFMUserChangedEventArgs(User? user)
        {
            User = user;
        }
    }
}

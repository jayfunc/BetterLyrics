using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.WinUI3.Messages
{
    internal class AlbumArtCornerRadiusChangedMessage(int value)
        : ValueChangedMessage<int>(value) { }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;
using DevWinUI;

namespace BetterLyrics.WinUI3.Messages
{
    public class SystemBackdropChangedMessage(BackdropType value)
        : ValueChangedMessage<BackdropType>(value) { }
}

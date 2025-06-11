using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Models;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.WinUI3.Messages
{
    public class TitleBarTypeChangedMessage(TitleBarType value)
        : ValueChangedMessage<TitleBarType>(value) { }
}

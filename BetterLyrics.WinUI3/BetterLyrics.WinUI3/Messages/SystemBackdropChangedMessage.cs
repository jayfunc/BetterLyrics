using CommunityToolkit.Mvvm.Messaging.Messages;
using DevWinUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Messages {
    public class SystemBackdropChangedMessage : ValueChangedMessage<BackdropType> {
        public SystemBackdropChangedMessage(BackdropType value) : base(value) {
        }
    }
}

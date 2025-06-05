using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Messages {
    public class ThemeChangedMessage(ElementTheme value) : ValueChangedMessage<ElementTheme>(value) {
    }
}

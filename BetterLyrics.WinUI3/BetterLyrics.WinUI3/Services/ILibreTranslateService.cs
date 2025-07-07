using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services
{
    public interface ILibreTranslateService
    {
        Task<string> TranslateAsync(string text, CancellationToken? token);
    }
}

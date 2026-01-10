using BetterLyrics.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.Core.Interfaces
{
    public interface IPlugin
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        string Author { get; }

        void Initialize();
    }
}

// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Events
{
    /// <summary>
    /// Defines the <see cref="LibChangedEventArgs" />
    /// </summary>
    public class LibChangedEventArgs : EventArgs
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LibChangedEventArgs"/> class.
        /// </summary>
        /// <param name="folder">The folder<see cref="string"/></param>
        /// <param name="filePath">The filePath<see cref="string"/></param>
        /// <param name="changeType">The changeType<see cref="WatcherChangeTypes"/></param>
        public LibChangedEventArgs(string folder, string filePath, WatcherChangeTypes changeType)
        {
            Folder = folder;
            FilePath = filePath;
            ChangeType = changeType;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the ChangeType
        /// </summary>
        public WatcherChangeTypes ChangeType { get; }

        /// <summary>
        /// Gets the FilePath
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the Folder
        /// </summary>
        public string Folder { get; }

        #endregion
    }
}

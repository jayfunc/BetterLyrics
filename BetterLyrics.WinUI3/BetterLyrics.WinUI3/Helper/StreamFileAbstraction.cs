using System;
using System.IO;

namespace BetterLyrics.WinUI3.Helper
{
    public class StreamFileAbstraction : TagLib.File.IFileAbstraction
    {
        private readonly string _name;
        private readonly Stream _stream;
        private readonly bool _closeStreamOnDispose;

        public StreamFileAbstraction(string path, Stream stream, bool closeStreamOnDispose = false)
        {
            _name = Path.GetFileName(path);
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _closeStreamOnDispose = closeStreamOnDispose;
        }

        public string Name => _name;

        public Stream ReadStream => _stream;

        public Stream WriteStream
        {
            get
            {
                if (_stream.CanWrite)
                {
                    return _stream;
                }
                throw new InvalidOperationException("The underlying stream is read-only. Tag saving is not supported for this source.");
            }
        }

        public void CloseStream(Stream stream)
        {
            if (_closeStreamOnDispose)
            {
                stream?.Dispose();
            }
        }
    }
}

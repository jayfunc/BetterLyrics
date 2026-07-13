using File = TagLib.File;

namespace BetterLyrics.Core.Helpers;

public class StreamFileAbstraction : File.IFileAbstraction
{
    private readonly bool _closeStreamOnDispose;

    public StreamFileAbstraction(string path, Stream? stream, bool closeStreamOnDispose = false)
    {
        Name = Path.GetFileName(path);
        ReadStream = stream ?? throw new ArgumentNullException(nameof(stream));
        _closeStreamOnDispose = closeStreamOnDispose;
    }

    public string Name { get; }

    public Stream ReadStream { get; }

    public Stream WriteStream
    {
        get
        {
            if (ReadStream.CanWrite) return ReadStream;
            throw new InvalidOperationException(
                "The underlying stream is read-only. Tag saving is not supported for this source.");
        }
    }

    public void CloseStream(Stream stream)
    {
        if (_closeStreamOnDispose) stream?.Dispose();
    }
}
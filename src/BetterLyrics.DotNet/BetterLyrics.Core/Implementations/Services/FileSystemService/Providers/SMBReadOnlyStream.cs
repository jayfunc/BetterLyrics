using System.Diagnostics;
using SMBLibrary;
using SMBLibrary.Client;

namespace BetterLyrics.Core.Implementations.Services.FileSystemService.Providers;

public class SMBReadOnlyStream : Stream
{
    private readonly object _handle;
    private readonly long _length;
    private readonly ISMBFileStore _store;
    private long _position;

    public SMBReadOnlyStream(ISMBFileStore store, object handle)
    {
        _store = store;
        _handle = handle;
        _position = 0;
        _buffer = new byte[BufferSize];

        var status = _store.GetFileInformation(out var result, handle, FileInformationClass.FileStandardInformation);
        if (status == NTStatus.STATUS_SUCCESS && result is FileStandardInformation info)
        {
            _length = info.EndOfFile;
        }
        else
        {
            _length = 0; // 这是一个风险点，但为了不 crash 先设为 0
            Debug.WriteLine($"SMB GetLength Error: {status}");
        }
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set => _position = value;
    }

    private byte[] _buffer;
    private long _bufferStart = -1;
    private int _bufferLength = 0;
    // 256KB 缓存区
    private const int BufferSize = 256 * 1024; 

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_position >= _length) return 0;

        // 如果要读的数据完全在缓存里，直接命中缓存返回
        if (_bufferStart != -1 && _position >= _bufferStart && _position + count <= _bufferStart + _bufferLength)
        {
            int bufferOffset = (int)(_position - _bufferStart);
            Array.Copy(_buffer, bufferOffset, buffer, offset, count);
            _position += count;
            return count;
        }

        // 缓存没命中，去网络请求
        long remainingFile = _length - _position;
        int bytesToRequest = (int)Math.Min(Math.Max(count, BufferSize), remainingFile);
        
        // 由于 SMB 可能会限制单次请求大小，但这没关系，SMBLibrary 会返回它能给的最大实际数据
        var status = _store.ReadFile(out var data, _handle, _position, bytesToRequest);

        if (status == NTStatus.STATUS_END_OF_FILE || data == null || data.Length == 0) return 0;
        if (status != NTStatus.STATUS_SUCCESS)
            throw new IOException($"SMB Read failed. Status: {status}, Position: {_position}, ChunkReq: {bytesToRequest}");

        // 更新缓存
        _bufferStart = _position;
        _bufferLength = data.Length;

        if (data.Length > _buffer.Length)
        {
            _buffer = new byte[data.Length];
        }
        Array.Copy(data, 0, _buffer, 0, data.Length);

        // 返回给调用者实际需要的大小
        int bytesToReturn = Math.Min(count, data.Length);
        Array.Copy(data, 0, buffer, offset, bytesToReturn);
        _position += bytesToReturn;

        return bytesToReturn;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_position >= _length) return 0;
        int count = buffer.Length;

        // 如果要读的数据完全在缓存里，直接命中缓存返回
        if (_bufferStart != -1 && _position >= _bufferStart && _position + count <= _bufferStart + _bufferLength)
        {
            int bufferOffset = (int)(_position - _bufferStart);
            new Span<byte>(_buffer, bufferOffset, count).CopyTo(buffer.Span);
            _position += count;
            return count;
        }

        // 缓存没命中，去网络请求
        long remainingFile = _length - _position;
        int bytesToRequest = (int)Math.Min(Math.Max(count, BufferSize), remainingFile);
        
        // 由于 SMB 可能会限制单次请求大小，但这没关系，SMBLibrary 会返回它能给的最大实际数据
        // 使用包装方法以避免在 Task.Run 里遇到 out 参数的编译错误
        var (status, data) = await Task.Run(() => 
        {
            var st = _store.ReadFile(out var d, _handle, _position, bytesToRequest);
            return (st, d);
        }, cancellationToken).ConfigureAwait(false);

        if (status == NTStatus.STATUS_END_OF_FILE || data == null || data.Length == 0) return 0;
        if (status != NTStatus.STATUS_SUCCESS)
            throw new IOException($"SMB Read failed. Status: {status}, Position: {_position}, ChunkReq: {bytesToRequest}");

        // 更新缓存
        _bufferStart = _position;
        _bufferLength = data.Length;

        if (data.Length > _buffer.Length)
        {
            _buffer = new byte[data.Length];
        }
        Array.Copy(data, 0, _buffer, 0, data.Length);

        // 返回给调用者实际需要的大小
        int bytesToReturn = Math.Min(count, data.Length);
        new Span<byte>(data, 0, bytesToReturn).CopyTo(buffer.Span);
        _position += bytesToReturn;

        return bytesToReturn;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var newPos = _position;

        switch (origin)
        {
            case SeekOrigin.Begin:
                newPos = offset;
                break;
            case SeekOrigin.Current:
                newPos = _position + offset;
                break;
            case SeekOrigin.End:
                newPos = _length + offset;
                break;
        }

        if (newPos < 0) throw new IOException("Seek before beginning.");

        _position = newPos;
        return _position;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            try
            {
                _store.CloseFile(_handle);
            }
            catch
            {
            }
    }
}
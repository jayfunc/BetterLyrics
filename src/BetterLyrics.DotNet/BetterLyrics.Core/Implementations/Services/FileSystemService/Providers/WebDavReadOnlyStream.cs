using System.Net.Http.Headers;

namespace BetterLyrics.Core.Implementations.Services.FileSystemService.Providers;

public class WebDavReadOnlyStream : Stream
{
    private readonly HttpClient _httpClient;
    private readonly string _uri;
    private readonly long _length;
    private long _position;
    private byte[] _buffer;
    private long _bufferStart = -1;
    private int _bufferLength = 0;
    // 256KB 缓存区
    private const int BufferSize = 256 * 1024; 

    public WebDavReadOnlyStream(HttpClient httpClient, string uri, long length)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _uri = uri ?? throw new ArgumentNullException(nameof(uri));
        _length = length;
        _position = 0;
        _buffer = new byte[BufferSize];
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

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Task.Run(() => ReadAsync(buffer, offset, count, CancellationToken.None)).GetAwaiter().GetResult();
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_position >= _length) return 0;
        int count = buffer.Length;

        // 如果要读的数据完全在缓存里，直接命中缓存返回！光速！
        if (_bufferStart != -1 && _position >= _bufferStart && _position + count <= _bufferStart + _bufferLength)
        {
            int bufferOffset = (int)(_position - _bufferStart);
            new Span<byte>(_buffer, bufferOffset, count).CopyTo(buffer.Span);
            _position += count;
            return count;
        }

        // 缓存没命中（或者需要跨区读），去网络请求
        long remainingFile = _length - _position;
        int bytesToRequest = (int)Math.Min(Math.Max(count, BufferSize), remainingFile);

        using var request = new HttpRequestMessage(HttpMethod.Get, _uri);
        request.Headers.Range = new RangeHeaderValue(_position, _position + bytesToRequest - 1);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new IOException($"WebDAV Range Read failed. Status: {response.StatusCode}");
        }

        var data = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        if (data == null || data.Length == 0) return 0;

        // 更新缓存
        _bufferStart = _position;
        _bufferLength = data.Length;

        // 如果请求的块异常大，临时扩容缓存数组
        if (data.Length > _buffer.Length)
        {
            _buffer = new byte[data.Length];
        }
        Array.Copy(data, 0, _buffer, 0, data.Length);

        // 返回给调用者它实际请求的大小（或者读取到的最大大小）
        int bytesToReturn = Math.Min(count, data.Length);
        new Span<byte>(data, 0, bytesToReturn).CopyTo(buffer.Span);
        _position += bytesToReturn;

        return bytesToReturn;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        long newPos = _position;
        switch (origin)
        {
            case SeekOrigin.Begin: newPos = offset; break;
            case SeekOrigin.Current: newPos = _position + offset; break;
            case SeekOrigin.End: newPos = _length + offset; break;
        }
        
        if (newPos < 0) throw new IOException("Seek before beginning.");
        _position = newPos;
        
        return _position;
    }

    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

using System.Diagnostics;
using SMBLibrary;
using SMBLibrary.Client;

namespace BetterLyrics.Core.Implementations.Services.FileSystemService.Providers;

public class SMBReadOnlyStream : Stream
{
    // SMB 协议建议的最大读取块大小 (64KB 是最安全的通用值)
    private const int MaxReadChunkSize = 65536;
    private readonly object _handle;
    private readonly long _length;
    private readonly ISMBFileStore _store;
    private long _position;

    public SMBReadOnlyStream(ISMBFileStore store, object handle)
    {
        _store = store;
        _handle = handle;
        _position = 0;

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

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_position >= _length) return 0;

        var totalBytesRead = 0;
        var remainingRequest = count;

        // 循环读取，直到读完请求的数量，或者文件结束
        while (remainingRequest > 0)
        {
            // 计算剩余文件长度
            var remainingFile = _length - _position;
            if (remainingFile <= 0) break; // 已到末尾

            // 计算本次 SMB 请求的大小 (取三者最小值：请求剩余量、文件剩余量、SMB最大块限制)
            var bytesToReadThisChunk = (int)Math.Min(Math.Min(remainingRequest, remainingFile), MaxReadChunkSize);

            // 发送 SMB 请求
            var status = _store.ReadFile(out var data, _handle, _position, bytesToReadThisChunk);

            // 处理结果
            if (status == NTStatus.STATUS_END_OF_FILE) break;

            if (status != NTStatus.STATUS_SUCCESS)
                // 遇到错误抛出详细信息
                throw new IOException(
                    $"SMB Read failed. Status: {status}, Position: {_position}, ChunkReq: {bytesToReadThisChunk}");

            if (data == null || data.Length == 0) break;

            // 复制数据到输出 buffer
            Array.Copy(data, 0, buffer, offset + totalBytesRead, data.Length);

            // 更新指针和计数器
            _position += data.Length;
            totalBytesRead += data.Length;
            remainingRequest -= data.Length;

            // 如果实际读到的比请求的少，通常意味着提前到了 EOF，或者网络包较小
            // 这里选择继续循环尝试，直到读不够或者明确 EOF
            if (data.Length < bytesToReadThisChunk) break;
        }

        return totalBytesRead;
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
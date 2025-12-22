using SMBLibrary;
using SMBLibrary.Client;
using System;
using System.IO;

namespace BetterLyrics.WinUI3.Models.FileSystem
{
    public partial class SMBReadOnlyStream : Stream
    {
        private readonly ISMBFileStore _store;
        private readonly object _handle;
        private long _position;
        private long _length; // 新增：缓存文件长度

        public SMBReadOnlyStream(ISMBFileStore store, object handle)
        {
            _store = store;
            _handle = handle;
            _position = 0;

            var status = _store.GetFileInformation(out FileInformation result, handle, FileInformationClass.FileStandardInformation);
            if (status == NTStatus.STATUS_SUCCESS && result is FileStandardInformation info)
            {
                _length = info.EndOfFile;
            }
            else
            {
                // 如果获取失败，这是一个严重问题，意味着无法 Seek 到末尾
                // 暂时设为 0，但后续读取可能会出问题
                _length = 0;
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
            // 保护：如果位置已经超过文件末尾，直接返回 0 (EOF)
            if (_position >= _length) return 0;

            // 保护：防止读取越界 (请求读取量不能超过剩余量)
            long remaining = _length - _position;
            int bytesToRequest = (int)Math.Min(count, remaining);

            // 为了安全，保留对 remaining 的检查是必须的
            if (bytesToRequest <= 0) return 0;

            var status = _store.ReadFile(out byte[] data, _handle, _position, bytesToRequest);

            if (status == NTStatus.STATUS_END_OF_FILE) return 0;

            if (status != NTStatus.STATUS_SUCCESS)
            {
                throw new IOException($"SMB Read failed. Status: {status} (Pos: {_position}, Req: {bytesToRequest})");
            }

            if (data == null || data.Length == 0) return 0;

            Array.Copy(data, 0, buffer, offset, data.Length);
            _position += data.Length;
            return data.Length;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPos = _position;

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

            // 允许 Seek 超过 EOF (标准 Stream 行为)，但在 Read 时会返回 0
            if (newPos < 0)
            {
                throw new IOException("An attempt was made to move the file pointer before the beginning of the file.");
            }

            _position = newPos;
            return _position;
        }

        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() { }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                try { _store.CloseFile(_handle); } catch { }
            }
        }
    }
}

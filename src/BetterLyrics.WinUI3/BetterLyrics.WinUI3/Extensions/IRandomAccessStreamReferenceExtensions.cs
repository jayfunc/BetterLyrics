using System;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Buffer = Windows.Storage.Streams.Buffer;

namespace BetterLyrics.WinUI3.Extensions;

public static class IRandomAccessStreamReferenceExtensions
{
    extension(IRandomAccessStreamReference reference)
    {
        public async Task<IBuffer> ToBufferAsync()
        {
            using IRandomAccessStream stream = await reference.OpenReadAsync();
            stream.Seek(0);
            var buffer = new Buffer((uint)stream.Size);
            await stream.ReadAsync(buffer, (uint)stream.Size, InputStreamOptions.None);
            return buffer;
        }

        public async Task<byte[]?> ToByteArrayAsync()
        {
            if (reference == null) return null;

            using (var stream = await reference.OpenReadAsync())
            {
                using (var reader = new DataReader(stream.GetInputStreamAt(0)))
                {
                    await reader.LoadAsync((uint)stream.Size);

                    var bytes = new byte[stream.Size];
                    reader.ReadBytes(bytes);
                    return bytes;
                }
            }
        }
    }
}
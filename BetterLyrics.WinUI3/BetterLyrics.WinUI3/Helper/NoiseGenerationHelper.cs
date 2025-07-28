using System;
using System.Buffers;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using static BetterLyrics.WinUI3.Helper.NoiseOverlayHelper.BitmapFileCreator;

namespace BetterLyrics.WinUI3.Helper
{
    internal static class NoiseOverlayHelper
    {

        const string NoiseOverlayFileName = "noise_overlay.bmp";

        static readonly string NoiseOverlayFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "Assets", NoiseOverlayFileName);

        /// <summary>
        /// 生成 BGRA 格式的灰阶噪声像素数据
        /// </summary>
        public static byte[] GenerateNoiseBitmapBGRA(int width, int height)
        {
            var random = new Random();
            var pixelData = new byte[width * height * 4];
            for (int i = 0; i < width * height; i++)
            {
                byte gray = (byte)random.Next(0, 256);
                pixelData[i * 4 + 0] = gray; // B
                pixelData[i * 4 + 1] = gray; // G
                pixelData[i * 4 + 2] = gray; // R
                pixelData[i * 4 + 3] = 255;  // A
            }
            return pixelData;
        }

        /// <summary>
        /// 生成单色灰阶随机噪声
        /// </summary>
        /// <param name="outputPath">输出文件路径</param>
        /// <param name="width">图片宽度</param>
        /// <param name="height">图片高度</param>
        public static BitmapFile GenerateNoiseBitmap(int width, int height)
        {
            const uint NumOfGrayscale = 16;
            uint bitCount = NextPowerOfTwo((uint)Math.Round(Math.Sqrt(NumOfGrayscale)));

            var palette = BitmapFileCreator.CreateGrayscalePalette(16);
            var pixelData = GenerateRandomNoise(width, height, bitCount);

            var fileHeader = BitmapFileCreator.CreateFileHeader(palette, pixelData);
            var infoHeader = BitmapFileCreator.CreateInfoHeader(width, height, bitCount);

            return new BitmapFile(fileHeader, infoHeader, palette, pixelData);
        }

        /// <summary>
        /// 读取噪声图片的位头信息
        /// </summary>
        public static BitmapFileCreator.WINBMPINFOHEADER ReadBitmapInfoHeaders(string? FilePath)
        {
            var _filePath = FilePath ?? NoiseOverlayFilePath;
            using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);

            // 跳过文件头
            fs.Seek(Marshal.SizeOf<BitmapFileCreator.BITMAPFILEHEADER>(), SeekOrigin.Begin);

            // 读取信息头
            byte[] infoHeaderBytes = br.ReadBytes(Marshal.SizeOf<BitmapFileCreator.WINBMPINFOHEADER>());
            return MemoryMarshal.Read<BitmapFileCreator.WINBMPINFOHEADER>(infoHeaderBytes);
        }

        public static BitmapFileCreator.WINBMPINFOHEADER ReadBitmapInfoHeaders(byte[] FileBytes)
        {
            // 跳过文件头
            var offset = Marshal.SizeOf<BitmapFileCreator.BITMAPFILEHEADER>();

            // 读取信息头
            var infoHeaderLength = Marshal.SizeOf<BitmapFileCreator.WINBMPINFOHEADER>();
            Span<byte> infoHeaderBytes = new(FileBytes, offset, infoHeaderLength);
            return MemoryMarshal.Read<BitmapFileCreator.WINBMPINFOHEADER>(infoHeaderBytes);
        }

        /// <summary>
        /// safe 的写入 struct 到流
        /// </summary>
        /// <typeparam name="T">值 strcut</typeparam>
        /// <param name="stream">要写入的字节流</param>
        /// <param name="structure">要被写入的结构</param>
        public static void WriteStruct<T>(Stream stream, in T structure) where T : struct
        {
            int size = Unsafe.SizeOf<T>();
            byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                MemoryMarshal.Write(buffer, in structure);
                stream.Write(buffer, 0, size);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// 随机填充位图内容
        /// </summary>
        /// <param name="width">填充宽度</param>
        /// <param name="height">填充高度</param>
        /// <param name="bitCount">单个调色盘索引所占比特位数</param>
        /// <returns>字节数据</returns>
        private static byte[] GenerateRandomNoise(int width, int height, uint bitCount)
        {
            // 创建位图行字节数，4K 对齐
            int rowSize = ((width * (int)bitCount + 31) >> 5) << 2;

            // 创建随机位图数据
            Random rnd = new();
            return Enumerable.Range(0, rowSize * height)
                .Select(i => (byte)rnd.Next(0x00, 0xFF))
                .ToArray();
        }

        private static uint NextPowerOfTwo(uint value)
        {
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return value + 1;
        }
        public static class BitmapFileCreator
        {

            /// <summary>
            /// 创建BMP文件头
            /// </summary>
            /// <param name="palette">调色盘数据</param>
            /// <param name="pixelData">像素数据</param>
            /// <returns>文件头结构</returns>
            public static BITMAPFILEHEADER CreateFileHeader(byte[] palette, byte[] pixelData)
            {
                return new BITMAPFILEHEADER
                {
                    bfType = 0x4D42,
                    bfSize = (uint)(Marshal.SizeOf<BITMAPFILEHEADER>() +
                                    Marshal.SizeOf<WINBMPINFOHEADER>() +
                                    palette.Length +
                                    pixelData.Length),
                    bfReserved1 = 0,
                    bfReserved2 = 0,
                    bfOffBits = (uint)(Marshal.SizeOf<BITMAPFILEHEADER>() +
                                        Marshal.SizeOf<WINBMPINFOHEADER>() +
                                        palette.Length)
                };
            }

            /// <summary>
            /// 将指定值填充到为最接近的2的幂数
            /// </summary>
            /// <summary>
            /// 生成灰阶调色盘
            /// </summary>
            /// <param name="colors">灰阶数量</param>
            /// <returns>调色盘byte数组</returns>
            public static byte[] CreateGrayscalePalette(int colors)
            {
                return Enumerable.Range(0, colors)
                    .SelectMany(i => Enumerable.Repeat((byte)(i * 0x10), 4))
                    .ToArray();
            }

            /// <summary>
            /// 创建BMP信息头
            /// </summary>
            /// <param name="width">宽度</param>
            /// <param name="height">高度</param>
            /// <param name="bitCount">单个像素（调色盘索引）位数</param>
            /// <returns>BMP信息头结构</returns>
            public static WINBMPINFOHEADER CreateInfoHeader(int width, int height, uint bitCount)
            {
                return new WINBMPINFOHEADER
                {
                    biSize = (uint)Marshal.SizeOf<WINBMPINFOHEADER>(),
                    biWidth = (uint)width,
                    biHeight = (uint)height,
                    biPlanes = 1,
                    biBitCount = (ushort)bitCount,
                    biCompression = 0,
                    biSizeImage = 0,
                    biXPelsPerMeter = 0,
                    biYPelsPerMeter = 0,
                    biClrUsed = 0,
                    biClrImportant = 0
                };
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            /// <summary>
            /// BMP 位图文件头
            /// </summary>
            public struct BITMAPFILEHEADER
            {
                /// <summary>
                /// 文件类型标识，用于指定文件格式
                /// </summary>
                public ushort bfType;
                /// <summary>
                /// 文件大小，以字节为单位
                /// </summary>
                public uint bfSize;
                /// <summary>
                /// 保留字段，未使用
                /// </summary>
                public ushort bfReserved1;
                /// <summary>
                /// 保留字段，未使用
                /// </summary>
                public ushort bfReserved2;
                /// <summary>
                /// 像素数据的起始位置，以字节为单位，从文件头开始计算
                /// </summary>
                public uint bfOffBits;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            /// <summary>
            /// BMP 位图信息头
            /// </summary>
            public struct WINBMPINFOHEADER
            {
                /// <summary>
                /// 指定此结构体的字节大小。
                /// </summary>
                public uint biSize;

                /// <summary>
                /// 以像素为单位的位图宽度。
                /// </summary>
                public uint biWidth;

                /// <summary>
                /// 以像素为单位的位图高度。
                /// </summary>
                public uint biHeight;

                /// <summary>
                /// 目标设备的平面数，通常为1。
                /// </summary>
                public ushort biPlanes;

                /// <summary>
                /// 每个像素的位数，表示颜色深度，如1、4、8、16、24、32等。
                /// </summary>
                public ushort biBitCount;

                /// <summary>
                /// 压缩类型，0表示不压缩。
                /// </summary>
                public uint biCompression;

                /// <summary>
                /// 位图图像数据的大小（以字节为单位），若图像未压缩，该值可设为0。
                /// </summary>
                public uint biSizeImage;

                /// <summary>
                /// 每米X轴方向的像素数（水平分辨率），通常设为0。
                /// </summary>
                public uint biXPelsPerMeter;

                /// <summary>
                /// 每米Y轴方向的像素数（垂直分辨率），通常设为0。
                /// </summary>
                public uint biYPelsPerMeter;

                /// <summary>
                /// 实际使用的颜色索引数，若为0，则使用位图中实际出现的颜色数。
                /// </summary>
                public uint biClrUsed;

                /// <summary>
                /// 重要颜色索引数，0表示所有颜色都重要。
                /// </summary>
                public uint biClrImportant;
            }
        }

        public class BitmapFile(BitmapFileCreator.BITMAPFILEHEADER fileHeader, BitmapFileCreator.WINBMPINFOHEADER infoHeader, byte[] palette, byte[] pixelData)
        {
            public BITMAPFILEHEADER FileHeader = fileHeader;
            public WINBMPINFOHEADER InfoHeader = infoHeader;
            public byte[] Palette = palette;
            public byte[] PixelData = pixelData;

            /// <summary>
            /// 转换为byte[]
            /// </summary>
            /// <param name="bf"></param>
            public static explicit operator byte[](BitmapFile bf)
            {
                var result = new byte[bf.FileHeader.bfSize];
                var ms = new MemoryStream(result);
                bf.WriteToStream(ms);
                return result;
            }

            public byte[] ToByteArray() => (byte[])this;

            public Task<byte[]> ToByteArrayAsync() { return Task.FromResult(ToByteArray());}

            /// <summary>
            /// 写入此结构到流中
            /// </summary>
            public void WriteToStream(Stream stream)
            { 
                NoiseOverlayHelper.WriteStruct(stream, FileHeader);
                NoiseOverlayHelper.WriteStruct(stream, InfoHeader);
                stream.Write(Palette);
                stream.Write(PixelData);
                stream.Flush();
            }
        }
    }
}

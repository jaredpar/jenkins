using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Sql
{
    internal static class ZipUtil
    {
        internal static readonly Encoding TextEncoding = Encoding.UTF8;

        internal static byte[] CompressText(string text)
        {
            var bytes = TextEncoding.GetBytes(text);
            using (var destStream = new MemoryStream())
            using (var zipStream = new GZipStream(destStream, CompressionMode.Compress, leaveOpen: true))
            {
                zipStream.Write(bytes, 0, bytes.Length);
                zipStream.Close();

                var compressedBytes = new byte[destStream.Length];
                destStream.Position = 0;
                destStream.Read(compressedBytes, 0, compressedBytes.Length);
                return compressedBytes;
            }
        }

        internal static string DecompressText(byte[] data)
        {
            using (var destStream = new MemoryStream())
            using (var sourceStream = new MemoryStream(data))
            using (var zipStream = new GZipStream(sourceStream, CompressionMode.Decompress, leaveOpen: true))
            {
                zipStream.CopyTo(destStream);
                zipStream.Close();

                var bytes = new byte[destStream.Length];
                destStream.Position = 0;
                destStream.Read(bytes, 0, bytes.Length);
                return TextEncoding.GetString(bytes);
            }
        }
    }
}

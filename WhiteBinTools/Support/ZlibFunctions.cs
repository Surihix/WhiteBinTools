using Ionic.Zlib;
using System.IO;

namespace WhiteBinTools.Support
{
    internal class ZlibFunctions
    {
        public static void ZlibDecompress(Stream cmpStreamName, Stream outStreamName)
        {
            using (ZlibStream decompressor = new ZlibStream(cmpStreamName, CompressionMode.Decompress))
            {
                decompressor.CopyTo(outStreamName);
            }
        }

        public static byte[] ZlibDecompressBuffer(byte[] cmpData)
        {
            return ZlibStream.UncompressBuffer(cmpData);
        }

        public static byte[] ZlibCompress(string fileToCmp)
        {
            var dataToCompressBuffer = File.ReadAllBytes(fileToCmp);
            var compressedDataBuffer = ZlibStream.CompressBuffer(dataToCompressBuffer);

            return compressedDataBuffer;
        }

        public static byte[] ZlibCompressBuffer(byte[] dataToCmp)
        {
            var compressedDataBuffer = ZlibStream.CompressBuffer(dataToCmp);

            return compressedDataBuffer;
        }
    }
}
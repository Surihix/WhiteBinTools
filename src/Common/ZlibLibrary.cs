using Ionic.Zlib;
using System.IO;

namespace WhiteBinTools.src.Common
{
    internal class ZlibLibrary
    {
        public static void ZlibDecompress(Stream CmpStreamName, Stream OutStreamName)
        {
            using (ZlibStream Decompressor = new ZlibStream(CmpStreamName, CompressionMode.Decompress))
            {
                Decompressor.CopyTo(OutStreamName);
            }
        }

        public static void ZlibCompress(string FileToCmp, string NewCmpFile, CompressionLevel lvl)
        {
            byte[] DataToCompress = File.ReadAllBytes(FileToCmp);
            using (FileStream OutStream = new FileStream(NewCmpFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                using (ZlibStream zlib = new ZlibStream(OutStream, CompressionMode.Compress, lvl))
                {
                    zlib.Write(DataToCompress, 0, DataToCompress.Length);
                }
            }
        }
    }
}
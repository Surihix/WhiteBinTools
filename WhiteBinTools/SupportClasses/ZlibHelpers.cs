﻿using Ionic.Zlib;
using System.IO;

namespace WhiteBinTools.SupportClasses
{
    internal static class ZlibHelpers
    {
        public static void ZlibDecompress(this Stream cmpStreamName, Stream outStreamName)
        {
            using (ZlibStream decompressor = new ZlibStream(cmpStreamName, CompressionMode.Decompress))
            {
                decompressor.CopyTo(outStreamName);
            }
        }

        public static byte[] ZlibCompress(this string fileToCmp)
        {
            var dataToCompressBuffer = File.ReadAllBytes(fileToCmp);
            var compressedDataBuffer = ZlibStream.CompressBuffer(dataToCompressBuffer);

            return compressedDataBuffer;
        }
    }
}
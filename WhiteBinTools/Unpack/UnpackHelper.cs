using System.IO;
using WhiteBinTools.Support;
using WhiteBinTools.Support.Structures;
using static WhiteBinTools.Support.Enumerators;

namespace WhiteBinTools.Unpack
{
    internal class UnpackHelper
    {
        public static UnpackedState UnpackFile(WhiteFileInfoData whiteFileInfoData, string unpackDir, ref int duplicateCounter, FileStream whiteBinStream)
        {
            UnpackedState unpackedState;

            whiteFileInfoData.FilePath = whiteFileInfoData.FilePath.Replace('/', Path.DirectorySeparatorChar);
            var fileDir = Path.Combine(unpackDir, Path.GetDirectoryName(whiteFileInfoData.FilePath));

            if (!Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            var outFile = Path.Combine(unpackDir, whiteFileInfoData.FilePath);

            if (File.Exists(outFile))
            {
                File.Delete(outFile);
                duplicateCounter++;
            }

            _ = whiteBinStream.Seek(whiteFileInfoData.FilePosition, SeekOrigin.Begin);

            using (var outFileStream = new FileStream(outFile, FileMode.Create, FileAccess.Write))
            {
                if (whiteFileInfoData.IsCompressed)
                {
                    var cmpData = new byte[whiteFileInfoData.CmpSize];
                    _ = whiteBinStream.Read(cmpData, 0, cmpData.Length);

                    var uncmpData = ZlibFunctions.ZlibDecompressBuffer(cmpData);
                    outFileStream.Write(uncmpData, 0, uncmpData.Length);

                    unpackedState = UnpackedState.Decompressed;
                }
                else
                {
                    whiteBinStream.CopyStreamTo(outFileStream, whiteFileInfoData.UncmpSize, false);
                    unpackedState = UnpackedState.Copied;
                }
            }

            return unpackedState;
        }
    }
}
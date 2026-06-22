using System.IO;
using System.Text;
using WhiteBinTools.Support;
using WhiteBinTools.Support.Structures;
using static WhiteBinTools.Support.Enumerators;

namespace WhiteBinTools.Repack
{
    internal class RepackHelpers
    {
        private static byte[] CmpData { get; set; }
        private static bool HasCheckedInject { get; set; }

        public static string RepackAppend(ref RepackedState repackedState, WhiteFileInfoData whiteFileInfoData, string outFile, FileStream whiteBinStream)
        {
            long position = whiteBinStream.Length;

            if (position % 2048 != 0)
            {
                var remainder = position % 2048;
                var increaseBytes = 2048 - remainder;
                var newPos = position + increaseBytes;
                var padNulls = newPos - position;

                whiteBinStream.PadNull(padNulls);

                position = whiteBinStream.Length;
            }

            return RepackFile(position, whiteFileInfoData, outFile, whiteBinStream, ref repackedState);
        }

        public static bool DetermineInject(WhiteFileInfoData whiteFileInfoData, string outFile)
        {
            if (whiteFileInfoData.IsCompressed)
            {
                CmpData = ZlibFunctions.ZlibCompressBuffer(File.ReadAllBytes(outFile));
                HasCheckedInject = true;

                if (CmpData.Length <= whiteFileInfoData.CmpSize)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                var fileSize = (uint)new FileInfo(outFile).Length;

                if (fileSize <= whiteFileInfoData.UncmpSize)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static void CleanOldFile(FileStream whiteBinStream, long position, uint oldFileSize)
        {
            whiteBinStream.Seek(position, SeekOrigin.Begin);
            whiteBinStream.PadNull(oldFileSize);
        }

        public static string RepackInject(WhiteFileInfoData whiteFileInfoData, string outFile, FileStream whiteBinStream, ref RepackedState repackedState)
        {
            whiteBinStream.Seek(whiteFileInfoData.FilePosition, SeekOrigin.Begin);

            return RepackFile(whiteFileInfoData.FilePosition, whiteFileInfoData, outFile, whiteBinStream, ref repackedState);
        }

        private static string RepackFile(long position, WhiteFileInfoData whiteFileInfoData, string outFile, FileStream whiteBinStream, ref RepackedState repackedState)
        {
            var whiteFileInfoStringBuilder = new StringBuilder();

            whiteFileInfoStringBuilder.Append((position / 2048).ToString("x"));
            whiteFileInfoStringBuilder.Append(":");

            var fileSize = (uint)new FileInfo(outFile).Length;

            whiteFileInfoStringBuilder.Append(fileSize.ToString("x"));
            whiteFileInfoStringBuilder.Append(":");

            if (whiteFileInfoData.IsCompressed)
            {
                if (HasCheckedInject)
                {
                    whiteFileInfoStringBuilder.Append(CmpData.Length.ToString("x"));
                    whiteBinStream.Write(CmpData, 0, CmpData.Length);
                    HasCheckedInject = false;
                }
                else
                {
                    var cmpData = ZlibFunctions.ZlibCompressBuffer(File.ReadAllBytes(outFile));

                    if (cmpData.Length == 0)
                    {
                        whiteFileInfoStringBuilder.Append((8).ToString("x"));

                        var emptyCmpData = new byte[8];
                        emptyCmpData[0] = 0x78;
                        emptyCmpData[1] = 0xDA;
                        emptyCmpData[2] = 0x03;
                        emptyCmpData[7] = 0x01;

                        whiteBinStream.Write(emptyCmpData, 0, emptyCmpData.Length);
                    }
                    else
                    {
                        whiteFileInfoStringBuilder.Append(cmpData.Length.ToString("x"));
                        whiteBinStream.Write(cmpData, 0, cmpData.Length);
                    }
                }

                whiteFileInfoStringBuilder.Append(":");

                repackedState = RepackedState.Compressed;
            }
            else
            {
                whiteFileInfoStringBuilder.Append(fileSize.ToString("x"));
                whiteFileInfoStringBuilder.Append(":");

                using (var outFileStream = new FileStream(outFile, FileMode.Open, FileAccess.Read))
                {
                    outFileStream.CopyTo(whiteBinStream);
                }

                repackedState = RepackedState.Copied;
            }

            if (whiteFileInfoData.FilePath.StartsWith("noPath") || whiteFileInfoData.IsPathGenerated)
            {
                whiteFileInfoStringBuilder.Append(" ");
            }
            else
            {
                var currentFilePath = whiteFileInfoData.FilePath;
                whiteFileInfoStringBuilder.Append(currentFilePath.Replace("\\", "/"));
            }

            whiteFileInfoStringBuilder.Append("\0");

            return whiteFileInfoStringBuilder.ToString();
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WhiteBinTools.Filelist;
using WhiteBinTools.Support;
using static WhiteBinTools.Support.ProgramEnums;

namespace WhiteBinTools.Repack
{
    internal class RepackProcesses
    {
        public static void PrepareRepackVars(RepackVariables repackVariables, string filelistFile, FilelistVariables filelistVariables, string extractedDir)
        {
            repackVariables.FilelistFileName = Path.GetFileName(filelistFile);
            repackVariables.NewFilelistFile = Path.Combine(filelistVariables.MainFilelistDirectory, repackVariables.FilelistFileName);
            repackVariables.NewWhiteBinFileName = Path.GetFileName(extractedDir).Remove(0, 1);
            repackVariables.NewWhiteBinFile = Path.Combine(Path.GetDirectoryName(extractedDir), repackVariables.NewWhiteBinFileName);
        }


        public static void CreateFilelistBackup(string filelistFile, RepackVariables repackVariables)
        {
            repackVariables.OldFilelistFileBckup = filelistFile + ".bak";
            IOhelpers.IfFileExistsDel(repackVariables.OldFilelistFileBckup);
            File.Copy(filelistFile, repackVariables.OldFilelistFileBckup);
        }

        
        public static void CreateWhiteBinBackup(string whiteBinFile, RepackVariables repackVariables)
        {
            repackVariables.OldWhiteBinFileBackup = whiteBinFile + ".bak";
            IOhelpers.IfFileExistsDel(repackVariables.OldWhiteBinFileBackup);
            File.Copy(repackVariables.NewWhiteBinFile, repackVariables.OldWhiteBinFileBackup);
        }


        public static void CreateEmptyNewChunksDict(FilelistVariables filelistVariables, Dictionary<int, List<byte>> newChunksDict)
        {
            for (int c = 0; c < filelistVariables.TotalChunks; c++)
            {
                var chunkDataList = new List<byte>();
                newChunksDict.Add(c, chunkDataList);
            }
        }


        public static void GetPackedState(string convertedString, RepackVariables repackVariables, string extractedDir)
        {
            repackVariables.ConvertedOgStringData = convertedString.Split(':');
            repackVariables.OgFilePos = Convert.ToUInt32(repackVariables.ConvertedOgStringData[0], 16) * 2048;
            repackVariables.OgUnCmpSize = Convert.ToUInt32(repackVariables.ConvertedOgStringData[1], 16);
            repackVariables.OgCmpSize = Convert.ToUInt32(repackVariables.ConvertedOgStringData[2], 16);
            repackVariables.OgMainPath = repackVariables.ConvertedOgStringData[3].Replace("/", Core.PathSeparatorChar);

            if (repackVariables.OgMainPath == " ")
            {
                repackVariables.OgNoPathFileCount++;
                repackVariables.OgDirectoryPath = "noPath";
                repackVariables.OgFileName = "FILE_" + repackVariables.OgNoPathFileCount;
                repackVariables.OgFullFilePath = Path.Combine(extractedDir, repackVariables.OgDirectoryPath, repackVariables.OgFileName);
                repackVariables.RepackPathInChunk = " ";
            }
            else
            {
                repackVariables.OgDirectoryPath = Path.GetDirectoryName(repackVariables.OgMainPath);
                repackVariables.OgFileName = Path.GetFileName(repackVariables.OgMainPath);
                repackVariables.OgFullFilePath = Path.Combine(extractedDir, repackVariables.OgDirectoryPath, repackVariables.OgFileName);
                repackVariables.RepackPathInChunk = repackVariables.OgMainPath.Replace(Core.PathSeparatorChar, "/");
            }

            if (repackVariables.OgUnCmpSize != repackVariables.OgCmpSize)
            {
                repackVariables.WasCompressed = true;
                repackVariables.RepackState = "Compressed";
            }
            else
            {
                repackVariables.WasCompressed = false;
                repackVariables.RepackState = "Copied";
            }

            repackVariables.RepackLogMsg = Path.Combine(repackVariables.OgDirectoryPath, repackVariables.OgFileName);
        }


        public static void RepackTypeAppend(RepackVariables repackVariables, FileStream newWhiteBinStream, string fileToAppend)
        {
            var filePositionInDecimal = (uint)newWhiteBinStream.Length;

            // Check if file position is divisible by 2048
            // and if its not divisible, add in null bytes
            // till next closest divisible number
            if (filePositionInDecimal % 2048 != 0)
            {
                var remainder = filePositionInDecimal % 2048;
                var increaseBytes = 2048 - remainder;
                var newPos = filePositionInDecimal + increaseBytes;
                var padNulls = newPos - filePositionInDecimal;

                newWhiteBinStream.Seek(filePositionInDecimal, SeekOrigin.Begin);
                newWhiteBinStream.PadNull(padNulls);

                filePositionInDecimal = (uint)newWhiteBinStream.Length;
            }

            var filePositionForChunk = filePositionInDecimal / 2048;
            repackVariables.AsciiFilePos = filePositionForChunk.ToString("x");

            var fileSizeInDecimal = (uint)new FileInfo(fileToAppend).Length;
            repackVariables.AsciiUnCmpSize = fileSizeInDecimal.ToString("x");

            newWhiteBinStream.Seek(filePositionInDecimal, SeekOrigin.Begin);
            RepackFiles(repackVariables, newWhiteBinStream, fileToAppend);
        }


        public static void RepackTypeInject(RepackVariables repackVariables, FileStream whiteBinStream, string fileToInject)
        {
            var filePositionForChunk = repackVariables.OgFilePos / 2048;
            repackVariables.AsciiFilePos = filePositionForChunk.ToString("x");

            var fileSizeInDecimal = (uint)new FileInfo(fileToInject).Length;
            repackVariables.AsciiUnCmpSize = fileSizeInDecimal.ToString("x");

            whiteBinStream.Seek(repackVariables.OgFilePos, SeekOrigin.Begin);
            RepackFiles(repackVariables, whiteBinStream, fileToInject);
        }


        private static void RepackFiles(RepackVariables repackVariables, FileStream whiteBinStream, string fileToPack)
        {
            if (repackVariables.WasCompressed)
            {
                var cmpData = ZlibMethods.ZlibCompress(fileToPack);
                whiteBinStream.Write(cmpData, 0, cmpData.Length);

                var cmpFileSizeInDecimal = (uint)cmpData.Length;
                repackVariables.AsciiCmpSize = cmpFileSizeInDecimal.ToString("x");
            }
            else
            {
                repackVariables.AsciiCmpSize = repackVariables.AsciiUnCmpSize;

                using (var unCmpFile = new FileStream(fileToPack, FileMode.Open, FileAccess.Read))
                {
                    unCmpFile.Seek(0, SeekOrigin.Begin);
                    unCmpFile.CopyTo(whiteBinStream);
                }
            }
        }


        public static void CleanOldFile(string whiteBinFile, uint filePos, uint size)
        {
            using (var fs = new FileStream(whiteBinFile, FileMode.Open, FileAccess.Write))
            {
                fs.Seek(filePos, SeekOrigin.Begin);
                fs.PadNull(size);
            }
        }


        public static void AppendProcess(RepackVariables repackVariables, ref string packedAs)
        {
            using (var appendBin = new FileStream(repackVariables.NewWhiteBinFile, FileMode.Append, FileAccess.Write))
            {
                packedAs = "(Appended)";
                RepackTypeAppend(repackVariables, appendBin, repackVariables.OgFullFilePath);
            }
        }


        public static void InjectProcess(RepackVariables repackVariables, ref string packedAs)
        {
            using (var injectBin = new FileStream(repackVariables.NewWhiteBinFile, FileMode.Open, FileAccess.ReadWrite))
            {
                packedAs = "(Injected)";
                RepackTypeInject(repackVariables, injectBin, repackVariables.OgFullFilePath);
            }
        }


        public static void BuildPathForChunk(RepackVariables repackVariables, GameCodes gameCode, FilelistVariables filelistVariables, Dictionary<int, List<byte>> newChunksDict)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(repackVariables.AsciiFilePos).Append(":").
                Append(repackVariables.AsciiUnCmpSize).Append(":").
                Append(repackVariables.AsciiCmpSize).Append(":").
                Append(repackVariables.RepackPathInChunk).Append("\0");

            var stringData = stringBuilder.ToString();

            if (gameCode == GameCodes.ff132)
            {
                newChunksDict[filelistVariables.CurrentChunkNumber].AddRange(Encoding.UTF8.GetBytes(stringData));
                filelistVariables.LastChunkNumber = filelistVariables.CurrentChunkNumber;
            }
            else
            {
                newChunksDict[filelistVariables.ChunkNumber].AddRange(Encoding.UTF8.GetBytes(stringData));
                filelistVariables.LastChunkNumber = filelistVariables.ChunkNumber;
            }
        }
    }
}
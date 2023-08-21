using System;
using System.IO;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.SupportClasses;

namespace WhiteBinTools.RepackClasses
{
    internal partial class RepackProcesses
    {
        public static void PrepareRepackVars(RepackProcesses repackVariables, string filelistFileVar, FilelistProcesses filelistVariables, string extractedDirVar)
        {
            repackVariables.FilelistFileName = Path.GetFileName(filelistFileVar);
            repackVariables.NewFilelistFile = filelistVariables.MainFilelistDirectory + "\\" + repackVariables.FilelistFileName;
            repackVariables.NewWhiteBinFileName = Path.GetFileName(extractedDirVar).Remove(0, 1);
            repackVariables.NewWhiteBinFile = filelistVariables.MainFilelistDirectory + "\\" + repackVariables.NewWhiteBinFileName;

            filelistVariables.DefaultChunksExtDir = extractedDirVar + "\\_chunks";
            filelistVariables.ChunkFile = filelistVariables.DefaultChunksExtDir + "\\chunk_";

            repackVariables.NewChunksExtDir = extractedDirVar + "\\_newChunks";
            repackVariables.NewChunkFile = repackVariables.NewChunksExtDir + "\\newChunk_";
        }


        public static void CreateFilelistBackup(string filelistFileVar, RepackProcesses repackVariables)
        {
            repackVariables.OldFilelistFileBckup = filelistFileVar + ".bak";
            repackVariables.OldFilelistFileBckup.IfFileExistsDel();
            File.Copy(filelistFileVar, repackVariables.OldFilelistFileBckup);
        }


        public static void GetPackedState(string convertedString, RepackProcesses repackVariables, string extractedDir)
        {
            repackVariables.ConvertedOgStringData = convertedString.Split(':');
            repackVariables.OgFilePos = Convert.ToUInt32(repackVariables.ConvertedOgStringData[0], 16) * 2048;
            repackVariables.OgUnCmpSize = Convert.ToUInt32(repackVariables.ConvertedOgStringData[1], 16);
            repackVariables.OgCmpSize = Convert.ToUInt32(repackVariables.ConvertedOgStringData[2], 16);
            repackVariables.OgMainPath = repackVariables.ConvertedOgStringData[3];

            if (repackVariables.OgMainPath.Equals(" "))
            {
                repackVariables.OgNoPathFileCount++;
                repackVariables.OgDirectoryPath = "noPath";
                repackVariables.OgFileName = "FILE_" + repackVariables.OgNoPathFileCount;
                repackVariables.OgFullFilePath = extractedDir + "\\" + repackVariables.OgDirectoryPath + "\\" + repackVariables.OgFileName;
                repackVariables.RepackPathInChunk = " ";
            }
            else
            {
                repackVariables.OgDirectoryPath = Path.GetDirectoryName(repackVariables.OgMainPath);
                repackVariables.OgFileName = Path.GetFileName(repackVariables.OgMainPath);
                repackVariables.OgFullFilePath = extractedDir + "\\" + repackVariables.OgDirectoryPath + "\\" + repackVariables.OgFileName;
                repackVariables.RepackPathInChunk = repackVariables.OgMainPath;
            }

            if (!repackVariables.OgUnCmpSize.Equals(repackVariables.OgCmpSize))
            {
                repackVariables.WasCompressed = true;
                repackVariables.RepackState = "Compressed";
            }
            else
            {
                repackVariables.WasCompressed = false;
                repackVariables.RepackState = "Copied";
            }

            repackVariables.RepackLogMsg = repackVariables.OgDirectoryPath + "\\" + repackVariables.OgFileName;
        }


        public static void RepackTypeAppend(RepackProcesses repackVariables, FileStream newWhiteBin, string fileToAppend, string extractedDirVar)
        {
            var filePositionInDecimal = (uint)newWhiteBin.Length;

            // Check if file position is divisible by 2048
            // and if its not divisible, add in null bytes
            // till next closest divisible number
            if (filePositionInDecimal % 2048 != 0)
            {
                var remainder = filePositionInDecimal % 2048;
                var increaseBytes = 2048 - remainder;
                var newPos = filePositionInDecimal + increaseBytes;
                var padNulls = newPos - filePositionInDecimal;

                newWhiteBin.Seek(filePositionInDecimal, SeekOrigin.Begin);
                for (int pad = 0; pad < padNulls; pad++)
                {
                    newWhiteBin.WriteByte(0);
                }
                filePositionInDecimal = (uint)newWhiteBin.Length;
            }

            var filePositionForChunk = filePositionInDecimal / 2048;
            repackVariables.AsciiFilePos = filePositionForChunk.DecimalToAscii();

            var fileSizeInDecimal = (uint)new FileInfo(fileToAppend).Length;
            repackVariables.AsciiUnCmpSize = fileSizeInDecimal.DecimalToAscii();

            newWhiteBin.Seek(filePositionInDecimal, SeekOrigin.Begin);
            RepackFiles(repackVariables, newWhiteBin, fileToAppend, extractedDirVar);
        }


        public static void RepackTypeInject(RepackProcesses repackVariables, FileStream whiteBin, string fileToInject, string extractedDirVar)
        {
            var filePositionForChunk = repackVariables.OgFilePos / 2048;
            repackVariables.AsciiFilePos = filePositionForChunk.DecimalToAscii();

            var fileSizeInDecimal = (uint)new FileInfo(fileToInject).Length;
            repackVariables.AsciiUnCmpSize = fileSizeInDecimal.DecimalToAscii();

            whiteBin.Seek(repackVariables.OgFilePos, SeekOrigin.Begin);
            RepackFiles(repackVariables, whiteBin, fileToInject, extractedDirVar);
        }


        static void RepackFiles(RepackProcesses repackVariables, FileStream whiteBinStream, string fileToPack, string extractedDirVar)
        {
            switch (repackVariables.WasCompressed)
            {
                case true:
                    repackVariables.TmpCmpDataFile = extractedDirVar + "\\zlib_data";
                    var zlibTmpDataStream = File.Create(repackVariables.TmpCmpDataFile);
                    zlibTmpDataStream.Close();

                    fileToPack.ZlibCompress(repackVariables.TmpCmpDataFile, Ionic.Zlib.CompressionLevel.Level9);

                    using (var cmpData = new FileStream(repackVariables.TmpCmpDataFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        cmpData.Seek(0, SeekOrigin.Begin);
                        cmpData.CopyTo(whiteBinStream);

                        var cmpFileSizeInDecimal = (uint)new FileInfo(repackVariables.TmpCmpDataFile).Length;
                        repackVariables.AsciiCmpSize = cmpFileSizeInDecimal.DecimalToAscii();
                    }

                    File.Delete(repackVariables.TmpCmpDataFile);
                    break;

                case false:
                    repackVariables.AsciiCmpSize = repackVariables.AsciiUnCmpSize;

                    using (var unCmpFile = new FileStream(fileToPack, FileMode.Open, FileAccess.Read))
                    {
                        unCmpFile.Seek(0, SeekOrigin.Begin);
                        unCmpFile.CopyTo(whiteBinStream);
                    }
                    break;
            }
        }


        public static void CleanOldFile(string whiteBinFileVar, uint filePos, uint sizeVar)
        {
            using (var fs = new FileStream(whiteBinFileVar, FileMode.Open, FileAccess.Write))
            {
                fs.Seek(filePos, SeekOrigin.Begin);
                for (int pad = 0; pad < sizeVar; pad++)
                {
                    fs.WriteByte(0);
                }
            }
        }


        public static void AppendProcess(RepackProcesses repackVariables, string extractedDirVar, ref string packedAs)
        {
            using (var appendBin = new FileStream(repackVariables.NewWhiteBinFile, FileMode.Append, FileAccess.Write))
            {
                packedAs = "(Appended)";
                RepackTypeAppend(repackVariables, appendBin, repackVariables.OgFullFilePath, extractedDirVar);
            }
        }


        public static void InjectProcess(RepackProcesses repackVariables, string extractedDirVar, ref string packedAs)
        {
            using (var injectBin = new FileStream(repackVariables.NewWhiteBinFile, FileMode.Open, FileAccess.ReadWrite))
            {
                packedAs = "(Injected)";
                RepackTypeInject(repackVariables, injectBin, repackVariables.OgFullFilePath, extractedDirVar);
            }
        }
    }
}
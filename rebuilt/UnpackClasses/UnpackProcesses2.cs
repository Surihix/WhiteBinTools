using System;
using System.IO;
using WhiteBinTools.SupportClasses;
using WhiteBinTools.FilelistClasses;

namespace WhiteBinTools.UnpackClasses
{
    internal partial class UnpackProcess
    {
        public static void PrepareFilelistVars(FilelistProcesses filelistVariables, string filelistFileVar)
        {
            filelistVariables.MainFilelistFile = filelistFileVar;

            var inFilelistFilePath = Path.GetFullPath(filelistVariables.MainFilelistFile);
            filelistVariables.MainFilelistDirectory = Path.GetDirectoryName(inFilelistFilePath);
            filelistVariables.TmpDcryptFilelistFile = filelistVariables.MainFilelistDirectory + "\\filelist_tmp.bin";
        }


        public static void PrepareBinVars(string whiteBinFileVar, UnpackProcess unpackVariables)
        {
            unpackVariables.WhiteBinName = Path.GetFileName(whiteBinFileVar);

            var inBinFilePath = Path.GetFullPath(whiteBinFileVar);
            unpackVariables.InBinFileDir = Path.GetDirectoryName(inBinFilePath);

            unpackVariables.ExtractDirName = Path.GetFileName(whiteBinFileVar);
            unpackVariables.ExtractDir = unpackVariables.InBinFileDir + "\\_" + unpackVariables.ExtractDirName;
        }


        public static uint GetFilesInChunkCount(FilelistProcesses filelistVariables)
        {
            var filesInChunkCount = (uint)0;
            using (var fileCountReader = new StreamReader(filelistVariables.DefaultChunksExtDir + "/chunk_" + filelistVariables.ChunkFNameCount))
            {
                while (!fileCountReader.EndOfStream)
                {
                    var currentNullChar = fileCountReader.Read();
                    if (currentNullChar == 0)
                    {
                        filesInChunkCount++;
                    }
                }
            }

            return filesInChunkCount;
        }


        public static void PrepareExtraction(string convertedString, FilelistProcesses filelistVariables, string extractDir)
        {
            filelistVariables.ConvertedStringData = convertedString.Split(':');
            filelistVariables.Position = Convert.ToUInt32(filelistVariables.ConvertedStringData[0], 16) * 2048;
            filelistVariables.UnCmpSize = Convert.ToUInt32(filelistVariables.ConvertedStringData[1], 16);
            filelistVariables.CmpSize = Convert.ToUInt32(filelistVariables.ConvertedStringData[2], 16);
            filelistVariables.MainPath = filelistVariables.ConvertedStringData[3].Replace("/", "\\");
            filelistVariables.IsCompressed = false;

            if (filelistVariables.MainPath.Equals(" "))
            {
                filelistVariables.NoPathFileCount++;
                filelistVariables.DirectoryPath = "noPath";
                filelistVariables.FileName = "FILE_" + filelistVariables.NoPathFileCount;
                filelistVariables.FullFilePath = extractDir + "\\" + filelistVariables.DirectoryPath + "\\" + filelistVariables.FileName;
                filelistVariables.MainPath = filelistVariables.DirectoryPath + "\\" + filelistVariables.FileName;
            }
            else
            {
                filelistVariables.DirectoryPath = Path.GetDirectoryName(filelistVariables.MainPath);
                filelistVariables.FileName = Path.GetFileName(filelistVariables.MainPath);
                filelistVariables.FullFilePath = extractDir + "\\" + filelistVariables.DirectoryPath + "\\" + filelistVariables.FileName;
            }

            if (!filelistVariables.UnCmpSize.Equals(filelistVariables.CmpSize))
            {
                filelistVariables.IsCompressed = true;
            }
            else
            {
                filelistVariables.IsCompressed = false;
            }
        }


        public static void UnpackFile(FilelistProcesses filelistVariables, FileStream whiteBin, UnpackProcess unpackVariables)
        {
            switch (filelistVariables.IsCompressed)
            {
                case true:
                    using (var cmpData = new MemoryStream())
                    {
                        whiteBin.ExtendedCopyTo(cmpData, filelistVariables.Position, filelistVariables.CmpSize);

                        using (var outFile = new FileStream(filelistVariables.FullFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                        {
                            cmpData.Seek(0, SeekOrigin.Begin);
                            cmpData.ZlibDecompress(outFile);
                            unpackVariables.UnpackedState = "Decompressed";
                        }
                    }
                    break;

                case false:
                    using (var outFile = new FileStream(filelistVariables.FullFilePath, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        outFile.Seek(0, SeekOrigin.Begin);
                        whiteBin.ExtendedCopyTo(outFile, filelistVariables.Position, filelistVariables.UnCmpSize);
                        unpackVariables.UnpackedState = "Copied";
                    }
                    break;
            }
        }
    }
}
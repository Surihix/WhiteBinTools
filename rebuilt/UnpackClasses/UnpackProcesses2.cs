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


        public static void PrepareExtraction(string convertedString, UnpackProcess unpackVariables, string extractDir)
        {
            unpackVariables.ConvertedStringData = convertedString.Split(':');
            unpackVariables.Position = Convert.ToUInt32(unpackVariables.ConvertedStringData[0], 16) * 2048;
            unpackVariables.UnCmpSize = Convert.ToUInt32(unpackVariables.ConvertedStringData[1], 16);
            unpackVariables.CmpSize = Convert.ToUInt32(unpackVariables.ConvertedStringData[2], 16);
            unpackVariables.MainPath = unpackVariables.ConvertedStringData[3].Replace("/", "\\");

            unpackVariables.DirectoryPath = Path.GetDirectoryName(unpackVariables.MainPath);
            unpackVariables.FileName = Path.GetFileName(unpackVariables.MainPath);
            unpackVariables.FullFilePath = extractDir + "\\" + unpackVariables.DirectoryPath + "\\" + unpackVariables.FileName;
            unpackVariables.CompressedState = false;

            if (!unpackVariables.UnCmpSize.Equals(unpackVariables.CmpSize))
            {
                unpackVariables.CompressedState = true;
                unpackVariables.UnpackedState = "Decompressed";
            }
            else
            {
                unpackVariables.CompressedState = false;
                unpackVariables.UnpackedState = "Copied";
            }
        }


        public static void UnpackFile(UnpackProcess unpackVariables, FileStream whiteBin)
        {
            switch (unpackVariables.CompressedState)
            {
                case true:
                    using (var cmpData = new MemoryStream())
                    {
                        whiteBin.ExtendedCopyTo(cmpData, unpackVariables.Position, unpackVariables.CmpSize);

                        using (var outFile = new FileStream(unpackVariables.FullFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                        {
                            cmpData.Seek(0, SeekOrigin.Begin);
                            ZlibLibrary.ZlibDecompress(cmpData, outFile);
                        }
                    }
                    break;

                case false:
                    using (var outFile = new FileStream(unpackVariables.FullFilePath, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        outFile.Seek(0, SeekOrigin.Begin);
                        whiteBin.ExtendedCopyTo(outFile, unpackVariables.Position, unpackVariables.UnCmpSize);
                    }
                    break;
            }
        }
    }
}
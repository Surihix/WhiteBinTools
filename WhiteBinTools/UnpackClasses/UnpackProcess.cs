using System;
using System.IO;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.SupportClasses;

namespace WhiteBinTools.UnpackClasses
{
    internal class UnpackProcess
    {
        public static void PrepareBinVars(string whiteBinFile, UnpackVariables unpackVariables)
        {
            unpackVariables.WhiteBinName = Path.GetFileName(whiteBinFile);

            var inBinFilePath = Path.GetFullPath(whiteBinFile);
            unpackVariables.InBinFileDir = Path.GetDirectoryName(inBinFilePath);

            unpackVariables.ExtractDirName = Path.GetFileName(whiteBinFile);
            unpackVariables.ExtractDir = Path.Combine(unpackVariables.InBinFileDir, "_" + unpackVariables.ExtractDirName);
        }


        public static void PrepareExtraction(string convertedString, FilelistVariables filelistVariables, string extractDir)
        {
            filelistVariables.ConvertedStringData = convertedString.Split(':');
            filelistVariables.Position = Convert.ToUInt32(filelistVariables.ConvertedStringData[0], 16) * 2048;
            filelistVariables.UnCmpSize = Convert.ToUInt32(filelistVariables.ConvertedStringData[1], 16);
            filelistVariables.CmpSize = Convert.ToUInt32(filelistVariables.ConvertedStringData[2], 16);
            filelistVariables.MainPath = filelistVariables.ConvertedStringData[3].Replace("/", Core.PathSeparatorChar);
            filelistVariables.IsCompressed = false;

            if (filelistVariables.MainPath == " ")
            {
                filelistVariables.NoPathFileCount++;
                filelistVariables.DirectoryPath = "noPath";
                filelistVariables.FileName = "FILE_" + filelistVariables.NoPathFileCount;
                filelistVariables.FullFilePath = Path.Combine(extractDir, filelistVariables.DirectoryPath, filelistVariables.FileName);
                filelistVariables.MainPath = Path.Combine(filelistVariables.DirectoryPath, filelistVariables.FileName);
            }
            else
            {
                filelistVariables.DirectoryPath = Path.GetDirectoryName(filelistVariables.MainPath);
                filelistVariables.FileName = Path.GetFileName(filelistVariables.MainPath);
                filelistVariables.FullFilePath = Path.Combine(extractDir, filelistVariables.DirectoryPath, filelistVariables.FileName);
            }

            if (filelistVariables.UnCmpSize != filelistVariables.CmpSize)
            {
                filelistVariables.IsCompressed = true;
            }
            else
            {
                filelistVariables.IsCompressed = false;
            }
        }


        public static void UnpackFile(FilelistVariables filelistVariables, FileStream whiteBinStream, UnpackVariables unpackVariables)
        {
            switch (filelistVariables.IsCompressed)
            {
                case true:
                    using (var cmpData = new MemoryStream())
                    {
                        whiteBinStream.Seek(filelistVariables.Position, SeekOrigin.Begin);
                        whiteBinStream.CopyStreamTo(cmpData, filelistVariables.CmpSize, false);

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
                        whiteBinStream.Seek(filelistVariables.Position, SeekOrigin.Begin);
                        whiteBinStream.CopyStreamTo(outFile, filelistVariables.UnCmpSize, false);
                        unpackVariables.UnpackedState = "Copied";
                    }
                    break;
            }
        }
    }
}
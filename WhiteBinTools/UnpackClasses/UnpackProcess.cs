using System;
using System.IO;
using WhiteBinTools.SupportClasses;
using WhiteBinTools.FilelistClasses;

namespace WhiteBinTools.UnpackClasses
{
    internal class UnpackProcess
    {
        public static void PrepareBinVars(string whiteBinFileVar, UnpackVariables unpackVariables)
        {
            unpackVariables.WhiteBinName = Path.GetFileName(whiteBinFileVar);

            var inBinFilePath = Path.GetFullPath(whiteBinFileVar);
            unpackVariables.InBinFileDir = Path.GetDirectoryName(inBinFilePath);

            unpackVariables.ExtractDirName = Path.GetFileName(whiteBinFileVar);
            unpackVariables.ExtractDir = Path.Combine(unpackVariables.InBinFileDir, "_" + unpackVariables.ExtractDirName);
        }


        public static void PrepareExtraction(string convertedString, FilelistVariables filelistVariables, string extractDir)
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


        public static void UnpackFile(FilelistVariables filelistVariables, FileStream whiteBin, UnpackVariables unpackVariables)
        {
            switch (filelistVariables.IsCompressed)
            {
                case true:
                    using (var cmpData = new MemoryStream())
                    {
                        whiteBin.ExCopyTo(cmpData, filelistVariables.Position, filelistVariables.CmpSize);

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
                        whiteBin.ExCopyTo(outFile, filelistVariables.Position, filelistVariables.UnCmpSize);
                        unpackVariables.UnpackedState = "Copied";
                    }
                    break;
            }
        }
    }
}
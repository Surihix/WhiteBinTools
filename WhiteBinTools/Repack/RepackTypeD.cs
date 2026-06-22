using System;
using System.IO;
using WhiteBinTools.Filelist;
using WhiteBinTools.Support;
using WhiteBinTools.Support.Structures;
using static WhiteBinTools.Support.Enumerators;

namespace WhiteBinTools.Repack
{
    internal class RepackTypeD
    {
        public static void RepackFilelist(GameCode gameCode, string extractedFilelistDir, StreamWriter logWriter)
        {
            SharedFunctions.CheckDirExists(extractedFilelistDir, logWriter, "Error: Unpacked filelist directory specified in the argument is missing");

            var infoFile = Path.Combine(extractedFilelistDir, "#info.txt");
            SharedFunctions.CheckFileExists(infoFile, logWriter, "Error: Unable to locate '#info.txt' file in the unpacked filelist folder");

            logWriter.LogMessage("\nParsing '#info.txt' file....");

            var infoFileLines = File.ReadAllLines(infoFile);

            var filelistHeader = new FilelistHeader();
            var filelistCryptHeader = new FilelistCryptHeader();

            // Get all the necessary information
            // from the #info.txt file
            if (gameCode == GameCode.ff132)
            {
                if (infoFileLines.Length < 3)
                {
                    SharedFunctions.ErrorExit("Not enough data present in the #info.txt file");
                }

                var index = 0;
                CheckPropertyInInfoFile(infoFileLines[index], "encrypted: ", ValueTypes.Boolean);
                filelistCryptHeader.HasCryptHeader = bool.Parse(infoFileLines[index].Split(' ')[1]);
                index++;

                if (filelistCryptHeader.HasCryptHeader)
                {
                    CheckPropertyInInfoFile(infoFileLines[index], "seedA: ", ValueTypes.Ulong);
                    var seedA = ulong.Parse(infoFileLines[index].Split(' ')[1]);
                    index++;

                    CheckPropertyInInfoFile(infoFileLines[index], "seedB: ", ValueTypes.Ulong);
                    var seedB = ulong.Parse(infoFileLines[index].Split(' ')[1]);
                    index++;

                    filelistCryptHeader.MD5Hash = new byte[16];
                    Array.Copy(BitConverter.GetBytes(seedA), 0, filelistCryptHeader.MD5Hash, 0, 8);
                    Array.Copy(BitConverter.GetBytes(seedB), 0, filelistCryptHeader.MD5Hash, 8, 8);

                    CheckPropertyInInfoFile(infoFileLines[index], "encryptionTag(DO_NOT_CHANGE): ", ValueTypes.Uint);
                    filelistCryptHeader.EncryptionTag = uint.Parse(infoFileLines[index].Split(' ')[1]);
                    index++;
                }

                CheckPropertyInInfoFile(infoFileLines[index], "fileCount: ", ValueTypes.Uint);
                filelistHeader.FileCount = uint.Parse(infoFileLines[index].Split(' ')[1]);
                index++;

                CheckPropertyInInfoFile(infoFileLines[index], "chunkCount: ", ValueTypes.Int);
                filelistHeader.ChunkCount = int.Parse(infoFileLines[index].Split(' ')[1]);
            }
            else
            {
                if (infoFileLines.Length < 2)
                {
                    SharedFunctions.ErrorExit("Error: Not enough data present in the #info.txt file");
                }

                CheckPropertyInInfoFile(infoFileLines[0], "fileCount: ", ValueTypes.Uint);
                filelistHeader.FileCount = uint.Parse(infoFileLines[0].Split(' ')[1]);

                CheckPropertyInInfoFile(infoFileLines[1], "chunkCount: ", ValueTypes.Uint);
                filelistHeader.ChunkCount = int.Parse(infoFileLines[1].Split(' ')[1]);
            }

            logWriter.LogMessage("TotalChunks: " + filelistHeader.ChunkCount);
            logWriter.LogMessage("No of files: " + filelistHeader.FileCount + "\n");

            var newFilelistFileName = Path.GetFileName(extractedFilelistDir);

            if (newFilelistFileName.StartsWith("_"))
            {
                newFilelistFileName = newFilelistFileName.Remove(0, 1);
            }

            var newFilelistFile = Path.Combine(Path.GetDirectoryName(extractedFilelistDir), newFilelistFileName);

            if (Core.ShouldBckup)
            {
                logWriter.LogMessage("\nBacking up filelist bin file....\n");
                SharedFunctions.IfFileExistsDel($"{newFilelistFile}.bak");
                File.Move(newFilelistFile, $"{newFilelistFile}.bak");
            }

            FilelistEntryV1[] newEntryV1Table = Array.Empty<FilelistEntryV1>();
            FilelistEntryV2[] newEntryV2Table = Array.Empty<FilelistEntryV2>();

            if (gameCode == GameCode.ff131 || gameCode == GameCode.dirge)
            {
                newEntryV1Table = new FilelistEntryV1[filelistHeader.FileCount];
            }
            else
            {
                newEntryV2Table = new FilelistEntryV2[filelistHeader.FileCount];
            }

            var fileInfoStringPackTable = new FileInfoStringPack[filelistHeader.FileCount];

            logWriter.LogMessage("Parsing filepaths....");
            var fileIndex = 0;

            for (int i = 0; i < filelistHeader.ChunkCount; i++)
            {
                var currentChunkFile = Path.Combine(extractedFilelistDir, $"Chunk_{i}.txt");
                SharedFunctions.CheckFileExists(currentChunkFile, logWriter, $"Error: Unable to locate 'Chunk_{i}.txt' file in the unpacked filelist folder");

                var currentChunkData = File.ReadAllLines(currentChunkFile);

                for (int j = 0; j < currentChunkData.Length; j++)
                {
                    var currentEntryData = currentChunkData[j].Split('|');

                    string whiteFileInfoString;
                    ushort chunkID;

                    if (gameCode == GameCode.ff131 || gameCode == GameCode.dirge)
                    {
                        if (currentEntryData.Length < 2)
                        {
                            SharedFunctions.ErrorExit($"Error: Not enough data specified for the entry at line_{j} in 'Chunk_{i}.txt' file. check if the entry contains valid data for the game code specified in the argument.");
                        }

                        var filelistEntryV1 = new FilelistEntryV1();
                        
                        CheckChunkEntryData(currentEntryData[0], ValueTypes.Uint, i, j);

                        filelistEntryV1.FileCode = uint.Parse(currentEntryData[0]);
                        whiteFileInfoString = currentEntryData[1] + "\0";
                        filelistEntryV1.ChunkID = (ushort)i;

                        newEntryV1Table[fileIndex] = filelistEntryV1;

                        chunkID = filelistEntryV1.ChunkID;
                    }
                    else
                    {
                        if (currentEntryData.Length < 3)
                        {
                            SharedFunctions.ErrorExit($"Error: Not enough data specified for the entry at line_{j} in 'Chunk_{i}.txt' file. check if the entry contains valid data for the game code specified in the argument.");
                        }

                        var filelistEntryV2 = new FilelistEntryV2();

                        CheckChunkEntryData(currentEntryData[0], ValueTypes.Uint, i, j);
                        CheckChunkEntryData(currentEntryData[1], ValueTypes.Byte, i, j);

                        filelistEntryV2.FileCode = uint.Parse(currentEntryData[0]);
                        filelistEntryV2.FileTypeID = byte.Parse(currentEntryData[1]);
                        whiteFileInfoString = currentEntryData[2] + "\0";
                        filelistEntryV2.ChunkID = (ushort)i;

                        newEntryV2Table[fileIndex] = filelistEntryV2;

                        chunkID = filelistEntryV2.ChunkID;
                    }

                    fileInfoStringPackTable[fileIndex] = new FileInfoStringPack() { ChunkID = chunkID, FileInfoString = whiteFileInfoString };
                    fileIndex++;
                }
            }

            logWriter.LogMessage("\nBuilding filelist....");
            FilelistBuilder.BuildFilelist(filelistCryptHeader, filelistHeader, gameCode, newEntryV1Table, newEntryV2Table, fileInfoStringPackTable, newFilelistFile);

            if (filelistCryptHeader.HasCryptHeader)
            {
                FilelistCrypto.EncryptProcess(newFilelistFile, logWriter);
            }

            logWriter.LogMessage($"\n\nFinished repacking filelist data to \"{newFilelistFileName}\"");
        }

        private static void CheckPropertyInInfoFile(string propertyDataRead, string expectedPropertyName, ValueTypes valueType)
        {
            if (!propertyDataRead.StartsWith(expectedPropertyName))
            {
                SharedFunctions.ErrorExit($"Error: The '{expectedPropertyName}' property in '#info.txt' file is invalid. Please check if the property is specified correctly as well as check if you have set the correct game code.");
            }

            var isValidVal = true;

            switch (valueType)
            {
                case ValueTypes.Boolean:
                    isValidVal = bool.TryParse(propertyDataRead.Split(' ')[1], out _);
                    break;

                case ValueTypes.Int:
                    isValidVal = int.TryParse(propertyDataRead.Split(' ')[1], out _);
                    break;

                case ValueTypes.Uint:
                    isValidVal = uint.TryParse(propertyDataRead.Split(' ')[1], out _);
                    break;

                case ValueTypes.Ulong:
                    isValidVal = ulong.TryParse(propertyDataRead.Split(' ')[1], out _);
                    break;
            }

            if (!isValidVal)
            {
                SharedFunctions.ErrorExit($"Error: Invalid value specified for '{expectedPropertyName}' property in the #info.txt file");
            }
        }

        private static void CheckChunkEntryData(string currentLine, ValueTypes entryValueType, int chunkId, int lineNo)
        {
            var isValidVal = true;

            switch (entryValueType)
            {
                case ValueTypes.Byte:
                    isValidVal = byte.TryParse(currentLine, out _);
                    break;

                case ValueTypes.Uint:
                    isValidVal = uint.TryParse(currentLine, out _);
                    break;
            }

            if (!isValidVal)
            {
                SharedFunctions.ErrorExit($"Invalid data found when parsing line_{lineNo} in 'Chunk_{chunkId}'.txt file. Please check if the line is specified correctly as well as check if you have selected the correct game in this tool");
            }
        }
    }
}
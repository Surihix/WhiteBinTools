using System.Collections.Generic;
using System.IO;
using System.Text;
using WhiteBinTools.Filelist;
using WhiteBinTools.Support;
using static WhiteBinTools.Support.ProgramEnums;

namespace WhiteBinTools.Repack
{
    internal class RepackTypeD
    {
        public static void RepackFilelist(GameCodes gameCode, string extractedFilelistDir, StreamWriter logWriter)
        {
            IOhelpers.CheckDirExists(extractedFilelistDir, logWriter, "Error: Unpacked filelist directory specified in the argument is missing");

            var infoFile = Path.Combine(extractedFilelistDir, "#info.txt");
            IOhelpers.CheckFileExists(infoFile, logWriter, "Error: Unable to locate '#info.txt' file in the unpacked filelist folder");

            var infoFileLines = File.ReadAllLines(infoFile);

            var filelistVariables = new FilelistVariables();

            // Get all the necessary information
            // from the #info.txt file
            if (gameCode == GameCodes.ff131)
            {
                if (infoFileLines.Length < 2)
                {
                    IOhelpers.ErrorExit("Error: Not enough data present in the #info.txt file");
                }

                CheckPropertyInInfoFile(infoFileLines[0], "fileCount: ", ValueTypes.Uint);
                filelistVariables.TotalFiles = uint.Parse(infoFileLines[0].Split(' ')[1]);

                CheckPropertyInInfoFile(infoFileLines[1], "chunkCount: ", ValueTypes.Uint);
                filelistVariables.TotalChunks = uint.Parse(infoFileLines[1].Split(' ')[1]);
            }
            else if (gameCode == GameCodes.ff132)
            {
                if (infoFileLines.Length < 3)
                {
                    IOhelpers.ErrorExit("Not enough data present in the #info.txt file");
                }

                CheckPropertyInInfoFile(infoFileLines[0], "encrypted: ", ValueTypes.Boolean);
                filelistVariables.IsEncrypted = bool.Parse(infoFileLines[0].Split(' ')[1]);

                if (filelistVariables.IsEncrypted)
                {
                    CheckPropertyInInfoFile(infoFileLines[1], "seedA: ", ValueTypes.Ulong);
                    filelistVariables.SeedA = ulong.Parse(infoFileLines[1].Split(' ')[1]);

                    CheckPropertyInInfoFile(infoFileLines[2], "seedB: ", ValueTypes.Ulong);
                    filelistVariables.SeedB = ulong.Parse(infoFileLines[2].Split(' ')[1]);

                    CheckPropertyInInfoFile(infoFileLines[3], "encryptionTag(DO_NOT_CHANGE): ", ValueTypes.Uint);
                    filelistVariables.EncTag = uint.Parse(infoFileLines[3].Split(' ')[1]);

                    CheckPropertyInInfoFile(infoFileLines[4], "fileCount: ", ValueTypes.Uint);
                    filelistVariables.TotalFiles = uint.Parse(infoFileLines[4].Split(' ')[1]);

                    CheckPropertyInInfoFile(infoFileLines[5], "chunkCount: ", ValueTypes.Uint);
                    filelistVariables.TotalChunks = uint.Parse(infoFileLines[5].Split(' ')[1]);

                    using (var encHeaderStream = new MemoryStream())
                    {
                        using (var encHeaderWriter = new BinaryWriter(encHeaderStream))
                        {
                            encHeaderStream.Seek(0, SeekOrigin.Begin);

                            encHeaderWriter.WriteBytesUInt64(filelistVariables.SeedA, false);
                            encHeaderWriter.WriteBytesUInt64(filelistVariables.SeedB, false);
                            encHeaderWriter.WriteBytesUInt32(0, false);
                            encHeaderWriter.WriteBytesUInt32(filelistVariables.EncTag, false);
                            encHeaderWriter.WriteBytesUInt64(0, false);

                            encHeaderStream.Seek(0, SeekOrigin.Begin);
                            filelistVariables.EncryptedHeaderData = new byte[32];
                            filelistVariables.EncryptedHeaderData = encHeaderStream.ToArray();
                        }
                    }
                }
                else
                {
                    CheckPropertyInInfoFile(infoFileLines[1], "fileCount: ", ValueTypes.Uint);
                    filelistVariables.TotalFiles = uint.Parse(infoFileLines[1].Split(' ')[1]);

                    CheckPropertyInInfoFile(infoFileLines[2], "chunkCount: ", ValueTypes.Uint);
                    filelistVariables.TotalChunks = uint.Parse(infoFileLines[2].Split(' ')[1]);
                }
            }

            logWriter.LogMessage("TotalChunks: " + filelistVariables.TotalChunks);
            logWriter.LogMessage("No of files: " + filelistVariables.TotalFiles + "\n");

            // Begin building the filelist
            logWriter.LogMessage("\n\nBuilding filelist....");

            var repackVariables = new RepackVariables();
            repackVariables.NewFilelistFile = Path.Combine(Path.GetDirectoryName(extractedFilelistDir), Path.GetFileName(extractedFilelistDir).Remove(0, 1));

            if (Core.ShouldBckup)
            {
                if (File.Exists(repackVariables.NewFilelistFile))
                {
                    IOhelpers.IfFileExistsDel(repackVariables.NewFilelistFile + ".bak");

                    File.Copy(repackVariables.NewFilelistFile, repackVariables.NewFilelistFile + ".bak");
                }
            }

            IOhelpers.IfFileExistsDel(repackVariables.NewFilelistFile);

            // Build an empty dictionary
            // for the chunks 
            var newChunksDict = new Dictionary<int, List<byte>>();
            RepackProcesses.CreateEmptyNewChunksDict(filelistVariables, newChunksDict);

            // Build a number list containing all
            // the odd number chunks if the code
            // is set to 2
            var oddChunkNumValues = new List<int>();
            if (gameCode == GameCodes.ff132 && filelistVariables.TotalChunks > 1)
            {
                var nextChunkNo = 1;
                for (int i = 0; i < filelistVariables.TotalChunks; i++)
                {
                    if (i == nextChunkNo)
                    {
                        oddChunkNumValues.Add(i);
                        nextChunkNo += 2;
                    }
                }
            }

            using (var entriesStream = new MemoryStream())
            {
                using (var entriesWriter = new BinaryWriter(entriesStream))
                {

                    // Process each path in chunks
                    var currentChunkFile = string.Empty;
                    var currentChunkData = new string[] { };
                    var currentEntryData = new string[] { };
                    var oddChunkCounter = 0;
                    long entriesWriterPos = 0;

                    for (int c = 0; c < filelistVariables.TotalChunks; c++)
                    {
                        filelistVariables.LastChunkNumber = c;
                        currentChunkFile = Path.Combine(extractedFilelistDir, $"Chunk_{c}.txt");

                        currentChunkData = File.ReadAllLines(currentChunkFile);

                        for (int l = 0; l < currentChunkData.Length; l++)
                        {
                            currentEntryData = currentChunkData[l].Split('|');

                            if (gameCode == GameCodes.ff131)
                            {
                                if (currentEntryData.Length < 2)
                                {
                                    IOhelpers.ErrorExit($"Error: Not enough data specified for the entry at line_{l} in 'Chunk_{c}.txt' file. check if the entry contains valid data for the game code specified in the argument.");
                                }

                                CheckChunkEntryData(currentEntryData[0], ValueTypes.Uint, c, l);
                                filelistVariables.FileCode = uint.Parse(currentEntryData[0]);

                                // Write filecode
                                entriesWriter.BaseStream.Position = entriesWriterPos;
                                entriesWriter.WriteBytesUInt32(filelistVariables.FileCode, false);

                                // Write chunk number
                                entriesWriter.BaseStream.Position = entriesWriterPos + 4;
                                entriesWriter.WriteBytesUInt16((ushort)c, false);

                                // Write zero as path number
                                entriesWriter.BaseStream.Position = entriesWriterPos + 6;
                                entriesWriter.WriteBytesUInt16(0, false);

                                filelistVariables.PathString = currentEntryData[1];
                            }
                            else if (gameCode == GameCodes.ff132)
                            {
                                if (currentEntryData.Length < 3)
                                {
                                    IOhelpers.ErrorExit($"Error: Not enough data specified for the entry at line_{l} in 'Chunk_{c}.txt' file. check if the entry contains valid data for the game code specified in the argument.");
                                }

                                CheckChunkEntryData(currentEntryData[0], ValueTypes.Uint, c, l);
                                filelistVariables.FileCode = uint.Parse(currentEntryData[0]);

                                CheckChunkEntryData(currentEntryData[1], ValueTypes.Byte, c, l);
                                filelistVariables.FileTypeID = byte.Parse(currentEntryData[1]);

                                entriesWriter.BaseStream.Position = entriesWriterPos;
                                entriesWriter.WriteBytesUInt32(filelistVariables.FileCode, false);

                                entriesWriter.BaseStream.Position = entriesWriterPos + 4;

                                if (oddChunkNumValues.Contains(c))
                                {
                                    // Write the 32768 position value
                                    // to indicate that the chunk
                                    // number is odd
                                    oddChunkCounter = oddChunkNumValues.IndexOf(c);
                                    entriesWriter.WriteBytesUInt16(32768, false);
                                }
                                else
                                {
                                    // Write zero as path number
                                    entriesWriter.WriteBytesUInt16(0, false);
                                }

                                // Write chunk number
                                entriesWriter.BaseStream.Position = entriesWriterPos + 6;
                                entriesWriter.Write((byte)oddChunkCounter);

                                // Write FileTypeID
                                entriesWriter.BaseStream.Position = entriesWriterPos + 7;
                                entriesWriter.Write(filelistVariables.FileTypeID);

                                filelistVariables.PathString = currentEntryData[2];
                            }

                            // Add path to dictionary
                            newChunksDict[c].AddRange(Encoding.UTF8.GetBytes(filelistVariables.PathString + "\0"));

                            entriesWriterPos += 8;
                        }

                        oddChunkCounter++;
                    }

                    filelistVariables.EntriesData = new byte[entriesStream.Length];
                    entriesStream.Seek(0, SeekOrigin.Begin);
                    entriesStream.Read(filelistVariables.EntriesData, 0, filelistVariables.EntriesData.Length);
                }
            }

            RepackFilelistData.BuildFilelist(filelistVariables, newChunksDict, repackVariables, gameCode);

            if (filelistVariables.IsEncrypted)
            {
                FilelistCrypto.EncryptProcess(repackVariables, logWriter);
            }

            logWriter.LogMessage($"\n\nFinished repacking filelist data to \"{Path.GetFileName(repackVariables.NewFilelistFile)}\"");
        }


        private enum ValueTypes
        {
            Boolean,
            Byte,
            Uint,
            Ulong
        }


        private static void CheckPropertyInInfoFile(string propertyDataRead, string expectedPropertyName, ValueTypes valueType)
        {
            if (!propertyDataRead.StartsWith(expectedPropertyName))
            {
                IOhelpers.ErrorExit($"Error: The '{expectedPropertyName}' property in '#info.txt' file is invalid. Please check if the property is specified correctly as well as check if you have set the correct game code.");
            }

            var isValidVal = true;

            switch (valueType)
            {
                case ValueTypes.Boolean:
                    isValidVal = bool.TryParse(propertyDataRead.Split(' ')[1], out _);
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
                IOhelpers.ErrorExit($"Error: Invalid value specified for '{expectedPropertyName}' property in the #info.txt file");
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
                IOhelpers.ErrorExit($"Invalid data found when parsing line_{lineNo} in 'Chunk_{chunkId}'.txt file. Please check if the line is specified correctly as well as check if you have selected the correct game in this tool");
            }
        }
    }
}
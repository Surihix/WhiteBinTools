using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WhiteBinTools.Filelist;
using WhiteBinTools.Support;
using static WhiteBinTools.Support.ProgramEnums;

namespace WhiteBinTools.Repack
{
    internal class RepackTypeE
    {
        public static void RepackJsonFilelist(GameCodes gameCode, string jsonFile, StreamWriter logWriter)
        {
            IOhelpers.CheckFileExists(jsonFile, logWriter, "Error: JSON file specified in the argument is missing");

            var filelistVariables = new FilelistVariables();

            using (var jsonReader = new StreamReader(jsonFile))
            {
                _ = jsonReader.ReadLine();

                if (gameCode == GameCodes.ff132)
                {
                    // Determine encryption status
                    filelistVariables.IsEncrypted = bool.Parse(CheckGetMainProperty(jsonReader, "\"encrypted\"",  ValueTypes.Boolean));

                    if (filelistVariables.IsEncrypted)
                    {
                        filelistVariables.SeedA = ulong.Parse(CheckGetMainProperty(jsonReader, "\"seedA\"", ValueTypes.Ulong));
                        filelistVariables.SeedB = ulong.Parse(CheckGetMainProperty(jsonReader, "\"seedB\"", ValueTypes.Ulong));
                        filelistVariables.EncTag = uint.Parse(CheckGetMainProperty(jsonReader, "\"encryptionTag(DO_NOT_CHANGE)\"", ValueTypes.Uint));

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
                }

                filelistVariables.TotalFiles = uint.Parse(CheckGetMainProperty(jsonReader, "\"fileCount\"", ValueTypes.Uint));
                filelistVariables.TotalChunks = uint.Parse(CheckGetMainProperty(jsonReader, "\"chunkCount\"", ValueTypes.Uint));
                logWriter.LogMessage("TotalChunks: " + filelistVariables.TotalChunks);
                logWriter.LogMessage("No of files: " + filelistVariables.TotalFiles + "\n");

                if (!jsonReader.ReadLine().TrimStart(' ').StartsWith("\"data\": {"))
                {
                    IOhelpers.ErrorExit("Error: data property specified in the json file is invalid");
                }

                // Begin building the filelist
                logWriter.LogMessage("\n\nBuilding filelist....\n");

                var repackVariables = new RepackVariables();
                repackVariables.NewFilelistFile = Path.Combine(Path.GetDirectoryName(jsonFile), Path.GetFileNameWithoutExtension(jsonFile));

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
                        var currentChunk = string.Empty;
                        var currentJsonLine = string.Empty;
                        var currentEntryPropertyValue = string.Empty;
                        var oddChunkCounter = 0;
                        var splitChara = new string[] { "\": " };
                        long entriesWriterPos = 0;

                        for (int c = 0; c < filelistVariables.TotalChunks; c++)
                        {
                            filelistVariables.LastChunkNumber = c;
                            currentChunk = $"Chunk_{c}";

                            if (!jsonReader.ReadLine().TrimStart(' ').StartsWith($"\"{currentChunk}\": ["))
                            {
                                IOhelpers.ErrorExit($"Error: {currentChunk} property string specified in the json file is missing or invalid");
                            }

                            while (true)
                            {
                                currentJsonLine = jsonReader.ReadLine().TrimStart(' ').TrimEnd(' ');

                                // Determine how to end
                                // processing the
                                // current chunk
                                if (currentJsonLine == "}")
                                {
                                    _ = jsonReader.ReadLine();
                                    break;
                                }
                                else if (currentJsonLine == "},")
                                {
                                    continue;
                                }

                                // Process entry
                                if (currentJsonLine == "{")
                                {
                                    currentJsonLine = jsonReader.ReadLine().TrimStart(' ').TrimEnd(' ');
                                    currentEntryPropertyValue = CheckGetChunkEntryProperty(currentJsonLine, "\"fileCode\"", c, ValueTypes.Uint);
                                    filelistVariables.FileCode = uint.Parse(currentEntryPropertyValue);

                                    // Write filecode
                                    entriesWriter.BaseStream.Position = entriesWriterPos;
                                    entriesWriter.WriteBytesUInt32(filelistVariables.FileCode, false);

                                    if (gameCode == GameCodes.ff131)
                                    {
                                        // Write chunk number
                                        entriesWriter.BaseStream.Position = entriesWriterPos + 4;
                                        entriesWriter.WriteBytesUInt16((ushort)c, false);

                                        // Write zero as path number
                                        entriesWriter.BaseStream.Position = entriesWriterPos + 6;
                                        entriesWriter.WriteBytesUInt16(0, false);
                                    }
                                    else if (gameCode == GameCodes.ff132)
                                    {
                                        currentJsonLine = jsonReader.ReadLine().TrimStart(' ').TrimEnd(' ');
                                        currentEntryPropertyValue = CheckGetChunkEntryProperty(currentJsonLine, "\"fileTypeID\"", c, ValueTypes.Byte);
                                        filelistVariables.FileTypeID = byte.Parse(currentEntryPropertyValue);

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
                                    }

                                    // Add path to dictionary
                                    currentJsonLine = jsonReader.ReadLine().TrimStart(' ').TrimEnd(' ');
                                    var filePathProperty = currentJsonLine.Split(splitChara, StringSplitOptions.None);

                                    if (!filePathProperty[0].StartsWith("\"filePath"))
                                    {
                                        IOhelpers.ErrorExit($"Error: Missing filePath property at expected position. occured when parsing Chunk_{c}.");
                                    }

                                    filelistVariables.PathString = filePathProperty[1].Replace("\"", "");

                                    newChunksDict[c].AddRange(Encoding.UTF8.GetBytes(filelistVariables.PathString + "\0"));

                                    entriesWriterPos += 8;
                                }
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

                logWriter.LogMessage($"\n\nFinished repacking JSON data to \"{Path.GetFileName(repackVariables.NewFilelistFile)}\"");
            }
        }


        private enum ValueTypes
        {
            Boolean,
            Byte,
            Uint,
            Ulong
        }


        private static string CheckGetMainProperty(StreamReader jsonReader, string expectedPropertyName, ValueTypes valueType)
        {
            var jsonPropertyVal = string.Empty;

            var propertyDataRead = jsonReader.ReadLine().Split(':');

            if (!propertyDataRead[0].TrimStart(' ').StartsWith(expectedPropertyName))
            {
                IOhelpers.ErrorExit($"Error: Missing {expectedPropertyName} property at expected position");
            }

            var isValidVal = true;

            switch (valueType)
            {
                case ValueTypes.Boolean:
                    if (bool.TryParse(propertyDataRead[1].TrimEnd(','), out bool boolVal))
                    {
                        isValidVal = true;
                        jsonPropertyVal = boolVal.ToString();
                    }
                    break;

                case ValueTypes.Uint:
                    if (uint.TryParse(propertyDataRead[1].TrimEnd(','), out uint uintVal))
                    {
                        isValidVal = true;
                        jsonPropertyVal = uintVal.ToString();
                    }
                    break;

                case ValueTypes.Ulong:
                    if (ulong.TryParse(propertyDataRead[1].TrimEnd(','), out ulong ulongVal))
                    {
                        isValidVal = true;
                        jsonPropertyVal = ulongVal.ToString();
                    }
                    break;
            }

            if (!isValidVal)
            {
                IOhelpers.ErrorExit($"Error: Invalid value specified for '{expectedPropertyName}' property in the #info.txt file");
            }

            return jsonPropertyVal;
        }


        private static string CheckGetChunkEntryProperty(string currentJsonLine, string expectedPropertyName, int chunkCounter, ValueTypes valueType)
        {
            var chunkEntryPropertyValue = string.Empty;

            var propertyDataRead = currentJsonLine.Split(':');

            if (!propertyDataRead[0].StartsWith(expectedPropertyName))
            {
                IOhelpers.ErrorExit($"Error: Missing {expectedPropertyName} property at expected position. occured when parsing Chunk_{chunkCounter}.");
            }

            var isValidVal = false;

            switch (valueType)
            {
                case ValueTypes.Uint:
                    if (uint.TryParse(propertyDataRead[1].TrimEnd(','), out uint uintVal))
                    {
                        isValidVal = true;
                        chunkEntryPropertyValue = uintVal.ToString();
                    }
                    break;

                case ValueTypes.Byte:
                    if (byte.TryParse(propertyDataRead[1].TrimEnd(','), out byte byteVal))
                    {
                        isValidVal = true;
                        chunkEntryPropertyValue = byteVal.ToString();
                    }
                    break;
            }

            if (!isValidVal)
            {
                IOhelpers.ErrorExit($"Error: Invalid value specified for '{expectedPropertyName}' property. occured when parsing Chunk_{chunkCounter}.");
            }

            return chunkEntryPropertyValue;
        }
    }
}
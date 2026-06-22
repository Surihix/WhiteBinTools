using System;
using System.IO;
using WhiteBinTools.Filelist;
using WhiteBinTools.Support;
using WhiteBinTools.Support.Structures;
using static WhiteBinTools.Support.Enumerators;

namespace WhiteBinTools.Repack
{
    internal class RepackTypeE
    {
        public static void RepackJsonFilelist(GameCode gameCode, string jsonFile, StreamWriter logWriter)
        {
            SharedFunctions.CheckFileExists(jsonFile, logWriter, "Error: JSON file specified in the argument is missing");

            var newFilelistFileName = $"{Path.GetFileNameWithoutExtension(jsonFile)}";
            var newFilelistFile = Path.Combine(Path.GetDirectoryName(jsonFile), newFilelistFileName);

            if (Core.ShouldBckup)
            {
                logWriter.LogMessage("\nBacking up filelist bin file....\n");
                SharedFunctions.IfFileExistsDel($"{newFilelistFile}.bak");
                File.Move(newFilelistFile, $"{newFilelistFile}.bak");
            }

            logWriter.LogMessage("\nParsing json file....");

            using (var jsonReader = new StreamReader(jsonFile))
            {
                _ = jsonReader.ReadLine();

                var filelistHeader = new FilelistHeader();
                var filelistCryptHeader = new FilelistCryptHeader();

                if (gameCode == GameCode.ff132)
                {
                    // Determine encryption status
                    filelistCryptHeader.HasCryptHeader = bool.Parse(CheckGetMainProperty(jsonReader, "\"encrypted\"", ValueTypes.Boolean));

                    if (filelistCryptHeader.HasCryptHeader)
                    {
                        var seedA = ulong.Parse(CheckGetMainProperty(jsonReader, "\"seedA\"", ValueTypes.Ulong));
                        var seedB = ulong.Parse(CheckGetMainProperty(jsonReader, "\"seedB\"", ValueTypes.Ulong));

                        filelistCryptHeader.MD5Hash = new byte[16];
                        Array.Copy(BitConverter.GetBytes(seedA), 0, filelistCryptHeader.MD5Hash, 0, 8);
                        Array.Copy(BitConverter.GetBytes(seedB), 0, filelistCryptHeader.MD5Hash, 8, 8);

                        filelistCryptHeader.EncryptionTag = uint.Parse(CheckGetMainProperty(jsonReader, "\"encryptionTag(DO_NOT_CHANGE)\"", ValueTypes.Uint));
                    }
                }

                filelistHeader.FileCount = uint.Parse(CheckGetMainProperty(jsonReader, "\"fileCount\"", ValueTypes.Uint));
                filelistHeader.ChunkCount = int.Parse(CheckGetMainProperty(jsonReader, "\"chunkCount\"", ValueTypes.Int));

                logWriter.LogMessage("TotalChunks: " + filelistHeader.ChunkCount);
                logWriter.LogMessage("No of files: " + filelistHeader.FileCount + "\n");

                if (!jsonReader.ReadLine().TrimStart(' ').StartsWith("\"data\": {"))
                {
                    SharedFunctions.ErrorExit("Error: data property specified in the json file is invalid");
                }

                // Begin building the filelist
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

                // Process each path in chunks
                var currentEntryPropertyValue = string.Empty;
                var fileIndex = 0;
                var splitChara = new string[] { "\": " };

                for (int i = 0; i < filelistHeader.ChunkCount; i++)
                {
                    var currentChunk = $"Chunk_{i}";

                    if (!jsonReader.ReadLine().TrimStart(' ').StartsWith($"\"{currentChunk}\": ["))
                    {
                        SharedFunctions.ErrorExit($"Error: {currentChunk} property string specified in the json file is missing or invalid");
                    }

                    while (true)
                    {
                        var currentJsonLine = jsonReader.ReadLine().TrimStart(' ').TrimEnd(' ');

                        // Determine how to end processing the current chunk
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
                            currentEntryPropertyValue = CheckGetChunkEntryProperty(currentJsonLine, "\"fileCode\"", i, ValueTypes.Uint);
                        }

                        if (gameCode == GameCode.ff131 || gameCode == GameCode.dirge)
                        {
                            var filelistEntryV1 = new FilelistEntryV1
                            {
                                FileCode = uint.Parse(currentEntryPropertyValue),
                                ChunkID = (ushort)i
                            };

                            newEntryV1Table[fileIndex] = filelistEntryV1;
                        }
                        else
                        {
                            var filelistEntryV2 = new FilelistEntryV2()
                            {
                                FileCode = uint.Parse(currentEntryPropertyValue),
                                ChunkID = (ushort)i
                            };

                            currentJsonLine = jsonReader.ReadLine().TrimStart(' ').TrimEnd(' ');
                            currentEntryPropertyValue = CheckGetChunkEntryProperty(currentJsonLine, "\"fileTypeID\"", i, ValueTypes.Byte);
                            filelistEntryV2.FileTypeID = byte.Parse(currentEntryPropertyValue);

                            newEntryV2Table[fileIndex] = filelistEntryV2;
                        }

                        currentJsonLine = jsonReader.ReadLine().TrimStart(' ').TrimEnd(' ');
                        var fileInfoProperty = currentJsonLine.Split(splitChara, StringSplitOptions.None);

                        if (!fileInfoProperty[0].StartsWith("\"fileInfo"))
                        {
                            SharedFunctions.ErrorExit($"Error: Missing fileInfo property at expected position. occured when parsing Chunk_{i}.");
                        }

                        var whiteFileInfoString = fileInfoProperty[1].Replace("\"", "");

                        fileInfoStringPackTable[fileIndex] = new FileInfoStringPack() { ChunkID = (ushort)i, FileInfoString = $"{whiteFileInfoString}\0" };
                        fileIndex++;
                    }
                }

                logWriter.LogMessage("\nBuilding filelist....");
                FilelistBuilder.BuildFilelist(filelistCryptHeader, filelistHeader, gameCode, newEntryV1Table, newEntryV2Table, fileInfoStringPackTable, newFilelistFile);

                if (filelistCryptHeader.HasCryptHeader)
                {
                    FilelistCrypto.EncryptProcess(newFilelistFile, logWriter);
                }
            }

            logWriter.LogMessage($"\n\nFinished repacking JSON data to \"{Path.GetFileName(newFilelistFileName)}\"");
        }

        private static string CheckGetMainProperty(StreamReader jsonReader, string expectedPropertyName, ValueTypes valueType)
        {
            var jsonPropertyVal = string.Empty;

            var propertyDataRead = jsonReader.ReadLine().Split(':');

            if (!propertyDataRead[0].TrimStart(' ').StartsWith(expectedPropertyName))
            {
                SharedFunctions.ErrorExit($"Error: Missing {expectedPropertyName} property at expected position");
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

                case ValueTypes.Int:
                    if (int.TryParse(propertyDataRead[1].TrimEnd(','), out int intVal))
                    {
                        isValidVal = true;
                        jsonPropertyVal = intVal.ToString();
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
                SharedFunctions.ErrorExit($"Error: Invalid value specified for '{expectedPropertyName}' property in the #info.txt file");
            }

            return jsonPropertyVal;
        }

        private static string CheckGetChunkEntryProperty(string currentJsonLine, string expectedPropertyName, int chunkCounter, ValueTypes valueType)
        {
            var chunkEntryPropertyValue = string.Empty;

            var propertyDataRead = currentJsonLine.Split(':');

            if (!propertyDataRead[0].StartsWith(expectedPropertyName))
            {
                SharedFunctions.ErrorExit($"Error: Missing {expectedPropertyName} property at expected position. occured when parsing Chunk_{chunkCounter}.");
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
                SharedFunctions.ErrorExit($"Error: Invalid value specified for '{expectedPropertyName}' property. occured when parsing Chunk_{chunkCounter}.");
            }

            return chunkEntryPropertyValue;
        }
    }
}
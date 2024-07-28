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
            jsonFile.CheckFileExists(logWriter, "Error: json file specified in the argument is missing");

            var filelistVariables = new FilelistVariables();

            object tmpValueRead = string.Empty;
            using (var jsonReader = new StreamReader(jsonFile))
            {
                _ = jsonReader.ReadLine();

                // Determine encryption status
                tmpValueRead = CheckParseJsonProperty(jsonReader, "\"encrypted\":", "Error: encrypted status string in the json file is invalid");

                if (!bool.TryParse(tmpValueRead.ToString().Split(':')[1].TrimEnd(','), out bool isEncrypted))
                {
                    IOhelpers.ErrorExit("Error: unable to parse encrypted property's value");
                }

                filelistVariables.IsEncrypted = isEncrypted;

                // Get seed values if encrypted
                // and create the encryptedHeaderData
                // variable
                if (filelistVariables.IsEncrypted)
                {
                    tmpValueRead = CheckParseJsonProperty(jsonReader, "\"seedA\":", "Error: seedA property string in the json file is invalid");
                    if (!ulong.TryParse(tmpValueRead.ToString().Split(':')[1].TrimEnd(','), out ulong seedA))
                    {
                        IOhelpers.ErrorExit("Error: unable to parse seedA property's value");
                    }

                    tmpValueRead = CheckParseJsonProperty(jsonReader, "\"seedB\":", "Error: seedB property string in the json file is invalid");
                    if (!ulong.TryParse(tmpValueRead.ToString().Split(':')[1].TrimEnd(','), out ulong seedB))
                    {
                        IOhelpers.ErrorExit("Error: unable to parse seedB property's value");
                    }

                    tmpValueRead = CheckParseJsonProperty(jsonReader, "\"encryptionTag\":", "Error: encryptionTag property string in the json file is invalid");
                    if (!uint.TryParse(tmpValueRead.ToString().Split(':')[1].TrimEnd(','), out uint encTag))
                    {
                        IOhelpers.ErrorExit("Error: unable to parse encTag property's value");
                    }

                    using (var encHeaderStream = new MemoryStream())
                    {
                        using (var encHeaderWriter = new BinaryWriter(encHeaderStream))
                        {
                            encHeaderStream.Seek(0, SeekOrigin.Begin);

                            encHeaderWriter.WriteBytesUInt64(seedA, false);
                            encHeaderWriter.WriteBytesUInt64(seedB, false);
                            encHeaderWriter.WriteBytesUInt32(0, false);
                            encHeaderWriter.WriteBytesUInt32(encTag, false);
                            encHeaderWriter.WriteBytesUInt64(0, false);

                            encHeaderStream.Seek(0, SeekOrigin.Begin);
                            filelistVariables.EncryptedHeaderData = new byte[32];
                            filelistVariables.EncryptedHeaderData = encHeaderStream.ToArray();
                        }
                    }
                }

                // Get fileCount and chunkCount values
                tmpValueRead = CheckParseJsonProperty(jsonReader, "\"fileCount\":", "Error: fileCount property string in the json file is invalid");
                if (!uint.TryParse(tmpValueRead.ToString().Split(':')[1].TrimEnd(','), out uint fileCount))
                {
                    IOhelpers.ErrorExit("Error: unable to parse fileCount property's value");
                }

                tmpValueRead = CheckParseJsonProperty(jsonReader, "\"chunkCount\":", "Error: chunkCount property string in the json file is invalid");
                if (!uint.TryParse(tmpValueRead.ToString().Split(':')[1], out uint chunkCount))
                {
                    IOhelpers.ErrorExit("Error: unable to parse chunkCount property's value");
                }

                filelistVariables.TotalFiles = fileCount;
                filelistVariables.TotalChunks = chunkCount;

                logWriter.LogMessage("TotalChunks: " + filelistVariables.TotalChunks);
                logWriter.LogMessage("No of files: " + filelistVariables.TotalFiles + "\n");


                // Begin building the filelist
                logWriter.LogMessage("\n\nBuilding filelist....\n");
                tmpValueRead = CheckParseJsonProperty(jsonReader, "\"data\": {", "Error: data property string in the json file is invalid");

                var repackVariables = new RepackVariables();
                repackVariables.NewFilelistFile = Path.Combine(Path.GetDirectoryName(jsonFile), Path.GetFileNameWithoutExtension(jsonFile));

                if (Core.ShouldBckup)
                {
                    if (File.Exists(repackVariables.NewFilelistFile))
                    {
                        (repackVariables.NewFilelistFile + ".bak").IfFileExistsDel();

                        File.Copy(repackVariables.NewFilelistFile, repackVariables.NewFilelistFile + ".bak");
                    }
                }

                repackVariables.NewFilelistFile.IfFileExistsDel();

                // Build an empty dictionary
                // for the chunks 
                var newChunksDict = new Dictionary<int, List<byte>>();
                RepackProcesses.CreateEmptyNewChunksDict(filelistVariables, newChunksDict);

                // Build a number list containing all
                // the odd number chunks if the code
                // is set to 2
                var oddChunkNumValues = new List<int>();
                if (gameCode.Equals(GameCodes.ff132) && filelistVariables.TotalChunks > 1)
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


                // Process each path in chunks

                using (var entriesStream = new MemoryStream())
                {
                    using (var entriesWriter = new BinaryWriter(entriesStream))
                    {
                        var runMainLoop = true;
                        var endLoopNext = false;

                        var currentChunk = string.Empty;
                        var chunkStringStartChara = "\"Chunk_";
                        var chunkCounter = 0;
                        var oddChunkCounter = 0;
                        long entriesWriterPos = 0;

                        var currentData = new string[] { };
                        var splitChara = new string[] { ", " };
                        var splitChara2 = new string[] { ": " };
                        var fileCodeData = string.Empty;
                        var unkValueData = string.Empty;

                        uint fileCounter = uint.MinValue;
                        var lastFile = filelistVariables.TotalFiles;

                        while (runMainLoop)
                        {
                            filelistVariables.LastChunkNumber = chunkCounter;
                            currentChunk = chunkStringStartChara + chunkCounter + "\"";
                            tmpValueRead = CheckParseJsonProperty(jsonReader, currentChunk, $"Error: {currentChunk} property string in the json file is invalid");

                            endLoopNext = false;

                            while (true)
                            {
                                if (endLoopNext)
                                {
                                    chunkCounter++;
                                    oddChunkCounter++;
                                    _ = jsonReader.ReadLine();
                                    _ = jsonReader.ReadLine();
                                    break;
                                }

                                tmpValueRead = jsonReader.ReadLine().TrimStart(' ').TrimEnd(' ');

                                // Assume that the chunk
                                // is going to end if the
                                // string is ending with '}' chara
                                if (tmpValueRead.ToString().EndsWith("}"))
                                {
                                    endLoopNext = true;
                                }

                                // Assume that we have the
                                // data at this stage
                                currentData = tmpValueRead.ToString().Split(splitChara, StringSplitOptions.None);
                                if (currentData.Length < 2)
                                {
                                    IOhelpers.ErrorExit($"Error: Unable to parse data. occured when parsing {currentChunk}.");
                                }

                                // filecode
                                fileCodeData = currentData[0].ToString();

                                if (!fileCodeData.StartsWith("{ \"fileCode\":"))
                                {
                                    IOhelpers.ErrorExit($"Error: fileCode property string in the json file was invalid. occured when parsing {currentChunk}.");
                                }

                                if (!uint.TryParse(fileCodeData.Split(splitChara2, StringSplitOptions.None)[1], out uint fileCode))
                                {
                                    IOhelpers.ErrorExit($"Error: unable to parse fileCode property's value. occured when parsing {currentChunk}.");
                                }

                                entriesWriter.BaseStream.Position = entriesWriterPos;
                                entriesWriter.WriteBytesUInt32(fileCode, false);

                                // According to the gameCode
                                // determine how the
                                // path positon, chunk number
                                // and unkValue values are written
                                if (gameCode.Equals(GameCodes.ff132))
                                {
                                    if (currentData.Length < 3)
                                    {
                                        IOhelpers.ErrorExit($"Error: Unable to parse data. occured at position {jsonReader.BaseStream.Position}.");
                                    }

                                    // unkValue
                                    unkValueData = currentData[1].ToString();

                                    if (!unkValueData.StartsWith("\"unkValue\":"))
                                    {
                                        IOhelpers.ErrorExit($"Error: unkValue property string in the json file was invalid. occured when parsing {currentChunk}.");
                                    }

                                    if (!byte.TryParse(unkValueData.Split(splitChara2, StringSplitOptions.None)[1].TrimEnd(','), out byte unkValue))
                                    {
                                        IOhelpers.ErrorExit($"Error: unable to parse unkValue property's value. occured when parsing {currentChunk}.");
                                    }

                                    // path position
                                    if (oddChunkNumValues.Contains(chunkCounter))
                                    {
                                        oddChunkCounter = oddChunkNumValues.IndexOf(chunkCounter);

                                        // Write the 32768 position value
                                        // to indicate that the chunk
                                        // number is odd
                                        entriesWriter.BaseStream.Position = entriesWriterPos + 4;
                                        entriesWriter.WriteBytesUInt16(32768, false);
                                    }
                                    else
                                    {
                                        // Write zero as path number
                                        entriesWriter.BaseStream.Position = entriesWriterPos + 4;
                                        entriesWriter.WriteBytesUInt16(0, false);
                                    }

                                    // chunk number
                                    entriesWriter.BaseStream.Position = entriesWriterPos + 6;
                                    entriesWriter.Write((byte)oddChunkCounter);

                                    // unkValue
                                    entriesWriter.BaseStream.Position = entriesWriterPos + 7;
                                    entriesWriter.Write(unkValue);

                                    // Add path to dictionary
                                    var pathRead = currentData[2];
                                    if (!pathRead.StartsWith("\"filePath\":"))
                                    {
                                        IOhelpers.ErrorExit($"Error: filePath property string in the json file was invalid. occured when parsing {currentChunk}.");
                                    }

                                    pathRead = pathRead.Split(splitChara2, StringSplitOptions.None)[1];
                                    pathRead = pathRead.Remove(0, 1);
                                    pathRead = ProcessPathString(pathRead);

                                    newChunksDict[chunkCounter].AddRange(Encoding.UTF8.GetBytes(pathRead + "\0"));
                                }
                                else
                                {
                                    // Write chunk number
                                    entriesWriter.BaseStream.Position = entriesWriterPos + 4;
                                    entriesWriter.WriteBytesUInt16((ushort)chunkCounter, false);

                                    // Write zero as path number
                                    entriesWriter.BaseStream.Position = entriesWriterPos + 6;
                                    entriesWriter.WriteBytesUInt16(0, false);

                                    // Add path to dictionary
                                    var pathRead = currentData[1];
                                    if (!pathRead.StartsWith("\"filePath\":"))
                                    {
                                        IOhelpers.ErrorExit($"Error: filePath property string in the json file was invalid. occured when parsing {currentChunk}.");
                                    }

                                    pathRead = pathRead.Split(splitChara2, StringSplitOptions.None)[1];
                                    pathRead = pathRead.Remove(0, 1);
                                    pathRead = ProcessPathString(pathRead);

                                    newChunksDict[chunkCounter].AddRange(Encoding.UTF8.GetBytes(pathRead + "\0"));
                                }

                                entriesWriterPos += 8;
                                fileCounter++;

                                // End the loop if the 
                                // filecounter value is
                                // same as the last file
                                if (fileCounter == filelistVariables.TotalFiles)
                                {
                                    runMainLoop = false;
                                    break;
                                }
                            }
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

                logWriter.LogMessage("\n\nFinished repacking filelist data into " + "\"" + Path.GetFileName(repackVariables.NewFilelistFile) + "\"");
            }
        }


        private static string CheckParseJsonProperty(StreamReader jsonReader, string propertyValue, string errorMsg)
        {
            var valueRead = jsonReader.ReadLine().TrimStart(' ');

            if (!valueRead.StartsWith(propertyValue))
            {
                IOhelpers.ErrorExit(errorMsg);
            }

            return valueRead;
        }


        private static string ProcessPathString(string pathRead)
        {
            var pathString = string.Empty;
            for (int s = 0; s < pathRead.Length; s++)
            {
                if (pathRead[s] == '"')
                {
                    break;
                }
                else
                {
                    pathString += pathRead[s];
                }
            }

            return pathString;
        }
    }
}
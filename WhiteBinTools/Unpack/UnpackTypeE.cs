using System;
using System.IO;
using System.Text;
using WhiteBinTools.Filelist;
using WhiteBinTools.Support;
using WhiteBinTools.Support.Structures;
using static WhiteBinTools.Support.Enumerators;

namespace WhiteBinTools.Unpack
{
    internal class UnpackTypeE
    {
        public static void UnpackFilelistJson(GameCode gameCode, string filelistFile, StreamWriter logWriter)
        {
            SharedFunctions.CheckFileExists(filelistFile, logWriter, "Error: Filelist file specified in the argument is missing");

            var filelistLoadData = FilelistLoader.LoadFilelist(gameCode, filelistFile, logWriter);

            var filelistCryptHeader = filelistLoadData.FilelistCryptHeader;
            var filelistHeader = filelistLoadData.FilelistHeader;
            var filelistEntryV1Table = filelistLoadData.FilelistEntryV1Table;
            var filelistEntryV2Table = filelistLoadData.FilelistEntryV2Table;
            var filelistChunks = filelistLoadData.FilelistChunks;

            var outJsonFile = $"{filelistFile}.json";

            SharedFunctions.IfFileExistsDel(outJsonFile);

            using (var outJsonWriter = new StreamWriter(outJsonFile, true))
            {
                outJsonWriter.WriteLine("{");

                if (gameCode == GameCode.ff132)
                {
                    outJsonWriter.WriteLine($"  \"encrypted\": {filelistCryptHeader.HasCryptHeader.ToString().ToLowerInvariant()},");

                    if (filelistCryptHeader.HasCryptHeader)
                    {
                        var seedA = BitConverter.ToUInt64(filelistCryptHeader.MD5Hash, 0);
                        var seedB = BitConverter.ToUInt64(filelistCryptHeader.MD5Hash, 8);

                        outJsonWriter.WriteLine($"  \"seedA\": {seedA},");
                        outJsonWriter.WriteLine($"  \"seedB\": {seedB},");
                        outJsonWriter.WriteLine($"  \"encryptionTag(DO_NOT_CHANGE)\": {filelistCryptHeader.EncryptionTag},");
                    }
                }

                outJsonWriter.WriteLine($"  \"fileCount\": {filelistHeader.FileCount},");
                outJsonWriter.WriteLine($"  \"chunkCount\": {filelistHeader.ChunkCount},");
                outJsonWriter.WriteLine("  \"data\": {");

                var fileInfoStringPackTable = new FileInfoStringPack[filelistHeader.FileCount];
                var stringsCountTable = new int[filelistHeader.ChunkCount];

                for (int i = 0; i < filelistHeader.FileCount; i++)
                {
                    string whiteFileInfoString;
                    var entryAndPathDataString = new StringBuilder();
                    ushort currentChunkID;

                    if (gameCode == GameCode.ff131 || gameCode == GameCode.dirge)
                    {
                        var filelistEntryV1 = filelistEntryV1Table[i];
                        whiteFileInfoString = FilelistLoader.GetWhiteFileInfoString(filelistEntryV1.FileInfoPos, filelistChunks, filelistEntryV1.ChunkID);

                        entryAndPathDataString.Append($"        \"fileCode\": {filelistEntryV1.FileCode},\r\n");
                        entryAndPathDataString.Append($"        \"fileInfo\": \"{whiteFileInfoString}\"\r\n");

                        fileInfoStringPackTable[i] = new FileInfoStringPack() { ChunkID = filelistEntryV1.ChunkID, FileInfoString = entryAndPathDataString.ToString() };
                        currentChunkID = filelistEntryV1.ChunkID;
                    }
                    else
                    {
                        var filelistEntryV2 = filelistEntryV2Table[i];
                        whiteFileInfoString = FilelistLoader.GetWhiteFileInfoString(filelistEntryV2.FileInfoPos, filelistChunks, filelistEntryV2.ChunkID);

                        entryAndPathDataString.Append($"        \"fileCode\": {filelistEntryV2.FileCode},\r\n");
                        entryAndPathDataString.Append($"        \"fileTypeID\": {filelistEntryV2.FileTypeID},\r\n");
                        entryAndPathDataString.Append($"        \"fileInfo\": \"{whiteFileInfoString}\"\r\n");

                        fileInfoStringPackTable[i] = new FileInfoStringPack() { ChunkID = filelistEntryV2.ChunkID, FileInfoString = entryAndPathDataString.ToString() };
                        currentChunkID = filelistEntryV2.ChunkID;
                    }

                    stringsCountTable[currentChunkID] = stringsCountTable[currentChunkID] + 1;
                }

                for (int i = 0; i < filelistHeader.ChunkCount; i++)
                {
                    outJsonWriter.WriteLine($"    \"Chunk_{i}\": [");
                    var currentChunkStringsCount = stringsCountTable[i];
                    int stringsProcessed = 0;

                    for (int j = 0; j < filelistHeader.FileCount; j++)
                    {
                        if (fileInfoStringPackTable[j].ChunkID == i)
                        {
                            outJsonWriter.WriteLine("      {");
                            outJsonWriter.Write(fileInfoStringPackTable[j].FileInfoString);

                            if (stringsProcessed + 1 == currentChunkStringsCount)
                            {
                                outJsonWriter.WriteLine("      }");
                            }
                            else
                            {
                                outJsonWriter.WriteLine("      },");
                            }

                            stringsProcessed++;
                        }
                    }

                    if ((i + 1) == filelistHeader.ChunkCount)
                    {
                        outJsonWriter.WriteLine("    ]");
                    }
                    else
                    {
                        outJsonWriter.WriteLine("    ],");
                    }
                }

                outJsonWriter.WriteLine("  }");
                outJsonWriter.Write("}");
            }

            logWriter.LogMessage($"\n\nFinished writing filelist data to \"{Path.GetFileName(outJsonFile)}\"");
        }
    }
}
﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using WhiteBinTools.Filelist;
using WhiteBinTools.Support;
using static WhiteBinTools.Support.ProgramEnums;

namespace WhiteBinTools.Unpack
{
    internal class UnpackTypeD
    {
        public static void UnpackFilelist(GameCodes gameCode, string filelistFile, StreamWriter logWriter)
        {
            IOhelpers.CheckFileExists(filelistFile, logWriter, "Error: Filelist file specified in the argument is missing");

            var filelistVariables = new FilelistVariables();

            FilelistProcesses.PrepareFilelistVars(filelistVariables, filelistFile);

            FilelistCrypto.DecryptProcess(gameCode, filelistVariables, logWriter);

            using (var filelistStream = new FileStream(filelistVariables.MainFilelistFile, FileMode.Open, FileAccess.Read))
            {
                using (var filelistReader = new BinaryReader(filelistStream))
                {
                    FilelistChunksPrep.GetFilelistOffsets(filelistReader, logWriter, filelistVariables);
                    FilelistChunksPrep.BuildChunks(filelistStream, filelistVariables);
                }
            }

            if (filelistVariables.IsEncrypted)
            {
                IOhelpers.IfFileExistsDel(filelistVariables.TmpDcryptFilelistFile);
                filelistVariables.MainFilelistFile = filelistFile;

                using (var encDataReader = new BinaryReader(File.Open(filelistFile, FileMode.Open, FileAccess.Read)))
                {
                    encDataReader.BaseStream.Position = 0;
                    filelistVariables.SeedA = encDataReader.ReadUInt64();
                    filelistVariables.SeedB = encDataReader.ReadUInt64();

                    encDataReader.BaseStream.Position += 4;
                    filelistVariables.EncTag = encDataReader.ReadUInt32();
                }
            }

            var filelistOutName = Path.GetFileName(filelistFile);
            var extractedFilelistDir = Path.Combine(filelistVariables.MainFilelistDirectory, "_" + filelistOutName);
            var infoFile = Path.Combine(extractedFilelistDir, "#info.txt");
            var chunkTxtFilePathPrefix = Path.Combine(extractedFilelistDir, $"Chunk_");

            IOhelpers.IfDirExistsDel(extractedFilelistDir);
            Directory.CreateDirectory(extractedFilelistDir);

            using (var infoStreamWriter = new StreamWriter(infoFile, true))
            {
                if (gameCode == GameCodes.ff132)
                {
                    filelistVariables.CurrentChunkNumber = -1;
                    infoStreamWriter.WriteLine($"encrypted: {filelistVariables.IsEncrypted.ToString().ToLower()}");

                    if (filelistVariables.IsEncrypted)
                    {
                        infoStreamWriter.WriteLine($"seedA: {filelistVariables.SeedA}");
                        infoStreamWriter.WriteLine($"seedB: {filelistVariables.SeedB}");
                        infoStreamWriter.WriteLine($"encryptionTag(DO_NOT_CHANGE): {filelistVariables.EncTag}");
                    }
                }

                infoStreamWriter.WriteLine($"fileCount: {filelistVariables.TotalFiles}");
                infoStreamWriter.WriteLine($"chunkCount: {filelistVariables.TotalChunks}");
            }

            // Build an empty dictionary
            var outChunksDict = new Dictionary<int, List<string>>();
            for (int c = 0; c < filelistVariables.TotalChunks; c++)
            {
                var chunkDataList = new List<string>();
                outChunksDict.Add(c, chunkDataList);
            }

            // Collect all of the chunk data into
            // the empty dictionary
            using (var entriesStream = new MemoryStream())
            {
                entriesStream.Write(filelistVariables.EntriesData, 0, filelistVariables.EntriesData.Length);
                entriesStream.Seek(0, SeekOrigin.Begin);

                using (var entriesReader = new BinaryReader(entriesStream))
                {
                    // Process each file entry from 
                    // the entry section
                    long entriesReadPos = 0;
                    var stringData = "";

                    for (int f = 0; f < filelistVariables.TotalFiles; f++)
                    {
                        FilelistProcesses.GetCurrentFileEntry(gameCode, entriesReader, entriesReadPos, filelistVariables);
                        entriesReadPos += 8;

                        stringData = "";

                        if (gameCode == GameCodes.ff131)
                        {
                            stringData += filelistVariables.FileCode + "|";
                            stringData += filelistVariables.PathString;

                            outChunksDict[filelistVariables.ChunkNumber].Add(stringData);
                        }
                        else
                        {
                            stringData += filelistVariables.FileCode + "|";
                            stringData += filelistVariables.FileTypeID + "|";
                            stringData += filelistVariables.PathString;

                            outChunksDict[filelistVariables.CurrentChunkNumber].Add(stringData);
                        }
                    }
                }
            }

            // Write all of the collected data from
            // the dictionary into multiple txt
            // files
            for (int d = 0; d < filelistVariables.TotalChunks; d++)
            {
                using (var chunkWriter = new StreamWriter(chunkTxtFilePathPrefix + d + ".txt", true, new UTF8Encoding(false)))
                {
                    foreach (var stringData in outChunksDict[d])
                    {
                        chunkWriter.WriteLine(stringData);
                    }
                }
            }

            logWriter.LogMessage($"\nFinished unpacking \"{filelistOutName}\"");
        }
    }
}
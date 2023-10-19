using System.Collections.Generic;
using System.IO;
using System.Linq;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.SupportClasses;

namespace WhiteBinTools.RepackClasses
{
    internal class RepackTypeD
    {
        public static void RepackFilelist(CmnEnums.GameCodes gameCodeVar, string filelistFileVar, string extractedFilelistDir, StreamWriter logWriter)
        {
            filelistFileVar.CheckFileExists(logWriter, "Error: Filelist file specified in the argument is missing");
            extractedFilelistDir.CheckDirExists(logWriter, "Error: Unpacked filelist directory specified in the argument is missing");

            var filelistFileDir = Path.GetDirectoryName(filelistFileVar);
            var filelistFileName = Path.GetFileName(filelistFileVar);

            var countsFile = extractedFilelistDir + "\\~Counts.txt";
            var encHeaderFile = extractedFilelistDir + "\\EncryptionHeader_(DON'T EDIT)";
            var outChunksDir = extractedFilelistDir + "\\_chunks";

            countsFile.CheckFileExists(logWriter, "Error: Unable to locate the '~Counts.txt' file");


            var filelistVariables = new FilelistProcesses();
            filelistVariables.IsEncrypted = FilelistProcesses.CheckIfEncrypted(filelistFileVar);

            uint encHeaderAdjustedOffset = 0;
            if (filelistVariables.IsEncrypted)
            {
                encHeaderAdjustedOffset += 32;
                encHeaderFile.CheckFileExists(logWriter, "Error: Unable to locate the 'EncryptionHeader_(DON'T EDIT)' file");
                "ffxiiicrypt.exe".CheckFileExists(logWriter, "Error: Unable to locate ffxiiicrypt tool in the main app folder to encrypt the filelist file");
            }

            var backupFilelistFile = Path.Combine(filelistFileDir, filelistFileName + ".bak");
            backupFilelistFile.IfFileExistsDel();
            File.Copy(filelistFileVar, backupFilelistFile);
            File.Delete(filelistFileVar);


            using (var countsReader = new StreamReader(countsFile))
            {
                filelistVariables.TotalFiles = uint.Parse(countsReader.ReadLine());
                filelistVariables.TotalChunks = uint.Parse(countsReader.ReadLine());
            }


            var int16RangeValues = new List<uint>();
            if (gameCodeVar.Equals(CmnEnums.GameCodes.ff132) && filelistVariables.TotalChunks > 1)
            {
                var nextChunkNo = 1;
                for (int i = 0; i < filelistVariables.TotalChunks; i++)
                {
                    if (i == nextChunkNo)
                    {
                        int16RangeValues.Add((uint)i);
                        nextChunkNo += 2;
                    }
                }
            }


            using (var emptyFilelistStream = new FileStream(filelistFileVar, FileMode.Append, FileAccess.Write))
            {
                if (filelistVariables.IsEncrypted)
                {
                    using (var encHeader = new FileStream(encHeaderFile, FileMode.Open, FileAccess.Read))
                    {
                        encHeader.ExtendedCopyTo(emptyFilelistStream, 0, encHeader.Length);
                    }
                }

                var amountToPad = 12 + (filelistVariables.TotalFiles * 8) + (filelistVariables.TotalChunks * 12);
                for (int b = 0; b < amountToPad; b++)
                {
                    emptyFilelistStream.WriteByte(0);
                }
            }


            filelistVariables.ChunkFNameCount = 0;
            using (var entriesStream = new FileStream(filelistFileVar, FileMode.Open, FileAccess.Write))
            {
                using (var entriesWriter = new BinaryWriter(entriesStream))
                {
                    outChunksDir.IfDirExistsDel();
                    Directory.CreateDirectory(outChunksDir);


                    var lastChunk = filelistVariables.TotalChunks - 1;
                    uint entriesWritePos = encHeaderAdjustedOffset + 12;

                    for (int c = 0; c < filelistVariables.TotalChunks; c++)
                    {
                        var currentChunkFile = extractedFilelistDir + "\\" + $"Chunk_{filelistVariables.ChunkFNameCount}.txt";
                        var outChunkFile = outChunksDir + "\\" + $"Chunk_{filelistVariables.ChunkFNameCount}";

                        using (var currentChunkReader = new StreamReader(currentChunkFile))
                        {
                            using (var outChunkStream = new FileStream(outChunkFile, FileMode.Append, FileAccess.Write))
                            {
                                using (var outChunkWriter = new StreamWriter(outChunkStream))
                                {

                                    var filesInChunk = File.ReadAllLines(currentChunkFile).Count();
                                    ushort pathPos = 0;

                                    for (int f = 0; f < filesInChunk; f++)
                                    {
                                        var chunkData = currentChunkReader.ReadLine().Split('|');
                                        var fileCode = uint.Parse(chunkData[0]);

                                        entriesWriter.AdjustBytesUInt32(entriesWritePos, fileCode, CmnEnums.Endianness.LittleEndian);

                                        switch (gameCodeVar)
                                        {
                                            case CmnEnums.GameCodes.ff131:
                                                var chunkNumber = ushort.Parse(chunkData[1]);

                                                entriesWriter.AdjustBytesUInt16(entriesWritePos + 4, chunkNumber);
                                                entriesWriter.AdjustBytesUInt16(entriesWritePos + 6, pathPos);

                                                outChunkWriter.Write(chunkData[2] + "\0");
                                                pathPos += (ushort)(chunkData[2] + "\0").Length;
                                                break;

                                            case CmnEnums.GameCodes.ff132:
                                                if (int16RangeValues.Contains(filelistVariables.ChunkFNameCount))
                                                {
                                                    entriesWriter.AdjustBytesUInt16(entriesWritePos + 4, (ushort)(32768 + pathPos));
                                                }
                                                else
                                                {
                                                    entriesWriter.AdjustBytesUInt16(entriesWritePos + 4, pathPos);
                                                }

                                                var chunkNumByte = byte.Parse(chunkData[1]);
                                                var unkVal = byte.Parse(chunkData[2]);

                                                entriesWriter.BaseStream.Position = entriesWritePos + 6;
                                                entriesWriter.Write(chunkNumByte);

                                                entriesWriter.BaseStream.Position = entriesWritePos + 7;
                                                entriesWriter.Write(unkVal);

                                                outChunkWriter.Write(chunkData[3] + "\0");
                                                pathPos += (ushort)(chunkData[3] + "\0").Length;
                                                break;
                                        }

                                        entriesWritePos += 8;
                                    }

                                    if (filelistVariables.ChunkFNameCount == lastChunk)
                                    {
                                        outChunkWriter.Write("end\0");
                                    }
                                }
                            }
                        }

                        filelistVariables.ChunkFNameCount++;
                    }
                }
            }


            filelistVariables.ChunkFNameCount = 0;
            uint chunkStart = 0;
            var chunksInfoWriterPos = encHeaderAdjustedOffset + 12 + (filelistVariables.TotalFiles * 8);
            var chunksDataStartPos = encHeaderAdjustedOffset + 12 + (filelistVariables.TotalFiles * 8) + (filelistVariables.TotalChunks * 12);

            using (var chunkDataStream = new FileStream(filelistFileVar, FileMode.Append, FileAccess.Write, FileShare.Write))
            {
                using (var chunkInfoStream = new FileStream(filelistFileVar, FileMode.Open, FileAccess.Write, FileShare.Write))
                {
                    using (var chunkInfoWriter = new BinaryWriter(chunkInfoStream))
                    {

                        chunkInfoWriter.AdjustBytesUInt32(encHeaderAdjustedOffset, chunksInfoWriterPos - encHeaderAdjustedOffset, CmnEnums.Endianness.LittleEndian);
                        chunkInfoWriter.AdjustBytesUInt32(encHeaderAdjustedOffset + 4, chunksDataStartPos - encHeaderAdjustedOffset, CmnEnums.Endianness.LittleEndian);
                        chunkInfoWriter.AdjustBytesUInt32(encHeaderAdjustedOffset + 8, filelistVariables.TotalFiles, CmnEnums.Endianness.LittleEndian);


                        for (int fc = 0; fc < filelistVariables.TotalChunks; fc++)
                        {
                            var currentChunkFile = outChunksDir + "\\" + $"Chunk_{filelistVariables.ChunkFNameCount}";
                            var uncmpSize = (uint)new FileInfo(currentChunkFile).Length;

                            var cmpChunkArray = currentChunkFile.ZlibCompress();
                            var cmpSize = (uint)cmpChunkArray.Length;
                            chunkDataStream.Write(cmpChunkArray, 0, cmpChunkArray.Length);

                            chunkInfoWriter.AdjustBytesUInt32(chunksInfoWriterPos, uncmpSize, CmnEnums.Endianness.LittleEndian);
                            chunkInfoWriter.AdjustBytesUInt32(chunksInfoWriterPos + 4, cmpSize, CmnEnums.Endianness.LittleEndian);
                            chunkInfoWriter.AdjustBytesUInt32(chunksInfoWriterPos + 8, chunkStart, CmnEnums.Endianness.LittleEndian);

                            chunkStart += cmpSize;
                            chunksInfoWriterPos += 12;
                            filelistVariables.ChunkFNameCount++;
                        }
                    }
                }
            }


            outChunksDir.IfDirExistsDel();

            var repackVariables = new RepackProcesses();
            repackVariables.NewFilelistFile = filelistFileVar;

            if (filelistVariables.IsEncrypted.Equals(true))
            {
                FilelistProcesses.EncryptProcess(repackVariables, logWriter);
            }

            IOhelpers.LogMessage("\n\nFinished repacking " + Path.GetFileName(filelistFileVar), logWriter);
        }
    }
}
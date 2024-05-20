using System.Collections.Generic;
using System.IO;
using System.Text;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.SupportClasses;
using static WhiteBinTools.SupportClasses.ProgramEnums;

namespace WhiteBinTools.RepackClasses
{
    internal class RepackTypeD
    {
        public static void RepackFilelist(GameCodes gameCode, string extractedFilelistDir, StreamWriter logWriter)
        {
            extractedFilelistDir.CheckDirExists(logWriter, "Error: Unpacked filelist directory specified in the argument is missing");

            var encHeaderFile = Path.Combine(extractedFilelistDir, "~EncryptionHeader_(DON'T DELETE)");
            var countsFile = Path.Combine(extractedFilelistDir, "~Counts.txt");

            countsFile.CheckFileExists(logWriter, "Error: Unable to locate the '~Counts.txt' file");

            // Assume the filelist is supposed to be 
            // encrypted if the encHeader file exists
            var filelistVariables = new FilelistVariables();
            if (File.Exists(encHeaderFile))
            {
                filelistVariables.IsEncrypted = true;
                filelistVariables.EncryptedHeaderData = new byte[32];
                filelistVariables.EncryptedHeaderData = File.ReadAllBytes(encHeaderFile);
            }

            var repackVariables = new RepackVariables();
            repackVariables.NewFilelistFile = Path.Combine(Path.GetDirectoryName(extractedFilelistDir), Path.GetFileName(extractedFilelistDir).Remove(0, 1));

            if (File.Exists(repackVariables.NewFilelistFile))
            {
                (repackVariables.NewFilelistFile + ".bak").IfFileExistsDel();

                File.Copy(repackVariables.NewFilelistFile, repackVariables.NewFilelistFile + ".bak");
                File.Delete(repackVariables.NewFilelistFile);
            }

            using (var countsReader = new StreamReader(countsFile))
            {
                filelistVariables.TotalFiles = uint.Parse(countsReader.ReadLine());
                filelistVariables.TotalChunks = uint.Parse(countsReader.ReadLine());

                IOhelpers.LogMessage("TotalChunks: " + filelistVariables.TotalChunks, logWriter);
                IOhelpers.LogMessage("No of files: " + filelistVariables.TotalFiles + "\n", logWriter);
            }

            IOhelpers.LogMessage("\n\nBuilding filelist....", logWriter);


            // Build an empty dictionary
            // for the chunks 
            var newChunksDict = new Dictionary<int, List<byte>>();
            for (int c = 0; c < filelistVariables.TotalChunks; c++)
            {
                var chunkDataList = new List<byte>();
                newChunksDict.Add(c, chunkDataList);
            }

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


            filelistVariables.LastChunkNumber = 0;

            using (var entriesStream = new MemoryStream())
            {
                using (var entriesWriter = new BinaryWriter(entriesStream))
                {                
                    
                    // Process each path in chunks
                    var chunkFilePath = Path.Combine(extractedFilelistDir, "Chunk_");
                    long entriesWriterPos = 0;
                    for (int c = 0; c < filelistVariables.TotalChunks; c++)
                    {
                        using (var currentChunkReader = new StreamReader(chunkFilePath + c + ".txt", Encoding.UTF8))
                        {
                            string line;
                            while ((line = currentChunkReader.ReadLine()) != null)
                            {
                                var stringData = line.Split('|');

                                // Write filecode
                                entriesWriter.BaseStream.Position = entriesWriterPos;
                                entriesWriter.WriteBytesUInt32(uint.Parse(stringData[0]), false);

                                if (gameCode.Equals(GameCodes.ff132))
                                {
                                    if (oddChunkNumValues.Contains(c))
                                    {
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

                                    // Write chunk number
                                    entriesWriter.BaseStream.Position = entriesWriterPos + 6;
                                    entriesWriter.Write(byte.Parse(stringData[1]));

                                    // Write UnkEntryVal
                                    entriesWriter.BaseStream.Position = entriesWriterPos + 7;
                                    entriesWriter.Write(byte.Parse(stringData[2]));

                                    // Add path to dictionary
                                    newChunksDict[c].AddRange(Encoding.UTF8.GetBytes(stringData[3] + "\0"));
                                }
                                else
                                {
                                    // Write chunk number
                                    entriesWriter.BaseStream.Position = entriesWriterPos + 4;
                                    entriesWriter.WriteBytesUInt16(ushort.Parse(stringData[1]), false);

                                    // Write zero as path number
                                    entriesWriter.BaseStream.Position = entriesWriterPos + 6;
                                    entriesWriter.WriteBytesUInt16(0, false);

                                    // Add path to dictionary
                                    newChunksDict[c].AddRange(Encoding.UTF8.GetBytes(stringData[2] + "\0"));
                                }

                                entriesWriterPos += 8;
                            }
                        }

                        filelistVariables.LastChunkNumber = c;
                    }

                    filelistVariables.EntriesData = new byte[entriesStream.Length];
                    entriesStream.Seek(0, SeekOrigin.Begin);
                    entriesStream.Read(filelistVariables.EntriesData, 0, filelistVariables.EntriesData.Length);
                }
            }


            RepackFilelistData.BuildFilelist(filelistVariables, newChunksDict, repackVariables, gameCode);

            if (filelistVariables.IsEncrypted)
            {
                FilelistProcesses.EncryptProcess(repackVariables, logWriter);
            }

            IOhelpers.LogMessage("\n\nFinished repacking filelist data into " + "\"" + Path.GetFileName(repackVariables.NewFilelistFile) + "\"", logWriter);


            //using (var emptyFilelistStream = new FileStream(newFilelistFile, FileMode.Append, FileAccess.Write))
            //{
            //    if (filelistVariables.IsEncrypted)
            //    {
            //        using (var encHeader = new FileStream(encHeaderFile, FileMode.Open, FileAccess.Read))
            //        {
            //            encHeader.Seek(0, SeekOrigin.Begin);
            //            encHeader.CopyStreamTo(emptyFilelistStream, encHeader.Length, false);
            //        }
            //    }

            //    var amountToPad = 12 + (filelistVariables.TotalFiles * 8) + (filelistVariables.TotalChunks * 12);
            //    for (int b = 0; b < amountToPad; b++)
            //    {
            //        emptyFilelistStream.WriteByte(0);
            //    }
            //}


            //filelistVariables.ChunkFNameCount = 0;
            //using (var entriesStream = new FileStream(newFilelistFile, FileMode.Open, FileAccess.Write))
            //{
            //    using (var entriesWriter = new BinaryWriter(entriesStream))
            //    {
            //        outChunksDir.IfDirExistsDel();
            //        Directory.CreateDirectory(outChunksDir);


            //        var lastChunk = filelistVariables.TotalChunks - 1;
            //        uint entriesWritePos = encHeaderAdjustedOffset + 12;

            //        for (int c = 0; c < filelistVariables.TotalChunks; c++)
            //        {
            //            var currentChunkFile = Path.Combine(extractedFilelistDir, $"Chunk_{filelistVariables.ChunkFNameCount}.txt");
            //            var outChunkFile = Path.Combine(outChunksDir, $"Chunk_{filelistVariables.ChunkFNameCount}");

            //            using (var currentChunkReader = new StreamReader(currentChunkFile))
            //            {
            //                using (var outChunkStream = new FileStream(outChunkFile, FileMode.Append, FileAccess.Write))
            //                {
            //                    using (var outChunkWriter = new StreamWriter(outChunkStream))
            //                    {

            //                        var filesInChunk = File.ReadAllLines(currentChunkFile).Count();
            //                        ushort pathPos = 0;

            //                        for (int f = 0; f < filesInChunk; f++)
            //                        {
            //                            var chunkData = currentChunkReader.ReadLine().Split('|');
            //                            var fileCode = uint.Parse(chunkData[0]);

            //                            entriesWriter.BaseStream.Position = entriesWritePos;
            //                            entriesWriter.WriteBytesUInt32(fileCode, false);

            //                            switch (gameCode)
            //                            {
            //                                case GameCodes.ff131:
            //                                    var chunkNumber = ushort.Parse(chunkData[1]);

            //                                    entriesWriter.BaseStream.Position = entriesWritePos + 4;
            //                                    entriesWriter.WriteBytesUInt16(chunkNumber, false);

            //                                    entriesWriter.BaseStream.Position = entriesWritePos + 6;
            //                                    entriesWriter.WriteBytesUInt16(pathPos, false);

            //                                    outChunkWriter.Write(chunkData[2] + "\0");
            //                                    pathPos += (ushort)(chunkData[2] + "\0").Length;
            //                                    break;

            //                                case GameCodes.ff132:
            //                                    if (int16RangeValues.Contains(filelistVariables.ChunkFNameCount))
            //                                    {
            //                                        entriesWriter.BaseStream.Position = entriesWritePos + 4;
            //                                        entriesWriter.WriteBytesUInt16((ushort)(32768 + pathPos), false);
            //                                    }
            //                                    else
            //                                    {
            //                                        entriesWriter.BaseStream.Position = entriesWritePos + 4;
            //                                        entriesWriter.WriteBytesUInt16(pathPos, false);
            //                                    }

            //                                    var chunkNumByte = byte.Parse(chunkData[1]);
            //                                    var unkVal = byte.Parse(chunkData[2]);

            //                                    entriesWriter.BaseStream.Position = entriesWritePos + 6;
            //                                    entriesWriter.Write(chunkNumByte);

            //                                    entriesWriter.BaseStream.Position = entriesWritePos + 7;
            //                                    entriesWriter.Write(unkVal);

            //                                    outChunkWriter.Write(chunkData[3] + "\0");
            //                                    pathPos += (ushort)(chunkData[3] + "\0").Length;
            //                                    break;
            //                            }

            //                            entriesWritePos += 8;
            //                        }

            //                        if (filelistVariables.ChunkFNameCount == lastChunk)
            //                        {
            //                            outChunkWriter.Write("end\0");
            //                        }
            //                    }
            //                }
            //            }

            //            filelistVariables.ChunkFNameCount++;
            //        }
            //    }
            //}


            //filelistVariables.ChunkFNameCount = 0;
            //uint chunkStart = 0;
            //var chunksInfoWriterPos = encHeaderAdjustedOffset + 12 + (filelistVariables.TotalFiles * 8);
            //var chunksDataStartPos = encHeaderAdjustedOffset + 12 + (filelistVariables.TotalFiles * 8) + (filelistVariables.TotalChunks * 12);

            //using (var chunkDataStream = new FileStream(newFilelistFile, FileMode.Append, FileAccess.Write, FileShare.Write))
            //{
            //    using (var chunkInfoStream = new FileStream(newFilelistFile, FileMode.Open, FileAccess.Write, FileShare.Write))
            //    {
            //        using (var chunkInfoWriter = new BinaryWriter(chunkInfoStream))
            //        {

            //            chunkInfoWriter.BaseStream.Position = encHeaderAdjustedOffset;
            //            chunkInfoWriter.WriteBytesUInt32(chunksInfoWriterPos - encHeaderAdjustedOffset, false);

            //            chunkInfoWriter.BaseStream.Position = encHeaderAdjustedOffset + 4;
            //            chunkInfoWriter.WriteBytesUInt32(chunksDataStartPos - encHeaderAdjustedOffset, false);

            //            chunkInfoWriter.BaseStream.Position = encHeaderAdjustedOffset + 8;
            //            chunkInfoWriter.WriteBytesUInt32(filelistVariables.TotalFiles, false);


            //            for (int fc = 0; fc < filelistVariables.TotalChunks; fc++)
            //            {
            //                var currentChunkFile = Path.Combine(outChunksDir, $"Chunk_{filelistVariables.ChunkFNameCount}");
            //                var uncmpSize = (uint)new FileInfo(currentChunkFile).Length;

            //                var cmpChunkArray = currentChunkFile.ZlibCompress();
            //                var cmpSize = (uint)cmpChunkArray.Length;
            //                chunkDataStream.Write(cmpChunkArray, 0, cmpChunkArray.Length);

            //                chunkInfoWriter.BaseStream.Position = chunksInfoWriterPos;
            //                chunkInfoWriter.WriteBytesUInt32(uncmpSize, false);

            //                chunkInfoWriter.BaseStream.Position = chunksInfoWriterPos + 4;
            //                chunkInfoWriter.WriteBytesUInt32(cmpSize, false);

            //                chunkInfoWriter.BaseStream.Position = chunksInfoWriterPos + 8;
            //                chunkInfoWriter.WriteBytesUInt32(chunkStart, false);

            //                chunkStart += cmpSize;
            //                chunksInfoWriterPos += 12;
            //                filelistVariables.ChunkFNameCount++;
            //            }
            //        }
            //    }
            //}


            //outChunksDir.IfDirExistsDel();

            //var repackVariables = new RepackVariables();
            //repackVariables.NewFilelistFile = newFilelistFile;

            //if (filelistVariables.IsEncrypted)
            //{
            //    FilelistProcesses.EncryptProcess(repackVariables, logWriter);
            //}
        }
    }
}
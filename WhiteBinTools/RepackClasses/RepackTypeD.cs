using System.IO;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.RepackClasses;
using WhiteBinTools.SupportClasses;

namespace WhiteBinTools
{
    internal class RepackTypeD
    {
        public static void RepackFilelist(CmnEnums.GameCodes gameCodeVar, string filelistFileVar, string extractedFilelistDir, StreamWriter logWriter)
        {
            filelistFileVar.CheckFileExists(logWriter, "Error: Filelist file specified in the argument is missing");
            extractedFilelistDir.CheckDirExists(logWriter, "Error: Unpacked filelist directory specified in the argument is missing");
            var cryptTool = "ffxiiicrypt.exe";

            var filelistFileDir = Path.GetDirectoryName(filelistFileVar);
            var filelistFileName = Path.GetFileName(filelistFileVar);
            var outChunkFile = extractedFilelistDir + "\\Chunks\\Chunk_";
            var outEntriesFile = extractedFilelistDir + "\\Chunk_entries\\Chunk_";
            var encHeaderFile = extractedFilelistDir + "\\EncryptionHeader_(DON'T EDIT)";

            (extractedFilelistDir + "\\Chunks").CheckDirExists(logWriter, "Error: Unable to locate the unpacked 'Chunks' directory");
            (extractedFilelistDir + "\\Chunk_entries").CheckDirExists(logWriter, "Error: Unable to locate the unpacked 'Chunk_entries' directory");


            var filelistVariables = new FilelistProcesses();
            filelistVariables.TotalChunks = (uint)Directory.GetFiles(extractedFilelistDir + "\\Chunks", "*.txt", SearchOption.AllDirectories).Length;
            filelistVariables.IsEncrypted = FilelistProcesses.CheckIfEncrypted(filelistFileVar);
            uint encHeaderAdjustedOffset = 0;

            if (filelistVariables.IsEncrypted.Equals(true))
            {
                encHeaderAdjustedOffset += 32;
                encHeaderFile.CheckFileExists(logWriter, "Error: Unable to locate the 'EncryptionHeader_(DON'T EDIT)' file");
                cryptTool.CheckFileExists(logWriter, "Error: Unable to locate ffxiiicrypt tool in the main app folder to encrypt the filelist file");
            }

            var backupFilelistFile = Path.Combine(filelistFileDir, filelistFileName + ".bak");
            backupFilelistFile.IfFileExistsDel();
            File.Copy(filelistFileVar, backupFilelistFile);
            File.Delete(filelistFileVar);


            var repackVariables = new RepackProcesses();

            using (var newFilelist = new FileStream(filelistFileVar, FileMode.Append, FileAccess.Write))
            {
                if (filelistVariables.IsEncrypted.Equals(true))
                {
                    // Copy the encryption header file's data
                    // into the new filelist file
                    using (var encHeader = new FileStream(encHeaderFile, FileMode.Open, FileAccess.Read))
                    {
                        encHeader.Seek(0, SeekOrigin.Begin);
                        encHeader.CopyTo(newFilelist);
                    }
                }

                for (int b = 0; b < 12; b++)
                {
                    newFilelist.WriteByte(0);
                }

                filelistVariables.ChunkFNameCount = 0;
                for (int c = 0; c < filelistVariables.TotalChunks; c++)
                {
                    // Copy the entries file's data into
                    // the new filelist file
                    using (var entryFiles = new FileStream(outEntriesFile + filelistVariables.ChunkFNameCount + "_entries.bin", FileMode.Open, FileAccess.Read))
                    {
                        entryFiles.CopyTo(newFilelist);
                    }

                    repackVariables.LastChunkFileNumber = filelistVariables.ChunkFNameCount;

                    filelistVariables.ChunkFNameCount++;
                }

                filelistVariables.ChunkInfoSectionOffset = (uint)newFilelist.Length;
                if (filelistVariables.IsEncrypted.Equals(true))
                {
                    filelistVariables.ChunkInfoSectionOffset -= 32;
                }

                for (int b = 0; b < filelistVariables.TotalChunks * 12; b++)
                {
                    newFilelist.WriteByte(0);
                }
            }


            // Compress and pack chunks into the new filelist
            // file and update the info offsets.
            using (var newFilelistChunks = new FileStream(filelistFileVar, FileMode.Append, FileAccess.Write, FileShare.Write))
            {
                using (var newChunksInfoWriterStream = new FileStream(filelistFileVar, FileMode.Open, FileAccess.Write, FileShare.Write))
                {
                    using (var newChunksInfoWriter = new BinaryWriter(newChunksInfoWriterStream))
                    {

                        filelistVariables.ChunkFNameCount = 0;
                        var chunkInfoWriterPos = filelistVariables.ChunkInfoSectionOffset;
                        var chunkCmpSize = (uint)0;
                        var chunkUncmpSize = (uint)0;
                        var chunkStartVal = (uint)0;
                        var appendAt = (uint)0;
                        for (int nc = 0; nc < filelistVariables.TotalChunks; nc++)
                        {
                            appendAt = (uint)newFilelistChunks.Length;
                            newFilelistChunks.Seek(appendAt, SeekOrigin.Begin);

                            if (nc.Equals(0))
                            {
                                filelistVariables.ChunkDataSectionOffset = appendAt;
                                if (filelistVariables.IsEncrypted.Equals(true))
                                {
                                    filelistVariables.ChunkDataSectionOffset -= 32;
                                }
                            }

                            chunkUncmpSize = (uint)new FileInfo(outChunkFile + filelistVariables.ChunkFNameCount + ".txt").Length;
                            var chunkCmpData = (outChunkFile + filelistVariables.ChunkFNameCount + ".txt").ZlibCompress();
                            newFilelistChunks.Write(chunkCmpData, 0, chunkCmpData.Length);
                            chunkCmpSize = (uint)chunkCmpData.Length;

                            newChunksInfoWriter.AdjustBytesUInt32(encHeaderAdjustedOffset + chunkInfoWriterPos, chunkUncmpSize, CmnEnums.Endianness.LittleEndian);
                            newChunksInfoWriter.AdjustBytesUInt32(encHeaderAdjustedOffset + chunkInfoWriterPos + 4, chunkCmpSize, CmnEnums.Endianness.LittleEndian);
                            newChunksInfoWriter.AdjustBytesUInt32(encHeaderAdjustedOffset + chunkInfoWriterPos + 8, chunkStartVal, CmnEnums.Endianness.LittleEndian);

                            var newChunkStartVal = chunkStartVal + chunkCmpSize;
                            chunkStartVal = newChunkStartVal;

                            chunkInfoWriterPos += 12;

                            filelistVariables.ChunkFNameCount++;
                        }
                    }
                }
            }


            // Update the header offsets and 
            // the File Entry offsets
            repackVariables.NewFilelistFile = filelistFileVar;

            using (var updatedFilelist = new FileStream(filelistFileVar, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var updatedFilelistWriter = new BinaryWriter(updatedFilelist))
                {
                    using (var updatedFilelistReader = new BinaryReader(updatedFilelist))
                    {

                        updatedFilelistWriter.AdjustBytesUInt32(encHeaderAdjustedOffset + 0, filelistVariables.ChunkInfoSectionOffset, CmnEnums.Endianness.LittleEndian);
                        updatedFilelistWriter.AdjustBytesUInt32(encHeaderAdjustedOffset + 4, filelistVariables.ChunkDataSectionOffset, CmnEnums.Endianness.LittleEndian);
                        using (var sr = new StreamReader(extractedFilelistDir + "\\FileCount.txt"))
                        {
                            filelistVariables.TotalFiles = uint.Parse(sr.ReadLine());
                        }
                        updatedFilelistWriter.AdjustBytesUInt32(encHeaderAdjustedOffset + 8, filelistVariables.TotalFiles, CmnEnums.Endianness.LittleEndian);

                        filelistVariables.ChunkFNameCount = 0;
                        var fileInfoWriterPos = (uint)18;
                        if (gameCodeVar.Equals(CmnEnums.GameCodes.ff132))
                        {
                            // Change fileInfo writer position
                            // according to the game code.
                            fileInfoWriterPos = 16;

                            // If encrypted, increase the 
                            // position to factor in the
                            // encryption header.
                            if (filelistVariables.IsEncrypted.Equals(true))
                            {
                                fileInfoWriterPos += 32;
                            }
                        }
                        for (int ncf = 0; ncf < filelistVariables.TotalChunks; ncf++)
                        {
                            var filesInNewChunkCount = FilelistProcesses.GetFilesInChunkCount(outChunkFile + filelistVariables.ChunkFNameCount + ".txt");
                            if (repackVariables.LastChunkFileNumber.Equals(filelistVariables.ChunkFNameCount))
                            {
                                filesInNewChunkCount--;
                            }

                            using (var chunkStream = new FileStream(outChunkFile + filelistVariables.ChunkFNameCount + ".txt", FileMode.Open, FileAccess.Read))
                            {
                                using (var chunkReader = new BinaryReader(chunkStream))
                                {

                                    var filePosInChunk = (ushort)0;
                                    var filePosInChunkToWrite = (ushort)0;
                                    for (int fic = 0; fic < filesInNewChunkCount; fic++)
                                    {
                                        // According to the game code, check how to
                                        // write the value and then set the appropriate
                                        // converted value to write.
                                        if (gameCodeVar.Equals(CmnEnums.GameCodes.ff132))
                                        {
                                            updatedFilelistReader.BaseStream.Position = fileInfoWriterPos;
                                            var checkVal = updatedFilelistReader.ReadUInt16();

                                            if (checkVal > 32767)
                                            {
                                                filePosInChunkToWrite = (ushort)(filePosInChunkToWrite + 32768);
                                            }
                                        }

                                        updatedFilelistWriter.AdjustBytesUInt16(fileInfoWriterPos, filePosInChunkToWrite);

                                        var readString = chunkReader.BinaryToString(filePosInChunk);

                                        filePosInChunk = (ushort)chunkReader.BaseStream.Position;
                                        filePosInChunkToWrite = (ushort)chunkReader.BaseStream.Position;
                                        fileInfoWriterPos += 8;
                                    }
                                }
                            }

                            filelistVariables.ChunkFNameCount++;
                        }
                    }
                }
            }

            if (filelistVariables.IsEncrypted.Equals(true))
            {
                FilelistProcesses.EncryptProcess(repackVariables, logWriter);
            }

            IOhelpers.LogMessage("\nFinished repacking " + filelistFileName, logWriter);
        }
    }
}
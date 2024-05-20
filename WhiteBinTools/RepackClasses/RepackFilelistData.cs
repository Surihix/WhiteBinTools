using System.Collections.Generic;
using System.IO;
using System.Text;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.SupportClasses;
using static WhiteBinTools.SupportClasses.ProgramEnums;

namespace WhiteBinTools.RepackClasses
{
    internal class RepackFilelistData
    {
        public static void BuildFilelist(FilelistVariables filelistVariables, Dictionary<int, List<byte>> newChunksDict, RepackVariables repackVariables, GameCodes gameCode)
        {
            // Add 'end' string to the last chunk
            // in the dictionary
            newChunksDict[filelistVariables.LastChunkNumber].AddRange(Encoding.UTF8.GetBytes("end\0"));

            // Update chunk info offsets and
            // compress chunks in two streams
            // Then copy these two streams
            // into the new filelist stream
            uint chunkInfoOffset = 12;
            uint chunkDataOffset = 12;

            using (var chunkInfoStream = new MemoryStream())
            {
                using (var chunkInfoWriter = new BinaryWriter(chunkInfoStream))
                {
                    using (var chunkDataStream = new MemoryStream())
                    {
                        long chunkInfoWritePos = 0;
                        uint chunkUncmpSize = 0;
                        uint chunkCmpSize = 0;
                        uint chunkStart = 0;

                        for (int c = 0; c < filelistVariables.TotalChunks; c++)
                        {
                            // Get info offsets
                            var currentChunk = newChunksDict[c].ToArray();
                            chunkUncmpSize = (uint)currentChunk.Length;

                            var currentChunkCmp = currentChunk.ZlibCompressBuffer();
                            chunkCmpSize = (uint)currentChunkCmp.Length;

                            chunkStart = (uint)chunkDataStream.Length;

                            // Write chunk data to
                            // the stream
                            chunkDataStream.Seek(chunkStart, SeekOrigin.Begin);
                            chunkDataStream.Write(currentChunkCmp, 0, currentChunkCmp.Length);

                            // Write the info offsets
                            chunkInfoWriter.BaseStream.Position = chunkInfoWritePos;
                            chunkInfoWriter.WriteBytesUInt32(chunkUncmpSize, false);

                            chunkInfoWriter.BaseStream.Position = chunkInfoWritePos + 4;
                            chunkInfoWriter.WriteBytesUInt32(chunkCmpSize, false);

                            chunkInfoWriter.BaseStream.Position = chunkInfoWritePos + 8;
                            chunkInfoWriter.WriteBytesUInt32(chunkStart, false);

                            chunkInfoWritePos += 12;
                        }

                        // Update the chunkInfoOffset and 
                        // chunkData offset values
                        chunkInfoOffset += (uint)filelistVariables.EntriesData.Length;
                        chunkDataOffset += (uint)filelistVariables.EntriesData.Length + (uint)chunkInfoStream.Length;

                        using (var newFilelistChunks = new FileStream(repackVariables.NewFilelistFile, FileMode.Append, FileAccess.Write))
                        {
                            using (var newFilelistChunksWriter = new BinaryWriter(newFilelistChunks))
                            {
                                if (filelistVariables.IsEncrypted)
                                {
                                    newFilelistChunks.Write(filelistVariables.EncryptedHeaderData, 0, 32);
                                }

                                // Write the header values
                                newFilelistChunksWriter.WriteBytesUInt32(chunkInfoOffset, false);
                                newFilelistChunksWriter.WriteBytesUInt32(chunkDataOffset, false);
                                newFilelistChunksWriter.WriteBytesUInt32(filelistVariables.TotalFiles, false);

                                // Copy the entries data
                                newFilelistChunks.Write(filelistVariables.EntriesData, 0, filelistVariables.EntriesData.Length);

                                // Copy the chunk info and data
                                // streams
                                chunkInfoStream.Seek(0, SeekOrigin.Begin);
                                chunkInfoStream.CopyStreamTo(newFilelistChunks, chunkInfoStream.Length, false);

                                chunkDataStream.Seek(0, SeekOrigin.Begin);
                                chunkDataStream.CopyStreamTo(newFilelistChunks, chunkDataStream.Length, false);
                            }
                        }
                    }
                }
            }


            // Update the file entry section
            using (var tmpEntriesStream = new MemoryStream())
            {
                tmpEntriesStream.Write(filelistVariables.EntriesData, 0, filelistVariables.EntriesData.Length);
                tmpEntriesStream.Seek(0, SeekOrigin.Begin);

                using (var tmpEntriesReader = new BinaryReader(tmpEntriesStream))
                {
                    using (var newEntryWriter = new BinaryWriter(File.Open(repackVariables.NewFilelistFile, FileMode.Open, FileAccess.Write)))
                    {
                        long tmpEntryReaderPos = 0;
                        long newEntryWriterPos = 12;

                        if (filelistVariables.IsEncrypted)
                        {
                            newEntryWriterPos += 32;
                        }


                        // Start updating the entries
                        uint fileEntriesProcessed = 0;
                        for (int c = 0; c < filelistVariables.TotalChunks; c++)
                        {
                            var currentChunkData = newChunksDict[c];
                            var currentChunkLength = currentChunkData.Count;
                            int currentStringPos = 0;
                            ushort posInChunkVal = 0;
                            long fixedEntryWriterPos = newEntryWriterPos;

                            // Process the file paths 
                            // in a chunk
                            while (true)
                            {
                                fixedEntryWriterPos = newEntryWriterPos + 6;

                                if (currentStringPos >= currentChunkLength)
                                {
                                    break;
                                }

                                // Adjust path position value
                                // when code is set to ff13-2
                                if (gameCode.Equals(GameCodes.ff132))
                                {
                                    fixedEntryWriterPos -= 2;

                                    tmpEntriesReader.BaseStream.Position = tmpEntryReaderPos + 4;
                                    tmpEntryReaderPos += 8;

                                    if (tmpEntriesReader.ReadUInt16() > 32767)
                                    {
                                        posInChunkVal += 32768;
                                    }
                                }

                                // Write the string's position
                                newEntryWriter.BaseStream.Position = fixedEntryWriterPos;
                                newEntryWriter.WriteBytesUInt16(posInChunkVal, false);
                                newEntryWriterPos += 8;
                                fileEntriesProcessed++;

                                if (fileEntriesProcessed >= filelistVariables.TotalFiles)
                                {
                                    break;
                                }

                                for (int i = currentStringPos; i < currentChunkLength; i++)
                                {
                                    if (currentChunkData[i] == 0)
                                    {
                                        currentStringPos = i + 1;
                                        posInChunkVal = (ushort)currentStringPos;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        public static void CreateFilelist(FilelistVariables filelistVariables, RepackVariables repackVariables, GameCodes gameCode)
        {
            // Create a copy of the filelist that is being used.
            // The copying and renaming is done for unencrypted
            // filelists as both the filelist and the new filelist 
            // would have the same name if not renamed.
            File.Copy(filelistVariables.MainFilelistFile, filelistVariables.MainFilelistFile + ".old");
            File.Delete(filelistVariables.MainFilelistFile);


            // Copy the base filelist file's data into the
            // new filelist file till the chunk data begins.
            using (var oldFilelistBase = new FileStream(filelistVariables.MainFilelistFile + ".old", FileMode.Open, FileAccess.Read))
            {
                using (var newFilelistBase = new FileStream(repackVariables.NewFilelistFile, FileMode.Append, FileAccess.Write))
                {
                    oldFilelistBase.Seek(0, SeekOrigin.Begin);
                    oldFilelistBase.CopyStreamTo(newFilelistBase, filelistVariables.ChunkDataSectionOffset, false);
                }
            }


            // Compress and pack chunks into the new filelist
            // file and update the info offsets.
            using (var newFilelistChunks = new FileStream(repackVariables.NewFilelistFile, FileMode.Append, FileAccess.Write, FileShare.Write))
            {
                using (var newChunksInfoWriterStream = new FileStream(repackVariables.NewFilelistFile, FileMode.Open, FileAccess.Write, FileShare.Write))
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

                            chunkUncmpSize = (uint)new FileInfo(repackVariables.NewChunkFile + filelistVariables.ChunkFNameCount).Length;
                            var chunkCmpData = (repackVariables.NewChunkFile + filelistVariables.ChunkFNameCount).ZlibCompress();
                            newFilelistChunks.Write(chunkCmpData, 0, chunkCmpData.Length);
                            chunkCmpSize = (uint)chunkCmpData.Length;

                            newChunksInfoWriter.BaseStream.Position = chunkInfoWriterPos;
                            newChunksInfoWriter.WriteBytesUInt32(chunkUncmpSize, false);

                            newChunksInfoWriter.BaseStream.Position = chunkInfoWriterPos + 4;
                            newChunksInfoWriter.WriteBytesUInt32(chunkCmpSize, false);

                            newChunksInfoWriter.BaseStream.Position = chunkInfoWriterPos + 8;
                            newChunksInfoWriter.WriteBytesUInt32(chunkStartVal, false);

                            var newChunkStartVal = chunkStartVal + chunkCmpSize;
                            chunkStartVal = newChunkStartVal;

                            chunkInfoWriterPos += 12;

                            filelistVariables.ChunkFNameCount++;
                        }
                    }
                }
            }


            // Update each file path position in the chunk, in
            // the new filelist's file info offsets.
            using (var oldFilelistFileInfo = new FileStream(filelistVariables.MainFilelistFile + ".old", FileMode.Open, FileAccess.Read))
            {
                using (var oldFileInfoReader = new BinaryReader(oldFilelistFileInfo))
                {
                    using (var newFilelistFileInfo = new FileStream(repackVariables.NewFilelistFile, FileMode.Open, FileAccess.Write))
                    {
                        using (var newFileInfoWriter = new BinaryWriter(newFilelistFileInfo))
                        {

                            filelistVariables.ChunkFNameCount = 0;
                            var fileInfoWriterPos = (uint)18;
                            if (gameCode.Equals(GameCodes.ff132))
                            {
                                // Change fileInfo writer position
                                // according to the game code.
                                fileInfoWriterPos = 16;

                                // If encrypted, increase the 
                                // position to factor in the
                                // encryption header.
                                if (filelistVariables.IsEncrypted)
                                {
                                    fileInfoWriterPos += 32;
                                }
                            }
                            for (int ncf = 0; ncf < filelistVariables.TotalChunks; ncf++)
                            {
                                var filesInNewChunkCount = FilelistProcesses.GetFilesInChunkCount(repackVariables.NewChunkFile + filelistVariables.ChunkFNameCount);
                                if (repackVariables.HasEndString && repackVariables.LastChunkFileNumber == filelistVariables.ChunkFNameCount)
                                {
                                    filesInNewChunkCount--;
                                }

                                using (var newChunkStream = new FileStream(repackVariables.NewChunkFile + filelistVariables.ChunkFNameCount, FileMode.Open, FileAccess.Read))
                                {
                                    using (var newChunkReader = new BinaryReader(newChunkStream))
                                    {

                                        var filePosInChunk = (ushort)0;
                                        var filePosInChunkToWrite = (ushort)0;
                                        for (int fic = 0; fic < filesInNewChunkCount; fic++)
                                        {
                                            // According to the game code, check how to
                                            // write the value and then set the appropriate
                                            // converted value to write.
                                            if (gameCode.Equals(GameCodes.ff132))
                                            {
                                                oldFileInfoReader.BaseStream.Position = fileInfoWriterPos;
                                                var checkVal = oldFileInfoReader.ReadUInt16();

                                                if (checkVal > 32767)
                                                {
                                                    filePosInChunkToWrite = (ushort)(filePosInChunkToWrite + 32768);
                                                }
                                            }

                                            newFileInfoWriter.BaseStream.Position = fileInfoWriterPos;
                                            newFileInfoWriter.WriteBytesUInt16(filePosInChunkToWrite, false);

                                            newChunkReader.BaseStream.Position = filePosInChunk;
                                            var readString = newChunkReader.ReadStringTillNull();

                                            filePosInChunk = (ushort)newChunkReader.BaseStream.Position;
                                            filePosInChunkToWrite = (ushort)newChunkReader.BaseStream.Position;
                                            fileInfoWriterPos += 8;
                                        }
                                    }
                                }

                                filelistVariables.ChunkFNameCount++;
                            }
                        }
                    }
                }
            }


            File.Delete(filelistVariables.MainFilelistFile + ".old");

            repackVariables.NewChunksExtDir.IfDirExistsDel();
        }
    }
}
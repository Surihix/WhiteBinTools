using System.IO;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.SupportClasses;
using static WhiteBinTools.SupportClasses.ProgramEnums;

namespace WhiteBinTools.RepackClasses
{
    internal class RepackFilelist
    {
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
                    oldFilelistBase.ExCopyTo(newFilelistBase, 0, filelistVariables.ChunkDataSectionOffset);
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

                            newChunksInfoWriter.ExWriteBytesUInt32(chunkInfoWriterPos, chunkUncmpSize, Endianness.LittleEndian);
                            newChunksInfoWriter.ExWriteBytesUInt32(chunkInfoWriterPos + 4, chunkCmpSize, Endianness.LittleEndian);
                            newChunksInfoWriter.ExWriteBytesUInt32(chunkInfoWriterPos + 8, chunkStartVal, Endianness.LittleEndian);

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
                                if (repackVariables.LastChunkFileNumber == filelistVariables.ChunkFNameCount)
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

                                            newFileInfoWriter.ExWriteBytesUInt16(fileInfoWriterPos, filePosInChunkToWrite);

                                            var readString = newChunkReader.BinaryToString(filePosInChunk);

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
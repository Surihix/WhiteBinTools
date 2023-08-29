using System;
using System.IO;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.SupportClasses;

namespace WhiteBinTools.RepackClasses
{
    internal partial class RepackProcesses
    {
        public static void CreateFilelist(FilelistProcesses filelistVariables, RepackProcesses repackVariables, CmnEnums.GameCodes gameCodeVar)
        {
            // Create a copy of the filelist that is being used.
            // The copying and renaming is done for unencrypted
            // filelists as both the filelist and the new filelist 
            // would have the same name if not renamed
            File.Copy(filelistVariables.MainFilelistFile, filelistVariables.MainFilelistFile + ".old");
            File.Delete(filelistVariables.MainFilelistFile);


            using (var oldFilelist = new FileStream(filelistVariables.MainFilelistFile + ".old", FileMode.Open, FileAccess.Read))
            {
                using (var oldFilelistReader = new BinaryReader(oldFilelist))
                {

                    // Fileinfo updating and chunk compression section
                    // Copy the base filelist file's data into the new filelist file till the chunk data begins
                    var appendAt = (uint)0;
                    using (var newFilelist = new FileStream(repackVariables.NewFilelistFile, FileMode.Append, FileAccess.Write, FileShare.Write))
                    {
                        using (var newFilelistToWrite = new FileStream(repackVariables.NewFilelistFile, FileMode.Open, FileAccess.Write, FileShare.Write))
                        {
                            using (var newFilelistWriter = new BinaryWriter(newFilelistToWrite))
                            {
                                oldFilelist.ExtendedCopyTo(newFilelist, 0, filelistVariables.ChunkDataSectionOffset);


                                filelistVariables.ChunkFNameCount = 0;
                                var chunkInfoWriterPos = filelistVariables.ChunkInfoSectionOffset;
                                var chunkCmpSize = (uint)0;
                                var chunkUncmpSize = (uint)0;
                                var chunkStartVal = (uint)0;
                                var fileInfoWriterPos = (uint)18;
                                if (gameCodeVar.Equals(CmnEnums.GameCodes.ff132))
                                {
                                    // Change Fileinfo writer position
                                    // according to the game code
                                    fileInfoWriterPos = 16;

                                    // If encrypted, increase the 
                                    // position to factor in the
                                    // encryption header
                                    if (filelistVariables.IsEncrypted.Equals(true))
                                    {
                                        fileInfoWriterPos += 32;
                                    }
                                }

                                for (int nc = 0; nc < filelistVariables.TotalChunks; nc++)
                                {
                                    var filesInNewChunkCount = FilelistProcesses.GetFilesInChunkCount(repackVariables.NewChunkFile + filelistVariables.ChunkFNameCount);

                                    if (repackVariables.LastChunkFileNumber.Equals(filelistVariables.ChunkFNameCount))
                                    {
                                        filesInNewChunkCount--;
                                    }

                                    using (var newChunkStream = new FileStream(repackVariables.NewChunkFile + filelistVariables.ChunkFNameCount, FileMode.Open, FileAccess.Read))
                                    {
                                        using (var newChunkReader = new BinaryReader(newChunkStream))
                                        {

                                            var filePosInChunk = (UInt16)0;
                                            var filePosInChunkToWrite = (UInt16)0;
                                            for (int fic = 0; fic < filesInNewChunkCount; fic++)
                                            {
                                                // According to the game code, check how to
                                                // write the value and then set the appropriate
                                                // converted value to write
                                                if (gameCodeVar.Equals(CmnEnums.GameCodes.ff132))
                                                {
                                                    oldFilelistReader.BaseStream.Position = fileInfoWriterPos;
                                                    var checkVal = oldFilelistReader.ReadUInt16();

                                                    if (checkVal > 32767)
                                                    {
                                                        filePosInChunkToWrite = (ushort)(filePosInChunkToWrite + 32768);
                                                    }
                                                }

                                                newFilelistWriter.AdjustBytesUInt16(fileInfoWriterPos, filePosInChunkToWrite, CmnEnums.Endianness.LittleEndian);

                                                var readString = newChunkReader.BinaryToString(filePosInChunk);

                                                filePosInChunk = (UInt16)newChunkReader.BaseStream.Position;
                                                filePosInChunkToWrite = (UInt16)newChunkReader.BaseStream.Position;
                                                fileInfoWriterPos += 8;
                                            }
                                        }
                                    }


                                    // Compress and package a chunk back into the new filelist file and update the 
                                    // offsets in the chunk info section of the filelist file
                                    appendAt = (uint)newFilelist.Length;
                                    newFilelist.Seek(appendAt, SeekOrigin.Begin);

                                    chunkUncmpSize = (uint)new FileInfo(repackVariables.NewChunkFile + filelistVariables.ChunkFNameCount).Length;

                                    repackVariables.TmpCmpChunkDataFile = repackVariables.NewChunksExtDir + "zlib_chunk";
                                    var createChunkFile = File.Create(repackVariables.TmpCmpChunkDataFile);
                                    createChunkFile.Close();

                                    (repackVariables.NewChunkFile + filelistVariables.ChunkFNameCount).ZlibCompress(repackVariables.TmpCmpChunkDataFile, Ionic.Zlib.CompressionLevel.Level9);

                                    using (var cmpChunkStream = new FileStream(repackVariables.TmpCmpChunkDataFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                                    {
                                        cmpChunkStream.Seek(0, SeekOrigin.Begin);
                                        cmpChunkStream.CopyTo(newFilelist);

                                        chunkCmpSize = (uint)new FileInfo(repackVariables.TmpCmpChunkDataFile).Length;
                                    }
                                    File.Delete(repackVariables.TmpCmpChunkDataFile);

                                    newFilelistWriter.AdjustBytesUInt32(chunkInfoWriterPos, chunkUncmpSize, CmnEnums.Endianness.LittleEndian);
                                    newFilelistWriter.AdjustBytesUInt32(chunkInfoWriterPos + 4, chunkCmpSize, CmnEnums.Endianness.LittleEndian);
                                    newFilelistWriter.AdjustBytesUInt32(chunkInfoWriterPos + 8, chunkStartVal, CmnEnums.Endianness.LittleEndian);

                                    var newChunkStartVal = chunkStartVal + chunkCmpSize;
                                    chunkStartVal = newChunkStartVal;

                                    chunkInfoWriterPos += 12;

                                    filelistVariables.ChunkFNameCount++;
                                }
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
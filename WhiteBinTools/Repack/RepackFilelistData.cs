using System.Collections.Generic;
using System.IO;
using System.Text;
using WhiteBinTools.Filelist;
using WhiteBinTools.Support;
using static WhiteBinTools.Support.ProgramEnums;

namespace WhiteBinTools.Repack
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

                            var currentChunkCmp = ZlibMethods.ZlibCompressBuffer(currentChunk);
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
                                if (gameCode == GameCodes.ff132)
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
    }
}
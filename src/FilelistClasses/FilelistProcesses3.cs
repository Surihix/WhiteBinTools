using System.IO;
using WhiteBinTools.SupportClasses;

namespace WhiteBinTools.FilelistClasses
{
    internal partial class FilelistProcesses
    {
        public static void GetFilelistOffsets(BinaryReader filelistReader, StreamWriter logWriter, FilelistProcesses filelistVariables, CmnEnums.GameCodes gameCodeVar)
        {
            var readStartPositionVar = new uint();
            var adjustOffset = new uint();

            switch (filelistVariables.IsEncrypted)
            {
                case true:
                    readStartPositionVar = 32;
                    adjustOffset = 32;
                    break;

                case false:
                    readStartPositionVar = 0;
                    adjustOffset = 0;
                    break;
            }

            filelistReader.BaseStream.Position = readStartPositionVar;
            filelistVariables.ChunkInfoSectionOffset = filelistReader.ReadUInt32() + adjustOffset;
            filelistVariables.ChunkDataSectionOffset = filelistReader.ReadUInt32() + adjustOffset;
            filelistVariables.TotalFiles = filelistReader.ReadUInt32();

            filelistVariables.ChunkInfoSize = filelistVariables.ChunkDataSectionOffset - filelistVariables.ChunkInfoSectionOffset;
            filelistVariables.TotalChunks = filelistVariables.ChunkInfoSize / 12;

            IOhelpers.LogMessage("TotalChunks: " + filelistVariables.TotalChunks, logWriter);
            IOhelpers.LogMessage("No of files: " + filelistVariables.TotalFiles + "\n", logWriter);
        }


        public static void UnpackChunks(FileStream filelist, string chunkFile, FilelistProcesses filelistVariables)
        {
            // Make a memorystream for holding all Chunks info
            using (var chunkInfoStream = new MemoryStream())
            {
                filelist.Seek(filelistVariables.ChunkInfoSectionOffset, SeekOrigin.Begin);
                var chunkInfoBuffer = new byte[filelistVariables.ChunkInfoSize];
                filelist.Read(chunkInfoBuffer, 0, chunkInfoBuffer.Length);
                chunkInfoStream.Write(chunkInfoBuffer, 0, chunkInfoBuffer.Length);

                // Make memorystream for all Chunks compressed data
                using (var chunkStream = new MemoryStream())
                {
                    filelist.Seek(filelistVariables.ChunkDataSectionOffset, SeekOrigin.Begin);
                    filelist.CopyTo(chunkStream);

                    // Open a binary reader and read each chunk's info and
                    // dump them as separate files
                    using (var chunkInfoReader = new BinaryReader(chunkInfoStream))
                    {

                        var chunkInfoReadVal = (uint)0;
                        for (int c = 0; c < filelistVariables.TotalChunks; c++)
                        {
                            chunkInfoReader.BaseStream.Position = chunkInfoReadVal + 4;
                            filelistVariables.ChunkCmpSize = chunkInfoReader.ReadUInt32();
                            filelistVariables.ChunkStartOffset = chunkInfoReader.ReadUInt32();

                            chunkStream.Seek(filelistVariables.ChunkStartOffset, SeekOrigin.Begin);
                            using (var chunkToDcmp = new MemoryStream())
                            {
                                var chunkBuffer = new byte[filelistVariables.ChunkCmpSize];
                                var readCmpBytes = chunkStream.Read(chunkBuffer, 0, chunkBuffer.Length);
                                chunkToDcmp.Write(chunkBuffer, 0, readCmpBytes);


                                using (var chunksOutStream = new FileStream(chunkFile + filelistVariables.ChunkFNameCount, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                                {
                                    chunkToDcmp.Seek(0, SeekOrigin.Begin);
                                    chunkToDcmp.ZlibDecompress(chunksOutStream);
                                }
                            }

                            chunkInfoReadVal += 12;
                            filelistVariables.ChunkFNameCount++;
                        }
                    }
                }
            }
        }
    }
}
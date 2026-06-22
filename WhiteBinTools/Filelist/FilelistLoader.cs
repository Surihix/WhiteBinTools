using System;
using System.IO;
using System.Text;
using WhiteBinTools.Filelist.DirgePathHelpers;
using WhiteBinTools.Support;
using WhiteBinTools.Support.Structures;
using static WhiteBinTools.Support.Enumerators;

namespace WhiteBinTools.Filelist
{
    internal class FilelistLoader
    {
        private static uint filelistBaseOffset = 0;

        public static FilelistLoadData LoadFilelist(GameCode gameCode, string filelistFile, StreamWriter logWriter)
        {
            var filelistLoadData = new FilelistLoadData();

            var filelistCryptHeader = new FilelistCryptHeader();
            var hasDecrypted = FilelistCrypto.DecryptProcess(gameCode, ref filelistFile, filelistCryptHeader, logWriter);
            filelistLoadData.FilelistCryptHeader = filelistCryptHeader;

            if (filelistCryptHeader.HasCryptHeader)
            {
                filelistBaseOffset = 32;
            }

            var filelistHeader = new FilelistHeader();

            using (var filelistReader = new BinaryReader(File.Open(filelistFile, FileMode.Open, FileAccess.Read)))
            {
                _ = filelistReader.BaseStream.Position = filelistBaseOffset;

                filelistHeader.ChunkInfoTableOffset = filelistReader.ReadUInt32();
                filelistHeader.ChunkDataStartOffset = filelistReader.ReadUInt32();
                filelistHeader.FileCount = filelistReader.ReadUInt32();
                filelistLoadData.FilelistHeader = filelistHeader;

                logWriter.LogMessage($"No of files: {filelistHeader.FileCount}");
                logWriter.LogMessage($"Loading entries....\n");

                var filelistEntryV1Table = new FilelistEntryV1[filelistHeader.FileCount];
                var filelistEntryV2Table = new FilelistEntryV2[filelistHeader.FileCount];
                LoadEntryTable(filelistHeader, gameCode, filelistEntryV1Table, filelistEntryV2Table, filelistReader);

                filelistLoadData.FilelistEntryV1Table = filelistEntryV1Table;
                filelistLoadData.FilelistEntryV2Table = filelistEntryV2Table;

                logWriter.LogMessage($"TotalChunks: {filelistHeader.ChunkCount}");
                logWriter.LogMessage($"Loading chunks....\n");

                var filelistChunks = new FilelistChunk[filelistHeader.ChunkCount];
                LoadPathChunks(filelistReader, filelistHeader, filelistChunks);

                filelistLoadData.FilelistChunks = filelistChunks;
            }

            if (hasDecrypted)
            {
                SharedFunctions.IfFileExistsDel(filelistFile);
            }

            return filelistLoadData;
        }

        private static void LoadEntryTable(FilelistHeader filelistHeader, GameCode gameCode, FilelistEntryV1[] filelistEntryV1Table, FilelistEntryV2[] filelistEntryV2Table, BinaryReader filelistReader)
        {
            for (int i = 0; i < filelistHeader.FileCount; i++)
            {
                if (gameCode == GameCode.ff131 || gameCode == GameCode.dirge)
                {
                    var filelistEntryV1 = new FilelistEntryV1()
                    {
                        FileCode = filelistReader.ReadUInt32(),
                        ChunkID = filelistReader.ReadUInt16(),
                        FileInfoPos = filelistReader.ReadUInt16()
                    };

                    filelistEntryV1Table[i] = filelistEntryV1;
                    filelistHeader.ChunkCount = filelistEntryV1.ChunkID;
                }
                else
                {
                    var filelistEntryV2 = new FilelistEntryV2()
                    {
                        FileCode = filelistReader.ReadUInt32(),
                        FileInfoPos = filelistReader.ReadUInt16(),
                        ChunkID = filelistReader.ReadByte(),
                        FileTypeID = filelistReader.ReadByte()
                    };

                    filelistEntryV2.ChunkID = (ushort)((filelistEntryV2.ChunkID << 1) | (filelistEntryV2.FileInfoPos >> 15));
                    filelistEntryV2.FileInfoPos = (ushort)(short.MaxValue & filelistEntryV2.FileInfoPos);

                    filelistEntryV2Table[i] = filelistEntryV2;
                    filelistHeader.ChunkCount = filelistEntryV2.ChunkID;
                }
            }

            filelistHeader.ChunkCount += 1;
        }

        private static void LoadPathChunks(BinaryReader filelistReader, FilelistHeader filelistHeader, FilelistChunk[] filelistChunks)
        {
            var lastPos = filelistReader.BaseStream.Position = filelistBaseOffset + filelistHeader.ChunkInfoTableOffset;

            for (int i = 0; i < filelistHeader.ChunkCount; i++)
            {
                _ = filelistReader.BaseStream.Position = lastPos;

                var filelistPathChunkInfo = new FilelistPathChunkInfo()
                {
                    UncmpSize = filelistReader.ReadUInt32(),
                    CmpSize = filelistReader.ReadUInt32(),
                    ChunkPosition = filelistReader.ReadUInt32()
                };

                _ = filelistReader.BaseStream.Position = filelistBaseOffset + filelistHeader.ChunkDataStartOffset + filelistPathChunkInfo.ChunkPosition;
                var cmpChunkData = filelistReader.ReadBytes((int)filelistPathChunkInfo.CmpSize);

                filelistChunks[i] = new FilelistChunk() { ChunkSize = filelistPathChunkInfo.UncmpSize, ChunkData = ZlibFunctions.ZlibDecompressBuffer(cmpChunkData) };
                lastPos += 12;
            }
        }

        public static string GetWhiteFileInfoString(ushort fileInfoPos, FilelistChunk[] filelistChunks, int chunkID)
        {
            var length = 0;

            for (int i = fileInfoPos; i < filelistChunks[chunkID].ChunkSize && filelistChunks[chunkID].ChunkData[i] != 0; i++)
            {
                length++;
            }

            return Encoding.ASCII.GetString(filelistChunks[chunkID].ChunkData, fileInfoPos, length);
        }

        public static WhiteFileInfoData GetWhiteFileInfoData(string fileInfoString, GameCode gameCode, uint fileCode, ref int noPathCounter)
        {
            var whiteFileInfoData = new WhiteFileInfoData();

            var fileInfoStringData = fileInfoString.Split(':');

            if (fileInfoStringData.Length < 4)
            {
                SharedFunctions.ErrorExit($"Error: Missing fileinfo data in path '{fileInfoString}'!");
            }

            whiteFileInfoData.FilePosition = Convert.ToInt64(fileInfoStringData[0], 16) * 2048;
            whiteFileInfoData.UncmpSize = Convert.ToUInt32(fileInfoStringData[1], 16);
            whiteFileInfoData.CmpSize = Convert.ToUInt32(fileInfoStringData[2], 16);
            whiteFileInfoData.FilePath = fileInfoStringData[3];
            whiteFileInfoData.IsPathGenerated = false;

            if (whiteFileInfoData.FilePath == " ")
            {
                if (gameCode == GameCode.dirge)
                {
                    whiteFileInfoData.FilePath = PathGenerator.GenerateDirgePath(fileCode);
                    whiteFileInfoData.IsPathGenerated = true;

                    if (whiteFileInfoData.FilePath == "")
                    {
                        GenerateNoPath(whiteFileInfoData, ref noPathCounter);
                        whiteFileInfoData.IsPathGenerated = false;
                    }
                }
                else
                {
                    GenerateNoPath(whiteFileInfoData, ref noPathCounter);
                }
            }

            if (whiteFileInfoData.UncmpSize == whiteFileInfoData.CmpSize)
            {
                whiteFileInfoData.IsCompressed = false;
            }
            else
            {
                whiteFileInfoData.IsCompressed = true;
            }

            return whiteFileInfoData;
        }

        private static void GenerateNoPath(WhiteFileInfoData fileInfoData, ref int noPathCounter)
        {
            fileInfoData.FilePath = $"noPaths/FILE_{noPathCounter}";
            noPathCounter++;
        }
    }
}
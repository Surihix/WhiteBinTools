using System;
using System.IO;
using System.Text;
using WhiteBinTools.Support;
using WhiteBinTools.Support.Structures;
using static WhiteBinTools.Support.Enumerators;

namespace WhiteBinTools.Filelist
{
    internal class FilelistBuilder
    {
        public static void BuildFilelist(FilelistCryptHeader filelistCryptHeader, FilelistHeader filelistHeader, GameCode gameCode, FilelistEntryV1[] newEntryV1Table, FilelistEntryV2[] newEntryV2Table, FileInfoStringPack[] fileInfoStringPackTable, string newFilelistFile)
        {
            var cryptHeaderData = Array.Empty<byte>();

            if (filelistCryptHeader.HasCryptHeader)
            {
                cryptHeaderData = BuildCryptHeader(filelistCryptHeader);
            }

            var headerData = BuildHeader(filelistHeader);
            var chunkPackTable = BuildChunkPackTable(filelistHeader.ChunkCount, filelistHeader.FileCount, gameCode, newEntryV1Table, newEntryV2Table, fileInfoStringPackTable);
            var entriesData = BuildEntryTable(filelistHeader.FileCount, gameCode, newEntryV1Table, newEntryV2Table, chunkPackTable);
            var pathChunksAndInfoData = BuildPathChunksAndInfo(filelistHeader.ChunkCount, chunkPackTable);

            SharedFunctions.IfFileExistsDel(newFilelistFile);

            using (var newFilelistStream = new FileStream(newFilelistFile, FileMode.Append, FileAccess.Write))
            {
                if (filelistCryptHeader.HasCryptHeader)
                {
                    newFilelistStream.Write(cryptHeaderData, 0, 32);
                }

                newFilelistStream.Write(headerData, 0, headerData.Length);
                newFilelistStream.Write(entriesData, 0, entriesData.Length);
                newFilelistStream.Write(pathChunksAndInfoData, 0, pathChunksAndInfoData.Length);
            }
        }

        private static byte[] BuildCryptHeader(FilelistCryptHeader filelistCryptHeader)
        {
            var cryptHeaderData = new byte[32];

            if (filelistCryptHeader.HasCryptHeader)
            {
                using (var cryptHeaderWriter = new BinaryWriter(new MemoryStream(cryptHeaderData)))
                {
                    cryptHeaderWriter.Write(filelistCryptHeader.MD5Hash);
                    cryptHeaderWriter.WriteBytesUInt32(uint.MaxValue, false);
                    cryptHeaderWriter.WriteBytesUInt32(filelistCryptHeader.EncryptionTag, false);
                }
            }

            return cryptHeaderData;
        }

        private static byte[] BuildHeader(FilelistHeader filelistHeader)
        {
            var headerSize = 12;
            var headerData = new byte[headerSize];

            using (var headerWriter = new BinaryWriter(new MemoryStream(headerData)))
            {
                headerWriter.WriteBytesUInt32((uint)(headerSize + filelistHeader.FileCount * 8), false);
                headerWriter.WriteBytesUInt32((uint)(headerSize + (filelistHeader.FileCount * 8) + (filelistHeader.ChunkCount * 12)), false);
                headerWriter.WriteBytesUInt32(filelistHeader.FileCount, false);
            }

            return headerData;
        }

        private static ChunkPack[] BuildChunkPackTable(int chunkCount, uint fileCount, GameCode gameCode, FilelistEntryV1[] newEntryV1Table, FilelistEntryV2[] newEntryV2Table, FileInfoStringPack[] fileInfoStringPackTable)
        {
            var stringsCountTable = new int[chunkCount];

            for (int i = 0; i < fileCount; i++)
            {
                ushort currentChunkID;

                if (gameCode == GameCode.ff131 || gameCode == GameCode.dirge)
                {
                    currentChunkID = newEntryV1Table[i].ChunkID;
                }
                else
                {
                    currentChunkID = newEntryV2Table[i].ChunkID;
                }

                stringsCountTable[currentChunkID] = stringsCountTable[currentChunkID] + 1;
            }

            var chunkPackTable = new ChunkPack[chunkCount];
            var lastChunkIndex = chunkCount - 1;

            for (int i = 0; i < chunkCount; i++)
            {
                if (i != lastChunkIndex)
                {
                    chunkPackTable[i] = new ChunkPack() { FileInfoStrings = new string[stringsCountTable[i]] };
                }
                else
                {
                    chunkPackTable[i] = new ChunkPack() { FileInfoStrings = new string[(stringsCountTable[i]) + 1] };
                }
            }

            for (int i = 0; i < chunkCount; i++)
            {
                var currentChunkFileInfoStringCount = chunkPackTable[i].FileInfoStrings.Length;

                for (int j = 0; j < currentChunkFileInfoStringCount; j++)
                {
                    for (int k = 0; k < fileCount; k++)
                    {
                        if (fileInfoStringPackTable[k].ChunkID == i)
                        {
                            chunkPackTable[i].FileInfoStrings[j] = fileInfoStringPackTable[k].FileInfoString;
                            j++;
                        }
                    }
                }
            }

            var endStringIndex = chunkPackTable[lastChunkIndex].FileInfoStrings.Length - 1;
            chunkPackTable[lastChunkIndex].FileInfoStrings[endStringIndex] = "end\0";

            return chunkPackTable;
        }

        private static byte[] BuildEntryTable(uint fileCount, GameCode gameCode, FilelistEntryV1[] newEntryV1Table, FilelistEntryV2[] newEntryV2Table, ChunkPack[] chunkPackTable)
        {
            var entryTableData = new byte[fileCount * 8];

            using (var entriesWriter = new BinaryWriter(new MemoryStream(entryTableData)))
            {
                int fileInfoStringIndex = 0;
                ushort currentFileInfoStringPos = 0;
                ushort currentChunkID = 0;

                for (int i = 0; i < fileCount; i++)
                {
                    if (gameCode == GameCode.ff131 || gameCode == GameCode.dirge)
                    {
                        var currentEntryV1 = newEntryV1Table[i];
                        entriesWriter.WriteBytesUInt32(currentEntryV1.FileCode, false);
                        entriesWriter.WriteBytesUInt16(currentEntryV1.ChunkID, false);

                        if (currentChunkID != currentEntryV1.ChunkID)
                        {
                            currentChunkID = currentEntryV1.ChunkID;
                            fileInfoStringIndex = 0;
                            currentFileInfoStringPos = 0;
                        }

                        var newWhiteFileInfoString = chunkPackTable[currentEntryV1.ChunkID].FileInfoStrings[fileInfoStringIndex];
                        fileInfoStringIndex++;

                        entriesWriter.WriteBytesUInt16(currentFileInfoStringPos, false);
                        currentFileInfoStringPos += (ushort)(Encoding.ASCII.GetByteCount(newWhiteFileInfoString));
                    }
                    else
                    {
                        var currentEntryV2 = newEntryV2Table[i];
                        entriesWriter.WriteBytesUInt32(currentEntryV2.FileCode, false);

                        if (currentChunkID != currentEntryV2.ChunkID)
                        {
                            currentChunkID = currentEntryV2.ChunkID;
                            fileInfoStringIndex = 0;
                            currentFileInfoStringPos = 0;
                        }

                        var newWhiteFileInfoString = chunkPackTable[currentEntryV2.ChunkID].FileInfoStrings[fileInfoStringIndex];
                        fileInfoStringIndex++;

                        if (currentChunkID % 2 == 0)
                        {
                            entriesWriter.WriteBytesUInt16(currentFileInfoStringPos, false);
                        }
                        else
                        {
                            entriesWriter.WriteBytesUInt16((ushort)(currentFileInfoStringPos | 0x8000), false);
                        }

                        entriesWriter.Write((byte)(currentChunkID >> 1));
                        entriesWriter.Write(currentEntryV2.FileTypeID);

                        currentFileInfoStringPos += (ushort)Encoding.ASCII.GetByteCount(newWhiteFileInfoString);
                    }
                }
            }

            return entryTableData;
        }

        private static byte[] BuildPathChunksAndInfo(int chunkCount, ChunkPack[] chunkPackTable)
        {
            var pathChunksAndInfoData = new byte[] { };
            var pathChunksData = new byte[] { };
            var pathChunksInfoData = new byte[chunkCount * 12];
            var newPathChunksInfoTable = new FilelistPathChunkInfo[chunkCount];

            using (var chunkDataStream = new MemoryStream())
            {
                for (int i = 0; i < chunkCount; i++)
                {
                    var currentChunkFileInfoStringCount = chunkPackTable[i].FileInfoStrings.Length;
                    var currentChunkFileInfoStrings = new string[currentChunkFileInfoStringCount];
                    var currentChunkSize = 0;

                    for (int j = 0; j < currentChunkFileInfoStringCount; j++)
                    {
                        var currentFileInfoString = chunkPackTable[i].FileInfoStrings[j];
                        currentChunkFileInfoStrings[j] = currentFileInfoString;
                        currentChunkSize += Encoding.ASCII.GetByteCount(currentFileInfoString);
                    }

                    var currentChunkData = new byte[currentChunkSize];
                    var copyIndex = 0;

                    for (int k = 0; k < currentChunkFileInfoStringCount; k++)
                    {
                        var currentFileInfoStringData = Encoding.ASCII.GetBytes(currentChunkFileInfoStrings[k]);
                        var currentFileInfoStringDataSize = currentFileInfoStringData.Length;

                        Array.ConstrainedCopy(currentFileInfoStringData, 0, currentChunkData, copyIndex, currentFileInfoStringDataSize);
                        copyIndex += currentFileInfoStringDataSize;
                    }

                    var currentChunkDataCmp = ZlibFunctions.ZlibCompressBuffer(currentChunkData);

                    var chunkPosition = (uint)chunkDataStream.Length;

                    var currentChunkInfo = new FilelistPathChunkInfo
                    {
                        UncmpSize = (uint)currentChunkData.Length,
                        CmpSize = (uint)currentChunkDataCmp.Length,
                        ChunkPosition = chunkPosition
                    };

                    newPathChunksInfoTable[i] = currentChunkInfo;

                    chunkDataStream.Write(currentChunkDataCmp, 0, currentChunkDataCmp.Length);
                }

                chunkDataStream.Seek(0, SeekOrigin.Begin);
                pathChunksData = chunkDataStream.ToArray();
            }

            using (var chunkInfoStream = new BinaryWriter(new MemoryStream(pathChunksInfoData)))
            {
                for (int i = 0; i < chunkCount; i++)
                {
                    chunkInfoStream.WriteBytesUInt32(newPathChunksInfoTable[i].UncmpSize, false);
                    chunkInfoStream.WriteBytesUInt32(newPathChunksInfoTable[i].CmpSize, false);
                    chunkInfoStream.WriteBytesUInt32(newPathChunksInfoTable[i].ChunkPosition, false);
                }
            }

            using (var chunkDataAndInfoStream = new MemoryStream())
            {
                chunkDataAndInfoStream.Write(pathChunksInfoData, 0, pathChunksInfoData.Length);
                chunkDataAndInfoStream.Write(pathChunksData, 0, pathChunksData.Length);

                chunkDataAndInfoStream.Seek(0, SeekOrigin.Begin);
                pathChunksAndInfoData = chunkDataAndInfoStream.ToArray();
            }

            return pathChunksAndInfoData;
        }
    }
}
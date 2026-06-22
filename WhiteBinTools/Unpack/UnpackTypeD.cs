using System;
using System.IO;
using System.Text;
using WhiteBinTools.Filelist;
using WhiteBinTools.Support;
using WhiteBinTools.Support.Structures;
using static WhiteBinTools.Support.Enumerators;

namespace WhiteBinTools.Unpack
{
    internal class UnpackTypeD
    {
        public static void UnpackFilelist(GameCode gameCode, string filelistFile, StreamWriter logWriter)
        {
            SharedFunctions.CheckFileExists(filelistFile, logWriter, "Error: Filelist file specified in the argument is missing");

            var filelistLoadData = FilelistLoader.LoadFilelist(gameCode, filelistFile, logWriter);

            var filelistCryptHeader = filelistLoadData.FilelistCryptHeader;
            var filelistHeader = filelistLoadData.FilelistHeader;
            var filelistEntryV1Table = filelistLoadData.FilelistEntryV1Table;
            var filelistEntryV2Table = filelistLoadData.FilelistEntryV2Table;
            var filelistChunks = filelistLoadData.FilelistChunks;

            var filelistOutName = Path.GetFileName(filelistFile);
            var extractedFilelistDir = Path.Combine(Path.GetDirectoryName(filelistFile), $"_{filelistOutName}");
            var infoFile = Path.Combine(extractedFilelistDir, "#info.txt");
            var chunkTxtFilePathPrefix = Path.Combine(extractedFilelistDir, $"Chunk_");

            SharedFunctions.IfDirExistsDel(extractedFilelistDir);
            Directory.CreateDirectory(extractedFilelistDir);

            using (var infoStreamWriter = new StreamWriter(infoFile, true))
            {
                if (gameCode == GameCode.ff132)
                {
                    infoStreamWriter.WriteLine($"encrypted: {filelistCryptHeader.HasCryptHeader.ToString().ToLowerInvariant()}");

                    if (filelistCryptHeader.HasCryptHeader)
                    {
                        var seedA = BitConverter.ToUInt64(filelistCryptHeader.MD5Hash, 0);
                        var seedB = BitConverter.ToUInt64(filelistCryptHeader.MD5Hash, 8);

                        infoStreamWriter.WriteLine($"seedA: {seedA}");
                        infoStreamWriter.WriteLine($"seedB: {seedB}");
                        infoStreamWriter.WriteLine($"encryptionTag(DO_NOT_CHANGE): {filelistCryptHeader.EncryptionTag}");
                    }
                }

                infoStreamWriter.WriteLine($"fileCount: {filelistHeader.FileCount}");
                infoStreamWriter.WriteLine($"chunkCount: {filelistHeader.ChunkCount}");
            }

            var fileInfoStringPackTable = new FileInfoStringPack[filelistHeader.FileCount];

            logWriter.LogMessage("Parsing filepaths....");

            for (int i = 0; i < filelistHeader.FileCount; i++)
            {
                string whiteFileInfoString;
                var entryAndInfoDataString = new StringBuilder();

                if (gameCode == GameCode.ff131 || gameCode == GameCode.dirge)
                {
                    var filelistEntryV1 = filelistEntryV1Table[i];
                    whiteFileInfoString = FilelistLoader.GetWhiteFileInfoString(filelistEntryV1.FileInfoPos, filelistChunks, filelistEntryV1.ChunkID);

                    entryAndInfoDataString.Append($"{filelistEntryV1.FileCode}|{whiteFileInfoString}");
                    fileInfoStringPackTable[i] = new FileInfoStringPack() { ChunkID = filelistEntryV1.ChunkID, FileInfoString = entryAndInfoDataString.ToString() };
                }
                else
                {
                    var filelistEntryV2 = filelistEntryV2Table[i];
                    whiteFileInfoString = FilelistLoader.GetWhiteFileInfoString(filelistEntryV2.FileInfoPos, filelistChunks, filelistEntryV2.ChunkID);

                    entryAndInfoDataString.Append($"{filelistEntryV2.FileCode}|{filelistEntryV2.FileTypeID}|{whiteFileInfoString}");
                    fileInfoStringPackTable[i] = new FileInfoStringPack() { ChunkID = filelistEntryV2.ChunkID, FileInfoString = entryAndInfoDataString.ToString() };
                }
            }

            for (int i = 0; i < filelistHeader.ChunkCount; i++)
            {
                using (var chunkWriter = new StreamWriter($"{chunkTxtFilePathPrefix}{i}.txt", true, new UTF8Encoding(false)))
                {
                    for (int j = 0; j < filelistHeader.FileCount; j++)
                    {
                        if (fileInfoStringPackTable[j].ChunkID == i)
                        {
                            chunkWriter.WriteLine(fileInfoStringPackTable[j].FileInfoString);
                        }
                    }
                }
            }

            logWriter.LogMessage($"\nFinished unpacking \"{filelistOutName}\"");
        }
    }
}
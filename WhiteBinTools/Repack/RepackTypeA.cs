using System;
using System.IO;
using WhiteBinTools.Filelist;
using WhiteBinTools.Support;
using WhiteBinTools.Support.Structures;
using static WhiteBinTools.Support.Enumerators;

namespace WhiteBinTools.Repack
{
    internal class RepackTypeA
    {
        public static void RepackAll(GameCode gameCode, string filelistFile, string unpackedDir, StreamWriter logWriter)
        {
            SharedFunctions.CheckFileExists(filelistFile, logWriter, "Error: Filelist file specified in the argument is missing");
            SharedFunctions.CheckDirExists(unpackedDir, logWriter, "Error: Unpacked directory specified in the argument is missing");

            var filelistLoadData = FilelistLoader.LoadFilelist(gameCode, filelistFile, logWriter);

            var filelistCryptHeader = filelistLoadData.FilelistCryptHeader;
            var filelistHeader = filelistLoadData.FilelistHeader;
            var filelistEntryV1Table = filelistLoadData.FilelistEntryV1Table;
            var filelistEntryV2Table = filelistLoadData.FilelistEntryV2Table;
            var filelistChunks = filelistLoadData.FilelistChunks;

            var newFilelistFile = filelistFile;
            var newWhiteBinName = Path.GetFileName(unpackedDir);

            if (newWhiteBinName.StartsWith("_white"))
            {
                newWhiteBinName = newWhiteBinName.Remove(0, 1);
            }

            var newWhiteBinFile = Path.Combine(Path.GetDirectoryName(unpackedDir), newWhiteBinName);

            if (Core.ShouldBckup)
            {
                SharedFunctions.IfFileExistsDel($"{newFilelistFile}.bak");
                File.Move(newFilelistFile, $"{newFilelistFile}.bak");

                SharedFunctions.IfFileExistsDel($"{newWhiteBinFile}.bak");

                if (File.Exists(newWhiteBinFile))
                {
                    File.Move(newWhiteBinFile, $"{newWhiteBinFile}.bak");
                }
            }

            SharedFunctions.IfFileExistsDel($"{newWhiteBinFile}");

            FilelistEntryV1[] newEntryV1Table = Array.Empty<FilelistEntryV1>();
            FilelistEntryV2[] newEntryV2Table = Array.Empty<FilelistEntryV2>();

            if (gameCode == GameCode.ff131 || gameCode == GameCode.dirge)
            {
                newEntryV1Table = new FilelistEntryV1[filelistHeader.FileCount];
            }
            else
            {
                newEntryV2Table = new FilelistEntryV2[filelistHeader.FileCount];
            }

            var fileInfoStringPackTable = new FileInfoStringPack[filelistHeader.FileCount];

            using (var newWhiteBinStream = new FileStream(newWhiteBinFile, FileMode.Append, FileAccess.Write))
            {
                var noPathCounter = 0;

                for (int i = 0; i < filelistHeader.FileCount; i++)
                {
                    string whiteFileInfoString;
                    uint fileCode;
                    ushort chunkID;

                    if (gameCode == GameCode.ff131 || gameCode == GameCode.dirge)
                    {
                        var filelistEntryV1 = filelistEntryV1Table[i];
                        newEntryV1Table[i] = filelistEntryV1;

                        whiteFileInfoString = FilelistLoader.GetWhiteFileInfoString(filelistEntryV1.FileInfoPos, filelistChunks, filelistEntryV1.ChunkID);
                        fileCode = filelistEntryV1.FileCode;
                        chunkID = filelistEntryV1.ChunkID;
                    }
                    else
                    {
                        var filelistEntryV2 = filelistEntryV2Table[i];
                        newEntryV2Table[i] = filelistEntryV2;

                        whiteFileInfoString = FilelistLoader.GetWhiteFileInfoString(filelistEntryV2.FileInfoPos, filelistChunks, filelistEntryV2.ChunkID);
                        fileCode = filelistEntryV2.FileCode;
                        chunkID = filelistEntryV2.ChunkID;
                    }

                    var whiteFileInfoData = FilelistLoader.GetWhiteFileInfoData(whiteFileInfoString, gameCode, fileCode, ref noPathCounter);
                    whiteFileInfoData.FilePath = whiteFileInfoData.FilePath.Replace('/', Path.DirectorySeparatorChar);

                    var outFile = Path.Combine(unpackedDir, whiteFileInfoData.FilePath);

                    if (!File.Exists(outFile))
                    {
                        File.Create(outFile);
                    }

                    var repackedState = new RepackedState();
                    var pathString = RepackHelpers.RepackAppend(ref repackedState, whiteFileInfoData, outFile, newWhiteBinStream);

                    fileInfoStringPackTable[i] = new FileInfoStringPack() { ChunkID = chunkID, FileInfoString = pathString };

                    logWriter.LogMessage($"{repackedState} _{Path.Combine(newWhiteBinName, whiteFileInfoData.FilePath)}");
                }
            }

            logWriter.LogMessage("\nBuilding filelist....");
            FilelistBuilder.BuildFilelist(filelistCryptHeader, filelistHeader, gameCode, newEntryV1Table, newEntryV2Table, fileInfoStringPackTable, newFilelistFile);

            if (filelistCryptHeader.HasCryptHeader)
            {
                FilelistCrypto.EncryptProcess(newFilelistFile, logWriter);
            }

            logWriter.LogMessage($"\nFinished repacking files to \"{newWhiteBinName}\"");
        }
    }
}
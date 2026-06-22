using System;
using System.IO;
using WhiteBinTools.Filelist;
using WhiteBinTools.Support;
using WhiteBinTools.Support.Structures;
using static WhiteBinTools.Support.Enumerators;

namespace WhiteBinTools.Repack
{
    internal class RepackTypeC
    {
        public static void RepackMultiple(GameCode gameCode, string filelistFile, string whiteBinFile, string whiteExtractedDir, StreamWriter logWriter)
        {
            SharedFunctions.CheckFileExists(filelistFile, logWriter, "Error: Filelist file specified in the argument is missing");
            SharedFunctions.CheckFileExists(whiteBinFile, logWriter, "Error: Image bin file specified in the argument is missing");
            SharedFunctions.CheckDirExists(whiteExtractedDir, logWriter, "Error: Unpacked directory specified in the argument is missing");

            var filelistLoadData = FilelistLoader.LoadFilelist(gameCode, filelistFile, logWriter);

            var filelistCryptHeader = filelistLoadData.FilelistCryptHeader;
            var filelistHeader = filelistLoadData.FilelistHeader;
            var filelistEntryV1Table = filelistLoadData.FilelistEntryV1Table;
            var filelistEntryV2Table = filelistLoadData.FilelistEntryV2Table;
            var filelistChunks = filelistLoadData.FilelistChunks;

            var newFilelistFile = filelistFile;
            var whiteBinName = Path.GetFileName(whiteBinFile);

            if (Core.ShouldBckup)
            {
                logWriter.LogMessage("\nBacking up filelist and image bin files....\n");
                SharedFunctions.IfFileExistsDel($"{newFilelistFile}.bak");
                File.Move(newFilelistFile, $"{newFilelistFile}.bak");

                SharedFunctions.IfFileExistsDel($"{whiteBinFile}.bak");
                File.Copy(whiteBinFile, $"{whiteBinFile}.bak");
            }

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

                if (File.Exists(Path.Combine(whiteExtractedDir, whiteFileInfoData.FilePath)))
                {
                    var outFile = Path.Combine(whiteExtractedDir, whiteFileInfoData.FilePath);
                    var shouldInject = RepackHelpers.DetermineInject(whiteFileInfoData, outFile);
                    var repackedState = new RepackedState();
                    string pathString;
                    string packedState;

                    if (shouldInject)
                    {
                        using (var whiteBinStream = new FileStream(whiteBinFile, FileMode.Open, FileAccess.Write))
                        {
                            RepackHelpers.CleanOldFile(whiteBinStream, whiteFileInfoData.FilePosition, whiteFileInfoData.CmpSize);
                            pathString = RepackHelpers.RepackInject(whiteFileInfoData, outFile, whiteBinStream, ref repackedState);
                            packedState = "(Injected)";
                        }
                    }
                    else
                    {
                        using (var whiteBinStream = new FileStream(whiteBinFile, FileMode.Append, FileAccess.Write))
                        {
                            RepackHelpers.CleanOldFile(whiteBinStream, whiteFileInfoData.FilePosition, whiteFileInfoData.CmpSize);
                            pathString = RepackHelpers.RepackAppend(ref repackedState, whiteFileInfoData, outFile, whiteBinStream);
                            packedState = "(Appended)";
                        }
                    }

                    fileInfoStringPackTable[i] = new FileInfoStringPack() { ChunkID = chunkID, FileInfoString = pathString };

                    logWriter.LogMessage($"{repackedState} _{Path.Combine(whiteBinName, whiteFileInfoData.FilePath)} {packedState}");
                }
                else
                {
                    fileInfoStringPackTable[i] = new FileInfoStringPack() { ChunkID = chunkID, FileInfoString = whiteFileInfoString + "\0" };
                }
            }

            logWriter.LogMessage("\nBuilding filelist....");
            FilelistBuilder.BuildFilelist(filelistCryptHeader, filelistHeader, gameCode, newEntryV1Table, newEntryV2Table, fileInfoStringPackTable, newFilelistFile);

            if (filelistCryptHeader.HasCryptHeader)
            {
                FilelistCrypto.EncryptProcess(newFilelistFile, logWriter);
            }

            logWriter.LogMessage($"\nFinished repacking file(s) to \"{whiteBinName}\"");
        }
    }
}
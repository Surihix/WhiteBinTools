using System.Collections.Generic;
using System.IO;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.SupportClasses;
using static WhiteBinTools.SupportClasses.ProgramEnums;

namespace WhiteBinTools.RepackClasses
{
    internal class RepackTypeC
    {
        public static void RepackMultiple(GameCodes gameCode, string filelistFile, string whiteBinFile, string whiteExtractedDir, StreamWriter logWriter)
        {
            filelistFile.CheckFileExists(logWriter, "Error: Filelist file specified in the argument is missing");
            whiteBinFile.CheckFileExists(logWriter, "Error: Image bin file specified in the argument is missing");
            whiteExtractedDir.CheckDirExists(logWriter, "Error: Unpacked directory specified in the argument is missing");

            var filelistVariables = new FilelistVariables();
            var repackVariables = new RepackVariables();

            FilelistProcesses.PrepareFilelistVars(filelistVariables, filelistFile);
            RepackProcesses.PrepareRepackVars(repackVariables, filelistFile, filelistVariables, whiteExtractedDir);

            IOhelpers.LogMessage("\nBacking up filelist and image bin files....\n", logWriter);
            RepackProcesses.CreateFilelistBackup(filelistFile, repackVariables);
            RepackProcesses.CreateWhiteBinBackup(whiteBinFile, repackVariables);


            FilelistProcesses.DecryptProcess(gameCode, filelistVariables, logWriter);

            using (var filelistStream = new FileStream(filelistVariables.MainFilelistFile, FileMode.Open, FileAccess.Read))
            {
                using (var filelistReader = new BinaryReader(filelistStream))
                {
                    FilelistChunksPrep.GetFilelistOffsets(filelistReader, logWriter, filelistVariables);
                    FilelistChunksPrep.BuildChunks(filelistStream, filelistVariables);

                    if (filelistVariables.IsEncrypted)
                    {
                        filelistStream.Seek(0, SeekOrigin.Begin);
                        filelistVariables.EncryptedHeaderData = new byte[32];
                        filelistStream.Read(filelistVariables.EncryptedHeaderData, 0, 32);

                        filelistStream.Dispose();
                        File.Delete(filelistVariables.MainFilelistFile);
                    }
                }
            }

            filelistFile.IfFileExistsDel();

            if (gameCode.Equals(GameCodes.ff132))
            {
                filelistVariables.CurrentChunkNumber = -1;
            }

            // Build an empty dictionary
            // for the chunks 
            var newChunksDict = new Dictionary<int, List<byte>>();
            RepackProcesses.CreateEmptyNewChunksDict(filelistVariables, newChunksDict);


            filelistVariables.LastChunkNumber = 0;

            using (var entriesStream = new MemoryStream())
            {
                entriesStream.Write(filelistVariables.EntriesData, 0, filelistVariables.EntriesData.Length);
                entriesStream.Seek(0, SeekOrigin.Begin);

                using (var entriesReader = new BinaryReader(entriesStream))
                {

                    // Repacking files section
                    long entriesReadPos = 0;
                    var packedAs = "";
                    for (int f = 0; f < filelistVariables.TotalFiles; f++)
                    {
                        FilelistProcesses.GetCurrentFileEntry(gameCode, entriesReader, entriesReadPos, filelistVariables);
                        entriesReadPos += 8;

                        RepackProcesses.GetPackedState(filelistVariables.PathString, repackVariables, whiteExtractedDir);

                        repackVariables.AsciiFilePos = repackVariables.ConvertedOgStringData[0];
                        repackVariables.AsciiUnCmpSize = repackVariables.ConvertedOgStringData[1];
                        repackVariables.AsciiCmpSize = repackVariables.ConvertedOgStringData[2];

                        // Repack a specific file
                        var currentFileInProcess = Path.Combine(repackVariables.OgDirectoryPath, repackVariables.OgFileName);
                        if (File.Exists(Path.Combine(whiteExtractedDir, currentFileInProcess)))
                        {
                            switch (repackVariables.WasCompressed)
                            {
                                case true:
                                    RepackProcesses.CleanOldFile(repackVariables.NewWhiteBinFile, repackVariables.OgFilePos, repackVariables.OgCmpSize);

                                    var zlibTmpCmpData = repackVariables.OgFullFilePath.ZlibCompress();
                                    var zlibCmpFileSize = (uint)zlibTmpCmpData.Length;

                                    if (zlibCmpFileSize < repackVariables.OgCmpSize || zlibCmpFileSize == repackVariables.OgCmpSize)
                                    {
                                        RepackProcesses.InjectProcess(repackVariables, ref packedAs);
                                    }
                                    else
                                    {
                                        RepackProcesses.AppendProcess(repackVariables, ref packedAs);
                                    }
                                    break;

                                case false:
                                    RepackProcesses.CleanOldFile(repackVariables.NewWhiteBinFile, repackVariables.OgFilePos, repackVariables.OgUnCmpSize);

                                    var dummyFileSize = (uint)new FileInfo(repackVariables.OgFullFilePath).Length;

                                    if (dummyFileSize < repackVariables.OgUnCmpSize || dummyFileSize == repackVariables.OgUnCmpSize)
                                    {
                                        RepackProcesses.InjectProcess(repackVariables, ref packedAs);
                                    }
                                    else
                                    {
                                        RepackProcesses.AppendProcess(repackVariables, ref packedAs);
                                    }
                                    break;
                            }

                            IOhelpers.LogMessage(repackVariables.RepackState + " " + Path.Combine(repackVariables.NewWhiteBinFileName, repackVariables.RepackLogMsg) + " " + packedAs, logWriter);
                        }

                        RepackProcesses.BuildPathForChunk(repackVariables, gameCode, filelistVariables, newChunksDict);
                    }
                }
            }


            IOhelpers.LogMessage("\nBuilding filelist....", logWriter);
            RepackFilelistData.BuildFilelist(filelistVariables, newChunksDict, repackVariables, gameCode);

            if (filelistVariables.IsEncrypted)
            {
                FilelistProcesses.EncryptProcess(repackVariables, logWriter);
            }

            IOhelpers.LogMessage("\nFinished repacking multiple files into " + "\"" + repackVariables.NewWhiteBinFileName + "\"", logWriter);
        }
    }
}
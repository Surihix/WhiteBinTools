using System.IO;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.SupportClasses;

namespace WhiteBinTools.RepackClasses
{
    internal class RepackTypeA
    {
        public static void RepackAll(CmnEnums.GameCodes gameCodeVar, string filelistFileVar, string extractedDirVar, StreamWriter logWriter)
        {
            filelistFileVar.CheckFileExists(logWriter, "Error: Filelist file specified in the argument is missing");
            extractedDirVar.CheckDirExists(logWriter, "Error: Unpacked directory specified in the argument is missing");

            var filelistVariables = new FilelistProcesses();
            var repackVariables = new RepackProcesses();

            FilelistProcesses.PrepareFilelistVars(filelistVariables, filelistFileVar);
            RepackProcesses.PrepareRepackVars(repackVariables, filelistFileVar, filelistVariables, extractedDirVar);

            filelistVariables.DefaultChunksExtDir.IfDirExistsDel();
            Directory.CreateDirectory(filelistVariables.DefaultChunksExtDir);

            repackVariables.NewChunksExtDir.IfDirExistsDel();
            Directory.CreateDirectory(repackVariables.NewChunksExtDir);

            RepackProcesses.CreateFilelistBackup(filelistFileVar, repackVariables);

            repackVariables.OldWhiteBinFileBackup = repackVariables.NewWhiteBinFile + ".bak";
            repackVariables.OldWhiteBinFileBackup.IfFileExistsDel();
            if (File.Exists(repackVariables.NewWhiteBinFile))
            {
                File.Move(repackVariables.NewWhiteBinFile, repackVariables.OldWhiteBinFileBackup);
            }


            FilelistProcesses.DecryptProcess(gameCodeVar, filelistVariables, logWriter);

            using (var filelist = new FileStream(filelistVariables.MainFilelistFile, FileMode.Open, FileAccess.Read))
            {
                using (var filelistReader = new BinaryReader(filelist))
                {
                    FilelistProcesses.GetFilelistOffsets(filelistReader, logWriter, filelistVariables);
                    FilelistProcesses.UnpackChunks(filelist, filelistVariables.ChunkFile, filelistVariables);
                }
            }


            using (var newWhiteBin = new FileStream(repackVariables.NewWhiteBinFile, FileMode.Append, FileAccess.Write))
            {

                filelistVariables.ChunkFNameCount = 0;
                repackVariables.LastChunkFileNumber = 0;

                for (int ch = 0; ch < filelistVariables.TotalChunks; ch++)
                {
                    var filesInChunkCount = FilelistProcesses.GetFilesInChunkCount(filelistVariables.ChunkFile + filelistVariables.ChunkFNameCount);

                    using (var currentChunk = new FileStream(filelistVariables.ChunkFile + filelistVariables.ChunkFNameCount, FileMode.Open, FileAccess.Read))
                    {
                        using (var chunkStringReader = new BinaryReader(currentChunk))
                        {

                            using (var updChunkStrings = new FileStream(repackVariables.NewChunkFile + filelistVariables.ChunkFNameCount, FileMode.Append, FileAccess.Write))
                            {
                                using (var updChunkStringsWriter = new StreamWriter(updChunkStrings))
                                {

                                    var chunkStringReaderPos = (uint)0;
                                    for (int f = 0; f < filesInChunkCount; f++)
                                    {
                                        var convertedString = chunkStringReader.BinaryToString(chunkStringReaderPos);
                                        if (convertedString.Equals("end"))
                                        {
                                            updChunkStringsWriter.Write("end\0");
                                            repackVariables.LastChunkFileNumber = filelistVariables.ChunkFNameCount;
                                            break;
                                        }

                                        RepackProcesses.GetPackedState(convertedString, repackVariables, extractedDirVar);

                                        if (!File.Exists(repackVariables.OgFullFilePath))
                                        {
                                            var createDummyFile = File.Create(repackVariables.OgFullFilePath);
                                            createDummyFile.Close();
                                        }

                                        RepackProcesses.RepackTypeAppend(repackVariables, newWhiteBin, repackVariables.OgFullFilePath);

                                        updChunkStringsWriter.Write(repackVariables.AsciiFilePos + ":");
                                        updChunkStringsWriter.Write(repackVariables.AsciiUnCmpSize + ":");
                                        updChunkStringsWriter.Write(repackVariables.AsciiCmpSize + ":");
                                        updChunkStringsWriter.Write(repackVariables.RepackPathInChunk + "\0");

                                        IOhelpers.LogMessage(repackVariables.RepackState + " " + repackVariables.NewWhiteBinFileName + "\\" + repackVariables.RepackLogMsg, logWriter);

                                        chunkStringReaderPos = (uint)chunkStringReader.BaseStream.Position;
                                    }
                                }
                            }
                        }
                    }

                    filelistVariables.ChunkFNameCount++;
                }
            }

            filelistVariables.DefaultChunksExtDir.IfDirExistsDel();


            if (filelistVariables.IsEncrypted.Equals(true))
            {
                File.Delete(filelistFileVar);
            }

            RepackProcesses.CreateFilelist(filelistVariables, repackVariables, gameCodeVar);

            if (filelistVariables.IsEncrypted.Equals(true))
            {
                FilelistProcesses.EncryptProcess(repackVariables, logWriter);
                filelistVariables.TmpDcryptFilelistFile.IfFileExistsDel();
            }

            IOhelpers.LogMessage("\nFinished repacking files into " + repackVariables.NewWhiteBinFileName, logWriter);
        }
    }
}
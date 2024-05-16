using System.IO;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.SupportClasses;
using static WhiteBinTools.SupportClasses.ProgramEnums;

namespace WhiteBinTools.RepackClasses
{
    internal class RepackTypeA
    {
        public static void RepackAll(GameCodes gameCode, string filelistFile, string extractedDir, StreamWriter logWriter)
        {
            filelistFile.CheckFileExists(logWriter, "Error: Filelist file specified in the argument is missing");
            extractedDir.CheckDirExists(logWriter, "Error: Unpacked directory specified in the argument is missing");

            var filelistVariables = new FilelistVariables();
            var repackVariables = new RepackVariables();

            FilelistProcesses.PrepareFilelistVars(filelistVariables, filelistFile);
            RepackProcesses.PrepareRepackVars(repackVariables, filelistFile, filelistVariables, extractedDir);

            filelistVariables.DefaultChunksExtDir.IfDirExistsDel();
            Directory.CreateDirectory(filelistVariables.DefaultChunksExtDir);

            repackVariables.NewChunksExtDir.IfDirExistsDel();
            Directory.CreateDirectory(repackVariables.NewChunksExtDir);

            RepackProcesses.CreateFilelistBackup(filelistFile, repackVariables);

            repackVariables.OldWhiteBinFileBackup = repackVariables.NewWhiteBinFile + ".bak";
            repackVariables.OldWhiteBinFileBackup.IfFileExistsDel();
            if (File.Exists(repackVariables.NewWhiteBinFile))
            {
                File.Move(repackVariables.NewWhiteBinFile, repackVariables.OldWhiteBinFileBackup);
            }


            FilelistProcesses.DecryptProcess(gameCode, filelistVariables, logWriter);

            using (var filelistStream = new FileStream(filelistVariables.MainFilelistFile, FileMode.Open, FileAccess.Read))
            {
                using (var filelistReader = new BinaryReader(filelistStream))
                {
                    FilelistChunksPrep.GetFilelistOffsets(filelistReader, logWriter, filelistVariables);
                    FilelistChunksPrep.UnpackChunks(filelistStream, filelistVariables.ChunkFile, filelistVariables);
                }
            }


            using (var newWhiteBinStream = new FileStream(repackVariables.NewWhiteBinFile, FileMode.Append, FileAccess.Write))
            {

                filelistVariables.ChunkFNameCount = 0;
                repackVariables.LastChunkFileNumber = filelistVariables.TotalChunks - 1;

                for (int ch = 0; ch < filelistVariables.TotalChunks; ch++)
                {
                    var filesInChunkCount = FilelistProcesses.GetFilesInChunkCount(filelistVariables.ChunkFile + filelistVariables.ChunkFNameCount);

                    using (var currentChunkStream = new FileStream(filelistVariables.ChunkFile + filelistVariables.ChunkFNameCount, FileMode.Open, FileAccess.Read))
                    {
                        using (var chunkStringReader = new BinaryReader(currentChunkStream))
                        {

                            using (var updChunkStrings = new FileStream(repackVariables.NewChunkFile + filelistVariables.ChunkFNameCount, FileMode.Append, FileAccess.Write))
                            {
                                using (var updChunkStringsWriter = new StreamWriter(updChunkStrings))
                                {

                                    var chunkStringReaderPos = (uint)0;
                                    for (int f = 0; f < filesInChunkCount; f++)
                                    {
                                        chunkStringReader.BaseStream.Position = chunkStringReaderPos;
                                        var convertedString = chunkStringReader.ReadStringTillNull();
                                        if (convertedString == "end")
                                        {
                                            repackVariables.HasEndString = true;
                                            updChunkStringsWriter.Write("end\0");
                                            break;
                                        }

                                        RepackProcesses.GetPackedState(convertedString, repackVariables, extractedDir);

                                        if (!File.Exists(repackVariables.OgFullFilePath))
                                        {
                                            var fullFilePathDir = Path.GetDirectoryName(repackVariables.OgFullFilePath);
                                            if (!Directory.Exists(fullFilePathDir))
                                            {
                                                Directory.CreateDirectory(fullFilePathDir);
                                            }

                                            var createDummyFile = File.Create(repackVariables.OgFullFilePath);
                                            createDummyFile.Close();
                                        }

                                        RepackProcesses.RepackTypeAppend(repackVariables, newWhiteBinStream, repackVariables.OgFullFilePath);

                                        updChunkStringsWriter.Write(repackVariables.AsciiFilePos + ":");
                                        updChunkStringsWriter.Write(repackVariables.AsciiUnCmpSize + ":");
                                        updChunkStringsWriter.Write(repackVariables.AsciiCmpSize + ":");
                                        updChunkStringsWriter.Write(repackVariables.RepackPathInChunk + "\0");

                                        IOhelpers.LogMessage(repackVariables.RepackState + " " + Path.Combine(repackVariables.NewWhiteBinFileName, repackVariables.RepackLogMsg), logWriter);

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


            if (filelistVariables.IsEncrypted)
            {
                File.Delete(filelistFile);
            }

            RepackFilelist.CreateFilelist(filelistVariables, repackVariables, gameCode);

            if (filelistVariables.IsEncrypted)
            {
                FilelistProcesses.EncryptProcess(repackVariables, logWriter);
                filelistVariables.TmpDcryptFilelistFile.IfFileExistsDel();
            }

            IOhelpers.LogMessage("\nFinished repacking files into " + "\"" + repackVariables.NewWhiteBinFileName + "\"", logWriter);
        }
    }
}
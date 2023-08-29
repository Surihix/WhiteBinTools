using System.IO;
using System;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.SupportClasses;

namespace WhiteBinTools.RepackClasses
{
    internal class RepackTypeC
    {
        public static void RepackMultiple(CmnEnums.GameCodes gameCodeVar, string filelistFileVar, string whiteBinFileVar, string whiteExtractedDirVar, StreamWriter logWriter)
        {
            filelistFileVar.CheckFileExists(logWriter, "Error: Filelist file specified in the argument is missing");
            whiteBinFileVar.CheckFileExists(logWriter, "Error: Image bin file specified in the argument is missing");

            var filelistVariables = new FilelistProcesses();
            var repackVariables = new RepackProcesses();

            FilelistProcesses.PrepareFilelistVars(filelistVariables, filelistFileVar);

            RepackProcesses.PrepareRepackVars(repackVariables, filelistFileVar, filelistVariables, whiteExtractedDirVar);

            filelistVariables.DefaultChunksExtDir.IfDirExistsDel();
            Directory.CreateDirectory(filelistVariables.DefaultChunksExtDir);

            repackVariables.NewChunksExtDir.IfDirExistsDel();
            Directory.CreateDirectory(repackVariables.NewChunksExtDir);

            RepackProcesses.CreateFilelistBackup(filelistFileVar, repackVariables);

            repackVariables.OldWhiteBinFileBackup = repackVariables.NewWhiteBinFile + ".bak";
            repackVariables.OldWhiteBinFileBackup.IfFileExistsDel();

            IOhelpers.LogMessage("Backing up Image bin file....", logWriter);
            File.Copy(repackVariables.NewWhiteBinFile, repackVariables.OldWhiteBinFileBackup);


            FilelistProcesses.DecryptProcess(gameCodeVar, filelistVariables, logWriter);

            using (var filelist = new FileStream(filelistVariables.MainFilelistFile, FileMode.Open, FileAccess.Read))
            {
                using (var filelistReader = new BinaryReader(filelist))
                {
                    FilelistProcesses.GetFilelistOffsets(filelistReader, logWriter, filelistVariables, gameCodeVar);
                    FilelistProcesses.UnpackChunks(filelist, filelistVariables.ChunkFile, filelistVariables);
                }
            }


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
                                var packedAs = "";
                                for (int f = 0; f < filesInChunkCount; f++)
                                {
                                    var convertedString = chunkStringReader.BinaryToString(chunkStringReaderPos);
                                    if (convertedString.Equals("end"))
                                    {
                                        updChunkStringsWriter.Write("end\0");
                                        repackVariables.LastChunkFileNumber = filelistVariables.ChunkFNameCount;
                                        break;
                                    }

                                    RepackProcesses.GetPackedState(convertedString, repackVariables, whiteExtractedDirVar);

                                    repackVariables.AsciiFilePos = repackVariables.ConvertedOgStringData[0];
                                    repackVariables.AsciiUnCmpSize = repackVariables.ConvertedOgStringData[1];
                                    repackVariables.AsciiCmpSize = repackVariables.ConvertedOgStringData[2];

                                    // Repack a specific file
                                    var currentFileInProcess = repackVariables.OgDirectoryPath + "\\" + repackVariables.OgFileName;
                                    if (File.Exists(whiteExtractedDirVar + "\\" + currentFileInProcess))
                                    {
                                        switch (repackVariables.WasCompressed)
                                        {
                                            case true:
                                                RepackProcesses.CleanOldFile(repackVariables.NewWhiteBinFile, repackVariables.OgFilePos, repackVariables.OgCmpSize);

                                                repackVariables.TmpCmpDataFile = whiteExtractedDirVar + "\\zlib_data";
                                                var zlibTmpCmpDataStream = File.Create(repackVariables.TmpCmpDataFile);
                                                zlibTmpCmpDataStream.Close();

                                                repackVariables.OgFullFilePath.ZlibCompress(repackVariables.TmpCmpDataFile, Ionic.Zlib.CompressionLevel.Level9);
                                                var zlibCmpFileSize = (uint)new FileInfo(repackVariables.TmpCmpDataFile).Length;

                                                if (zlibCmpFileSize < repackVariables.OgCmpSize || zlibCmpFileSize == repackVariables.OgCmpSize)
                                                {
                                                    RepackProcesses.InjectProcess(repackVariables, whiteExtractedDirVar, ref packedAs);
                                                }
                                                else
                                                {
                                                    RepackProcesses.AppendProcess(repackVariables, whiteExtractedDirVar, ref packedAs);
                                                }
                                                break;

                                            case false:
                                                RepackProcesses.CleanOldFile(repackVariables.NewWhiteBinFile, repackVariables.OgFilePos, repackVariables.OgUnCmpSize);

                                                var dummyFileSize = (uint)new FileInfo(repackVariables.OgFullFilePath).Length;

                                                if (dummyFileSize < repackVariables.OgUnCmpSize || dummyFileSize == repackVariables.OgUnCmpSize)
                                                {
                                                    RepackProcesses.InjectProcess(repackVariables, whiteExtractedDirVar, ref packedAs);
                                                }
                                                else
                                                {
                                                    RepackProcesses.AppendProcess(repackVariables, whiteExtractedDirVar, ref packedAs);
                                                }
                                                break;
                                        }

                                        IOhelpers.LogMessage(repackVariables.RepackState + " " + repackVariables.NewWhiteBinFileName + "\\" + repackVariables.RepackLogMsg + " " + packedAs, logWriter);
                                    }

                                    updChunkStringsWriter.Write(repackVariables.AsciiFilePos + ":");
                                    updChunkStringsWriter.Write(repackVariables.AsciiUnCmpSize + ":");
                                    updChunkStringsWriter.Write(repackVariables.AsciiCmpSize + ":");
                                    updChunkStringsWriter.Write(repackVariables.RepackPathInChunk + "\0");

                                    chunkStringReaderPos = (uint)chunkStringReader.BaseStream.Position;
                                }
                            }
                        }
                    }
                }

                filelistVariables.ChunkFNameCount++;
            }

            filelistVariables.DefaultChunksExtDir.IfDirExistsDel();


            if (filelistVariables.IsEncrypted.Equals(true))
            {
                File.Delete(filelistFileVar);
            }

            RepackProcesses.CreateFilelist(filelistVariables, repackVariables, gameCodeVar);

            if (filelistVariables.IsEncrypted.Equals(true))
            {
                FilelistProcesses.EncryptProcess(repackVariables, filelistVariables, logWriter);
                filelistVariables.TmpDcryptFilelistFile.IfFileExistsDel();
            }

            IOhelpers.LogMessage("\nFinished repacking files into " + repackVariables.NewWhiteBinFileName, logWriter);
            Console.ReadLine();
        }
    }
}
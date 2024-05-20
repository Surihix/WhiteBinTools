using System.Collections.Generic;
using System.IO;
using System.Text;
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

            var filelistVariables = new FilelistVariables();
            var repackVariables = new RepackVariables();

            FilelistProcesses.PrepareFilelistVars(filelistVariables, filelistFile);

            RepackProcesses.PrepareRepackVars(repackVariables, filelistFile, filelistVariables, whiteExtractedDir);

            RepackProcesses.CreateFilelistBackup(filelistFile, repackVariables);

            repackVariables.OldWhiteBinFileBackup = repackVariables.NewWhiteBinFile + ".bak";
            repackVariables.OldWhiteBinFileBackup.IfFileExistsDel();

            IOhelpers.LogMessage("Backing up Image bin file....", logWriter);
            File.Copy(repackVariables.NewWhiteBinFile, repackVariables.OldWhiteBinFileBackup);


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
                    }
                }
            }

            if (gameCode.Equals(GameCodes.ff132))
            {
                filelistVariables.CurrentChunkNumber = -1;
            }

            if (filelistVariables.IsEncrypted)
            {
                File.Delete(filelistVariables.MainFilelistFile);
            }

            // Build an empty dictionary
            // for the chunks 
            var newChunksDict = new Dictionary<int, List<byte>>();
            for (int c = 0; c < filelistVariables.TotalChunks; c++)
            {
                var chunkDataList = new List<byte>();
                newChunksDict.Add(c, chunkDataList);
            }


            filelistVariables.LastChunkNumber = 0;

            using (var entriesStream = new MemoryStream())
            {
                entriesStream.Write(filelistVariables.EntriesData, 0, filelistVariables.EntriesData.Length);
                entriesStream.Seek(0, SeekOrigin.Begin);

                using (var entriesReader = new BinaryReader(entriesStream))
                {

                    // Repacking files section
                    long entriesReadPos = 0;
                    var stringData = "";
                    var packedAs = "";
                    for (int f = 0; f < filelistVariables.TotalFiles; f++)
                    {
                        FilelistProcesses.GetCurrentFileEntry(gameCode, entriesReader, entriesReadPos, filelistVariables);
                        entriesReadPos += 8;

                        RepackProcesses.GetPackedState(filelistVariables.PathString, repackVariables, whiteExtractedDir);

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

                        var stringBuilder = new StringBuilder();
                        stringBuilder.Append(repackVariables.AsciiFilePos).Append(":").
                            Append(repackVariables.AsciiUnCmpSize).Append(":").
                            Append(repackVariables.AsciiCmpSize).Append(":").
                            Append(repackVariables.RepackPathInChunk).Append("\0");

                        stringData = stringBuilder.ToString();

                        if (gameCode.Equals(GameCodes.ff132))
                        {
                            newChunksDict[filelistVariables.CurrentChunkNumber].AddRange(Encoding.UTF8.GetBytes(stringData));
                            filelistVariables.LastChunkNumber = filelistVariables.CurrentChunkNumber;
                        }
                        else
                        {
                            newChunksDict[filelistVariables.ChunkNumber].AddRange(Encoding.UTF8.GetBytes(stringData));
                            filelistVariables.LastChunkNumber = filelistVariables.ChunkNumber;
                        }
                    }
                }
            }


            filelistFile.IfFileExistsDel();

            IOhelpers.LogMessage("\nBuilding filelist....", logWriter);

            RepackFilelistData.BuildFilelist(filelistVariables, newChunksDict, repackVariables, gameCode);

            if (filelistVariables.IsEncrypted)
            {
                FilelistProcesses.EncryptProcess(repackVariables, logWriter);
            }

            IOhelpers.LogMessage("\nFinished repacking multiple files into " + "\"" + repackVariables.NewWhiteBinFileName + "\"", logWriter);


            //filelistVariables.ChunkFNameCount = 0;
            //repackVariables.LastChunkFileNumber = filelistVariables.TotalChunks - 1;

            //for (int ch = 0; ch < filelistVariables.TotalChunks; ch++)
            //{
            //    var filesInChunkCount = FilelistProcesses.GetFilesInChunkCount(filelistVariables.ChunkFile + filelistVariables.ChunkFNameCount);

            //    using (var currentChunkStream = new FileStream(filelistVariables.ChunkFile + filelistVariables.ChunkFNameCount, FileMode.Open, FileAccess.Read))
            //    {
            //        using (var chunkStringReader = new BinaryReader(currentChunkStream))
            //        {

            //            using (var updChunkStrings = new FileStream(repackVariables.NewChunkFile + filelistVariables.ChunkFNameCount, FileMode.Append, FileAccess.Write))
            //            {
            //                using (var updChunkStringsWriter = new StreamWriter(updChunkStrings))
            //                {

            //                    var chunkStringReaderPos = (uint)0;
            //                    var packedAs = "";
            //                    for (int f = 0; f < filesInChunkCount; f++)
            //                    {
            //                        chunkStringReader.BaseStream.Position = chunkStringReaderPos;
            //                        var convertedString = chunkStringReader.ReadStringTillNull();
            //                        if (convertedString == "end")
            //                        {
            //                            repackVariables.HasEndString = true;
            //                            updChunkStringsWriter.Write("end\0");
            //                            break;
            //                        }

            //                        RepackProcesses.GetPackedState(convertedString, repackVariables, whiteExtractedDir);

            //                        repackVariables.AsciiFilePos = repackVariables.ConvertedOgStringData[0];
            //                        repackVariables.AsciiUnCmpSize = repackVariables.ConvertedOgStringData[1];
            //                        repackVariables.AsciiCmpSize = repackVariables.ConvertedOgStringData[2];

            //                        // Repack a specific file
            //                        var currentFileInProcess = Path.Combine(repackVariables.OgDirectoryPath, repackVariables.OgFileName);
            //                        if (File.Exists(Path.Combine(whiteExtractedDir, currentFileInProcess)))
            //                        {
            //                            switch (repackVariables.WasCompressed)
            //                            {
            //                                case true:
            //                                    RepackProcesses.CleanOldFile(repackVariables.NewWhiteBinFile, repackVariables.OgFilePos, repackVariables.OgCmpSize);

            //                                    var zlibTmpCmpData = repackVariables.OgFullFilePath.ZlibCompress();
            //                                    var zlibCmpFileSize = (uint)zlibTmpCmpData.Length;

            //                                    if (zlibCmpFileSize < repackVariables.OgCmpSize || zlibCmpFileSize == repackVariables.OgCmpSize)
            //                                    {
            //                                        RepackProcesses.InjectProcess(repackVariables, ref packedAs);
            //                                    }
            //                                    else
            //                                    {
            //                                        RepackProcesses.AppendProcess(repackVariables, ref packedAs);
            //                                    }
            //                                    break;

            //                                case false:
            //                                    RepackProcesses.CleanOldFile(repackVariables.NewWhiteBinFile, repackVariables.OgFilePos, repackVariables.OgUnCmpSize);

            //                                    var dummyFileSize = (uint)new FileInfo(repackVariables.OgFullFilePath).Length;

            //                                    if (dummyFileSize < repackVariables.OgUnCmpSize || dummyFileSize == repackVariables.OgUnCmpSize)
            //                                    {
            //                                        RepackProcesses.InjectProcess(repackVariables, ref packedAs);
            //                                    }
            //                                    else
            //                                    {
            //                                        RepackProcesses.AppendProcess(repackVariables, ref packedAs);
            //                                    }
            //                                    break;
            //                            }

            //                            IOhelpers.LogMessage(repackVariables.RepackState + " " + Path.Combine(repackVariables.NewWhiteBinFileName, repackVariables.RepackLogMsg) + " " + packedAs, logWriter);
            //                        }

            //                        updChunkStringsWriter.Write(repackVariables.AsciiFilePos + ":");
            //                        updChunkStringsWriter.Write(repackVariables.AsciiUnCmpSize + ":");
            //                        updChunkStringsWriter.Write(repackVariables.AsciiCmpSize + ":");
            //                        updChunkStringsWriter.Write(repackVariables.RepackPathInChunk + "\0");

            //                        chunkStringReaderPos = (uint)chunkStringReader.BaseStream.Position;
            //                    }
            //                }
            //            }
            //        }
            //    }

            //    filelistVariables.ChunkFNameCount++;
            //}

            //filelistVariables.DefaultChunksExtDir.IfDirExistsDel();


            //if (filelistVariables.IsEncrypted)
            //{
            //    File.Delete(filelistFile);
            //}

            //RepackFilelist.CreateFilelist(filelistVariables, repackVariables, gameCode);

            //if (filelistVariables.IsEncrypted)
            //{
            //    FilelistProcesses.EncryptProcess(repackVariables, logWriter);
            //    filelistVariables.TmpDcryptFilelistFile.IfFileExistsDel();
            //}
        }
    }
}
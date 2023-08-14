using System;
using System.IO;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.SupportClasses;

namespace WhiteBinTools.UnpackClasses
{
    internal class UnpackTypeB
    {
        public static void UnpackSingle(CmnEnums.GameCodes gameCodeVar, string filelistFileVar, string whiteBinFileVar, string whiteFilePathVar)
        {
            using (var logStream = new FileStream("ProcessLog.txt", FileMode.Append, FileAccess.Write, FileShare.Write))
            {
                using (var logWriter = new StreamWriter(logStream))
                {
                    filelistFileVar.CheckFileExists(logWriter, "Error: Filelist file specified in the argument is missing");
                    whiteBinFileVar.CheckFileExists(logWriter, "Error: Image bin file specified in the argument is missing");

                    var filelistVariables = new FilelistProcesses();
                    var unpackVariables = new UnpackProcess();

                    UnpackProcess.PrepareFilelistVars(filelistVariables, filelistFileVar);
                    UnpackProcess.PrepareBinVars(whiteBinFileVar, unpackVariables);

                    filelistVariables.DefaultChunksExtDir = unpackVariables.ExtractDir + "\\_chunks";
                    filelistVariables.ChunkFile = filelistVariables.DefaultChunksExtDir + "\\chunk_";


                    if (!Directory.Exists(unpackVariables.ExtractDir))
                    {
                        Directory.CreateDirectory(unpackVariables.ExtractDir);
                    }
                    if (Directory.Exists(filelistVariables.DefaultChunksExtDir))
                    {
                        Directory.Delete(filelistVariables.DefaultChunksExtDir, true);
                    }
                    Directory.CreateDirectory(filelistVariables.DefaultChunksExtDir);


                    FilelistProcesses.DecryptProcess(gameCodeVar, filelistVariables, logWriter);

                    using (var filelist = new FileStream(filelistVariables.MainFilelistFile, FileMode.Open, FileAccess.Read))
                    {
                        using (var filelistReader = new BinaryReader(filelist))
                        {
                            FilelistProcesses.GetFilelistOffsets(filelistReader, logWriter, filelistVariables, gameCodeVar);
                            FilelistProcesses.UnpackChunks(filelist, filelistVariables.ChunkFile, filelistVariables);
                        }
                    }

                    if (filelistVariables.IsEncrypted.Equals(true))
                    {
                        filelistVariables.TmpDcryptFilelistFile.IfFileExistsDel();
                        filelistVariables.MainFilelistFile = filelistFileVar;

                        if (filelistVariables.CryptToolPresentBefore.Equals(false))
                        {
                            File.Delete(filelistVariables.MainFilelistDirectory + "\\ffxiiicrypt.exe");
                        }
                    }


                    // Extracting a single file section 
                    filelistVariables.ChunkFNameCount = 0;
                    unpackVariables.CountDuplicates = 0;
                    var hasExtracted = false;
                    for (int ch = 0; ch < filelistVariables.TotalChunks; ch++)
                    {
                        var filesInChunkCount = UnpackProcess.GetFilesInChunkCount(filelistVariables);

                        // Open a chunk file for reading
                        using (var currentChunk = new FileStream(filelistVariables.ChunkFile + filelistVariables.ChunkFNameCount, FileMode.Open, FileAccess.Read))
                        {
                            using (var chunkStringReader = new BinaryReader(currentChunk))
                            {
                                var chunkStringReaderPos = (uint)0;
                                for (int f = 0; f < filesInChunkCount; f++)
                                {
                                    var convertedString = chunkStringReader.BinaryToString(chunkStringReaderPos);

                                    if (convertedString.StartsWith("end"))
                                    {
                                        break;
                                    }

                                    UnpackProcess.PrepareExtraction(convertedString, filelistVariables, unpackVariables.ExtractDir);

                                    // Extract a specific file
                                    if (filelistVariables.MainPath.Equals(whiteFilePathVar))
                                    {
                                        using (var whiteBin = new FileStream(whiteBinFileVar, FileMode.Open, FileAccess.Read))
                                        {
                                            if (!Directory.Exists(unpackVariables.ExtractDir + "\\" + filelistVariables.DirectoryPath))
                                            {
                                                Directory.CreateDirectory(unpackVariables.ExtractDir + "\\" + filelistVariables.DirectoryPath);
                                            }
                                            if (File.Exists(filelistVariables.FullFilePath))
                                            {
                                                File.Delete(filelistVariables.FullFilePath);
                                                unpackVariables.CountDuplicates++;
                                            }

                                            UnpackProcess.UnpackFile(filelistVariables, whiteBin, unpackVariables);
                                        }

                                        hasExtracted = true;

                                        IOhelpers.LogMessage(unpackVariables.UnpackedState + " _" + unpackVariables.ExtractDirName + "\\" + filelistVariables.MainPath, logWriter);
                                    }

                                    chunkStringReaderPos = (uint)chunkStringReader.BaseStream.Position;
                                }
                            }
                        }

                        filelistVariables.ChunkFNameCount++;
                    }

                    Directory.Delete(filelistVariables.DefaultChunksExtDir, true);

                    if (hasExtracted.Equals(false))
                    {
                        IOhelpers.LogMessage("Specified file does not exist. please specify the correct file path", logWriter);
                        IOhelpers.LogMessage("\nFinished extracting file " + unpackVariables.WhiteBinName, logWriter);
                    }
                    else
                    {
                        IOhelpers.LogMessage("\nFinished extracting file " + unpackVariables.WhiteBinName, logWriter);

                        if (unpackVariables.CountDuplicates > 0)
                        {
                            IOhelpers.LogMessage(unpackVariables.CountDuplicates + " duplicate file(s)", logWriter);
                        }
                    }

                    Console.ReadLine();
                }
            }
        }
    }
}
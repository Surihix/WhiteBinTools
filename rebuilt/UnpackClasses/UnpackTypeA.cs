using System;
using System.IO;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.SupportClasses;

namespace WhiteBinTools.UnpackClasses
{
    internal class UnpackTypeA
    {
        public static void UnpackFull(CmnEnums.GameCodes gameCodeVar, string filelistFileVar, string whiteBinFileVar)
        {
            using (var logStream = new FileStream("ProcessLog.txt", FileMode.Append, FileAccess.Write, FileShare.Write))
            {
                using (var logWriter = new StreamWriter(logStream))
                {
                    IOhelpers.CheckFileExists(filelistFileVar, logWriter, "Error: Filelist file specified in the argument is missing");
                    IOhelpers.CheckFileExists(whiteBinFileVar, logWriter, "Error: Image bin file specified in the argument is missing");

                    var filelistVariables = new FilelistProcesses();
                    var unpackVariables = new UnpackProcess();

                    UnpackProcess.PrepareFilelistVars(filelistVariables, filelistFileVar);
                    UnpackProcess.PrepareBinVars(whiteBinFileVar, unpackVariables);

                    filelistVariables.DefaultChunksExtDir = unpackVariables.ExtractDir + "\\_chunks";
                    filelistVariables.ChunkFile = filelistVariables.DefaultChunksExtDir + "\\chunk_";


                    if (Directory.Exists(unpackVariables.ExtractDir))
                    {
                        IOhelpers.LogMessage("Detected previous unpack. deleting....", logWriter);
                        Directory.Delete(unpackVariables.ExtractDir, true);
                        Console.Clear();
                    }

                    Directory.CreateDirectory(unpackVariables.ExtractDir);
                    Directory.CreateDirectory(filelistVariables.DefaultChunksExtDir);


                    FilelistProcesses.CryptProcess(gameCodeVar, filelistVariables, logWriter);

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
                    }


                    // Extracting files section 
                    filelistVariables.ChunkFNameCount = 0;
                    unpackVariables.CountDuplicates = 1;
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

                                    UnpackProcess.PrepareExtraction(convertedString, unpackVariables, unpackVariables.ExtractDir);

                                    // Extract all files
                                    using (var whiteBin = new FileStream(whiteBinFileVar, FileMode.Open, FileAccess.Read))
                                    {
                                        if (!Directory.Exists(unpackVariables.ExtractDir + "\\" + unpackVariables.DirectoryPath))
                                        {
                                            Directory.CreateDirectory(unpackVariables.ExtractDir + "\\" + unpackVariables.DirectoryPath);
                                        }
                                        if (File.Exists(unpackVariables.FullFilePath))
                                        {
                                            File.Delete(unpackVariables.FullFilePath);
                                            unpackVariables.CountDuplicates++;
                                        }

                                        UnpackProcess.UnpackFile(unpackVariables, whiteBin);
                                    }

                                    IOhelpers.LogMessage(unpackVariables.UnpackedState + " _" + unpackVariables.ExtractDirName + "\\" + unpackVariables.MainPath, logWriter);

                                    chunkStringReaderPos = (uint)chunkStringReader.BaseStream.Position;
                                }
                            }
                        }

                        filelistVariables.ChunkFNameCount++;
                    }

                    Directory.Delete(filelistVariables.DefaultChunksExtDir, true);

                    IOhelpers.LogMessage("\nFinished extracting file " + unpackVariables.WhiteBinName, logWriter);

                    if (unpackVariables.CountDuplicates > 1)
                    {
                        IOhelpers.LogMessage(unpackVariables.CountDuplicates + " duplicate file(s)", logWriter);
                    }

                    Console.ReadLine();
                }
            }
        }
    }
}
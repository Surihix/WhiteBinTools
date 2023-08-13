using System;
using System.IO;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.SupportClasses;

namespace WhiteBinTools.UnpackClasses
{
    internal class UnpackTypeC
    {
        public static void UnpackFilelist(CmnEnums.GameCodes gameCodeVar, string filelistFileVar)
        {
            using (var logStream = new FileStream("ProcessLog.txt", FileMode.Append, FileAccess.Write, FileShare.Write))
            {
                using (var logWriter = new StreamWriter(logStream))
                {
                    filelistFileVar.CheckFileExists(logWriter, "Error: Filelist file specified in the argument is missing");

                    var filelistVariables = new FilelistProcesses();
                    var unpackVariables = new UnpackProcess();

                    UnpackProcess.PrepareFilelistVars(filelistVariables, filelistFileVar);

                    var filelistOutName = Path.GetFileName(filelistFileVar);
                    filelistVariables.DefaultChunksExtDir = filelistVariables.MainFilelistDirectory + "\\_chunks";
                    filelistVariables.ChunkFile = filelistVariables.DefaultChunksExtDir + "\\chunk_";
                    var outChunkFile = filelistVariables.MainFilelistDirectory + "\\" + filelistOutName + ".txt";


                    if (Directory.Exists(filelistVariables.DefaultChunksExtDir))
                    {
                        Directory.Delete(filelistVariables.DefaultChunksExtDir, true);
                    }
                    Directory.CreateDirectory(filelistVariables.DefaultChunksExtDir);

                    outChunkFile.IfFileExistsDel();


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


                    // Write all file paths strings
                    // to a text file
                    filelistVariables.ChunkFNameCount = 0;
                    for (int cf = 0; cf < filelistVariables.TotalChunks; cf++)
                    {
                        var filesInChunkCount = UnpackProcess.GetFilesInChunkCount(filelistVariables);

                        // Open a chunk file for reading
                        using (var currentChunk = new FileStream(filelistVariables.ChunkFile + filelistVariables.ChunkFNameCount, FileMode.Open, FileAccess.Read))
                        {
                            using (var chunkStringReader = new BinaryReader(currentChunk))
                            {

                                using (var outChunk = new FileStream(outChunkFile, FileMode.Append, FileAccess.Write))
                                {
                                    using (var outChunkWriter = new StreamWriter(outChunk))
                                    {

                                        var chunkStringReaderPos = (uint)0;
                                        for (int f = 0; f < filesInChunkCount; f++)
                                        {
                                            var convertedString = chunkStringReader.BinaryToString(chunkStringReaderPos);

                                            outChunkWriter.WriteLine(convertedString);

                                            chunkStringReaderPos = (uint)chunkStringReader.BaseStream.Position;
                                        }
                                    }
                                }
                            }
                        }

                        filelistVariables.ChunkFNameCount++;
                    }

                    Directory.Delete(filelistVariables.DefaultChunksExtDir, true);

                    IOhelpers.LogMessage("\nExtracted filepaths to " + filelistOutName + ".txt file", logWriter);

                    Console.ReadLine();
                }
            }
        }
    }
}
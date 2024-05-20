using System.Collections.Generic;
using System.IO;
using System.Text;
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

            using (var newWhiteBinStream = new FileStream(repackVariables.NewWhiteBinFile, FileMode.Append, FileAccess.Write))
            {
                using (var entriesStream = new MemoryStream())
                {
                    entriesStream.Write(filelistVariables.EntriesData, 0, filelistVariables.EntriesData.Length);
                    entriesStream.Seek(0, SeekOrigin.Begin);

                    using (var entriesReader = new BinaryReader(entriesStream))
                    {

                        // Repacking files section
                        long entriesReadPos = 0;
                        var stringData = "";
                        for (int f = 0; f < filelistVariables.TotalFiles; f++)
                        {
                            FilelistProcesses.GetCurrentFileEntry(gameCode, entriesReader, entriesReadPos, filelistVariables);
                            entriesReadPos += 8;

                            RepackProcesses.GetPackedState(filelistVariables.PathString, repackVariables, extractedDir);

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

                            IOhelpers.LogMessage(repackVariables.RepackState + " " + Path.Combine(repackVariables.NewWhiteBinFileName, repackVariables.RepackLogMsg), logWriter);
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

            IOhelpers.LogMessage("\nFinished repacking files into " + "\"" + repackVariables.NewWhiteBinFileName + "\"", logWriter);
        }
    }
}
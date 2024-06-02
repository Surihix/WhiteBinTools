using System.Collections.Generic;
using System.IO;
using System.Text;
using WhiteBinTools.Filelist;
using WhiteBinTools.Support;
using static WhiteBinTools.Support.ProgramEnums;

namespace WhiteBinTools.Unpack
{
    internal class UnpackTypeD
    {
        public static void UnpackFilelist(GameCodes gameCode, string filelistFile, StreamWriter logWriter)
        {
            filelistFile.CheckFileExists(logWriter, "Error: Filelist file specified in the argument is missing");

            var filelistVariables = new FilelistVariables();

            FilelistProcesses.PrepareFilelistVars(filelistVariables, filelistFile);

            var filelistOutName = Path.GetFileName(filelistFile);
            var extractedFilelistDir = Path.Combine(filelistVariables.MainFilelistDirectory, "_" + filelistOutName);
            var encHeaderFile = Path.Combine(extractedFilelistDir, "~EncryptionHeader_(DON'T DELETE)");
            var countsFile = Path.Combine(extractedFilelistDir, "~Counts.txt");
            var outChunkFile = Path.Combine(extractedFilelistDir, "Chunk_");

            extractedFilelistDir.IfDirExistsDel();
            Directory.CreateDirectory(extractedFilelistDir);


            FilelistCrypto.DecryptProcess(gameCode, filelistVariables, logWriter);

            using (var filelistStream = new FileStream(filelistVariables.MainFilelistFile, FileMode.Open, FileAccess.Read))
            {
                using (var filelistReader = new BinaryReader(filelistStream))
                {
                    FilelistChunksPrep.GetFilelistOffsets(filelistReader, logWriter, filelistVariables);
                    FilelistChunksPrep.BuildChunks(filelistStream, filelistVariables);

                    if (filelistVariables.IsEncrypted)
                    {
                        using (var encHeader = new FileStream(encHeaderFile, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            filelistStream.Seek(0, SeekOrigin.Begin);
                            filelistStream.CopyStreamTo(encHeader, 32, false);
                        }
                    }

                    using (var countsStream = new StreamWriter(countsFile, true))
                    {
                        countsStream.WriteLine(filelistVariables.TotalFiles);
                        countsStream.WriteLine(filelistVariables.TotalChunks);
                    }
                }
            }

            if (gameCode.Equals(GameCodes.ff132))
            {
                filelistVariables.CurrentChunkNumber = -1;
            }

            if (filelistVariables.IsEncrypted)
            {
                filelistVariables.TmpDcryptFilelistFile.IfFileExistsDel();
                filelistVariables.MainFilelistFile = filelistFile;
            }

            // Build an empty dictionary
            var outChunksDict = new Dictionary<int, List<string>>();
            for (int c = 0; c < filelistVariables.TotalChunks; c++)
            {
                var chunkDataList = new List<string>();
                outChunksDict.Add(c, chunkDataList);
            }


            // Collect all of the chunk data into
            // the empty dictionary
            using (var entriesStream = new MemoryStream())
            {
                entriesStream.Write(filelistVariables.EntriesData, 0, filelistVariables.EntriesData.Length);
                entriesStream.Seek(0, SeekOrigin.Begin);

                using (var entriesReader = new BinaryReader(entriesStream))
                {

                    // Process each file entry from 
                    // the entry section
                    long entriesReadPos = 0;
                    var stringData = "";

                    for (int f = 0; f < filelistVariables.TotalFiles; f++)
                    {
                        FilelistProcesses.GetCurrentFileEntry(gameCode, entriesReader, entriesReadPos, filelistVariables);
                        entriesReadPos += 8;

                        stringData = "";

                        if (gameCode.Equals(GameCodes.ff132))
                        {
                            stringData += filelistVariables.FileCode + "|";
                            stringData += filelistVariables.ChunkNumber + "|";
                            stringData += filelistVariables.UnkEntryVal + "|";
                            stringData += filelistVariables.PathString;

                            outChunksDict[filelistVariables.CurrentChunkNumber].Add(stringData);
                        }
                        else
                        {
                            stringData += filelistVariables.FileCode + "|";
                            stringData += filelistVariables.ChunkNumber + "|";
                            stringData += filelistVariables.PathString;

                            outChunksDict[filelistVariables.ChunkNumber].Add(stringData);
                        }
                    }
                }
            }

            // Write all of the collected data from
            // the dictionary into multiple txt
            // files
            for (int d = 0; d < filelistVariables.TotalChunks; d++)
            {
                using (var chunkWriter = new StreamWriter(outChunkFile + d + ".txt", true, new UTF8Encoding(false)))
                {
                    foreach (var stringData in outChunksDict[d])
                    {
                        chunkWriter.WriteLine(stringData);
                    }
                }
            }

            logWriter.LogMessage("\nFinished unpacking " + "\"" + filelistOutName + "\"");
        }
    }
}
using System.IO;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.SupportClasses;

namespace WhiteBinTools
{
    internal class UnpackTypeD
    {
        public static void UnpackFilelist(CmnEnums.GameCodes gameCodeVar, string filelistFileVar, StreamWriter logWriter)
        {
            var filelistVariables = new FilelistProcesses();

            FilelistProcesses.PrepareFilelistVars(filelistVariables, filelistFileVar);

            var filelistOutName = Path.GetFileName(filelistFileVar);
            filelistVariables.DefaultChunksExtDir = filelistVariables.MainFilelistDirectory + "\\_chunks";
            filelistVariables.ChunkFile = filelistVariables.DefaultChunksExtDir + "\\chunk_";
            var extractedFilelistDir = filelistVariables.MainFilelistDirectory + "\\_" + filelistOutName;
            var outChunkFile = extractedFilelistDir + "\\Chunks\\Chunk_";
            var outEntriesFile = extractedFilelistDir + "\\Chunk_entries\\Chunk_";


            filelistVariables.DefaultChunksExtDir.IfDirExistsDel();
            Directory.CreateDirectory(filelistVariables.DefaultChunksExtDir);

            extractedFilelistDir.IfDirExistsDel();
            Directory.CreateDirectory(extractedFilelistDir);

            (extractedFilelistDir + "\\Chunks").IfFileExistsDel();
            Directory.CreateDirectory(extractedFilelistDir + "\\Chunks");

            (extractedFilelistDir + "\\Chunk_entries").IfFileExistsDel();
            Directory.CreateDirectory(extractedFilelistDir + "\\Chunk_entries");


            FilelistProcesses.DecryptProcess(gameCodeVar, filelistVariables, logWriter);

            using (var filelist = new FileStream(filelistVariables.MainFilelistFile, FileMode.Open, FileAccess.Read))
            {
                using (var filelistReader = new BinaryReader(filelist))
                {
                    FilelistProcesses.GetFilelistOffsets(filelistReader, logWriter, filelistVariables);
                    FilelistProcesses.UnpackChunks(filelist, filelistVariables.ChunkFile, filelistVariables);

                    using (var entriesFile = new FileStream(extractedFilelistDir + "\\_entries", FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        var entriesStartPos = 12;
                        if (filelistVariables.IsEncrypted.Equals(true))
                        {
                            entriesStartPos += 32;

                            using (var encHeader = new FileStream(extractedFilelistDir + "\\EncryptionHeader_(DON'T EDIT)", FileMode.OpenOrCreate, FileAccess.Write))
                            {
                                filelist.ExtendedCopyTo(encHeader, 0, 32);
                            }
                        }

                        filelist.ExtendedCopyTo(entriesFile, entriesStartPos, filelistVariables.ChunkInfoSectionOffset);
                    }

                    File.WriteAllText(extractedFilelistDir + "\\FileCount.txt", filelistVariables.TotalFiles.ToString());
                }
            }


            if (filelistVariables.IsEncrypted.Equals(true))
            {
                filelistVariables.TmpDcryptFilelistFile.IfFileExistsDel();
                filelistVariables.MainFilelistFile = filelistFileVar;
            }


            // Write all file paths strings
            // to a text files and the entries
            // data into bin files
            filelistVariables.ChunkFNameCount = 0;
            uint entriesReadStartPos = 0;
            for (int cf = 0; cf < filelistVariables.TotalChunks; cf++)
            {
                var filesInChunkCount = FilelistProcesses.GetFilesInChunkCount(filelistVariables.ChunkFile + filelistVariables.ChunkFNameCount);

                using (var filelistEntries = new FileStream(extractedFilelistDir + "\\_entries", FileMode.OpenOrCreate, FileAccess.Read))
                {

                    // Open a chunk file for reading
                    using (var currentChunk = new FileStream(filelistVariables.ChunkFile + filelistVariables.ChunkFNameCount, FileMode.Open, FileAccess.Read))
                    {
                        using (var chunkStringReader = new BinaryReader(currentChunk))
                        {

                            // Open an empty txt file for writing the chunk data
                            using (var outChunk = new FileStream(outChunkFile + filelistVariables.ChunkFNameCount + ".txt", FileMode.Append, FileAccess.Write))
                            {
                                using (var outChunkWriter = new StreamWriter(outChunk))
                                {

                                    // Open an empty file for writing the entries data
                                    using (var outEntries = new FileStream(outEntriesFile + filelistVariables.ChunkFNameCount + "_entries.bin", FileMode.OpenOrCreate, FileAccess.Write))
                                    {
                                        using (var outEntriesWriter = new BinaryWriter(outEntries))
                                        {

                                            var chunkStringReaderPos = (uint)0;
                                            for (int f = 0; f < filesInChunkCount; f++)
                                            {
                                                var convertedString = chunkStringReader.BinaryToString(chunkStringReaderPos);

                                                if (convertedString.StartsWith("end"))
                                                {
                                                    outChunkWriter.Write(convertedString + "\0");
                                                    filesInChunkCount--;
                                                    break;
                                                }

                                                outChunkWriter.Write(convertedString + "\0");

                                                chunkStringReaderPos = (uint)chunkStringReader.BaseStream.Position;
                                            }

                                            filelistEntries.ExtendedCopyTo(outEntries, entriesReadStartPos, filesInChunkCount * 8);
                                            entriesReadStartPos += filesInChunkCount * 8;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    filelistVariables.ChunkFNameCount++;
                }
            }

            Directory.Delete(filelistVariables.DefaultChunksExtDir, true);
            (extractedFilelistDir + "\\_entries").IfFileExistsDel();

            IOhelpers.LogMessage("\nFinished unpacking " + filelistOutName, logWriter);
        }
    }
}
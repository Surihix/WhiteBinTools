using System.IO;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.SupportClasses;
using static WhiteBinTools.SupportClasses.ProgramEnums;

namespace WhiteBinTools.UnpackClasses
{
    internal class UnpackTypeC
    {
        public static void UnpackFilelistPaths(GameCodes gameCode, string filelistFile, StreamWriter logWriter)
        {
            filelistFile.CheckFileExists(logWriter, "Error: Filelist file specified in the argument is missing");

            var filelistVariables = new FilelistVariables();

            FilelistProcesses.PrepareFilelistVars(filelistVariables, filelistFile);

            var filelistOutName = Path.GetFileName(filelistFile);
            filelistVariables.DefaultChunksExtDir = filelistVariables.MainFilelistDirectory + "\\_chunks";
            filelistVariables.ChunkFile = filelistVariables.DefaultChunksExtDir + "\\chunk_";
            var outChunkFile = filelistVariables.MainFilelistDirectory + "\\" + filelistOutName + ".txt";


            filelistVariables.DefaultChunksExtDir.IfDirExistsDel();
            Directory.CreateDirectory(filelistVariables.DefaultChunksExtDir);

            outChunkFile.IfFileExistsDel();


            FilelistProcesses.DecryptProcess(gameCode, filelistVariables, logWriter);

            using (var filelistStream = new FileStream(filelistVariables.MainFilelistFile, FileMode.Open, FileAccess.Read))
            {
                using (var filelistReader = new BinaryReader(filelistStream))
                {
                    FilelistChunksPrep.GetFilelistOffsets(filelistReader, logWriter, filelistVariables);
                    FilelistChunksPrep.UnpackChunks(filelistStream, filelistVariables.ChunkFile, filelistVariables);
                }
            }

            if (filelistVariables.IsEncrypted)
            {
                filelistVariables.TmpDcryptFilelistFile.IfFileExistsDel();
                filelistVariables.MainFilelistFile = filelistFile;
            }


            // Write all file paths strings
            // to a text file
            filelistVariables.ChunkFNameCount = 0;
            for (int cf = 0; cf < filelistVariables.TotalChunks; cf++)
            {
                var filesInChunkCount = FilelistProcesses.GetFilesInChunkCount(filelistVariables.ChunkFile + filelistVariables.ChunkFNameCount);

                // Open a chunk file for reading
                using (var currentChunkStream = new FileStream(filelistVariables.ChunkFile + filelistVariables.ChunkFNameCount, FileMode.Open, FileAccess.Read))
                {
                    using (var chunkStringReader = new BinaryReader(currentChunkStream))
                    {

                        using (var outChunkStream = new FileStream(outChunkFile, FileMode.Append, FileAccess.Write))
                        {
                            using (var outChunkWriter = new StreamWriter(outChunkStream))
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

            IOhelpers.LogMessage("\nExtracted filepaths to " + "\"" + filelistOutName + "\"" + ".txt file", logWriter);
        }
    }
}
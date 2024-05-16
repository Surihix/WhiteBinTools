using System.IO;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.SupportClasses;
using static WhiteBinTools.SupportClasses.ProgramEnums;

namespace WhiteBinTools.UnpackClasses
{
    internal class UnpackTypeA
    {
        public static void UnpackFull(GameCodes gameCode, string filelistFile, string whiteBinFile, StreamWriter logWriter)
        {
            filelistFile.CheckFileExists(logWriter, "Error: Filelist file specified in the argument is missing");
            whiteBinFile.CheckFileExists(logWriter, "Error: Image bin file specified in the argument is missing");

            var filelistVariables = new FilelistVariables();
            var unpackVariables = new UnpackVariables();

            FilelistProcesses.PrepareFilelistVars(filelistVariables, filelistFile);
            UnpackProcess.PrepareBinVars(whiteBinFile, unpackVariables);

            filelistVariables.DefaultChunksExtDir = Path.Combine(unpackVariables.ExtractDir, "_chunks");
            filelistVariables.ChunkFile = Path.Combine(filelistVariables.DefaultChunksExtDir, "chunk_");

            if (Directory.Exists(unpackVariables.ExtractDir))
            {
                IOhelpers.LogMessage("Detected previous unpack. deleting....", logWriter);
                unpackVariables.ExtractDir.IfDirExistsDel();
            }

            Directory.CreateDirectory(unpackVariables.ExtractDir);
            Directory.CreateDirectory(filelistVariables.DefaultChunksExtDir);


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


            using (var whiteBinStream = new FileStream(whiteBinFile, FileMode.Open, FileAccess.Read))
            {
                // Extracting files section 
                filelistVariables.ChunkFNameCount = 0;
                unpackVariables.CountDuplicates = 0;

                for (int ch = 0; ch < filelistVariables.TotalChunks; ch++)
                {
                    var filesInChunkCount = FilelistProcesses.GetFilesInChunkCount(filelistVariables.ChunkFile + filelistVariables.ChunkFNameCount);

                    // Open a chunk file for reading
                    using (var currentChunkStream = new FileStream(filelistVariables.ChunkFile + filelistVariables.ChunkFNameCount, FileMode.Open, FileAccess.Read))
                    {
                        using (var chunkStringReader = new BinaryReader(currentChunkStream))
                        {

                            var chunkStringReaderPos = (uint)0;
                            for (int f = 0; f < filesInChunkCount; f++)
                            {
                                chunkStringReader.BaseStream.Position = chunkStringReaderPos;
                                var convertedString = chunkStringReader.ReadStringTillNull();

                                if (convertedString == "end" || convertedString == " " || convertedString == null)
                                {
                                    break;
                                }

                                UnpackProcess.PrepareExtraction(convertedString, filelistVariables, unpackVariables.ExtractDir);

                                // Extract all files
                                if (!Directory.Exists(Path.Combine(unpackVariables.ExtractDir, filelistVariables.DirectoryPath)))
                                {
                                    Directory.CreateDirectory(Path.Combine(unpackVariables.ExtractDir, filelistVariables.DirectoryPath));
                                }
                                if (File.Exists(filelistVariables.FullFilePath))
                                {
                                    File.Delete(filelistVariables.FullFilePath);
                                    unpackVariables.CountDuplicates++;
                                }

                                UnpackProcess.UnpackFile(filelistVariables, whiteBinStream, unpackVariables);

                                IOhelpers.LogMessage(unpackVariables.UnpackedState + " _" + Path.Combine(unpackVariables.ExtractDirName, filelistVariables.MainPath), logWriter);

                                chunkStringReaderPos = (uint)chunkStringReader.BaseStream.Position;
                            }
                        }
                    }

                    filelistVariables.ChunkFNameCount++;
                }
            }

            Directory.Delete(filelistVariables.DefaultChunksExtDir, true);

            IOhelpers.LogMessage("\nFinished extracting file " + "\"" + unpackVariables.WhiteBinName + "\"", logWriter);

            if (unpackVariables.CountDuplicates > 1)
            {
                IOhelpers.LogMessage(unpackVariables.CountDuplicates + " duplicate file(s)", logWriter);
            }
        }
    }
}
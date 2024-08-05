using System.IO;
using WhiteBinTools.Filelist;
using WhiteBinTools.Support;
using static WhiteBinTools.Support.ProgramEnums;

namespace WhiteBinTools.Unpack
{
    internal class UnpackTypeA
    {
        public static void UnpackFull(GameCodes gameCode, string filelistFile, string whiteBinFile, StreamWriter logWriter)
        {
            IOhelpers.CheckFileExists(filelistFile, logWriter, "Error: Filelist file specified in the argument is missing");
            IOhelpers.CheckFileExists(whiteBinFile, logWriter, "Error: Image bin file specified in the argument is missing");

            var filelistVariables = new FilelistVariables();
            var unpackVariables = new UnpackVariables();

            FilelistProcesses.PrepareFilelistVars(filelistVariables, filelistFile);
            UnpackProcesses.PrepareBinVars(whiteBinFile, unpackVariables);

            if (Directory.Exists(unpackVariables.ExtractDir))
            {
                logWriter.LogMessage("Detected previous unpack. deleting....");
                IOhelpers.IfDirExistsDel(unpackVariables.ExtractDir);
            }

            Directory.CreateDirectory(unpackVariables.ExtractDir);


            FilelistCrypto.DecryptProcess(gameCode, filelistVariables, logWriter);

            using (var filelistStream = new FileStream(filelistVariables.MainFilelistFile, FileMode.Open, FileAccess.Read))
            {
                using (var filelistReader = new BinaryReader(filelistStream))
                {
                    FilelistChunksPrep.GetFilelistOffsets(filelistReader, logWriter, filelistVariables);
                    FilelistChunksPrep.BuildChunks(filelistStream, filelistVariables);
                }
            }

            if (gameCode.Equals(GameCodes.ff132))
            {
                filelistVariables.CurrentChunkNumber = -1;
            }

            if (filelistVariables.IsEncrypted)
            {
                IOhelpers.IfFileExistsDel(filelistVariables.TmpDcryptFilelistFile);
                filelistVariables.MainFilelistFile = filelistFile;
            }


            using (var whiteBinStream = new FileStream(whiteBinFile, FileMode.Open, FileAccess.Read))
            {
                using (var entriesStream = new MemoryStream())
                {
                    entriesStream.Write(filelistVariables.EntriesData, 0, filelistVariables.EntriesData.Length);
                    entriesStream.Seek(0, SeekOrigin.Begin);

                    using (var entriesReader = new BinaryReader(entriesStream))
                    {

                        // Extracting files section 
                        long entriesReadPos = 0;
                        unpackVariables.CountDuplicates = 0;

                        for (int f = 0; f < filelistVariables.TotalFiles; f++)
                        {
                            FilelistProcesses.GetCurrentFileEntry(gameCode, entriesReader, entriesReadPos, filelistVariables);
                            entriesReadPos += 8;

                            UnpackProcesses.PrepareExtraction(filelistVariables.PathString, filelistVariables, unpackVariables.ExtractDir);

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

                            UnpackProcesses.UnpackFile(filelistVariables, whiteBinStream, unpackVariables);

                            logWriter.LogMessage(unpackVariables.UnpackedState + " _" + Path.Combine(unpackVariables.ExtractDirName, filelistVariables.MainPath));
                        }
                    }
                }
            }


            logWriter.LogMessage("\nFinished unpacking " + "\"" + unpackVariables.WhiteBinName + "\"");

            if (unpackVariables.CountDuplicates > 1)
            {
                logWriter.LogMessage(unpackVariables.CountDuplicates + " duplicate file(s)");
            }
        }
    }
}
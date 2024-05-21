using System.IO;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.SupportClasses;
using static WhiteBinTools.SupportClasses.ProgramEnums;

namespace WhiteBinTools.UnpackClasses
{
    internal class UnpackTypeC
    {
        public static void UnpackMultiple(GameCodes gameCode, string filelistFile, string whiteBinFile, string whiteVirtualDirPath, StreamWriter logWriter)
        {
            whiteVirtualDirPath = whiteVirtualDirPath.Replace("*", "");

            filelistFile.CheckFileExists(logWriter, "Error: Filelist file specified in the argument is missing");
            whiteBinFile.CheckFileExists(logWriter, "Error: Image bin file specified in the argument is missing");

            var filelistVariables = new FilelistVariables();
            var unpackVariables = new UnpackVariables();

            FilelistProcesses.PrepareFilelistVars(filelistVariables, filelistFile);
            UnpackProcesses.PrepareBinVars(whiteBinFile, unpackVariables);

            if (!Directory.Exists(unpackVariables.ExtractDir))
            {
                Directory.CreateDirectory(unpackVariables.ExtractDir);
            }


            FilelistProcesses.DecryptProcess(gameCode, filelistVariables, logWriter);

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
                filelistVariables.TmpDcryptFilelistFile.IfFileExistsDel();
                filelistVariables.MainFilelistFile = filelistFile;
            }


            var hasExtracted = false;

            using (var entriesStream = new MemoryStream())
            {
                entriesStream.Write(filelistVariables.EntriesData, 0, filelistVariables.EntriesData.Length);
                entriesStream.Seek(0, SeekOrigin.Begin);

                using (var entriesReader = new BinaryReader(entriesStream))
                {

                    // Extracting files section 
                    long entriesReadPos = 0;
                    unpackVariables.CountDuplicates = 0;
                    string[] currentPathDataArray;
                    string assembledDir;

                    for (int f = 0; f < filelistVariables.TotalFiles; f++)
                    {
                        FilelistProcesses.GetCurrentFileEntry(gameCode, entriesReader, entriesReadPos, filelistVariables);
                        entriesReadPos += 8;

                        UnpackProcesses.PrepareExtraction(filelistVariables.PathString, filelistVariables, unpackVariables.ExtractDir);

                        // Extract files from a specific dir
                        currentPathDataArray = filelistVariables.MainPath.Split('\\');
                        assembledDir = string.Empty;

                        foreach (var dir in currentPathDataArray)
                        {
                            assembledDir += dir;
                            assembledDir += "\\";

                            if (assembledDir == whiteVirtualDirPath)
                            {
                                break;
                            }
                        }

                        if (assembledDir == whiteVirtualDirPath)
                        {
                            using (var whiteBinStream = new FileStream(whiteBinFile, FileMode.Open, FileAccess.Read))
                            {
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
                            }

                            hasExtracted = true;

                            IOhelpers.LogMessage(unpackVariables.UnpackedState + " _" + Path.Combine(unpackVariables.ExtractDirName, filelistVariables.MainPath), logWriter);
                        }
                    }
                }
            }

            if (!hasExtracted)
            {
                IOhelpers.LogMessage("Specified directory does not exist. please specify the correct directory", logWriter);
                IOhelpers.LogMessage("\nFinished extracting files from " + "\"" + unpackVariables.WhiteBinName + "\"", logWriter);
            }
            else
            {
                IOhelpers.LogMessage("\nFinished extracting files from " + "\"" + unpackVariables.WhiteBinName + "\"", logWriter);

                if (unpackVariables.CountDuplicates > 0)
                {
                    IOhelpers.LogMessage(unpackVariables.CountDuplicates + " duplicate file(s)", logWriter);
                }
            }
        }
    }
}
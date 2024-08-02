using System.IO;
using WhiteBinTools.Filelist;
using WhiteBinTools.Support;
using static WhiteBinTools.Support.ProgramEnums;

namespace WhiteBinTools.Unpack
{
    internal class UnpackTypePaths
    {
        public static void UnpackFilelistPaths(GameCodes gameCode, string filelistFile, StreamWriter logWriter)
        {
            filelistFile.CheckFileExists(logWriter, "Error: Filelist file specified in the argument is missing");

            var filelistVariables = new FilelistVariables();

            FilelistProcesses.PrepareFilelistVars(filelistVariables, filelistFile);

            var filelistOutName = Path.GetFileNameWithoutExtension(filelistFile);
            var outTxtFile = Path.Combine(filelistVariables.MainFilelistDirectory, filelistOutName + ".txt");

            outTxtFile.IfFileExistsDel();


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
                filelistVariables.TmpDcryptFilelistFile.IfFileExistsDel();
                filelistVariables.MainFilelistFile = filelistFile;
            }


            // Write all file paths strings
            // to a text file
            using (var outchunkWriter = new StreamWriter(outTxtFile, true))
            {
                using (var entriesStream = new MemoryStream())
                {
                    entriesStream.Write(filelistVariables.EntriesData, 0, filelistVariables.EntriesData.Length);
                    entriesStream.Seek(0, SeekOrigin.Begin);

                    using (var entriesReader = new BinaryReader(entriesStream))
                    {

                        // Process each file entry from 
                        // the entry section
                        long entriesReadPos = 0;
                        for (int f = 0; f < filelistVariables.TotalFiles; f++)
                        {
                            FilelistProcesses.GetCurrentFileEntry(gameCode, entriesReader, entriesReadPos, filelistVariables);
                            entriesReadPos += 8;

                            outchunkWriter.WriteLine(filelistVariables.PathString);
                        }

                        outchunkWriter.WriteLine("end");
                    }
                }
            }

            logWriter.LogMessage("\nFinished writing filepaths to " + "\"" + filelistOutName + "\"" + ".txt file");
        }
    }
}
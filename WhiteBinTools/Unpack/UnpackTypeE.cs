using System.IO;
using WhiteBinTools.Filelist;
using WhiteBinTools.Support;
using static WhiteBinTools.Support.ProgramEnums;

namespace WhiteBinTools.Unpack
{
    internal class UnpackTypeE
    {
        public static void UnpackFilelistJson(GameCodes gameCode, string filelistFile, StreamWriter logWriter)
        {
            filelistFile.CheckFileExists(logWriter, "Error: Filelist file specified in the argument is missing");

            var filelistVariables = new FilelistVariables();

            FilelistProcesses.PrepareFilelistVars(filelistVariables, filelistFile);

            var filelistOutName = Path.GetFileName(filelistFile);
            var outJsonFile = Path.Combine(filelistVariables.MainFilelistDirectory, filelistOutName + ".json");

            outJsonFile.IfFileExistsDel();


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

            var seedA = ulong.MinValue;
            var seedB = ulong.MinValue;
            var encTag = uint.MinValue;
            if (filelistVariables.IsEncrypted)
            {
                filelistVariables.TmpDcryptFilelistFile.IfFileExistsDel();
                filelistVariables.MainFilelistFile = filelistFile;

                using (var encDataReader = new BinaryReader(File.Open(filelistFile, FileMode.Open, FileAccess.Read)))
                {
                    encDataReader.BaseStream.Position = 0;
                    seedA = encDataReader.ReadUInt64();
                    seedB = encDataReader.ReadUInt64();

                    encDataReader.BaseStream.Position += 4;
                    encTag = encDataReader.ReadUInt32();
                }
            }


            // Write all file paths strings
            // to a json file
            using (var outJsonWriter = new StreamWriter(outJsonFile, true))
            {
                outJsonWriter.WriteLine("{");
                if (filelistVariables.IsEncrypted)
                {
                    outJsonWriter.WriteLine($"  \"encrypted\": true,");
                    outJsonWriter.WriteLine($"  \"seedA\": {seedA},");
                    outJsonWriter.WriteLine($"  \"seedB\": {seedB},");
                    outJsonWriter.WriteLine($"  \"encryptionTag\": {encTag},");
                }
                else
                {
                    outJsonWriter.WriteLine($"  \"encrypted\": false,");
                }
                outJsonWriter.WriteLine($"  \"fileCount\": {filelistVariables.TotalFiles},");
                outJsonWriter.WriteLine($"  \"chunkCount\": {filelistVariables.TotalChunks},");
                outJsonWriter.WriteLine("  \"data\": {");
                outJsonWriter.WriteLine("             \"Chunk_0\": [");

                using (var entriesStream = new MemoryStream())
                {
                    entriesStream.Write(filelistVariables.EntriesData, 0, filelistVariables.EntriesData.Length);
                    entriesStream.Seek(0, SeekOrigin.Begin);

                    using (var entriesReader = new BinaryReader(entriesStream))
                    {

                        // Process each file entry from 
                        // the entry section
                        int chunkNumberJson = -1;
                        long entriesReadPos = 0;
                        var lastChunkNumber = (int)filelistVariables.TotalChunks - 1;
                        var currentPathData = new string[4];
                        var lastFile = filelistVariables.TotalFiles - 1;

                        for (int f = 0; f < filelistVariables.TotalFiles; f++)
                        {
                            FilelistProcesses.GetCurrentFileEntry(gameCode, entriesReader, entriesReadPos, filelistVariables);
                            entriesReadPos += 8;

                            if (gameCode.Equals(GameCodes.ff131))
                            {
                                DetermineArrayClosure(chunkNumberJson, filelistVariables.ChunkNumber, outJsonWriter, f, filelistVariables.TotalFiles);
                                chunkNumberJson = filelistVariables.ChunkNumber;

                                outJsonWriter.Write("               { \"fileCode\": ");
                                outJsonWriter.Write($"{filelistVariables.FileCode}, ");
                            }
                            else
                            {
                                DetermineArrayClosure(chunkNumberJson, filelistVariables.CurrentChunkNumber, outJsonWriter, f, filelistVariables.TotalFiles);
                                chunkNumberJson = filelistVariables.CurrentChunkNumber;

                                outJsonWriter.Write("               { \"fileCode\": ");
                                outJsonWriter.Write($"{filelistVariables.FileCode}, ");
                                outJsonWriter.Write($"\"unkValue\": {filelistVariables.UnkEntryVal}, ");
                            }

                            outJsonWriter.Write($"\"filePath\": \"{filelistVariables.PathString}\"");

                            if (f == lastFile)
                            {
                                outJsonWriter.WriteLine(" }");
                                outJsonWriter.WriteLine("             ]");
                            }
                        }
                    }
                }

                outJsonWriter.WriteLine("  }");
                outJsonWriter.WriteLine("}");
            }

            logWriter.LogMessage("\n\nFinished writing filelist data to " + "\"" + filelistOutName + "\"" + ".json file");
        }


        private static void DetermineArrayClosure(int chunkNumberJson, int chunkNumberProcess, StreamWriter outJsonWriter, int f, uint totalFileCount)
        {
            if (chunkNumberJson != chunkNumberProcess)
            {
                if (chunkNumberJson != -1)
                {
                    outJsonWriter.WriteLine(" }");
                    outJsonWriter.WriteLine("             ],");
                    outJsonWriter.WriteLine();
                    outJsonWriter.WriteLine($"             \"Chunk_{chunkNumberProcess}\": [");
                }
            }
            else
            {
                outJsonWriter.WriteLine(" },");
            }
        }
    }
}
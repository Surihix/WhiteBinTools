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
            IOhelpers.CheckFileExists(filelistFile, logWriter, "Error: Filelist file specified in the argument is missing");

            var filelistVariables = new FilelistVariables();

            FilelistProcesses.PrepareFilelistVars(filelistVariables, filelistFile);

            FilelistCrypto.DecryptProcess(gameCode, filelistVariables, logWriter);

            using (var filelistStream = new FileStream(filelistVariables.MainFilelistFile, FileMode.Open, FileAccess.Read))
            {
                using (var filelistReader = new BinaryReader(filelistStream))
                {
                    FilelistChunksPrep.GetFilelistOffsets(filelistReader, logWriter, filelistVariables);
                    FilelistChunksPrep.BuildChunks(filelistStream, filelistVariables);
                }
            }

            if (gameCode == GameCodes.ff132)
            {
                filelistVariables.CurrentChunkNumber = -1;
            }

            if (filelistVariables.IsEncrypted)
            {
                IOhelpers.IfFileExistsDel(filelistVariables.TmpDcryptFilelistFile);
                filelistVariables.MainFilelistFile = filelistFile;

                using (var encDataReader = new BinaryReader(File.Open(filelistFile, FileMode.Open, FileAccess.Read)))
                {
                    encDataReader.BaseStream.Position = 0;
                    filelistVariables.SeedA = encDataReader.ReadUInt64();
                    filelistVariables.SeedB = encDataReader.ReadUInt64();

                    encDataReader.BaseStream.Position += 4;
                    filelistVariables.EncTag = encDataReader.ReadUInt32();
                }
            }

            var filelistOutName = Path.GetFileName(filelistFile);
            var outJsonFile = Path.Combine(filelistVariables.MainFilelistDirectory, filelistOutName + ".json");

            IOhelpers.IfFileExistsDel(outJsonFile);


            // Write all file paths strings
            // to a json file
            using (var outJsonWriter = new StreamWriter(outJsonFile, true))
            {
                outJsonWriter.WriteLine("{");

                if (gameCode == GameCodes.ff132)
                {
                    outJsonWriter.WriteLine($"  \"encrypted\": {filelistVariables.IsEncrypted.ToString().ToLower()},");

                    if (filelistVariables.IsEncrypted)
                    {
                        outJsonWriter.WriteLine($"  \"seedA\": {filelistVariables.SeedA},");
                        outJsonWriter.WriteLine($"  \"seedB\": {filelistVariables.SeedB},");
                        outJsonWriter.WriteLine($"  \"encryptionTag(DO_NOT_CHANGE)\": {filelistVariables.EncTag},");
                    }
                }

                outJsonWriter.WriteLine($"  \"fileCount\": {filelistVariables.TotalFiles},");
                outJsonWriter.WriteLine($"  \"chunkCount\": {filelistVariables.TotalChunks},");
                outJsonWriter.WriteLine("  \"data\": {");
                outJsonWriter.WriteLine("    \"Chunk_0\": [");

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

                            if (gameCode == GameCodes.ff131)
                            {
                                DetermineArrayClosure(chunkNumberJson, filelistVariables.ChunkNumber, outJsonWriter);
                                chunkNumberJson = filelistVariables.ChunkNumber;

                                outJsonWriter.WriteLine("      {");
                                outJsonWriter.WriteLine("        \"fileCode\": " + $"{filelistVariables.FileCode},");
                            }
                            else
                            {
                                DetermineArrayClosure(chunkNumberJson, filelistVariables.CurrentChunkNumber, outJsonWriter);
                                chunkNumberJson = filelistVariables.CurrentChunkNumber;

                                outJsonWriter.WriteLine("      {");
                                outJsonWriter.WriteLine("        \"fileCode\": " + $"{filelistVariables.FileCode},");
                                outJsonWriter.WriteLine("        \"fileTypeID\": " + $"{filelistVariables.FileTypeID},");
                            }

                            outJsonWriter.WriteLine("        \"filePath\": " + $"\"{filelistVariables.PathString}\"");

                            if (f == lastFile)
                            {
                                outJsonWriter.WriteLine("      }");
                                outJsonWriter.WriteLine("    ]");
                            }
                        }
                    }
                }

                outJsonWriter.WriteLine("  }");
                outJsonWriter.Write("}");
            }

            logWriter.LogMessage($"\n\nFinished writing filelist data to \"{Path.GetFileName(outJsonFile)}\"");
        }


        private static void DetermineArrayClosure(int chunkNumberJson, int chunkNumberProcess, StreamWriter outJsonWriter)
        {
            if (chunkNumberJson != chunkNumberProcess)
            {
                if (chunkNumberJson != -1)
                {
                    outJsonWriter.WriteLine("      }");
                    outJsonWriter.WriteLine("    ],");
                    outJsonWriter.WriteLine($"    \"Chunk_{chunkNumberProcess}\": [");
                }
            }
            else
            {
                outJsonWriter.WriteLine("      },");
            }
        }
    }
}
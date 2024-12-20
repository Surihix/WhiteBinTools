using System.Collections.Generic;
using System.IO;
using System.Text;
using WhiteBinTools.Filelist;
using WhiteBinTools.Support;
using static WhiteBinTools.Support.ProgramEnums;

namespace WhiteBinTools.Repack
{
    internal class RepackTypeD
    {
        public static void RepackFilelist(GameCodes gameCode, string extractedFilelistDir, StreamWriter logWriter)
        {
            IOhelpers.CheckDirExists(extractedFilelistDir, logWriter, "Error: Unpacked filelist directory specified in the argument is missing");

            var encHeaderFile = Path.Combine(extractedFilelistDir, "~EncryptionHeader_(DON'T DELETE)");
            var countsFile = Path.Combine(extractedFilelistDir, "~Counts.txt");

            IOhelpers.CheckFileExists(countsFile, logWriter, "Error: Unable to locate the '~Counts.txt' file");

            // Assume the filelist is supposed to be 
            // encrypted if the encHeader file exists
            var filelistVariables = new FilelistVariables();
            if (File.Exists(encHeaderFile))
            {
                filelistVariables.IsEncrypted = true;
                filelistVariables.EncryptedHeaderData = new byte[32];
                filelistVariables.EncryptedHeaderData = File.ReadAllBytes(encHeaderFile);
            }

            var repackVariables = new RepackVariables();
            repackVariables.NewFilelistFile = Path.Combine(Path.GetDirectoryName(extractedFilelistDir), Path.GetFileName(extractedFilelistDir).Remove(0, 1));

            if (Core.ShouldBckup)
            {
                if (File.Exists(repackVariables.NewFilelistFile))
                {
                    IOhelpers.IfFileExistsDel(repackVariables.NewFilelistFile + ".bak");

                    File.Copy(repackVariables.NewFilelistFile, repackVariables.NewFilelistFile + ".bak");
                }
            }

            IOhelpers.IfFileExistsDel(repackVariables.NewFilelistFile);

            using (var countsReader = new StreamReader(countsFile))
            {
                filelistVariables.TotalFiles = uint.Parse(countsReader.ReadLine());
                filelistVariables.TotalChunks = uint.Parse(countsReader.ReadLine());

                logWriter.LogMessage("TotalChunks: " + filelistVariables.TotalChunks);
                logWriter.LogMessage("No of files: " + filelistVariables.TotalFiles + "\n");
            }

            logWriter.LogMessage("\n\nBuilding filelist....");

            // Build an empty dictionary
            // for the chunks 
            var newChunksDict = new Dictionary<int, List<byte>>();
            RepackProcesses.CreateEmptyNewChunksDict(filelistVariables, newChunksDict);

            // Build a number list containing all
            // the odd number chunks if the code
            // is set to 2
            var oddChunkNumValues = new List<int>();
            if (gameCode == GameCodes.ff132 && filelistVariables.TotalChunks > 1)
            {
                var nextChunkNo = 1;
                for (int i = 0; i < filelistVariables.TotalChunks; i++)
                {
                    if (i == nextChunkNo)
                    {
                        oddChunkNumValues.Add(i);
                        nextChunkNo += 2;
                    }
                }
            }


            filelistVariables.LastChunkNumber = 0;

            using (var entriesStream = new MemoryStream())
            {
                using (var entriesWriter = new BinaryWriter(entriesStream))
                {                
                    
                    // Process each path in chunks
                    var chunkFilePath = Path.Combine(extractedFilelistDir, "Chunk_");
                    long entriesWriterPos = 0;
                    for (int c = 0; c < filelistVariables.TotalChunks; c++)
                    {
                        using (var currentChunkReader = new StreamReader(chunkFilePath + c + ".txt", Encoding.UTF8))
                        {
                            string line;
                            while ((line = currentChunkReader.ReadLine()) != null)
                            {
                                var stringData = line.Split('|');

                                // Write filecode
                                entriesWriter.BaseStream.Position = entriesWriterPos;
                                entriesWriter.WriteBytesUInt32(uint.Parse(stringData[0]), false);

                                if (gameCode == GameCodes.ff132)
                                {
                                    if (oddChunkNumValues.Contains(c))
                                    {
                                        // Write the 32768 position value
                                        // to indicate that the chunk
                                        // number is odd
                                        entriesWriter.BaseStream.Position = entriesWriterPos + 4;
                                        entriesWriter.WriteBytesUInt16(32768, false);
                                    }
                                    else
                                    {
                                        // Write zero as path number
                                        entriesWriter.BaseStream.Position = entriesWriterPos + 4;
                                        entriesWriter.WriteBytesUInt16(0, false);
                                    }

                                    // Write chunk number
                                    entriesWriter.BaseStream.Position = entriesWriterPos + 6;
                                    entriesWriter.Write(byte.Parse(stringData[1]));

                                    // Write UnkEntryVal
                                    entriesWriter.BaseStream.Position = entriesWriterPos + 7;
                                    entriesWriter.Write(byte.Parse(stringData[2]));

                                    // Add path to dictionary
                                    newChunksDict[c].AddRange(Encoding.UTF8.GetBytes(stringData[3] + "\0"));
                                }
                                else
                                {
                                    // Write chunk number
                                    entriesWriter.BaseStream.Position = entriesWriterPos + 4;
                                    entriesWriter.WriteBytesUInt16(ushort.Parse(stringData[1]), false);

                                    // Write zero as path number
                                    entriesWriter.BaseStream.Position = entriesWriterPos + 6;
                                    entriesWriter.WriteBytesUInt16(0, false);

                                    // Add path to dictionary
                                    newChunksDict[c].AddRange(Encoding.UTF8.GetBytes(stringData[2] + "\0"));
                                }

                                entriesWriterPos += 8;
                            }
                        }

                        filelistVariables.LastChunkNumber = c;
                    }

                    filelistVariables.EntriesData = new byte[entriesStream.Length];
                    entriesStream.Seek(0, SeekOrigin.Begin);
                    entriesStream.Read(filelistVariables.EntriesData, 0, filelistVariables.EntriesData.Length);
                }
            }


            RepackFilelistData.BuildFilelist(filelistVariables, newChunksDict, repackVariables, gameCode);

            if (filelistVariables.IsEncrypted)
            {
                FilelistCrypto.EncryptProcess(repackVariables, logWriter);
            }

            logWriter.LogMessage($"\n\nFinished repacking filelist data to \"{Path.GetFileName(repackVariables.NewFilelistFile)}\"");
        }
    }
}
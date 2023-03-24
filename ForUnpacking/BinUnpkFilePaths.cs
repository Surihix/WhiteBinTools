using System;
using System.IO;
using System.Linq;
using System.Text;

namespace WhiteBinTools
{
    internal class BinUnpkFilePaths
    {
        public static void UnpkFilelist(int GameCode, string FilelistFile)
        {
            // Check if the filelist file exists
            if (!File.Exists(FilelistFile))
            {
                CmnMethods.LogMsgs("Error: Filelist file specified in the argument is missing");
                CmnMethods.ErrorExit("");
            }


            // Set the filelist file names
            var FilelistName = Path.GetFileName(FilelistFile);
            var FilelistOutName = Path.GetFileNameWithoutExtension(FilelistFile);

            // Set directories and file paths for the filelist file
            // and the extracted chunk files
            var InFilelistFilePath = Path.GetFullPath(FilelistFile);
            var InFilelistFileDir = Path.GetDirectoryName(InFilelistFilePath);
            var TmpDcryptFilelistFile = InFilelistFileDir + "\\filelist_tmp.bin";

            var ChunksExtDir = InFilelistFileDir + "\\" + "_chunks";
            var ChunkFile = ChunksExtDir + "\\chunk_";
            var OutChunkFile = InFilelistFileDir + "\\" + FilelistOutName + ".txt";


            // Check and delete backup filelist file, the chunk
            // files and extracted chunk file directory if it exists
            CmnMethods.IfFileExistsDel(FilelistFile + ".bak");

            if (Directory.Exists(ChunksExtDir))
            {
                Directory.Delete(ChunksExtDir, true);
            }
            Directory.CreateDirectory(ChunksExtDir);


            // Store a list of unencrypted filelist files
            string[] UnEncryptedFilelists = { "movielista.win32.bin", "movielistv.win32.bin", "movielist.win32.bin", 
                "filelist_sound_pack.win32.bin", "filelist_sound_pack.win32_us.bin", "filelist_sound_pack_fixed.win32.bin", 
                "filelist_sound_pack_fixed.win32_us.bin" };


            // Check for encryption header in the filelist file, if the
            // game code is set to 1
            if (GameCode.Equals(1))
            {
                using (FileStream CheckEncHeader = new FileStream(FilelistFile, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader EncHeaderReader = new BinaryReader(CheckEncHeader))
                    {
                        EncHeaderReader.BaseStream.Position = 20;
                        var EncHeaderNumber = EncHeaderReader.ReadUInt32();

                        if (EncHeaderNumber == 501232760)
                        {
                            CmnMethods.LogMsgs("Error: Detected encrypted filelist file. set the game code to 2 for handling " +
                                "this type of filelist");
                            CmnMethods.ErrorExit("");
                        }
                    }
                }
            }


            // Check if the ffxiiicrypt tool is present in the filelist directory
            // and if it doesn't exist copy it to the directory from the app
            // directory if it doesn't exist
            if (GameCode.Equals(2))
            {
                if (!File.Exists(InFilelistFileDir + "\\ffxiiicrypt.exe"))
                {
                    if (File.Exists("ffxiiicrypt.exe"))
                    {
                        if (!File.Exists(InFilelistFileDir + "\\ffxiiicrypt.exe"))
                        {
                            File.Copy("ffxiiicrypt.exe", InFilelistFileDir + "\\ffxiiicrypt.exe");
                        }
                    }
                    else
                    {
                        CmnMethods.LogMsgs("Error: Unable to locate ffxiiicrypt tool in the main app folder to " +
                            "decrypt the filelist file");
                        CmnMethods.ErrorExit("");
                    }
                }
            }


            try
            {
                // According to the game code and the filelist name, decide whether
                // to decrypt and trim the filelist file for extraction
                switch (GameCode)
                {
                    case 1:
                        CmnMethods.LogMsgs("Game is set to 13-1");
                        break;

                    case 2:
                        CmnMethods.LogMsgs("Game is set to 13-2 / 13-LR");

                        if (!UnEncryptedFilelists.Contains(FilelistName))
                        {
                            CmnMethods.IfFileExistsDel(TmpDcryptFilelistFile);

                            File.Copy(FilelistFile, TmpDcryptFilelistFile);

                            var CryptFilelistCode = " filelist";

                            CmnMethods.FFXiiiCryptTool(InFilelistFileDir, " -d ", "\"" + TmpDcryptFilelistFile + "\"",
                                ref CryptFilelistCode);

                            File.Move(FilelistFile, FilelistFile + ".bak");

                            using (FileStream ToAdjust = new FileStream(TmpDcryptFilelistFile, FileMode.Open, FileAccess.Read))
                            {
                                using (FileStream Adjusted = new FileStream(FilelistFile, FileMode.OpenOrCreate,
                                    FileAccess.Write))
                                {
                                    ToAdjust.Seek(32, SeekOrigin.Begin);
                                    ToAdjust.CopyTo(Adjusted);
                                }
                            }

                            File.Delete(TmpDcryptFilelistFile);
                        }
                        break;
                }


                // Process File chunks section
                // Intialize the variables required for extraction
                var ChunkFNameCount = (uint)0;
                var TotalChunks = (uint)0;
                var TotalFiles = (uint)0;

                using (FileStream Filelist = new FileStream(FilelistFile, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader FilelistReader = new BinaryReader(Filelist))
                    {
                        FilelistReader.BaseStream.Position = 0;
                        var chunksInfoStartPos = FilelistReader.ReadUInt32();
                        var chunksStartPos = FilelistReader.ReadUInt32();
                        TotalFiles = FilelistReader.ReadUInt32();

                        var ChunkInfo_size = chunksStartPos - chunksInfoStartPos;
                        TotalChunks = ChunkInfo_size / 12;

                        CmnMethods.LogMsgs("No of files: " + TotalFiles);

                        // Make a memorystream for holding all Chunks info
                        using (MemoryStream ChunkInfoStream = new MemoryStream())
                        {
                            Filelist.Seek(chunksInfoStartPos, SeekOrigin.Begin);
                            byte[] ChunkInfoBuffer = new byte[ChunkInfo_size];
                            var ChunkBytesRead = Filelist.Read(ChunkInfoBuffer, 0, ChunkInfoBuffer.Length);
                            ChunkInfoStream.Write(ChunkInfoBuffer, 0, ChunkBytesRead);

                            // Make memorystream for all Chunks compressed data
                            using (MemoryStream ChunkStream = new MemoryStream())
                            {
                                Filelist.Seek(chunksStartPos, SeekOrigin.Begin);
                                Filelist.CopyTo(ChunkStream);

                                // Open a binary reader and read each chunk's info and
                                // dump them as separate files
                                using (BinaryReader ChunkInfoReader = new BinaryReader(ChunkInfoStream))
                                {
                                    var ChunkInfoReadVal = (uint)0;
                                    for (int c = 0; c < TotalChunks; c++)
                                    {
                                        ChunkInfoReader.BaseStream.Position = ChunkInfoReadVal + 4;
                                        var ChunkCmpSize = ChunkInfoReader.ReadUInt32();
                                        var ChunkDataStart = ChunkInfoReader.ReadUInt32();

                                        ChunkStream.Seek(ChunkDataStart, SeekOrigin.Begin);
                                        using (MemoryStream ChunkToDcmp = new MemoryStream())
                                        {
                                            byte[] ChunkBuffer = new byte[ChunkCmpSize];
                                            var ReadCmpBytes = ChunkStream.Read(ChunkBuffer, 0, ChunkBuffer.Length);
                                            ChunkToDcmp.Write(ChunkBuffer, 0, ReadCmpBytes);

                                            using (FileStream ChunksOutStream = new FileStream(ChunkFile + ChunkFNameCount,
                                                FileMode.OpenOrCreate, FileAccess.ReadWrite))
                                            {
                                                ChunkToDcmp.Seek(0, SeekOrigin.Begin);
                                                ZlibLibrary.ZlibDecompress(ChunkToDcmp, ChunksOutStream);
                                            }
                                        }

                                        ChunkInfoReadVal += 12;
                                        ChunkFNameCount++;
                                    }
                                }
                            }
                        }
                    }
                }


                // Write all file paths strings
                // to a text file
                ChunkFNameCount = 0;
                for (int cf = 0; cf < TotalChunks; cf++)
                {
                    // Get the total number of entries in a chunk file by counting the number of times
                    // an null character occurs in the chunk file
                    var EntriesInChunk = (uint)0;
                    using (StreamReader FileCountReader = new StreamReader(ChunksExtDir + "/chunk_" + ChunkFNameCount))
                    {
                        while (!FileCountReader.EndOfStream)
                        {
                            var CurrentNullChar = FileCountReader.Read();
                            if (CurrentNullChar == 0)
                            {
                                EntriesInChunk++;
                            }
                        }
                    }

                    // Open a chunk file for reading
                    using (FileStream CurrentChunk = new FileStream(ChunkFile + ChunkFNameCount, FileMode.Open, FileAccess.Read))
                    {
                        using (FileStream OutChunk = new FileStream(OutChunkFile, FileMode.Append, FileAccess.Write))
                        {
                            using (StreamWriter EntriesWriter = new StreamWriter(OutChunk))
                            {
                                using (BinaryReader ChunkStringReader = new BinaryReader(CurrentChunk))
                                {
                                    var ChunkStringReaderPos = (uint)0;
                                    for (int e = 0; e < EntriesInChunk; e++)
                                    {
                                        ChunkStringReader.BaseStream.Position = ChunkStringReaderPos;
                                        var ParsedString = new StringBuilder();
                                        char GetParsedString;
                                        while ((GetParsedString = ChunkStringReader.ReadChar()) != default)
                                        {
                                            ParsedString.Append(GetParsedString);
                                        }
                                        var Parsed = ParsedString.ToString();

                                        EntriesWriter.WriteLine(Parsed);

                                        ChunkStringReaderPos = (uint)ChunkStringReader.BaseStream.Position;
                                    }
                                }
                            }
                        }
                    }

                    File.Delete(ChunkFile + ChunkFNameCount);
                    ChunkFNameCount++;
                }

                Directory.Delete(ChunksExtDir, true);


                // Restore old filefile file if game code is
                // set to 2 and if the filelist file is not encrypted
                if (GameCode.Equals(2))
                {
                    if (!UnEncryptedFilelists.Contains(FilelistName))
                    {
                        File.Delete(FilelistFile);
                        File.Move(FilelistFile + ".bak", FilelistFile);
                    }
                }


                CmnMethods.LogMsgs("\nExtracted filepaths to " + FilelistOutName + ".txt file");
            }
            catch (Exception ex)
            {
                CmnMethods.LogMsgs("Error: " + ex);
                CmnMethods.ErrorExit("");
            }
        }
    }
}
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace WhiteBinTools
{
    internal class BinUnpkAFile
    {
        private static readonly object _lockObject = new object();
        public static void UnpackFile(string GameCode, string FilelistFile, string WhiteBinFile, string WhiteFilePath)
        {
            using (FileStream LogStream = new FileStream("ProcessLog.txt", FileMode.Append, FileAccess.Write, FileShare.Write))
            {
                using (StreamWriter LogWriter = new StreamWriter(LogStream))
                {

                    // Check if the filelist file and the white image bin file exists
                    if (!File.Exists(FilelistFile))
                    {
                        Console.WriteLine("Error: Filelist file specified in the argument is missing");
                        LogWriter.WriteLine("Error: Filelist file specified in the argument is missing");
                        CmnMethods.ErrorExit("");
                    }
                    if (!File.Exists(WhiteBinFile))
                    {
                        Console.WriteLine("Error: Image bin file specified in the argument is missing");
                        LogWriter.WriteLine("Error: Image bin file specified in the argument is missing");
                        CmnMethods.ErrorExit("");
                    }


                    // Set the filelist and the white image bin file names
                    var FilelistName = Path.GetFileName(FilelistFile);
                    var WhiteBinName = Path.GetFileName(WhiteBinFile);


                    // Set directories and file paths for the filelist files and the white bin files
                    var InFilelistFilePath = Path.GetFullPath(FilelistFile);
                    var InBinFilePath = Path.GetFullPath(WhiteBinFile);
                    var InFilelistFileDir = Path.GetDirectoryName(InFilelistFilePath);
                    var InBinFileDir = Path.GetDirectoryName(InBinFilePath);
                    var TmpDcryptFilelistFile = InFilelistFileDir + "\\filelist_tmp.bin";
                    var Extract_dir_Name = Path.GetFileNameWithoutExtension(WhiteBinFile).Replace(".win32", "_win32").
                        Replace(".ps3", "_ps3").Replace(".x360", "_x360");
                    var Extract_dir = InBinFileDir + "\\" + Extract_dir_Name;
                    var DefaultChunksExtDir = Extract_dir + "\\_chunks";
                    var ChunkFile = DefaultChunksExtDir + "\\chunk_";


                    // Check and delete backup filelist file if it exists
                    CmnMethods.IfFileExistsDel(FilelistFile + ".bak");

                    // Check and delete extracted directory if they exist in the
                    // folder where they are supposed to be extracted
                    if (Directory.Exists(Extract_dir))
                    {
                        Console.WriteLine("Detected previous unpack. deleting....");
                        Directory.Delete(Extract_dir, true);
                        Console.Clear();
                    }

                    Directory.CreateDirectory(Extract_dir);
                    Directory.CreateDirectory(DefaultChunksExtDir);


                    // Store a list of unencrypted filelist files
                    string[] UnEncryptedFilelists = { "movielista.win32.bin", "movielistv.win32.bin", "movielist.win32.bin" };


                    // Check for encryption header in the filelist file, if the
                    // game code is set to 1
                    if (GameCode.Equals("-ff131"))
                    {
                        using (FileStream CheckEncHeader = new FileStream(FilelistFile, FileMode.Open, FileAccess.Read))
                        {
                            using (BinaryReader EncHeaderReader = new BinaryReader(CheckEncHeader))
                            {
                                EncHeaderReader.BaseStream.Position = 20;
                                var EncHeaderNumber = EncHeaderReader.ReadUInt32();

                                if (EncHeaderNumber == 501232760)
                                {
                                    Console.WriteLine("Error: Detected encrypted filelist file. set the game code to 2 for handling this type of filelist");
                                    LogWriter.WriteLine("Error: Detected encrypted filelist file. set the game code to 2 for handling this type of filelist");
                                    CmnMethods.ErrorExit("");
                                }
                            }
                        }
                    }

                    // Check if the ffxiiicrypt tool is present in the filelist directory
                    // and if it doesn't exist copy it to the directory from the app
                    // directory if it doesn't exist
                    if (GameCode.Equals("-ff132"))
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
                                Console.WriteLine("Error: Unable to locate ffxiiicrypt tool in the main app folder to decrypt the filelist file");
                                LogWriter.WriteLine("Error: Unable to locate ffxiiicrypt tool in the main app folder to decrypt the filelist file");
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
                            case "-ff131":
                                lock (_lockObject)
                                {
                                    Console.WriteLine("Game is set to 13-1");
                                    LogWriter.WriteLine("Game is set to 13-1");
                                }
                                break;

                            case "-ff132":
                                lock (_lockObject)
                                {
                                    Console.WriteLine("Game is set to 13-2 / 13-LR");
                                    LogWriter.WriteLine("Game is set to 13-2 / 13-LR");
                                }

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

                                lock (_lockObject)
                                {
                                    Console.WriteLine("TotalChunks: " + TotalChunks);
                                    LogWriter.WriteLine("TotalChunks: " + TotalChunks);

                                    Console.WriteLine("No of files: " + TotalFiles + "\n");
                                    LogWriter.WriteLine("No of files: " + TotalFiles + "\n");
                                }

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


                        // Extracting files section 
                        ChunkFNameCount = 0;
                        var CountDuplicate = 1;
                        var HasExtracted = false;
                        for (int ch = 0; ch < TotalChunks; ch++)
                        {
                            // Get the total number of files in a chunk file by counting the number of times
                            // an null character occurs in the chunk file
                            var FilesInChunkCount = (uint)0;
                            using (StreamReader FileCountReader = new StreamReader(DefaultChunksExtDir + "/chunk_" +
                                ChunkFNameCount))
                            {
                                while (!FileCountReader.EndOfStream)
                                {
                                    var CurrentNullChar = FileCountReader.Read();
                                    if (CurrentNullChar == 0)
                                    {
                                        FilesInChunkCount++;
                                    }
                                }
                            }

                            // Open a chunk file for reading
                            using (FileStream CurrentChunk = new FileStream(ChunkFile + ChunkFNameCount, FileMode.Open,
                                FileAccess.Read))
                            {
                                using (BinaryReader ChunkStringReader = new BinaryReader(CurrentChunk))
                                {
                                    var ChunkStringReaderPos = (uint)0;
                                    for (int f = 0; f < FilesInChunkCount; f++)
                                    {
                                        ChunkStringReader.BaseStream.Position = ChunkStringReaderPos;
                                        var ParsedString = new StringBuilder();
                                        char GetParsedString;
                                        while ((GetParsedString = ChunkStringReader.ReadChar()) != default)
                                        {
                                            ParsedString.Append(GetParsedString);
                                        }
                                        var Parsed = ParsedString.ToString();

                                        if (Parsed.StartsWith("end"))
                                        {
                                            break;
                                        }

                                        string[] data = Parsed.Split(':');
                                        var Pos = Convert.ToUInt32(data[0], 16) * 2048;
                                        var UncmpSize = Convert.ToUInt32(data[1], 16);
                                        var CmpSize = Convert.ToUInt32(data[2], 16);
                                        var MainPath = data[3].Replace("/", "\\");

                                        var DirectoryPath = Path.GetDirectoryName(MainPath);
                                        var FileName = Path.GetFileName(MainPath);
                                        var FullFilePath = Extract_dir + "\\" + DirectoryPath + "\\" + FileName;
                                        var CompressedState = false;
                                        var UnpackedState = "";

                                        if (!UncmpSize.Equals(CmpSize))
                                        {
                                            CompressedState = true;
                                            UnpackedState = "Decompressed";
                                        }
                                        else
                                        {
                                            CompressedState = false;
                                            UnpackedState = "Copied";
                                        }

                                        // Extract a specific file
                                        if (MainPath.Equals(WhiteFilePath))
                                        {
                                            using (FileStream Bin = new FileStream(WhiteBinFile, FileMode.Open, FileAccess.Read))
                                            {
                                                if (!Directory.Exists(Extract_dir + "\\" + DirectoryPath))
                                                {
                                                    Directory.CreateDirectory(Extract_dir + "\\" + DirectoryPath);
                                                }
                                                if (File.Exists(FullFilePath))
                                                {
                                                    File.Delete(FullFilePath);
                                                    CountDuplicate++;
                                                }

                                                switch (CompressedState)
                                                {
                                                    case true:
                                                        using (MemoryStream CmpData = new MemoryStream())
                                                        {
                                                            Bin.CopyTo(CmpData, Pos, CmpSize);

                                                            using (FileStream OutFile = new FileStream(FullFilePath,
                                                                FileMode.OpenOrCreate, FileAccess.ReadWrite))
                                                            {
                                                                CmpData.Seek(0, SeekOrigin.Begin);
                                                                ZlibLibrary.ZlibDecompress(CmpData, OutFile);
                                                            }
                                                        }
                                                        break;

                                                    case false:
                                                        using (FileStream OutFile = new FileStream(FullFilePath, FileMode.OpenOrCreate,
                                                            FileAccess.Write))
                                                        {
                                                            OutFile.Seek(0, SeekOrigin.Begin);
                                                            Bin.CopyTo(OutFile, Pos, UncmpSize);
                                                        }
                                                        break;
                                                }
                                            }

                                            HasExtracted = true;

                                            lock (_lockObject)
                                            {
                                                Console.WriteLine(UnpackedState + " " + Extract_dir_Name + "\\" + MainPath);
                                                LogWriter.WriteLine(UnpackedState + " " + Extract_dir_Name + "\\" + MainPath);
                                            }
                                        }

                                        ChunkStringReaderPos = (uint)ChunkStringReader.BaseStream.Position;
                                    }
                                }
                            }

                            ChunkFNameCount++;
                        }

                        Directory.Delete(DefaultChunksExtDir, true);


                        // Restore old filefile file if game code is
                        // set to 2 and if the filelist file is not encrypted
                        if (GameCode.Equals("-ff132"))
                        {
                            if (!UnEncryptedFilelists.Contains(FilelistName))
                            {
                                File.Delete(FilelistFile);
                                File.Move(FilelistFile + ".bak", FilelistFile);
                            }
                        }


                        lock (_lockObject)
                        {
                            if (HasExtracted.Equals(false))
                            {
                                Console.WriteLine("Specified file does not exist. please specify the correct file path");
                                LogWriter.WriteLine("Specified file does not exist. please specify the correct file path");
                                Console.WriteLine("\nFinished extracting file " + WhiteBinName);
                                LogWriter.WriteLine("\nFinished extracting file " + WhiteBinName);
                            }
                            else
                            {
                                Console.WriteLine("\nFinished extracting file " + WhiteBinName);
                                LogWriter.WriteLine("\nFinished extracting file " + WhiteBinName);
                                if (CountDuplicate > 1)
                                {
                                    Console.WriteLine("\n" + CountDuplicate + " duplicate file(s)");
                                    LogWriter.WriteLine("\n" + CountDuplicate + " duplicate file(s)");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (File.Exists(FilelistFile + ".bak"))
                        {
                            File.Delete(FilelistFile);
                            File.Move(FilelistFile + ".bak", FilelistFile);
                        }

                        Console.WriteLine("Error: " + ex);
                        CmnMethods.CrashLog("Error: " + ex);
                        Console.WriteLine("");
                        CmnMethods.ErrorExit("Crash exception recorded in CrashLog.txt file");
                    }
                }
            }
        }
    }
}
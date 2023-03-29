using System;
using System.IO;
using System.Linq;
using System.Text;

namespace WhiteBinTools
{
    internal class BinRpkMoreFiles
    {
        private static readonly object _lockObject = new object();
        public static void RepackMoreFiles(string GameCode, string FilelistFile, string WhiteBinFile, string Extracted_Dir)
        {
            // Check if the filelist file and the unpacked directory exists
            if (!File.Exists(FilelistFile))
            {
                CmnMethods.LogMsgs("Error: Filelist file specified in the argument is missing");
                CmnMethods.ErrorExit("");
            }
            if (!File.Exists(WhiteBinFile))
            {
                CmnMethods.LogMsgs("Error: Unpacked directory specified in the argument is missing");
                CmnMethods.ErrorExit("");
            }
            if (!Directory.Exists(Extracted_Dir))
            {
                CmnMethods.LogMsgs("Error: Unpacked directory specified in the argument is missing");
                CmnMethods.ErrorExit("");
            }


            // Set the filelist name
            var FilelistName = Path.GetFileName(FilelistFile);

            // Set directories and file paths for the filelist files,
            // the extracted white bin folder, and other temp files
            var InFilelistFilePath = Path.GetFullPath(FilelistFile);
            var InFilelistFileDir = Path.GetDirectoryName(InFilelistFilePath);

            var WhiteBinFolderName = Path.GetFileName(Extracted_Dir);

            var BackupOldFilelistFile = InFilelistFileDir + "\\" + FilelistName + ".bak";
            var TmpDcryptFilelistFile = InFilelistFileDir + "\\filelist_tmp.bin";
            var TmpCmpDataFile = Extracted_Dir + "\\CmpData";
            var TmpCmpChunkFile = Extracted_Dir + "\\CmpChunk";
            var DefaultChunksExtDir = Extracted_Dir + "\\_default_chunks";
            var NewChunksExtDir = Extracted_Dir + "\\_new_chunks";

            var DefChunkFile = DefaultChunksExtDir + "\\chunk_";
            var NewChunkFile = NewChunksExtDir + "\\chunk_";
            var NewFileListFile = InFilelistFileDir + "\\" + FilelistName + ".new";

            // ffxiiicrypt tool action codes
            var CryptFilelistCode = " filelist";
            var CryptCheckSumCode = " write";


            // Check and delete the white bin file, backup filelist file,
            // and other files if it exists in the respective directories
            CmnMethods.IfFileExistsDel(BackupOldFilelistFile);
            CmnMethods.IfFileExistsDel(TmpDcryptFilelistFile);
            CmnMethods.IfFileExistsDel("_encryptionHeader.bin");
            CmnMethods.IfFileExistsDel(TmpCmpDataFile);
            CmnMethods.IfFileExistsDel(TmpCmpChunkFile);

            // Check and delete extracted directory if they exist in the
            // folder where they are supposed to be extracted
            if (Directory.Exists(DefaultChunksExtDir))
            {
                Directory.Delete(DefaultChunksExtDir, true);
            }
            Directory.CreateDirectory(DefaultChunksExtDir);

            if (Directory.Exists(NewChunksExtDir))
            {
                Directory.Delete(NewChunksExtDir, true);
            }
            Directory.CreateDirectory(NewChunksExtDir);


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
                    case "-ff131":
                        lock (_lockObject)
                        {
                            CmnMethods.LogMsgs("Game is set to 13-1");
                        }
                        break;

                    case "-ff132":
                        lock (_lockObject)
                        {
                            CmnMethods.LogMsgs("Game is set to 13-2 / 13-LR");
                        }

                        if (!UnEncryptedFilelists.Contains(FilelistName))
                        {
                            CmnMethods.IfFileExistsDel(TmpDcryptFilelistFile);

                            File.Copy(FilelistFile, TmpDcryptFilelistFile);

                            CmnMethods.FFXiiiCryptTool(InFilelistFileDir, " -d ", "\"" + TmpDcryptFilelistFile + "\"",
                                ref CryptFilelistCode);

                            File.Move(FilelistFile, BackupOldFilelistFile);

                            // Copy the decrypted filelist data from offset 32 onwards
                            // from the temp file that you renamed after decrypted, to a file
                            // that is with the same name as the original filelist name
                            using (FileStream ToAdjust = new FileStream(TmpDcryptFilelistFile, FileMode.Open, FileAccess.Read))
                            {
                                // Store the filelist data in a separate filelist file
                                using (FileStream Adjusted = new FileStream(FilelistFile, FileMode.OpenOrCreate,
                                    FileAccess.Write))
                                {
                                    ToAdjust.Seek(32, SeekOrigin.Begin);
                                    ToAdjust.CopyTo(Adjusted);

                                    // Store the encryption header data in a separate file
                                    using (FileStream EncryptedHeader = new FileStream("_encryptionHeader.bin",
                                        FileMode.OpenOrCreate, FileAccess.Write))
                                    {
                                        ToAdjust.Seek(0, SeekOrigin.Begin);
                                        byte[] EncryptionBuffer = new byte[32];
                                        var EncryptionBytesRead = ToAdjust.Read(EncryptionBuffer, 0,
                                            EncryptionBuffer.Length);
                                        EncryptedHeader.Write(EncryptionBuffer, 0, EncryptionBytesRead);
                                    }
                                }
                            }

                            File.Delete(TmpDcryptFilelistFile);
                        }
                        break;
                }


                // Initialise variables to commonly use
                var chunksInfoStartPos = (uint)0;
                var chunksStartPos = (uint)0;
                var ChunkFNameCount = (uint)0;
                var TotalChunks = (uint)0;
                var LastChunkFileNumber = (uint)1000;

                // Set the values to the initialised variables
                using (FileStream BaseFilelist = new FileStream(FilelistFile, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader BaseFilelistReader = new BinaryReader(BaseFilelist))
                    {
                        BaseFilelistReader.BaseStream.Position = 0;
                        chunksInfoStartPos = BaseFilelistReader.ReadUInt32();
                        chunksStartPos = BaseFilelistReader.ReadUInt32();
                        var TotalFiles = BaseFilelistReader.ReadUInt32();

                        var ChunkInfo_size = chunksStartPos - chunksInfoStartPos;
                        TotalChunks = ChunkInfo_size / 12;

                        lock (_lockObject)
                        {
                            CmnMethods.LogMsgs("TotalChunks: " + TotalChunks);
                            CmnMethods.LogMsgs("No of files: " + TotalFiles);
                            CmnMethods.LogMsgs("\n");
                        }

                        // Make a memorystream for holding all Chunks info
                        using (MemoryStream ChunkInfoStream = new MemoryStream())
                        {
                            BaseFilelist.Seek(chunksInfoStartPos, SeekOrigin.Begin);
                            byte[] ChunkInfoBuffer = new byte[ChunkInfo_size];
                            var ChunkBytesRead = BaseFilelist.Read(ChunkInfoBuffer, 0, ChunkInfoBuffer.Length);
                            ChunkInfoStream.Write(ChunkInfoBuffer, 0, ChunkBytesRead);

                            // Make memorystream for all Chunks compressed data
                            using (MemoryStream ChunkStream = new MemoryStream())
                            {
                                BaseFilelist.Seek(chunksStartPos, SeekOrigin.Begin);
                                BaseFilelist.CopyTo(ChunkStream);

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

                                            using (FileStream ChunksOutStream = new FileStream(DefChunkFile + ChunkFNameCount,
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


                        // Compress each file into the white image archive section
                        // Open a chunk file and start the repacking process
                        ChunkFNameCount = 0;
                        for (int ch = 0; ch < TotalChunks; ch++)
                        {
                            // Get the total number of files in a chunk file by counting the number of times
                            // an null character occurs in the chunk file
                            var FilesInChunkCount = (uint)0;
                            using (StreamReader FileCountReader = new StreamReader(DefChunkFile + ChunkFNameCount))
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
                            using (FileStream CurrentChunk = new FileStream(DefChunkFile + ChunkFNameCount, FileMode.Open,
                                FileAccess.Read))
                            {
                                using (BinaryReader ChunkStringReader = new BinaryReader(CurrentChunk))
                                {
                                    // Create a new chunk file with append mode for writing updated values back to the 
                                    // filelist file
                                    using (FileStream UpdChunk = new FileStream(NewChunkFile + ChunkFNameCount,
                                        FileMode.Append, FileAccess.Write))
                                    {
                                        using (StreamWriter UpdChunkStrings = new StreamWriter(UpdChunk))
                                        {

                                            // Compress files in a chunk into the archive 
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
                                                    UpdChunkStrings.Write("end\0");
                                                    LastChunkFileNumber = ChunkFNameCount;
                                                    break;
                                                }

                                                string[] data = Parsed.Split(':');
                                                var OgFilePos = Convert.ToUInt32(data[0], 16) * 2048;
                                                var OgUSize = Convert.ToUInt32(data[1], 16);
                                                var OgCSize = Convert.ToUInt32(data[2], 16);
                                                var MainPath = data[3];
                                                var DirectoryPath = Path.GetDirectoryName(MainPath);
                                                var FileName = Path.GetFileName(MainPath);
                                                var FullFilePath = Extracted_Dir + "\\" + DirectoryPath + "\\" + FileName;

                                                // Assign values to the variables to ensure that 
                                                // they get modified only when the file to repack
                                                // is found
                                                uint NewFilePos = OgFilePos;
                                                uint NewUcmpSize = OgUSize;
                                                uint NewCmpSize = OgCSize;
                                                var AsciCmpSize = "";
                                                var AsciUcmpSize = "";
                                                var AsciFilePos = "";
                                                var PackedState = "";
                                                var PackedAs = "";
                                                var CompressedState = false;

                                                if (!OgUSize.Equals(OgCSize))
                                                {
                                                    CompressedState = true;
                                                    PackedState = "Compressed";
                                                }
                                                else
                                                {
                                                    CompressedState = false;
                                                    PackedState = "Copied";
                                                }

                                                // Repack a specific file if it
                                                // exists in the directory
                                                if (File.Exists(FullFilePath))
                                                {
                                                    using (FileStream CleanBin = new FileStream(WhiteBinFile, FileMode.Open,
                                                        FileAccess.Write))
                                                    {
                                                        CleanBin.Seek(OgFilePos, SeekOrigin.Begin);
                                                        for (int pad = 0; pad < OgCSize; pad++)
                                                        {
                                                            CleanBin.WriteByte(0);
                                                        }
                                                    }

                                                    // According to the compressed state, compress or
                                                    // copy the file
                                                    switch (CompressedState)
                                                    {
                                                        case true:
                                                            // Compress the file and get its uncompressed
                                                            // and compressed size
                                                            var CreateFile = File.Create(TmpCmpDataFile);
                                                            CreateFile.Close();

                                                            ZlibLibrary.ZlibCompress(FullFilePath, TmpCmpDataFile,
                                                                Ionic.Zlib.CompressionLevel.Level9);

                                                            FileInfo UcmpDataInfo = new FileInfo(FullFilePath);
                                                            NewUcmpSize = (uint)UcmpDataInfo.Length;

                                                            FileInfo CmpDataInfo = new FileInfo(TmpCmpDataFile);
                                                            NewCmpSize = (uint)CmpDataInfo.Length;

                                                            // Open the compressed file in a stream and
                                                            // decide whether to inject or append the
                                                            // compressed file
                                                            using (FileStream CmpDataStream = new FileStream(TmpCmpDataFile,
                                                                    FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                                                            {
                                                                // If file is smaller or same as original, then inject
                                                                // the file at the original position
                                                                if (NewCmpSize < OgCSize || NewCmpSize.Equals(OgCSize))
                                                                {
                                                                    PackedAs = " (Injected)";
                                                                    NewFilePos = OgFilePos;

                                                                    using (FileStream InjectWhiteBin =
                                                                        new FileStream(WhiteBinFile, FileMode.Open,
                                                                        FileAccess.Write))
                                                                    {
                                                                        InjectWhiteBin.Seek(OgFilePos, SeekOrigin.Begin);
                                                                        CmpDataStream.CopyTo(InjectWhiteBin);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    // If file is larger, then append
                                                                    // the file at the end
                                                                    using (FileStream AppendWhiteBin =
                                                                        new FileStream(WhiteBinFile, FileMode.Append,
                                                                        FileAccess.Write))
                                                                    {
                                                                        PackedAs = " (Appended)";
                                                                        NewFilePos = (uint)AppendWhiteBin.Length;

                                                                        // Check if file position is divisible by 2048
                                                                        // and if its not divisible, add in null bytes
                                                                        // till next closest divisible number
                                                                        if (NewFilePos % 2048 != 0)
                                                                        {
                                                                            var Remainder = NewFilePos % 2048;
                                                                            var IncreaseBytes = 2048 - Remainder;
                                                                            var NewPos = NewFilePos + IncreaseBytes;
                                                                            var PadNulls = NewPos - NewFilePos;

                                                                            AppendWhiteBin.Seek(NewFilePos, SeekOrigin.Begin);
                                                                            for (int pad = 0; pad < PadNulls; pad++)
                                                                            {
                                                                                AppendWhiteBin.WriteByte(0);
                                                                            }
                                                                            NewFilePos = (uint)AppendWhiteBin.Length;
                                                                        }

                                                                        AppendWhiteBin.Seek(NewFilePos, SeekOrigin.Begin);
                                                                        CmpDataStream.CopyTo(AppendWhiteBin);
                                                                    }
                                                                }
                                                            }
                                                            File.Delete(TmpCmpDataFile);
                                                            break;

                                                        case false:
                                                            // Get the file size and copy the file
                                                            FileInfo CopyTypeFileInfo = new FileInfo(FullFilePath);
                                                            NewUcmpSize = (uint)CopyTypeFileInfo.Length;
                                                            NewCmpSize = NewUcmpSize;

                                                            // Open the file in a stream and decide whether
                                                            // to inject or append the compressed file
                                                            using (FileStream CopyTypeFileStream = new FileStream(FullFilePath,
                                                                FileMode.Open, FileAccess.Read))
                                                            {
                                                                // If file is smaller or same as original, then inject
                                                                // the file at the original position
                                                                if (NewUcmpSize < OgUSize || NewUcmpSize == OgUSize)
                                                                {
                                                                    PackedAs = " (Injected)";
                                                                    NewFilePos = OgFilePos;

                                                                    using (FileStream InjectWhiteBin =
                                                                        new FileStream(WhiteBinFile, FileMode.Open,
                                                                        FileAccess.Write))
                                                                    {
                                                                        InjectWhiteBin.Seek(OgFilePos, SeekOrigin.Begin);
                                                                        CopyTypeFileStream.CopyTo(InjectWhiteBin);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    // If file is larger, then append
                                                                    // the file at the end
                                                                    using (FileStream AppendWhiteBin =
                                                                        new FileStream(WhiteBinFile, FileMode.Append,
                                                                        FileAccess.Write))
                                                                    {
                                                                        PackedAs = " (Appended)";
                                                                        NewFilePos = (uint)AppendWhiteBin.Length;

                                                                        // Check if file position is divisible by 2048
                                                                        // and if its not divisible, add in null bytes
                                                                        // till next closest divisible number
                                                                        if (NewFilePos % 2048 != 0)
                                                                        {
                                                                            var Remainder = NewFilePos % 2048;
                                                                            var IncreaseBytes = 2048 - Remainder;
                                                                            var NewPos = NewFilePos + IncreaseBytes;
                                                                            var PadNulls = NewPos - NewFilePos;

                                                                            AppendWhiteBin.Seek(NewFilePos, SeekOrigin.Begin);
                                                                            for (int pad = 0; pad < PadNulls; pad++)
                                                                            {
                                                                                AppendWhiteBin.WriteByte(0);
                                                                            }
                                                                            NewFilePos = (uint)AppendWhiteBin.Length;
                                                                        }

                                                                        AppendWhiteBin.Seek(NewFilePos, SeekOrigin.Begin);
                                                                        CopyTypeFileStream.CopyTo(AppendWhiteBin);
                                                                    }
                                                                }
                                                            }
                                                            break;
                                                    }

                                                    lock (_lockObject)
                                                    {
                                                        CmnMethods.LogMsgs(PackedState + " " + WhiteBinFolderName + "/" + MainPath +
                                                            PackedAs);
                                                    }
                                                }

                                                NewFilePos /= 2048;
                                                CmnMethods.DecToHex(NewFilePos, ref AsciFilePos);
                                                CmnMethods.DecToHex(NewUcmpSize, ref AsciUcmpSize);
                                                CmnMethods.DecToHex(NewCmpSize, ref AsciCmpSize);

                                                var NewUpdatedPath = AsciFilePos + ":" + AsciUcmpSize + ":" + AsciCmpSize + ":" +
                                                    MainPath + "\0";
                                                UpdChunkStrings.Write(NewUpdatedPath);

                                                ChunkStringReaderPos = (uint)ChunkStringReader.BaseStream.Position;
                                            }
                                        }
                                    }
                                }
                            }

                            ChunkFNameCount++;
                        }


                        // Fileinfo updating and chunk compression section
                        // Copy the base filelist file's data into the new filelist file till the chunk data begins
                        var AppendAt = (uint)0;
                        using (FileStream NewFilelist = new FileStream(NewFileListFile, FileMode.Append, FileAccess.Write))
                        {
                            using (BinaryWriter NewFilelistWriter = new BinaryWriter(NewFilelist))
                            {
                                BaseFilelist.Seek(0, SeekOrigin.Begin);
                                byte[] NewFilelistBuffer = new byte[chunksStartPos];
                                var NewFilelistBytesRead = BaseFilelist.Read(NewFilelistBuffer, 0, NewFilelistBuffer.Length);
                                NewFilelist.Write(NewFilelistBuffer, 0, NewFilelistBytesRead);

                                // Compress and append multiple chunks to the new filelist file
                                ChunkFNameCount = 0;
                                var ChunkInfoWriterPos = chunksInfoStartPos;
                                var ChunkCmpSize = (uint)0;
                                var ChunkUncmpSize = (uint)0;
                                var ChunkStartVal = (uint)0;
                                var FileInfoWriterPos = 18;
                                if (GameCode.Equals("-ff132"))
                                {
                                    // Change Fileinfo writer position
                                    // according to the game code 
                                    FileInfoWriterPos = 16;
                                }
                                for (int Ac = 0; Ac < TotalChunks; Ac++)
                                {
                                    // Get total number of files in the chunk and decrease the filecount by 1 if the 
                                    // the lastchunk number matches with the current chunk number running in this for loop
                                    var FilesInChunkCount = (uint)0;
                                    using (StreamReader FileCountReader = new StreamReader(NewChunkFile + ChunkFNameCount))
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

                                    if (LastChunkFileNumber.Equals(ChunkFNameCount))
                                    {
                                        FilesInChunkCount--;
                                    }

                                    // Get each file strings start position in a chunk and update the position
                                    // value in the info section of the new filelist file
                                    using (FileStream FileStrings = new FileStream(NewChunkFile + ChunkFNameCount,
                                        FileMode.Open, FileAccess.Read))
                                    {
                                        using (BinaryReader FileStringsReader = new BinaryReader(FileStrings))
                                        {
                                            var FilePosInChunk = (UInt16)0;
                                            var FilePosInChunkToWrite = (UInt16)0;
                                            for (int Fic = 0; Fic < FilesInChunkCount; Fic++)
                                            {
                                                // According to the game code, check how to
                                                // write the value and then set the appropriate
                                                // converted value to write
                                                if (GameCode.Equals("-ff132"))
                                                {
                                                    BaseFilelistReader.BaseStream.Position = FileInfoWriterPos;
                                                    var CheckVal = BaseFilelistReader.ReadUInt16();

                                                    if (CheckVal > 32767)
                                                    {
                                                        FilePosInChunkToWrite = (ushort)(FilePosInChunkToWrite + 32768);
                                                    }
                                                }

                                                CmnMethods.AdjustBytesUInt16(NewFilelistWriter, FileInfoWriterPos,
                                                    out byte[] AdjustFilePosInChunk, FilePosInChunkToWrite);

                                                FileStringsReader.BaseStream.Position = FilePosInChunk;
                                                var ParsedVal = new StringBuilder();
                                                char GetParsedVal;
                                                while ((GetParsedVal = FileStringsReader.ReadChar()) != default)
                                                {
                                                    ParsedVal.Append(GetParsedVal);
                                                }

                                                FilePosInChunk = (UInt16)FileStringsReader.BaseStream.Position;
                                                FilePosInChunkToWrite = (UInt16)FileStringsReader.BaseStream.Position;
                                                FileInfoWriterPos += 8;
                                            }
                                        }
                                    }


                                    // Compress and package a chunk back into the new filelist file and update the 
                                    // offsets in the chunk info section of the filelist file
                                    AppendAt = (uint)NewFilelist.Length;
                                    NewFilelist.Seek(AppendAt, SeekOrigin.Begin);

                                    FileInfo ChunkDataInfo = new FileInfo(NewChunkFile + ChunkFNameCount);
                                    ChunkUncmpSize = (uint)ChunkDataInfo.Length;

                                    var CreateChunkFile = File.Create(TmpCmpChunkFile);
                                    CreateChunkFile.Close();

                                    ZlibLibrary.ZlibCompress(NewChunkFile + ChunkFNameCount, TmpCmpChunkFile,
                                        Ionic.Zlib.CompressionLevel.Level9);

                                    using (FileStream CmpChunkDataStream = new FileStream(TmpCmpChunkFile,
                                        FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                                    {
                                        CmpChunkDataStream.Seek(0, SeekOrigin.Begin);
                                        CmpChunkDataStream.CopyTo(NewFilelist);

                                        FileInfo CmpChunkDataInfo = new FileInfo(TmpCmpChunkFile);
                                        ChunkCmpSize = (uint)CmpChunkDataInfo.Length;
                                    }
                                    File.Delete(TmpCmpChunkFile);

                                    CmnMethods.AdjustBytesUInt32(NewFilelistWriter, ChunkInfoWriterPos,
                                        out byte[] AdjustChunkUnCmpSize, ChunkUncmpSize);
                                    CmnMethods.AdjustBytesUInt32(NewFilelistWriter, ChunkInfoWriterPos + 4,
                                        out byte[] AdjustChunkCmpSize, ChunkCmpSize);
                                    CmnMethods.AdjustBytesUInt32(NewFilelistWriter, ChunkInfoWriterPos + 8,
                                        out byte[] AdjustChunkStart, ChunkStartVal);

                                    var NewChunkStartVal = ChunkStartVal + ChunkCmpSize;
                                    ChunkStartVal = NewChunkStartVal;

                                    ChunkInfoWriterPos += 12;
                                    ChunkFNameCount++;
                                }
                            }
                        }
                    }
                }

                Directory.Delete(DefaultChunksExtDir, true);
                Directory.Delete(NewChunksExtDir, true);


                // Make a backup of the old filelist file according to the game code 
                // and if that filelist file is inside the unencrypted filelist array
                if (GameCode.Equals("-ff131"))
                {
                    File.Copy(FilelistFile, BackupOldFilelistFile);
                }
                if (UnEncryptedFilelists.Contains(FilelistName))
                {
                    File.Copy(FilelistFile, BackupOldFilelistFile);
                }

                // Delete the old filelist file and rename the new filelist file
                // to the old filelist file name
                File.Delete(FilelistFile);
                File.Move(NewFileListFile, FilelistFile);


                // Re Encrypt filelist file and add the neccessary encryption header if the game code is set to 2
                if (GameCode.Equals("-ff132"))
                {
                    if (!UnEncryptedFilelists.Contains(FilelistName))
                    {
                        var MaxFilelistSize = (uint)0;

                        // Rename the filelist file to temp filelist name
                        File.Move(FilelistFile, TmpDcryptFilelistFile);

                        // Copy the encrypted header data and the filelist data to
                        // the filelist file 
                        // Open the final filelist
                        using (FileStream EncryptedFilelist = new FileStream(FilelistFile, FileMode.Append,
                            FileAccess.Write))
                        {
                            using (BinaryWriter EncryptedFilelistWriter = new BinaryWriter(EncryptedFilelist))
                            {
                                // Encryption header copy
                                using (FileStream EncryptedData = new FileStream("_encryptionHeader.bin", FileMode.Open,
                                    FileAccess.Read))
                                {
                                    EncryptedData.Seek(0, SeekOrigin.Begin);
                                    EncryptedData.CopyTo(EncryptedFilelist);

                                    // NewFilelist data copy 
                                    using (FileStream NewFilelistData = new FileStream(TmpDcryptFilelistFile, FileMode.Open,
                                        FileAccess.Read))
                                    {
                                        var FilelistSize = (uint)NewFilelistData.Length;
                                        NewFilelistData.Seek(0, SeekOrigin.Begin);
                                        NewFilelistData.CopyTo(EncryptedFilelist);

                                        if (FilelistSize % 8 != 0)
                                        {
                                            // Get remainder from the division and
                                            // reduce the remainder with 8. set that
                                            // reduced value to a variable
                                            var Remainder = FilelistSize % 8;
                                            var IncreaseByteAmount = 8 - Remainder;

                                            // Increase the filelist size with the
                                            // increase byte variable from the previous step and
                                            // set this as a variable
                                            // Then get the amount of null bytes to pad by subtracting 
                                            // the new size  with the filelist size
                                            var NewSize = FilelistSize + IncreaseByteAmount;
                                            var PaddingNulls = NewSize - FilelistSize;

                                            EncryptedFilelist.Seek((uint)EncryptedFilelist.Length, SeekOrigin.Begin);
                                            for (int padding = 0; padding < PaddingNulls; padding++)
                                            {
                                                EncryptedFilelist.WriteByte(0);
                                            }

                                            FilelistSize = NewSize;
                                        }

                                        CmnMethods.AdjustBytesUInt32(EncryptedFilelistWriter, 16, out byte[] AdjTotalFilelistSize,
                                            FilelistSize);

                                        EncryptedFilelist.Seek(0, SeekOrigin.Begin);
                                        MaxFilelistSize = (uint)EncryptedFilelist.Length;

                                        EncryptedFilelistWriter.BaseStream.Position = (uint)EncryptedFilelist.Length;
                                        EncryptedFilelistWriter.Write(FilelistSize);

                                        EncryptedFilelist.Seek((uint)EncryptedFilelist.Length, SeekOrigin.Begin);
                                        for (int n = 0; n < 12; n++)
                                        {
                                            EncryptedFilelist.WriteByte(0);
                                        }
                                    }
                                }
                            }
                        }

                        // Write checksum to the filelist file
                        var CryptAsciiSize = "";
                        CmnMethods.DecToHex(MaxFilelistSize, ref CryptAsciiSize);
                        var CheckSumActionArg = " 000" + CryptAsciiSize + CryptCheckSumCode;

                        CmnMethods.FFXiiiCryptTool(InFilelistFileDir, " -c ", "\"" + FilelistFile + "\"",
                            ref CheckSumActionArg);
                        CmnMethods.LogMsgs("\nWrote new checksum to the filelist");

                        // Delete the encryption header data file
                        File.Delete("_encryptionHeader.bin");

                        // Delete the temp filelist file
                        File.Delete(TmpDcryptFilelistFile);

                        // Encrypt the filelist file                 
                        CmnMethods.FFXiiiCryptTool(InFilelistFileDir, " -e ", "\"" + FilelistFile + "\"",
                            ref CryptFilelistCode);
                        CmnMethods.LogMsgs("\nEncrypted filelist file");
                    }
                }


                CmnMethods.LogMsgs("\nFinished repacking files to " + WhiteBinFile);
            }
            catch (Exception ex)
            {
                if (File.Exists(FilelistFile + ".bak"))
                {
                    File.Delete(FilelistFile);
                    File.Move(FilelistFile + ".bak", FilelistFile);
                }

                CmnMethods.LogMsgs("Error: " + ex);
                CmnMethods.ErrorExit("");
            }
        }
    }
}
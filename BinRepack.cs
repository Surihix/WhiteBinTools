using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Text;

namespace WhiteBinTools
{
    internal class BinRepack
    {
        private static readonly object _lockObject = new object();
        public static void Repack(int GameCode, string FilelistFile, string Extracted_Dir)
        {
            // Check if the filelist file and the unpacked directory exists
            if (!File.Exists(FilelistFile))
            {
                Core.LogMsgs("Error: Filelist file specified in the argument is missing");
                Core.ErrorExit("");
            }
            if (!Directory.Exists(Extracted_Dir))
            {
                Core.LogMsgs("Error: Unpacked directory specified in the argument is missing");
                Core.ErrorExit("");
            }


            // Set the filelist name
            var FilelistName = Path.GetFileName(FilelistFile);

            // Set directories and file paths for the filelist files,
            // the extracted white bin folder, and other temp files
            var InFilelistFilePath = Path.GetFullPath(FilelistFile);
            var InFilelistFileDir = Path.GetDirectoryName(InFilelistFilePath);

            var WhiteBinFolderName = Path.GetFileName(Extracted_Dir);
            var WhiteBinName = WhiteBinFolderName.Replace("_win32", ".win32.bin").Replace("_ps3", ".ps3.bin").
                Replace("_x360", ".x360.bin");
            var WhiteBinFile = InFilelistFileDir + "\\" + WhiteBinName;

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
            Core.IfFileExistsDel(WhiteBinFile);
            Core.IfFileExistsDel(BackupOldFilelistFile);
            Core.IfFileExistsDel(TmpDcryptFilelistFile);
            Core.IfFileExistsDel("_encryptionHeader.bin");
            Core.IfFileExistsDel(TmpCmpDataFile);
            Core.IfFileExistsDel(TmpCmpChunkFile);

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
                            Core.LogMsgs("Error: Detected encrypted filelist file. set the game code to 2 for handling " +
                                "this type of filelist");
                            Core.ErrorExit("");
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
                        Core.LogMsgs("Error: Unable to locate ffxiiicrypt tool in the main app folder to " +
                            "decrypt the filelist file");
                        Core.ErrorExit("");
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
                        lock (_lockObject)
                        {
                            Core.LogMsgs("Game is set to 13-1");
                        }
                        break;

                    case 2:
                        lock (_lockObject)
                        {
                            Core.LogMsgs("Game is set to 13-2 / 13-LR");
                        }

                        if (!UnEncryptedFilelists.Contains(FilelistName))
                        {
                            Core.IfFileExistsDel(TmpDcryptFilelistFile);

                            File.Copy(FilelistFile, TmpDcryptFilelistFile);

                            CryptProcess.FFXiiiCryptTool(InFilelistFileDir, " -d ", "\"" + TmpDcryptFilelistFile + "\"",
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
                            Core.LogMsgs("TotalChunks: " + TotalChunks);
                            Core.LogMsgs("No of files: " + TotalFiles);
                            Core.LogMsgs("\n");
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
                                                var OgUSize = Convert.ToUInt32(data[1], 16);
                                                var OgCSize = Convert.ToUInt32(data[2], 16);
                                                var MainPath = data[3];
                                                var DirectoryPath = Path.GetDirectoryName(MainPath);
                                                var FileName = Path.GetFileName(MainPath);
                                                var FullFilePath = Extracted_Dir + "\\" + DirectoryPath + "\\" + FileName;

                                                uint CmpSize = 0;
                                                uint UcmpSize = 0;
                                                uint FilePos = 0;
                                                var AsciCmpSize = "";
                                                var AsciUcmpSize = "";
                                                var AsciFilePos = "";
                                                var PackedState = "";
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

                                                using (FileStream WhiteBin = new FileStream(WhiteBinFile, FileMode.Append,
                                                    FileAccess.Write))
                                                {
                                                    FilePos = (uint)WhiteBin.Length;

                                                    // Check if file position is divisible by 2048
                                                    // and if its not divisible, add in null bytes
                                                    // till next closest divisible number
                                                    if (FilePos % 2048 != 0)
                                                    {
                                                        var Remainder = FilePos % 2048;
                                                        var IncreaseBytes = 2048 - Remainder;
                                                        var NewPos = FilePos + IncreaseBytes;
                                                        var PadNulls = NewPos - FilePos;

                                                        WhiteBin.Seek(FilePos, SeekOrigin.Begin);
                                                        for (int pad = 0; pad < PadNulls; pad++)
                                                        {
                                                            WhiteBin.WriteByte(0);
                                                        }
                                                        FilePos = (uint)WhiteBin.Length;
                                                    }

                                                    if (!File.Exists(FullFilePath))
                                                    {
                                                        var CreateNullFile = File.Create(FullFilePath);
                                                    }

                                                    using (FileStream PackAFile = new FileStream(FullFilePath, FileMode.Open,
                                                        FileAccess.Read))
                                                    {
                                                        WhiteBin.Seek(FilePos, SeekOrigin.Begin);
                                                        UcmpSize = (uint)PackAFile.Length;

                                                        switch (CompressedState)
                                                        {
                                                            case true:
                                                                var CreateFile = File.Create(TmpCmpDataFile);
                                                                CreateFile.Close();

                                                                ZlibLibrary.ZlibCompress(FullFilePath, TmpCmpDataFile,
                                                                    Ionic.Zlib.CompressionLevel.Level9);

                                                                using (FileStream CmpDataStream = new FileStream(TmpCmpDataFile,
                                                                    FileMode.OpenOrCreate, FileAccess.ReadWrite,
                                                                    FileShare.ReadWrite))
                                                                {
                                                                    CmpDataStream.Seek(0, SeekOrigin.Begin);
                                                                    CmpDataStream.CopyTo(WhiteBin);

                                                                    FileInfo CmpDataInfo = new FileInfo(TmpCmpDataFile);
                                                                    CmpSize = (uint)CmpDataInfo.Length;
                                                                }
                                                                File.Delete(TmpCmpDataFile);
                                                                break;

                                                            case false:
                                                                PackAFile.CopyTo(WhiteBin);
                                                                CmpSize = UcmpSize;
                                                                break;
                                                        }
                                                    }
                                                }

                                                FilePos /= 2048;
                                                DecToHex(FilePos, ref AsciFilePos);
                                                DecToHex(UcmpSize, ref AsciUcmpSize);
                                                DecToHex(CmpSize, ref AsciCmpSize);

                                                var NewUpdatedPath = AsciFilePos + ":" + AsciUcmpSize + ":" + AsciCmpSize + ":" +
                                                    MainPath + "\0";
                                                UpdChunkStrings.Write(NewUpdatedPath);
                                                lock (_lockObject)
                                                {
                                                    Core.LogMsgs(PackedState + " " + WhiteBinFolderName + "/" + MainPath);
                                                }

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
                                if (GameCode.Equals(2))
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
                                                if (GameCode.Equals(2))
                                                {
                                                    BaseFilelistReader.BaseStream.Position = FileInfoWriterPos;
                                                    var CheckVal = BaseFilelistReader.ReadUInt16();

                                                    if (CheckVal > 32767)
                                                    {
                                                        FilePosInChunkToWrite = (ushort)(FilePosInChunkToWrite + 32768);
                                                    }
                                                }

                                                NewFilelistWriter.BaseStream.Position = FileInfoWriterPos;
                                                byte[] AdjustFilePosInChunk = new byte[2];
                                                BinaryPrimitives.WriteUInt16LittleEndian(AdjustFilePosInChunk,
                                                    FilePosInChunkToWrite);
                                                NewFilelistWriter.Write(AdjustFilePosInChunk);

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

                                    NewFilelistWriter.BaseStream.Position = ChunkInfoWriterPos;
                                    byte[] AdjustChunkUnCmpSize = new byte[4];
                                    BinaryPrimitives.WriteUInt32LittleEndian(AdjustChunkUnCmpSize, ChunkUncmpSize);
                                    NewFilelistWriter.Write(AdjustChunkUnCmpSize);

                                    NewFilelistWriter.BaseStream.Position = ChunkInfoWriterPos + 4;
                                    byte[] AdjustChunkCmpSize = new byte[4];
                                    BinaryPrimitives.WriteUInt32LittleEndian(AdjustChunkCmpSize, ChunkCmpSize);
                                    NewFilelistWriter.Write(AdjustChunkCmpSize);

                                    NewFilelistWriter.BaseStream.Position = ChunkInfoWriterPos + 8;
                                    byte[] AdjustChunkStart = new byte[4];
                                    BinaryPrimitives.WriteUInt32LittleEndian(AdjustChunkStart, ChunkStartVal);
                                    NewFilelistWriter.Write(AdjustChunkStart);

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
                if (GameCode.Equals(1))
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
                if (GameCode.Equals(2))
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

                                        EncryptedFilelistWriter.BaseStream.Position = 16;
                                        byte[] AdjTotalFilelistSize = new byte[4];
                                        BinaryPrimitives.WriteUInt32BigEndian(AdjTotalFilelistSize, FilelistSize);
                                        EncryptedFilelistWriter.Write(AdjTotalFilelistSize);

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
                        DecToHex(MaxFilelistSize, ref CryptAsciiSize);
                        var CheckSumActionArg = " 000" + CryptAsciiSize + CryptCheckSumCode;

                        CryptProcess.FFXiiiCryptTool(InFilelistFileDir, " -c ", "\"" + FilelistFile + "\"", 
                            ref CheckSumActionArg);
                        Core.LogMsgs("\nWrote new checksum to the filelist");

                        // Delete the encryption header data file
                        File.Delete("_encryptionHeader.bin");

                        // Delete the temp filelist file
                        File.Delete(TmpDcryptFilelistFile);

                        // Encrypt the filelist file                 
                        CryptProcess.FFXiiiCryptTool(InFilelistFileDir, " -e ", "\"" + FilelistFile + "\"", 
                            ref CryptFilelistCode);
                        Core.LogMsgs("\nEncrypted filelist file");
                    }
                }


                Core.LogMsgs("\nFinished repacking files to white bin");
                Console.ReadLine();
            }
            catch (Exception ex)
            {                
                if (File.Exists(FilelistFile + ".bak"))
                {
                    File.Delete(FilelistFile);
                    File.Move(FilelistFile + ".bak", FilelistFile);
                }

                Core.LogMsgs("Error: " + ex);
                Core.ErrorExit("");
            }
        }

        static void DecToHex(uint DecValue, ref string HexValue)
        {
            HexValue = DecValue.ToString("x");
        }
    }
}
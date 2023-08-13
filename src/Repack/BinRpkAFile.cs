﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using WhiteBinTools.Common;
using WhiteBinTools.src.Common;

namespace WhiteBinTools.src.Repack
{
    internal class BinRpkAFile
    {
        private static readonly object _lockObject = new object();
        public static void RepackFile(CmnEnums.GameCodes gameCodeVar, string filelistFileVar, string whiteBinFileVar, string whiteFilePathVar)
        {
            using (var logStream = new FileStream("ProcessLog.txt", FileMode.Append, FileAccess.Write, FileShare.Write))
            {
                using (var logWriter = new StreamWriter(logStream))
                {

                    // Replace the slashes to ones that are similar to what is used for
                    // the file path strings in the chunks
                    whiteFilePathVar = whiteFilePathVar.Replace("\\", "/");

                    // Check if the filelist file and the unpacked directory exists
                    if (!File.Exists(filelistFileVar))
                    {
                        Console.WriteLine("Error: Filelist file specified in the argument is missing");
                        logWriter.WriteLine("Error: Filelist file specified in the argument is missing");
                        CmnMethods.ErrorExit("");
                    }
                    if (!File.Exists(whiteBinFileVar))
                    {
                        Console.WriteLine("Error: Unpacked directory specified in the argument is missing");
                        logWriter.WriteLine("Error: Unpacked directory specified in the argument is missing");
                        CmnMethods.ErrorExit("");
                    }


                    // Set the filelist name
                    var filelistName = Path.GetFileName(filelistFileVar);

                    // Set directories and file paths for the filelist files,
                    // the extracted white bin folder, and other temp files
                    var inFilelistFilePath = Path.GetFullPath(filelistFileVar);
                    var inFilelistFileDir = Path.GetDirectoryName(inFilelistFilePath);

                    var extractedDirName = Path.GetFileName(whiteBinFileVar).Replace(".win32.bin", "_win32").Replace(".ps3.bin", "_ps3").
                        Replace(".x360.bin", "_x360");
                    var whiteBinFolderName = Path.GetFileName(extractedDirName);
                    var extractedDir = inFilelistFileDir + "\\" + extractedDirName;

                    var backupOldFilelistFile = inFilelistFileDir + "\\" + filelistName + ".bak";
                    var tmpDcryptFilelistFile = inFilelistFileDir + "\\filelist_tmp.bin";
                    var tmpCmpDataFile = extractedDir + "\\CmpData";
                    var tmpCmpChunkFile = extractedDir + "\\CmpChunk";
                    var defaultChunksExtDir = extractedDir + "\\_default_chunks";
                    var newChunksExtDir = extractedDir + "\\_new_chunks";

                    var defChunkFile = defaultChunksExtDir + "\\chunk_";
                    var newChunkFile = newChunksExtDir + "\\chunk_";
                    var newFileListFile = inFilelistFileDir + "\\" + filelistName + ".new";

                    // ffxiiicrypt tool action codes
                    var cryptFilelistCode = " filelist";
                    var cryptCheckSumCode = " write";


                    // Check and delete the white bin file, backup filelist file,
                    // and other files if it exists in the respective directories
                    CmnMethods.IfFileExistsDel(backupOldFilelistFile);
                    CmnMethods.IfFileExistsDel(tmpDcryptFilelistFile);
                    CmnMethods.IfFileExistsDel("_encryptionHeader.bin");
                    CmnMethods.IfFileExistsDel(tmpCmpDataFile);
                    CmnMethods.IfFileExistsDel(tmpCmpChunkFile);


                    // Check if the extracted directory for this white bin
                    // and filelist exists
                    if (!Directory.Exists(extractedDir))
                    {
                        Console.WriteLine("Error: Extracted directory is missing");
                        logWriter.WriteLine("Error: Extracted directory is missing");
                        CmnMethods.ErrorExit("Extracted directory is missing");
                    }

                    // Check and delete extracted chunk directory if they exist in the
                    // folder where they are supposed to be extracted
                    if (Directory.Exists(defaultChunksExtDir))
                    {
                        Directory.Delete(defaultChunksExtDir, true);
                    }
                    Directory.CreateDirectory(defaultChunksExtDir);

                    if (Directory.Exists(newChunksExtDir))
                    {
                        Directory.Delete(newChunksExtDir, true);
                    }
                    Directory.CreateDirectory(newChunksExtDir);


                    // Store a list of unencrypted filelist files
                    string[] unEncryptedFilelists = { "movielista.win32.bin", "movielistv.win32.bin", "movielist.win32.bin" };


                    // Check for encryption header in the filelist file, if the
                    // game code is set to 1
                    if (gameCodeVar.Equals(CmnEnums.GameCodes.ff131))
                    {
                        using (var checkEncHeader = new FileStream(filelistFileVar, FileMode.Open, FileAccess.Read))
                        {
                            using (var encHeaderReader = new BinaryReader(checkEncHeader))
                            {
                                encHeaderReader.BaseStream.Position = 20;
                                var encHeaderNumber = encHeaderReader.ReadUInt32();

                                if (encHeaderNumber == 501232760)
                                {
                                    Console.WriteLine("Error: Detected encrypted filelist file. set the game code to 2 for handling this type of filelist");
                                    logWriter.WriteLine("Error: Detected encrypted filelist file. set the game code to 2 for handling this type of filelist");
                                    CmnMethods.ErrorExit("");
                                }
                            }
                        }
                    }

                    // Check if the ffxiiicrypt tool is present in the filelist directory
                    // and if it doesn't exist copy it to the directory from the app
                    // directory if it doesn't exist
                    if (gameCodeVar.Equals(CmnEnums.GameCodes.ff132))
                    {
                        if (!File.Exists(inFilelistFileDir + "\\ffxiiicrypt.exe"))
                        {
                            if (File.Exists("ffxiiicrypt.exe"))
                            {
                                if (!File.Exists(inFilelistFileDir + "\\ffxiiicrypt.exe"))
                                {
                                    File.Copy("ffxiiicrypt.exe", inFilelistFileDir + "\\ffxiiicrypt.exe");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Error: Unable to locate ffxiiicrypt tool in the main app folder to decrypt the filelist file");
                                logWriter.WriteLine("Error: Unable to locate ffxiiicrypt tool in the main app folder to decrypt the filelist file");
                                CmnMethods.ErrorExit("");
                            }
                        }
                    }


                    try
                    {
                        // According to the game code and the filelist name, decide whether
                        // to decrypt and trim the filelist file for extraction
                        switch (gameCodeVar)
                        {
                            case CmnEnums.GameCodes.ff131:
                                lock (_lockObject)
                                {
                                    Console.WriteLine("Game is set to 13-1");
                                    logWriter.WriteLine("Game is set to 13-1");
                                }
                                break;

                            case CmnEnums.GameCodes.ff132:
                                lock (_lockObject)
                                {
                                    Console.WriteLine("Game is set to 13-2 / 13-LR");
                                    logWriter.WriteLine("Game is set to 13-2 / 13-LR");
                                }

                                if (!unEncryptedFilelists.Contains(filelistName))
                                {
                                    CmnMethods.IfFileExistsDel(tmpDcryptFilelistFile);

                                    File.Copy(filelistFileVar, tmpDcryptFilelistFile);

                                    CmnMethods.FFXiiiCryptTool(inFilelistFileDir, " -d ", "\"" + tmpDcryptFilelistFile + "\"", ref cryptFilelistCode);

                                    File.Move(filelistFileVar, backupOldFilelistFile);

                                    // Copy the decrypted filelist data from offset 32 onwards
                                    // from the temp file that you renamed after decrypted, to a file
                                    // that is with the same name as the original filelist name
                                    using (var toAdjust = new FileStream(tmpDcryptFilelistFile, FileMode.Open, FileAccess.Read))
                                    {
                                        // Store the filelist data in a separate filelist file
                                        using (var adjusted = new FileStream(filelistFileVar, FileMode.OpenOrCreate, FileAccess.Write))
                                        {
                                            toAdjust.Seek(32, SeekOrigin.Begin);
                                            toAdjust.CopyTo(adjusted);

                                            // Store the encryption header data in a separate file
                                            using (var encryptedHeader = new FileStream("_encryptionHeader.bin", FileMode.OpenOrCreate, FileAccess.Write))
                                            {
                                                toAdjust.Seek(0, SeekOrigin.Begin);
                                                var encryptionBuffer = new byte[32];
                                                var encryptionBytesRead = toAdjust.Read(encryptionBuffer, 0, encryptionBuffer.Length);
                                                encryptedHeader.Write(encryptionBuffer, 0, encryptionBytesRead);
                                            }
                                        }
                                    }

                                    File.Delete(tmpDcryptFilelistFile);
                                }
                                break;
                        }


                        // Initialise variables to commonly use
                        var chunksInfoStartPos = (uint)0;
                        var chunksStartPos = (uint)0;
                        var chunkFNameCount = (uint)0;
                        var totalChunks = (uint)0;
                        var lastChunkFileNumber = (uint)1000;

                        // Set the values to the initialised variables
                        using (var baseFilelist = new FileStream(filelistFileVar, FileMode.Open, FileAccess.Read))
                        {
                            using (var baseFilelistReader = new BinaryReader(baseFilelist))
                            {
                                baseFilelistReader.BaseStream.Position = 0;
                                chunksInfoStartPos = baseFilelistReader.ReadUInt32();
                                chunksStartPos = baseFilelistReader.ReadUInt32();
                                var totalFiles = baseFilelistReader.ReadUInt32();

                                var chunkInfoSize = chunksStartPos - chunksInfoStartPos;
                                totalChunks = chunkInfoSize / 12;

                                lock (_lockObject)
                                {
                                    Console.WriteLine("TotalChunks: " + totalChunks);
                                    logWriter.WriteLine("TotalChunks: " + totalChunks);

                                    Console.WriteLine("No of files: " + totalFiles + "\n");
                                    logWriter.WriteLine("No of files: " + totalFiles + "\n");
                                }

                                // Make a memorystream for holding all Chunks info
                                using (var chunkInfoStream = new MemoryStream())
                                {
                                    baseFilelist.Seek(chunksInfoStartPos, SeekOrigin.Begin);
                                    var chunkInfoBuffer = new byte[chunkInfoSize];
                                    var chunkBytesRead = baseFilelist.Read(chunkInfoBuffer, 0, chunkInfoBuffer.Length);
                                    chunkInfoStream.Write(chunkInfoBuffer, 0, chunkBytesRead);

                                    // Make memorystream for all Chunks compressed data
                                    using (var chunkStream = new MemoryStream())
                                    {
                                        baseFilelist.Seek(chunksStartPos, SeekOrigin.Begin);
                                        baseFilelist.CopyTo(chunkStream);

                                        // Open a binary reader and read each chunk's info and
                                        // dump them as separate files
                                        using (var chunkInfoReader = new BinaryReader(chunkInfoStream))
                                        {
                                            var chunkInfoReadVal = (uint)0;
                                            for (int c = 0; c < totalChunks; c++)
                                            {
                                                chunkInfoReader.BaseStream.Position = chunkInfoReadVal + 4;
                                                var chunkCmpSize = chunkInfoReader.ReadUInt32();
                                                var chunkDataStart = chunkInfoReader.ReadUInt32();

                                                chunkStream.Seek(chunkDataStart, SeekOrigin.Begin);
                                                using (var chunkToDcmp = new MemoryStream())
                                                {
                                                    var chunkBuffer = new byte[chunkCmpSize];
                                                    var readCmpBytes = chunkStream.Read(chunkBuffer, 0, chunkBuffer.Length);
                                                    chunkToDcmp.Write(chunkBuffer, 0, readCmpBytes);

                                                    using (var chunksOutStream = new FileStream(defChunkFile + chunkFNameCount, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                                                    {
                                                        chunkToDcmp.Seek(0, SeekOrigin.Begin);
                                                        ZlibLibrary.ZlibDecompress(chunkToDcmp, chunksOutStream);
                                                    }
                                                }

                                                chunkInfoReadVal += 12;
                                                chunkFNameCount++;
                                            }
                                        }
                                    }
                                }


                                // Compress each file into the white image archive section
                                // Open a chunk file and start the repacking process
                                chunkFNameCount = 0;
                                for (int ch = 0; ch < totalChunks; ch++)
                                {
                                    // Get the total number of files in a chunk file by counting the number of times
                                    // an null character occurs in the chunk file
                                    var filesInChunkCount = (uint)0;
                                    using (var fileCountReader = new StreamReader(defChunkFile + chunkFNameCount))
                                    {
                                        while (!fileCountReader.EndOfStream)
                                        {
                                            var currentNullChar = fileCountReader.Read();
                                            if (currentNullChar == 0)
                                            {
                                                filesInChunkCount++;
                                            }
                                        }
                                    }

                                    // Open a chunk file for reading
                                    using (var currentChunk = new FileStream(defChunkFile + chunkFNameCount, FileMode.Open, FileAccess.Read))
                                    {
                                        using (var chunkStringReader = new BinaryReader(currentChunk))
                                        {
                                            // Create a new chunk file with append mode for writing updated values back to the 
                                            // filelist file
                                            using (var updChunk = new FileStream(newChunkFile + chunkFNameCount, FileMode.Append, FileAccess.Write))
                                            {
                                                using (var updChunkStrings = new StreamWriter(updChunk))
                                                {

                                                    // Compress files in a chunk into the archive 
                                                    var chunkStringReaderPos = (uint)0;
                                                    for (int f = 0; f < filesInChunkCount; f++)
                                                    {
                                                        chunkStringReader.BaseStream.Position = chunkStringReaderPos;
                                                        var parsedString = new StringBuilder();
                                                        char getParsedString;
                                                        while ((getParsedString = chunkStringReader.ReadChar()) != default)
                                                        {
                                                            parsedString.Append(getParsedString);
                                                        }
                                                        var parsed = parsedString.ToString();

                                                        if (parsed.StartsWith("end"))
                                                        {
                                                            updChunkStrings.Write("end\0");
                                                            lastChunkFileNumber = chunkFNameCount;
                                                            break;
                                                        }

                                                        string[] data = parsed.Split(':');
                                                        var ogFilePos = Convert.ToUInt32(data[0], 16) * 2048;
                                                        var ogUSize = Convert.ToUInt32(data[1], 16);
                                                        var ogCSize = Convert.ToUInt32(data[2], 16);
                                                        var mainPath = data[3];
                                                        var directoryPath = Path.GetDirectoryName(mainPath);
                                                        var fileName = Path.GetFileName(mainPath);
                                                        var fullFilePath = extractedDir + "\\" + directoryPath + "\\" + fileName;

                                                        // Assign values to the variables to ensure that 
                                                        // they get modified only when the file to repack
                                                        // is found
                                                        uint newFilePos = ogFilePos;
                                                        uint newUcmpSize = ogUSize;
                                                        uint newCmpSize = ogCSize;
                                                        var asciCmpSize = "";
                                                        var asciUcmpSize = "";
                                                        var asciFilePos = "";
                                                        var packedState = "";
                                                        var packedAs = "";
                                                        var compressedState = false;

                                                        if (!ogUSize.Equals(ogCSize))
                                                        {
                                                            compressedState = true;
                                                            packedState = "Compressed";
                                                        }
                                                        else
                                                        {
                                                            compressedState = false;
                                                            packedState = "Copied";
                                                        }

                                                        // Repack a specific file
                                                        if (mainPath.Equals(whiteFilePathVar))
                                                        {
                                                            using (var cleanBin = new FileStream(whiteBinFileVar, FileMode.Open, FileAccess.Write))
                                                            {
                                                                cleanBin.Seek(ogFilePos, SeekOrigin.Begin);
                                                                for (int pad = 0; pad < ogCSize; pad++)
                                                                {
                                                                    cleanBin.WriteByte(0);
                                                                }
                                                            }

                                                            // According to the compressed state, compress or
                                                            // copy the file
                                                            switch (compressedState)
                                                            {
                                                                case true:
                                                                    // Compress the file and get its uncompressed
                                                                    // and compressed size
                                                                    var createFile = File.Create(tmpCmpDataFile);
                                                                    createFile.Close();

                                                                    ZlibLibrary.ZlibCompress(fullFilePath, tmpCmpDataFile, Ionic.Zlib.CompressionLevel.Level9);

                                                                    var ucmpDataInfo = new FileInfo(fullFilePath);
                                                                    newUcmpSize = (uint)ucmpDataInfo.Length;

                                                                    var cmpDataInfo = new FileInfo(tmpCmpDataFile);
                                                                    newCmpSize = (uint)cmpDataInfo.Length;

                                                                    // Open the compressed file in a stream and
                                                                    // decide whether to inject or append the
                                                                    // compressed file
                                                                    using (var cmpDataStream = new FileStream(tmpCmpDataFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                                                                    {
                                                                        // If file is smaller or same as original, then inject
                                                                        // the file at the original position
                                                                        if (newCmpSize < ogCSize || newCmpSize.Equals(ogCSize))
                                                                        {
                                                                            packedAs = " (Injected)";
                                                                            newFilePos = ogFilePos;

                                                                            using (var injectWhiteBin = new FileStream(whiteBinFileVar, FileMode.Open, FileAccess.Write))
                                                                            {
                                                                                injectWhiteBin.Seek(ogFilePos, SeekOrigin.Begin);
                                                                                cmpDataStream.CopyTo(injectWhiteBin);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            // If file is larger, then append
                                                                            // the file at the end
                                                                            using (var appendWhiteBin = new FileStream(whiteBinFileVar, FileMode.Append, FileAccess.Write))
                                                                            {
                                                                                packedAs = " (Appended)";
                                                                                newFilePos = (uint)appendWhiteBin.Length;

                                                                                // Check if file position is divisible by 2048
                                                                                // and if its not divisible, add in null bytes
                                                                                // till next closest divisible number
                                                                                if (newFilePos % 2048 != 0)
                                                                                {
                                                                                    var remainder = newFilePos % 2048;
                                                                                    var increaseBytes = 2048 - remainder;
                                                                                    var newPos = newFilePos + increaseBytes;
                                                                                    var padNulls = newPos - newFilePos;

                                                                                    appendWhiteBin.Seek(newFilePos, SeekOrigin.Begin);
                                                                                    for (int pad = 0; pad < padNulls; pad++)
                                                                                    {
                                                                                        appendWhiteBin.WriteByte(0);
                                                                                    }
                                                                                    newFilePos = (uint)appendWhiteBin.Length;
                                                                                }

                                                                                appendWhiteBin.Seek(newFilePos, SeekOrigin.Begin);
                                                                                cmpDataStream.CopyTo(appendWhiteBin);
                                                                            }
                                                                        }
                                                                    }
                                                                    File.Delete(tmpCmpDataFile);
                                                                    break;

                                                                case false:
                                                                    // Get the file size and copy the file
                                                                    var copyTypeFileInfo = new FileInfo(fullFilePath);
                                                                    newUcmpSize = (uint)copyTypeFileInfo.Length;
                                                                    newCmpSize = newUcmpSize;

                                                                    // Open the file in a stream and decide whether
                                                                    // to inject or append the compressed file
                                                                    using (var copyTypeFileStream = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read))
                                                                    {
                                                                        // If file is smaller or same as original, then inject
                                                                        // the file at the original position
                                                                        if (newUcmpSize < ogUSize || newUcmpSize == ogUSize)
                                                                        {
                                                                            packedAs = " (Injected)";
                                                                            newFilePos = ogFilePos;

                                                                            using (var injectWhiteBin = new FileStream(whiteBinFileVar, FileMode.Open, FileAccess.Write))
                                                                            {
                                                                                injectWhiteBin.Seek(ogFilePos, SeekOrigin.Begin);
                                                                                copyTypeFileStream.CopyTo(injectWhiteBin);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            // If file is larger, then append
                                                                            // the file at the end
                                                                            using (var appendWhiteBin = new FileStream(whiteBinFileVar, FileMode.Append, FileAccess.Write))
                                                                            {
                                                                                packedAs = " (Appended)";
                                                                                newFilePos = (uint)appendWhiteBin.Length;

                                                                                // Check if file position is divisible by 2048
                                                                                // and if its not divisible, add in null bytes
                                                                                // till next closest divisible number
                                                                                if (newFilePos % 2048 != 0)
                                                                                {
                                                                                    var remainder = newFilePos % 2048;
                                                                                    var increaseBytes = 2048 - remainder;
                                                                                    var newPos = newFilePos + increaseBytes;
                                                                                    var padNulls = newPos - newFilePos;

                                                                                    appendWhiteBin.Seek(newFilePos, SeekOrigin.Begin);
                                                                                    for (int pad = 0; pad < padNulls; pad++)
                                                                                    {
                                                                                        appendWhiteBin.WriteByte(0);
                                                                                    }
                                                                                    newFilePos = (uint)appendWhiteBin.Length;
                                                                                }

                                                                                appendWhiteBin.Seek(newFilePos, SeekOrigin.Begin);
                                                                                copyTypeFileStream.CopyTo(appendWhiteBin);
                                                                            }
                                                                        }
                                                                    }
                                                                    break;
                                                            }

                                                            lock (_lockObject)
                                                            {
                                                                Console.WriteLine(packedState + " " + whiteBinFolderName + "/" + mainPath + packedAs);
                                                                logWriter.WriteLine(packedState + " " + whiteBinFolderName + "/" + mainPath + packedAs);
                                                            }
                                                        }

                                                        newFilePos /= 2048;
                                                        CmnMethods.DecToHex(newFilePos, ref asciFilePos);
                                                        CmnMethods.DecToHex(newUcmpSize, ref asciUcmpSize);
                                                        CmnMethods.DecToHex(newCmpSize, ref asciCmpSize);

                                                        var newUpdatedPath = asciFilePos + ":" + asciUcmpSize + ":" + asciCmpSize + ":" + mainPath + "\0";
                                                        updChunkStrings.Write(newUpdatedPath);

                                                        chunkStringReaderPos = (uint)chunkStringReader.BaseStream.Position;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    chunkFNameCount++;
                                }


                                // Fileinfo updating and chunk compression section
                                // Copy the base filelist file's data into the new filelist file till the chunk data begins
                                var appendAt = (uint)0;
                                using (var newFilelist = new FileStream(newFileListFile, FileMode.Append, FileAccess.Write))
                                {
                                    using (var newFilelistWriter = new BinaryWriter(newFilelist))
                                    {
                                        baseFilelist.Seek(0, SeekOrigin.Begin);
                                        var newFilelistBuffer = new byte[chunksStartPos];
                                        var newFilelistBytesRead = baseFilelist.Read(newFilelistBuffer, 0, newFilelistBuffer.Length);
                                        newFilelist.Write(newFilelistBuffer, 0, newFilelistBytesRead);

                                        // Compress and append multiple chunks to the new filelist file
                                        chunkFNameCount = 0;
                                        var chunkInfoWriterPos = chunksInfoStartPos;
                                        var chunkCmpSize = (uint)0;
                                        var chunkUncmpSize = (uint)0;
                                        var chunkStartVal = (uint)0;
                                        var fileInfoWriterPos = 18;
                                        if (gameCodeVar.Equals(CmnEnums.GameCodes.ff132))
                                        {
                                            // Change Fileinfo writer position
                                            // according to the game code 
                                            fileInfoWriterPos = 16;
                                        }
                                        for (int ac = 0; ac < totalChunks; ac++)
                                        {
                                            // Get total number of files in the chunk and decrease the filecount by 1 if the 
                                            // the lastchunk number matches with the current chunk number running in this for loop
                                            var filesInChunkCount = (uint)0;
                                            using (var fileCountReader = new StreamReader(newChunkFile + chunkFNameCount))
                                            {
                                                while (!fileCountReader.EndOfStream)
                                                {
                                                    var currentNullChar = fileCountReader.Read();
                                                    if (currentNullChar == 0)
                                                    {
                                                        filesInChunkCount++;
                                                    }
                                                }
                                            }

                                            if (lastChunkFileNumber.Equals(chunkFNameCount))
                                            {
                                                filesInChunkCount--;
                                            }

                                            // Get each file strings start position in a chunk and update the position
                                            // value in the info section of the new filelist file
                                            using (var fileStrings = new FileStream(newChunkFile + chunkFNameCount, FileMode.Open, FileAccess.Read))
                                            {
                                                using (var fileStringsReader = new BinaryReader(fileStrings))
                                                {
                                                    var filePosInChunk = (UInt16)0;
                                                    var filePosInChunkToWrite = (UInt16)0;
                                                    for (int fic = 0; fic < filesInChunkCount; fic++)
                                                    {
                                                        // According to the game code, check how to
                                                        // write the value and then set the appropriate
                                                        // converted value to write
                                                        if (gameCodeVar.Equals(CmnEnums.GameCodes.ff132))
                                                        {
                                                            baseFilelistReader.BaseStream.Position = fileInfoWriterPos;
                                                            var checkVal = baseFilelistReader.ReadUInt16();

                                                            if (checkVal > 32767)
                                                            {
                                                                filePosInChunkToWrite = (ushort)(filePosInChunkToWrite + 32768);
                                                            }
                                                        }

                                                        CmnMethods.AdjustBytesUInt16(newFilelistWriter, fileInfoWriterPos, out byte[] adjustFilePosInChunk, filePosInChunkToWrite);

                                                        fileStringsReader.BaseStream.Position = filePosInChunk;
                                                        var parsedVal = new StringBuilder();
                                                        char getParsedVal;
                                                        while ((getParsedVal = fileStringsReader.ReadChar()) != default)
                                                        {
                                                            parsedVal.Append(getParsedVal);
                                                        }

                                                        filePosInChunk = (UInt16)fileStringsReader.BaseStream.Position;
                                                        filePosInChunkToWrite = (UInt16)fileStringsReader.BaseStream.Position;
                                                        fileInfoWriterPos += 8;
                                                    }
                                                }
                                            }


                                            // Compress and package a chunk back into the new filelist file and update the 
                                            // offsets in the chunk info section of the filelist file
                                            appendAt = (uint)newFilelist.Length;
                                            newFilelist.Seek(appendAt, SeekOrigin.Begin);

                                            var chunkDataInfo = new FileInfo(newChunkFile + chunkFNameCount);
                                            chunkUncmpSize = (uint)chunkDataInfo.Length;

                                            var createChunkFile = File.Create(tmpCmpChunkFile);
                                            createChunkFile.Close();

                                            ZlibLibrary.ZlibCompress(newChunkFile + chunkFNameCount, tmpCmpChunkFile, Ionic.Zlib.CompressionLevel.Level9);

                                            using (var cmpChunkDataStream = new FileStream(tmpCmpChunkFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                                            {
                                                cmpChunkDataStream.Seek(0, SeekOrigin.Begin);
                                                cmpChunkDataStream.CopyTo(newFilelist);

                                                var cmpChunkDataInfo = new FileInfo(tmpCmpChunkFile);
                                                chunkCmpSize = (uint)cmpChunkDataInfo.Length;
                                            }
                                            File.Delete(tmpCmpChunkFile);

                                            CmnMethods.AdjustBytesUInt32(newFilelistWriter, chunkInfoWriterPos,
                                                out byte[] adjustChunkUnCmpSize, chunkUncmpSize, "le");
                                            CmnMethods.AdjustBytesUInt32(newFilelistWriter, chunkInfoWriterPos + 4,
                                                out byte[] adjustChunkCmpSize, chunkCmpSize, "le");
                                            CmnMethods.AdjustBytesUInt32(newFilelistWriter, chunkInfoWriterPos + 8,
                                                out byte[] adjustChunkStart, chunkStartVal, "le");

                                            var newChunkStartVal = chunkStartVal + chunkCmpSize;
                                            chunkStartVal = newChunkStartVal;

                                            chunkInfoWriterPos += 12;
                                            chunkFNameCount++;
                                        }
                                    }
                                }
                            }
                        }

                        Directory.Delete(defaultChunksExtDir, true);
                        Directory.Delete(newChunksExtDir, true);


                        // Make a backup of the old filelist file according to the game code 
                        // and if that filelist file is inside the unencrypted filelist array
                        if (gameCodeVar.Equals(CmnEnums.GameCodes.ff131))
                        {
                            File.Copy(filelistFileVar, backupOldFilelistFile);
                        }
                        if (unEncryptedFilelists.Contains(filelistName))
                        {
                            File.Copy(filelistFileVar, backupOldFilelistFile);
                        }

                        // Delete the old filelist file and rename the new filelist file
                        // to the old filelist file name
                        File.Delete(filelistFileVar);
                        File.Move(newFileListFile, filelistFileVar);


                        // Re Encrypt filelist file and add the neccessary encryption header if the game code is set to 2
                        if (gameCodeVar.Equals(CmnEnums.GameCodes.ff132))
                        {
                            if (!unEncryptedFilelists.Contains(filelistName))
                            {
                                var maxFilelistSize = (uint)0;

                                // Rename the filelist file to temp filelist name
                                File.Move(filelistFileVar, tmpDcryptFilelistFile);

                                // Copy the encrypted header data and the filelist data to
                                // the filelist file 
                                // Open the final filelist
                                using (var encryptedFilelist = new FileStream(filelistFileVar, FileMode.Append, FileAccess.Write))
                                {
                                    using (var encryptedFilelistWriter = new BinaryWriter(encryptedFilelist))
                                    {
                                        // Encryption header copy
                                        using (var encryptedData = new FileStream("_encryptionHeader.bin", FileMode.Open, FileAccess.Read))
                                        {
                                            encryptedData.Seek(0, SeekOrigin.Begin);
                                            encryptedData.CopyTo(encryptedFilelist);

                                            // NewFilelist data copy 
                                            using (var newFilelistData = new FileStream(tmpDcryptFilelistFile, FileMode.Open, FileAccess.Read))
                                            {
                                                var filelistSize = (uint)newFilelistData.Length;
                                                newFilelistData.Seek(0, SeekOrigin.Begin);
                                                newFilelistData.CopyTo(encryptedFilelist);

                                                if (filelistSize % 8 != 0)
                                                {
                                                    // Get remainder from the division and
                                                    // reduce the remainder with 8. set that
                                                    // reduced value to a variable
                                                    var remainder = filelistSize % 8;
                                                    var increaseByteAmount = 8 - remainder;

                                                    // Increase the filelist size with the
                                                    // increase byte variable from the previous step and
                                                    // set this as a variable
                                                    // Then get the amount of null bytes to pad by subtracting 
                                                    // the new size  with the filelist size
                                                    var newSize = filelistSize + increaseByteAmount;
                                                    var paddingNulls = newSize - filelistSize;

                                                    encryptedFilelist.Seek((uint)encryptedFilelist.Length, SeekOrigin.Begin);
                                                    for (int padding = 0; padding < paddingNulls; padding++)
                                                    {
                                                        encryptedFilelist.WriteByte(0);
                                                    }

                                                    filelistSize = newSize;
                                                }

                                                CmnMethods.AdjustBytesUInt32(encryptedFilelistWriter, 16, out byte[] adjTotalFilelistSize, filelistSize, "be");

                                                encryptedFilelist.Seek(0, SeekOrigin.Begin);
                                                maxFilelistSize = (uint)encryptedFilelist.Length;

                                                encryptedFilelistWriter.BaseStream.Position = (uint)encryptedFilelist.Length;
                                                encryptedFilelistWriter.Write(filelistSize);

                                                encryptedFilelist.Seek((uint)encryptedFilelist.Length, SeekOrigin.Begin);
                                                for (int n = 0; n < 12; n++)
                                                {
                                                    encryptedFilelist.WriteByte(0);
                                                }
                                            }
                                        }
                                    }
                                }

                                // Write checksum to the filelist file
                                var cryptAsciiSize = "";
                                CmnMethods.DecToHex(maxFilelistSize, ref cryptAsciiSize);
                                var checkSumActionArg = " 000" + cryptAsciiSize + cryptCheckSumCode;

                                CmnMethods.FFXiiiCryptTool(inFilelistFileDir, " -c ", "\"" + filelistFileVar + "\"", ref checkSumActionArg);
                                Console.WriteLine("\nWrote new checksum to the filelist");
                                logWriter.WriteLine("\nWrote new checksum to the filelist");

                                // Delete the encryption header data file
                                File.Delete("_encryptionHeader.bin");

                                // Delete the temp filelist file
                                File.Delete(tmpDcryptFilelistFile);

                                // Encrypt the filelist file                 
                                CmnMethods.FFXiiiCryptTool(inFilelistFileDir, " -e ", "\"" + filelistFileVar + "\"", ref cryptFilelistCode);
                                Console.WriteLine("\nEncrypted filelist file");
                                logWriter.WriteLine("\nEncrypted filelist file");
                            }
                        }


                        Console.WriteLine("\nFinished repacking file to " + whiteBinFileVar);
                        logWriter.WriteLine("\nFinished repacking file to " + whiteBinFileVar);
                    }
                    catch (Exception ex)
                    {
                        if (File.Exists(filelistFileVar + ".bak"))
                        {
                            File.Delete(filelistFileVar);
                            File.Move(filelistFileVar + ".bak", filelistFileVar);
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
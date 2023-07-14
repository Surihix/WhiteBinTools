using System;
using System.IO;
using System.Linq;
using System.Text;
using WhiteBinTools.src.Common;

namespace WhiteBinTools.src.Unpack
{
    internal class BinUnpack
    {
        private static readonly object _lockObject = new object();
        public static void Unpack(CmnEnums.GameCodes gameCodeVar, string filelistFileVar, string whiteBinFileVar)
        {
            using (var logStream = new FileStream("ProcessLog.txt", FileMode.Append, FileAccess.Write, FileShare.Write))
            {
                using (var logWriter = new StreamWriter(logStream))
                {

                    // Check if the filelist file and the white image bin file exists
                    if (!File.Exists(filelistFileVar))
                    {
                        Console.WriteLine("Error: Filelist file specified in the argument is missing");
                        logWriter.WriteLine("Error: Filelist file specified in the argument is missing");
                        CmnMethods.ErrorExit("");
                    }
                    if (!File.Exists(whiteBinFileVar))
                    {
                        Console.WriteLine("Error: Image bin file specified in the argument is missing");
                        logWriter.WriteLine("Error: Image bin file specified in the argument is missing");
                        CmnMethods.ErrorExit("");
                    }


                    // Set the filelist and the white image bin file names
                    var filelistName = Path.GetFileName(filelistFileVar);
                    var whiteBinName = Path.GetFileName(whiteBinFileVar);


                    // Set directories and file paths for the filelist files and the white bin files
                    var inFilelistFilePath = Path.GetFullPath(filelistFileVar);
                    var inBinFilePath = Path.GetFullPath(whiteBinFileVar);
                    var inFilelistFileDir = Path.GetDirectoryName(inFilelistFilePath);
                    var inBinFileDir = Path.GetDirectoryName(inBinFilePath);
                    var tmpDcryptFilelistFile = inFilelistFileDir + "\\filelist_tmp.bin";
                    var extractDirName = Path.GetFileNameWithoutExtension(whiteBinFileVar).Replace(".win32", "_win32").
                        Replace(".ps3", "_ps3").Replace(".x360", "_x360");
                    var extractDir = inBinFileDir + "\\" + extractDirName;
                    var defaultChunksExtDir = extractDir + "\\_chunks";
                    var chunkFile = defaultChunksExtDir + "\\chunk_";


                    // Check and delete backup filelist file if it exists
                    CmnMethods.IfFileExistsDel(filelistFileVar + ".bak");

                    // Check and delete extracted directory if they exist in the
                    // folder where they are supposed to be extracted
                    if (Directory.Exists(extractDir))
                    {
                        Console.WriteLine("Detected previous unpack. deleting....");
                        Directory.Delete(extractDir, true);
                        Console.Clear();
                    }

                    Directory.CreateDirectory(extractDir);
                    Directory.CreateDirectory(defaultChunksExtDir);


                    // Store a list of unencrypted filelist files
                    string[] unEncryptedFilelists = { "movielista.win32.bin", "movielistv.win32.bin", "movielist.win32.bin" };


                    // Check for encryption header in the filelist file, if the
                    // game code is set to ff13-1
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

                                    var cryptFilelistCode = " filelist";

                                    CmnMethods.FFXiiiCryptTool(inFilelistFileDir, " -d ", "\"" + tmpDcryptFilelistFile + "\"", ref cryptFilelistCode);

                                    File.Move(filelistFileVar, filelistFileVar + ".bak");

                                    using (var toAdjust = new FileStream(tmpDcryptFilelistFile, FileMode.Open, FileAccess.Read))
                                    {
                                        using (var adjusted = new FileStream(filelistFileVar, FileMode.OpenOrCreate, FileAccess.Write))
                                        {
                                            toAdjust.Seek(32, SeekOrigin.Begin);
                                            toAdjust.CopyTo(adjusted);
                                        }
                                    }

                                    File.Delete(tmpDcryptFilelistFile);
                                }
                                break;
                        }


                        // Process File chunks section
                        // Intialize the variables required for extraction
                        var chunkFNameCount = (uint)0;
                        var totalChunks = (uint)0;
                        var totalFiles = (uint)0;

                        using (var filelist = new FileStream(filelistFileVar, FileMode.Open, FileAccess.Read))
                        {
                            using (var filelistReader = new BinaryReader(filelist))
                            {
                                filelistReader.BaseStream.Position = 0;
                                var chunksInfoStartPos = filelistReader.ReadUInt32();
                                var chunksStartPos = filelistReader.ReadUInt32();
                                totalFiles = filelistReader.ReadUInt32();

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
                                    filelist.Seek(chunksInfoStartPos, SeekOrigin.Begin);
                                    var chunkInfoBuffer = new byte[chunkInfoSize];
                                    var chunkBytesRead = filelist.Read(chunkInfoBuffer, 0, chunkInfoBuffer.Length);
                                    chunkInfoStream.Write(chunkInfoBuffer, 0, chunkBytesRead);

                                    // Make memorystream for all Chunks compressed data
                                    using (var chunkStream = new MemoryStream())
                                    {
                                        filelist.Seek(chunksStartPos, SeekOrigin.Begin);
                                        filelist.CopyTo(chunkStream);

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

                                                    using (var chunksOutStream = new FileStream(chunkFile + chunkFNameCount, FileMode.OpenOrCreate, FileAccess.ReadWrite))
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
                            }
                        }


                        // Extracting files section 
                        chunkFNameCount = 0;
                        var countDuplicate = 1;
                        for (int ch = 0; ch < totalChunks; ch++)
                        {
                            // Get the total number of files in a chunk file by counting the number of times
                            // an null character occurs in the chunk file
                            var filesInChunkCount = (uint)0;
                            using (var fileCountReader = new StreamReader(defaultChunksExtDir + "/chunk_" + chunkFNameCount))
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
                            using (var currentChunk = new FileStream(chunkFile + chunkFNameCount, FileMode.Open, FileAccess.Read))
                            {
                                using (var chunkStringReader = new BinaryReader(currentChunk))
                                {
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
                                            break;
                                        }

                                        string[] data = parsed.Split(':');
                                        var position = Convert.ToUInt32(data[0], 16) * 2048;
                                        var uncmpSize = Convert.ToUInt32(data[1], 16);
                                        var cmpSize = Convert.ToUInt32(data[2], 16);
                                        var mainPath = data[3].Replace("/", "\\");

                                        var directoryPath = Path.GetDirectoryName(mainPath);
                                        var fileName = Path.GetFileName(mainPath);
                                        var fullFilePath = extractDir + "\\" + directoryPath + "\\" + fileName;
                                        var compressedState = false;
                                        var unpackedState = "";

                                        if (!uncmpSize.Equals(cmpSize))
                                        {
                                            compressedState = true;
                                            unpackedState = "Decompressed";
                                        }
                                        else
                                        {
                                            compressedState = false;
                                            unpackedState = "Copied";
                                        }

                                        using (var whiteBin = new FileStream(whiteBinFileVar, FileMode.Open, FileAccess.Read))
                                        {
                                            if (!Directory.Exists(extractDir + "\\" + directoryPath))
                                            {
                                                Directory.CreateDirectory(extractDir + "\\" + directoryPath);
                                            }
                                            if (File.Exists(fullFilePath))
                                            {
                                                File.Delete(fullFilePath);
                                                countDuplicate++;
                                            }

                                            switch (compressedState)
                                            {
                                                case true:
                                                    using (var cmpData = new MemoryStream())
                                                    {
                                                        whiteBin.CopyTo(cmpData, position, cmpSize);

                                                        using (var outFile = new FileStream(fullFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                                                        {
                                                            cmpData.Seek(0, SeekOrigin.Begin);
                                                            ZlibLibrary.ZlibDecompress(cmpData, outFile);
                                                        }
                                                    }
                                                    break;

                                                case false:
                                                    using (var outFile = new FileStream(fullFilePath, FileMode.OpenOrCreate, FileAccess.Write))
                                                    {
                                                        outFile.Seek(0, SeekOrigin.Begin);
                                                        whiteBin.CopyTo(outFile, position, uncmpSize);
                                                    }
                                                    break;
                                            }
                                        }

                                        lock (_lockObject)
                                        {
                                            Console.WriteLine(unpackedState + " " + extractDirName + "\\" + mainPath);
                                            logWriter.WriteLine(unpackedState + " " + extractDirName + "\\" + mainPath);
                                        }

                                        chunkStringReaderPos = (uint)chunkStringReader.BaseStream.Position;
                                    }
                                }
                            }

                            chunkFNameCount++;
                        }

                        Directory.Delete(defaultChunksExtDir, true);


                        // Restore old filefile file if game code is
                        // set to 2 and if the filelist file is not encrypted
                        if (gameCodeVar.Equals(CmnEnums.GameCodes.ff132))
                        {
                            if (!unEncryptedFilelists.Contains(filelistName))
                            {
                                File.Delete(filelistFileVar);
                                File.Move(filelistFileVar + ".bak", filelistFileVar);
                            }
                        }


                        lock (_lockObject)
                        {
                            Console.WriteLine("");
                            Console.WriteLine("Finished extracting file " + whiteBinName);
                            logWriter.WriteLine("\nFinished extracting file " + whiteBinName);
                            if (countDuplicate > 1)
                            {
                                Console.WriteLine(countDuplicate + " duplicate file(s)");
                                logWriter.WriteLine(countDuplicate + " duplicate file(s)");
                            }
                        }
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
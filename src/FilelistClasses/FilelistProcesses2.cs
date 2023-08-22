using System.Diagnostics;
using System.IO;
using WhiteBinTools.RepackClasses;
using WhiteBinTools.SupportClasses;

namespace WhiteBinTools.FilelistClasses
{
    internal partial class FilelistProcesses
    {
        public static void PrepareFilelistVars(FilelistProcesses filelistVariables, string filelistFileVar)
        {
            filelistVariables.MainFilelistFile = filelistFileVar;

            var inFilelistFilePath = Path.GetFullPath(filelistVariables.MainFilelistFile);
            filelistVariables.MainFilelistDirectory = Path.GetDirectoryName(inFilelistFilePath);
            filelistVariables.TmpDcryptFilelistFile = filelistVariables.MainFilelistDirectory + "\\filelist_tmp.bin";
        }


        public static void DecryptProcess(CmnEnums.GameCodes gameCodeVar, FilelistProcesses filelistVariables, StreamWriter writerName)
        {
            // Check for encryption header in the filelist file,
            // if the game code is set to ff13-1
            if (gameCodeVar.Equals(CmnEnums.GameCodes.ff131))
            {
                filelistVariables.IsEncrypted = CheckIfEncrypted(filelistVariables.MainFilelistFile);

                if (filelistVariables.IsEncrypted.Equals(true))
                {
                    if (Directory.Exists(filelistVariables.DefaultChunksExtDir))
                    {
                        Directory.Delete(filelistVariables.DefaultChunksExtDir, true);
                    }

                    IOhelpers.LogMessage("Error: Detected encrypted filelist file. set the game code to '-ff132' for handling this type of filelist", writerName);

                    writerName.DisposeIfLogStreamOpen();
                    IOhelpers.ErrorExit("");
                }
            }


            // Check if the ffxiiicrypt tool is present in the filelist directory
            // and if it doesn't exist copy it to the directory from the app
            // directory.
            // If the ffxiiicrypt tool does not exist in app directory, then
            // throw a error and exit
            if (gameCodeVar.Equals(CmnEnums.GameCodes.ff132))
            {
                filelistVariables.IsEncrypted = CheckIfEncrypted(filelistVariables.MainFilelistFile);

                if (filelistVariables.IsEncrypted.Equals(true))
                {
                    filelistVariables.CryptToolPresentBefore = true;

                    if (!File.Exists(filelistVariables.MainFilelistDirectory + "\\ffxiiicrypt.exe"))
                    {
                        filelistVariables.CryptToolPresentBefore = false;

                        if (File.Exists("ffxiiicrypt.exe"))
                        {
                            File.Copy("ffxiiicrypt.exe", filelistVariables.MainFilelistDirectory + "\\ffxiiicrypt.exe");
                        }
                        else
                        {
                            IOhelpers.LogMessage("Error: Unable to locate ffxiiicrypt tool in the main app folder to decrypt the filelist file", writerName);

                            if (Directory.Exists(filelistVariables.DefaultChunksExtDir))
                            {
                                Directory.Delete(filelistVariables.DefaultChunksExtDir, true);
                            }

                            writerName.DisposeIfLogStreamOpen();
                            IOhelpers.ErrorExit("");
                        }
                    }
                }
            }


            // If the filelist is encrypted then decrypt the filelist file
            // by first creating a temp copy of the filelist 
            if (filelistVariables.IsEncrypted.Equals(true))
            {
                filelistVariables.TmpDcryptFilelistFile.IfFileExistsDel();
                File.Copy(filelistVariables.MainFilelistFile, filelistVariables.TmpDcryptFilelistFile);

                var cryptFilelistCode = " filelist";

                FFXiiiCryptTool(filelistVariables.MainFilelistDirectory, " -d ", "\"" + filelistVariables.TmpDcryptFilelistFile + "\"", ref cryptFilelistCode);

                filelistVariables.MainFilelistFile = filelistVariables.TmpDcryptFilelistFile;
            }
        }


        static bool CheckIfEncrypted(string filelistFileVar)
        {
            var isEncrypted = false;
            using (var encStream = new FileStream(filelistFileVar, FileMode.Open, FileAccess.Read))
            {
                using (var encStreamReader = new BinaryReader(encStream))
                {
                    encStreamReader.BaseStream.Position = 20;
                    var encHeaderNumber = encStreamReader.ReadUInt32();

                    if (encHeaderNumber == 501232760)
                    {
                        isEncrypted = true;
                    }
                }
            }

            return isEncrypted;
        }


        public static uint GetFilesInChunkCount(string chunkToRead)
        {
            var filesInChunkCount = (uint)0;
            using (var fileCountReader = new StreamReader(chunkToRead))
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

            return filesInChunkCount;
        }


        public static void EncryptProcess(RepackProcesses repackVariables, FilelistProcesses filelistVariables, StreamWriter writerName)
        {
            var filelistDataSize = (uint)0;

            // Check filelist size if divisibile by 4
            // and pad in null bytes if not divisible.
            // Also write some null bytes for the size 
            // and hash offsets
            using (var preEncryptedfilelist = new FileStream(repackVariables.NewFilelistFile, FileMode.Append, FileAccess.Write))
            {
                filelistDataSize = (uint)preEncryptedfilelist.Length - 32;

                if (filelistDataSize % 4 != 0)
                {
                    // Get remainder from the division and
                    // reduce the remainder with 4. set that
                    // reduced value to a variable
                    var remainder = filelistDataSize % 4;
                    var increaseByteAmount = 4 - remainder;

                    // Increase the filelist size with the
                    // increase byte variable from the previous step and
                    // set this as a variable
                    // Then get the amount of null bytes to pad by subtracting 
                    // the new size  with the filelist size
                    var newSize = filelistDataSize + increaseByteAmount;
                    var padNulls = newSize - filelistDataSize;

                    preEncryptedfilelist.Seek((uint)preEncryptedfilelist.Length, SeekOrigin.Begin);
                    for (int pad = 0; pad < padNulls; pad++)
                    {
                        preEncryptedfilelist.WriteByte(0);
                    }

                    filelistDataSize = newSize;
                }

                // Add 8 bytes for the size and hash
                // offsets and 8 null bytes
                preEncryptedfilelist.Seek((uint)preEncryptedfilelist.Length, SeekOrigin.Begin);
                for (int ofs = 0; ofs < 16; ofs++)
                {
                    preEncryptedfilelist.WriteByte(0);
                }
            }

            using (var filelistToEncrypt = new FileStream(repackVariables.NewFilelistFile, FileMode.Open, FileAccess.Write))
            {
                using (var filelistToEncryptWriter = new BinaryWriter(filelistToEncrypt))
                {
                    filelistToEncrypt.Seek(0, SeekOrigin.Begin);

                    filelistToEncryptWriter.AdjustBytesUInt32(16, filelistDataSize, CmnEnums.Endianness.BigEndian);
                    filelistToEncryptWriter.AdjustBytesUInt32((uint)filelistToEncrypt.Length - 16, filelistDataSize, CmnEnums.Endianness.LittleEndian);
                }
            }


            // Write checksum to the filelist file
            filelistDataSize += 32;
            var asciiSize = filelistDataSize.DecimalToAscii();
            var cryptCheckSumCode = " write";
            var checkSumActionArg = " 000" + asciiSize + cryptCheckSumCode;
            FFXiiiCryptTool(filelistVariables.MainFilelistDirectory, " -c ", "\"" + repackVariables.NewFilelistFile + "\"", ref checkSumActionArg);


            // Encrypt the filelist file
            var cryptFilelistCode = " filelist";
            FFXiiiCryptTool(filelistVariables.MainFilelistDirectory, " -e ", "\"" + repackVariables.NewFilelistFile + "\"", ref cryptFilelistCode);


            IOhelpers.LogMessage("\nFinished encrypting new filelist", writerName);
        }


        static void FFXiiiCryptTool(string cryptDir, string actionSwitch, string filelistName, ref string actionType)
        {
            using (Process xiiiCrypt = new Process())
            {
                xiiiCrypt.StartInfo.WorkingDirectory = cryptDir;
                xiiiCrypt.StartInfo.FileName = "ffxiiicrypt.exe";
                xiiiCrypt.StartInfo.Arguments = actionSwitch + filelistName + actionType;
                xiiiCrypt.StartInfo.UseShellExecute = true;
                xiiiCrypt.Start();
                xiiiCrypt.WaitForExit();
            }
        }
    }
}
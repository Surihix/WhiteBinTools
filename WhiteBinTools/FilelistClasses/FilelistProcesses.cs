using System.IO;
using System.Text;
using WhiteBinTools.CryptoClasses;
using WhiteBinTools.RepackClasses;
using WhiteBinTools.SupportClasses;
using static WhiteBinTools.SupportClasses.ProgramEnums;

namespace WhiteBinTools.FilelistClasses
{
    internal class FilelistProcesses
    {
        public static void PrepareFilelistVars(FilelistVariables filelistVariables, string filelistFile)
        {
            filelistVariables.MainFilelistFile = filelistFile;

            var inFilelistFilePath = Path.GetFullPath(filelistVariables.MainFilelistFile);
            filelistVariables.MainFilelistDirectory = Path.GetDirectoryName(inFilelistFilePath);
            filelistVariables.TmpDcryptFilelistFile = Path.Combine(filelistVariables.MainFilelistDirectory, "filelist_tmp.bin");
        }


        public static void DecryptProcess(GameCodes gameCode, FilelistVariables filelistVariables, StreamWriter writerName)
        {
            // Check for encryption header in the filelist file,
            // if the game code is set to ff13-1
            if (gameCode.Equals(GameCodes.ff131))
            {
                filelistVariables.IsEncrypted = CheckIfEncrypted(filelistVariables.MainFilelistFile);

                if (filelistVariables.IsEncrypted)
                {
                    IOhelpers.LogMessage("Error: Detected encrypted filelist file. set the game code to '-ff132' for handling this type of filelist", writerName);

                    writerName.DisposeIfLogStreamOpen();
                    IOhelpers.ErrorExit("");
                }
            }

            // Check for encryption header in the filelist file,
            // if the game code is set to ff13-2
            if (gameCode.Equals(GameCodes.ff132))
            {
                filelistVariables.IsEncrypted = CheckIfEncrypted(filelistVariables.MainFilelistFile);
            }

            // Check if the filelist is in decrypted
            // state and the length of the cryptBody
            // for divisibility.
            // If the file was decrypted then skip
            // decrypting it.
            // If the filelist is encrypted then
            // decrypt the filelist file by first
            // creating a temp copy of the filelist.            
            if (filelistVariables.IsEncrypted)
            {
                var wasDecrypted = false;

                using (var encCheckReader = new BinaryReader(File.Open(filelistVariables.MainFilelistFile, FileMode.Open, FileAccess.Read)))
                {
                    encCheckReader.BaseStream.Position = 16;
                    var cryptBodySize = encCheckReader.ReadBytesUInt32(true);
                    cryptBodySize += 8;

                    if (cryptBodySize % 8 != 0)
                    {
                        IOhelpers.LogMessage("Error: Length of the body to decrypt/encrypt is not valid", writerName);

                        writerName.DisposeIfLogStreamOpen();
                        IOhelpers.ErrorExit("");
                    }

                    encCheckReader.BaseStream.Position = 32 + cryptBodySize - 8;
                    cryptBodySize -= 8;

                    if (encCheckReader.ReadUInt32() == cryptBodySize)
                    {
                        wasDecrypted = true;
                    }
                }

                switch (wasDecrypted)
                {
                    case true:
                        filelistVariables.TmpDcryptFilelistFile.IfFileExistsDel();
                        File.Copy(filelistVariables.MainFilelistFile, filelistVariables.TmpDcryptFilelistFile);

                        filelistVariables.MainFilelistFile = filelistVariables.TmpDcryptFilelistFile;
                        break;

                    case false:
                        filelistVariables.TmpDcryptFilelistFile.IfFileExistsDel();
                        File.Copy(filelistVariables.MainFilelistFile, filelistVariables.TmpDcryptFilelistFile);

                        IOhelpers.LogMessage("\nDecrypting filelist file....", writerName);
                        CryptFilelist.ProcessFilelist(CryptActions.d, filelistVariables.TmpDcryptFilelistFile);

                        using (var decFilelistReader = new BinaryReader(File.Open(filelistVariables.TmpDcryptFilelistFile, FileMode.Open, FileAccess.Read)))
                        {
                            decFilelistReader.BaseStream.Position = 16;
                            var filelistDataSize = decFilelistReader.ReadBytesUInt32(true);
                            var hashOffset = 32 + filelistDataSize + 4;

                            decFilelistReader.BaseStream.Position = hashOffset;
                            var filelistHash = decFilelistReader.ReadUInt32();

                            if (filelistHash != CryptoFunctions.ComputeCheckSum(decFilelistReader, filelistDataSize / 4, 32))
                            {
                                decFilelistReader.Dispose();

                                var errorMsg = "Error: Filelist was not decrypted correctly";

                                IOhelpers.LogMessage(errorMsg, writerName);
                                writerName.DisposeIfLogStreamOpen();

                                IOhelpers.ErrorExit(errorMsg);
                            }
                        }

                        IOhelpers.LogMessage("Finished decrypting filelist file\n", writerName);

                        filelistVariables.MainFilelistFile = filelistVariables.TmpDcryptFilelistFile;
                        break;
                }
            }
        }


        public static bool CheckIfEncrypted(string filelistFile)
        {
            var isEncrypted = false;
            using (var encStream = new FileStream(filelistFile, FileMode.Open, FileAccess.Read))
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


        public static void GetCurrentFileEntry(GameCodes gameCode, BinaryReader entriesReader, long entriesReadPos, FilelistVariables filelistVariables)
        {
            entriesReader.BaseStream.Position = entriesReadPos;
            filelistVariables.FileCode = entriesReader.ReadUInt32();

            if (gameCode.Equals(GameCodes.ff131))
            {
                filelistVariables.ChunkNumber = entriesReader.ReadUInt16();
                filelistVariables.PathStringPos = entriesReader.ReadUInt16();

                GeneratePathString(filelistVariables.PathStringPos, filelistVariables.ChunkDataDict[filelistVariables.ChunkNumber], filelistVariables);
            }
            else if (gameCode.Equals(GameCodes.ff132))
            {
                filelistVariables.PathStringPos = entriesReader.ReadUInt16();
                filelistVariables.ChunkNumber = entriesReader.ReadByte();
                filelistVariables.UnkEntryVal = entriesReader.ReadByte();

                if (filelistVariables.PathStringPos == 0)
                {
                    filelistVariables.CurrentChunkNumber++;
                }

                if (filelistVariables.PathStringPos == 32768)
                {
                    filelistVariables.CurrentChunkNumber++;
                    filelistVariables.PathStringPos -= 32768;
                }

                if (filelistVariables.PathStringPos > 32768)
                {
                    filelistVariables.PathStringPos -= 32768;
                }

                GeneratePathString(filelistVariables.PathStringPos, filelistVariables.ChunkDataDict[filelistVariables.CurrentChunkNumber], filelistVariables);
            }
        }

        static void GeneratePathString(ushort pathPos, byte[] currentChunkData, FilelistVariables filelistVariables)
        {
            var length = 0;

            for (int i = pathPos; i < currentChunkData.Length && currentChunkData[i] != 0; i++)
            {
                length++;
            }

            filelistVariables.PathString = Encoding.UTF8.GetString(currentChunkData, pathPos, length);
        }


        public static void EncryptProcess(RepackVariables repackVariables, StreamWriter writerName)
        {
            var filelistDataSize = (uint)0;

            // Check filelist size if divisibile by 8
            // and pad in null bytes if not divisible.
            // Then write some null bytes for the size 
            // and hash offsets
            using (var preEncryptedfilelist = new FileStream(repackVariables.NewFilelistFile, FileMode.Append, FileAccess.Write))
            {
                filelistDataSize = (uint)preEncryptedfilelist.Length - 32;

                if (filelistDataSize % 8 != 0)
                {
                    // Get remainder from the division and
                    // reduce the remainder with 8. set that
                    // reduced value to a variable
                    var remainder = filelistDataSize % 8;
                    var increaseByteAmount = 8 - remainder;

                    // Increase the filelist size with the
                    // increase byte variable from the previous step and
                    // set this as a variable
                    // Then get the amount of null bytes to pad by subtracting 
                    // the new size with the filelist size
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

                    filelistToEncryptWriter.BaseStream.Position = 16;
                    filelistToEncryptWriter.WriteBytesUInt32(filelistDataSize, true);

                    filelistToEncryptWriter.BaseStream.Position = (uint)filelistToEncrypt.Length - 16;
                    filelistToEncryptWriter.WriteBytesUInt32(filelistDataSize, false);
                }
            }

            // Encrypt the filelist file
            CryptFilelist.ProcessFilelist(CryptActions.e, repackVariables.NewFilelistFile);
            IOhelpers.LogMessage("\nFinished encrypting new filelist", writerName);
        }
    }
}
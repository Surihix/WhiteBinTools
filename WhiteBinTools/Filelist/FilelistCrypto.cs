using System.IO;
using WhiteBinTools.Crypto;
using WhiteBinTools.Repack;
using WhiteBinTools.Support;
using static WhiteBinTools.Support.ProgramEnums;

namespace WhiteBinTools.Filelist
{
    internal class FilelistCrypto
    {
        public static void DecryptProcess(GameCodes gameCode, FilelistVariables filelistVariables, StreamWriter logWriter)
        {
            // Check for encryption header in the filelist file,
            // if the game code is set to ff13-1
            if (gameCode == GameCodes.ff131)
            {
                filelistVariables.IsEncrypted = CheckIfEncrypted(filelistVariables.MainFilelistFile);

                if (filelistVariables.IsEncrypted)
                {
                    logWriter.LogMessage("Error: Detected encrypted filelist file. set the game code to '-ff132' for handling this type of filelist");

                    logWriter.DisposeIfLogStreamOpen();
                    IOhelpers.ErrorExit("");
                }
            }

            // Check for encryption header in the filelist file,
            // if the game code is set to ff13-2
            if (gameCode == GameCodes.ff132)
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
                        logWriter.LogMessage("Error: Length of the body to decrypt/encrypt is not valid");

                        logWriter.DisposeIfLogStreamOpen();
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
                        IOhelpers.IfFileExistsDel(filelistVariables.TmpDcryptFilelistFile);
                        File.Copy(filelistVariables.MainFilelistFile, filelistVariables.TmpDcryptFilelistFile);

                        filelistVariables.MainFilelistFile = filelistVariables.TmpDcryptFilelistFile;
                        break;

                    case false:
                        IOhelpers.IfFileExistsDel(filelistVariables.TmpDcryptFilelistFile);
                        File.Copy(filelistVariables.MainFilelistFile, filelistVariables.TmpDcryptFilelistFile);

                        logWriter.LogMessage("\nDecrypting filelist file....");
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

                                logWriter.LogMessage(errorMsg);
                                logWriter.DisposeIfLogStreamOpen();

                                IOhelpers.ErrorExit(errorMsg);
                            }
                        }

                        logWriter.LogMessage("Finished decrypting filelist file\n");

                        filelistVariables.MainFilelistFile = filelistVariables.TmpDcryptFilelistFile;
                        break;
                }
            }
        }


        private static bool CheckIfEncrypted(string filelistFile)
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


        public static void EncryptProcess(RepackVariables repackVariables, StreamWriter logWriter)
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
                    preEncryptedfilelist.PadNull(padNulls);

                    filelistDataSize = newSize;
                }

                // Add 8 bytes for the size and hash
                // offsets and 8 null bytes
                preEncryptedfilelist.Seek((uint)preEncryptedfilelist.Length, SeekOrigin.Begin);
                preEncryptedfilelist.PadNull(16);
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
            logWriter.LogMessage("\nFinished encrypting new filelist");
        }
    }
}
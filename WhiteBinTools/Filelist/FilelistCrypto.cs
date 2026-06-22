using System.IO;
using WhiteBinTools.Crypto;
using WhiteBinTools.Support;
using WhiteBinTools.Support.Structures;
using static WhiteBinTools.Support.Enumerators;

namespace WhiteBinTools.Filelist
{
    internal class FilelistCrypto
    {
        public static bool DecryptProcess(GameCode gameCode, ref string filelistFile, FilelistCryptHeader filelistCryptHeader, StreamWriter logWriter)
        {
            bool hasDecrypted = false;

            // Check for encryption header in the filelist file,
            // if the game code is set to ff13-1/dirge
            if (gameCode == GameCode.ff131 || gameCode == GameCode.dirge)
            {
                filelistCryptHeader.HasCryptHeader = CheckEncryptionTag(filelistFile);

                if (filelistCryptHeader.HasCryptHeader)
                {
                    logWriter.LogMessage("Error: Detected encrypted filelist file. set the game code to '-ff132' for handling this type of filelist");

                    logWriter.DisposeIfLogStreamOpen();
                    SharedFunctions.ErrorExit("");
                }
            }

            // Check for encryption header in the filelist file,
            // if the game code is set to ff13-2
            if (gameCode == GameCode.ff132)
            {
                filelistCryptHeader.HasCryptHeader = CheckEncryptionTag(filelistFile);
            }

            // Check if the filelist is in decrypted
            // state and the length of the cryptBody
            // for divisibility.
            // If the file was decrypted then skip
            // decrypting it.
            // If the filelist is encrypted then
            // decrypt the filelist file by first
            // creating a temp copy of the filelist.            
            if (filelistCryptHeader.HasCryptHeader)
            {
                var isDecrypted = false;

                using (var encCheckReader = new BinaryReader(File.Open(filelistFile, FileMode.Open, FileAccess.Read)))
                {
                    filelistCryptHeader.MD5Hash = encCheckReader.ReadBytes(16);
                    filelistCryptHeader.FilelistDataSizeBE = encCheckReader.ReadBytesUInt32(true);
                    filelistCryptHeader.EncryptionTag = encCheckReader.ReadUInt32();

                    var cryptBodySize = filelistCryptHeader.FilelistDataSizeBE;
                    cryptBodySize += 8;

                    if (cryptBodySize % 8 != 0)
                    {
                        logWriter.LogMessage("Error: Length of the body to decrypt/encrypt is not valid");

                        logWriter.DisposeIfLogStreamOpen();
                        SharedFunctions.ErrorExit("");
                    }

                    encCheckReader.BaseStream.Position = 32 + cryptBodySize - 8;
                    cryptBodySize -= 8;

                    if (encCheckReader.ReadUInt32() == cryptBodySize)
                    {
                        isDecrypted = true;
                    }
                }

                var cryptFilelist = filelistFile + ".crypt";

                if (!isDecrypted)
                {
                    SharedFunctions.IfFileExistsDel(cryptFilelist);
                    File.Copy(filelistFile, cryptFilelist);

                    logWriter.LogMessage("\nDecrypting filelist file....");
                    CryptFilelist.ProcessFilelist(CryptAction.decrypt, cryptFilelist);

                    using (var decFilelistReader = new BinaryReader(File.Open(cryptFilelist, FileMode.Open, FileAccess.Read)))
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

                            SharedFunctions.ErrorExit(errorMsg);
                        }
                    }

                    logWriter.LogMessage("Finished decrypting filelist file\n");

                    hasDecrypted = true;
                    filelistFile = cryptFilelist;
                }
            }

            return hasDecrypted;
        }


        private static bool CheckEncryptionTag(string filelistFile)
        {
            var hasCryptHeader = false;

            using (var encStream = new FileStream(filelistFile, FileMode.Open, FileAccess.Read))
            {
                using (var encStreamReader = new BinaryReader(encStream))
                {
                    encStreamReader.BaseStream.Position = 20;
                    var encHeaderNumber = encStreamReader.ReadUInt32();

                    if (encHeaderNumber == FilelistCryptHeader.EncryptionTagConstant)
                    {
                        hasCryptHeader = true;
                    }
                }
            }

            return hasCryptHeader;
        }


        public static void EncryptProcess(string newFilelistFile, StreamWriter logWriter)
        {
            uint filelistDataSize = 0;

            // Check filelist size if divisibile by 8
            // and pad in null bytes if not divisible.
            // Then write some null bytes for the size 
            // and hash offsets
            using (var preEncryptedfilelist = new FileStream(newFilelistFile, FileMode.Append, FileAccess.Write))
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

            using (var filelistToEncrypt = new FileStream(newFilelistFile, FileMode.Open, FileAccess.Write))
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
            CryptFilelist.ProcessFilelist(CryptAction.encrypt, newFilelistFile);
            logWriter.LogMessage("\nFinished encrypting new filelist");
        }
    }
}
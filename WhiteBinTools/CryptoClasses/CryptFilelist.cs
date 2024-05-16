using System;
using System.IO;
using WhiteBinTools.SupportClasses;
using static WhiteBinTools.SupportClasses.ProgramEnums;

namespace WhiteBinTools.CryptoClasses
{
    internal class CryptFilelist
    {
        public static void ProcessFilelist(CryptActions cryptAction, string inFile)
        {
            using (var inFileReader = new BinaryReader(File.Open(inFile, FileMode.Open, FileAccess.Read)))
            {
                inFileReader.BaseStream.Position = 16;
                var cryptBodySizeVal = inFileReader.ReadBytes(4);
                Array.Reverse(cryptBodySizeVal);

                var cryptBodySize = BitConverter.ToUInt32(cryptBodySizeVal, 0);
                cryptBodySize += 8;

                var fileLength = (uint)inFileReader.BaseStream.Length;
                fileLength -= cryptBodySize;
                fileLength -= 32;

                uint remainderBytes = 0;

                if (fileLength > 0)
                {
                    remainderBytes = fileLength;
                }

                uint readPos = 32;
                uint writePos = 32;

                inFileReader.BaseStream.Position = 0;
                var baseSeedArray = inFileReader.ReadBytes(16);
                var seedArray8Bytes = (ulong)((baseSeedArray[9] << 24) | (baseSeedArray[12] << 16) | (baseSeedArray[2] << 8) | (baseSeedArray[0]));
                var seedArray = BitConverter.GetBytes(seedArray8Bytes);

                var xorTable = Generator.GenerateXORtable(seedArray, false);

                switch (cryptAction)
                {
                    case CryptActions.d:
                        (inFile + ".dec").IfFileExistsDel();

                        using (var decryptedStreamBinWriter = new BinaryWriter(File.Open(inFile + ".dec", FileMode.Append, FileAccess.Write)))
                        {
                            inFileReader.BaseStream.Position = 0;
                            inFileReader.BaseStream.CopyStreamTo(decryptedStreamBinWriter.BaseStream, writePos, false);

                            var blockCount = cryptBodySize / 8;
                            Decryption.DecryptBlocks(xorTable, blockCount, readPos, writePos, inFileReader, decryptedStreamBinWriter, false);

                            inFileReader.BaseStream.Position = decryptedStreamBinWriter.BaseStream.Length;
                            inFileReader.BaseStream.CopyStreamTo(decryptedStreamBinWriter.BaseStream, remainderBytes, false);
                        }

                        inFileReader.Dispose();

                        CreateFinalFile(inFile, inFile + ".dec");
                        break;

                    case CryptActions.e:
                        (inFile + ".tmp2").IfFileExistsDel();

                        using (var chkSumStreamBinWriter = new BinaryWriter(File.Open(inFile + ".tmp2", FileMode.Append, FileAccess.Write)))
                        {
                            inFileReader.BaseStream.Position = 0;
                            inFileReader.BaseStream.CopyStreamTo(chkSumStreamBinWriter.BaseStream, readPos, false);

                            inFileReader.BaseStream.Position = readPos;
                            inFileReader.BaseStream.CopyStreamTo(chkSumStreamBinWriter.BaseStream, cryptBodySize - 8, false);

                            inFileReader.BaseStream.Position = readPos + cryptBodySize - 8;
                            inFileReader.BaseStream.CopyStreamTo(chkSumStreamBinWriter.BaseStream, 4, false);

                            var checkSum = inFileReader.ComputeCheckSum((cryptBodySize - 8) / 4, readPos);

                            chkSumStreamBinWriter.Write(checkSum);

                            inFileReader.BaseStream.Position = chkSumStreamBinWriter.BaseStream.Length;
                            inFileReader.BaseStream.CopyStreamTo(chkSumStreamBinWriter.BaseStream, remainderBytes, false);
                        }

                        inFileReader.Dispose();

                        (inFile + ".enc").IfFileExistsDel();

                        using (var inFileReaderTmp = new BinaryReader(File.Open(inFile + ".tmp2", FileMode.Open, FileAccess.Read)))
                        {
                            using (var encryptedStreamBinWriter = new BinaryWriter(File.Open(inFile + ".enc", FileMode.Append, FileAccess.Write)))
                            {
                                inFileReaderTmp.BaseStream.Position = 0;
                                inFileReaderTmp.BaseStream.CopyStreamTo(encryptedStreamBinWriter.BaseStream, writePos, false);

                                var blockCount = cryptBodySize / 8;
                                Encryption.EncryptBlocks(xorTable, blockCount, readPos, writePos, inFileReaderTmp, encryptedStreamBinWriter, false);

                                inFileReaderTmp.BaseStream.Position = encryptedStreamBinWriter.BaseStream.Length;
                                inFileReaderTmp.BaseStream.CopyTo(encryptedStreamBinWriter.BaseStream);
                            }
                        }

                        (inFile + ".tmp2").IfFileExistsDel();

                        CreateFinalFile(inFile, inFile + ".enc");
                        break;
                }
            }
        }

        static void CreateFinalFile(string ogFile, string processedFile)
        {
            var ogFileName = Path.GetFileName(ogFile);
            var ogFileDir = Path.GetDirectoryName(ogFile);
            var newFile = Path.Combine(ogFileDir, ogFileName);

            File.Delete(ogFile);
            File.Move(processedFile, newFile);
        }
    }
}
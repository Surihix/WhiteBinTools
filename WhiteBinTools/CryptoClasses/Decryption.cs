using System;
using System.IO;

namespace WhiteBinTools.CryptoClasses
{
    internal class Decryption
    {
        public static void DecryptBlocks(byte[] xorTable, uint blockCount, uint readPos, uint writePos, BinaryReader inFileReader, BinaryWriter decryptedStreamBinWriter, bool logDisplay)
        {
            uint blockCounter = 0;
            uint currentBlockId, tableOffset, decryptedBytesHigherVal, decryptedBytesLowerVal;
            byte[] currentBytes;
            uint decryptedByte1, decryptedByte2, decryptedByte3, decryptedByte4;
            uint decryptedByte5, decryptedByte6, decryptedByte7, decryptedByte8;
            byte[] decryptedBytesArray;
            uint xorBlockLowerVal, xorBlockHigherVal, carryFlag;
            long specialKey1, specialKey2, decryptedBytesLongLowerVal, decryptedBytesLongHigherVal;
            byte[] decryptedByteLowerArray, decryptedByteHigherArray;

            for (int i = 0; i < blockCount; i++)
            {
                // Setup BlockCounter according
                // to the currentBlockId and read
                // 8 bytes (a block) to decrypt
                currentBlockId = blockCounter >> 3;

                inFileReader.BaseStream.Position = readPos;
                currentBytes = inFileReader.ReadBytes(8);


                // Setup BlockCounter variables
                tableOffset = 0;
                CryptoBase.BlockCounterSetup(blockCounter, ref tableOffset);


                // Shift all of the byte
                // value 8 times and 
                // perform a XOR operation
                decryptedByte1 = ((currentBlockId ^ 69) & 255) ^ currentBytes[0];
                decryptedByte1 = decryptedByte1.LoopAByte(xorTable, tableOffset);

                decryptedByte2 = (uint)currentBytes[0] ^ currentBytes[1];
                decryptedByte2 = decryptedByte2.LoopAByte(xorTable, tableOffset);

                decryptedByte3 = (uint)currentBytes[1] ^ currentBytes[2];
                decryptedByte3 = decryptedByte3.LoopAByte(xorTable, tableOffset);

                decryptedByte4 = (uint)currentBytes[2] ^ currentBytes[3];
                decryptedByte4 = decryptedByte4.LoopAByte(xorTable, tableOffset);

                decryptedByte5 = (uint)currentBytes[3] ^ currentBytes[4];
                decryptedByte5 = decryptedByte5.LoopAByte(xorTable, tableOffset);

                decryptedByte6 = (uint)currentBytes[4] ^ currentBytes[5];
                decryptedByte6 = decryptedByte6.LoopAByte(xorTable, tableOffset);

                decryptedByte7 = (uint)currentBytes[5] ^ currentBytes[6];
                decryptedByte7 = decryptedByte7.LoopAByte(xorTable, tableOffset);

                decryptedByte8 = (uint)currentBytes[6] ^ currentBytes[7];
                decryptedByte8 = decryptedByte8.LoopAByte(xorTable, tableOffset);


                // Setup decrypted byte variables
                decryptedBytesArray = new byte[] { (byte)decryptedByte5, (byte)decryptedByte6, (byte)decryptedByte7,
                    (byte)decryptedByte8, (byte)decryptedByte1, (byte)decryptedByte2, (byte)decryptedByte3, (byte)decryptedByte4 };

                decryptedBytesHigherVal = BitConverter.ToUInt32(decryptedBytesArray, 0);
                decryptedBytesLowerVal = BitConverter.ToUInt32(decryptedBytesArray, 4);


                // Setup xorBlock variables
                xorBlockLowerVal = 0;
                xorBlockHigherVal = 0;
                CryptoBase.XORblockSetup(xorTable, tableOffset, ref xorBlockLowerVal, ref xorBlockHigherVal);


                // Setup SpecialKey variables
                carryFlag = 0;
                specialKey1 = 0;
                specialKey2 = 0;
                CryptoBase.SpecialKeySetup(ref carryFlag, ref specialKey1, ref specialKey2);


                // Process bytes with the SpecialKey
                // and xorBlock variables
                decryptedBytesLongLowerVal = decryptedBytesLowerVal;
                decryptedBytesLongHigherVal = decryptedBytesHigherVal;

                if (decryptedBytesLongLowerVal < xorBlockLowerVal)
                {
                    carryFlag = 1;
                }
                else
                {
                    carryFlag = 0;
                }

                decryptedBytesLongLowerVal -= xorBlockLowerVal;
                decryptedBytesLongHigherVal -= xorBlockHigherVal;
                decryptedBytesLongHigherVal -= carryFlag;

                decryptedBytesLongLowerVal ^= specialKey1;
                decryptedBytesLongHigherVal ^= specialKey2;

                decryptedBytesLongLowerVal ^= xorBlockLowerVal;
                decryptedBytesLongHigherVal ^= xorBlockHigherVal;


                // Store the bytes in a array
                // and write it to the stream
                decryptedByteLowerArray = BitConverter.GetBytes((uint)decryptedBytesLongLowerVal);
                decryptedByteHigherArray = BitConverter.GetBytes((uint)decryptedBytesLongHigherVal);

                decryptedStreamBinWriter.BaseStream.Position = writePos;
                decryptedStreamBinWriter.Write(decryptedByteHigherArray);

                decryptedStreamBinWriter.BaseStream.Position = writePos + 4;
                decryptedStreamBinWriter.Write(decryptedByteLowerArray);


                if (logDisplay)
                {
                    Console.Write($"Block: {i}  ");

                    Console.Write(decryptedByteHigherArray[0].ToString("X2") + " " +
                        decryptedByteHigherArray[1].ToString("X2") + " " + decryptedByteHigherArray[2].ToString("X2") + " " +
                        decryptedByteHigherArray[3].ToString("X2") + " ");

                    Console.WriteLine(decryptedByteLowerArray[0].ToString("X2") + " " +
                        decryptedByteLowerArray[1].ToString("X2") + " " + decryptedByteLowerArray[2].ToString("X2") + " " +
                        decryptedByteLowerArray[3].ToString("X2"));
                }


                // Move to next block
                blockCounter += 8;
                readPos += 8;
                writePos += 8;
            }
        }
    }
}
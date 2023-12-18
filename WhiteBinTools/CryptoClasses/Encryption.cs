using System;
using System.IO;

namespace WhiteBinTools.CryptoClasses
{
    internal class Encryption
    {
        public static void EncryptBlocks(byte[] xorTable, uint blockCount, uint readPos, uint writePos, BinaryReader inFileReader, BinaryWriter encryptedStreamBinWriter, bool logDisplay)
        {
            uint blockCounter = 0;
            uint currentBlockId, tableOffset, xorBlockLowerVal, xorBlockHigherVal, carryFlag;
            byte[] bytesToEncrypt, bytesToEncryptLowerArray, bytesToEncryptHigherArray;
            byte[] computedBytesArray, encryptedByteArray;
            long bytesToEncryptLowerVal, bytesToEncryptHigherVal, specialKey1, specialKey2;
            long bytesToEncryptLowerValFixed;
            uint bytesToEncryptHigherValUInt, bytesToEncryptLowerValUInt;
            uint encryptedByte1, encryptedByte2, encryptedByte3, encryptedByte4;
            uint encryptedByte5, encryptedByte6, encryptedByte7, encryptedByte8;

            for (int i = 0; i < blockCount; i++)
            {
                // Setup BlockCounter according
                // to the currentBlockId and read
                // 8 bytes (a block) to encrypt
                currentBlockId = blockCounter >> 3;

                inFileReader.BaseStream.Position = readPos;
                bytesToEncrypt = inFileReader.ReadBytes(8);
                bytesToEncryptLowerArray = new byte[] { bytesToEncrypt[7], bytesToEncrypt[6], bytesToEncrypt[5], bytesToEncrypt[4] };
                bytesToEncryptHigherArray = new byte[] { bytesToEncrypt[3], bytesToEncrypt[2], bytesToEncrypt[1], bytesToEncrypt[0] };

                bytesToEncryptLowerVal = bytesToEncryptLowerArray.ArrayToFFNum();
                bytesToEncryptHigherVal = bytesToEncryptHigherArray.ArrayToFFNum();


                // Setup BlockCounter variables
                tableOffset = 0;
                CryptoBase.BlockCounterSetup(blockCounter, ref tableOffset);


                // Setup xorBlock variables
                xorBlockLowerVal = 0;
                xorBlockHigherVal = 0;
                CryptoBase.XORblockSetup(xorTable, tableOffset, ref xorBlockLowerVal, ref xorBlockHigherVal);


                // Setup SpecialKey variables
                carryFlag = 0;
                specialKey1 = 0;
                specialKey2 = 0;
                CryptoBase.SpecialKeySetup(ref carryFlag, ref specialKey1, ref specialKey2);


                // XOR the bytes to encrypt 
                // with the xorBlock variables
                bytesToEncryptLowerVal ^= xorBlockLowerVal;
                bytesToEncryptHigherVal ^= xorBlockHigherVal;


                // XOR the bytes to encrypt 
                // with the SpecialKey
                // variables and increase the
                // bytes with the xorBlock variables
                bytesToEncryptLowerVal ^= specialKey1;
                bytesToEncryptHigherVal ^= specialKey2;

                bytesToEncryptLowerVal += xorBlockLowerVal;
                bytesToEncryptHigherVal += xorBlockHigherVal;


                // Get the lowermostbits value of the bytes and
                // compare that adjusted value to determine
                // the carryFlag value.
                // After that increase the higher 
                // value bytes with the carryFlag value
                bytesToEncryptLowerValFixed = bytesToEncryptLowerVal & 0xFFFFFFFF;

                if (bytesToEncryptLowerValFixed < xorBlockLowerVal)
                {
                    carryFlag = 1;
                }
                else
                {
                    carryFlag = 0;
                }

                bytesToEncryptHigherVal += carryFlag;


                // Get the rightmost uint value from the lower
                // and higher bytesToEncrypt variables and
                // store them into a common array
                bytesToEncryptHigherValUInt = (uint)bytesToEncryptHigherVal & 0xFFFFFFFF;
                bytesToEncryptLowerValUInt = (uint)bytesToEncryptLowerVal & 0xFFFFFFFF;

                computedBytesArray = new byte[8];
                Array.ConstrainedCopy(BitConverter.GetBytes(bytesToEncryptLowerValUInt), 0, computedBytesArray, 0, 4);
                Array.ConstrainedCopy(BitConverter.GetBytes(bytesToEncryptHigherValUInt), 0, computedBytesArray, 4, 4);


                // Reverse shift all of
                // the byte value 8 times
                // and perform a XOR
                // operation
                encryptedByte1 = computedBytesArray[0].LoopAByteReverse(xorTable, tableOffset);
                encryptedByte1 = ((currentBlockId ^ 69) & 255) ^ encryptedByte1;

                encryptedByte2 = computedBytesArray[1].LoopAByteReverse(xorTable, tableOffset);
                encryptedByte2 = encryptedByte1 ^ encryptedByte2;

                encryptedByte3 = computedBytesArray[2].LoopAByteReverse(xorTable, tableOffset);
                encryptedByte3 = encryptedByte2 ^ encryptedByte3;

                encryptedByte4 = computedBytesArray[3].LoopAByteReverse(xorTable, tableOffset);
                encryptedByte4 = encryptedByte3 ^ encryptedByte4;

                encryptedByte5 = computedBytesArray[4].LoopAByteReverse(xorTable, tableOffset);
                encryptedByte5 = encryptedByte4 ^ encryptedByte5;

                encryptedByte6 = computedBytesArray[5].LoopAByteReverse(xorTable, tableOffset);
                encryptedByte6 = encryptedByte5 ^ encryptedByte6;

                encryptedByte7 = computedBytesArray[6].LoopAByteReverse(xorTable, tableOffset);
                encryptedByte7 = encryptedByte6 ^ encryptedByte7;

                encryptedByte8 = computedBytesArray[7].LoopAByteReverse(xorTable, tableOffset);
                encryptedByte8 = encryptedByte7 ^ encryptedByte8;


                // Store the bytes in a array
                // and write it to the stream
                encryptedByteArray = new byte[] { (byte)encryptedByte1, (byte)encryptedByte2, (byte)encryptedByte3,
                    (byte)encryptedByte4, (byte)encryptedByte5, (byte)encryptedByte6, (byte)encryptedByte7, (byte)encryptedByte8 };

                encryptedStreamBinWriter.BaseStream.Position = writePos;
                encryptedStreamBinWriter.Write(encryptedByteArray);


                if (logDisplay)
                {
                    Console.Write($"Block: {i}  ");

                    Console.WriteLine(encryptedByteArray[0].ToString("X2") + " " + encryptedByteArray[1].ToString("X2") + " " +
                        encryptedByteArray[2].ToString("X2") + " " + encryptedByteArray[3].ToString("X2") + " " +
                        encryptedByteArray[4].ToString("X2") + " " + encryptedByteArray[5].ToString("X2") + " " +
                        encryptedByteArray[6].ToString("X2") + " " + encryptedByteArray[7].ToString("X2"));
                }


                // Move to next block
                readPos += 8;
                blockCounter += 8;
                writePos += 8;
            }
        }
    }
}
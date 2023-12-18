using System;
using System.Linq;

namespace WhiteBinTools.CryptoClasses
{
    internal class Generator
    {
        public static byte[] GenerateXORtable(byte[] seedArray, bool logDisplay)
        {
            var xorTable = new byte[264];

            Array.Reverse(seedArray);

            var seedHalfA = BitConverter.ToUInt32(seedArray, 0);
            var seedHalfB = BitConverter.ToUInt32(seedArray, 4);

            seedHalfA = (seedHalfA << 0x08) | (seedHalfA >> 0x18);
            seedHalfB = (seedHalfB >> 0x10) | (seedHalfB << 0x10);

            var xorBlock = BitConverter.GetBytes(seedHalfB).Concat(BitConverter.GetBytes(seedHalfA)).ToArray();
            xorBlock[0] += 0x45;

            // Loop 1
            int i = 1;
            int tmp;
            while (i < 8)
            {
                tmp = xorBlock[i] + 0xD4 + xorBlock[i - 1];
                tmp ^= (xorBlock[i - 1]) << 2;
                tmp ^= 0x45;

                xorBlock[i] = (byte)tmp;
                i++;
            }

            Array.ConstrainedCopy(xorBlock, 0, xorTable, 0, xorBlock.Length);

            if (logDisplay)
            {
                Console.WriteLine($"Block 0: {xorBlock[0]:X2} {xorBlock[1]:X2} {xorBlock[2]:X2} {xorBlock[3]:X2} " +
                    $"{xorBlock[4]:X2} {xorBlock[5]:X2} {xorBlock[6]:X2} {xorBlock[7]:X2}");
            }


            // Loop 2
            i = 1;
            ulong previousXORBlock = BitConverter.ToUInt64(xorBlock, 0);
            uint blockHalfA;
            uint blockHalfB;
            ulong a;
            ulong tmpBlockHalfA;
            ulong tmpBlockHalfB;
            ulong b;
            uint xorBlockHalfA;
            uint xorBlockHalfB;
            var copyIndex = 8;

            while (i < 0x21)
            {
                blockHalfA = (uint)(previousXORBlock & 0xFFFFFFFF);
                blockHalfB = (uint)(previousXORBlock >> 32);

                a = 5 * previousXORBlock;
                a ^= (ulong)blockHalfB << 32;

                tmpBlockHalfA = (uint)(blockHalfA ^ a);
                tmpBlockHalfB = (uint)(a >> 32);

                a = blockHalfA | (a & 0xFFFFFFFF00000000);

                b = tmpBlockHalfA;

                xorBlockHalfA = (uint)(a ^ b);
                tmpBlockHalfB ^= blockHalfB;
                xorBlockHalfB = (uint)tmpBlockHalfB;

                xorBlock = BitConverter.GetBytes(xorBlockHalfA).Concat(BitConverter.GetBytes(xorBlockHalfB)).ToArray();

                Array.ConstrainedCopy(xorBlock, 0, xorTable, copyIndex, xorBlock.Length);

                if (logDisplay)
                {
                    Console.WriteLine($"Block {i}: {xorBlock[0]:X2} {xorBlock[1]:X2} {xorBlock[2]:X2} {xorBlock[3]:X2} " +
                        $"{xorBlock[4]:X2} {xorBlock[5]:X2} {xorBlock[6]:X2} {xorBlock[7]:X2}");
                }

                previousXORBlock = BitConverter.ToUInt64(xorBlock, 0);

                i++;
                copyIndex += 8;
            }

            //File.WriteAllBytes("KeysDump", xorTable);

            return xorTable;
        }
    }
}
using System;
using System.IO;

internal static class BinaryWriterHelpers
{
    public static void WriteBytesUInt16(this BinaryWriter writerName, ushort valueToWrite, bool isBigEndian)
    {
        var writeValueBuffer = BitConverter.GetBytes(valueToWrite);
        ReverseIfBigEndian(isBigEndian, writeValueBuffer);

        writerName.Write(writeValueBuffer);
    }


    public static void WriteBytesUInt32(this BinaryWriter writerName, uint valueToWrite, bool isBigEndian)
    {
        var writeValueBuffer = BitConverter.GetBytes(valueToWrite);
        ReverseIfBigEndian(isBigEndian, writeValueBuffer);

        writerName.Write(writeValueBuffer);
    }


    static void ReverseIfBigEndian(bool isBigEndian, byte[] writeValueBuffer)
    {
        if (isBigEndian)
        {
            Array.Reverse(writeValueBuffer);
        }
    }
}
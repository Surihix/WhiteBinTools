using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace WhiteBinTools.SupportClasses
{
    internal static class BinaryHelpers
    {
        public static string BinaryToString(this BinaryReader readerName, uint readerPos)
        {
            readerName.BaseStream.Position = readerPos;
            var parsedString = new StringBuilder();
            char getParsedString;
            while ((getParsedString = readerName.ReadChar()) != default)
            {
                parsedString.Append(getParsedString);
            }

            return parsedString.ToString();
        }


        public static string DecimalToAscii(this uint decValue)
        {
            return decValue.ToString("x");
        }


        public static void AdjustBytesUInt16(this BinaryWriter writerName, uint writerPos, ushort adjustVal, CmnEnums.Endianness endiannessVar)
        {
            writerName.BaseStream.Position = writerPos;
            var adjustValBytes = new byte[2];

            switch (endiannessVar)
            {
                case CmnEnums.Endianness.LittleEndian:
                    BinaryPrimitives.WriteUInt16LittleEndian(adjustValBytes, adjustVal);
                    break;

                case CmnEnums.Endianness.BigEndian:
                    BinaryPrimitives.WriteUInt16BigEndian(adjustValBytes, adjustVal);
                    break;
            }

            writerName.Write(adjustValBytes);
        }


        public static void AdjustBytesUInt32(this BinaryWriter writerName, uint writerPos, uint adjustVal, CmnEnums.Endianness endiannessVar)
        {
            writerName.BaseStream.Position = writerPos;
            var adjustedValBytes = new byte[4];

            switch (endiannessVar)
            {
                case CmnEnums.Endianness.LittleEndian:
                    BinaryPrimitives.WriteUInt32LittleEndian(adjustedValBytes, adjustVal);
                    break;

                case CmnEnums.Endianness.BigEndian:
                    BinaryPrimitives.WriteUInt32BigEndian(adjustedValBytes, adjustVal);
                    break;
            }

            writerName.Write(adjustedValBytes);
        }
    }
}
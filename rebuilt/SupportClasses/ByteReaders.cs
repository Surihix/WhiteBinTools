using System.IO;
using System.Text;

namespace WhiteBinTools.SupportClasses
{
    internal static class BinaryReaders
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
    }
}
namespace WhiteBinTools.Support.Structures
{
    internal class WhiteFileInfoData
    {
        public long FilePosition;
        public uint UncmpSize;
        public uint CmpSize;
        public bool IsPathGenerated;
        public string FilePath;
        public bool IsCompressed;
    }
}
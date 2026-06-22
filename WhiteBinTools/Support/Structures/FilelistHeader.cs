namespace WhiteBinTools.Support.Structures
{
    internal class FilelistHeader
    {
        public uint ChunkInfoTableOffset;
        public uint ChunkDataStartOffset;
        public uint FileCount;
        public int ChunkCount;
    }
}
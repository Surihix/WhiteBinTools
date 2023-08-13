namespace WhiteBinTools.FilelistClasses
{
    internal partial class FilelistProcesses
    {
        public string MainFilelistFile;
        public string MainFilelistDirectory;
        public string TmpDcryptFilelistFile;
        public string DefaultChunksExtDir;
        public string ChunkFile;
        public bool IsEncrypted;

        public uint ChunkInfoSectionOffset;
        public uint ChunkDataSectionOffset;
        public uint TotalFiles;

        public uint ChunkCmpSize;
        public uint ChunkStartOffset;

        public uint ChunkInfoSize;
        public uint TotalChunks;
        public uint ChunkFNameCount;
    }
}
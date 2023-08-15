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

        public bool CryptToolPresentBefore;

        public uint ChunkInfoSectionOffset;
        public uint ChunkDataSectionOffset;
        public uint TotalFiles;

        public uint ChunkCmpSize;
        public uint ChunkStartOffset;

        public uint ChunkInfoSize;
        public uint TotalChunks;
        public uint ChunkFNameCount;

        public string[] ConvertedStringData;
        public uint Position;
        public uint UnCmpSize;
        public uint CmpSize;
        public string MainPath;
        
        public string DirectoryPath;
        public string FileName;
        public uint NoPathFileCount;
        public string FullFilePath;
        public bool IsCompressed;
    }
}
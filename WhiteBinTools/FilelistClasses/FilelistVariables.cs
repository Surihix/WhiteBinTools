using System.Collections.Generic;

namespace WhiteBinTools.FilelistClasses
{
    internal class FilelistVariables
    {
        // Program variables
        public string MainFilelistFile { get; set; }
        public string MainFilelistDirectory { get; set; }
        public string TmpDcryptFilelistFile { get; set; }
        public string DefaultChunksExtDir { get; set; }
        public string ChunkFile { get; set; }
        public byte[] EntriesData { get; set; }
        public bool IsEncrypted { get; set; }
        public byte[] EncryptedHeaderData { get; set; }

        // Program variables
        // (For Chunks)
        public uint ChunkInfoSize { get; set; }
        public uint TotalChunks { get; set; }
        public uint ChunkFNameCount { get; set; }
        public int LastChunkNumber { get; set; }
        public int CurrentChunkNumber { get; set; }
        public Dictionary<int, byte[]> ChunkDataDict = new Dictionary<int, byte[]>();

        // Header section variables
        public uint ChunkInfoSectionOffset { get; set; }
        public uint ChunkDataSectionOffset { get; set; }
        public uint TotalFiles { get; set; }

        // ChunkInfo section variables
        public uint ChunkCmpSize { get; set; }
        public uint ChunkStartOffset { get; set; }

        // Entry section variables
        public uint FileCode { get; set; }
        public int ChunkNumber { get; set; }
        public ushort PathStringPos { get; set; }
        public byte UnkEntryVal { get; set; }

        // Program variables
        // (For a string in chunk)
        public string PathString { get; set; }
        public string[] ConvertedStringData { get; set; }
        public uint Position { get; set; }
        public uint UnCmpSize { get; set; }
        public uint CmpSize { get; set; }
        public string MainPath { get; set; }
        public string DirectoryPath { get; set; }
        public string FileName { get; set; }
        public uint NoPathFileCount { get; set; }
        public string FullFilePath { get; set; }
        public bool IsCompressed { get; set; }
    }
}
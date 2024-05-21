namespace WhiteBinTools.RepackClasses
{
    internal class RepackVariables
    {
        public string FilelistFileName { get; set; }
        public string NewFilelistFile { get; set; }
        public string OldFilelistFileBckup { get; set; }

        public string NewWhiteBinFileName { get; set; }
        public string NewWhiteBinFile { get; set; }
        public string OldWhiteBinFileBackup { get; set; }

        public string[] ConvertedOgStringData { get; set; }
        public uint OgFilePos { get; set; }
        public uint OgCmpSize { get; set; }
        public uint OgUnCmpSize { get; set; }
        public string OgMainPath { get; set; }
        public string OgDirectoryPath { get; set; }
        public string OgFileName { get; set; }
        public uint OgNoPathFileCount { get; set; }
        public string OgFullFilePath { get; set; }
        public bool WasCompressed { get; set; }

        public string AsciiFilePos { get; set; }
        public string AsciiUnCmpSize { get; set; }
        public string AsciiCmpSize { get; set; }
        public string RepackPathInChunk { get; set; }
        public string RepackState { get; set; }
        public string RepackLogMsg { get; set; }
    }
}
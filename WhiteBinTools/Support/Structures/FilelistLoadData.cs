namespace WhiteBinTools.Support.Structures
{
    internal class FilelistLoadData
    {
        public FilelistCryptHeader FilelistCryptHeader;
        public FilelistHeader FilelistHeader;
        public FilelistEntryV1[] FilelistEntryV1Table;
        public FilelistEntryV2[] FilelistEntryV2Table;
        public FilelistChunk[] FilelistChunks;
    }
}
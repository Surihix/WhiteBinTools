namespace WhiteBinTools.Support.Structures
{
    internal class FilelistCryptHeader
    {
        public static readonly uint EncryptionTagConstant = 0x1DE03478;
        public bool HasCryptHeader;
        public byte[] MD5Hash;
        public uint FilelistDataSizeBE;
        public uint EncryptionTag;
    }
}
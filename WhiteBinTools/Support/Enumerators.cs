namespace WhiteBinTools.Support
{
    internal class Enumerators
    {
        public enum GameCode
        {
            dirge,
            ff131,
            ff132
        }

        public enum CryptAction
        {
            decrypt,
            encrypt
        }

        public enum UnpackedState
        {
            Decompressed,
            Copied
        }

        public enum RepackedState
        {
            Compressed,
            Copied
        }

        public enum ValueTypes
        {
            Boolean,
            Byte,
            Int,
            Uint,
            Ulong
        }
    }
}
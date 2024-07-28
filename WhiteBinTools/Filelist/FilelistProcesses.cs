using System.IO;
using System.Text;
using static WhiteBinTools.Support.ProgramEnums;

namespace WhiteBinTools.Filelist
{
    internal class FilelistProcesses
    {
        public static void PrepareFilelistVars(FilelistVariables filelistVariables, string filelistFile)
        {
            filelistVariables.MainFilelistFile = filelistFile;

            var inFilelistFilePath = Path.GetFullPath(filelistVariables.MainFilelistFile);
            filelistVariables.MainFilelistDirectory = Path.GetDirectoryName(inFilelistFilePath);
            filelistVariables.TmpDcryptFilelistFile = Path.Combine(filelistVariables.MainFilelistDirectory, "filelist_tmp.bin");
        }


        public static void GetCurrentFileEntry(GameCodes gameCode, BinaryReader entriesReader, long entriesReadPos, FilelistVariables filelistVariables)
        {
            entriesReader.BaseStream.Position = entriesReadPos;
            filelistVariables.FileCode = entriesReader.ReadUInt32();

            if (gameCode.Equals(GameCodes.ff131))
            {
                filelistVariables.ChunkNumber = entriesReader.ReadUInt16();
                filelistVariables.PathStringPos = entriesReader.ReadUInt16();

                GeneratePathString(filelistVariables.PathStringPos, filelistVariables.ChunkDataDict[filelistVariables.ChunkNumber], filelistVariables);
            }
            else if (gameCode.Equals(GameCodes.ff132))
            {
                filelistVariables.PathStringPos = entriesReader.ReadUInt16();
                filelistVariables.ChunkNumber = entriesReader.ReadByte();
                filelistVariables.UnkEntryVal = entriesReader.ReadByte();

                if (filelistVariables.PathStringPos == 0)
                {
                    filelistVariables.CurrentChunkNumber++;
                }

                if (filelistVariables.PathStringPos == 32768)
                {
                    filelistVariables.CurrentChunkNumber++;
                    filelistVariables.PathStringPos -= 32768;
                }

                if (filelistVariables.PathStringPos > 32768)
                {
                    filelistVariables.PathStringPos -= 32768;
                }

                GeneratePathString(filelistVariables.PathStringPos, filelistVariables.ChunkDataDict[filelistVariables.CurrentChunkNumber], filelistVariables);
            }
        }

        private static void GeneratePathString(ushort pathPos, byte[] currentChunkData, FilelistVariables filelistVariables)
        {
            var length = 0;

            for (int i = pathPos; i < currentChunkData.Length && currentChunkData[i] != 0; i++)
            {
                length++;
            }

            filelistVariables.PathString = Encoding.UTF8.GetString(currentChunkData, pathPos, length);
        }
    }
}
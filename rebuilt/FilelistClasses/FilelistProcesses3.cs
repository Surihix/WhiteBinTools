using System.IO;
using WhiteBinTools.SupportClasses;

namespace WhiteBinTools.FilelistClasses
{
    internal partial class FilelistProcesses
    {
        public static void GetFilelistOffsets(BinaryReader filelistReader, StreamWriter logWriter, FilelistProcesses filelistVariables, CmnEnums.GameCodes gameCodeVar)
        {
            var readStartPositionVar = new uint();
            var adjustOffset = new uint();

            switch (filelistVariables.IsEncrypted)
            {
                case true:
                    readStartPositionVar = 32;
                    adjustOffset = 32;
                    break;

                case false:
                    readStartPositionVar = 0;
                    adjustOffset = 0;
                    break;
            }

            filelistReader.BaseStream.Position = readStartPositionVar;
            filelistVariables.ChunkInfoSectionOffset = filelistReader.ReadUInt32() + adjustOffset;
            filelistVariables.ChunkDataSectionOffset = filelistReader.ReadUInt32() + adjustOffset;
            filelistVariables.TotalFiles = filelistReader.ReadUInt32();

            filelistVariables.ChunkInfoSize = filelistVariables.ChunkDataSectionOffset - filelistVariables.ChunkInfoSectionOffset;
            filelistVariables.TotalChunks = filelistVariables.ChunkInfoSize / 12;

            IOhelpers.LogMessage("TotalChunks: " + filelistVariables.TotalChunks, logWriter);
            IOhelpers.LogMessage("No of files: " + filelistVariables.TotalFiles + "\n", logWriter);
        }
    }
}
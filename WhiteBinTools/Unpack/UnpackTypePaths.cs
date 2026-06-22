using System.IO;
using WhiteBinTools.Filelist;
using WhiteBinTools.Support;
using static WhiteBinTools.Support.Enumerators;

namespace WhiteBinTools.Unpack
{
    internal class UnpackTypePaths
    {
        public static void UnpackFilelistPaths(GameCode gameCode, string filelistFile, StreamWriter logWriter)
        {
            SharedFunctions.CheckFileExists(filelistFile, logWriter, "Error: Filelist file specified in the argument is missing");

            var filelistLoaderData = FilelistLoader.LoadFilelist(gameCode, filelistFile, logWriter);

            var filelistHeader = filelistLoaderData.FilelistHeader;
            var filelistEntryV1Table = filelistLoaderData.FilelistEntryV1Table;
            var filelistEntryV2Table = filelistLoaderData.FilelistEntryV2Table;
            var filelistChunks = filelistLoaderData.FilelistChunks;

            var outTxtFile = Path.Combine(Path.GetDirectoryName(filelistFile), Path.GetFileName(filelistFile) + ".txt");
            SharedFunctions.IfFileExistsDel(outTxtFile);

            using (var outchunkWriter = new StreamWriter(outTxtFile, true))
            {
                for (int i = 0; i < filelistHeader.FileCount; i++)
                {
                    string whiteFileInfoString;

                    if (gameCode == GameCode.ff131 || gameCode == GameCode.dirge)
                    {
                        var filelistEntryV1 = filelistEntryV1Table[i];
                        whiteFileInfoString = FilelistLoader.GetWhiteFileInfoString(filelistEntryV1.FileInfoPos, filelistChunks, filelistEntryV1.ChunkID);
                    }
                    else
                    {
                        var filelistEntryV2 = filelistEntryV2Table[i];
                        whiteFileInfoString = FilelistLoader.GetWhiteFileInfoString(filelistEntryV2.FileInfoPos, filelistChunks, filelistEntryV2.ChunkID);
                    }

                    outchunkWriter.WriteLine(whiteFileInfoString);
                }

                outchunkWriter.WriteLine("end");
            }

            logWriter.LogMessage($"\nFinished writing filepaths to \"{Path.GetFileName(outTxtFile)}\"");
        }
    }
}
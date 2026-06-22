using System.IO;
using System.Text;
using WhiteBinTools.Filelist;
using WhiteBinTools.Support;
using static WhiteBinTools.Support.Enumerators;

namespace WhiteBinTools.Unpack
{
    internal class UnpackTypeC
    {
        public static void UnpackMultiple(GameCode gameCode, string filelistFile, string whiteBinFile, string whiteVirtualDirPath, StreamWriter logWriter)
        {
            whiteVirtualDirPath = whiteVirtualDirPath.Replace("*", "");
            whiteVirtualDirPath = whiteVirtualDirPath.Replace('\\', '/');

            SharedFunctions.CheckFileExists(filelistFile, logWriter, "Error: Filelist file specified in the argument is missing");
            SharedFunctions.CheckFileExists(whiteBinFile, logWriter, "Error: Image bin file specified in the argument is missing");

            var filelistLoadData = FilelistLoader.LoadFilelist(gameCode, filelistFile, logWriter);

            var filelistHeader = filelistLoadData.FilelistHeader;
            var filelistEntryV1Table = filelistLoadData.FilelistEntryV1Table;
            var filelistEntryV2Table = filelistLoadData.FilelistEntryV2Table;
            var filelistChunks = filelistLoadData.FilelistChunks;

            var whiteBinName = Path.GetFileName(whiteBinFile);
            var unpackDir = Path.Combine(Path.GetDirectoryName(whiteBinFile), $"_{whiteBinName}");

            if (!Directory.Exists(unpackDir))
            {
                Directory.CreateDirectory(unpackDir);
            }

            var hasExtracted = false;

            var duplicateCounter = 0;

            using (var whiteBinStream = new FileStream(whiteBinFile, FileMode.Open, FileAccess.Read))
            {
                var noPathCounter = 0;
                string[] filePathDirData;

                for (int i = 0; i < filelistHeader.FileCount; i++)
                {
                    string whiteFileInfoString;
                    uint fileCode;

                    if (gameCode == GameCode.ff131 || gameCode == GameCode.dirge)
                    {
                        var filelistEntryV1 = filelistEntryV1Table[i];
                        whiteFileInfoString = FilelistLoader.GetWhiteFileInfoString(filelistEntryV1.FileInfoPos, filelistChunks, filelistEntryV1.ChunkID);
                        fileCode = filelistEntryV1.FileCode;
                    }
                    else
                    {
                        var filelistEntryV2 = filelistEntryV2Table[i];
                        whiteFileInfoString = FilelistLoader.GetWhiteFileInfoString(filelistEntryV2.FileInfoPos, filelistChunks, filelistEntryV2.ChunkID);
                        fileCode= filelistEntryV2.FileCode;
                    }

                    var whiteFileInfoData = FilelistLoader.GetWhiteFileInfoData(whiteFileInfoString, gameCode, fileCode, ref noPathCounter);

                    filePathDirData = whiteFileInfoData.FilePath.Split('/');

                    var assembledDir = new StringBuilder();
                    var assembledDirFixed = string.Empty;

                    foreach (var dir in filePathDirData)
                    {
                        assembledDir.Append(dir);
                        assembledDir.Append('/');
                        assembledDirFixed = assembledDir.ToString();

                        if (assembledDirFixed == whiteVirtualDirPath)
                        {
                            break;
                        }
                    }

                    if (assembledDirFixed == whiteVirtualDirPath)
                    {
                        var unpackedState = UnpackHelper.UnpackFile(whiteFileInfoData, unpackDir, ref duplicateCounter, whiteBinStream);

                        logWriter.LogMessage($"{unpackedState} _{Path.Combine(whiteBinName, whiteFileInfoData.FilePath)}");
                        hasExtracted = true;
                    }
                }
            }

            if (hasExtracted)
            {
                logWriter.LogMessage($"\nFinished unpacking file from \"{whiteBinName}\"");

                if (duplicateCounter > 0)
                {
                    logWriter.LogMessage($"{duplicateCounter} duplicate file(s)");
                }
            }
            else
            {
                logWriter.LogMessage("Specified file does not exist. please specify a valid file path.");
            }
        }
    }
}
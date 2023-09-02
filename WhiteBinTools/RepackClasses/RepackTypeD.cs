using System.IO;
using WhiteBinTools.SupportClasses;

namespace WhiteBinTools
{
    internal class RepackTypeD
    {
        public static void RepackFilelist(CmnEnums.GameCodes gameCodeVar, string filelistFileVar, string extractedFilelistDir, StreamWriter logWriter)
        {
            filelistFileVar.CheckFileExists(logWriter, "Error: Filelist file specified in the argument is missing");
            extractedFilelistDir.CheckDirExists(logWriter, "Error: Unpacked filelist directory specified in the argument is missing");


        }
    }
}
using System.Diagnostics;

namespace WhiteBinTools
{
    internal class CryptProcess
    {
        public static void FFXiiiCryptTool(string CryptDir, string Action, string FileListName, ref string ActionType)
        {
            using (Process xiiiCrypt = new Process())
            {
                xiiiCrypt.StartInfo.WorkingDirectory = CryptDir;
                xiiiCrypt.StartInfo.FileName = "ffxiiicrypt.exe";
                xiiiCrypt.StartInfo.Arguments = Action + FileListName + ActionType;
                xiiiCrypt.StartInfo.UseShellExecute = true;
                xiiiCrypt.Start();
                xiiiCrypt.WaitForExit();
            }
        }
    }
}
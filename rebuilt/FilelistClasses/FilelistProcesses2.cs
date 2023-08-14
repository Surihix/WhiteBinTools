using System.Diagnostics;
using System.IO;
using WhiteBinTools.SupportClasses;

namespace WhiteBinTools.FilelistClasses
{
    internal partial class FilelistProcesses
    {
        public static void CryptProcess(CmnEnums.GameCodes gameCodeVar, FilelistProcesses filelistVariables, StreamWriter writerName)
        {

            // Check for encryption header in the filelist file,
            // if the game code is set to ff13-1
            if (gameCodeVar.Equals(CmnEnums.GameCodes.ff131))
            {
                filelistVariables.IsEncrypted = CheckIfEncrypted(filelistVariables.MainFilelistFile);

                if (filelistVariables.IsEncrypted.Equals(true))
                {
                    IOhelpers.LogMessage("Error: Detected encrypted filelist file. set the game code to -ff132 for handling this type of filelist", writerName);
                    
                    if (Directory.Exists(filelistVariables.DefaultChunksExtDir))
                    {
                        Directory.Delete(filelistVariables.DefaultChunksExtDir, true);
                    }

                    IOhelpers.ErrorExit("");
                }
            }


            // Check if the ffxiiicrypt tool is present in the filelist directory
            // and if it doesn't exist copy it to the directory from the app
            // directory.
            // If the ffxiiicrypt tool does not exist in app directory, then
            // throw a error and exit
            if (gameCodeVar.Equals(CmnEnums.GameCodes.ff132))
            {
                filelistVariables.IsEncrypted = CheckIfEncrypted(filelistVariables.MainFilelistFile);

                if (filelistVariables.IsEncrypted.Equals(true))
                {
                    filelistVariables.CryptToolPresentBefore = true;

                    if (!File.Exists(filelistVariables.MainFilelistDirectory + "\\ffxiiicrypt.exe"))
                    {
                        filelistVariables.CryptToolPresentBefore = false;

                        if (File.Exists("ffxiiicrypt.exe"))
                        {
                            File.Copy("ffxiiicrypt.exe", filelistVariables.MainFilelistDirectory + "\\ffxiiicrypt.exe");
                        }
                        else
                        {
                            IOhelpers.LogMessage("Error: Unable to locate ffxiiicrypt tool in the main app folder to decrypt the filelist file", writerName);

                            if (Directory.Exists(filelistVariables.DefaultChunksExtDir))
                            {
                                Directory.Delete(filelistVariables.DefaultChunksExtDir, true);
                            }

                            IOhelpers.ErrorExit("");
                        }
                    }
                }
            }


            // If the filelist is encrypted then decrypt the filelist file
            // by first creating a temp copy of the filelist 
            if (filelistVariables.IsEncrypted.Equals(true))
            {
                filelistVariables.TmpDcryptFilelistFile.IfFileExistsDel();
                File.Copy(filelistVariables.MainFilelistFile, filelistVariables.TmpDcryptFilelistFile);

                var cryptFilelistCode = " filelist";

                FFXiiiCryptTool(filelistVariables.MainFilelistDirectory, " -d ", "\"" + filelistVariables.TmpDcryptFilelistFile + "\"", ref cryptFilelistCode);

                filelistVariables.MainFilelistFile = filelistVariables.TmpDcryptFilelistFile;
            }
        }


        static bool CheckIfEncrypted(string filelistFileVar)
        {
            var isEncrypted = false;
            using (var encStream = new FileStream(filelistFileVar, FileMode.Open, FileAccess.Read))
            {
                using (var encStreamReader = new BinaryReader(encStream))
                {
                    encStreamReader.BaseStream.Position = 20;
                    var encHeaderNumber = encStreamReader.ReadUInt32();

                    if (encHeaderNumber == 501232760)
                    {
                        isEncrypted = true;
                    }
                }
            }

            return isEncrypted;
        }


        public static void Encrypt()
        {

        }


        static void FFXiiiCryptTool(string cryptDir, string actionSwitch, string filelistName, ref string actionType)
        {
            using (Process xiiiCrypt = new Process())
            {
                xiiiCrypt.StartInfo.WorkingDirectory = cryptDir;
                xiiiCrypt.StartInfo.FileName = "ffxiiicrypt.exe";
                xiiiCrypt.StartInfo.Arguments = actionSwitch + filelistName + actionType;
                xiiiCrypt.StartInfo.UseShellExecute = true;
                xiiiCrypt.Start();
                xiiiCrypt.WaitForExit();
            }
        }
    }
}
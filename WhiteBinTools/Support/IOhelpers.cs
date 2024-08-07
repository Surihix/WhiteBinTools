using System;
using System.IO;

namespace WhiteBinTools.Support
{
    internal class IOhelpers
    {
        public static void ErrorExit(string errorMsg)
        {
            Console.WriteLine(errorMsg);
            Environment.Exit(1);
        }


        public static void CheckFileExists(string fileToCheck, StreamWriter logWriter, string missingErrorMsg)
        {
            if (!File.Exists(fileToCheck))
            {
                logWriter.LogMessage(missingErrorMsg);
                ErrorExit("");
            }
        }


        public static void CheckDirExists(string directoryPath, StreamWriter logWriter, string missingErrorMsg)
        {
            if (!Directory.Exists(directoryPath))
            {
                logWriter.LogMessage(missingErrorMsg);
                ErrorExit("");
            }
        }


        public static void IfFileExistsDel(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }


        public static void IfDirExistsDel(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }        
    }
}
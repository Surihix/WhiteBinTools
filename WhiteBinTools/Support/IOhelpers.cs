using System;
using System.IO;

namespace WhiteBinTools.Support
{
    internal static class IOhelpers
    {
        public static void ErrorExit(this string errorMsg)
        {
            Console.WriteLine(errorMsg);
            Environment.Exit(1);
        }


        public static void LogMessage(this StreamWriter logWriter, string message)
        {
            logWriter.WriteLine(message);
            Console.WriteLine(message);
        }


        public static void CheckFileExists(this string fileToCheck, StreamWriter logWriter, string missingErrorMsg)
        {
            if (!File.Exists(fileToCheck))
            {
                LogMessage(logWriter, missingErrorMsg);
                logWriter.DisposeIfLogStreamOpen();
                ErrorExit("");
            }
        }


        public static void CheckDirExists(this string directoryPath, StreamWriter logWriter, string missingErrorMsg)
        {
            if (!Directory.Exists(directoryPath))
            {
                LogMessage(logWriter, missingErrorMsg);
                logWriter.DisposeIfLogStreamOpen();
                ErrorExit("");
            }
        }


        public static void DisposeIfLogStreamOpen(this StreamWriter logWriter)
        {
            if (logWriter.BaseStream.CanWrite)
            {
                logWriter.Dispose();
            }
        }


        public static void IfFileExistsDel(this string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }


        public static void IfDirExistsDel(this string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }        
    }
}
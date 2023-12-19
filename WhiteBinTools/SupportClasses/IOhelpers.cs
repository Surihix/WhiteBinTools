using System;
using System.IO;

namespace WhiteBinTools.SupportClasses
{
    internal static class IOhelpers
    {
        public static void ErrorExit(string errorMsg)
        {
            Console.WriteLine(errorMsg);
            Console.ReadLine();
            Environment.Exit(0);
        }


        public static void LogMessage(string message, StreamWriter logWriter)
        {
            Console.WriteLine(message);
            logWriter.WriteLine(message);
        }


        public static void CheckFileExists(this string fileToCheck, StreamWriter logWriter, string missingErrorMsg)
        {
            if (!File.Exists(fileToCheck))
            {
                LogMessage(missingErrorMsg, logWriter);
                logWriter.DisposeIfLogStreamOpen();
                ErrorExit("");
            }
        }


        public static void CheckDirExists(this string directoryPath, StreamWriter logWriter, string missingErrorMsg)
        {
            if (!Directory.Exists(directoryPath))
            {
                LogMessage(missingErrorMsg, logWriter);
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

        
        public static void ExCopyTo(this Stream source, Stream destination, long offset, long count, int bufferSize = 81920)
        {
            var returnAddress = source.Position;
            source.Seek(offset, SeekOrigin.Begin);

            var bytesRemaining = count;
            while (bytesRemaining > 0)
            {
                var readSize = Math.Min(bufferSize, bytesRemaining);
                var buffer = new byte[readSize];
                _ = source.Read(buffer, 0, (int)readSize);

                destination.Write(buffer, 0, (int)readSize);
                bytesRemaining -= readSize;
            }

            source.Seek(returnAddress, SeekOrigin.Begin);
        }
    }
}
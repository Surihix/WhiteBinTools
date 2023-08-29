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


        public static void CheckFileExists(this string fileVar, StreamWriter logWriter, string missingErrorMsg)
        {
            if (!File.Exists(fileVar))
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
            if (logWriter.BaseStream.CanWrite.Equals(true))
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


        /// <summary>
        /// Copies a set amount of bytes from the source stream to the destination stream
        /// </summary>
        /// <param name="source">The stream to copy bytes from</param>
        /// <param name="destination">The stream to write the copied bytes to</param>
        /// <param name="offset">The position in the source stream to begin copying from</param>
        /// <param name="count">The number of bytes to copy from the source stream</param>
        /// <param name="bufferSize">The size of the temporary buffer the bytes are copied to (default size taken from <see cref="FileStream"/>)</param>
        public static void ExtendedCopyTo(this Stream source, Stream destination, long offset, long count, int bufferSize = 81920)
        {
            // Seek to the given offset of the source stream
            var returnAddress = source.Position;
            source.Seek(offset, SeekOrigin.Begin);

            // Copy the data in chunks of bufferSize bytes until all are done
            var bytesRemaining = count;
            while (bytesRemaining > 0)
            {
                var readSize = Math.Min(bufferSize, bytesRemaining);
                var buffer = new byte[readSize];
                _ = source.Read(buffer, 0, (int)readSize);

                destination.Write(buffer, 0, (int)readSize);
                bytesRemaining -= readSize;
            }

            // Seek the source stream back to where it was
            source.Seek(returnAddress, SeekOrigin.Begin);
        }
    }
}
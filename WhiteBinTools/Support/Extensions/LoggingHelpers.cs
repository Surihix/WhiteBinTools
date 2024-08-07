using System;
using System.IO;

internal static class LoggingHelpers
{
    public static void LogMessage(this StreamWriter logWriter, string message)
    {
        logWriter.WriteLine(message);
        Console.WriteLine(message);
    }


    public static void DisposeIfLogStreamOpen(this StreamWriter logWriter)
    {
        if (logWriter.BaseStream.CanWrite)
        {
            logWriter.Dispose();
        }
    }
}
using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;

namespace WhiteBinTools
{
    internal class CmnMethods
    {
        public static void CheckArguments(ref int TotalLength, int requiredLength)
        {
            if (TotalLength < requiredLength)
            {
                Console.WriteLine("Error: Specified action requires one or more arguments");
                ErrorExit("");
            }
        }

        public static void CrashLog(string CrashMsg)
        {
            IfFileExistsDel("CrashLog.txt");
            using (FileStream CrashLogFile = new FileStream("CrashLog.txt", FileMode.Append, FileAccess.Write))
            {
                using (StreamWriter CrashLogWriter = new StreamWriter(CrashLogFile))
                {
                    CrashLogWriter.WriteLine(CrashMsg);
                }
            }
        }

        public static void ErrorExit(string ErrorMsg)
        {
            Console.WriteLine(ErrorMsg);
            Console.ReadLine();
            Environment.Exit(0);
        }

        public static void IfFileExistsDel(string FilePath)
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }

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

        public static void DecToHex(uint DecValue, ref string HexValue)
        {
            HexValue = DecValue.ToString("x");
        }

        public static void AdjustBytesUInt16(BinaryWriter WriterName, int WriterPos, out byte[] AdjustByteVar,
            ushort NewAdjustVar)
        {
            WriterName.BaseStream.Position = WriterPos;
            AdjustByteVar = new byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(AdjustByteVar, NewAdjustVar);
            WriterName.Write(AdjustByteVar);
        }

        public static void AdjustBytesUInt32(BinaryWriter WriterName, uint WriterPos, out byte[] AdjustByteVar,
            uint NewAdjustVar, string EndianType)
        {
            WriterName.BaseStream.Position = WriterPos;
            AdjustByteVar = new byte[4];
            switch (EndianType)
            {
                case "le":
                    BinaryPrimitives.WriteUInt32LittleEndian(AdjustByteVar, NewAdjustVar);
                    break;
                case "be":
                    BinaryPrimitives.WriteUInt32BigEndian(AdjustByteVar, NewAdjustVar);
                    break;
            }
            WriterName.Write(AdjustByteVar);
        }
    }
}
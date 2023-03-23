using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WhiteBinTools
{
    internal class Core
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Warning: Enough arguments not specified");
                Console.WriteLine("");
                Help.ShowCommands();
            }

            try
            {
                // Basic arguments
                // Assign the arguments to the proper variables
                var argument_1 = Convert.ToInt16(args[0]);
                var argument_2 = args[1].ToLower();
                var argument_3 = args[2];

                var argument_4 = "";
                if (args.Length > 3)
                {
                    argument_4 = args[3];
                }

                var GameCodeVar = argument_1;
                var ToolAction = argument_2;
                var FilelistFileVar = argument_3;
                var WhiteBinOrDirVar = argument_4;

                // Check argument 1 and 2
                string[] ActionList = { "-u", "-r", "-f", "-uf", "-rf", "-rfm" };
                int[] GameCodesList = { 1, 2 };

                if (!ActionList.Contains(ToolAction))
                {
                    Console.WriteLine("Warning: Invalid tool action specified");
                    Help.ShowCommands();
                }
                if (!GameCodesList.Contains(GameCodeVar))
                {
                    Console.WriteLine("Warning: Specified game code is incorrect");
                    Help.ShowCommands();
                }

                // Additional argument for handling a specific file
                // Also used for directory argument for -rfm tool action
                var WhiteFilePathOrDirVar = "";
                if (args.Length > 4)
                {
                    var argument_5 = args[4];
                    WhiteFilePathOrDirVar = argument_5;
                }

                IfFileExistsDel("log.txt");
                var TotalArgCount = args.Length;


                switch (ToolAction)
                {
                    case "-u":
                        CheckArguments(ref TotalArgCount, 3);
                        BinUnpack.Unpack(GameCodeVar, FilelistFileVar, WhiteBinOrDirVar);
                        break;

                    case "-r":
                        CheckArguments(ref TotalArgCount, 3);
                        BinRepack.Repack(GameCodeVar, FilelistFileVar, WhiteBinOrDirVar);
                        break;

                    case "-f":
                        CheckArguments(ref TotalArgCount, 2);
                        BinUnpkFilePaths.UnpkFilelist(GameCodeVar, FilelistFileVar);
                        break;

                    case "-uf":
                        CheckArguments(ref TotalArgCount, 5);
                        BinUnpkAFile.UnpackFile(GameCodeVar, FilelistFileVar, WhiteBinOrDirVar, WhiteFilePathOrDirVar);
                        break;

                    case "-rf":
                        CheckArguments(ref TotalArgCount, 5);
                        BinRpkAFile.RepackFile(GameCodeVar, FilelistFileVar, WhiteBinOrDirVar, WhiteFilePathOrDirVar);
                        break;

                    case "-rfm":
                        CheckArguments(ref TotalArgCount, 5);
                        BinRpkMoreFiles.RepackMoreFiles(GameCodeVar, FilelistFileVar, WhiteBinOrDirVar, WhiteFilePathOrDirVar);
                        break;

                    default:
                        LogMsgs("Error: Proper tool action is not specified");
                        ErrorExit("");
                        break;
                }
            }
            catch (Exception ex)
            {
                ErrorExit("Error: " + ex);
            }
        }


        static void CheckArguments(ref int TotalLength, int requiredLength)
        {
            if (TotalLength < requiredLength)
            {
                LogMsgs("Error: Specified action requires one or more arguments");
                ErrorExit("");
            }
        }

        public static void LogMsgs(string LogInfo)
        {
            Console.WriteLine(LogInfo);
            using (StreamWriter LogWriter = new StreamWriter("log.txt", append: true))
            {
                LogWriter.WriteLine(LogInfo);
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
            uint NewAdjustVar)
        {
            WriterName.BaseStream.Position = WriterPos;
            AdjustByteVar = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(AdjustByteVar, NewAdjustVar);
            WriterName.Write(AdjustByteVar);
        }
    }
}
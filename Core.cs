using System;
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

                // Additional argument for specific file
                // also used to spoof the argument for 
                // -rfm tool action
                var WhiteFilePathVar = "";
                if (args.Length > 4)
                {
                    var argument_5 = args[4];
                    WhiteFilePathVar = argument_5;
                }

                IfFileExistsDel("log.txt");
                var TotalArgCount = args.Length;

                string[] ActionList = { "-u", "-uf", "-r", "-rf", "-rfm", "-f" };

                if (!ActionList.Contains(ToolAction))
                {
                    Console.WriteLine("Warning: Invalid tool action specified");
                    Help.ShowCommands();
                }

                switch (ToolAction)
                {
                    case "-u":
                        CheckArguments(ref TotalArgCount, 3);
                        CheckGameCode(ref GameCodeVar);
                        BinUnpack.Unpack(GameCodeVar, FilelistFileVar, WhiteBinOrDirVar);
                        break;

                    case "-uf":
                        CheckArguments(ref TotalArgCount, 5);
                        CheckGameCode(ref GameCodeVar);
                        BinUnpkAFile.UnpackFile(GameCodeVar, FilelistFileVar, WhiteBinOrDirVar, WhiteFilePathVar);
                        break;

                    case "-r":
                        CheckArguments(ref TotalArgCount, 3);
                        CheckGameCode(ref GameCodeVar);
                        BinRepack.Repack(GameCodeVar, FilelistFileVar, WhiteBinOrDirVar);
                        break;

                    case "-rf":
                        CheckArguments(ref TotalArgCount, 5);
                        CheckGameCode(ref GameCodeVar);
                        BinRpkAFile.RepackFile(GameCodeVar, FilelistFileVar, WhiteBinOrDirVar, WhiteFilePathVar);
                        break;

                    case "-rfm":
                        CheckArguments(ref TotalArgCount, 5);
                        CheckGameCode(ref GameCodeVar);
                        BinRpkMoreFiles.RepackMoreFiles(GameCodeVar, FilelistFileVar, WhiteBinOrDirVar, WhiteFilePathVar);
                        break;

                    case "-f":
                        CheckArguments(ref TotalArgCount, 2);
                        CheckGameCode(ref GameCodeVar);
                        BinUnpkFilePaths.UnpkFilelist(GameCodeVar, FilelistFileVar);
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

        static void CheckGameCode(ref short GameCodeVal)
        {
            short[] GameCodes = { 1, 2 };

            if (!GameCodes.Contains(GameCodeVal))
            {
                LogMsgs("Error: Specified game code is incorrect");
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
    }
}
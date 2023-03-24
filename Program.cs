using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Linq;
using WhiteBinTools.Core;

namespace WhiteBinTools {
    internal class Program {
        static void Main(string[] args) {
            if (args.Length < 2) {
                Console.WriteLine("Warning: Enough arguments not specified");
                Console.WriteLine("");
                Help.ShowCommands();
            }

            try {
                // Basic arguments
                // Assign the arguments to the proper variables
                var argument_1 = Convert.ToInt16(args[0]);
                var argument_2 = args[1];
                var argument_3 = args[2];

                var argument_4 = "";
                if (args.Length > 3) {
                    argument_4 = args[3];
                }

                var GameCodeVar = argument_1;
                var ToolAction = argument_2;
                var FilelistFileVar = argument_3;
                var WhiteBinOrDirVar = argument_4;

                // Check argument 1 and 2
                string[] ActionList = { "-u", "-r", "-f", "-uf", "-rf", "-rfm" };
                int[] GameCodesList = { 1, 2 };

                if (!ActionList.Contains(ToolAction)) {
                    Console.WriteLine("Warning: Invalid tool action specified");
                    Help.ShowCommands();
                }
                if (!GameCodesList.Contains(GameCodeVar)) {
                    Console.WriteLine("Warning: Specified game code is incorrect");
                    Help.ShowCommands();
                }

                // Additional argument for handling a specific file
                // Also used for directory argument for -rfm tool action
                var WhiteFilePathOrDirVar = "";
                if (args.Length > 4) {
                    var argument_5 = args[4];
                    WhiteFilePathOrDirVar = argument_5;
                }

                Core.Utils.IfFileExistsDel("log.txt");
                var TotalArgCount = args.Length;


                switch (ToolAction) {
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
                        Core.Utils.LogMsgs("Error: Proper tool action is not specified");
                        Core.Utils.ErrorExit("");
                        break;
                }
            }
            catch (Exception ex) {
                Core.Utils.ErrorExit("Error: " + ex);
            }
        }


        static void CheckArguments(ref int TotalLength, int requiredLength) {
            if (TotalLength < requiredLength) {
                Core.Utils.LogMsgs("Error: Specified action requires one or more arguments");
                Core.Utils.ErrorExit("");
            }
        }
    }
}
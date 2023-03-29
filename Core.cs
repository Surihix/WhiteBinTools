using System;
using System.Linq;

namespace WhiteBinTools
{
    internal class Core
    {
        static void Main(string[] args)
        {
            // Check for arg length to check if the app is 
            // launched with either of the help switches
            if (args.Length < 1)
            {
                Console.WriteLine("Warning: Enough arguments not specified");
                Console.WriteLine("");
                Environment.Exit(0);
            }

            if (args[0].Contains("-h") || args[0].Contains("-?"))
            {
                Help.ShowCommands();
            }


            // Check for arg length for checking if the app is
            // launched with any of the supported tool functions
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
                var argument_1 = args[0];
                var argument_2 = args[1];
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
                string[] GameCodesList = { "-ff131", "-ff132" };

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

                CmnMethods.IfFileExistsDel("log.txt");
                var TotalArgCount = args.Length;


                switch (ToolAction)
                {
                    case "-u":
                        CmnMethods.CheckArguments(ref TotalArgCount, 3);
                        BinUnpack.Unpack(GameCodeVar, FilelistFileVar, WhiteBinOrDirVar);
                        break;

                    case "-r":
                        CmnMethods.CheckArguments(ref TotalArgCount, 3);
                        BinRepack.Repack(GameCodeVar, FilelistFileVar, WhiteBinOrDirVar);
                        break;

                    case "-f":
                        CmnMethods.CheckArguments(ref TotalArgCount, 2);
                        BinUnpkFilePaths.UnpkFilelist(GameCodeVar, FilelistFileVar);
                        break;

                    case "-uf":
                        CmnMethods.CheckArguments(ref TotalArgCount, 5);
                        BinUnpkAFile.UnpackFile(GameCodeVar, FilelistFileVar, WhiteBinOrDirVar, WhiteFilePathOrDirVar);
                        break;

                    case "-rf":
                        CmnMethods.CheckArguments(ref TotalArgCount, 5);
                        BinRpkAFile.RepackFile(GameCodeVar, FilelistFileVar, WhiteBinOrDirVar, WhiteFilePathOrDirVar);
                        break;

                    case "-rfm":
                        CmnMethods.CheckArguments(ref TotalArgCount, 5);
                        BinRpkMoreFiles.RepackMoreFiles(GameCodeVar, FilelistFileVar, WhiteBinOrDirVar, WhiteFilePathOrDirVar);
                        break;

                    default:
                        CmnMethods.LogMsgs("Error: Proper tool action is not specified");
                        CmnMethods.ErrorExit("");
                        break;
                }
            }
            catch (Exception ex)
            {
                CmnMethods.ErrorExit("Error: " + ex);
            }
        }
    }
}
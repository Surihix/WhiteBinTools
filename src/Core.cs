using System;
using WhiteBinTools.src.Common;
using WhiteBinTools.src.Repack;
using WhiteBinTools.src.Unpack;

namespace WhiteBinTools.src
{
    internal class Core
    {
        static void Main(string[] args)
        {
            // Check for arg length to check if the app is 
            // launched with either of the help switches
            if (args.Length < 1)
            {
                Console.WriteLine("Warning: Enough arguments not specified. Please use -? or -h switches for more information");
                Console.WriteLine("");
                Environment.Exit(0);
            }

            if (args[0].Contains("-h") || args[0].Contains("-?"))
            {
                Help.ShowCommands();
            }


            // Check for arg length when checking if the app is
            // launched with any of the supported tool functions
            // This prevents exceptions from occuring if
            // the basic args is not provided.
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
                var arg_1 = args[0];
                var arg_2 = args[1];
                var arg_3 = args[2];

                // White bin file or unpacked dir is 
                // used only when the length of the args
                // is more than 3.
                // Its this way cause of the filepaths 
                // feature.
                var arg_4 = "";
                if (args.Length > 3)
                {
                    arg_4 = args[3];
                }

                var specifiedGameCode = arg_1.Replace("-", "");
                var specifiedActionSwitch = arg_2.Replace("-", "");
                var filelistFile = arg_3;
                var whiteBinOrDir = arg_4;

                // Check argument 1 and 2
                var gameCode = CmnEnums.GameCodes.none;
                if (Enum.TryParse(specifiedGameCode, false, out CmnEnums.GameCodes convertedGameCode))
                {
                    gameCode = convertedGameCode;
                }
                else
                {
                    Console.WriteLine("Warning: Specified game code is incorrect");
                    Help.ShowCommands();
                }

                var actionSwitch = CmnEnums.ActionSwitches.none;
                if (Enum.TryParse(specifiedActionSwitch, false, out CmnEnums.ActionSwitches convertedActionSwitch))
                {
                    actionSwitch = convertedActionSwitch;
                }
                else
                {
                    Console.WriteLine("Warning: Specified tool action is invalid");
                    Help.ShowCommands();
                }


                // Additional argument for handling a specific file
                // Also used for directory argument for -rmf tool action
                var WhiteFilePathOrDirVar = "";
                if (args.Length > 4)
                {
                    var argument_5 = args[4];
                    WhiteFilePathOrDirVar = argument_5;
                }

                CmnMethods.IfFileExistsDel("ProcessLog.txt");
                var totalArgCount = args.Length;


                switch (actionSwitch)
                {
                    case CmnEnums.ActionSwitches.u:
                        CmnMethods.CheckArguments(ref totalArgCount, 3);
                        BinUnpack.Unpack(gameCode, filelistFile, whiteBinOrDir);
                        break;

                    case CmnEnums.ActionSwitches.r:
                        CmnMethods.CheckArguments(ref totalArgCount, 3);
                        BinRepack.Repack(gameCode, filelistFile, whiteBinOrDir);
                        break;

                    case CmnEnums.ActionSwitches.ufp:
                        CmnMethods.CheckArguments(ref totalArgCount, 2);
                        BinUnpkFilePaths.UnpkFilelist(gameCode, filelistFile);
                        break;

                    case CmnEnums.ActionSwitches.uaf:
                        CmnMethods.CheckArguments(ref totalArgCount, 5);
                        BinUnpkAFile.UnpackFile(gameCode, filelistFile, whiteBinOrDir, WhiteFilePathOrDirVar);
                        break;

                    case CmnEnums.ActionSwitches.raf:
                        CmnMethods.CheckArguments(ref totalArgCount, 5);
                        BinRpkAFile.RepackFile(gameCode, filelistFile, whiteBinOrDir, WhiteFilePathOrDirVar);
                        break;

                    case CmnEnums.ActionSwitches.rmf:
                        CmnMethods.CheckArguments(ref totalArgCount, 5);
                        BinRpkMoreFiles.RepackMoreFiles(gameCode, filelistFile, whiteBinOrDir, WhiteFilePathOrDirVar);
                        break;

                    default:
                        Console.WriteLine("Error: Proper tool action is not specified");
                        CmnMethods.ErrorExit("");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
                CmnMethods.CrashLog("Error: " + ex);
                Console.WriteLine("");
                CmnMethods.ErrorExit("Crash exception recorded in CrashLog.txt file");
            }
        }
    }
}
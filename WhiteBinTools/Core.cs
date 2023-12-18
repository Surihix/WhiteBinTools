using System;
using System.IO;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.RepackClasses;
using WhiteBinTools.SupportClasses;
using WhiteBinTools.UnpackClasses;
using static WhiteBinTools.SupportClasses.ProgramEnums;

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
                var specifiedGameCode = args[0].Replace("-", "");
                var specifiedActionSwitch = args[1].Replace("-", "");
                var filelistFileOrDir = args[2];

                // whiteBinOrDir value is assigned from
                // the arg only when the length of
                // the args is more than 3.
                // This is to accomodate for the filepaths
                // feature.
                var whiteBinOrDir = "";
                if (args.Length > 3)
                {
                    whiteBinOrDir = args[3];
                }


                // Check argument 1 and 2 and assign
                // the appropriate enum values to it
                var gameCode = GameCodes.none;
                if (Enum.TryParse(specifiedGameCode, false, out GameCodes convertedGameCode))
                {
                    gameCode = convertedGameCode;
                }
                else
                {
                    Console.WriteLine("Warning: Specified game code was incorrect");
                    Help.ShowCommands();
                }

                var actionSwitch = ActionSwitches.none;
                if (Enum.TryParse(specifiedActionSwitch, false, out ActionSwitches convertedActionSwitch))
                {
                    actionSwitch = convertedActionSwitch;
                }
                else
                {
                    Console.WriteLine("Warning: Specified tool action was invalid");
                    Help.ShowCommands();
                }


                // args for handling a specific file
                // and directory of unpacked files.
                // whiteFilePathOrDirVar value is assigned from
                // the arg only when the args length is more than 4.
                var whiteFilePathOrDirVar = "";
                if (args.Length > 4)
                {
                    whiteFilePathOrDirVar = args[4];
                }

                IOhelpers.IfFileExistsDel("ProcessLog.txt");
                var totalArgCount = args.Length;


                using (var logStream = new FileStream("ProcessLog.txt", FileMode.Append, FileAccess.Write, FileShare.Write))
                {
                    using (var logWriter = new StreamWriter(logStream))
                    {

                        switch (gameCode)
                        {
                            case GameCodes.ff131:
                                IOhelpers.LogMessage("GameCode is set to ff13-1", logWriter);
                                break;

                            case GameCodes.ff132:
                                IOhelpers.LogMessage("GameCode is set to ff13-2", logWriter);
                                break;
                        }


                        switch (actionSwitch)
                        {
                            case ActionSwitches.u:
                                CheckArguments(totalArgCount, 3);
                                UnpackTypeA.UnpackFull(gameCode, filelistFileOrDir, whiteBinOrDir, logWriter);
                                break;

                            case ActionSwitches.r:
                                CheckArguments(totalArgCount, 3);
                                RepackTypeA.RepackAll(gameCode, filelistFileOrDir, whiteBinOrDir, logWriter);
                                break;

                            case ActionSwitches.uaf:
                                CheckArguments(totalArgCount, 5);
                                UnpackTypeB.UnpackSingle(gameCode, filelistFileOrDir, whiteBinOrDir, whiteFilePathOrDirVar, logWriter);
                                break;

                            case ActionSwitches.ufp:
                                CheckArguments(totalArgCount, 2);
                                UnpackTypeC.UnpackFilelistPaths(gameCode, filelistFileOrDir, logWriter);
                                break;

                            case ActionSwitches.ufl:
                                CheckArguments(totalArgCount, 2);
                                UnpackTypeD.UnpackFilelist(gameCode, filelistFileOrDir, logWriter);
                                break;

                            case ActionSwitches.raf:
                                CheckArguments(totalArgCount, 5);
                                RepackTypeB.RepackSingle(gameCode, filelistFileOrDir, whiteBinOrDir, whiteFilePathOrDirVar, logWriter);
                                break;

                            case ActionSwitches.rmf:
                                CheckArguments(totalArgCount, 5);
                                RepackTypeC.RepackMultiple(gameCode, filelistFileOrDir, whiteBinOrDir, whiteFilePathOrDirVar, logWriter);
                                break;

                            case ActionSwitches.rfl:
                                CheckArguments(totalArgCount, 2);
                                RepackTypeD.RepackFilelist(gameCode, filelistFileOrDir, logWriter);
                                break;

                            default:
                                Console.WriteLine("Error: Proper tool action is not specified");
                                IOhelpers.ErrorExit("");
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
                IOhelpers.IfFileExistsDel("CrashLog.txt");

                var filelistVariables = new FilelistVariables();
                if (Directory.Exists(filelistVariables.DefaultChunksExtDir))
                {
                    Directory.Delete(filelistVariables.DefaultChunksExtDir, true);
                }

                using (FileStream crashLogFile = new FileStream("CrashLog.txt", FileMode.Append, FileAccess.Write))
                {
                    using (StreamWriter crashLogWriter = new StreamWriter(crashLogFile))
                    {
                        crashLogWriter.WriteLine("Error: " + ex);
                    }
                }

                Console.WriteLine("");
                IOhelpers.ErrorExit("Crash exception recorded in CrashLog.txt file");
            }
        }


        enum ActionSwitches
        {
            u,
            r,
            ufp,
            uaf,
            ufl,
            raf,
            rmf,
            rfl,
            none
        }


        static void CheckArguments(int totalLength, int requiredLength)
        {
            if (totalLength < requiredLength)
            {
                IOhelpers.ErrorExit("Error: Specified action requires one or more arguments");
            }
        }
    }
}
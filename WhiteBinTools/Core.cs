using System;
using System.IO;
using WhiteBinTools.Repack;
using WhiteBinTools.Support;
using WhiteBinTools.Unpack;
using static WhiteBinTools.Support.ProgramEnums;

namespace WhiteBinTools
{
    internal class Core
    {
        public static readonly string PathSeparatorChar = Convert.ToString(Path.DirectorySeparatorChar);
        public static bool ShouldBckup { get; set; }

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Check for arg length to see
            // if the app is launched with
            // either of the help switches
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

            var argsLength = args.Length;

            if (argsLength < 2)
            {
                Console.WriteLine("Warning: Enough arguments not specified");
                Console.WriteLine("");
                Help.ShowCommands();
            }


            // Assign the gameCode and
            // actionSwitch args
            if (Enum.TryParse(args[0].Replace("-", ""), false, out GameCodes gameCode) == false)
            {
                Console.WriteLine("Warning: Specified game code was incorrect");
                Help.ShowCommands();
            }

            if (Enum.TryParse(args[1].Replace("-", ""), false, out ActionSwitches actionSwitch) == false)
            {
                Console.WriteLine("Warning: Specified tool action was invalid");
                Help.ShowCommands();
            }

            try
            {
                IOhelpers.IfFileExistsDel("ProcessLog.txt");

                using (var logStream = new FileStream("ProcessLog.txt", FileMode.Append, FileAccess.Write, FileShare.Write))
                {
                    using (var logWriter = new StreamWriter(logStream))
                    {
                        if (gameCode.Equals(GameCodes.ff131))
                        {
                            logWriter.LogMessage("GameCode is set to ff13-1");
                        }
                        else
                        {
                            logWriter.LogMessage("GameCode is set to ff13-2");
                        }

                        // Initialise commonly used
                        // variables
                        var filelistFile = string.Empty;
                        var whiteBinFile = string.Empty;
                        var extractedBinDir = string.Empty;
                        var whitePath = string.Empty;

                        switch (actionSwitch)
                        {
                            case ActionSwitches.u:
                                CheckArguments(argsLength, 4, actionSwitch);

                                filelistFile = args[2];
                                whiteBinFile = args[3];

                                UnpackTypeA.UnpackFull(gameCode, filelistFile, whiteBinFile, logWriter);
                                break;

                            case ActionSwitches.r:
                                CheckArguments(argsLength, 4, actionSwitch);

                                filelistFile = args[2];
                                extractedBinDir = args[3];

                                DetermineBckup(actionSwitch, argsLength, args);

                                RepackTypeA.RepackAll(gameCode, filelistFile, extractedBinDir, logWriter);
                                break;

                            case ActionSwitches.uaf:
                                CheckArguments(argsLength, 5, actionSwitch);

                                filelistFile = args[2];
                                whiteBinFile = args[3];
                                whitePath = args[4];

                                UnpackTypeB.UnpackSingle(gameCode, filelistFile, whiteBinFile, whitePath, logWriter);
                                break;

                            case ActionSwitches.umf:
                                CheckArguments(argsLength, 5, actionSwitch);

                                filelistFile = args[2];
                                whiteBinFile = args[3];
                                var whiteDir = args[4];

                                UnpackTypeC.UnpackMultiple(gameCode, filelistFile, whiteBinFile, whiteDir, logWriter);
                                break;

                            case ActionSwitches.ufl:
                                CheckArguments(argsLength, 3, actionSwitch);

                                filelistFile = args[2];

                                UnpackTypeD.UnpackFilelist(gameCode, filelistFile, logWriter);
                                break;

                            case ActionSwitches.raf:
                                CheckArguments(argsLength, 5, actionSwitch);

                                filelistFile = args[2];
                                whiteBinFile = args[3];
                                whitePath = args[4];

                                DetermineBckup(actionSwitch, argsLength, args);

                                RepackTypeB.RepackSingle(gameCode, filelistFile, whiteBinFile, whitePath, logWriter);
                                break;

                            case ActionSwitches.rmf:
                                CheckArguments(argsLength, 5, actionSwitch);

                                filelistFile = args[2];
                                whiteBinFile = args[3];
                                extractedBinDir = args[4];

                                DetermineBckup(actionSwitch, argsLength, args);

                                RepackTypeC.RepackMultiple(gameCode, filelistFile, whiteBinFile, extractedBinDir, logWriter);
                                break;

                            case ActionSwitches.rfl:
                                CheckArguments(argsLength, 3, actionSwitch);

                                var extractedFilelistDir = args[2];

                                DetermineBckup(actionSwitch, argsLength, args);

                                RepackTypeD.RepackFilelist(gameCode, extractedFilelistDir, logWriter);
                                break;

                            case ActionSwitches.cfj:
                                CheckArguments(argsLength, 3, actionSwitch);

                                filelistFile = args[2];

                                UnpackTypeE.UnpackFilelistJson(gameCode, filelistFile, logWriter);
                                break;

                            case ActionSwitches.cjf:
                                CheckArguments(argsLength, 3, actionSwitch);

                                var jsonFile = args[2];

                                DetermineBckup(actionSwitch, argsLength, args);

                                RepackTypeE.RepackJsonFilelist(gameCode, jsonFile, logWriter);
                                break;

                            case ActionSwitches.ufp:
                                CheckArguments(argsLength, 3, actionSwitch);

                                filelistFile = args[2];

                                UnpackTypePaths.UnpackFilelistPaths(gameCode, filelistFile, logWriter);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
                IOhelpers.IfFileExistsDel("CrashLog.txt");

                using (FileStream crashLogFile = new FileStream("CrashLog.txt", FileMode.Append, FileAccess.Write))
                {
                    using (StreamWriter crashLogWriter = new StreamWriter(crashLogFile))
                    {
                        crashLogWriter.WriteLine("Error: " + ex);
                    }
                }

                Console.WriteLine("");
                Console.WriteLine("Crash exception recorded in CrashLog.txt file");
                Environment.Exit(2);
            }
        }


        private enum ActionSwitches
        {
            u,
            r,
            uaf,
            umf,
            ufl,
            raf,
            rmf,
            rfl,
            cfj,
            cjf,
            ufp
        }


        private static void CheckArguments(int totalLength, int requiredLength, ActionSwitches actionSwitch)
        {
            if (totalLength < requiredLength)
            {
                IOhelpers.ErrorExit($"Error: Specified action '{actionSwitch}' requires one or more arguments");
            }
        }


        private static void DetermineBckup(ActionSwitches actionSwitch, int argsLength, string[] args)
        {
            switch (actionSwitch)
            {
                case ActionSwitches.r:
                    if (argsLength > 4)
                    {
                        if (args[4] == "-bak")
                        {
                            ShouldBckup = true;
                        }
                    }
                    break;

                case ActionSwitches.raf:
                case ActionSwitches.rmf:
                    if (argsLength > 5)
                    {
                        if (args[5] == "-bak")
                        {
                            ShouldBckup = true;
                        }
                    }
                    break;

                case ActionSwitches.rfl:
                case ActionSwitches.cjf:
                    if (argsLength > 3)
                    {
                        if (args[3] == "-bak")
                        {
                            ShouldBckup = true;
                        }
                    }
                    break;
            }
        }
    }
}
using System;
using System.IO;
using WhiteBinTools.Repack;
using WhiteBinTools.Support;
using WhiteBinTools.Unpack;
using static WhiteBinTools.Support.Enumerators;

namespace WhiteBinTools
{
    internal class Core
    {
        public static bool ShouldBckup { get; set; }

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("");
            Console.WriteLine("[WhiteBinTools v2.0.0]");
            Console.WriteLine("");

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
            if (Enum.TryParse(args[0].Replace("-", ""), false, out GameCode gameCode) == false)
            {
                Console.WriteLine("Warning: Specified game code was incorrect");
                Help.ShowCommands();
            }

            if (Enum.TryParse(args[1].Replace("-", ""), false, out ActionSwitch actionSwitch) == false)
            {
                Console.WriteLine("Warning: Specified tool action was invalid");
                Help.ShowCommands();
            }

            try
            {
                SharedFunctions.IfFileExistsDel("ProcessLog.txt");

                using (var logStream = new FileStream("ProcessLog.txt", FileMode.Append, FileAccess.Write, FileShare.Write))
                {
                    using (var logWriter = new StreamWriter(logStream))
                    {
                        switch (gameCode)
                        {
                            case GameCode.dirge:
                                logWriter.LogMessage("GameCode is set to dirge");
                                break;

                            case GameCode.ff131:
                                logWriter.LogMessage("GameCode is set to ff13-1");
                                break;

                            case GameCode.ff132:
                                logWriter.LogMessage("GameCode is set to ff13-2");
                                break;
                        }

                        // Initialise commonly used
                        // variables
                        var filelistFile = string.Empty;
                        var whiteBinFile = string.Empty;
                        var unpackedDir = string.Empty;
                        var whitePath = string.Empty;

                        switch (actionSwitch)
                        {
                            case ActionSwitch.u:
                                CheckArguments(argsLength, 4, actionSwitch);

                                filelistFile = args[2];
                                whiteBinFile = args[3];

                                UnpackTypeA.UnpackFull(gameCode, filelistFile, whiteBinFile, logWriter);
                                break;

                            case ActionSwitch.uaf:
                                CheckArguments(argsLength, 5, actionSwitch);

                                filelistFile = args[2];
                                whiteBinFile = args[3];
                                whitePath = args[4];

                                UnpackTypeB.UnpackSingle(gameCode, filelistFile, whiteBinFile, whitePath, logWriter);
                                break;


                            case ActionSwitch.umf:
                                CheckArguments(argsLength, 5, actionSwitch);

                                filelistFile = args[2];
                                whiteBinFile = args[3];
                                var whiteDir = args[4];

                                UnpackTypeC.UnpackMultiple(gameCode, filelistFile, whiteBinFile, whiteDir, logWriter);
                                break;

                            case ActionSwitch.ufl:
                                CheckArguments(argsLength, 3, actionSwitch);

                                filelistFile = args[2];

                                UnpackTypeD.UnpackFilelist(gameCode, filelistFile, logWriter);
                                break;


                            case ActionSwitch.cfj:
                                CheckArguments(argsLength, 3, actionSwitch);

                                filelistFile = args[2];

                                UnpackTypeE.UnpackFilelistJson(gameCode, filelistFile, logWriter);
                                break;


                            case ActionSwitch.ufp:
                                CheckArguments(argsLength, 3, actionSwitch);

                                filelistFile = args[2];

                                UnpackTypePaths.UnpackFilelistPaths(gameCode, filelistFile, logWriter);
                                break;

                            case ActionSwitch.r:
                                CheckArguments(argsLength, 4, actionSwitch);

                                filelistFile = args[2];
                                unpackedDir = args[3];

                                DetermineBckup(actionSwitch, argsLength, args);

                                RepackTypeA.RepackAll(gameCode, filelistFile, unpackedDir, logWriter);
                                break;

                            case ActionSwitch.raf:
                                CheckArguments(argsLength, 5, actionSwitch);

                                filelistFile = args[2];
                                whiteBinFile = args[3];
                                whitePath = args[4];

                                DetermineBckup(actionSwitch, argsLength, args);

                                RepackTypeB.RepackSingle(gameCode, filelistFile, whiteBinFile, whitePath, logWriter);
                                break;

                            case ActionSwitch.rmf:
                                CheckArguments(argsLength, 5, actionSwitch);

                                filelistFile = args[2];
                                whiteBinFile = args[3];
                                unpackedDir = args[4];

                                DetermineBckup(actionSwitch, argsLength, args);

                                RepackTypeC.RepackMultiple(gameCode, filelistFile, whiteBinFile, unpackedDir, logWriter);
                                break;

                            case ActionSwitch.rfl:
                                CheckArguments(argsLength, 3, actionSwitch);

                                var unpackedFilelistDir = args[2];

                                DetermineBckup(actionSwitch, argsLength, args);

                                RepackTypeD.RepackFilelist(gameCode, unpackedFilelistDir, logWriter);
                                break;

                            case ActionSwitch.cjf:
                                CheckArguments(argsLength, 3, actionSwitch);

                                var jsonFile = args[2];

                                DetermineBckup(actionSwitch, argsLength, args);

                                RepackTypeE.RepackJsonFilelist(gameCode, jsonFile, logWriter);
                                break;

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
                SharedFunctions.IfFileExistsDel("CrashLog.txt");

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


        private enum ActionSwitch
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


        private static void CheckArguments(int totalLength, int requiredLength, ActionSwitch actionSwitch)
        {
            if (totalLength < requiredLength)
            {
                SharedFunctions.ErrorExit($"Error: Specified action '{actionSwitch}' requires one or more arguments");
            }
        }


        private static void DetermineBckup(ActionSwitch actionSwitch, int argsLength, string[] args)
        {
            switch (actionSwitch)
            {
                case ActionSwitch.r:
                    if (argsLength > 4)
                    {
                        if (args[4] == "-bak")
                        {
                            ShouldBckup = true;
                        }
                    }
                    break;

                case ActionSwitch.raf:
                case ActionSwitch.rmf:
                    if (argsLength > 5)
                    {
                        if (args[5] == "-bak")
                        {
                            ShouldBckup = true;
                        }
                    }
                    break;

                case ActionSwitch.rfl:
                case ActionSwitch.cjf:
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
using System;

namespace WhiteBinTools
{
    internal class Help
    {
        public static void ShowCommands()
        {
            Console.WriteLine("Game Codes:");
            Console.WriteLine("-ff131 = 13-1 and Dirge Of Cerberus");
            Console.WriteLine("-ff132 = 13-2 and 13-LR");
            Console.WriteLine("");
            Console.WriteLine("");

            Console.WriteLine("Tool actions:");
            Console.WriteLine("-u = Unpack a bin file");
            Console.WriteLine("-r = Repack a bin file");
            Console.WriteLine("");
            Console.WriteLine("-uaf = Unpack a single file from the bin file");
            Console.WriteLine("-umf = Unpack files in a specific directory from the bin file");
            Console.WriteLine("-ufl = Unpack filelist file");
            Console.WriteLine("-ufp = Unpack filepaths from the filelist");
            Console.WriteLine("");
            Console.WriteLine("-raf = Repack a single file into the bin file");
            Console.WriteLine("-rmf = Repack multiple files into the bin file");
            Console.WriteLine("-rfl = Repack filelist file");
            Console.WriteLine("");
            Console.WriteLine("-cfj = Convert filelist to Json file");
            Console.WriteLine("-cjf = convert Json file to filelist file");
            Console.WriteLine("");
            Console.WriteLine("-? or -h = Display this help page");

            Console.WriteLine("");
            Console.WriteLine("");

            Console.WriteLine("Examples with 13-1 game code:");
            Console.WriteLine("To unpack: WhiteBinTools -ff131 -u " + @"""filelist.bin""" + @" ""whitebin.bin""");
            Console.WriteLine("To repack: WhiteBinTools -ff131 -r " + @"""filelist.bin""" + @" ""unpacked_folder""");
            Console.WriteLine("");
            Console.WriteLine("To unpack a single file: WhiteBinTools -ff131 -uaf " + @"""filelist.bin""" + @" ""whitebin.bin""" + @" ""chr\pc\c201\bin\c201.win32.trb""");
            Console.WriteLine("To unpack multiple files: WhiteBinTools -ff131 -umf " + @"""filelist.bin""" + @" ""whitebin.bin""" + @" ""chr\pc\*""");
            Console.WriteLine("To unpack filelist file: WhiteBinTools -ff131 -ufl " + @"""filelist.bin""");
            Console.WriteLine("To unpack filepath chunks: WhiteBinTools -ff131 -ufp " + @"""filelist.bin""");
            Console.WriteLine("");
            Console.WriteLine("To repack a single file: WhiteBinTools -ff131 -raf " + @"""filelist.bin """ + @" ""whitebin.bin""" + @" ""chr\pc\c201\bin\c201.win32.trb""");
            Console.WriteLine("To repack multiple files: WhiteBinTools -ff131 -rmf " + @"""filelist.bin""" + @" ""whitebin.bin""" + @" ""unpacked_folder""");
            Console.WriteLine("To repack filelist file: WhiteBinTools -ff131 -rfl " + @"""unpacked_filelist_folder""");
            Console.WriteLine("");
            Console.WriteLine("To convert filelist to Json file: WhiteBinTools -ff131 -cfj " + @"""filelist.bin""");
            Console.WriteLine("To convert Json file to filelist: WhiteBinTools -ff131 -cjf " + @"""filelist.bin.json""");

            Console.WriteLine("");
            Console.WriteLine("");

            Console.WriteLine("Exit codes:");
            Console.WriteLine("Code 0: Process was successful");
            Console.WriteLine("Code 1: One or more specified arguments were invalid");
            Console.WriteLine("Code 2: An exception has occured with the program");
            Console.WriteLine("");

            Environment.Exit(0);
        }
    }
}
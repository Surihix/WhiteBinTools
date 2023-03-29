using System;

namespace WhiteBinTools
{
    internal class Help
    {
        public static void ShowCommands()
        {
            Console.WriteLine("Game Codes:");
            Console.WriteLine("-ff131 = 13-1");
            Console.WriteLine("-ff132 = 13-2 and 13-LR");
            Console.WriteLine("");
            Console.WriteLine("Tool actions:");
            Console.WriteLine("-u = Unpack a bin file");
            Console.WriteLine("-r = Repack a bin file");
            Console.WriteLine("-f = Unpack file paths from filelist");
            Console.WriteLine("-uf = Unpack a single file from the bin file");
            Console.WriteLine("-rf = Repack a single file into the bin file");
            Console.WriteLine("-rfm = Repack multiple files into the bin file");
            Console.WriteLine("-? or -h = Display this help page");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Examples with 13-1 game code:");
            Console.WriteLine("To unpack: WhiteBinTools -ff131 -u " + @"""filelist.bin""" + @" ""whitebin.bin""");
            Console.WriteLine("To repack: WhiteBinTools -ff131 -r " + @"""filelist.bin""" + @" ""unpacked_folder""");
            Console.WriteLine("");
            Console.WriteLine("To unpack file paths: WhiteBinTools -ff131 -f " + @"""filelist.bin""");
            Console.WriteLine("");
            Console.WriteLine("To unpack a sinlge file: WhiteBinTools -ff131 -uf " + @"""filelist.bin""" + @" ""whitebin.bin"""
                + @" ""chr\pc\c201\bin\c201.win32.trb""");
            Console.WriteLine("To repack a sinlge file: WhiteBinTools -ff131 -rf " + @"""filelist.bin """ +
                @" ""unpacked_folder""" + @" ""chr\pc\c201\bin\c201.win32.trb""");
            Console.WriteLine("To repack multiple files: WhiteBinTools -ff131 -rfm " + @"""filelist.bin""" + @" ""whitebin.bin"""
                + @" ""unpacked_folder""");
            Console.WriteLine("");
            Console.WriteLine("To show help window:");
            Console.WriteLine("WhiteBinTools -?");
            Console.WriteLine("WhiteBinTools -h");
            Environment.Exit(0);
        }
    }
}
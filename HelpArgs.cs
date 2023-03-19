using System;

namespace WhiteBinTools
{
    internal class HelpArgs
    {
        public static void HelpMsgs()
        {
            Console.WriteLine("Game Codes:");
            Console.WriteLine("1 = 13-1");
            Console.WriteLine("2 = 13-2 and 13-LR");
            Console.WriteLine("");
            Console.WriteLine("Tool actions:");
            Console.WriteLine("-u = Unpack a bin file");
            Console.WriteLine("-r = Repack a bin file");
            Console.WriteLine("-uf = Unpack a single file from the bin file");
            Console.WriteLine("-rf = Repack a single file into the bin file");
            Console.WriteLine("-rfm = Repack multiple files into the bin file");
            Console.WriteLine("-f = Unpack file paths from filelist");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Examples with 13-[1] game code:");
            Console.WriteLine("To unpack: WhiteBinTools 1 -u " + @"""filelist.bin""" + @" ""whitebin.bin""");
            Console.WriteLine("To repack: WhiteBinTools 1 -r " + @"""filelist.bin""" + @" ""unpacked_folder_name""");
            Console.WriteLine("");
            Console.WriteLine("To unpack a sinlge file: WhiteBinTools 1 -uf " + @"""filelist.bin""" + @" ""whitebin.bin"""
                + @" ""chr\pc\c201\bin\c201.win32.trb""");
            Console.WriteLine("To repack a sinlge file: WhiteBinTools 1 -rf " + @"""filelist.bin """ +
                @" ""unpacked_folder_name""" + @" ""chr\pc\c201\bin\c201.win32.trb""");
            Console.WriteLine("To repack multiple files: WhiteBinTools 1 -rfm " + @"""filelist.bin""" + @" ""whitebin.bin"""
                + @" ""unpacked_folder_name""");
            Console.WriteLine("");
            Console.WriteLine("To unpack file paths: WhiteBinTools 1 -f " + @"""filelist.bin""");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Filelist file and the white bin file or the unpacked folder has to be specified");
            Console.WriteLine("after the game code and the action arguments");
            Console.WriteLine("");
            Console.WriteLine("If you want to unpack or repack a single file, then provide the virtual");
            Console.WriteLine("file path of that file after the white bin file or the unpacked folder argument");
            Console.WriteLine("");
            Console.WriteLine("If you want repack multiple files from the unpacked folder, then provide the");
            Console.WriteLine("unpacked folder as the last argument");
            Console.WriteLine("");
            Console.WriteLine("The single file and the multiple files repacking options will inject the file");
            Console.WriteLine("at the original position in the archive or append the file at the end depending");
            Console.WriteLine("on whether the compressed size or the file size when its in the archive, is equal");
            Console.WriteLine("or less than the size of the file that is being replaced.");

            Environment.Exit(0);
        }
    }
}
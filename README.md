# WhiteBinTools
This program allows you to unpack and repack the main white_img archive files from the FF13 game trilogy as well as the KEL.DAT archive from Dirge of Cerberus. the program should be launched from command prompt with a few argument switches to perform a function. the list of valid argument switches are given below:

**Game Codes:**
<br>``-ff131`` For FF13-1 and Dirge Of Cerberus files
<br>``-ff132`` For FF13-2 and FF13-LR 's files


<br>**Tool actions:**
<br>``-u`` Unpack a bin file
<br>``-r`` Repack a bin file
<br>``-uaf`` Unpack a single file from the bin file
<br>``-umf`` Unpacks files in a specific directory from the bin file. (refer the example given in the program's Help page)
<br>``-ufl`` Unpack filelist file
<br>``-ufc`` Unpack filepath chunks from filelist
<br>``-raf`` Repack a single file into the bin file
<br>``-rmf`` Repack multiple files into the bin file
<br>``-rfl`` Repack filelist file
<br>``cfj`` Convert filelist to Json file
<br>``cjf`` convert Json file to filelist file
<br>``-?`` or ``-h`` Display the help page
<br>

## Important notes
- Filelist file and the white bin file or the unpacked folder, has to be specified after the game code and the tool action argument switches.
- The game code switch determines how this app handles the filelist and white bin file during unpacking and repacking. this is very important during repacking and if an incorrect game code switch is specified, then the filelist and white bin file will not be repacked correctly.
- If you want to unpack or repack a single file, then provide the virtual file path of that file after the white bin file or the unpacked folder argument. refer to the app's help page that can be accessed with the `-?` or `-h` switches. 
- The single file and the multiple files repacking options will either inject the file at the original position in the archive or append the file at the end of the archive. the file will be injected at the original position if the compressed data size (i.e if its stored compressed) or the file size (i.e if not stored compressed), is lesser than or equal to the size of the file that is being replaced. if its greater than the original size, then the file is appended at the end of the archive.
- When repacking the Json file back to filelist, ensure that the structure of the text data in the Json file is similar to what it was when dumped by the `cfj` function switch.
- An optional `-bak` switch can be specified to backup the filelist and white_img files, when using any of the repack functions.

## For developers
- The following package's Zlib classes were used for Zlib compression and decompression:
<br>**DotNetZip** - https://github.com/haf/DotNetZip.Semverd

- Refer to this [page](https://github.com/LR-Research-Team/Datalog/wiki/White-Image-BIN-files) for information about the file structures of the filelist and the archive.
- The functions of this program are ported to this [reference project](https://github.com/Surihix/WhiteBinTools_dll) which you can compile as a dll file or directly use the dll file from the Releases section in your C# projects.

## Special Thanks
[**Kizari**](https://github.com/Kizari)

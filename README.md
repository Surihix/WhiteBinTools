# WhiteBinTools
This app allows you to unpack and repack the main white_img archive files from the FF13 game trilogy as well as the KEL.DAT archive from Dirge of Cerberus. the app should be launched from command prompt with a few argument switches to perform a function. the list of valid argument switches are given below:

**Game Codes:**
<br>``-ff131`` For FF13-1 and Dirge Of Cerberus files
<br>``-ff132`` For FF13-2 and FF13-LR 's files


<br>**Tool actions:**
<br>``-u`` Unpack a bin file
<br>``-r`` Repack a bin file
<br>``-ufp`` Unpack file paths from filelist
<br>``-uaf`` Unpack a single file from the bin file
<br>``-raf`` Repack a single file into the bin file
<br>``-rmf`` Repack multiple files into the bin file
<br>``-?`` or ``-h`` Display the help page
<br>

## Important notes
- Filelist file and the white bin file or the unpacked folder, has to be specified after the game code and the tool action argument switches.
- The ffxiiicrypt tool that is bundled with this app is required for unpacking and repacking the archive files from FF13-2 and FF13-LR.
- If you want to unpack or repack a single file, then provide the virtual file path of that file after the white bin file or the unpacked folder argument. refer to the app's help page that can be accessed with the `-?` or `-h` switches. 
- The single file and the multiple files repacking options will either inject the file at the original position in the archive or append the file at the end of the archive. the file will be injected into the archive if the compressed data size (i.e if its stored compressed) or the file size (i.e if not stored compressed), is lesser than or equal to the size of the file that is being replaced. if its greater than the original size, then its appended at the end of the archive.

## For developers
The following additional packages were used for Big Endian reading and writing byte values:
<br>**System.Memory** - https://www.nuget.org/packages/System.Memory/
<br>**System.Buffers** - https://www.nuget.org/packages/System.Buffers/
<br>
<br>
The following additional package is used for Zlib compression and decompression:
<br>**DotNetZip** - https://www.nuget.org/packages/DotNetZip
<br>
<br>
Refer to this [page](https://github.com/LR-Research-Team/Datalog/wiki/White-Image-BIN-files) for information about the file structures of the filelist and the archive.

## Credits
**ffxiiicrypt** - Echelo (from Xentax)

## Special Thanks
[**Kizari**](https://github.com/Kizari)

# WhiteBinTools
This app allows you to unpack and repack the main white_img archive files from the FF13 game trilogy. the app has to be run from a command prompt with a valid argument switch. the list of valid switches that you can use with this app are given below. 

**Game Codes:**
<br>``-ff131`` For 13-1's files
<br>``-ff132`` For 13-2 and 13-LR's files


<br>**Tool actions:**
<br>``-u`` Unpack a bin file
<br>``-r`` Repack a bin file
<br>``-ufp`` Unpack file paths from filelist
<br>``-uaf`` Unpack a single file from the bin file
<br>``-raf`` Repack a single file into the bin file
<br>``-rmf`` Repack multiple files into the bin file
<br>``-?`` or ``-h`` Display the help page
<br>

<br>* Filelist file and the white bin file or the unpacked folder has to be specified after the game code and the action argument switches.
<br>* If you want to unpack or repack a single file, then provide the virtual file path of that file after the white bin file or the unpacked folder argument.
<br>* If you want repack multiple files from the unpacked folder, then provide the unpacked folder as the last argument.
<br>* The single file and the multiple files repacking options will inject the file at the original position in the archive or append the file at the end depending on whether the compressed size or the file size when its in the archive, is equal or less than the size of the file that is being replaced.
<br>
<br>**Important:** The ffxiiicrypt tool that is bundled with this app is required for unpacking and repacking the archive files from FF13-2 and FF13-LR. 
<br>The author of this ffxiiicrypt tool is Echelo from Xentax.

## For developers
The following additional packages were used for Big Endian reading and writing byte values:
<br>**System.Memory** - https://www.nuget.org/packages/System.Memory/
<br>**System.Buffers** - https://www.nuget.org/packages/System.Buffers/
<br>
<br>
The following additional package is used for Zlib compression and decompression:
<br>**DotNetZip** - https://www.nuget.org/packages/DotNetZip

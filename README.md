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

- The game code switch determines how this app handles the filelist and white bin file during unpacking and repacking. this is very important and if an incorrect game code switch is specified, then the program will crash when processing the filelist file.

- If you want to unpack or repack a single file, then provide the virtual file path of that file after the white bin file or the unpacked folder argument. refer to the app's help page that can be accessed with the `-?` or `-h` switches. 

- An optional `-bak` switch can be specified to backup the filelist and white_img files, when using any of the repack functions.

- The single file and the multiple files repacking options will either inject the file at the original position in the white_img archive or append the file at the end of the archive.
  1. The file will be injected at the original position, if the compressed data size (i.e if its stored compressed) or the file size (i.e if not stored compressed), is lesser than or equal to the original size of the file, that is being replaced.
  2. The file will be appended to the end of the archive, if the compressed data size (i.e if its stored compressed) or the file size (i.e if not stored compressed), is greater than the original size of the file, that is being replaced. the original file's data in the archive, will be overwritten by null bytes, thereby cleaning up the original file's data.

- The `-ufl` function switch, will dump the filelist's data into multiple Chunk_ text files inside the unpacked folder. a "~Counts.txt" file will also be created inside the unpacked folder and would contain the filecount and the chunk count values. an "EncryptionHeader_(DON'T DELETE)" file will be created, if the filelist was encrypted. the presence of this file will determine whether the program should encrypt the filelist, when the `-rfl` function switch is used.
  <br>According to the game code specified, the structure of the text data inside the Chunk_ text files will be as follows:
  1. If the game code is set to `-ff131`, then this would be the structure:
    <br> ` FileCode | Chunk Number | Virtual Path data `

  2. If the game code is set to `-ff132`, then this would be the structure:
    <br> ` FileCode | Chunk Number | Unk Value | Virtual Path data `
    <br> The Chunk Number value will increase only after two Chunk_## text files. so for example, if the Chunk Number is `0` in "Chunk_0.txt" file, then the number will increase to `1` in "Chunk_2.txt" file. this only applies when the gamecode is set to `-ff132`. 

- When repacking the Json file back to filelist, ensure that the structure of the text data in the Json file is similar to what it was when dumped by the `cfj` function switch.

## For developers
- The following package's Zlib classes were used for Zlib compression and decompression:
<br>**DotNetZip** - https://github.com/haf/DotNetZip.Semverd

- Refer to this [page](https://github.com/LR-Research-Team/Datalog/wiki/White-Image-BIN-files) for information about the structure of the filelist file.
- The functions of this program can also be used in C# based projects via this [reference library](https://github.com/Surihix/WhiteBinTools_dll).

## Special Thanks
[**Kizari**](https://github.com/Kizari)

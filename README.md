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

- The game code switch determines how this app handles the filelist and white bin file during the unpacking and repacking functions. this is very important and if an incorrect game code switch is specified, then the program will crash when processing the filelist file.

- If you want to unpack or repack a single file, then provide the virtual file path of that file after the white bin file or the unpacked folder argument. refer to the app's help page, which can be accessed with the `-?` or `-h` switches. 

- An optional `-bak` switch can be specified to backup the filelist and white_img files, when using any of the repack functions.

- The single file and the multiple files repacking options will either inject the file at the original position in the white_img archive or append the file at the end of the archive.
  1. The file will be injected at the original position, if the compressed data size (i.e if its stored compressed) or the file size (i.e if not stored compressed), is lesser than or equal to the original size of the file, that is being replaced.
  2. The file will be appended to the end of the archive, if the compressed data size (i.e if its stored compressed) or the file size (i.e if not stored compressed), is greater than the original size of the file, that is being replaced. the original file's data in the archive, will be overwritten by null bytes, thereby cleaning up the original file's data.

- The `-ufl` function switch, will dump the filelist's data into multiple Chunk_ text files inside the unpacked folder. a "#info.txt" file containing some information about the filelist, will also be created inside the unpacked folder. the values in this file will determine whether how the program should pack the filelist, when the `-rfl` function switch is used.
  1. If the game code is set to `-ff131`, then this would be the structure of the text data inside the Chunk_ text files:
    <br>` 2826572288|631a7:1b4867:1b4867:sound/pack/8000/usa/music_white_e3.win32.scd `

  2. If the game code is set to `-ff132`, then this would be the structure of the text data inside the Chunk_ text files:
    <br>` 142217850|160|24617:92e100:92e100:sound/pack/8000/usa/music_Yasha.win32.scd `

- The `-cfj` function switch, will dump the filelist's data into a single Json file.
  1. If the game code is set to `-ff131`, then this would be the structure of each file entry object in the Json file:
     ```json
     {
       "fileCode": 2826572288,
       "filePath": "631a7:1b4867:1b4867:sound/pack/8000/usa/music_white_e3.win32.scd"
     }
     ```  
  2. If the game code is set to `-ff132`, then this would be the structure of each file entry object in the Json file:
     ```json
     { 
       "fileCode": 142217850, 
       "fileTypeID": 160,
       "filePath": "24617:92e100:92e100:sound/pack/8000/usa/music_Yasha.win32.scd"
     }
     ```

- When repacking the Json file back to filelist, ensure that the structure of the text data in the Json file is similar to what it was when dumped by the `-cfj` function switch. I strongly recommend using VS code to edit the Json file as other text editors might add non string character bytes in between two lines, which in turn will cause this program to throw errors when it tries repacking the filelist from the Json file.

## For developers
- The following package's Zlib classes were used for Zlib compression and decompression:
<br>**DotNetZip** - https://github.com/haf/DotNetZip.Semverd

- Refer to this [page](https://github.com/LR-Research-Team/Datalog/wiki/White-Image-BIN-files) for information about the structure of the filelist file.
- The functions of this program can also be used in C# based projects via this [reference library](https://github.com/Surihix/WhiteBinTools_dll).

## Special Thanks
[**Kizari**](https://github.com/Kizari)

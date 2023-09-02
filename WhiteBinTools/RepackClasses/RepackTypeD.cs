using System.IO;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.RepackClasses;
using WhiteBinTools.SupportClasses;

namespace WhiteBinTools
{
    internal class RepackTypeD
    {
        public static void RepackFilelist(string filelistFileVar, string extractedFilelistDir, StreamWriter logWriter)
        {
            filelistFileVar.CheckFileExists(logWriter, "Error: Filelist file specified in the argument is missing");
            extractedFilelistDir.CheckDirExists(logWriter, "Error: Unpacked filelist directory specified in the argument is missing");
            var cryptTool = "ffxiiicrypt.exe";
            
            var filelistFileDir = Path.GetDirectoryName(filelistFileVar);
            var filelistFileName = Path.GetFileName(filelistFileVar);
            var outChunkFile = extractedFilelistDir + "\\Chunks\\Chunk_";
            var outEntriesFile = extractedFilelistDir + "\\Chunk_entries\\Chunk_";
            var encHeaderFile = extractedFilelistDir + "\\EncryptionHeader_(DON'T EDIT)";

            (extractedFilelistDir + "\\Chunks").CheckDirExists(logWriter, "Error: Unable to locate the unpacked 'Chunks' directory");
            (extractedFilelistDir + "\\Chunk_entries").CheckDirExists(logWriter, "Error: Unable to locate the unpacked 'Chunk_entries' directory");


            var filelistVariables = new FilelistProcesses();
            filelistVariables.TotalChunks = (uint)Directory.GetFiles(extractedFilelistDir + "\\Chunks", "*.txt", SearchOption.AllDirectories).Length;
            filelistVariables.IsEncrypted = FilelistProcesses.CheckIfEncrypted(filelistFileVar);
            uint encHeaderAdjustedOffset = 0;

            if (filelistVariables.IsEncrypted.Equals(true))
            {
                encHeaderAdjustedOffset += 32;
                encHeaderFile.CheckFileExists(logWriter, "Error: Unable to locate the 'EncryptionHeader_(DON'T EDIT)' file");
                cryptTool.CheckFileExists(logWriter, "Error: Unable to locate ffxiiicrypt tool in the main app folder to encrypt the filelist file");
            }

            var backupFilelistFile = Path.Combine(filelistFileDir, filelistFileName + ".bak");
            backupFilelistFile.IfFileExistsDel();
            File.Copy(filelistFileVar, backupFilelistFile);
            File.Delete(filelistFileVar);


            using (var newFilelist = new FileStream(filelistFileVar, FileMode.Append, FileAccess.Write))
            {
                if (filelistVariables.IsEncrypted.Equals(true))
                {
                    // Copy the encryption header file's data
                    // into the new filelist file
                    using (var encHeader = new FileStream(encHeaderFile, FileMode.Open, FileAccess.Read))
                    {
                        encHeader.Seek(0, SeekOrigin.Begin);
                        encHeader.CopyTo(newFilelist);
                    }
                }

                for (int b = 0; b < 12; b++)
                {
                    newFilelist.WriteByte(0);
                }

                filelistVariables.ChunkFNameCount = 0;
                for (int c = 0; c < filelistVariables.TotalChunks; c++)
                {
                    // Copy the entries file's data into
                    // the new filelist file
                    using (var entryFiles = new FileStream(outEntriesFile + filelistVariables.ChunkFNameCount + "_entries.bin", FileMode.Open, FileAccess.Read))
                    {
                        entryFiles.CopyTo(newFilelist);
                    }

                    filelistVariables.ChunkFNameCount++;
                }

                filelistVariables.ChunkInfoSectionOffset = (uint)newFilelist.Length;
                if (filelistVariables.IsEncrypted.Equals(true))
                {
                    filelistVariables.ChunkInfoSectionOffset -= 32;
                }

                for (int b = 0; b < filelistVariables.TotalChunks * 12; b++)
                {
                    newFilelist.WriteByte(0);
                }
            }


            // Compress and pack chunks into the new filelist
            // file and update the info offsets.
            using (var newFilelistChunks = new FileStream(filelistFileVar, FileMode.Append, FileAccess.Write, FileShare.Write))
            {
                using (var newChunksInfoWriterStream = new FileStream(filelistFileVar, FileMode.Open, FileAccess.Write, FileShare.Write))
                {
                    using (var newChunksInfoWriter = new BinaryWriter(newChunksInfoWriterStream))
                    {

                        filelistVariables.ChunkFNameCount = 0;
                        var chunkInfoWriterPos = filelistVariables.ChunkInfoSectionOffset;
                        var chunkCmpSize = (uint)0;
                        var chunkUncmpSize = (uint)0;
                        var chunkStartVal = (uint)0;
                        var appendAt = (uint)0;
                        for (int nc = 0; nc < filelistVariables.TotalChunks; nc++)
                        {
                            appendAt = (uint)newFilelistChunks.Length;
                            newFilelistChunks.Seek(appendAt, SeekOrigin.Begin);

                            if (nc.Equals(0))
                            {
                                filelistVariables.ChunkDataSectionOffset = appendAt;
                                if (filelistVariables.IsEncrypted.Equals(true))
                                {
                                    filelistVariables.ChunkDataSectionOffset -= 32;
                                }
                            }

                            chunkUncmpSize = (uint)new FileInfo(outChunkFile + filelistVariables.ChunkFNameCount + ".txt").Length;
                            var chunkCmpData = (outChunkFile + filelistVariables.ChunkFNameCount + ".txt").ZlibCompress();
                            newFilelistChunks.Write(chunkCmpData, 0, chunkCmpData.Length);
                            chunkCmpSize = (uint)chunkCmpData.Length;

                            newChunksInfoWriter.AdjustBytesUInt32(encHeaderAdjustedOffset + chunkInfoWriterPos, chunkUncmpSize, CmnEnums.Endianness.LittleEndian);
                            newChunksInfoWriter.AdjustBytesUInt32(encHeaderAdjustedOffset + chunkInfoWriterPos + 4, chunkCmpSize, CmnEnums.Endianness.LittleEndian);
                            newChunksInfoWriter.AdjustBytesUInt32(encHeaderAdjustedOffset + chunkInfoWriterPos + 8, chunkStartVal, CmnEnums.Endianness.LittleEndian);

                            var newChunkStartVal = chunkStartVal + chunkCmpSize;
                            chunkStartVal = newChunkStartVal;

                            chunkInfoWriterPos += 12;

                            filelistVariables.ChunkFNameCount++;
                        }
                    }
                }
            }


            // Update the header offsets
            using (var updateNewFilelist = new FileStream(filelistFileVar, FileMode.Open, FileAccess.Write))
            {
                using (var updateNewFilelistWriter = new BinaryWriter(updateNewFilelist))
                {
                    updateNewFilelistWriter.AdjustBytesUInt32(encHeaderAdjustedOffset + 0, filelistVariables.ChunkInfoSectionOffset, CmnEnums.Endianness.LittleEndian);
                    updateNewFilelistWriter.AdjustBytesUInt32(encHeaderAdjustedOffset + 4, filelistVariables.ChunkDataSectionOffset, CmnEnums.Endianness.LittleEndian);

                    using (var sr = new StreamReader(extractedFilelistDir + "\\FileCount.txt"))
                    {
                        filelistVariables.TotalFiles = uint.Parse(sr.ReadLine());
                    }

                    updateNewFilelistWriter.AdjustBytesUInt32(encHeaderAdjustedOffset + 8, filelistVariables.TotalFiles, CmnEnums.Endianness.LittleEndian);
                }
            }

            if (filelistVariables.IsEncrypted.Equals(true))
            {
                var repackVariables = new RepackProcesses();
                repackVariables.NewFilelistFile = filelistFileVar;
                FilelistProcesses.EncryptProcess(repackVariables, logWriter);
            }

            IOhelpers.LogMessage("\nFinished repacking " + filelistFileName, logWriter);
        }
    }
}
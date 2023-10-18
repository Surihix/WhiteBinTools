using System.IO;
using WhiteBinTools.FilelistClasses;
using WhiteBinTools.SupportClasses;

namespace WhiteBinTools
{
    internal class UnpackTypeD
    {
        public static void UnpackFilelist(CmnEnums.GameCodes gameCodeVar, string filelistFileVar, StreamWriter logWriter)
        {
            filelistFileVar.CheckFileExists(logWriter, "Error: Filelist file specified in the argument is missing");

            var filelistVariables = new FilelistProcesses();

            FilelistProcesses.PrepareFilelistVars(filelistVariables, filelistFileVar);

            var filelistOutName = Path.GetFileName(filelistFileVar);
            var extractedFilelistDir = filelistVariables.MainFilelistDirectory + "\\_" + filelistOutName;
            var outChunkFile = extractedFilelistDir + "\\Chunk_";

            extractedFilelistDir.IfDirExistsDel();
            Directory.CreateDirectory(extractedFilelistDir);


            FilelistProcesses.DecryptProcess(gameCodeVar, filelistVariables, logWriter);

            using (var filelistStream = new FileStream(filelistVariables.MainFilelistFile, FileMode.Open, FileAccess.Read))
            {
                using (var filelistReader = new BinaryReader(filelistStream))
                {
                    FilelistProcesses.GetFilelistOffsets(filelistReader, logWriter, filelistVariables);

                    if (filelistVariables.IsEncrypted)
                    {
                        using (var encHeader = new FileStream(extractedFilelistDir + "\\EncryptionHeader_(DON'T EDIT)", FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            filelistStream.ExtendedCopyTo(encHeader, 0, 32);
                        }
                    }

                    using (var countsStream = new StreamWriter(extractedFilelistDir + "\\~Counts.txt", true))
                    {
                        countsStream.WriteLine(filelistVariables.TotalFiles);
                        countsStream.WriteLine(filelistVariables.TotalChunks);
                    }


                    var entryReadPos = 12;
                    if (filelistVariables.IsEncrypted)
                    {
                        entryReadPos = 44;
                    }
                    var chunkReadPos = filelistVariables.ChunkInfoSectionOffset;
                    filelistVariables.ChunkFNameCount = 0;

                    for (int c = 0; c < filelistVariables.TotalChunks; c++)
                    {
                        var currentChunkFile = outChunkFile + $"{filelistVariables.ChunkFNameCount}.txt";

                        using (var chunkDataStream = new StreamWriter(currentChunkFile, true))
                        {
                            filelistReader.BaseStream.Position = chunkReadPos;
                            var unCompressedSize = filelistReader.ReadUInt32();
                            var compressedSize = filelistReader.ReadUInt32();
                            var chunkStart = filelistVariables.ChunkDataSectionOffset + filelistReader.ReadUInt32();

                            using (var cmpChunkStream = new MemoryStream())
                            {
                                using (var dcmpChunkStream = new MemoryStream())
                                {
                                    using (var dcmpChunkReader = new BinaryReader(dcmpChunkStream))
                                    {
                                        filelistStream.ExtendedCopyTo(cmpChunkStream, chunkStart, compressedSize);

                                        cmpChunkStream.Seek(0, SeekOrigin.Begin);
                                        cmpChunkStream.ZlibDecompress(dcmpChunkStream);

                                        dcmpChunkStream.Seek(0, SeekOrigin.Begin);

                                        uint currentPathReadPos = 0;
                                        while (currentPathReadPos != unCompressedSize)
                                        {
                                            var filePath = dcmpChunkReader.BinaryToString(currentPathReadPos);

                                            if (filePath.Equals("end"))
                                            {
                                                break;
                                            }

                                            filelistReader.BaseStream.Position = entryReadPos;
                                            var fileCode = filelistReader.ReadUInt32();
                                            ushort chunkNumber = 0;
                                            ushort unkVal = 0;

                                            switch (gameCodeVar)
                                            {
                                                case CmnEnums.GameCodes.ff131:
                                                    chunkNumber = filelistReader.ReadUInt16();
                                                    _ = filelistReader.ReadUInt16();
                                                    break;

                                                case CmnEnums.GameCodes.ff132:
                                                    _ = filelistReader.ReadUInt16();
                                                    chunkNumber = filelistReader.ReadByte();
                                                    unkVal = filelistReader.ReadByte();
                                                    break;
                                            }

                                            chunkDataStream.Write(fileCode + "|");
                                            chunkDataStream.Write(chunkNumber + "|");
                                            if (gameCodeVar.Equals(CmnEnums.GameCodes.ff132))
                                            {
                                                chunkDataStream.Write(unkVal + "|");
                                            }
                                            chunkDataStream.WriteLine(filePath);

                                            currentPathReadPos = (uint)dcmpChunkReader.BaseStream.Position;
                                            entryReadPos += 8;
                                        }
                                    }
                                }
                            }
                        }

                        chunkReadPos += 12;
                        filelistVariables.ChunkFNameCount++;
                    }
                }
            }

            filelistVariables.TmpDcryptFilelistFile.IfFileExistsDel();

            IOhelpers.LogMessage("\nFinished unpacking " + filelistOutName, logWriter);
        }
    }
}
using System.IO;
using static WhiteBinTools.Filelist.DirgePathHelpers.PathStructures;

namespace WhiteBinTools.Filelist.DirgePathHelpers.PathCategories
{
    internal class EventCategory
    {
        public static string ProcessEventPath(uint fileCode)
        {
            var bitsRemaining = 24;

            var evFolderNum = PathGenMethods.DeriveValueFromBitsAndMaskValue(ref fileCode, ref bitsRemaining, 12);
            var subTypeVal = PathGenMethods.DeriveValueFromBitsAndMaskValue(ref fileCode, ref bitsRemaining, 4);

            var evFolderNumPadded = PathGenMethods.GenerateFolderNameWithNumber("ev", evFolderNum, 4);

            uint index;

            switch (subTypeVal)
            {
                case 0:
                case 1:
                    var subTypeVal2 = PathGenMethods.DeriveValueFromBitsAndMaskValue(ref fileCode, ref bitsRemaining, 5);
                    index = PathGenMethods.DeriveValueFromBitsAndMaskValue(ref fileCode, ref bitsRemaining, 3);

                    var isEventSceneStrBinType0 = subTypeVal == 1 && subTypeVal2 == 0 && index < 8;
                    var isEventSceneStrBinType1 = subTypeVal == 1 && subTypeVal2 == 1 && index < 8;
                    var isEventSceneClassType0 = subTypeVal == 0 && subTypeVal2 == 0 && index < 8;
                    var isEventSceneClassType1 = subTypeVal == 0 && subTypeVal2 == 1 && index < 8;
                    var isEventLocaleTxtBin = subTypeVal == 1 && subTypeVal2 == 25;

                    string generatedVPath;
                    string generatedFName;

                    if (isEventSceneStrBinType0)
                    {
                        generatedFName = PathGenMethods.ComputeFNameNum(index, "string", "bin");
                        generatedVPath = Path.Combine(EventSceneDir, evFolderNumPadded, generatedFName);
                        return generatedVPath;
                    }

                    if (isEventSceneStrBinType1)
                    {
                        generatedVPath = Path.Combine(EventSceneDir, evFolderNumPadded, "string08.bin");
                        return generatedVPath;
                    }

                    if (isEventSceneClassType0)
                    {
                        generatedFName = PathGenMethods.ComputeFNameNum(index, "scr0", "class");
                        generatedVPath = Path.Combine(EventSceneDir, evFolderNumPadded, generatedFName);
                        return generatedVPath;
                    }

                    if (isEventSceneClassType1)
                    {
                        generatedVPath = Path.Combine(EventSceneDir, evFolderNumPadded, "scr008.class");
                        return generatedVPath;
                    }

                    if (isEventLocaleTxtBin)
                    {
                        generatedFName = PathGenMethods.ComputeFNameLanguage(index, "string");
                        generatedVPath = Path.Combine(EventLocaleDir, evFolderNumPadded, generatedFName);
                        return generatedVPath;
                    }
                    break;

                case 2:
                case 4:
                    index = PathGenMethods.DeriveValueFromBitsAndMaskValue(ref fileCode, ref bitsRemaining, 8);

                    var isPtdBin = subTypeVal == 2;
                    var isEvmRfd = subTypeVal == 4;

                    if (isPtdBin)
                    {
                        generatedFName = PathGenMethods.GenerateFNameWithNumber("ptd", index, 3, ".bin");
                        generatedVPath = Path.Combine(EventSceneDir, evFolderNumPadded, generatedFName);
                        return generatedVPath;
                    }

                    if (isEvmRfd)
                    {
                        generatedFName = PathGenMethods.GenerateFNameWithNumber("evm", index, 3, ".rfd");
                        generatedVPath = Path.Combine(EventSceneDir, evFolderNumPadded, generatedFName);
                        return generatedVPath;
                    }
                    break;

                case 6:
                case 8:
                    index = PathGenMethods.DeriveValueFromBitsAndMaskValue(ref fileCode, ref bitsRemaining, 8);

                    var isTexRfd = subTypeVal == 6;
                    var isSepBin = subTypeVal == 8;

                    if (isTexRfd)
                    {
                        generatedFName = PathGenMethods.GenerateFNameWithNumber("tex", index, 3, ".rfd");
                        generatedVPath = Path.Combine(EventSceneDir, evFolderNumPadded, generatedFName);
                        return generatedVPath;
                    }

                    if (isSepBin)
                    {
                        if (index == 0)
                        {
                            generatedVPath = Path.Combine(EventSceneDir, evFolderNumPadded, "sep.bin");
                        }
                        else
                        {
                            generatedFName = PathGenMethods.GenerateFNameWithNumber("sep", index, 3, ".bin");
                            generatedVPath = Path.Combine(EventSceneDir, evFolderNumPadded, generatedFName);
                        }

                        return generatedVPath;
                    }
                    break;

                case 9:
                    index = PathGenMethods.DeriveValueFromBitsAndMaskValue(ref fileCode, ref bitsRemaining, 8);

                    generatedVPath = Path.Combine(EventSceneDir, evFolderNumPadded, "evtvib.bin");
                    return generatedVPath;
            }

            return string.Empty;
        }
    }
}
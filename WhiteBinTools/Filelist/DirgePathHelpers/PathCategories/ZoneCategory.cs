using System.IO;
using static WhiteBinTools.Filelist.DirgePathHelpers.PathStructures;

namespace WhiteBinTools.Filelist.DirgePathHelpers.PathCategories
{
    internal class ZoneCategory
    {
        public static string ProcessZonePath(uint fileCode, uint mainTypeVal)
        {
            var bitsRemaining = 24;

            var ZoneDirType = mainTypeVal;
            var zFolderNum = PathGenMethods.DeriveValueFromBitsAndMaskValue(ref fileCode, ref bitsRemaining, 8);
            var zFolderNumPadded = PathGenMethods.GenerateFolderNameWithNumber("z", zFolderNum, 3);

            uint index;

            string generatedVPath;

            if (ZoneDirType == 10)
            {
                // Assume file is st/ss####.bin
                index = PathGenMethods.DeriveValueFromBitsAndMaskValue(ref fileCode, ref bitsRemaining, 16);
                var ssNum = index.ToString("x").PadLeft(4, '0');

                generatedVPath = Path.Combine(ZoneDir, zFolderNumPadded, "st", $"ss{ssNum}.bin");
                return generatedVPath;
            }
            else
            {
                var dependantVal = PathGenMethods.DeriveValueFromBitsAndMaskValue(ref fileCode, ref bitsRemaining, 8);

                switch (dependantVal)
                {
                    // Assume the filepath
                    // is a map/mm##.bin
                    case 253:
                        index = PathGenMethods.DeriveValueFromBitsAndMaskValue(ref fileCode, ref bitsRemaining, 8);
                        var mmName = PathGenMethods.GenerateFNameWithNumber("mm", index, 2, ".bin");

                        generatedVPath = Path.Combine(ZoneDir, zFolderNumPadded, "map", mmName);

                        if (generatedVPath != string.Empty)
                        {
                            return generatedVPath;
                        }
                        break;

                    // Assume the filepath
                    // is a m255/##.txd
                    case 255:
                        index = PathGenMethods.DeriveValueFromBitsAndMaskValue(ref fileCode, ref bitsRemaining, 8);
                        var txdNum = index.ToString("x").PadLeft(2, '0');
                        string txdDir = "0";

                        if (index >= 0x00 && index <= 0x3f)
                        {
                            txdDir = "0";
                        }

                        if (index >= 0x40 && index <= 0x7f)
                        {
                            txdDir = "1";
                        }

                        if (index >= 0x80 && index <= 0xbf)
                        {
                            txdDir = "2";
                        }

                        if (index >= 0xc0 && index <= 0xff)
                        {
                            txdDir = "3";
                        }

                        generatedVPath = Path.Combine(ZoneDir, zFolderNumPadded, "m255", txdDir, txdNum + ".txd");

                        if (generatedVPath != string.Empty)
                        {
                            return generatedVPath;
                        }
                        break;

                    default:
                        var subTypeVal = PathGenMethods.DeriveValueFromBitsAndMaskValue(ref fileCode, ref bitsRemaining, 4);
                        index = PathGenMethods.DeriveValueFromBitsAndMaskValue(ref fileCode, ref bitsRemaining, 4);

                        var isZoneCnf = dependantVal == 0 && subTypeVal == 0 && index == 0;
                        var isZoneBzdBin = dependantVal == 0 && subTypeVal == 0 && index == 1;
                        var isZoneClass = dependantVal == 0 && subTypeVal == 0 && index == 2;
                        var isZoneStrBin = dependantVal == 0 && subTypeVal == 0 && index == 3;
                        var isZoneBrdBin = dependantVal == 0 && subTypeVal == 0 && index == 4;
                        var isZoneSepBin = dependantVal == 0 && subTypeVal == 0 && index == 5;
                        var isZoneShpBin = dependantVal == 0 && subTypeVal == 0 && index == 6;
                        var isZoneSdbBin = dependantVal == 0 && subTypeVal == 0 && index == 8;
                        var isZoneBmdSetBin = dependantVal == 0 && subTypeVal == 0 && index == 9;

                        var isZoneAnmKfd = subTypeVal == 1 && index == 0;
                        var isZoneMapidRfd = subTypeVal == 1 && index == 1;
                        var isZoneModelRfd = subTypeVal == 1 && index == 2;
                        var isZoneEffectFed = subTypeVal == 1 && index == 4;

                        var isZoneLocaleStrBin = dependantVal == 0 && subTypeVal == 2 && index >= 0 && index <= 6;

                        string generatedDirName = string.Empty;

                        if (subTypeVal == 1 && index != 4)
                        {
                            generatedDirName = PathGenMethods.GenerateFolderNameWithNumber("m", dependantVal, 3);
                        }

                        if (isZoneCnf)
                        {
                            generatedVPath = Path.Combine(ZoneDir, zFolderNumPadded, "zone.cnf");
                            return generatedVPath;
                        }

                        if (isZoneBzdBin)
                        {
                            generatedVPath = Path.Combine(ZoneDir, zFolderNumPadded, "bzd.bin");
                            return generatedVPath;
                        }

                        if (isZoneClass)
                        {
                            generatedVPath = Path.Combine(ZoneDir, zFolderNumPadded, "gmap.class");
                            return generatedVPath;
                        }

                        if (isZoneStrBin)
                        {
                            generatedVPath = Path.Combine(ZoneDir, zFolderNumPadded, "gmap_str.bin");
                            return generatedVPath;
                        }

                        if (isZoneBrdBin)
                        {
                            generatedVPath = Path.Combine(ZoneDir, zFolderNumPadded, "brd.bin");
                            return generatedVPath;
                        }

                        if (isZoneSepBin)
                        {
                            generatedVPath = Path.Combine(ZoneDir, zFolderNumPadded, "sound", "sep.bin");
                            return generatedVPath;
                        }

                        if (isZoneShpBin)
                        {
                            generatedVPath = Path.Combine(ZoneDir, zFolderNumPadded, "shp.bin");
                            return generatedVPath;
                        }

                        if (isZoneSdbBin)
                        {
                            generatedVPath = Path.Combine(ZoneDir, zFolderNumPadded, "sdb.bin");
                            return generatedVPath;
                        }

                        if (isZoneBmdSetBin)
                        {
                            if (PathGenMethods.IsZoneBmdBin)
                            {
                                // bmd.bin
                                PathGenMethods.IsZoneBmdBin = false;
                                generatedVPath = Path.Combine("data", "bmd", "bmd.bin");
                                return generatedVPath;
                            }
                            else
                            {
                                // bmd_off.bin
                                PathGenMethods.IsZoneBmdBin = true;
                                generatedVPath = Path.Combine("data", "bmd", "bmd_off.bin");
                                return generatedVPath;
                            }
                        }

                        if (isZoneAnmKfd)
                        {
                            generatedVPath = Path.Combine(ZoneDir, zFolderNumPadded, generatedDirName, "anm.kfd");
                            return generatedVPath;
                        }

                        if (isZoneMapidRfd)
                        {
                            generatedVPath = Path.Combine(ZoneDir, zFolderNumPadded, generatedDirName, "mapid.rfd");
                            return generatedVPath;
                        }

                        if (isZoneModelRfd)
                        {
                            generatedVPath = Path.Combine(ZoneDir, zFolderNumPadded, generatedDirName, "model.rfd");
                            return generatedVPath;
                        }

                        if (isZoneEffectFed)
                        {
                            zFolderNumPadded = PathGenMethods.GenerateFolderNameWithNumber("z", zFolderNum, 4);
                            generatedDirName = PathGenMethods.GenerateFolderNameWithNumber("f", dependantVal, 4);
                            generatedVPath = Path.Combine(ZoneEffectDir, zFolderNumPadded, generatedDirName, "fer", generatedDirName + ".fed");
                            return generatedVPath;
                        }

                        if (isZoneLocaleStrBin)
                        {
                            var generatedFName = PathGenMethods.ComputeFNameLanguage(index, "gmap_str");

                            generatedVPath = Path.Combine(ZoneLocaleDir, zFolderNumPadded, generatedFName);
                            return generatedVPath;
                        }

                        break;
                }
            }

            return string.Empty;
        }
    }
}
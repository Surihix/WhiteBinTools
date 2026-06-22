using System.IO;
using static WhiteBinTools.Filelist.DirgePathHelpers.PathStructures;

namespace WhiteBinTools.Filelist.DirgePathHelpers.PathCategories
{
    internal class ChrCategory
    {
        public static string ProcessChrPath(uint fileCode)
        {
            var bitsRemaining = 24;

            var subTypeVal = PathGenMethods.DeriveValueFromBitsAndMaskValue(ref fileCode, ref bitsRemaining, 8);
            var folderNumber = PathGenMethods.DeriveValueFromBitsAndMaskValue(ref fileCode, ref bitsRemaining, 10);
            var index = PathGenMethods.DeriveValueFromBitsAndMaskValue(ref fileCode, ref bitsRemaining, 6);

            var isMrfdInitialFile = index == 0;
            var isModelsRfd = index == 1;
            var isTxtrTxd = index == 2;
            var isEffectFed = index == 3;
            var isMrfdFileSet = index >= 10;

            var chrDirNumSet = string.Empty;

            if (folderNumber < 10)
            {
                chrDirNumSet = "00" + folderNumber.ToString();
            }

            if (folderNumber >= 10)
            {
                chrDirNumSet = "0" + folderNumber.ToString();
            }

            if (folderNumber >= 100)
            {
                chrDirNumSet = folderNumber.ToString();
            }

            chrDirNumSet = Path.Combine(chrDirNumSet[0].ToString(), chrDirNumSet[1].ToString(), chrDirNumSet[2].ToString());

            var idFolderRoot = string.Empty;
            if (folderNumber < 50)
            {
                idFolderRoot = "0000";
            }

            if (folderNumber >= 50 && folderNumber < 100)
            {
                idFolderRoot = "0050";
            }

            if (folderNumber >= 100 && folderNumber < 200)
            {
                idFolderRoot = "0100";
            }

            if (folderNumber >= 200 && folderNumber < 300)
            {
                idFolderRoot = "0200";
            }

            string generatedVPath;
            string id;

            switch (subTypeVal)
            {
                // chr/e/#/#/#/
                // effect/enemy/e####/e####/fer/e####.fed
                case 101:
                    if (isMrfdInitialFile)
                    {
                        generatedVPath = Path.Combine(ChrEdir, chrDirNumSet, "m000.rfd");
                        return generatedVPath;
                    }

                    if (isModelsRfd)
                    {
                        generatedVPath = Path.Combine(ChrEdir, chrDirNumSet, "models.rfd");
                        return generatedVPath;
                    }

                    if (isTxtrTxd)
                    {
                        generatedVPath = Path.Combine(ChrEdir, chrDirNumSet, "texture.txd");
                        return generatedVPath;
                    }

                    if (isEffectFed)
                    {
                        id = folderNumber.ToString().PadLeft(4, '0');
                        generatedVPath = Path.Combine(EffectEnemyDir, $"e{idFolderRoot}", $"e{id}", "fer", $"e{id}.fed");
                        return generatedVPath;
                    }

                    if (isMrfdFileSet)
                    {
                        id = (index - 10).ToString().PadLeft(3, '0');
                        generatedVPath = Path.Combine(ChrEdir, chrDirNumSet, $"m{id}.rfd");
                        return generatedVPath;
                    }
                    break;

                // chr/g/#/#/#/
                // effect/ground/g####/g####/fer/g####.fed
                case 103:
                    if (isMrfdInitialFile)
                    {
                        generatedVPath = Path.Combine(ChrGdir, chrDirNumSet, "m000.rfd");
                        return generatedVPath;
                    }

                    if (isModelsRfd)
                    {
                        generatedVPath = Path.Combine(ChrGdir, chrDirNumSet, "models.rfd");
                        return generatedVPath;
                    }

                    if (isTxtrTxd)
                    {
                        generatedVPath = Path.Combine(ChrGdir, chrDirNumSet, "texture.txd");
                        return generatedVPath;
                    }

                    if (isEffectFed)
                    {
                        id = folderNumber.ToString().PadLeft(4, '0');
                        generatedVPath = Path.Combine(EffectGroundDir, $"g{idFolderRoot}", $"g{id}", "fer", $"g{id}.fed");
                        return generatedVPath;
                    }

                    if (isMrfdFileSet)
                    {
                        id = (index - 10).ToString().PadLeft(3, '0');
                        generatedVPath = Path.Combine(ChrGdir, chrDirNumSet, $"m{id}.rfd");
                        return generatedVPath;
                    }
                    break;

                // chr/m/#/#/#/
                case 109:
                    if (isMrfdInitialFile)
                    {
                        generatedVPath = Path.Combine(ChrMdir, chrDirNumSet, "m000.rfd");
                        return generatedVPath;
                    }

                    if (isModelsRfd)
                    {
                        generatedVPath = Path.Combine(ChrMdir, chrDirNumSet, "models.rfd");
                        return generatedVPath;
                    }

                    if (isTxtrTxd)
                    {
                        generatedVPath = Path.Combine(ChrMdir, chrDirNumSet, "texture.txd");
                        return generatedVPath;
                    }

                    if (isMrfdFileSet)
                    {
                        id = (index - 10).ToString().PadLeft(3, '0');
                        generatedVPath = Path.Combine(ChrMdir, chrDirNumSet, $"m{id}.rfd");
                        return generatedVPath;
                    }
                    break;

                // chr/n/#/#/#/
                // effect/npc/n####/n####/fer/n####.fed
                case 110:
                    if (isMrfdInitialFile)
                    {
                        generatedVPath = Path.Combine(ChrNdir, chrDirNumSet, "m000.rfd");
                        return generatedVPath;
                    }

                    if (isModelsRfd)
                    {
                        generatedVPath = Path.Combine(ChrNdir, chrDirNumSet, "models.rfd");
                        return generatedVPath;
                    }

                    if (isTxtrTxd)
                    {
                        generatedVPath = Path.Combine(ChrNdir, chrDirNumSet, "texture.txd");
                        return generatedVPath;
                    }

                    if (isEffectFed)
                    {
                        id = folderNumber.ToString().PadLeft(4, '0');
                        generatedVPath = Path.Combine(EffectNpcDir, $"n{idFolderRoot}", $"n{id}", "fer", $"n{id}.fed");
                        return generatedVPath;
                    }

                    if (isMrfdFileSet)
                    {
                        id = (index - 10).ToString().PadLeft(3, '0');
                        generatedVPath = Path.Combine(ChrNdir, chrDirNumSet, $"m{id}.rfd");
                        return generatedVPath;
                    }
                    break;

                // chr/o/#/#/#/
                // effect/player/l####/l####/fer/l####.fed
                case 111:
                    if (isMrfdInitialFile)
                    {
                        generatedVPath = Path.Combine(ChrOdir, chrDirNumSet, "m000.rfd");
                        return generatedVPath;
                    }

                    if (isModelsRfd)
                    {
                        generatedVPath = Path.Combine(ChrOdir, chrDirNumSet, "models.rfd");
                        return generatedVPath;
                    }

                    if (isTxtrTxd)
                    {
                        generatedVPath = Path.Combine(ChrOdir, chrDirNumSet, "texture.txd");
                        return generatedVPath;
                    }

                    if (isEffectFed)
                    {
                        id = folderNumber.ToString().PadLeft(4, '0');
                        generatedVPath = Path.Combine(EffectLineDir, $"l{idFolderRoot}", $"l{id}", "fer", $"l{id}.fed");
                        return generatedVPath;
                    }

                    if (isMrfdFileSet)
                    {
                        id = (index - 10).ToString().PadLeft(3, '0');
                        generatedVPath = Path.Combine(ChrOdir, chrDirNumSet, $"m{id}.rfd");
                        return generatedVPath;
                    }
                    break;

                // chr/p/#/#/#/
                // effect/player/p####/p####/fer/p####.fed
                case 112:
                    if (isMrfdInitialFile)
                    {
                        generatedVPath = Path.Combine(ChrPdir, chrDirNumSet, "m000.rfd");
                        return generatedVPath;
                    }

                    if (isModelsRfd)
                    {
                        generatedVPath = Path.Combine(ChrPdir, chrDirNumSet, "models.rfd");
                        return generatedVPath;
                    }

                    if (isTxtrTxd)
                    {
                        generatedVPath = Path.Combine(ChrPdir, chrDirNumSet, "texture.txd");
                        return generatedVPath;
                    }

                    if (isEffectFed)
                    {
                        id = folderNumber.ToString().PadLeft(4, '0');
                        generatedVPath = Path.Combine(EffectPlayerDir, $"p{idFolderRoot}", $"p{id}", "fer", $"p{id}.fed");
                        return generatedVPath;
                    }

                    if (isMrfdFileSet)
                    {
                        id = (index - 10).ToString().PadLeft(3, '0');
                        generatedVPath = Path.Combine(ChrPdir, chrDirNumSet, $"m{id}.rfd");
                        return generatedVPath;
                    }
                    break;

                // chr/w/#/#/#/
                // effect/weapon/w####/w####/fer/w####.fed
                case 119:
                    if (isMrfdInitialFile)
                    {
                        generatedVPath = Path.Combine(ChrWdir, chrDirNumSet, "m000.rfd");
                        return generatedVPath;
                    }

                    if (isModelsRfd)
                    {
                        generatedVPath = Path.Combine(ChrWdir, chrDirNumSet, "models.rfd");
                        return generatedVPath;
                    }

                    if (isTxtrTxd)
                    {
                        generatedVPath = Path.Combine(ChrWdir, chrDirNumSet, "texture.txd");
                        return generatedVPath;
                    }

                    if (isEffectFed)
                    {
                        id = folderNumber.ToString().PadLeft(4, '0');
                        generatedVPath = Path.Combine(EffectWeaponDir, $"w{idFolderRoot}", $"w{id}", "fer", $"w{id}.fed");
                        return generatedVPath;
                    }

                    if (isMrfdFileSet)
                    {
                        id = (index - 10).ToString().PadLeft(3, '0');
                        generatedVPath = Path.Combine(ChrWdir, chrDirNumSet, $"m{id}.rfd");
                        return generatedVPath;
                    }
                    break;
            }

            return string.Empty;
        }
    }
}
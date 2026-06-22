using WhiteBinTools.Filelist.DirgePathHelpers.PathCategories;

namespace WhiteBinTools.Filelist.DirgePathHelpers
{
    internal class PathGenerator
    {
        public static string GenerateDirgePath(uint fileCode)
        {
            var mainTypeVal = fileCode >> 24;
            fileCode &= (1 << 24) - 1;

            string generatedPath;

            switch (mainTypeVal)
            {
                // data/zone
                // data/effect/field
                // data/bmd
                case 6:
                case 10:
                    generatedPath = ZoneCategory.ProcessZonePath(fileCode, mainTypeVal);
                    break;

                // data/chr/e
                // data/chr/g
                // data/chr/m
                // data/chr/n
                // data/chr/o
                // data/chr/p
                // data/chr/w
                // data/effect/enemy
                // data/effect/ground
                // data/effect/npc
                // data/effect/line
                // data/effect/player
                // data/effect/weapon
                case 7:
                    generatedPath = ChrCategory.ProcessChrPath(fileCode);
                    break;

                // data/event
                case 12:
                    generatedPath = EventCategory.ProcessEventPath(fileCode);
                    break;

                default:
                    generatedPath = string.Empty;
                    break;
            }

            return generatedPath;
        }
    }
}
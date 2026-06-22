namespace WhiteBinTools.Filelist.DirgePathHelpers
{
    internal class PathGenMethods
    {
        public static bool IsZoneBmdBin = true;
        public static uint DeriveValueFromBitsAndMaskValue(ref uint fileCode, ref int bitsRemaining, int bitAmount)
        {
            var derivedVal = fileCode >> (bitsRemaining - bitAmount);
            fileCode &= (uint)(1 << (bitsRemaining - bitAmount)) - 1;
            bitsRemaining -= bitAmount;

            return derivedVal;
        }

        public static string ComputeFNameLanguage(uint index, string fNamePattern)
        {
            var generatedFName = "";
            switch (index)
            {
                case 0:
                    generatedFName = fNamePattern + "_jp.bin";
                    break;

                case 1:
                    generatedFName = fNamePattern + "_us.bin";
                    break;

                case 2:
                    generatedFName = fNamePattern + "_uk.bin";
                    break;

                case 3:
                    generatedFName = fNamePattern + "_it.bin";
                    break;

                case 4:
                    generatedFName = fNamePattern + "_sp.bin";
                    break;

                case 5:
                    generatedFName = fNamePattern + "_fr.bin";
                    break;

                case 6:
                    generatedFName = fNamePattern + "_gr.bin";
                    break;
            }

            return generatedFName;
        }

        public static string ComputeFNameNum(uint index, string fNamePattern, string fExtension)
        {
            var generatedFName = "";
            switch (index)
            {
                case 0:
                    if (fExtension == "class")
                    {
                        generatedFName = fNamePattern + $"00.{fExtension}";
                    }
                    if (fExtension == "bin")
                    {
                        generatedFName = fNamePattern + $".{fExtension}";
                    }
                    break;

                case 1:
                    generatedFName = fNamePattern + $"01.{fExtension}";
                    break;

                case 2:
                    generatedFName = fNamePattern + $"02.{fExtension}";
                    break;

                case 3:
                    generatedFName = fNamePattern + $"03.{fExtension}";
                    break;

                case 4:
                    generatedFName = fNamePattern + $"04.{fExtension}";
                    break;

                case 5:
                    generatedFName = fNamePattern + $"05.{fExtension}";
                    break;

                case 6:
                    generatedFName = fNamePattern + $"06.{fExtension}";
                    break;

                case 7:
                    generatedFName = fNamePattern + $"07.{fExtension}";
                    break;
            }

            return generatedFName;
        }

        public static string GenerateFolderNameWithNumber(string fNamePattern, uint number, int padCount)
        {
            return fNamePattern + number.ToString().PadLeft(padCount, '0');
        }

        public static string GenerateFNameWithNumber(string fNamePattern, uint number, int padCount, string fExtn)
        {
            return fNamePattern + number.ToString().PadLeft(padCount, '0') + fExtn;
        }
    }
}
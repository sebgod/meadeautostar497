namespace ASCOM.Meade.net
{
    public static class StringExtensions
    {
        public static int ToInteger(this string str)
        {
            return int.Parse(str);
        }

        public static double ToDouble(this string str)
        {
            return double.Parse(str);
        }
    }
}
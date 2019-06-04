namespace ASCOM.Meade.net
{
    public static class StringExtensions
    {
        public static int ToInteger(this string str)
        {
            return int.Parse(str);
        }
    }
}
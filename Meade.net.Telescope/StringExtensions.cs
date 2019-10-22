namespace ASCOM.Meade.net
{
    public static class StringExtensions
    {
        public static int ToInteger(this string str)
        {
            return int.Parse(str);
        }

        //public static double ToDouble(this string str)
        //{
        //    return double.Parse(str);
        //}

        public static int Position(this string str, char find, int instance)
        {
            var currentInstance = 0;
            for (var i = 0; i < str.Length; i++)
            {
                if (str[i] == find)
                {
                    currentInstance++;
                    if (currentInstance == instance)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }
    }
}
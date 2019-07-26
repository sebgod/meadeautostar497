namespace ASCOM.Meade.net
{
    public static class DoubleExtensions
    {
        public static bool InRange(this double value, double low, double high)
        {
            if (value < low)
                return false;
            if (value > high)
                return false;
            return true;
        }
    }
}
namespace System
{
    static class DoubelExtensions
    {
        public static bool Between(this double value, double lower, double higher)
        {
            return value >= lower & value <= higher;
        }
    }
}

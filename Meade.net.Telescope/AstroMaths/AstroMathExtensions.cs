using System;
using ASCOM.Utilities;

namespace ASCOM.Meade.net.AstroMaths
{
    public static class AstroMathExtensions
    {
        public static double DegreesToRadians(this double degrees)
        {
            return (Math.PI / 180) * degrees;
        }

        public static double RadiansToDegrees(this double radians)
        {
            double degrees = (180 / Math.PI) * radians;
            return degrees;
        }

        public static double DateTimeToDecimalHours(this DateTime utcDateTime)
        {
            double sec = utcDateTime.Second;
            double min = utcDateTime.Minute;
            double hour = utcDateTime.Hour;

            var a = Math.Abs(sec) / 60;
            var b = (Math.Abs(min) + a) / 60;
            var c = Math.Abs(hour) + b;

            var d = c;

            if ((hour < 0) || (min < 0) || (sec < 0))
                d = -c;

            return d;
        }

        public static double UTtoGst( this DateTime utcDateTime)
        {
            Util util = new Util();

            var jd = util.DateUTCToJulian(utcDateTime) - 0.5;
            if ((jd % 1) <= 0.5)
                jd = Math.Floor(jd);
            else
                jd = Math.Floor(jd) + 0.5;

            var s = jd - 2451545.0;
            var t = s / 36525.0;
            var t0 = 6.697374558 + (2400.051336 * t) + (0.000025862 * (t * t));

            while (t0 < 0)
            {
                t0 += 24;
            }

            while (t0 >= 24)
            {
                t0 -= 24;
            }

            var ut = DateTimeToDecimalHours(utcDateTime);
            var a = ut * 1.002737909;

            var t1 = t0 + a;

            while (t1 < 0)
            {
                t1 += 24;
            }

            while (t1 >= 24)
            {
                t1 -= 24;
            }

            return t1;
        }
    }
}
using System;
using ASCOM.Utilities;

namespace ASCOM.Meade.net.AstroMaths
{
    public class AstroMaths : IAstroMaths
    {

        //returns the decimal hour angle for given right ascension on a given datetime for a given logitude.
        public double RightAscensionToHourAngle(DateTime utcDateTime, double longitude, double rightAscension)
        {
            //var ut = DateTimeToDecimalHours( utcDateTime);
            var gst = UTtoGst( utcDateTime);
            var lst = GsTtoLst( gst, longitude);
            var raHours = rightAscension;
            var h1 = lst - raHours;
            var h = h1;

            if (h < 0)
                h = h + 24;

            return h;
        }

        private double HourAngleToRightAscension(DateTime utcDateTime, double longitude, double hourAngle )
        {
            var gst = UTtoGst(utcDateTime);
            var lst = GsTtoLst( gst, longitude);
            var raHours = hourAngle;
            var h1 = lst - raHours;
            var h = h1;
            if (h1 < 0)
            {
                h += 24;
            }

            return h;
        }

        public EquatorialCoordinates ConvertHozToEq( DateTime utcDateTime, double latitude, double longitude, HorizonCoordinates altAz)
        {
            var az = DegreesToRadians(altAz.Azimuth);
            var alt = DegreesToRadians(altAz.Altitude);
            var lat = DegreesToRadians(latitude);
            
            var sinDec = Math.Sin(alt) * Math.Sin(lat) + Math.Cos(alt) * Math.Cos(lat) * Math.Cos(az);
            var dec = RadiansToDegrees(Math.Asin(sinDec));
            
            var y = -Math.Cos(alt) * Math.Cos(lat) * Math.Sin(az);
            var x = Math.Sin(alt) - Math.Sin(lat) * sinDec;
            var upperA = Math.Atan2(y,x);
            var upperB = RadiansToDegrees(upperA);

            var ha = upperB;

            if (upperB < 0)
            {
                ha += 360;
            }

            ha = ha / 15;

            EquatorialCoordinates equatorialCoordinates = new EquatorialCoordinates
            {
                RightAscension = HourAngleToRightAscension( utcDateTime, longitude, ha ),
                Declination = dec
            };

            return equatorialCoordinates;
        }

        public HorizonCoordinates ConvertEqToHoz(double hourAngle, double latitude, EquatorialCoordinates raDec)
        {
            var h = hourAngle * 15;
            var h1 = DegreesToRadians(h);
            var d = DegreesToRadians(raDec.Declination);
            var lat = DegreesToRadians(latitude);
            var sinA = Math.Sin(d) * Math.Sin(lat) + Math.Cos(d) * Math.Cos(lat) * Math.Cos(h1);

             var y = -Math.Cos(d) * Math.Cos(lat) * Math.Sin(h1);
            var x = Math.Sin(d) - Math.Sin(lat) * sinA;
            var upperA = Math.Atan2(y, x);
            var upperB = RadiansToDegrees(upperA);

            var horizonCoordinates = new HorizonCoordinates
            {
                Altitude = RadiansToDegrees(Math.Asin(sinA)), Azimuth = upperB
            };


            if (upperB < 0)
            {
                horizonCoordinates.Azimuth = 360 + horizonCoordinates.Azimuth;
            }
            
            return horizonCoordinates;
        }


        //todo convert to extension method
        public double DegreesToRadians(double degrees)
        {
            return (Math.PI / 180) * degrees;
        }

        //todo convert to extension method
        public double RadiansToDegrees(double radians)
        {
            double degrees = (180 / Math.PI) * radians;
            return degrees;
        }

        //todo convert to extension method
        public double DateTimeToDecimalHours( DateTime utcDateTime)
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

        //todo convert to extension method
        public double UTtoGst(DateTime utcDateTime)
        {
            Util util = new Util();

            var jd = util.DateUTCToJulian(utcDateTime) - 0.5;
            if ((jd % 1) <= 0.5 )
                jd = Math.Floor( jd );
            else
                jd = Math.Floor( jd ) + 0.5;

            var s = jd - 2451545.0;
            var t = s / 36525.0;
            var t0 = 6.697374558 + (2400.051336 * t ) +(0.000025862 * (t * t) );

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

        public double GsTtoLst(double gst, double longitude)
        {
            var l = longitude/ 15;

            var lst = gst + l;
            while (lst < 0 )
            {
                lst += 24;
            }
            while (lst >= 24)
            {
                lst -= 24;
            }

            return lst;
        }
    }
}

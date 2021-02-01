using System;

namespace ASCOM.Meade.net.AstroMaths
{
    public class AstroMaths : IAstroMaths
    {

        //returns the decimal hour angle for given right ascension on a given datetime for a given logitude.
        public double RightAscensionToHourAngle(DateTime utcDateTime, double longitude, double rightAscension)
        {
            //var ut = DateTimeToDecimalHours( utcDateTime);
            var gst = utcDateTime.UTtoGst();
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
            var gst = utcDateTime.UTtoGst();
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
            var az = altAz.Azimuth.DegreesToRadians();
            var alt = altAz.Altitude.DegreesToRadians();
            var lat = latitude.DegreesToRadians();
            
            var sinDec = Math.Sin(alt) * Math.Sin(lat) + Math.Cos(alt) * Math.Cos(lat) * Math.Cos(az);
            var dec = Math.Asin(sinDec).RadiansToDegrees();
            
            var y = -Math.Cos(alt) * Math.Cos(lat) * Math.Sin(az);
            var x = Math.Sin(alt) - Math.Sin(lat) * sinDec;
            var upperA = Math.Atan2(y,x);
            var upperB = upperA.RadiansToDegrees();

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
            var h1 = h.DegreesToRadians();
            var d = raDec.Declination.DegreesToRadians();
            var lat = latitude.DegreesToRadians();
            var sinA = Math.Sin(d) * Math.Sin(lat) + Math.Cos(d) * Math.Cos(lat) * Math.Cos(h1);

             var y = -Math.Cos(d) * Math.Cos(lat) * Math.Sin(h1);
            var x = Math.Sin(d) - Math.Sin(lat) * sinA;
            var upperA = Math.Atan2(y, x);
            var upperB = upperA.RadiansToDegrees();

            var horizonCoordinates = new HorizonCoordinates
            {
                Altitude = Math.Asin(sinA).RadiansToDegrees(), 
                Azimuth = upperB
            };


            if (upperB < 0)
            {
                horizonCoordinates.Azimuth = 360 + horizonCoordinates.Azimuth;
            }
            
            return horizonCoordinates;
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
using System;

namespace ASCOM.Meade.net.AstroMaths
{
    public interface IAstroMaths
    {
        double RightAscensionToHourAngle(DateTime utcDateTime, double longitude, double rightAscension);
        double HourAngleToRightAscension(DateTime utcDateTime, double longitude, double hourAngle );
        EquatorialCoordinates ConvertHozToEq( DateTime utcDateTime, double latitude, double longitude, HorizonCoordinates altAz);
        HorizonCoordinates ConvertEqToHoz(double hourAngle, double latitude, EquatorialCoordinates raDec);
        double DegreesToRadians(double degrees);
        double RadiansToDegrees(double radians);
        double DateTimeToDecimalHours( DateTime utcDateTime);
        double UTtoGst(DateTime utcDateTime);
        double GsTtoLst(double gst, double longitude);
    }
}
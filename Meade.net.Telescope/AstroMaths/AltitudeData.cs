using System;

namespace ASCOM.Meade.net.AstroMaths
{
    public class AltitudeData
    {
        public DateTime UtcDateTime { get; set; }
        public double SiteLatitude { get; set; }
        public double SiteLongitude { get; set; }
        public EquatorialCoordinates equatorialCoordinates { get; set; }
    }
}
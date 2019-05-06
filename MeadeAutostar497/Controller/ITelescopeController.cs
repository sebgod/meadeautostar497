using System;
using ASCOM.DeviceInterface;

namespace ASCOM.MeadeAutostar497.Controller
{
    public interface ITelescopeController
    {
        string Port { get; set; }
        bool Connected { get; set; }

        bool Slewing { get; }
        DateTime utcDate { get; set; }
        double SiteLatitude { get; set; }
        double SiteLongitude { get; set; }
        AlignmentModes AlignmentMode { get; set; }
        bool AtPark { get; }
        double Azimuth { get; }
        double Declination { get; }
        double Altitude { get; }
        void AbortSlew();
        void PulseGuide(GuideDirections direction, int duration);
        void Park();
    }
}
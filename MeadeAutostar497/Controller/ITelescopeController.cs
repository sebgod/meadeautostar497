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
        double Altitude { get; }
        double Azimuth { get; }
        double RightAscension { get; }
        double Declination { get; }
        double TargetRightAscension { get; set; }
        double TargetDeclination { get; set; }
        DriveRates TrackingRate { get; }
        int FocuserMaxIncrement { get; set; }
        int FocuserMaxStep { get; set; }
        void AbortSlew();
        void PulseGuide(GuideDirections direction, int duration);
        void Park();
        void SlewToCoordinates(double rightAscension, double declination);
        void SlewToCoordinatesAsync(double rightAscension, double declination);
        void SlewToAltAz(double azimuth, double altitude);
        void SlewToAltAzAsync(double azimuth, double altitude);
        void SyncToTarget();
        void SlewToTarget();
        void SlewToTargetAsync();
        void MoveAxis(TelescopeAxes axis, double rate);
        void FocuserHalt();
        void FocuserMove(int position);
    }
}
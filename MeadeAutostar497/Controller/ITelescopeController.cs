using System;

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
        void AbortSlew();
    }
}
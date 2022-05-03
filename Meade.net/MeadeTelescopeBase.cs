using System;
using System.Reflection;
using System.Runtime.InteropServices;
using ASCOM.Meade.net.Wrapper;
using ASCOM.Utilities;

namespace ASCOM.Meade.net
{
    [ComVisible(false)]
    public class MeadeTelescopeBase : ReferenceCountedObjectBase
    {
        /// <summary>
        /// Variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
        /// </summary>
        protected static TraceLogger Tl;

        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        protected static readonly string DriverDescription = "Meade Generic";

        protected readonly ISharedResourcesWrapper SharedResourcesWrapper;
        protected ProfileProperties _profileProperties;

        public MeadeTelescopeBase()
        {
            SharedResourcesWrapper = new SharedResourcesWrapper();
        }

        public MeadeTelescopeBase(ISharedResourcesWrapper sharedResourcesWrapper)
        {
            SharedResourcesWrapper = sharedResourcesWrapper;
        }

        protected void Initialise(string className)
        {
            Tl = new TraceLogger("", $"Meade.Generic.{className}");

            ReadProfile(); // Read device configuration from the ASCOM Profile store

            IsConnected = false; // Initialise connected to false

            LogMessage(className, "Completed initialisation");
            LogMessage(className, $"Driver version: {DriverVersion}");
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        protected void ReadProfile()
        {
            _profileProperties = SharedResourcesWrapper.ReadProfile();
            Tl.Enabled = _profileProperties.TraceLogger;

            LogMessage("ReadProfile", $"Trace logger enabled: {Tl.Enabled}");
            LogMessage("ReadProfile", $"Com Port: {_profileProperties.ComPort}");
            LogMessage("ReadProfile", $"Backlash Steps: {_profileProperties.BacklashCompensation}");
            LogMessage("ReadProfile", $"Dynamic breaking: {_profileProperties.DynamicBreaking}");
            LogMessage("ReadProfile", $"Guide Rate: {_profileProperties.GuideRateArcSecondsPerSecond}");
            LogMessage("ReadProfile", $"Precision: {_profileProperties.Precision}");
            LogMessage("ReadProfile", $"Guiding Style: {_profileProperties.GuidingStyle}");
            LogMessage("ReadProfile", $"Site Elevation: {_profileProperties.SiteElevation}");
            LogMessage("ReadProfile", $"Settle Time after slew: {_profileProperties.SettleTime}");
            LogMessage("ReadProfile", $"Send date and time on connect: {_profileProperties.SendDateTime}");
            LogMessage("ReadProfile", $"Parked Behaviour: {_profileProperties.ParkedBehaviour}");
            LogMessage("ReadProfile", $"Parked Alt: {_profileProperties.ParkedAlt}");
            LogMessage("ReadProfile", $"Parked Az: {_profileProperties.ParkedAz}");
            LogMessage("ReadProfile", $"Focal Length: {_profileProperties.FocalLength}");
            LogMessage("ReadProfile", $"Aperture Area: {_profileProperties.ApertureArea}");
            LogMessage("ReadProfile", $"Aperture Area: {_profileProperties.ApertureDiameter}");
        }

        /// <summary>
        /// Log helper function that takes formatted strings and arguments
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void LogMessage(string identifier, string message, params object[] args)
        {
            var msg = string.Format(message, args);
            Tl.LogMessage(identifier, msg);
        }

        /// <summary>
        /// Returns true if there is a valid connection to the driver hardware
        /// </summary>
        protected bool IsConnected { get; set; }

        public string Description
        {
            get
            {
                Tl.LogMessage("Description Get", DriverDescription);
                return DriverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                string driverInfo = $"{Description} .net driver. Version: {DriverVersion}";
                LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
                LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        #region ASCOM Registration

        private static IProfileFactory _profileFactory;

        public static IProfileFactory ProfileFactory
        {
            get => _profileFactory ?? (_profileFactory = new ProfileFactory());
            set => _profileFactory = value;
        }

        #endregion

        protected void UpdateSiteElevation()
        {
            var profileProperties = SharedResourcesWrapper.ReadProfile();
            profileProperties.SiteElevation = _profileProperties.SiteElevation;
            SharedResourcesWrapper.WriteProfile(profileProperties);
        }
    }
}
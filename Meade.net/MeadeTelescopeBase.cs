using System;
using System.Reflection;
using System.Runtime.InteropServices;
using ASCOM.Meade.net.AstroMaths;
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

        protected static string ComPort; // Variables to hold the currrent device configuration
        protected static int BacklashCompensation;
        protected static bool ReverseFocusDirection;
        protected static bool UseDynamicBreaking;
        protected double GuideRate;
        protected string Precision;
        protected string GuidingStyle;
        protected double SiteElevation;
        protected short ProfileSettleTime;
        protected bool SendDateTime;
        protected ParkedBehaviour ParkedBehaviour;
        protected HorizonCoordinates ParkedAltAz;

        protected readonly ISharedResourcesWrapper SharedResourcesWrapper;

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
            var profileProperties = SharedResourcesWrapper.ReadProfile();
            Tl.Enabled = profileProperties.TraceLogger;
            ComPort = profileProperties.ComPort;
            BacklashCompensation = profileProperties.BacklashCompensation;
            ReverseFocusDirection = profileProperties.ReverseFocusDirection;
            UseDynamicBreaking = profileProperties.DynamicBreaking;
            GuideRate = profileProperties.GuideRateArcSecondsPerSecond;
            Precision = profileProperties.Precision;
            GuidingStyle = profileProperties.GuidingStyle.ToLower();
            SiteElevation = profileProperties.SiteElevation;
            ProfileSettleTime = profileProperties.SettleTime;
            SendDateTime = profileProperties.SendDateTime;
            ParkedBehaviour = profileProperties.ParkedBehaviour;

            ParkedAltAz = new HorizonCoordinates
            {
                Altitude = profileProperties.ParkedAlt,
                Azimuth = profileProperties.ParkedAz
            };

            LogMessage("ReadProfile", $"Trace logger enabled: {Tl.Enabled}");
            LogMessage("ReadProfile", $"Com Port: {ComPort}");
            LogMessage("ReadProfile", $"Backlash Steps: {BacklashCompensation}");
            LogMessage("ReadProfile", $"Dynamic breaking: {UseDynamicBreaking}");
            LogMessage("ReadProfile", $"Guide Rate: {GuideRate}");
            LogMessage("ReadProfile", $"Precision: {Precision}");
            LogMessage("ReadProfile", $"Guiding Style: {GuidingStyle}");
            LogMessage("ReadProfile", $"Site Elevation: {SiteElevation}");
            LogMessage("ReadProfile", $"Settle Time after slew: {ProfileSettleTime}");
            LogMessage("ReadProfile", $"Send date and time on connect: {SendDateTime}");
            LogMessage("ReadProfile", $"Parked Behaviour: {ParkedBehaviour}");
            LogMessage("ReadProfile", $"Parked Alt: {ParkedAltAz.Altitude}");
            LogMessage("ReadProfile", $"Parked Az: {ParkedAltAz.Azimuth}");
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
            profileProperties.SiteElevation = SiteElevation;
            SharedResourcesWrapper.WriteProfile(profileProperties);
        }
    }
}
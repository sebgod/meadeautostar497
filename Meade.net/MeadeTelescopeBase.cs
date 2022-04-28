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

        protected static string _ComPort; // Variables to hold the currrent device configuration
        protected static int _BacklashCompensation;
        protected static bool _ReverseFocusDirection;
        protected static bool _UseDynamicBreaking;
        protected double _GuideRate;
        protected string _Precision;
        protected string _GuidingStyle;
        protected double _SiteElevation;
        protected short _ProfileSettleTime;
        protected bool _SendDateTime;
        protected ParkedBehaviour _ParkedBehaviour;
        protected HorizonCoordinates _ParkedAltAz;
        protected double _focalLength;

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
            _ComPort = profileProperties.ComPort;
            _BacklashCompensation = profileProperties.BacklashCompensation;
            _ReverseFocusDirection = profileProperties.ReverseFocusDirection;
            _UseDynamicBreaking = profileProperties.DynamicBreaking;
            _GuideRate = profileProperties.GuideRateArcSecondsPerSecond;
            _Precision = profileProperties.Precision;
            _GuidingStyle = profileProperties.GuidingStyle.ToLower();
            _SiteElevation = profileProperties.SiteElevation;
            _ProfileSettleTime = profileProperties.SettleTime;
            _SendDateTime = profileProperties.SendDateTime;
            _ParkedBehaviour = profileProperties.ParkedBehaviour;

            _ParkedAltAz = new HorizonCoordinates
            {
                Altitude = profileProperties.ParkedAlt,
                Azimuth = profileProperties.ParkedAz
            };

            _focalLength = profileProperties.FocalLength;

            LogMessage("ReadProfile", $"Trace logger enabled: {Tl.Enabled}");
            LogMessage("ReadProfile", $"Com Port: {_ComPort}");
            LogMessage("ReadProfile", $"Backlash Steps: {_BacklashCompensation}");
            LogMessage("ReadProfile", $"Dynamic breaking: {_UseDynamicBreaking}");
            LogMessage("ReadProfile", $"Guide Rate: {_GuideRate}");
            LogMessage("ReadProfile", $"Precision: {_Precision}");
            LogMessage("ReadProfile", $"Guiding Style: {_GuidingStyle}");
            LogMessage("ReadProfile", $"Site Elevation: {_SiteElevation}");
            LogMessage("ReadProfile", $"Settle Time after slew: {_ProfileSettleTime}");
            LogMessage("ReadProfile", $"Send date and time on connect: {_SendDateTime}");
            LogMessage("ReadProfile", $"Parked Behaviour: {_ParkedBehaviour}");
            LogMessage("ReadProfile", $"Parked Alt: {_ParkedAltAz.Altitude}");
            LogMessage("ReadProfile", $"Parked Az: {_ParkedAltAz.Azimuth}");
            LogMessage("ReadProfile", $"Focal Length: {_focalLength}");
            
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
            profileProperties.SiteElevation = _SiteElevation;
            SharedResourcesWrapper.WriteProfile(profileProperties);
        }
    }
}
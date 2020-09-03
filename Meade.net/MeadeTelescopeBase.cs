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
        protected static TraceLogger _tl;

        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        protected static readonly string DriverDescription = "Meade Generic";

        protected static string _comPort; // Variables to hold the currrent device configuration
        protected static int _backlashCompensation;
        protected static bool _reverseFocusDirection;
        protected static bool _useDynamicBreaking;
        protected double _guideRate;
        protected string _precision;
        protected string _guidingStyle;

        protected readonly ISharedResourcesWrapper _sharedResourcesWrapper;

        public MeadeTelescopeBase()
        {
            _sharedResourcesWrapper = new SharedResourcesWrapper();
        }

        public MeadeTelescopeBase(ISharedResourcesWrapper sharedResourcesWrapper)
        {
            _sharedResourcesWrapper = sharedResourcesWrapper;
        }

        protected void Initialise()
        {
            var typeName = GetType().Name;

            _tl = new TraceLogger("", $"Meade.Generic.{typeName}");

            ReadProfile(); // Read device configuration from the ASCOM Profile store

            IsConnected = false; // Initialise connected to false

            LogMessage(typeName, "Completed initialisation");
            LogMessage(typeName, $"Driver version: {DriverVersion}");
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        protected void ReadProfile()
        {
            var profileProperties = _sharedResourcesWrapper.ReadProfile();
            _tl.Enabled = profileProperties.TraceLogger;
            _comPort = profileProperties.ComPort;
            _backlashCompensation = profileProperties.BacklashCompensation;
            _reverseFocusDirection = profileProperties.ReverseFocusDirection;
            _useDynamicBreaking = profileProperties.DynamicBreaking;
            _guideRate = profileProperties.GuideRateArcSecondsPerSecond;
            _precision = profileProperties.Precision;
            _guidingStyle = profileProperties.GuidingStyle.ToLower();

            LogMessage("ReadProfile", $"Trace logger enabled: {_tl.Enabled}");
            LogMessage("ReadProfile", $"Com Port: {_comPort}");
            LogMessage("ReadProfile", $"Backlash Steps: {_backlashCompensation}");
            LogMessage("ReadProfile", $"Dynamic breaking: {_useDynamicBreaking}");
            LogMessage("ReadProfile", $"Guide Rate: {_guideRate}");
            LogMessage("ReadProfile", $"Precision: {_precision}");
            LogMessage("ReadProfile", $"Guiding Style: {_guidingStyle}");
        }

        /// <summary>
        /// Log helper function that takes formatted strings and arguments
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void LogMessage(string identifier, string message, params object[] args)
        {
            var msg = String.Format(message, args);
            _tl.LogMessage(identifier, msg);
        }

        /// <summary>
        /// Returns true if there is a valid connection to the driver hardware
        /// </summary>
        protected bool IsConnected { get; set; }

        public string Description
        {
            get
            {
                _tl.LogMessage("Description Get", DriverDescription);
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
    }
}
#define Focuser

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;
using System.Reflection;
using ASCOM.Meade.net.Wrapper;
using ASCOM.Utilities.Interfaces;

namespace ASCOM.Meade.net
{
    //
    // Your driver's DeviceID is ASCOM.Meade.net.Focuser
    //
    // The Guid attribute sets the CLSID for ASCOM.Meade.net.Focuser
    // The ClassInterface/None addribute prevents an empty interface called
    // _Meade.net from being created and used as the [default] interface
    //
    // TODO Replace the not implemented exceptions with code to implement the function or
    // throw the appropriate ASCOM exception.
    //

    /// <summary>
    /// ASCOM Focuser Driver for Meade.net.
    /// </summary>
    [Guid("a32ac647-bf0f-42f9-8ab0-d166fa5884ad")]
    [ProgId("ASCOM.MeadeGeneric.focuser")]
    [ServedClassName("Meade.net Focuser")]
    [ClassInterface(ClassInterfaceType.None)]
    public class Focuser : ReferenceCountedObjectBase, IFocuserV3
    {
        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        //internal static string driverID = "ASCOM.Meade.net.Focuser";
        private static readonly string DriverId = Marshal.GenerateProgIdForType(MethodBase.GetCurrentMethod().DeclaringType);
        // TODO Change the descriptive string for your driver then remove this line
        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        private static readonly string DriverDescription = "Meade Generic";

        private static string _comPort; // Variables to hold the currrent device configuration

        /// <summary>
        /// Private variable to hold an ASCOM Utilities object
        /// </summary>
        private readonly IUtil _utilities;

        /// <summary>
        /// Variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
        /// </summary>
        internal static TraceLogger Tl;

        private readonly ISharedResourcesWrapper _sharedResourcesWrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="Meade.net"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public Focuser()
        {
            //todo move this out to IOC
            _utilities = new Util(); //Initialise util object
            _sharedResourcesWrapper = new SharedResourcesWrapper();

            Initialise();
        }

        private void Initialise()
        {
            Tl = new TraceLogger("", "Meade.net.focusser");

            Tl.LogMessage("Focuser", "Starting initialisation");
            ReadProfile(); // Read device configuration from the ASCOM Profile store

            IsConnected = false; // Initialise connected to false

            Tl.LogMessage("Focuser", "Completed initialisation");
        }


        //
        // PUBLIC COM INTERFACE IFocuserV3 IMPLEMENTATION
        //

        #region Common properties and methods.

        /// <summary>
        /// Displays the Setup Dialog form.
        /// If the user clicks the OK button to dismiss the form, then
        /// the new settings are saved, otherwise the old values are reloaded.
        /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
        /// </summary>
        public void SetupDialog()
        {
            Tl.LogMessage("SetupDialog", "Opening setup dialog");
            _sharedResourcesWrapper.SetupDialog();
            ReadProfile();
            Tl.LogMessage("SetupDialog", "complete");
        }

        public ArrayList SupportedActions
        {
            get
            {
                Tl.LogMessage("SupportedActions Get", "Returning empty arraylist");
                return new ArrayList();
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            LogMessage("", "Action {0}, parameters {1} not implemented", actionName, actionParameters);
            throw new ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            // Call CommandString and return as soon as it finishes
            //this.CommandString(command, raw);
            _sharedResourcesWrapper.SendBlind(command);
            // or
            //throw new ASCOM.MethodNotImplementedException("CommandBlind");
            // DO NOT have both these sections!  One or the other
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            //string ret = CommandString(command, raw);
            // TODO decode the return string and return true or false
            // or
            throw new MethodNotImplementedException("CommandBool");
            // DO NOT have both these sections!  One or the other
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            // it's a good idea to put all the low level communication with the device here,
            // then all communication calls this function
            // you need something to ensure that only one command is in progress at a time
            return _sharedResourcesWrapper.SendString(command);

            throw new MethodNotImplementedException("CommandString");
        }

        public void Dispose()
        {
            // Clean up the tracelogger and util objects
            Tl.Enabled = false;
            Tl.Dispose();
            Tl = null;
        }

        public bool Connected
        {
            get
            {
                LogMessage("Connected", "Get {0}", IsConnected);
                return IsConnected;
            }
            set
            {
                Tl.LogMessage("Connected", "Set {0}", value);
                if (value == IsConnected)
                    return;

                if (value)
                {
                    try
                    {
                        _sharedResourcesWrapper.Connect("Serial");
                        try
                        {
                            SelectSite(1);
                            SetLongFormat(true);

                            IsConnected = true;
                        }
                        catch (Exception)
                        {
                            _sharedResourcesWrapper.Disconnect("Serial");
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage("Connected Set", "Error connecting to port {0} - {1}", _comPort, ex.Message);
                    }
                }
                else
                {
                    LogMessage("Connected Set", "Disconnecting from port {0}", _comPort);
                    _sharedResourcesWrapper.Disconnect("Serial");
                    IsConnected = false;
                }
            }
        }

        private void SetLongFormat(bool setLongFormat)
        {
            _sharedResourcesWrapper.Lock(() =>
            {
                var result = _sharedResourcesWrapper.SendString(":GZ#");
                //:GZ# Get telescope azimuth
                //Returns: DDD*MM#T or DDD*MM’SS#
                //The current telescope Azimuth depending on the selected precision.

                bool isLongFormat = result.Length > 6;

                if (isLongFormat != setLongFormat)
                {
                    _utilities.WaitForMilliseconds(500);
                    _sharedResourcesWrapper.SendBlind(":U#");
                    //:U# Toggle between low/hi precision positions
                    //Low - RA displays and messages HH:MM.T sDD*MM
                    //High - Dec / Az / El displays and messages HH:MM: SS sDD*MM:SS
                    //    Returns Nothing
                }
            });
        }

        private void SelectSite(int site)
        {
            _sharedResourcesWrapper.SendBlind($":W{site}#");
            //:W<n>#
            //Set current site to<n>, an ASCII digit in the range 1..4
            //Returns: Nothing
        }

        public string Description
        {
            // TODO customise this device description
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
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                // TODO customise this driver description
                string driverInfo = "Information about the driver itself. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                Tl.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                Tl.LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                LogMessage("InterfaceVersion Get", "3");
                return Convert.ToInt16("3");
            }
        }

        public string Name
        {
            get
            {
                //string name = "Short driver name - please customise";
                string name = DriverDescription;
                Tl.LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        #region IFocuser Implementation

        public bool Absolute
        {
            get
            {
                Tl.LogMessage("Absolute Get", false.ToString());
                return false; // This is a relative focuser
            }
        }

        public void Halt()
        {
            Tl.LogMessage("Halt", "Halting");

            CheckConnected("Halt");

            //A single halt command is sometimes missed by the #909 apm, so let's do it a few times to be safe.
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < 1000)
            {
                _sharedResourcesWrapper.SendBlind(":FQ#");
                //:FQ# Halt Focuser Motion
                //Returns: Nothing

                _utilities.WaitForMilliseconds(250);
            }
        }

        public bool IsMoving
        {
            get
            {
                Tl.LogMessage("IsMoving Get", false.ToString());
                return false; // This focuser always moves instantaneously so no need for IsMoving ever to be True
            }
        }

        public bool Link
        {
            get
            {
                Tl.LogMessage("Link Get", Connected.ToString());
                return Connected; // Direct function to the connected method, the Link method is just here for backwards compatibility
            }
            set
            {
                Tl.LogMessage("Link Set", value.ToString());
                Connected = value; // Direct function to the connected method, the Link method is just here for backwards compatibility
            }
        }

        private readonly int _maxIncrement = 7000;
        public int MaxIncrement
        {
            get
            {
                Tl.LogMessage("MaxIncrement Get", _maxIncrement.ToString());
                return _maxIncrement; // Maximum change in one move
            }
        }

        private readonly int _maxStep = 7000;
        public int MaxStep
        {
            get
            {
                Tl.LogMessage("MaxStep Get", _maxStep.ToString());
                return _maxStep;
            }
        }

        public void Move(int position)
        {
            Tl.LogMessage("Move", position.ToString());
            CheckConnected("Move");

            //todo implement backlash compensation
            //todo implement direction reverse
            //todo implement dynamic braking

            if (position < -MaxIncrement || position > MaxIncrement)
            {
                throw new InvalidValueException($"position out of range {-MaxIncrement} < {position} < {MaxIncrement}");
            }

            if (position == 0)
                return;

            if (position > 0)
            {
                //desired move direction is out
                MoveFocuser(true, Math.Abs(position));
            }
            else
            {
                //desired move direction is in
                MoveFocuser(false, Math.Abs(position));
            }
        }

        private void MoveFocuser(bool directionOut, int steps)
        {
            _sharedResourcesWrapper.Lock(() =>
            {
                //_sharedResourcesWrapper.SendBlind(":FF#");
                //:FF# Set Focus speed to fastest setting
                //Returns: Nothing

                //:FS# Set Focus speed to slowest setting
                //Returns: Nothing

                //:F<n># Autostar, Autostar II – set focuser speed to <n> where <n> is an ASCII digit 1..4
                //Returns: Nothing
                //All others – Not Supported
                _utilities.WaitForMilliseconds(100);

                //A Single focus command sometimes gets lost on the #909, so sending lots of them solves the issue.
                Stopwatch stopwatch = Stopwatch.StartNew();
                while (stopwatch.ElapsedMilliseconds < steps)
                {
                    _sharedResourcesWrapper.SendBlind(directionOut ? ":F+#" : ":F-#");
                    //:F+# Start Focuser moving inward (toward objective)
                    //Returns: None

                    //:F-# Start Focuser moving outward (away from objective)
                    //Returns: None

                    _utilities.WaitForMilliseconds(250);
                }

                Halt();

                //This gives the focuser time to physically stop.
                _utilities.WaitForMilliseconds(1000);
            });
        }

        public int Position => throw new PropertyNotImplementedException("Position", false);

        public double StepSize
        {
            get
            {
                Tl.LogMessage("StepSize Get", "Not implemented");
                throw new PropertyNotImplementedException("StepSize", false);
            }
        }

        public bool TempComp
        {
            get
            {
                Tl.LogMessage("TempComp Get", false.ToString());
                return false;
            }
            set
            {
                Tl.LogMessage("TempComp Set", "Not implemented");
                throw new PropertyNotImplementedException("TempComp", false);
            }
        }

        public bool TempCompAvailable
        {
            get
            {
                Tl.LogMessage("TempCompAvailable Get", false.ToString());
                return false; // Temperature compensation is not available in this driver
            }
        }

        public double Temperature
        {
            get
            {
                Tl.LogMessage("Temperature Get", "Not implemented");
                throw new PropertyNotImplementedException("Temperature", false);
            }
        }

        #endregion

        #region Private properties and methods
        // here are some useful properties and methods that can be used as required
        // to help with driver development

        #region ASCOM Registration

        // Register or unregister driver for ASCOM. This is harmless if already
        // registered or unregistered. 
        //
        /// <summary>
        /// Register or unregister the driver with the ASCOM Platform.
        /// This is harmless if the driver is already registered/unregistered.
        /// </summary>
        /// <param name="bRegister">If <c>true</c>, registers the driver, otherwise unregisters it.</param>
        private static void RegUnregAscom(bool bRegister)
        {
            using (var p = new Profile())
            {
                p.DeviceType = "Focuser";
                if (bRegister)
                {
                    p.Register(DriverId, DriverDescription);
                }
                else
                {
                    p.Unregister(DriverId);
                }
            }
        }

        /// <summary>
        /// This function registers the driver with the ASCOM Chooser and
        /// is called automatically whenever this class is registered for COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is successfully built.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During setup, when the installer registers the assembly for COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually register a driver with ASCOM.
        /// </remarks>
        [ComRegisterFunction]
        public static void RegisterAscom(Type t)
        {
            RegUnregAscom(true);
        }

        /// <summary>
        /// This function unregisters the driver from the ASCOM Chooser and
        /// is called automatically whenever this class is unregistered from COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is cleaned or prior to rebuilding.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During uninstall, when the installer unregisters the assembly from COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually unregister a driver from ASCOM.
        /// </remarks>
        [ComUnregisterFunction]
        public static void UnregisterAscom(Type t)
        {
            RegUnregAscom(false);
        }

        #endregion

        /// <summary>
        /// Returns true if there is a valid connection to the driver hardware
        /// </summary>
        private bool IsConnected { get; set; }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!IsConnected)
            {
                throw new NotConnectedException(message);
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {
            var profileProperties = _sharedResourcesWrapper.ReadProfile();
            Tl.Enabled = profileProperties.TraceLogger;
            _comPort = profileProperties.ComPort;
        }

        /// <summary>
        /// Log helper function that takes formatted strings and arguments
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        internal static void LogMessage(string identifier, string message, params object[] args)
        {
            var msg = string.Format(message, args);
            Tl.LogMessage(identifier, msg);
        }
        #endregion
    }
}

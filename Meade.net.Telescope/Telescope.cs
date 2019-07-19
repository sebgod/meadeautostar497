#define Telescope

using System;
using System.Runtime.InteropServices;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using System.Collections;
using System.Globalization;
using System.Reflection;
using ASCOM.Meade.net.AstroMaths;
using ASCOM.Meade.net.Wrapper;
using ASCOM.Utilities.Interfaces;

namespace ASCOM.Meade.net
{
    //
    // Your driver's DeviceID is ASCOM.Meade.net.Telescope
    //
    // The Guid attribute sets the CLSID for ASCOM.Meade.net.Telescope
    // The ClassInterface/None addribute prevents an empty interface called
    // _Meade.net from being created and used as the [default] interface
    //
    // TODO Replace the not implemented exceptions with code to implement the function or
    // throw the appropriate ASCOM exception.
    //

    /// <summary>
    /// ASCOM Telescope Driver for Meade.net.
    /// </summary>
    [Guid("d9fd4b3e-c4f1-48ac-a16f-d02eef30d86f")]
    [ProgId("ASCOM.MeadeGeneric.Telescope")]
    [ServedClassName("Meade.net Telescope")]
    [ClassInterface(ClassInterfaceType.None)]
    public class Telescope : ReferenceCountedObjectBase, ITelescopeV3
    {
        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        //internal static string driverID = "ASCOM.Meade.net.Telescope";
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
        private readonly IUtilExtra _utilitiesExtra;

        /// <summary>
        /// Private variable to hold an ASCOM AstroUtilities object to provide the Range method
        /// </summary>
        private readonly IAstroUtils _astroUtilities;

        private readonly IAstroMaths _astroMaths;

        /// <summary>
        /// Variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
        /// </summary>
        private TraceLogger _tl;

        private readonly ISharedResourcesWrapper _sharedResourcesWrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="Meade.net"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public Telescope()
        {
            //todo move this out to IOC
            var util = new Util(); //Initialise util object
            _utilities = util; 
            _utilitiesExtra = util; //Initialise util object
            _astroUtilities = new AstroUtils(); // Initialise astro utilities object
            _sharedResourcesWrapper = new SharedResourcesWrapper();
            _astroMaths = new AstroMaths.AstroMaths();

            Initialise();
        }

        public Telescope( IUtil util, IUtilExtra utilExtra, IAstroUtils astroUtilities, ISharedResourcesWrapper sharedResourcesWrapper, IAstroMaths astroMaths)
        {
            _utilities = util; //Initialise util object
            _utilitiesExtra = utilExtra; //Initialise util object
            _astroUtilities = astroUtilities; // Initialise astro utilities object
            _sharedResourcesWrapper = sharedResourcesWrapper;
            _astroMaths = astroMaths;

            Initialise();
        }

        private void Initialise()
        {
            //todo move the TraceLogger out to a factory class.
            _tl = new TraceLogger("", "Meade.net.Telescope");
            LogMessage("Telescope", "Starting initialisation");

            ReadProfile(); // Read device configuration from the ASCOM Profile store

            IsConnected = false; // Initialise connected to false

            LogMessage("Telescope", "Completed initialisation");
        }


        //
        // PUBLIC COM INTERFACE ITelescopeV3 IMPLEMENTATION
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
            LogMessage("SetupDialog", "Opening setup dialog");
            _sharedResourcesWrapper.SetupDialog();
            ReadProfile();
            LogMessage("SetupDialog", "complete");
            //// consider only showing the setup dialog if not connected
            //// or call a different dialog if connected
            //if (IsConnected)
            //    System.Windows.Forms.MessageBox.Show("Already connected, just press OK");

            //using (SetupDialogForm F = new SetupDialogForm())
            //{
            //    var result = F.ShowDialog();
            //    if (result == System.Windows.Forms.DialogResult.OK)
            //    {
            //        WriteProfile(); // Persist device configuration values to the ASCOM Profile store
            //    }
            //}
        }

        public ArrayList SupportedActions
        {
            get
            {
                LogMessage("SupportedActions Get", "Returning empty arraylist");
                var supportedActions = new ArrayList();
                supportedActions.Add("handbox");
                return supportedActions;
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            CheckConnected("Action");

            switch (actionName.ToLower())
            {
                case "handbox":
                    switch (actionParameters.ToLower())
                    {
                        //Read the screen
                        case "readdisplay":
                            var output = _sharedResourcesWrapper.SendString(":ED#");
                            return output;

                        //top row of buttons
                        case "enter":
                            _sharedResourcesWrapper.SendBlind(":EK13#");
                            break;
                        case "mode":
                            _sharedResourcesWrapper.SendBlind(":EK9#");
                            break;
                        case "longmode":
                            _sharedResourcesWrapper.SendBlind(":EK11#");
                            break;
                        case "goto":
                            _sharedResourcesWrapper.SendBlind(":EK24#");
                            break;

                        case "0": //light and 0
                            _sharedResourcesWrapper.SendBlind(":EK48#");
                            break;
                        case "1":
                            _sharedResourcesWrapper.SendBlind(":EK49#");
                            break;
                        case "2":
                            _sharedResourcesWrapper.SendBlind(":EK50#");
                            break;
                        case "3":
                            _sharedResourcesWrapper.SendBlind(":EK51#");
                            break;
                        case "4":
                            _sharedResourcesWrapper.SendBlind(":EK52#");
                            break;
                        case "5":
                            _sharedResourcesWrapper.SendBlind(":EK53#");
                            break;
                        case "6":
                            _sharedResourcesWrapper.SendBlind(":EK54#");
                            break;
                        case "7":
                            _sharedResourcesWrapper.SendBlind(":EK55#");
                            break;
                        case "8":
                            _sharedResourcesWrapper.SendBlind(":EK56#");
                            break;
                        case "9":
                            _sharedResourcesWrapper.SendBlind(":EK57#");
                            break;

                        case "up":
                            _sharedResourcesWrapper.SendBlind(":EK94#");
                            break;
                        case "down":
                            _sharedResourcesWrapper.SendBlind(":EK118#");
                            break;
                        case "back":
                            _sharedResourcesWrapper.SendBlind(":EK87#");
                            break;
                        case "forward":
                            _sharedResourcesWrapper.SendBlind(":EK69#");
                            break;
                        case "?":
                            _sharedResourcesWrapper.SendBlind(":EK63#");
                            break;
                        default:
                            LogMessage("", "Action {0}, parameters {1} not implemented", actionName, actionParameters);
                            throw new ActionNotImplementedException($"{actionName}({actionParameters})");
                    }
                    break;
                default:
                    LogMessage("", "Action {0}, parameters {1} not implemented", actionName, actionParameters);
                    throw new ActionNotImplementedException($"{actionName}");
            }

            return string.Empty;
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
            //throw new ASCOM.MethodNotImplementedException("CommandString");
        }

        public void Dispose()
        {
            if (Connected)
                Connected = false;

            // Clean up the tracelogger and util objects
            _tl.Enabled = false;
            _tl.Dispose();
            _tl = null;
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
                LogMessage("Connected", "Set {0}", value);
                if (value == IsConnected)
                    return;

                if (value)
                {
                    LogMessage("Connected Set", "Connecting to port {0}", _comPort);
                    try
                    {
                        _sharedResourcesWrapper.Connect("Serial");
                        try
                        {
                            LogMessage("Connected Set", $"Connected to port {_comPort}. Product: {_sharedResourcesWrapper.ProductName} Version:{_sharedResourcesWrapper.FirmwareVersion}");

                            SetLongFormat(true);
                            _userNewerPulseGuiding = IsNewPulseGuidingSupported();
                            _targetDeclination = InvalidParameter;
                            _targetRightAscension = InvalidParameter;
                            _tracking = true;

                            LogMessage("Connected Set", $"New Pulse Guiding Supported: {_userNewerPulseGuiding}");
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

        public bool IsNewPulseGuidingSupported()
        {
            if (_sharedResourcesWrapper.ProductName == _sharedResourcesWrapper.Autostar497)
            {
                return FirmwareIsGreaterThan(_sharedResourcesWrapper.Autostar49731Ee);
            }

            return false;
        }

        private bool FirmwareIsGreaterThan(string minVersion)
        {
            var currentVersion = _sharedResourcesWrapper.FirmwareVersion;
            var comparison = currentVersion.CompareTo(minVersion);
            return (comparison >= 0);
        }

        public void SetLongFormat(bool setLongFormat)
        {
            _sharedResourcesWrapper.Lock(() =>
            {
                var result = _sharedResourcesWrapper.SendString(":GZ#");
                //:GZ# Get telescope azimuth
                //Returns: DDD*MM# or DDD*MM’SS#
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

        //todo hook this up to a custom action
        public void SelectSite(int site)
        {
            if (site < 1)
                throw new ArgumentOutOfRangeException("site",site,"Site cannot be lower than 1");
            else if (site > 4)
                throw new ArgumentOutOfRangeException("site", site, "Site cannot be higher than 4");

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
                LogMessage("Description Get", DriverDescription);
                return DriverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                // TODO customise this driver description
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
                string driverVersion = $"{version.Major}.{version.Minor}.{version.Revision}.{version.Build}";
                LogMessage("DriverVersion Get", driverVersion);
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

                //var telescopeProduceName = _sharedResourcesWrapper.SendString(":GVP#");
                ////:GVP# Get Telescope Product Name
                ////Returns: <string>#

                //var firmwareVersion = _sharedResourcesWrapper.SendString(":GVN#");
                ////:GVN# Get Telescope Firmware Number
                ////Returns: dd.d#

                //string name = $"{telescopeProduceName} - {firmwareVersion}";
                string name = DriverDescription;
                LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        #region ITelescope Implementation
        
        public void AbortSlew()
        {
            CheckConnected("AbortSlew");

            LogMessage("AbortSlew", "Aborting slew");
            _sharedResourcesWrapper.SendBlind(":Q#");
            //:Q# Halt all current slewing
            //Returns:Nothing

            _movingPrimary = false;
            _movingSecondary = false;
        }

        public AlignmentModes AlignmentMode
        {
            get
            {
                LogMessage("AlignmentMode Get", "Getting alignmode");

                CheckConnected("AlignmentMode Get");

                const char ack = (char) 6;

                var alignmentString = _sharedResourcesWrapper.SendChar(ack.ToString());
                //ACK <0x06> Query of alignment mounting mode.
                //Returns:
                //A If scope in AltAz Mode
                //D If scope is currently in the Downloader[Autostar II & Autostar]
                //L If scope in Land Mode
                //P If scope in Polar Mode

                //todo implement GW Command - Supported in Autostar 43Eg and above
                //if FirmwareIsGreaterThan(_sharedResourcesWrapper.AUTOSTAR497_43EG)
                //{
                    //var alignmentString = SerialPort.CommandTerminated(":GW#", "#");
                    //:GW# Get Scope Alignment Status
                    //Returns: <mount><tracking><alignment>#
                    //    where:
                    //mount: A - AzEl mounted, P - Equatorially mounted, G - german mounted equatorial
                    //tracking: T - tracking, N - not tracking
                    //alignment: 0 - needs alignment, 1 - one star aligned, 2 - two star aligned, 3 - three star aligned.
                //}

                AlignmentModes alignmentMode;
                switch (alignmentString)
                {
                    case "A":
                        alignmentMode = AlignmentModes.algAltAz;
                        break;
                    case "P":
                        alignmentMode = AlignmentModes.algPolar;
                        break;
                    case "G":
                        alignmentMode = AlignmentModes.algGermanPolar;
                        break;
                    default:
                        throw new InvalidValueException(
                            $"unknown alignment returned from telescope: {alignmentString}");
                }

                LogMessage("AlignmentMode Get", $"alignmode = {alignmentMode}");
                return alignmentMode;
            }
            set
            {
                CheckConnected("AlignmentMode Set");

                //todo tidy this up into a better solution that means can :GW#, :AL#, :AA#, & :AP# and checked for Autostar properly
                if (!FirmwareIsGreaterThan(_sharedResourcesWrapper.Autostar49743Eg))
                    throw new PropertyNotImplementedException("AlignmentMode",true );

                //todo make this only try with Autostar 43Eg and above.

                    switch (value)
                {
                    case AlignmentModes.algAltAz:
                        _sharedResourcesWrapper.SendBlind(":AA#");
                        //:AA# Sets telescope the AltAz alignment mode
                        //Returns: nothing
                        break;
                    case AlignmentModes.algPolar:
                    case AlignmentModes.algGermanPolar:
                        _sharedResourcesWrapper.SendBlind(":AP#");
                        //:AP# Sets telescope to Polar alignment mode
                        //Returns: nothing
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }

                //:AL# Sets telescope to Land alignment mode
                //Returns: nothing
            }
        }

        public double Altitude
        {
            get
            {
                CheckConnected("Altitude Get");

                var altAz = CalcAltAzFromTelescopeEqData();
                LogMessage("Altitude", $"{altAz.Altitude}");
                return altAz.Altitude;

                ////todo firmware bug in 44Eg, :GA# is returning the dec, not the altitude!
                //var result = _sharedResourcesWrapper.SendString(":GA#");
                ////:GA# Get Telescope Altitude
                ////Returns: sDD* MM# or sDD*MM’SS#
                ////The current scope altitude. The returned format depending on the current precision setting.

                //var alt = utilities.DMSToDegrees(result);
                //LogMessage("Altitude", $"{alt}");
                //return alt;

                //LogMessage("Altitude Get", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("Altitude", false);
            }
        }

        private HorizonCoordinates CalcAltAzFromTelescopeEqData()
        {
            var altitudeData = _sharedResourcesWrapper.Lock(() => new AltitudeData
            {
                UtcDateTime = UTCDate,
                SiteLongitude = SiteLongitude,
                SiteLatitude = SiteLatitude,
                EquatorialCoordinates = new EquatorialCoordinates()
                {
                    RightAscension = RightAscension,
                    Declination = Declination
                }
            });

            double hourAngle = _astroMaths.RightAscensionToHourAngle(altitudeData.UtcDateTime, altitudeData.SiteLongitude,
                altitudeData.EquatorialCoordinates.RightAscension);
            var altAz = _astroMaths.ConvertEqToHoz(hourAngle, altitudeData.SiteLatitude, altitudeData.EquatorialCoordinates);
            return altAz;
        }

        public double ApertureArea
        {
            get
            {
                LogMessage("ApertureArea Get", "Not implemented");
                throw new PropertyNotImplementedException("ApertureArea", false);
            }
        }

        public double ApertureDiameter
        {
            get
            {
                LogMessage("ApertureDiameter Get", "Not implemented");
                throw new PropertyNotImplementedException("ApertureDiameter", false);
            }
        }

        public bool AtHome
        {
            get
            {
                LogMessage("AtHome", "Get - " + false.ToString());
                return false;
            }
        }

        private bool _atPark = false;

        public bool AtPark
        {
            get
            {
                LogMessage("AtPark", "Get - " + _atPark);
                return _atPark;
            }
            private set => _atPark = value;
        }

        public IAxisRates AxisRates(TelescopeAxes axis)
        {
            LogMessage("AxisRates", "Get - " + axis.ToString());
            return new AxisRates(axis);
        }

        public double Azimuth
        {
            get
            {
                CheckConnected("Azimuth Get");

                //var result = _sharedResourcesWrapper.SendString(":GZ#");
                //:GZ# Get telescope azimuth
                //Returns: DDD*MM#T or DDD*MM’SS#
                //The current telescope Azimuth depending on the selected precision.

                //double az = utilities.DMSToDegrees(result);

                //LogMessage("Azimuth Get", $"{az}");
                //return az;

                var altAz = CalcAltAzFromTelescopeEqData();
                LogMessage("Azimuth Get", $"{altAz.Azimuth}");
                return altAz.Azimuth;
            }
        }

        public bool CanFindHome
        {
            get
            {
                LogMessage("CanFindHome", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanMoveAxis(TelescopeAxes axis)
        {
            LogMessage("CanMoveAxis", "Get - " + axis.ToString());
            switch (axis)
            {
                case TelescopeAxes.axisPrimary: return true; //RA or AZ
                case TelescopeAxes.axisSecondary: return true; //Dev or Alt
                case TelescopeAxes.axisTertiary: return false; //rotator / derotator
                default: throw new InvalidValueException("CanMoveAxis", axis.ToString(), "0 to 2");
            }
        }

        public bool CanPark
        {
            get
            {
                LogMessage("CanPark", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanPulseGuide
        {
            get
            {
                LogMessage("CanPulseGuide", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSetDeclinationRate
        {
            get
            {
                LogMessage("CanSetDeclinationRate", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetGuideRates
        {
            get
            {
                LogMessage("CanSetGuideRates", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetPark
        {
            get
            {
                LogMessage("CanSetPark", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetPierSide
        {
            get
            {
                LogMessage("CanSetPierSide", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetRightAscensionRate
        {
            get
            {
                LogMessage("CanSetRightAscensionRate", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetTracking
        {
            get
            {
                LogMessage("CanSetTracking", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSlew
        {
            get
            {
                LogMessage("CanSlew", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSlewAltAz
        {
            get
            {
                LogMessage("CanSlewAltAz", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSlewAltAzAsync
        {
            get
            {
                LogMessage("CanSlewAltAzAsync", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSlewAsync
        {
            get
            {
                LogMessage("CanSlewAsync", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSync
        {
            get
            {
                LogMessage("CanSync", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSyncAltAz
        {
            get
            {
                LogMessage("CanSyncAltAz", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanUnpark
        {
            get
            {
                LogMessage("CanUnpark", "Get - " + false.ToString());
                return false;
            }
        }

        public double Declination
        {
            get
            {
                CheckConnected("Declination Get");

                var result = _sharedResourcesWrapper.SendString(":GD#");
                //:GD# Get Telescope Declination.
                //Returns: sDD*MM# or sDD*MM’SS#
                //Depending upon the current precision setting for the telescope.

                double declination = _utilities.DMSToDegrees(result);

                LogMessage("Declination", "Get - " + _utilitiesExtra.DegreesToDMS(declination, ":", ":"));
                return declination;
            }
        }

        public double DeclinationRate
        {
            get
            {
                double declination = 0.0;
                LogMessage("DeclinationRate", "Get - " + declination.ToString(CultureInfo.InvariantCulture));
                return declination;
            }
            set
            {
                LogMessage("DeclinationRate Set", "Not implemented");
                throw new PropertyNotImplementedException("DeclinationRate", true);
            }
        }

        public PierSide DestinationSideOfPier(double rightAscension, double declination)
        {
            LogMessage("DestinationSideOfPier Get", "Not implemented");
            throw new MethodNotImplementedException("DestinationSideOfPier");
        }

        public bool DoesRefraction
        {
            get
            {
                LogMessage("DoesRefraction Get", "Not implemented");
                throw new PropertyNotImplementedException("DoesRefraction", false);
            }
            set
            {
                LogMessage("DoesRefraction Set", "Not implemented");
                throw new PropertyNotImplementedException("DoesRefraction", true);
            }
        }

        public EquatorialCoordinateType EquatorialSystem
        {
            get
            {
                EquatorialCoordinateType equatorialSystem = EquatorialCoordinateType.equTopocentric;
                LogMessage("DeclinationRate", "Get - " + equatorialSystem.ToString());
                return equatorialSystem;
            }
        }

        public void FindHome()
        {
            LogMessage("FindHome", "Not implemented");
            throw new MethodNotImplementedException("FindHome");
        }

        public double FocalLength
        {
            get
            {
                LogMessage("FocalLength Get", "Not implemented");
                throw new PropertyNotImplementedException("FocalLength", false);
            }
        }

        public double GuideRateDeclination
        {
            get
            {
                LogMessage("GuideRateDeclination Get", "Not implemented");
                throw new PropertyNotImplementedException("GuideRateDeclination", false);
            }
            set
            {
                LogMessage("GuideRateDeclination Set", "Not implemented");
                throw new PropertyNotImplementedException("GuideRateDeclination", true);
            }
        }

        public double GuideRateRightAscension
        {
            get
            {
                LogMessage("GuideRateRightAscension Get", "Not implemented");
                throw new PropertyNotImplementedException("GuideRateRightAscension", false);
            }
            set
            {
                LogMessage("GuideRateRightAscension Set", "Not implemented");
                throw new PropertyNotImplementedException("GuideRateRightAscension", true);
            }
        }

        public bool IsPulseGuiding
        {
            get
            {
                //Todo implement this if I can make the new pulse guiding async
                LogMessage("IsPulseGuiding Get", "pulse guiding is synchronous for this driver");
                //throw new ASCOM.PropertyNotImplementedException("IsPulseGuiding", false);
                return false;
            }
        }

        private bool _movingPrimary;
        private bool _movingSecondary;

        public void MoveAxis(TelescopeAxes axis, double rate)
        {
            LogMessage("MoveAxis", $"Axis={axis} rate={rate}");
            CheckConnected("MoveAxis");

            var absRate = Math.Abs(rate);

            switch (absRate)
            {
                case 0:
                    //do nothing, it's ok this time as we're halting the slew.
                    break;
                case 1:
                    _sharedResourcesWrapper.SendBlind(":RG#");
                    //:RG# Set Slew rate to Guiding Rate (slowest)
                    //Returns: Nothing
                    break;
                case 2:
                    _sharedResourcesWrapper.SendBlind(":RC#");
                    //:RC# Set Slew rate to Centering rate (2nd slowest)
                    //Returns: Nothing
                    break;
                case 3:
                    _sharedResourcesWrapper.SendBlind(":RM#");
                    //:RM# Set Slew rate to Find Rate (2nd Fastest)
                    //Returns: Nothing
                    break;
                case 4:
                    _sharedResourcesWrapper.SendBlind(":RS#");
                    //:RS# Set Slew rate to max (fastest)
                    //Returns: Nothing
                    break;
                default:
                    throw new InvalidValueException($"Rate {rate} not supported");
            }

            switch (axis)
            {
                case TelescopeAxes.axisPrimary:
                    if (rate == 0)
                    {
                        _movingPrimary = false;
                        _sharedResourcesWrapper.SendBlind(":Qe#");
                        //:Qe# Halt eastward Slews
                        //Returns: Nothing
                        _sharedResourcesWrapper.SendBlind(":Qw#");
                        //:Qw# Halt westward Slews
                        //Returns: Nothing
                    }
                    else if (rate > 0)
                    {
                        _sharedResourcesWrapper.SendBlind(":Me#");
                        //:Me# Move Telescope East at current slew rate
                        //Returns: Nothing
                        _movingPrimary = true;
                    }
                    else
                    {
                        _sharedResourcesWrapper.SendBlind(":Mw#");
                        //:Mw# Move Telescope West at current slew rate
                        //Returns: Nothing
                        _movingPrimary = true;
                    }

                    break;
                case TelescopeAxes.axisSecondary:
                    if (rate == 0)
                    {
                        _movingSecondary = false;
                        _sharedResourcesWrapper.SendBlind(":Qn#");
                        //:Qn# Halt northward Slews
                        //Returns: Nothing
                        _sharedResourcesWrapper.SendBlind(":Qs#");
                        //:Qs# Halt southward Slews
                        //Returns: Nothing
                    }
                    else if (rate > 0)
                    {
                        _sharedResourcesWrapper.SendBlind(":Mn#");
                        //:Mn# Move Telescope North at current slew rate
                        //Returns: Nothing
                        _movingSecondary = true;
                    }
                    else
                    {
                        _sharedResourcesWrapper.SendBlind(":Ms#");
                        //:Ms# Move Telescope South at current slew rate
                        //Returns: Nothing
                        _movingSecondary = true;
                    }

                    break;
                default:
                    throw new InvalidValueException("Can not move this axis.");
            }
        }

        public void Park()
        {
            LogMessage("Park", "Parking telescope");
            CheckConnected("Park");

            if (AtPark)
                return;

            _sharedResourcesWrapper.SendBlind(":hP#");
            //:hP# Autostar, Autostar II and LX 16”Slew to Park Position
            //Returns: Nothing
            AtPark = true;
        }

        private bool _userNewerPulseGuiding = true;

        public void PulseGuide(GuideDirections direction, int duration)
        {
            LogMessage("PulseGuide", $"pulse guide direction {direction} duration {duration}");
            CheckConnected("PulseGuide");

            string d = string.Empty;
            switch (direction)
            {
                case GuideDirections.guideEast:
                    d = "e";
                    break;
                case GuideDirections.guideNorth:
                    d = "n";
                    break;
                case GuideDirections.guideSouth:
                    d = "s";
                    break;
                case GuideDirections.guideWest:
                    d = "w";
                    break;
            }

            if (_userNewerPulseGuiding)
            {
                _sharedResourcesWrapper.SendBlind($":Mg{d}{duration:0000}#");
                //:MgnDDDD#
                //:MgsDDDD#
                //:MgeDDDD#
                //:MgwDDDD#
                //Guide telescope in the commanded direction(nsew) for the number of milliseconds indicated by the unsigned number
                //passed in the command.These commands support serial port driven guiding.
                //Returns – Nothing
                //LX200 – Not Supported

                //todo implement IsPulseGuiding if WaitForMilliseconds is not needed
                _utilities.WaitForMilliseconds(duration); //todo figure out if this is really needed
            }
            else
            {
                _sharedResourcesWrapper.Lock(() =>
                {
                    _sharedResourcesWrapper.SendBlind(":RG#"); //Make sure we are at guide rate
                    //:RG# Set Slew rate to Guiding Rate (slowest)
                    //Returns: Nothing
                    _sharedResourcesWrapper.SendBlind($":M{d}#");
                    //:Me# Move Telescope East at current slew rate
                    //Returns: Nothing
                    //:Mn# Move Telescope North at current slew rate
                    //Returns: Nothing
                    //:Ms# Move Telescope South at current slew rate
                    //Returns: Nothing
                    //:Mw# Move Telescope West at current slew rate
                    //Returns: Nothing
                    _utilities.WaitForMilliseconds(duration);
                    _sharedResourcesWrapper.SendBlind($":Q{d}#");
                    //:Qe# Halt eastward Slews
                    //Returns: Nothing
                    //:Qn# Halt northward Slews
                    //Returns: Nothing
                    //:Qs# Halt southward Slews
                    //Returns: Nothing
                    //:Qw# Halt westward Slews
                    //Returns: Nothing
                });
            }
        }

        public double RightAscension
        {
            get
            {
                CheckConnected("RightAscension Get");
                var result = _sharedResourcesWrapper.SendString(":GR#");
                //:GR# Get Telescope RA
                //Returns: HH: MM.T# or HH:MM:SS#
                //Depending which precision is set for the telescope

                double rightAscension = _utilities.HMSToHours(result);

                LogMessage("RightAscension", "Get - " + _utilitiesExtra.HoursToHMS(rightAscension));
                return rightAscension;
            }
        }

        public double RightAscensionRate
        {
            get
            {
                double rightAscensionRate = 0.0;
                LogMessage("RightAscensionRate", "Get - " + rightAscensionRate.ToString(CultureInfo.InvariantCulture));
                return rightAscensionRate;
            }
            set
            {
                LogMessage("RightAscensionRate Set", "Not implemented");
                throw new PropertyNotImplementedException("RightAscensionRate", true);
            }
        }

        public void SetPark()
        {
            LogMessage("SetPark", "Not implemented");
            throw new MethodNotImplementedException("SetPark");
        }

        public PierSide SideOfPier
        {
            get
            {
                LogMessage("SideOfPier Get", "Not implemented");
                throw new PropertyNotImplementedException("SideOfPier", false);
            }
            set
            {
                LogMessage("SideOfPier Set", "Not implemented");
                throw new PropertyNotImplementedException("SideOfPier", true);
            }
        }

        public double SiderealTime
        {
            get
            {
                // Now using NOVAS 3.1
                double siderealTime = 0.0;
                using (var novas = new Astrometry.NOVAS.NOVAS31())
                {
                    var jd = _utilities.DateUTCToJulian(DateTime.UtcNow);
                    novas.SiderealTime(jd, 0, novas.DeltaT(jd),
                        Astrometry.GstType.GreenwichApparentSiderealTime,
                        Astrometry.Method.EquinoxBased,
                        Astrometry.Accuracy.Reduced, ref siderealTime);
                }

                // Allow for the longitude
                siderealTime += SiteLongitude / 360.0 * 24.0;

                // Reduce to the range 0 to 24 hours
                siderealTime = _astroUtilities.ConditionRA(siderealTime);

                LogMessage("SiderealTime", "Get - " + siderealTime.ToString(CultureInfo.InvariantCulture));
                return siderealTime;
            }
        }

        public double SiteElevation
        {
            get
            {
                LogMessage("SiteElevation Get", "Not implemented");
                throw new PropertyNotImplementedException("SiteElevation", false);
            }
            set
            {
                LogMessage("SiteElevation Set", "Not implemented");
                throw new PropertyNotImplementedException("SiteElevation", true);
            }
        }

        public double SiteLatitude
        {
            get
            {
                CheckConnected("SiteLatitude Get");

                var latitude = _sharedResourcesWrapper.SendString(":Gt#");
                //:Gt# Get Current Site Latitude
                //Returns: sDD* MM#
                //The latitude of the current site. Positive inplies North latitude.

                var siteLatitude = _utilities.DMSToDegrees(latitude);
                LogMessage("SiteLatitude Get", $"{_utilitiesExtra.DegreesToDMS(siteLatitude)}");
                return siteLatitude;
            }
            set
            {
                LogMessage("SiteLatitude Set", $"{_utilitiesExtra.DegreesToDMS(value)}");

                CheckConnected("SiteLatitude Set");

                if (value > 90)
                    throw new InvalidValueException("Latitude cannot be greater than 90 degrees.");

                if (value < -90)
                    throw new InvalidValueException("Latitude cannot be less than -90 degrees.");

                string sign = value > 0 ? "+" : "-";

                var absValue = Math.Abs(value);
                int d = Convert.ToInt32(Math.Floor(absValue));
                int m = Convert.ToInt32(60 * (absValue - d));
                var commandString = $":St{sign}{d:00}*{m:00}#";

                var result = _sharedResourcesWrapper.SendChar(commandString);
                //:StsDD*MM#
                //Sets the current site latitude to sDD* MM#
                //Returns:
                //0 – Invalid
                //1 - Valid
                if (result != "1")
                    throw new InvalidOperationException("Failed to set site latitude.");
            }
        }

        public double SiteLongitude
        {
            get
            {
                CheckConnected("SiteLongitude Get");

                var longitude = _sharedResourcesWrapper.SendString(":Gg#");
                //:Gg# Get Current Site Longitude
                //Returns: sDDD*MM#
                //The current site Longitude. East Longitudes are expressed as negative
                double siteLongitude = _utilities.DMSToDegrees(longitude);

                if (siteLongitude > 180)
                    siteLongitude = siteLongitude - 360;

                siteLongitude = -siteLongitude;

                LogMessage("SiteLongitude Get", $"{_utilitiesExtra.DegreesToDMS(siteLongitude)}");
                return siteLongitude;
            }
            set
            {
                var newLongitude = value;

                LogMessage("SiteLongitude Set", $"{_utilitiesExtra.DegreesToDMS(newLongitude)}");

                CheckConnected("SiteLongitude Set");

                if (newLongitude > 180)
                    throw new InvalidValueException("Longitude cannot be greater than 180 degrees.");

                if (newLongitude < -180)
                    throw new InvalidValueException("Longitude cannot be lower than -180 degrees.");

                if (newLongitude > 0)
                    newLongitude = 360 - newLongitude;

                newLongitude = Math.Abs(newLongitude);

                int d = Convert.ToInt32(Math.Floor(newLongitude));
                int m = Convert.ToInt32(60 * (newLongitude - d));

                var commandstring = $":Sg{d:000}*{m:00}#";

                var result = _sharedResourcesWrapper.SendChar(commandstring);
                //:SgDDD*MM#
                //Set current site’s longitude to DDD*MM an ASCII position string
                //Returns:
                //0 – Invalid
                //1 - Valid
                if (result != "1")
                    throw new InvalidOperationException("Failed to set site longitude.");
            }
        }

        public short SlewSettleTime
        {
            get
            {
                LogMessage("SlewSettleTime Get", "Not implemented");
                throw new PropertyNotImplementedException("SlewSettleTime", false);
            }
            set
            {
                LogMessage("SlewSettleTime Set", "Not implemented");
                throw new PropertyNotImplementedException("SlewSettleTime", true);
            }
        }

        public void SlewToAltAz(double azimuth, double altitude)
        {
            LogMessage("SlewToAltAz", $"Az=~{azimuth} Alt={altitude}");
            CheckConnected("SlewToAltAz");

            SlewToAltAzAsync(azimuth, altitude);

            while (Slewing) //wait for slew to complete
            {
                _utilities.WaitForMilliseconds(200); //be responsive to AbortSlew();
            }
        }

        private double TargetAltitude
        {
            set
            {
                if (value > 90)
                    throw new InvalidValueException("Altitude cannot be greater than 90.");

                if (value < 0)
                    throw new InvalidValueException("Altitide cannot be less than 0.");

                CheckConnected("TargetAltitude Set");

                //todo this serial string does not work.  Calculate the EQ version instead.

                var dms = _utilities.DegreesToDMS(value, "*", "'", "",0);
                var s = value < 0 ? string.Empty : "+";

                var result = _sharedResourcesWrapper.SendChar($":Sa{s}{dms}#");
                //:SasDD*MM#
                //Set target object altitude to sDD*MM# or sDD*MM’SS# [LX 16”, Autostar, Autostar II]
                //Returns:
                //1 Object within slew range
                //0 Object out of slew range

                if (result == "0")
                    throw new InvalidOperationException("Target altitude out of slew range");
            }
        }

        private double TargetAzimuth
        {
            set
            {
                if (value >= 360)
                    throw new InvalidValueException("Azimuth cannot be 360 or higher.");

                if (value < 0)
                    throw new InvalidValueException("Azimuth cannot be less than 0.");

                CheckConnected("TargetAzimuth Set");

                //todo this serial string does not work.  Calculate the EQ version instead.

                var dms = _utilitiesExtra.DegreesToDM(value, "*" );

                var result = _sharedResourcesWrapper.SendChar($":Sz{dms}#");
                //:SzDDD*MM#
                //Sets the target Object Azimuth[LX 16” and Autostar II only]
                //Returns:
                //0 – Invalid
                //1 - Valid

                if (result == "0")
                    throw new InvalidOperationException("Target Azimuth out of slew range");

            }
        }

        public void SlewToAltAzAsync(double azimuth, double altitude)
        {
            CheckConnected("SlewToAltAzAsync");

            if (altitude > 90)
                throw new InvalidValueException("Altitude cannot be greater than 90.");

            if (altitude < 0)
                throw new InvalidValueException("Altitide cannot be less than 0.");

            if (azimuth >= 360)
                throw new InvalidValueException("Azimuth cannot be 360 or higher.");

            if (azimuth < 0)
                throw new InvalidValueException("Azimuth cannot be less than 0.");

            LogMessage("SlewToAltAzAsync", $"Az={azimuth} Alt={altitude}");
            
            HorizonCoordinates altAz = new HorizonCoordinates();
            altAz.Azimuth = azimuth;
            altAz.Altitude = altitude;

            var utcDateTime = UTCDate;
            var latitude = SiteLatitude;
            var longitude = SiteLongitude;

            _sharedResourcesWrapper.Lock(() =>
            {
                var raDec = _astroMaths.ConvertHozToEq(utcDateTime, latitude, longitude, altAz);

                TargetRightAscension = raDec.RightAscension;
                TargetDeclination = raDec.Declination;

                DoSlewAsync(true);

                //TargetAltitude = altitude;
                //TargetAzimuth = azimuth;

                //DoSlewAsync(false);
            });
        }

        private void DoSlewAsync(bool polar)
        {
            CheckConnected("DoSlewAsync");

            _sharedResourcesWrapper.Lock(() =>
            {
                switch (polar)
                {
                    case true:
                        var response = _sharedResourcesWrapper.SendChar(":MS#");
                        //:MS# Slew to Target Object
                        //Returns:
                        //0 Slew is Possible
                        //1<string># Object Below Horizon w/string message
                        //2<string># Object Below Higher w/string message

                        switch (response)
                        {
                            case "0":
                                //We're slewing everything should be working just fine.
                                LogMessage("DoSlewAsync", "Slewing to target");
                                break;
                            case "1":
                                //Below Horizon 
                                string belowHorizonMessage = _sharedResourcesWrapper.ReadTerminated();
                                LogMessage("DoSlewAsync", $"Slew failed \"{belowHorizonMessage}\"");
                                throw new InvalidOperationException(belowHorizonMessage);
                            case "2":
                                //Below Horizon 
                                string belowMinimumElevationMessage = _sharedResourcesWrapper.ReadTerminated();
                                LogMessage("DoSlewAsync", $"Slew failed \"{belowMinimumElevationMessage}\"");
                                throw new InvalidOperationException(belowMinimumElevationMessage);
                            default:
                                LogMessage("DoSlewAsync", $"Slew failed - unknown response \"{response}\"");
                                throw new DriverException("This error should not happen");

                        }

                        break;
                    case false:
                        var maResponse = _sharedResourcesWrapper.SendChar(":MA#");
                        //:MA# Autostar, LX 16”, Autostar II – Slew to target Alt and Az
                        //Returns:
                        //0 - No fault
                        //1 – Fault
                        //    LX200 – Not supported

                        if (maResponse == "1")
                        {
                            throw new InvalidOperationException("fault");
                        }

                        break;
                }
            });
        }

        public void SlewToCoordinates(double rightAscension, double declination)
        {
            LogMessage("SlewToCoordinates", $"Ra={rightAscension}, Dec={declination}");
            CheckConnected("SlewToCoordinates");

            SlewToCoordinatesAsync(rightAscension, declination);

            while (Slewing) //wait for slew to complete
            {
                _utilities.WaitForMilliseconds(200); //be responsive to AbortSlew();
            }

            LogMessage("SlewToCoordinates", $"Slewing completed new coordinates Ra={RightAscension}, Dec={Declination}");
        }

        public void SlewToCoordinatesAsync(double rightAscension, double declination)
        {
            LogMessage("SlewToCoordinatesAsync", $"Ra={rightAscension}, Dec={declination}");
            CheckConnected("SlewToCoordinatesAsync");

            _sharedResourcesWrapper.Lock(() =>
                {
                    TargetRightAscension = rightAscension;
                    TargetDeclination = declination;

                    DoSlewAsync(true);
                }
            );
        }

        public void SlewToTarget()
        {
            LogMessage("SlewToTarget", "Executing");
            CheckConnected("SlewToTarget");
            SlewToTargetAsync();

            while (Slewing)
            {
                _utilities.WaitForMilliseconds(200);
            }
        }

        private const double InvalidParameter = -1000;

        public void SlewToTargetAsync()
        {
            CheckConnected("SlewToTargetAsync");

            if (TargetDeclination == InvalidParameter || TargetRightAscension == InvalidParameter)
                throw new InvalidOperationException("No target selected to slew to.");

            DoSlewAsync(true);
        }

        private bool MovingAxis()
        {
            return _movingPrimary || _movingSecondary;
        }

        public bool Slewing
        {
            get
            {
                if (!Connected) return false;


                if (MovingAxis())
                    return true;

                CheckConnected("Slewing Get");

                var result = _sharedResourcesWrapper.SendString(":D#");
                //:D# Requests a string of bars indicating the distance to the current target location.
                //Returns:
                //LX200's – a string of bar characters indicating the distance.
                //Autostars and Autostar II – a string containing one bar until a slew is complete, then a null string is returned.

                if (result == null)
                {
                    return false;
                }

                bool isSlewing = result != string.Empty;

                LogMessage("Slewing Get", $"Result = {isSlewing}");
                return isSlewing;
            }
        }

        public void SyncToAltAz(double azimuth, double altitude)
        {
            LogMessage("SyncToAltAz", "Not implemented");
            throw new MethodNotImplementedException("SyncToAltAz");
        }

        public void SyncToCoordinates(double rightAscension, double declination)
        {
            LogMessage("SyncToCoordinates", $"RA={rightAscension} Dec={declination}");
            CheckConnected("SyncToCoordinates");

            _sharedResourcesWrapper.Lock(() =>
            {
                TargetRightAscension = rightAscension;
                TargetDeclination = declination;

                SyncToTarget();
            });
        }

        public void SyncToTarget()
        {
            LogMessage("SyncToTarget", "Executing");
            CheckConnected("SyncToTarget");

            var result = _sharedResourcesWrapper.SendString(":CM#");
            //:CM# Synchronizes the telescope's position with the currently selected database object's coordinates.
            //Returns:
            //LX200's - a "#" terminated string with the name of the object that was synced.
            //    Autostars & Autostar II - A static string: " M31 EX GAL MAG 3.5 SZ178.0'#"

            if (result == string.Empty)
                throw new InvalidOperationException("Unable to perform sync");
        }

        private double _targetDeclination = InvalidParameter;
        public double TargetDeclination
        {
            get
            {
                if (_targetDeclination == InvalidParameter)
                    throw new InvalidOperationException("Target not set");

                //var result = SerialPort.CommandTerminated(":Gd#", "#");
                ////:Gd# Get Currently Selected Object/Target Declination
                ////Returns: sDD* MM# or sDD*MM’SS#
                ////Depending upon the current precision setting for the telescope.

                //double targetDec = DmsToDouble(result);

                //return targetDec;

                LogMessage("TargetDeclination Get", $"{_targetDeclination}");
                return _targetDeclination;
            }
            set
            {
                LogMessage("TargetDeclination Set", $"{value}");

                CheckConnected("TargetDeclination Set");

                //todo implement low precision version of this.
                if (value > 90)
                    throw new InvalidValueException("Declination cannot be greater than 90.");

                if (value < -90)
                    throw new InvalidValueException("Declination cannot be less than -90.");
                
                var dms = _utilities.DegreesToDMS(value, "*", ":", ":", 2);
                var s = value < 0 ? string.Empty : "+";

                var command = $":Sd{s}{dms}#";

                LogMessage("TargetDeclination Set", $"{command}");
                var result = _sharedResourcesWrapper.SendChar(command);
                //:SdsDD*MM#
                //Set target object declination to sDD*MM or sDD*MM:SS depending on the current precision setting
                //Returns:
                //1 - Dec Accepted
                //0 – Dec invalid

                if (result == "0")
                {
                    throw new InvalidOperationException("Target declination invalid");
                }

                _targetDeclination = value;
            }
        }

        private double _targetRightAscension = InvalidParameter;
        public double TargetRightAscension
        {
            get
            {
                if (_targetRightAscension == InvalidParameter)
                    throw new InvalidOperationException("Target not set");

                //var result = SerialPort.CommandTerminated(":Gr#", "#");
                ////:Gr# Get current/target object RA
                ////Returns: HH: MM.T# or HH:MM:SS
                ////Depending upon which precision is set for the telescope

                //double targetRa = HmsToDouble(result);
                //return targetRa;

                LogMessage("TargetRightAscension Get", $"{_targetRightAscension}");
                return _targetRightAscension;
            }
            set
            {
                LogMessage("TargetRightAscension Set", $"{value}");
                CheckConnected("TargetRightAscension Set");

                if (value < 0)
                    throw new InvalidValueException("Right ascension value cannot be below 0");

                if (value >= 24)
                    throw new InvalidValueException("Right ascension value cannot be greater than 23:59:59");
                //todo implement the low precision version

                var hms = _utilities.HoursToHMS(value, ":", ":", ":", 2);
                var response = _sharedResourcesWrapper.SendChar($":Sr{hms}#");
                //:SrHH:MM.T#
                //:SrHH:MM:SS#
                //Set target object RA to HH:MM.T or HH: MM: SS depending on the current precision setting.
                //    Returns:
                //0 – Invalid
                //1 - Valid

                if (response == "0")
                    throw new InvalidOperationException("Failed to set TargetRightAscension.");

                _targetRightAscension = value;
            }
        }

        private bool _tracking = true;
        public bool Tracking
        {
            get
            {
                LogMessage("Tracking", $"Get - {_tracking}");
                return _tracking;
            }
            set
            {
                LogMessage($"Tracking Set", $"{value}");
                _tracking = value;
            }
        }

        private DriveRates _trackingRate = DriveRates.driveSidereal;

        public DriveRates TrackingRate
        {
            get
            {
                //todo implement this with the GW command
                //var result = SerialPort.CommandTerminated(":GT#", "#");

                //double rate = double.Parse(result);


                //if (rate == 60.1)
                //    return DriveRates.driveLunar;
                //else if (rate == 60.1)
                //    return DriveRates.driveSidereal;

                //return DriveRates.driveKing;
                LogMessage("TrackingRate Get", $"{_trackingRate}");
                return _trackingRate;
            }
            set
            {
                LogMessage("TrackingRate Set", $"{value}");
                CheckConnected("TrackingRate Set");

                switch (value)
                {
                    case DriveRates.driveSidereal:
                        _sharedResourcesWrapper.SendBlind(":TQ#");
                        //:TQ# Selects sidereal tracking rate
                        //Returns: Nothing
                        break;
                    case DriveRates.driveLunar:
                        _sharedResourcesWrapper.SendBlind(":TL#");
                        //:TL# Set Lunar Tracking Rage
                        //Returns: Nothing
                        break;
                    //case DriveRates.driveSolar:
                    //    SerialPort.Command(":TS#");
                    //    //:TS# Select Solar tracking rate. [LS Only]
                    //    //Returns: Nothing
                    //    break;
                    //case DriveRates.driveKing:
                        //:TM# Select custom tracking rate [ no-op in Autostar II]
                        //Returns: Nothing
                    //    break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }

                _trackingRate = value;
            }
        }

        public ITrackingRates TrackingRates
        {
            get
            {
                ITrackingRates trackingRates = new TrackingRates();
                LogMessage("TrackingRates", "Get - ");
                foreach (DriveRates driveRate in trackingRates)
                {
                    LogMessage("TrackingRates", "Get - " + driveRate.ToString());
                }
                return trackingRates;
            }
        }

        private TimeSpan GetUtcCorrection()
        {
            string utcOffSet = _sharedResourcesWrapper.SendString(":GG#");
            //:GG# Get UTC offset time
            //Returns: sHH# or sHH.H#
            //The number of decimal hours to add to local time to convert it to UTC. If the number is a whole number the
            //sHH# form is returned, otherwise the longer form is returned.
            double utcOffsetHours = double.Parse(utcOffSet);
            TimeSpan utcCorrection = TimeSpan.FromHours(utcOffsetHours);
            return utcCorrection;
        }

        public class TelescopeDateDetails
        {
            public string TelescopeDate { get; set; }
            public string TelescopeTime { get; set; }
            public TimeSpan UtcCorrection { get; set; }
        }

        public DateTime UTCDate
        {
            get
            {
                CheckConnected("UTCDate Get");

                LogMessage("UTCDate", "Get started");

                TelescopeDateDetails telescopeDateDetails = _sharedResourcesWrapper.Lock(() =>
                {
                    TelescopeDateDetails tdd = new TelescopeDateDetails();
                    tdd.TelescopeDate = _sharedResourcesWrapper.SendString(":GC#");
                    //:GC# Get current date.
                    //Returns: MM/DD/YY#
                    //The current local calendar date for the telescope.
                    tdd.TelescopeTime = _sharedResourcesWrapper.SendString(":GL#");
                    //:GL# Get Local Time in 24 hour format
                    //Returns: HH:MM:SS#
                    //The Local Time in 24 - hour Format
                    tdd.UtcCorrection = GetUtcCorrection();

                    return tdd;
                });

                int month = telescopeDateDetails.TelescopeDate.Substring(0, 2).ToInteger();
                int day = telescopeDateDetails.TelescopeDate.Substring(3, 2).ToInteger();
                int year = telescopeDateDetails.TelescopeDate.Substring(6, 2).ToInteger();

                if (year < 2000) //todo fix this hack that will create a Y2K100 bug
                {
                    year = year + 2000;
                }

                int hour = telescopeDateDetails.TelescopeTime.Substring(0, 2).ToInteger();
                int minute = telescopeDateDetails.TelescopeTime.Substring(3, 2).ToInteger();
                int second = telescopeDateDetails.TelescopeTime.Substring(6, 2).ToInteger();

                var utcDate = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc) +
                              telescopeDateDetails.UtcCorrection;

                LogMessage("UTCDate", "Get - " + utcDate.ToString("MM/dd/yy HH:mm:ss"));

                return utcDate;
            }
            set
            {
                LogMessage("UTCDate", "Set - " + value.ToString("MM/dd/yy HH:mm:ss"));

                CheckConnected("UTCDate Set");

                _sharedResourcesWrapper.Lock(() =>
                {
                    var utcCorrection = GetUtcCorrection();
                    var localDateTime = value - utcCorrection;

                    string localStingCommand = $":SL{localDateTime:HH:mm:ss}#";
                    var timeResult = _sharedResourcesWrapper.SendChar(localStingCommand);
                    //:SLHH:MM:SS#
                    //Set the local Time
                    //Returns:
                    //0 – Invalid
                    //1 - Valid
                    if (timeResult != "1")
                    {
                        throw new InvalidOperationException("Failed to set local time");
                    }

                    string localDateCommand = $":SC{localDateTime:MM/dd/yy}#";
                    var dateResult = _sharedResourcesWrapper.SendChar(localDateCommand);
                    //:SCMM/DD/YY#
                    //Change Handbox Date to MM/DD/YY
                    //Returns: <D><string>
                    //D = ‘0’ if the date is invalid.The string is the null string.
                    //D = ‘1’ for valid dates and the string is “Updating Planetary Data#                       #”
                    //Note: For Autostar II this is the UTC data!
                    if (dateResult != "1")
                    {
                        throw new InvalidOperationException("Failed to set local date");
                    }

                    //throwing away these two strings which represent 
                    _sharedResourcesWrapper.ReadTerminated(); //Updating Planetary Data#
                    _sharedResourcesWrapper.ReadTerminated(); //                       #
                });
            }
        }

        public void Unpark()
        {
            LogMessage("Unpark", "Not implemented");
            throw new MethodNotImplementedException("Unpark");
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
                p.DeviceType = "Telescope";
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
                throw new NotConnectedException($"Not connected to telescope when trying to execute: {message}");
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {
            var profileProperties = _sharedResourcesWrapper.ReadProfile();
            _tl.Enabled = profileProperties.TraceLogger;
            _comPort = profileProperties.ComPort;
        }

        /// <summary>
        /// Log helper function that takes formatted strings and arguments
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        internal void LogMessage(string identifier, string message, params object[] args)
        {
            var msg = string.Format(message, args);
            _tl.LogMessage(identifier, msg);
        }
        #endregion
    }
}

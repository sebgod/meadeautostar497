//tabs=4
// --------------------------------------------------------------------------------
// TODO fill in this information for your driver, then remove this line!
//
// ASCOM Telescope driver for Meade.net
//
// Description:	Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam 
//				nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam 
//				erat, sed diam voluptua. At vero eos et accusam et justo duo 
//				dolores et ea rebum. Stet clita kasd gubergren, no sea takimata 
//				sanctus est Lorem ipsum dolor sit amet.
//
// Implements:	ASCOM Telescope interface version: <To be completed by driver developer>
// Author:		(XXX) Your N. Here <your@email.here>
//
// Edit Log:
//
// Date			Who	Vers	Description
// -----------	---	-----	-------------------------------------------------------
// dd-mmm-yyyy	XXX	6.0.0	Initial edit, created from ASCOM driver template
// --------------------------------------------------------------------------------
//


// This is used to define code in the template that is specific to one class implementation
// unused code canbe deleted and this definition removed.
#define Telescope

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

using ASCOM;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;
using System.Reflection;

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
        internal static string driverID = Marshal.GenerateProgIdForType(MethodBase.GetCurrentMethod().DeclaringType);

        // TODO Change the descriptive string for your driver then remove this line
        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        private static string driverDescription = "Meade Generic";

        internal static string comPortProfileName = "COM Port"; // Constants used for Profile persistence
        internal static string comPortDefault = "COM1";
        internal static string traceStateProfileName = "Trace Level";
        internal static string traceStateDefault = "false";

        internal static string comPort; // Variables to hold the currrent device configuration

        /// <summary>
        /// Private variable to hold the connected state
        /// </summary>
        private bool _connectedState;

        /// <summary>
        /// Private variable to hold an ASCOM Utilities object
        /// </summary>
        private Util utilities;

        /// <summary>
        /// Private variable to hold an ASCOM AstroUtilities object to provide the Range method
        /// </summary>
        private AstroUtils astroUtilities;

        private readonly AstroMaths _astroMaths;

        /// <summary>
        /// Variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
        /// </summary>
        internal static TraceLogger tl;

        /// <summary>
        /// Initializes a new instance of the <see cref="Meade.net"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public Telescope()
        {
            tl = new TraceLogger("", "Meade.net.Telescope");
            ReadProfile(); // Read device configuration from the ASCOM Profile store

            tl.LogMessage("Telescope", "Starting initialisation");

            _connectedState = false; // Initialise connected to false
            utilities = new Util(); //Initialise util object
            astroUtilities = new AstroUtils(); // Initialise astro utilities object

            //TODO: Implement your additional construction here
            _astroMaths = new AstroMaths();

            tl.LogMessage("Telescope", "Completed initialisation");
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
            tl.LogMessage("SetupDialog", "Opening setup dialog");
            SharedResources.SetupDialog();
            ReadProfile();
            tl.LogMessage("SetupDialog", "complete");
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
                tl.LogMessage("SupportedActions Get", "Returning empty arraylist");
                return new ArrayList();
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            LogMessage("", "Action {0}, parameters {1} not implemented", actionName, actionParameters);
            throw new ASCOM.ActionNotImplementedException("Action " + actionName +
                                                          " is not implemented by this driver");
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            // Call CommandString and return as soon as it finishes
            //this.CommandString(command, raw);
            SharedResources.SendBlind(command);
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
            throw new ASCOM.MethodNotImplementedException("CommandBool");
            // DO NOT have both these sections!  One or the other
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            // it's a good idea to put all the low level communication with the device here,
            // then all communication calls this function
            // you need something to ensure that only one command is in progress at a time
            return SharedResources.SendString(command);
            //throw new ASCOM.MethodNotImplementedException("CommandString");
        }

        public void Dispose()
        {
            // Clean up the tracelogger and util objects
            tl.Enabled = false;
            tl.Dispose();
            tl = null;
            utilities.Dispose();
            utilities = null;
            astroUtilities.Dispose();
            astroUtilities = null;
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
                tl.LogMessage("Connected", "Set {0}", value);
                if (value == IsConnected)
                    return;

                if (value)
                {
                    LogMessage("Connected Set", "Connecting to port {0}", comPort);
                    try
                    {
                        SharedResources.Connect("Serial");
                        try
                        {
                            SelectSite(1);
                            SetLongFormat(true);

                            _connectedState = true;
                        }
                        catch (Exception)
                        {
                            SharedResources.Disconnect("Serial");
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage("Connected Set", "Error connecting to port {0} - {1}", comPort, ex.Message);
                    }
                }
                else
                {
                    LogMessage("Connected Set", "Disconnecting from port {0}", comPort);
                    SharedResources.Disconnect("Serial");
                    _connectedState = false;
                }
            }
        }

        private void SetLongFormat(bool setLongFormat)
        {
            SharedResources.Lock(() =>
            {
                var result = SharedResources.SendString(":GZ#");
                //:GZ# Get telescope azimuth
                //Returns: DDD*MM#T or DDD*MM�SS#
                //The current telescope Azimuth depending on the selected precision.

                bool isLongFormat = result.Length > 6;

                if (isLongFormat != setLongFormat)
                {
                    utilities.WaitForMilliseconds(500);
                    SharedResources.SendBlind(":U#");
                    //:U# Toggle between low/hi precision positions
                    //Low - RA displays and messages HH:MM.T sDD*MM
                    //High - Dec / Az / El displays and messages HH:MM: SS sDD*MM:SS
                    //    Returns Nothing
                }
            });
        }

        private void SelectSite(int site)
        {
            SharedResources.SendBlind($":W{site}#");
            //:W<n>#
            //Set current site to<n>, an ASCII digit in the range 1..4
            //Returns: Nothing
        }

        public string Description
        {
            // TODO customise this device description
            get
            {
                tl.LogMessage("Description Get", driverDescription);
                return driverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                // TODO customise this driver description
                string driverInfo = "Information about the driver itself. Version: " +
                                    String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major,
                                        version.Minor);
                tl.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion =
                    String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverVersion Get", driverVersion);
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

                //var telescopeProduceName = SharedResources.SendString(":GVP#");
                ////:GVP# Get Telescope Product Name
                ////Returns: <string>#

                //var firmwareVersion = SharedResources.SendString(":GVN#");
                ////:GVN# Get Telescope Firmware Number
                ////Returns: dd.d#

                //string name = $"{telescopeProduceName} - {firmwareVersion}";
                string name = driverDescription;
                tl.LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        #region ITelescope Implementation
        
        public void AbortSlew()
        {
            CheckConnected("AbortSlew");

            tl.LogMessage("AbortSlew", "Aborting slew");
            SharedResources.SendBlind(":Q#");
            //:Q# Halt all current slewing
            //Returns:Nothing
        }

        public AlignmentModes AlignmentMode
        {
            get
            {
                tl.LogMessage("AlignmentMode Get", "Getting alignmode");

                CheckConnected("AlignmentMode Get");

                const char ack = (char) 6;

                var alignmentString = SharedResources.SendChar(ack.ToString());
                //ACK <0x06> Query of alignment mounting mode.
                //Returns:
                //A If scope in AltAz Mode
                //D If scope is currently in the Downloader[Autostar II & Autostar]
                //L If scope in Land Mode
                //P If scope in Polar Mode

                //todo implement GW Command
                //var alignmentString = SerialPort.CommandTerminated(":GW#", "#");
                //:GW# Get Scope Alignment Status
                //Returns: <mount><tracking><alignment>#
                //    where:
                //mount: A - AzEl mounted, P - Equatorially mounted, G - german mounted equatorial
                //tracking: T - tracking, N - not tracking
                //alignment: 0 - needs alignment, 1 - one star aligned, 2 - two star aligned, 3 - three star aligned.

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

                tl.LogMessage("AlignmentMode Get", $"alignmode = {alignmentMode}");
                return alignmentMode;
            }
            set
            {
                CheckConnected("AlignmentMode Set");

                switch (value)
                {
                    case AlignmentModes.algAltAz:
                        SharedResources.SendBlind(":AA#");
                        //:AA# Sets telescope the AltAz alignment mode
                        //Returns: nothing
                        break;
                    case AlignmentModes.algPolar:
                    case AlignmentModes.algGermanPolar:
                        SharedResources.SendBlind(":AP#");
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
                CheckConnected("Altitude get");

                var altAz = CalcAltAzFromTelescopeEqData();
                tl.LogMessage("Altitude", $"{altAz.Altitude}");
                return altAz.Altitude;

                ////todo firmware bug in 44Eg, :GA# is returning the dec, not the altitude!
                //var result = SharedResources.SendString(":GA#");
                ////:GA# Get Telescope Altitude
                ////Returns: sDD* MM# or sDD*MM�SS#
                ////The current scope altitude. The returned format depending on the current precision setting.

                //var alt = utilities.DMSToDegrees(result);
                //tl.LogMessage("Altitude", $"{alt}");
                //return alt;

                //tl.LogMessage("Altitude Get", "Not implemented");
                //throw new ASCOM.PropertyNotImplementedException("Altitude", false);
            }
        }

        private HorizonCoordinates CalcAltAzFromTelescopeEqData()
        {
            var altitudeData = SharedResources.Lock(() => new AltitudeData
            {
                UtcDateTime = this.UTCDate,
                SiteLongitude = this.SiteLongitude,
                SiteLatitude = this.SiteLatitude,
                equatorialCoordinates = new EquatorialCoordinates()
                {
                    RightAscension = this.RightAscension,
                    Declination = this.Declination
                }
            });

            double hourAngle = _astroMaths.RightAscensionToHourAngle(altitudeData.UtcDateTime, altitudeData.SiteLongitude,
                altitudeData.equatorialCoordinates.RightAscension);
            var altAz = _astroMaths.ConvertEqToHoz(hourAngle, altitudeData.SiteLatitude, altitudeData.equatorialCoordinates);
            return altAz;
        }

        public double ApertureArea
        {
            get
            {
                tl.LogMessage("ApertureArea Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("ApertureArea", false);
            }
        }

        public double ApertureDiameter
        {
            get
            {
                tl.LogMessage("ApertureDiameter Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("ApertureDiameter", false);
            }
        }

        public bool AtHome
        {
            get
            {
                tl.LogMessage("AtHome", "Get - " + false.ToString());
                return false;
            }
        }

        private bool _atPark = false;

        public bool AtPark
        {
            get
            {
                tl.LogMessage("AtPark", "Get - " + _atPark);
                return _atPark;
            }
            private set { _atPark = value; }
        }

        public IAxisRates AxisRates(TelescopeAxes Axis)
        {
            tl.LogMessage("AxisRates", "Get - " + Axis.ToString());
            return new AxisRates(Axis);
        }

        public double Azimuth
        {
            get
            {
                CheckConnected("Azimuth get");

                //var result = SharedResources.SendString(":GZ#");
                //:GZ# Get telescope azimuth
                //Returns: DDD*MM#T or DDD*MM�SS#
                //The current telescope Azimuth depending on the selected precision.

                //double az = utilities.DMSToDegrees(result);

                //tl.LogMessage("Azimuth Get", $"{az}");
                //return az;

                var altAz = CalcAltAzFromTelescopeEqData();
                tl.LogMessage("Azimuth Get", $"{altAz.Azimuth}");
                return altAz.Azimuth;
            }
        }

        public bool CanFindHome
        {
            get
            {
                tl.LogMessage("CanFindHome", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanMoveAxis(TelescopeAxes Axis)
        {
            tl.LogMessage("CanMoveAxis", "Get - " + Axis.ToString());
            switch (Axis)
            {
                case TelescopeAxes.axisPrimary: return true; //RA or AZ
                case TelescopeAxes.axisSecondary: return true; //Dev or Alt
                case TelescopeAxes.axisTertiary: return false; //rotator / derotator
                default: throw new InvalidValueException("CanMoveAxis", Axis.ToString(), "0 to 2");
            }
        }

        public bool CanPark
        {
            get
            {
                tl.LogMessage("CanPark", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanPulseGuide
        {
            get
            {
                tl.LogMessage("CanPulseGuide", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSetDeclinationRate
        {
            get
            {
                tl.LogMessage("CanSetDeclinationRate", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetGuideRates
        {
            get
            {
                tl.LogMessage("CanSetGuideRates", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetPark
        {
            get
            {
                tl.LogMessage("CanSetPark", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetPierSide
        {
            get
            {
                tl.LogMessage("CanSetPierSide", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetRightAscensionRate
        {
            get
            {
                tl.LogMessage("CanSetRightAscensionRate", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanSetTracking
        {
            get
            {
                tl.LogMessage("CanSetTracking", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSlew
        {
            get
            {
                tl.LogMessage("CanSlew", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSlewAltAz
        {
            get
            {
                tl.LogMessage("CanSlewAltAz", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSlewAltAzAsync
        {
            get
            {
                tl.LogMessage("CanSlewAltAzAsync", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSlewAsync
        {
            get
            {
                tl.LogMessage("CanSlewAsync", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSync
        {
            get
            {
                tl.LogMessage("CanSync", "Get - " + true.ToString());
                return true;
            }
        }

        public bool CanSyncAltAz
        {
            get
            {
                tl.LogMessage("CanSyncAltAz", "Get - " + false.ToString());
                return false;
            }
        }

        public bool CanUnpark
        {
            get
            {
                tl.LogMessage("CanUnpark", "Get - " + false.ToString());
                return false;
            }
        }

        public double Declination
        {
            get
            {
                CheckConnected("Declination Get");

                var result = SharedResources.SendString(":GD#");
                //:GD# Get Telescope Declination.
                //Returns: sDD* MM# or sDD*MM�SS#
                //Depending upon the current precision setting for the telescope.

                double declination = utilities.DMSToDegrees(result);

                tl.LogMessage("Declination", "Get - " + utilities.DegreesToDMS(declination, ":", ":"));
                return declination;
            }
        }

        public double DeclinationRate
        {
            get
            {
                double declination = 0.0;
                tl.LogMessage("DeclinationRate", "Get - " + declination.ToString());
                return declination;
            }
            set
            {
                tl.LogMessage("DeclinationRate Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("DeclinationRate", true);
            }
        }

        public PierSide DestinationSideOfPier(double rightAscension, double declination)
        {
            tl.LogMessage("DestinationSideOfPier Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("DestinationSideOfPier", false);
        }

        public bool DoesRefraction
        {
            get
            {
                tl.LogMessage("DoesRefraction Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("DoesRefraction", false);
            }
            set
            {
                tl.LogMessage("DoesRefraction Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("DoesRefraction", true);
            }
        }

        public EquatorialCoordinateType EquatorialSystem
        {
            get
            {
                EquatorialCoordinateType equatorialSystem = EquatorialCoordinateType.equTopocentric;
                tl.LogMessage("DeclinationRate", "Get - " + equatorialSystem.ToString());
                return equatorialSystem;
            }
        }

        public void FindHome()
        {
            tl.LogMessage("FindHome", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("FindHome");
        }

        public double FocalLength
        {
            get
            {
                tl.LogMessage("FocalLength Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("FocalLength", false);
            }
        }

        public double GuideRateDeclination
        {
            get
            {
                tl.LogMessage("GuideRateDeclination Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", false);
            }
            set
            {
                tl.LogMessage("GuideRateDeclination Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", true);
            }
        }

        public double GuideRateRightAscension
        {
            get
            {
                tl.LogMessage("GuideRateRightAscension Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", false);
            }
            set
            {
                tl.LogMessage("GuideRateRightAscension Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", true);
            }
        }

        public bool IsPulseGuiding
        {
            get
            {
                tl.LogMessage("IsPulseGuiding Get", "pulse guiding is synchronous for this driver");
                //throw new ASCOM.PropertyNotImplementedException("IsPulseGuiding", false);
                return false;
            }
        }

        private bool _movingPrimary;
        private bool _movingSecondary;

        public void MoveAxis(TelescopeAxes axis, double rate)
        {
            tl.LogMessage("MoveAxis", $"Axis={axis} rate={rate}");
            CheckConnected("MoveAxis");

            var absRate = Math.Abs(rate);

            switch (absRate)
            {
                case 0:
                    //do nothing, it's ok this time as we're halting the slew.
                    break;
                case 1:
                    SharedResources.SendBlind(":RG#");
                    //:RG# Set Slew rate to Guiding Rate (slowest)
                    //Returns: Nothing
                    break;
                case 2:
                    SharedResources.SendBlind(":RC#");
                    //:RC# Set Slew rate to Centering rate (2nd slowest)
                    //Returns: Nothing
                    break;
                case 3:
                    SharedResources.SendBlind(":RM#");
                    //:RM# Set Slew rate to Find Rate (2nd Fastest)
                    //Returns: Nothing
                    break;
                case 4:
                    SharedResources.SendBlind(":RS#");
                    //:RS# Set Slew rate to max (fastest)
                    //Returns: Nothing
                    break;
                default:
                    throw new ASCOM.InvalidValueException($"Rate {rate} not supported");
            }

            switch (axis)
            {
                case TelescopeAxes.axisPrimary:
                    if (rate == 0)
                    {
                        _movingPrimary = false;
                        SharedResources.SendBlind(":Qe#");
                        //:Qe# Halt eastward Slews
                        //Returns: Nothing
                        SharedResources.SendBlind(":Qw#");
                        //:Qw# Halt westward Slews
                        //Returns: Nothing
                    }
                    else if (rate > 0)
                    {
                        SharedResources.SendBlind(":Me#");
                        //:Me# Move Telescope East at current slew rate
                        //Returns: Nothing
                        _movingPrimary = true;
                    }
                    else
                    {
                        SharedResources.SendBlind(":Mw#");
                        //:Mw# Move Telescope West at current slew rate
                        //Returns: Nothing
                        _movingPrimary = true;
                    }

                    break;
                case TelescopeAxes.axisSecondary:
                    if (rate == 0)
                    {
                        _movingSecondary = false;
                        SharedResources.SendBlind(":Qn#");
                        //:Qn# Halt northward Slews
                        //Returns: Nothing
                        SharedResources.SendBlind(":Qs#");
                        //:Qs# Halt southward Slews
                        //Returns: Nothing
                    }
                    else if (rate > 0)
                    {
                        SharedResources.SendBlind(":Mn#");
                        //:Mn# Move Telescope North at current slew rate
                        //Returns: Nothing
                        _movingSecondary = true;
                    }
                    else
                    {
                        SharedResources.SendBlind(":Ms#");
                        //:Ms# Move Telescope South at current slew rate
                        //Returns: Nothing
                        _movingSecondary = true;
                    }

                    break;
                default:
                    throw new ASCOM.MethodNotImplementedException("Can not move this axis.");
            }
        }

        public void Park()
        {
            tl.LogMessage("Park", "Parking telescope");
            CheckConnected("Park");

            if (AtPark)
                return;

            SharedResources.SendBlind(":hP#");
            //:hP# Autostar, Autostar II and LX 16�Slew to Park Position
            //Returns: Nothing
            AtPark = true;
        }

        private readonly bool
            _userNewerPulseGuiding = true; //todo make this a device setting based on firmware revision

        public void PulseGuide(GuideDirections direction, int duration)
        {
            tl.LogMessage("PulseGuide", $"pulse guide direction {direction} duration {duration}");
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
                SharedResources.SendBlind($":Mg{d}{duration:0000}#");
                //:MgnDDDD#
                //:MgsDDDD#
                //:MgeDDDD#
                //:MgwDDDD#
                //Guide telescope in the commanded direction(nsew) for the number of milliseconds indicated by the unsigned number
                //passed in the command.These commands support serial port driven guiding.
                //Returns � Nothing
                //LX200 � Not Supported

               utilities.WaitForMilliseconds(duration); //todo figure out if this is really needed
            }
            else
            {
                SharedResources.Lock(() =>
                {
                    SharedResources.SendBlind(":RG#"); //Make sure we are at guide rate
                    //:RG# Set Slew rate to Guiding Rate (slowest)
                    //Returns: Nothing
                    SharedResources.SendBlind($":M{d}#");
                    //:Me# Move Telescope East at current slew rate
                    //Returns: Nothing
                    //:Mn# Move Telescope North at current slew rate
                    //Returns: Nothing
                    //:Ms# Move Telescope South at current slew rate
                    //Returns: Nothing
                    //:Mw# Move Telescope West at current slew rate
                    //Returns: Nothing
                    utilities.WaitForMilliseconds(duration);
                    SharedResources.SendBlind($":Q{d}#");
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
                var result = SharedResources.SendString(":GR#");
                //:GR# Get Telescope RA
                //Returns: HH: MM.T# or HH:MM:SS#
                //Depending which precision is set for the telescope

                double rightAscension = utilities.HMSToHours(result);

                tl.LogMessage("RightAscension", "Get - " + utilities.HoursToHMS(rightAscension));
                return rightAscension;
            }
        }

        public double RightAscensionRate
        {
            get
            {
                double rightAscensionRate = 0.0;
                tl.LogMessage("RightAscensionRate", "Get - " + rightAscensionRate.ToString());
                return rightAscensionRate;
            }
            set
            {
                tl.LogMessage("RightAscensionRate Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("RightAscensionRate", true);
            }
        }

        public void SetPark()
        {
            tl.LogMessage("SetPark", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SetPark");
        }

        public PierSide SideOfPier
        {
            get
            {
                tl.LogMessage("SideOfPier Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SideOfPier", false);
            }
            set
            {
                tl.LogMessage("SideOfPier Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SideOfPier", true);
            }
        }

        public double SiderealTime
        {
            get
            {
                // Now using NOVAS 3.1
                double siderealTime = 0.0;
                using (var novas = new ASCOM.Astrometry.NOVAS.NOVAS31())
                {
                    var jd = utilities.DateUTCToJulian(DateTime.UtcNow);
                    novas.SiderealTime(jd, 0, novas.DeltaT(jd),
                        ASCOM.Astrometry.GstType.GreenwichApparentSiderealTime,
                        ASCOM.Astrometry.Method.EquinoxBased,
                        ASCOM.Astrometry.Accuracy.Reduced, ref siderealTime);
                }

                // Allow for the longitude
                siderealTime += SiteLongitude / 360.0 * 24.0;

                // Reduce to the range 0 to 24 hours
                siderealTime = astroUtilities.ConditionRA(siderealTime);

                tl.LogMessage("SiderealTime", "Get - " + siderealTime.ToString());
                return siderealTime;
            }
        }

        public double SiteElevation
        {
            get
            {
                tl.LogMessage("SiteElevation Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SiteElevation", false);
            }
            set
            {
                tl.LogMessage("SiteElevation Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SiteElevation", true);
            }
        }

        public double SiteLatitude
        {
            get
            {
                CheckConnected("SiteLatitude Get");

                var latitude = SharedResources.SendString(":Gt#");
                //:Gt# Get Current Site Latitude
                //Returns: sDD* MM#
                //The latitude of the current site. Positive inplies North latitude.

                var siteLatitude = utilities.DMSToDegrees(latitude);
                tl.LogMessage("SiteLatitude Get", $"{utilities.DegreesToDMS(siteLatitude)}");
                return siteLatitude;
            }
            set
            {
                tl.LogMessage("SiteLatitude Set", $"{utilities.DegreesToDMS(value)}");

                CheckConnected("SiteLatitude Set");

                if (value > 90)
                    throw new InvalidValueException("Latitude cannot be greater than 90 degrees.");

                if (value < -90)
                    throw new InvalidValueException("Latitude cannot be less than -90 degrees.");

                string sign = value > 0 ? "+" : "-";
                int d = Convert.ToInt32(Math.Floor(value));
                int m = Convert.ToInt32(60 * (value - d));

                var result = SharedResources.SendChar($":St{sign}{d:00}*{m:00}#");
                //:StsDD*MM#
                //Sets the current site latitude to sDD* MM#
                //Returns:
                //0 � Invalid
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

                var longitude = SharedResources.SendString(":Gg#");
                //:Gg# Get Current Site Longitude
                //Returns: sDDD* MM#
                //The current site Longitude. East Longitudes are expressed as negative
                double siteLongitude = utilities.DMSToDegrees(longitude);

                if (siteLongitude > 180)
                    siteLongitude = siteLongitude - 360;

                siteLongitude = -siteLongitude;

                tl.LogMessage("SiteLongitude Get", $"{utilities.DegreesToDMS(siteLongitude)}");
                return siteLongitude;
            }
            set
            {
                var newLongitude = value;

                tl.LogMessage("SiteLongitude Set", $"{utilities.DegreesToDMS(newLongitude)}");

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

                var result = SharedResources.SendChar($":Sg{d:000}*{m:00}#");
                //:SgDDD*MM#
                //Set current site�s longitude to DDD*MM an ASCII position string
                //Returns:
                //0 � Invalid
                //1 - Valid
                if (result != "1")
                    throw new InvalidOperationException("Failed to set site longitude.");
            }
        }

        public short SlewSettleTime
        {
            get
            {
                tl.LogMessage("SlewSettleTime Get", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", false);
            }
            set
            {
                tl.LogMessage("SlewSettleTime Set", "Not implemented");
                throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", true);
            }
        }

        public void SlewToAltAz(double azimuth, double altitude)
        {
            tl.LogMessage("SlewToAltAz", $"Az=~{azimuth} Alt={altitude}");
            CheckConnected("SlewToAltAz");

            SlewToAltAzAsync(azimuth, altitude);

            while (Slewing) //wait for slew to complete
            {
                utilities.WaitForMilliseconds(200); //be responsive to AbortSlew();
            }
        }

        private double TargetAltitude
        {
            set
            {
                if (value > 90)
                    throw new ASCOM.InvalidValueException("Altitude cannot be greater than 90.");

                if (value < 0)
                    throw new ASCOM.InvalidValueException("Altitide cannot be less than 0.");

                CheckConnected("TargetAltitude Set");

                //todo this serial string does not work.  Calculate the EQ version instead.

                var dms = utilities.DegreesToDMS(value, "*", "'", "",0);
                var s = value < 0 ? "-" : "+";

                var result = SharedResources.SendChar($":Sa{s}{dms}#");
                //:SasDD*MM#
                //Set target object altitude to sDD*MM# or sDD*MM�SS# [LX 16�, Autostar, Autostar II]
                //Returns:
                //1 Object within slew range
                //0 Object out of slew range

                if (result == "0")
                    throw new ASCOM.InvalidOperationException("Target altitude out of slew range");
            }
        }

        private double TargetAzimuth
        {
            set
            {
                if (value >= 360)
                    throw new ASCOM.InvalidValueException("Azimuth cannot be 360 or higher.");

                if (value < 0)
                    throw new ASCOM.InvalidValueException("Azimuth cannot be less than 0.");

                CheckConnected("TargetAzimuth Set");

                //todo this serial string does not work.  Calculate the EQ version instead.

                var dms = utilities.DegreesToDM(value, "*" );

                var result = SharedResources.SendChar($":Sz{dms}#");
                //:SzDDD*MM#
                //Sets the target Object Azimuth[LX 16� and Autostar II only]
                //Returns:
                //0 � Invalid
                //1 - Valid

                if (result == "0")
                    throw new ASCOM.InvalidOperationException("Target Azimuth out of slew range");

            }
        }

        public void SlewToAltAzAsync(double azimuth, double altitude)
        {
            if (altitude > 90)
                throw new ASCOM.InvalidValueException("Altitude cannot be greater than 90.");

            if (altitude < 0)
                throw new ASCOM.InvalidValueException("Altitide cannot be less than 0.");

            if (azimuth >= 360)
                throw new ASCOM.InvalidValueException("Azimuth cannot be 360 or higher.");

            if (azimuth < 0)
                throw new ASCOM.InvalidValueException("Azimuth cannot be less than 0.");

            tl.LogMessage("SlewToAltAzAsync", $"Az={azimuth} Alt={altitude}");
            CheckConnected("SlewToAltAzAsync");

            HorizonCoordinates altAz = new HorizonCoordinates();
            altAz.Azimuth = azimuth;
            altAz.Altitude = altitude;

            var utcDateTime = UTCDate;
            var latitude = SiteLatitude;
            var longitude = SiteLongitude;

            SharedResources.Lock(() =>
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

            SharedResources.Lock(() =>
            {
                switch (polar)
                {
                    case true:
                        var response = SharedResources.SendChar(":MS#");
                        //:MS# Slew to Target Object
                        //Returns:
                        //0 Slew is Possible
                        //1<string># Object Below Horizon w/string message
                        //2<string># Object Below Higher w/string message

                        switch (response)
                        {
                            case "0":
                                //We're slewing everything should be working just fine.
                                break;
                            case "1":
                                //Below Horizon 
                                string belowHorizonMessage = SharedResources.ReadTerminated();
                                throw new ASCOM.InvalidOperationException(belowHorizonMessage);
                            case "2":
                                //Below Horizon 
                                string belowMinimumElevationMessage = SharedResources.ReadTerminated();
                                throw new ASCOM.InvalidOperationException(belowMinimumElevationMessage);
                            default:
                                throw new ASCOM.DriverException("This error should not happen");

                        }

                        break;
                    case false:
                        var maResponse = SharedResources.SendChar(":MA#");
                        //:MA# Autostar, LX 16�, Autostar II � Slew to target Alt and Az
                        //Returns:
                        //0 - No fault
                        //1 � Fault
                        //    LX200 � Not supported

                        if (maResponse == "1")
                        {
                            throw new ASCOM.InvalidOperationException("fault");
                        }

                        break;
                }
            });
        }

        public void SlewToCoordinates(double rightAscension, double declination)
        {
            tl.LogMessage("SlewToCoordinates", $"Ra={rightAscension}, Dec={declination}");
            CheckConnected("SlewToCoordinates");

            SlewToCoordinatesAsync(rightAscension, declination);

            while (Slewing) //wait for slew to complete
            {
                utilities.WaitForMilliseconds(200); //be responsive to AbortSlew();
            }
        }

        public void SlewToCoordinatesAsync(double rightAscension, double declination)
        {
            tl.LogMessage("SlewToCoordinatesAsync", $"Ra={rightAscension}, Dec={declination}");
            CheckConnected("SlewToCoordinatesAsync");

            SharedResources.Lock(() =>
                {
                    TargetRightAscension = rightAscension;
                    TargetDeclination = declination;

                    DoSlewAsync(true);
                }
            );
        }

        public void SlewToTarget()
        {
            tl.LogMessage("SlewToTarget", "Executing");
            CheckConnected("SlewToTarget");
            SlewToTargetAsync();

            while (Slewing)
            {
                utilities.WaitForMilliseconds(200);
            }
        }

        private const double INVALID_PARAMETER = -1000;

        public void SlewToTargetAsync()
        {
            CheckConnected("SlewToTargetAsync");

            if (TargetDeclination == INVALID_PARAMETER || TargetRightAscension == INVALID_PARAMETER)
                throw new ASCOM.InvalidOperationException("No target selected to slew to.");

            DoSlewAsync(true);
        }

        private bool movingAxis()
        {
            return _movingPrimary || _movingSecondary;
        }

        public bool Slewing
        {
            get
            {
                if (!Connected) return false;


                if (movingAxis())
                    return true;

                CheckConnected("Slewing Get");

                var result = SharedResources.SendString(":D#");
                //:D# Requests a string of bars indicating the distance to the current target location.
                //Returns:
                //LX200's � a string of bar characters indicating the distance.
                //Autostars and Autostar II � a string containing one bar until a slew is complete, then a null string is returned.
                bool isSlewing = result != string.Empty;

                tl.LogMessage("Slewing Get", $"Result = {isSlewing}");
                return isSlewing;
            }
        }

        public void SyncToAltAz(double azimuth, double altitude)
        {
            tl.LogMessage("SyncToAltAz", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("SyncToAltAz");
        }

        public void SyncToCoordinates(double rightAscension, double declination)
        {
            tl.LogMessage("SyncToCoordinates", $"RA={rightAscension} Dec={declination}");
            CheckConnected("SyncToCoordinates");

            SharedResources.Lock(() =>
            {
                TargetRightAscension = rightAscension;
                TargetDeclination = declination;

                SyncToTarget();
            });
        }

        public void SyncToTarget()
        {
            tl.LogMessage("SyncToTarget", "Executing");
            CheckConnected("SyncToTarget");

            var result = SharedResources.SendString(":CM#");
            //:CM# Synchronizes the telescope's position with the currently selected database object's coordinates.
            //Returns:
            //LX200's - a "#" terminated string with the name of the object that was synced.
            //    Autostars & Autostar II - At static string: " M31 EX GAL MAG 3.5 SZ178.0'#"

            if (result == string.Empty)
                throw new ASCOM.InvalidOperationException("Unable to perform sync");
        }

        private double _targetDeclination = INVALID_PARAMETER;
        public double TargetDeclination
        {
            get
            {
                if (_targetDeclination == INVALID_PARAMETER)
                    throw new ASCOM.InvalidOperationException("Target not set");

                //var result = SerialPort.CommandTerminated(":Gd#", "#");
                ////:Gd# Get Currently Selected Object/Target Declination
                ////Returns: sDD* MM# or sDD*MM�SS#
                ////Depending upon the current precision setting for the telescope.

                //double targetDec = DmsToDouble(result);

                //return targetDec;

                tl.LogMessage("TargetDeclination Get", $"{_targetDeclination}");
                return _targetDeclination;
            }
            set
            {
                tl.LogMessage("TargetDeclination Set", $"{value}");
                
                //todo implement low precision version of this.
                if (value > 90)
                    throw new ASCOM.InvalidValueException("Declination cannot be greater than 90.");

                if (value < -90)
                    throw new ASCOM.InvalidValueException("Declination cannot be less than -90.");

                CheckConnected("TargetDeclination Set");

                var dms = utilities.DegreesToDMS(value, "*", ":", ":", 2);
                var s = value < 0 ? '-' : '+';

                var result = SharedResources.SendChar($":Sd{s}{dms}#");
                //:SdsDD*MM#
                //Set target object declination to sDD*MM or sDD*MM:SS depending on the current precision setting
                //Returns:
                //1 - Dec Accepted
                //0 � Dec invalid

                if (result == "0")
                {
                    throw new ASCOM.InvalidOperationException("Target declination invalid");
                }

                _targetDeclination = value;
            }
        }

        private double _targetRightAscension = INVALID_PARAMETER;
        public double TargetRightAscension
        {
            get
            {
                if (_targetRightAscension == INVALID_PARAMETER)
                    throw new ASCOM.InvalidOperationException("Target not set");

                //var result = SerialPort.CommandTerminated(":Gr#", "#");
                ////:Gr# Get current/target object RA
                ////Returns: HH: MM.T# or HH:MM:SS
                ////Depending upon which precision is set for the telescope

                //double targetRa = HmsToDouble(result);
                //return targetRa;

                tl.LogMessage("TargetRightAscension Get", $"{_targetRightAscension}");
                return _targetRightAscension;
            }
            set
            {
                tl.LogMessage("TargetRightAscension Set", $"{value}");

                if (value < 0)
                    throw new InvalidValueException("Right ascension value cannot be below 0");

                if (value >= 24)
                    throw new InvalidValueException("Right ascension value cannot be greater than 23:59:59");

                CheckConnected("TargetRightAscension Set");
                //todo implement the low precision version

                var hms = utilities.HoursToHMS(value, ":", ":", ":", 2);
                var response = SharedResources.SendChar($":Sr{hms}#");
                //:SrHH:MM.T#
                //:SrHH:MM:SS#
                //Set target object RA to HH:MM.T or HH: MM: SS depending on the current precision setting.
                //    Returns:
                //0 � Invalid
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
                tl.LogMessage("Tracking", $"Get - {_tracking}");
                return _tracking;
            }
            set
            {
                tl.LogMessage($"Tracking Set", $"{value}");
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
                tl.LogMessage("TrackingRate Get", $"{_trackingRate}");
                return _trackingRate;
            }
            set
            {
                tl.LogMessage("TrackingRate Set", $"{value}");
                CheckConnected("TrackingRate Set");

                switch (value)
                {
                    case DriveRates.driveSidereal:
                        SharedResources.SendBlind(":TQ#");
                        //:TQ# Selects sidereal tracking rate
                        //Returns: Nothing
                        break;
                    case DriveRates.driveLunar:
                        SharedResources.SendBlind(":TL#");
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
                tl.LogMessage("TrackingRates", "Get - ");
                foreach (DriveRates driveRate in trackingRates)
                {
                    tl.LogMessage("TrackingRates", "Get - " + driveRate.ToString());
                }
                return trackingRates;
            }
        }

        private TimeSpan GetUtcCorrection()
        {
            CheckConnected("GetUtcCorrection");

            string utcOffSet = SharedResources.SendString(":GG#");
            //:GG# Get UTC offset time
            //Returns: sHH# or sHH.H#
            //The number of decimal hours to add to local time to convert it to UTC. If the number is a whole number the
            //sHH# form is returned, otherwise the longer form is returned.
            double utcOffsetHours = double.Parse(utcOffSet);
            TimeSpan utcCorrection = TimeSpan.FromHours(utcOffsetHours);
            return utcCorrection;
        }

        private class TelescopeDateDetails
        {
            public string telescopeDate { get; set; }
            public string telescopeTime { get; set; }
            public TimeSpan utcCorrection { get; set; }
        }

        public DateTime UTCDate
        {
            get
            {
                CheckConnected("UTCDate Get");

                tl.LogMessage("UTCDate", "Get started");

                TelescopeDateDetails telescopeDateDetails = SharedResources.Lock(() =>
                {
                    TelescopeDateDetails tdd = new TelescopeDateDetails();
                    tdd.telescopeDate = SharedResources.SendString(":GC#");
                    //:GC# Get current date.
                    //Returns: MM / DD / YY#
                    //The current local calendar date for the telescope.
                    tdd.telescopeTime = SharedResources.SendString(":GL#");
                    //:GL# Get Local Time in 24 hour format
                    //Returns: HH: MM: SS#
                    //The Local Time in 24 - hour Format
                    tdd.utcCorrection = GetUtcCorrection();

                    return tdd;
                });

                int month = telescopeDateDetails.telescopeDate.Substring(0, 2).ToInteger();
                int day = telescopeDateDetails.telescopeDate.Substring(3, 2).ToInteger();
                int year = telescopeDateDetails.telescopeDate.Substring(6, 2).ToInteger();

                if (year < 2000) //todo fix this hack that will create a Y2K100 bug
                {
                    year = year + 2000;
                }

                int hour = telescopeDateDetails.telescopeTime.Substring(0, 2).ToInteger();
                int minute = telescopeDateDetails.telescopeTime.Substring(3, 2).ToInteger();
                int second = telescopeDateDetails.telescopeTime.Substring(6, 2).ToInteger();

                var utcDate = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc) +
                              telescopeDateDetails.utcCorrection;

                tl.LogMessage("UTCDate", "Get - " + utcDate.ToString("MM/dd/yy HH:mm:ss"));

                return utcDate;
            }
            set
            {
                tl.LogMessage("UTCDate", "Set - " + value.ToString("MM/dd/yy HH:mm:ss"));

                CheckConnected("UTCDate Set");

                SharedResources.Lock(() =>
                {
                    var utcCorrection = GetUtcCorrection();
                    var localDateTime = value - utcCorrection;

                    var timeResult = SharedResources.SendChar($":SL{localDateTime:HH:mm:ss}#");
                    //:SLHH:MM:SS#
                    //Set the local Time
                    //Returns:
                    //0 � Invalid
                    //1 - Valid
                    if (timeResult != "1")
                    {
                        throw new InvalidOperationException("Failed to set local time");
                    }

                    var dateResult = SharedResources.SendChar($":SC{localDateTime:MM/dd/yy}#");
                    //:SCMM/DD/YY#
                    //Change Handbox Date to MM/DD/YY
                    //Returns: <D><string>
                    //D = �0� if the date is invalid.The string is the null string.
                    //D = �1� for valid dates and the string is �Updating Planetary Data#                       #�
                    //Note: For Autostar II this is the UTC data!
                    if (dateResult != "1")
                    {
                        throw new InvalidOperationException("Failed to set local date");
                    }

                    //throwing away these two strings which represent 
                    SharedResources.ReadTerminated(); //Updating Planetary Data#
                    SharedResources.ReadTerminated(); //                       #
                });
            }
        }

        public void Unpark()
        {
            tl.LogMessage("Unpark", "Not implemented");
            throw new ASCOM.MethodNotImplementedException("Unpark");
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
        private static void RegUnregASCOM(bool bRegister)
        {
            using (var P = new ASCOM.Utilities.Profile())
            {
                P.DeviceType = "Telescope";
                if (bRegister)
                {
                    P.Register(driverID, driverDescription);
                }
                else
                {
                    P.Unregister(driverID);
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
        public static void RegisterASCOM(Type t)
        {
            RegUnregASCOM(true);
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
        public static void UnregisterASCOM(Type t)
        {
            RegUnregASCOM(false);
        }

        #endregion

        /// <summary>
        /// Returns true if there is a valid connection to the driver hardware
        /// </summary>
        private bool IsConnected
        {
            get
            {
                // TODO check that the driver hardware connection exists and is connected to the hardware
                return _connectedState;
            }
        }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!IsConnected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {
            var profileProperties =  SharedResources.ReadProfile();
            tl.Enabled = profileProperties.TraceLogger;
            comPort = profileProperties.ComPort;
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
            tl.LogMessage(identifier, msg);
        }
        #endregion
    }
}
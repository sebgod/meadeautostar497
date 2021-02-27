#define Telescope

using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Astrometry.NOVAS;
using ASCOM.DeviceInterface;
using ASCOM.Meade.net.AstroMaths;
using ASCOM.Meade.net.Properties;
using ASCOM.Meade.net.Wrapper;
using ASCOM.Utilities;
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
    // Replace the not implemented exceptions with code to implement the function or
    // throw the appropriate ASCOM exception.
    //

    /// <summary>
    /// ASCOM Telescope Driver for Meade.net.
    /// </summary>
    [Guid("d9fd4b3e-c4f1-48ac-a16f-d02eef30d86f")]
    [ProgId("ASCOM.MeadeGeneric.Telescope")]
    [ServedClassName("Meade Generic")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class Telescope : MeadeTelescopeBase, ITelescopeV3
    {
        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        //internal static string driverID = "ASCOM.Meade.net.Telescope";
        private static readonly string DriverId = Marshal.GenerateProgIdForType(MethodBase.GetCurrentMethod().DeclaringType ?? throw new System.InvalidOperationException());

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

        private readonly IClock _clock;

        /// <summary>
        /// Private variable to hold number of decimals for RA
        /// </summary>
        private int _digitsRa = 2;

        /// <summary>
        /// Private variable to hold number of decimals for Dec
        /// </summary>
        private int _digitsDe = 2;

        private short _settleTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="Meade.net"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public Telescope()
        {
            try
            {
                //todo move this out to IOC
                var util = new Util(); //Initialise util object
                _utilities = util;
                _utilitiesExtra = util; //Initialise util object
                _astroUtilities = new AstroUtils(); // Initialise astro utilities object
                _astroMaths = new AstroMaths.AstroMaths();
                _clock = new Clock();

                Initialise(nameof(Telescope));
            }
            catch (Exception e)
            {
                StringBuilder sb = new StringBuilder();

                var currentException = e;

                while (currentException != null)
                {
                    AppendException(sb, currentException);

                    currentException = currentException.InnerException;

                    if (currentException != null)
                        sb.AppendLine("-----");
                }

                MessageBox.Show(sb.ToString());
                throw;
            }
        }

        private void AppendException(StringBuilder sb, Exception currentException)
        {
            sb.AppendLine(currentException.Message);
            sb.AppendLine();
            sb.AppendLine(currentException.StackTrace);
            sb.AppendLine();
        }

        public Telescope( IUtil util, IUtilExtra utilExtra, IAstroUtils astroUtilities, ISharedResourcesWrapper sharedResourcesWrapper, IAstroMaths astroMaths, IClock clock) : base(sharedResourcesWrapper)
        {
            _clock = clock;
            _utilities = util; //Initialise util object
            _utilitiesExtra = utilExtra; //Initialise util object
            _astroUtilities = astroUtilities; // Initialise astro utilities object
            _astroMaths = astroMaths;

            Initialise(nameof(Telescope));
        }

        private bool _isGuiding;

        private bool _isTargetCoordinateInitRequired = true;
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
            SharedResourcesWrapper.SetupDialog();
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
                var supportedActions = new ArrayList {"handbox", "site"};
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
                            var output = SharedResourcesWrapper.SendString(":ED#");
                            return output;

                        //top row of buttons
                        case "enter":
                            SharedResourcesWrapper.SendBlind(":EK13#");
                            break;
                        case "mode":
                            SharedResourcesWrapper.SendBlind(":EK9#");
                            break;
                        case "longmode":
                            SharedResourcesWrapper.SendBlind(":EK11#");
                            break;
                        case "goto":
                            SharedResourcesWrapper.SendBlind(":EK24#");
                            break;

                        case "0": //light and 0
                            SharedResourcesWrapper.SendBlind(":EK48#");
                            break;
                        case "1":
                            SharedResourcesWrapper.SendBlind(":EK49#");
                            break;
                        case "2":
                            SharedResourcesWrapper.SendBlind(":EK50#");
                            break;
                        case "3":
                            SharedResourcesWrapper.SendBlind(":EK51#");
                            break;
                        case "4":
                            SharedResourcesWrapper.SendBlind(":EK52#");
                            break;
                        case "5":
                            SharedResourcesWrapper.SendBlind(":EK53#");
                            break;
                        case "6":
                            SharedResourcesWrapper.SendBlind(":EK54#");
                            break;
                        case "7":
                            SharedResourcesWrapper.SendBlind(":EK55#");
                            break;
                        case "8":
                            SharedResourcesWrapper.SendBlind(":EK56#");
                            break;
                        case "9":
                            SharedResourcesWrapper.SendBlind(":EK57#");
                            break;

                        case "up":
                            SharedResourcesWrapper.SendBlind(":EK94#");
                            break;
                        case "down":
                            SharedResourcesWrapper.SendBlind(":EK118#");
                            break;
                        case "back":
                            SharedResourcesWrapper.SendBlind(":EK87#");
                            break;
                        case "forward":
                            SharedResourcesWrapper.SendBlind(":EK69#");
                            break;
                        case "?":
                            SharedResourcesWrapper.SendBlind(":EK63#");
                            break;
                        default:
                            LogMessage("", "Action {0}, parameters {1} not implemented", actionName, actionParameters);
                            throw new ActionNotImplementedException($"{actionName}({actionParameters})");
                    }

                    break;
                case "site":
                    var parames = actionParameters.ToLower().Split(' ');
                    switch (parames[0])
                    {
                        case "count":
                            return "4";
                        case "select":
                            switch (parames[1])
                            {
                                case "1":
                                case "2":
                                case "3":
                                case "4":
                                    SelectSite(parames[1].ToInteger());
                                    break;
                                default:
                                    LogMessage("", "Action {0}, parameters {1} not implemented", actionName,
                                        actionParameters);
                                    throw new InvalidValueException(
                                        $"Site {actionParameters} not allowed, must be between 1 and 4");

                            }
                            break;
                        case "getname":
                            switch (parames[1])
                            {
                                case "1":
                                case "2":
                                case "3":
                                case "4":
                                    return GetSiteName(parames[1].ToInteger());
                                default:
                                    LogMessage("", "Action {0}, parameters {1} not implemented", actionName,
                                        actionParameters);
                                    throw new InvalidValueException(
                                        $"Site {actionParameters} not allowed, must be between 1 and 4");

                            }
                        case "setname":
                            switch (parames[1])
                            {
                                case "1":
                                case "2":
                                case "3":
                                case "4":
                                    var sitename = actionParameters.Substring(actionParameters.Position(' ', 2)).Trim();

                                    SetSiteName(parames[1].ToInteger(), sitename);
                                    break;
                                default:
                                    LogMessage("", "Action {0}, parameters {1} not implemented", actionName,
                                        actionParameters);
                                    throw new InvalidValueException(
                                        $"Site {actionParameters} not allowed, must be between 1 and 4");

                            }
                            break;
                        default:
                            throw new InvalidValueException(
                                $"Site parameters {actionParameters} not known");
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
            SharedResourcesWrapper.SendBlind(command);
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
            return SharedResourcesWrapper.SendString(command);
            //throw new ASCOM.MethodNotImplementedException("CommandString");
        }

        public void Dispose()
        {
            if (Connected)
                Connected = false;

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
                LogMessage("Connected", "Set {0}", value);
                if (value == IsConnected)
                    return;

                if (value)
                {
                    try
                    {
                        ReadProfile();

                        LogMessage("Connected Set", "Connecting to port {0}", ComPort);
                        var connectionInfo = SharedResourcesWrapper.Connect("Serial", DriverId, Tl);
                        try
                        {
                            LogMessage("Connected Set", $"Connected to port {ComPort}. Product: {SharedResourcesWrapper.ProductName} Version:{SharedResourcesWrapper.FirmwareVersion}");

                            _userNewerPulseGuiding = IsNewPulseGuidingSupported();
                            _targetDeclination = InvalidParameter;
                            _targetRightAscension = InvalidParameter;
                            _tracking = true;

                            LogMessage("Connected Set", $"New Pulse Guiding Supported: {_userNewerPulseGuiding}");
                            IsConnected = true;

                            if (connectionInfo.SameDevice == 1)
                            {
                                LogMessage("Connected Set", "Making first connection telescope adjustments");

                                //These settings are applied only when the first device connects to the telescope.
                                SetLongFormat(true);

                                if (CanSetGuideRates)
                                {
                                    SetNewGuideRate(GuideRate, "Connect");
                                }

                                SetTelescopePrecision("Connect");
                            }
                            else
                            {
                                LogMessage("Connected Set", $"Skipping first connection telescope adjustments (current connections: {connectionInfo.SameDevice})");
                            }

                            var raAndDec = GetTelescopeRaAndDec();
                            LogMessage("Connected Set", $"Connected OK.  Current RA = {_utilitiesExtra.HoursToHMS(raAndDec.RightAscension)} Dec = {_utilitiesExtra.DegreesToDMS(raAndDec.Declination)}");
                        }
                        catch (Exception)
                        {
                            SharedResourcesWrapper.Disconnect("Serial", DriverId);
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage("Connected Set", "Error connecting to port {0} - {1}", ComPort, ex.Message);
                    }
                }
                else
                {
                    LogMessage("Connected Set", "Disconnecting from port {0}", ComPort);
                    SharedResourcesWrapper.Disconnect("Serial", DriverId);
                    IsConnected = false;
                }
            }
        }

        private void SetTelescopePrecision(string propertyName)
        {
            switch (Precision.ToLower())
            {
                case "high":
                    TelescopePointingPrecision(true);
                    LogMessage(propertyName, "High precision slewing selected");
                    break;
                case "low":
                    TelescopePointingPrecision(false);
                    LogMessage(propertyName, "Low precision slewing selected");
                    break;
                default:
                    LogMessage(propertyName, "Precision slewing unchanged");
                    break;
            }
        }

        public bool IsNewPulseGuidingSupported()
        {
            switch (GuidingStyle)
            {
                case "guide rate slew":
                    return false;
                case "pulse guiding":
                    return true;

                default:
                    if (SharedResourcesWrapper.ProductName == TelescopeList.Autostar497)
                    {
                        return FirmwareIsGreaterThan(TelescopeList.Autostar497_31Ee);
                    }

                    if (SharedResourcesWrapper.ProductName == TelescopeList.LX200GPS)
                    {
                        return true;
                    }

                    return false;
            }
        }

        private bool IsLongFormatSupported()
        {
            if (SharedResourcesWrapper.ProductName == TelescopeList.LX200CLASSIC)
            {
                return false;
            }
            return true;
        }

        private bool IsGuideRateSettingSupported()
        {
            if (SharedResourcesWrapper.ProductName == TelescopeList.LX200GPS)
            {
                return true;
            }
            return false;
        }

        private bool FirmwareIsGreaterThan(string minVersion)
        {
            var currentVersion = SharedResourcesWrapper.FirmwareVersion;
            var comparison = String.Compare(currentVersion, minVersion, StringComparison.Ordinal);
            return comparison >= 0;
        }

        private bool IsLongFormat { get; set; }

        /// <summary>
        /// classic LX200 needs initial set of target coordinates, if it is slewing and the target RA DE coordinates are 0 and differ from the current coordinates
        /// </summary>
        private bool IsTargetCoordinateInitRequired()
        {
            if (SharedResourcesWrapper.ProductName != TelescopeList.LX200CLASSIC)
                return false;

            if (!_isTargetCoordinateInitRequired)
                return _isTargetCoordinateInitRequired;

            if (!IsConnected)
                return true;

            if(SharedResourcesWrapper.ProductName != TelescopeList.LX200CLASSIC)
            {
                _isTargetCoordinateInitRequired = false;
                return _isTargetCoordinateInitRequired;
            }

            const double eps = 0.00001d;

            double rightTargetAscension = RightAscension;
            //target RA == 0
            if (Math.Abs(rightTargetAscension) > eps)
            {
                _isTargetCoordinateInitRequired = false;
                return _isTargetCoordinateInitRequired;
            }

            double targetDeclination = Declination;
            //target DE == 0
            if (Math.Abs(targetDeclination) > eps)
            {
                _isTargetCoordinateInitRequired = false;
                return _isTargetCoordinateInitRequired;
            }
            
            //target coordinates are equal current coordinates
            if((Math.Abs(RightAscension - rightTargetAscension ) <= eps) &&
                (Math.Abs(Declination - targetDeclination) <= eps))
            {
                LogMessage("IsTargetCoordinateInitRequired", $"0 diff -> false");
                _isTargetCoordinateInitRequired = false;
                return _isTargetCoordinateInitRequired;
            }

            LogMessage("IsTargetCoordinateInitRequired", $"{_isTargetCoordinateInitRequired}");
            return _isTargetCoordinateInitRequired;
        }

        private void InitTargetCoordinates()
        {
            try
            {
                var raAndDec = GetTelescopeRaAndDec();
                //when connection the first time the telescope target coordinates should be the current ones.
                //for the classic LX200 at least this is not the case, target ra and dec are 0, when switched on.
                LogMessage("InitTargetCoordinates", "sync telescope target");
                SyncToCoordinates(raAndDec.RightAscension, raAndDec.Declination);

                //do it only once
                _isTargetCoordinateInitRequired = false;
            }
            catch (Exception ex)
            {
                LogMessage("InitTargetCoordinates", $"Error sync telescope position", ex.Message);
            }
        }

        public void SetLongFormat(bool setLongFormat)
        {
            IsLongFormat = false;

            if (!IsLongFormatSupported())
            {
                LogMessage("SetLongFormat", "Long coordinate format not supported for this mount");
                _digitsRa = 1;
                _digitsDe = 0;
                return;
            }

            SharedResourcesWrapper.Lock(() =>
            {
                var result = SharedResourcesWrapper.SendString(":GZ#");
                LogMessage("SetLongFormat", $"Get - Azimuth {result}");
                //:GZ# Get telescope azimuth
                //Returns: DDD*MM.T or DDD*MM’SS#
                //The current telescope Azimuth depending on the selected precision.

                IsLongFormat = result.Length > 6;

                if (IsLongFormat != setLongFormat)
                {
                    _utilities.WaitForMilliseconds(500);
                    SharedResourcesWrapper.SendBlind(":U#");
                    //:U# Toggle between low/hi precision positions
                    //Low - RA displays and messages HH:MM.T sDD*MM
                    //High - Dec / Az / El displays and messages HH:MM: SS sDD*MM:SS
                    //    Returns Nothing
                    result = SharedResourcesWrapper.SendString(":GZ#");
                    IsLongFormat = result.Length > 6;
                    LogMessage("SetLongFormat", $"Get - Azimuth {result}");
                    if (IsLongFormat == setLongFormat)
                        LogMessage("SetLongFormat", $"Long coordinate format: {setLongFormat} ");
                } 
                else
                {
                    LogMessage("SetLongFormat", $"Long coordinate format: {setLongFormat} ");
                }
            });

            LogMessage("SetLongFormat", $"Long coordinate format: {setLongFormat} ");
        }

        private bool TogglePrecision()
        {
            LogMessage("TogglePrecision", "Toggling slewing precision");
            var result = SharedResourcesWrapper.SendChar(":P#");
            //:P# Toggles High Precsion Pointing. When High precision pointing is enabled scope will first allow the operator to center a nearby bright star before moving to the actual target.
            //Returns: <string>
            //“HIGH PRECISION” Current setting after this command.
            //“LOW PRECISION” Current setting after this command.

            int throwAwayCharacters = "LOW PRECISION".Length - 1;

            LogMessage("TogglePrecision", $"Result: {result}");
            bool highPrecision = false;
            switch (result)
            {
                case "H":
                    highPrecision = true;
                    throwAwayCharacters = "HIGH PRECISION".Length - 1;
                    break;
            }

            SharedResourcesWrapper.ReadCharacters(throwAwayCharacters);

            //Make sure that the buffers are cleared out.
            SharedResourcesWrapper.SendBlind("#");

            return highPrecision;
        }

        private void TelescopePointingPrecision(bool high)
        {
            var currentPrecision = TogglePrecision();

            while (currentPrecision != high)
            {
                currentPrecision = TogglePrecision();
            }
        }

        public void SelectSite(int site)
        {
            CheckConnectedAndValidateSite(site, "SelectSite");

            SharedResourcesWrapper.SendBlind($":W{site}#");
            //:W<n>#
            //Set current site to<n>, an ASCII digit in the range 1..4
            //Returns: Nothing
        }

        private void CheckConnectedAndValidateSite(int site, string message)
        {
            CheckConnected(message);

            if (site < 1)
                throw new ArgumentOutOfRangeException(nameof(site), site,
                    Resources.Telescope_SelectSite_Site_cannot_be_lower_than_1);
            if (site > 4)
                throw new ArgumentOutOfRangeException(nameof(site), site,
                    Resources.Telescope_SelectSite_Site_cannot_be_higher_than_4);
        }

        private void SetSiteName(int site, string sitename)
        {
            CheckConnectedAndValidateSite(site, "SetSiteName");

            string command;
            switch (site)
            {
                case 1:
                    command = $":SM{sitename}#";
                    //:SM<string>#
                    //Set site 1’s name to be<string>.LX200s only accept 3 character strings. Other scopes accept up to 15 characters.
                    //    Returns:
                    //0 – Invalid
                    //1 - Valid
                    break;
                case 2:
                    command = $":SN{sitename}#";
                    //:SN<string>#
                    //Set site 2’s name to be<string>.LX200s only accept 3 character strings. Other scopes accept up to 15 characters.
                    //    Returns:
                    //0 – Invalid
                    //1 - Valid
                    break;
                case 3:
                    command = $":SO{sitename}#";
                    //:SO<string>#
                    //Set site 3’s name to be<string>.LX200s only accept 3 character strings. Other scopes accept up to 15 characters.
                    //    Returns:
                    //0 – Invalid
                    //1 - Valid
                    break;
                case 4:
                    command = $":SP{sitename}#";
                    //:SP<string>#
                    //Set site 4’s name to be<string>.LX200s only accept 3 character strings. Other scopes accept up to 15 characters.
                    //    Returns:
                    //0 – Invalid
                    //1 - Valid
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(site), site, Resources.Telescope_GetSiteName_Site_out_of_range);
            }

            var result = SharedResourcesWrapper.SendChar(command);
            if (result != "1")
            {
                throw new InvalidOperationException("Failed to set site name.");
            }
        }

        private string GetSiteName(int site)
        {
            CheckConnectedAndValidateSite(site, "GetSiteName");

            switch (site)
            {
                case 1:
                    return SharedResourcesWrapper.SendString(":GM#");
                    //:GM# Get Site 1 Name
                    //Returns: <string>#
                    //A ‘#’ terminated string with the name of the requested site.
                case 2:
                    return SharedResourcesWrapper.SendString(":GN#");
                    //:GN# Get Site 2 Name
                    //Returns: <string>#
                    //A ‘#’ terminated string with the name of the requested site.
                case 3:
                    return SharedResourcesWrapper.SendString(":GO#");
                    //:GO# Get Site 3 Name
                    //Returns: <string>#
                    //A ‘#’ terminated string with the name of the requested site.
                case 4:
                    return SharedResourcesWrapper.SendString(":GP#");
                    //:GP# Get Site 4 Name
                    //Returns: <string>#
                    //A ‘#’ terminated string with the name of the requested site.
                default:
                    throw new ArgumentOutOfRangeException(nameof(site), site, Resources.Telescope_GetSiteName_Site_out_of_range);
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
            SharedResourcesWrapper.SendBlind(":Q#");
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

                var alignmentString = SharedResourcesWrapper.SendChar(ack.ToString());
                //ACK <0x06> Query of alignment mounting mode.
                //Returns:
                //A If scope in AltAz Mode
                //D If scope is currently in the Downloader[Autostar II & Autostar]
                //L If scope in Land Mode
                //P If scope in Polar Mode

                //todo implement GW Command - Supported in Autostar 43Eg and above
                //if FirmwareIsGreaterThan(TelescopeList.Autostar497_43EG)
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
                if (!FirmwareIsGreaterThan(TelescopeList.Autostar497_43Eg))
                    throw new PropertyNotImplementedException("AlignmentMode",true );

                switch (value)
                {
                    case AlignmentModes.algAltAz:
                        SharedResourcesWrapper.SendBlind(":AA#");
                        //:AA# Sets telescope the AltAz alignment mode
                        //Returns: nothing
                        break;
                    case AlignmentModes.algPolar:
                    case AlignmentModes.algGermanPolar:
                        SharedResourcesWrapper.SendBlind(":AP#");
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

                //firmware bug in 44Eg, :GA# is returning the dec, not the altitude!
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
            var altitudeData = SharedResourcesWrapper.Lock(() => new AltitudeData
            {
                UtcDateTime = UTCDate,
                SiteLongitude = SiteLongitude,
                SiteLatitude = SiteLatitude,
                EquatorialCoordinates = GetTelescopeRaAndDec()
            });

            double hourAngle = _astroMaths.RightAscensionToHourAngle(altitudeData.UtcDateTime, altitudeData.SiteLongitude,
                altitudeData.EquatorialCoordinates.RightAscension);
            var altAz = _astroMaths.ConvertEqToHoz(hourAngle, altitudeData.SiteLatitude, altitudeData.EquatorialCoordinates);
            return altAz;
        }

        private EquatorialCoordinates GetTelescopeRaAndDec()
        {
            return new EquatorialCoordinates
            {
                RightAscension = RightAscension,
                Declination = Declination
            };
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
                LogMessage("AtHome", "Get - " + false);
                return false;
            }
        }

        private bool _atPark;

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
            LogMessage("AxisRates", "Get - " + axis);
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
                LogMessage("CanFindHome", "Get - " + false);
                return false;
            }
        }

        public bool CanMoveAxis(TelescopeAxes axis)
        {
            LogMessage("CanMoveAxis", "Get - " + axis);
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
                LogMessage("CanPark", "Get - " + true);
                return true;
            }
        }

        public bool CanPulseGuide
        {
            get
            {
                LogMessage("CanPulseGuide", "Get - " + true);
                return true;
            }
        }

        public bool CanSetDeclinationRate
        {
            get
            {
                LogMessage("CanSetDeclinationRate", "Get - " + false);
                return false;
            }
        }

        public bool CanSetGuideRates
        {
            get
            {
                CheckConnected("CanSetGuideRates Get");

                var canSetGuideRate = IsGuideRateSettingSupported();

                LogMessage("CanSetGuideRates", "Get - " + canSetGuideRate);
                return canSetGuideRate;
            }
        }

        public bool CanSetPark
        {
            get
            {
                LogMessage("CanSetPark", "Get - " + false);
                return false;
            }
        }

        public bool CanSetPierSide
        {
            get
            {
                LogMessage("CanSetPierSide", "Get - " + false);
                return false;
            }
        }

        public bool CanSetRightAscensionRate
        {
            get
            {
                LogMessage("CanSetRightAscensionRate", "Get - " + false);
                return false;
            }
        }

        public bool CanSetTracking
        {
            get
            {
                LogMessage("CanSetTracking", "Get - " + true);
                return true;
            }
        }

        public bool CanSlew
        {
            get
            {
                LogMessage("CanSlew", "Get - " + true);
                return true;
            }
        }

        public bool CanSlewAltAz
        {
            get
            {
                LogMessage("CanSlewAltAz", "Get - " + true);
                return true;
            }
        }

        public bool CanSlewAltAzAsync
        {
            get
            {
                LogMessage("CanSlewAltAzAsync", "Get - " + true);
                return true;
            }
        }

        public bool CanSlewAsync
        {
            get
            {
                LogMessage("CanSlewAsync", "Get - " + true);
                return true;
            }
        }

        public bool CanSync
        {
            get
            {
                LogMessage("CanSync", "Get - " + true);
                return true;
            }
        }

        public bool CanSyncAltAz
        {
            get
            {
                LogMessage("CanSyncAltAz", "Get - " + false);
                return false;
            }
        }

        public bool CanUnpark
        {
            get
            {
                //todo make this return false for non LX-200 GPS telescopes
                LogMessage("CanUnpark", "Get - " + true);
                return true;
            }
        }

        public double Declination
        {
            get
            {
                CheckConnected("Declination Get");

                var result = SharedResourcesWrapper.SendString(":GD#");
                //:GD# Get Telescope Declination.
                //Returns: sDD*MM# or sDD*MM’SS#
                //Depending upon the current precision setting for the telescope.

                double declination = _utilities.DMSToDegrees(result);

                LogMessage("Declination", $"Get - {result} convert to {declination} {_utilitiesExtra.DegreesToDMS(declination, ":", ":")}");
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
            // ReSharper disable once ValueParameterNotUsed
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
            // ReSharper disable once ValueParameterNotUsed
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
                LogMessage("DeclinationRate", "Get - " + equatorialSystem);
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

        private void SetNewGuideRate(double value, string propertyName)
        {
            if (!IsGuideRateSettingSupported())
            {
                LogMessage($"{propertyName} Set", "Not implemented");
                throw new PropertyNotImplementedException(propertyName, true);
            }

            if (!value.InRange(0, 15.0417))
            {
                throw new InvalidValueException(propertyName, value.ToString(CultureInfo.CurrentCulture), $"{0.ToString(CultureInfo.CurrentCulture)} to {15.0417.ToString(CultureInfo.CurrentCulture)}”/sec");
            }

            LogMessage($"{propertyName} Set", $"Setting new guiderate {value.ToString(CultureInfo.CurrentCulture)} arc seconds/second ({value.ToString(CultureInfo.CurrentCulture)} degrees/second)");
            SharedResourcesWrapper.SendBlind($":Rg{value:00.0}#");
            //:RgSS.S#
            //Set guide rate to +/ -SS.S to arc seconds per second.This rate is added to or subtracted from the current tracking
            //Rates when the CCD guider or handbox guider buttons are pressed when the guide rate is selected.Rate shall not exceed
            //sidereal speed(approx 15.0417”/sec)[Autostar II only]
            //Returns: Nothing

            //info from RickB says that 15.04107 is a better value for 

            GuideRate = value;

            WriteProfile();
        }

        private double DegreesPerSecondToArcSecondPerSecond(double value)
        {
            return value * 3600.0;
        }

        private double ArcSecondPerSecondToDegreesPerSecond(double value)
        {
            return value / 3600.0;
        }

        public double GuideRateDeclination
        {
            get
            {
                var degreesPerSecond = ArcSecondPerSecondToDegreesPerSecond(GuideRate);
                LogMessage("GuideRateDeclination Get", $"{GuideRate} arc seconds / second = {degreesPerSecond} degrees per second");
                return degreesPerSecond;
            }
            set
            {
                var newValue = DegreesPerSecondToArcSecondPerSecond(value);
                SetNewGuideRate(newValue, "GuideRateDeclination");
            }
        }
        
        public double GuideRateRightAscension
        {
            get
            {
                double degreesPerSecond = ArcSecondPerSecondToDegreesPerSecond(GuideRate);
                LogMessage("GuideRateRightAscension Get", $"{GuideRate} arc seconds / second = {degreesPerSecond} degrees per second");
                return degreesPerSecond;
            }
            set
            {
                var newValue = DegreesPerSecondToArcSecondPerSecond(value);
                SetNewGuideRate(newValue, "GuideRateRightAscension");
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
                    SharedResourcesWrapper.SendBlind(":RG#");
                    //:RG# Set Slew rate to Guiding Rate (slowest)
                    //Returns: Nothing
                    break;
                case 2:
                    SharedResourcesWrapper.SendBlind(":RC#");
                    //:RC# Set Slew rate to Centering rate (2nd slowest)
                    //Returns: Nothing
                    break;
                case 3:
                    SharedResourcesWrapper.SendBlind(":RM#");
                    //:RM# Set Slew rate to Find Rate (2nd Fastest)
                    //Returns: Nothing
                    break;
                case 4:
                    SharedResourcesWrapper.SendBlind(":RS#");
                    //:RS# Set Slew rate to max (fastest)
                    //Returns: Nothing
                    break;
                default:
                    throw new InvalidValueException($"Rate {rate} not supported");
            }

            switch (axis)
            {
                case TelescopeAxes.axisPrimary:
                    switch (rate.Compare(0))
                    {
                        case ComparisonResult.Equals:
                            _movingPrimary = false;
                            SharedResourcesWrapper.SendBlind(":Qe#");
                            //:Qe# Halt eastward Slews
                            //Returns: Nothing
                            SharedResourcesWrapper.SendBlind(":Qw#");
                            //:Qw# Halt westward Slews
                            //Returns: Nothing
                            break;
                        case ComparisonResult.Greater:

                            SharedResourcesWrapper.SendBlind(":Me#");
                            //:Me# Move Telescope East at current slew rate
                            //Returns: Nothing
                            _movingPrimary = true;
                            break;
                        case ComparisonResult.Lower:
                            SharedResourcesWrapper.SendBlind(":Mw#");
                            //:Mw# Move Telescope West at current slew rate
                            //Returns: Nothing
                            _movingPrimary = true;
                            break;
                    }
                    break;
                case TelescopeAxes.axisSecondary:
                    switch (rate.Compare(0))
                    {
                        case ComparisonResult.Equals:
                            _movingSecondary = false;
                            SharedResourcesWrapper.SendBlind(":Qn#");
                            //:Qn# Halt northward Slews
                            //Returns: Nothing
                            SharedResourcesWrapper.SendBlind(":Qs#");
                            //:Qs# Halt southward Slews
                            //Returns: Nothing
                            break;
                        case ComparisonResult.Greater:
                            SharedResourcesWrapper.SendBlind(":Mn#");
                            //:Mn# Move Telescope North at current slew rate
                            //Returns: Nothing
                            _movingSecondary = true;
                            break;
                        case ComparisonResult.Lower:
                            SharedResourcesWrapper.SendBlind(":Ms#");
                            //:Ms# Move Telescope South at current slew rate
                            //Returns: Nothing
                            _movingSecondary = true;
                            break;

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

            SharedResourcesWrapper.SendBlind(":hP#");
            //:hP# Autostar, Autostar II and LX 16”Slew to Park Position
            //Returns: Nothing
            AtPark = true;
        }

        private bool _userNewerPulseGuiding = true;

        public void PulseGuide(GuideDirections direction, int duration)
        {
            LogMessage("PulseGuide", $"pulse guide direction {direction} duration {duration}");
            try
            {
                CheckConnected("PulseGuide");
                if (IsSlewingToTarget())
                    throw new InvalidOperationException("Unable to PulseGuide whilst slewing to target.");

                _isGuiding = true;
                try
                {
                    if (_movingPrimary &&
                        (direction == GuideDirections.guideEast || direction == GuideDirections.guideWest))
                        throw new InvalidOperationException("Unable to PulseGuide while moving same axis.");

                    if (_movingSecondary &&
                        (direction == GuideDirections.guideNorth || direction == GuideDirections.guideSouth))
                        throw new InvalidOperationException("Unable to PulseGuide while moving same axis.");

                    var coordinatesBeforeMove = GetTelescopeRaAndDec();

                    if (_userNewerPulseGuiding && duration < 10000)
                    {
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

                        LogMessage("PulseGuide", "Using new pulse guiding technique");
                        SharedResourcesWrapper.SendBlind($":Mg{d}{duration:0000}#");
                        //:MgnDDDD#
                        //:MgsDDDD#
                        //:MgeDDDD#
                        //:MgwDDDD#
                        //Guide telescope in the commanded direction(nsew) for the number of milliseconds indicated by the unsigned number
                        //passed in the command.These commands support serial port driven guiding.
                        //Returns – Nothing
                        //LX200 – Not Supported
                        _utilities.WaitForMilliseconds(duration);
                    }
                    else
                    {
                        LogMessage("PulseGuide", "Using old pulse guiding technique");
                        switch (direction)
                        {
                            case GuideDirections.guideEast:
                                MoveAxis(TelescopeAxes.axisPrimary, 1);
                                _utilities.WaitForMilliseconds(duration);
                                MoveAxis(TelescopeAxes.axisPrimary, 0);
                                break;
                            case GuideDirections.guideNorth:
                                MoveAxis(TelescopeAxes.axisSecondary, 1);
                                _utilities.WaitForMilliseconds(duration);
                                MoveAxis(TelescopeAxes.axisSecondary, 0);
                                break;
                            case GuideDirections.guideSouth:
                                MoveAxis(TelescopeAxes.axisSecondary, -1);
                                _utilities.WaitForMilliseconds(duration);
                                MoveAxis(TelescopeAxes.axisSecondary, 0);
                                break;
                            case GuideDirections.guideWest:
                                MoveAxis(TelescopeAxes.axisPrimary, -1);
                                _utilities.WaitForMilliseconds(duration);
                                MoveAxis(TelescopeAxes.axisPrimary, 0);
                                break;
                        }

                        LogMessage("PulseGuide", "Using old pulse guiding technique complete");
                    }

                    var coordinatesAfterMove = GetTelescopeRaAndDec();

                    LogMessage("PulseGuide",
                        $"Complete Before RA: {_utilitiesExtra.HoursToHMS(coordinatesBeforeMove.RightAscension)} Dec:{_utilitiesExtra.DegreesToDMS(coordinatesBeforeMove.Declination)}");
                    LogMessage("PulseGuide",
                        $"Complete After RA: {_utilitiesExtra.HoursToHMS(coordinatesAfterMove.RightAscension)} Dec:{_utilitiesExtra.DegreesToDMS(coordinatesAfterMove.Declination)}");
                }
                finally
                {
                    _isGuiding = false;
                }
            }
            catch (Exception ex)
            {
                LogMessage("PulseGuide", $"Error performing pulse guide: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// convert a HH:MM.T (classic LX200 RA Notation) string to a double hours. T is the decimal part of minutes which is converted into seconds
        /// </summary>
        public double HMToHours(string hm)
        {
            var token = hm.Split('.');
            if (token.Length != 2)
                return _utilities.HMSToHours(hm);

            var seconds = short.Parse(token[1]) * 6;
            var hms = $"{token[0]}:{seconds}";
            return _utilities.HMSToHours(hms);
        }

        public double RightAscension
        {
            get
            {
                CheckConnected("RightAscension Get");
                var result = SharedResourcesWrapper.SendString(":GR#");
                //:GR# Get Telescope RA
                //Returns: HH:MM.T# or HH:MM:SS#
                //Depending which precision is set for the telescope

                double rightAscension = HMToHours(result);

                LogMessage("RightAscension", $"Get - {result} convert to {rightAscension} {_utilitiesExtra.HoursToHMS(rightAscension)}");
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
            // ReSharper disable once ValueParameterNotUsed
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
            // ReSharper disable once ValueParameterNotUsed
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
                using (var novas = new NOVAS31())
                {
                    var jd = _utilities.DateUTCToJulian(DateTime.UtcNow);
                    novas.SiderealTime(jd, 0, novas.DeltaT(jd),
                        GstType.GreenwichApparentSiderealTime,
                        Method.EquinoxBased,
                        Accuracy.Reduced, ref siderealTime);
                }

                // Allow for the longitude
                siderealTime += SiteLongitude / 360.0 * 24.0;

                // Reduce to the range 0 to 24 hours
                siderealTime = _astroUtilities.ConditionRA(siderealTime);

                LogMessage("SiderealTime", "Get - " + siderealTime.ToString(CultureInfo.InvariantCulture));
                return siderealTime;
            }
        }

        public new double SiteElevation
        {
            get
            {
                CheckConnected("SiteElevation Get");

                LogMessage("SiteElevation",  $"Get {base.SiteElevation}");
                return base.SiteElevation;
            }
            // ReSharper disable once ValueParameterNotUsed
            set
            {
                CheckConnected("SiteElevation Set");

                LogMessage("SiteElevation", $"Set: {value}");
                if (value == base.SiteElevation)
                {
                    LogMessage("SiteElevation", $"Set: no change detected");
                    return;
                }
                
                LogMessage("SiteElevation", $"Set: {value} was {base.SiteElevation}");
                base.SiteElevation = value;
                base.UpdateSiteElevation();
            }
        }

        public double SiteLatitude
        {
            get
            {
                CheckConnected("SiteLatitude Get");

                var latitude = SharedResourcesWrapper.SendString(":Gt#");
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

                var result = SharedResourcesWrapper.SendChar(commandString);
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

                var longitude = SharedResourcesWrapper.SendString(":Gg#");
                //:Gg# Get Current Site Longitude
                //Returns: sDDD*MM#
                //The current site Longitude. East Longitudes are expressed as negative
                double siteLongitude = -_utilities.DMSToDegrees(longitude);

                if (siteLongitude < -180)
                    siteLongitude = siteLongitude + 360;
                
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

                var result = SharedResourcesWrapper.SendChar(commandstring);
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
                CheckConnected("SlewSettleTime Get");
                LogMessage("SlewSettleTime Get", $"{_settleTime} Seconds");
                return _settleTime;
            }
            set
            {
                CheckConnected("SlewSettleTime Set");
                LogMessage("SlewSettleTime Set", $"Setting from {_settleTime} to {value}");
                _settleTime = value;
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

            HorizonCoordinates altAz = new HorizonCoordinates {Azimuth = azimuth, Altitude = altitude};

            var utcDateTime = UTCDate;
            var latitude = SiteLatitude;
            var longitude = SiteLongitude;

            SharedResourcesWrapper.Lock(() =>
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

            SharedResourcesWrapper.Lock(() =>
            {
                switch (polar)
                {
                    case true:
                        var response = SharedResourcesWrapper.SendChar(":MS#");
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
                                string belowHorizonMessage = SharedResourcesWrapper.ReadTerminated();
                                LogMessage("DoSlewAsync", $"Slew failed \"{belowHorizonMessage}\"");
                                throw new InvalidOperationException(belowHorizonMessage);
                            case "2":
                                //Below minimum elevation 
                                string belowMinimumElevationMessage = SharedResourcesWrapper.ReadTerminated();
                                LogMessage("DoSlewAsync", $"Slew failed \"{belowMinimumElevationMessage}\"");
                                throw new InvalidOperationException(belowMinimumElevationMessage);
                            case "3":
                                //Telescope can hit the mount
                                string canHitMountMessage = SharedResourcesWrapper.ReadTerminated();
                                LogMessage("DoSlewAsync", $"Slew failed \"{canHitMountMessage}\"");
                                throw new InvalidOperationException(canHitMountMessage);
                            default:
                                LogMessage("DoSlewAsync", $"Slew failed - unknown response \"{response}\"");
                                throw new DriverException("This error should not happen");

                        }

                        break;
                    case false:
                        var maResponse = SharedResourcesWrapper.SendChar(":MA#");
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

            SharedResourcesWrapper.Lock(() =>
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

            if (TargetDeclination.Equals(InvalidParameter) || TargetRightAscension.Equals(InvalidParameter))
                throw new InvalidOperationException("No target selected to slew to.");

            DoSlewAsync(true);
        }

        private bool MovingAxis()
        {
            if (_isGuiding)
                return false;

            return _movingPrimary || _movingSecondary;
        }

        private DateTime _earliestNonSlewingTime = DateTime.MinValue;

        public bool Slewing
        {
            get
            {
                var isSlewing = GetSlewing();

                if (isSlewing)
                    _earliestNonSlewingTime = _clock.UtcNow + GetTotalSlewingSettleTime();
                else if (_clock.UtcNow < _earliestNonSlewingTime)
                    isSlewing = true;

                LogMessage("Slewing", $"Result = {isSlewing}");
                return isSlewing;
            }
        }

        private TimeSpan GetTotalSlewingSettleTime()
        {
            return TimeSpan.FromSeconds( SlewSettleTime + ProfileSettleTime );
        }

        private bool GetSlewing()
        {
            if (!Connected) return false;


            if (MovingAxis())
                return true;

            return IsSlewingToTarget();
        }

        private bool IsSlewingToTarget()
        {
            CheckConnected("IsSlewingToTarget");

            if (_isGuiding)
                return false;

            var result = SharedResourcesWrapper.SendString(":D#");
            //:D# Requests a string of bars indicating the distance to the current target location.
            //Returns:
            //LX200's – a string of bar characters indicating the distance.
            //Autostars and Autostar II – a string containing one bar until a slew is complete, then a null string is returned.

            bool isSlewing = false;
            try
            {
                if (string.IsNullOrEmpty(result))
                {
                    isSlewing = false;
                    return isSlewing;
                }

                if (result.Contains("|"))
                {
                    isSlewing = true;
                    return isSlewing;
                }

                ////classic LX200 return bar with 32 chars. FF is contained  from left to right when slewing
                //byte[] ba = Encoding.Default.GetBytes(result);
                ////replace fill chars not belonging to a slew bar.  Are there others? The bar character is a FF in hex.
                //var hexString = BitConverter.ToString(ba).Replace("-", "").Replace("20", "");
                //LogMessage("IsSlewingToTarget", $"Resulthex  = {hexString}");
                //isSlewing = (hexString.Length > 0);

                //if (!isSlewing)
                //    return isSlewing;

                ////classic LX200 got RA 0 DE 0 as Target Coordinates. If the RA DE is not 0 at switch on, the telescope will indicate slewing until
                ////the target coordinates are set and the telescope is slewed to that position.
                ////a 0 movement will solved that lock if the target coordinates are set to the current coordinates.
                //if (IsTargetCoordinateInitRequired())
                //    InitTargetCoordinates();

                return isSlewing;
            }
            finally
            {
                LogMessage("IsSlewingToTarget", $"IsSlewing = {isSlewing} : result = {result ?? "<null>"}");
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
            LogMessage("SyncToCoordinates", $"RA={_utilitiesExtra.HoursToHMS(rightAscension)} Dec={_utilitiesExtra.HoursToHMS(declination)}");
            CheckConnected("SyncToCoordinates");

            SharedResourcesWrapper.Lock(() =>
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

            var result = SharedResourcesWrapper.SendString(":CM#");
            //:CM# Synchronizes the telescope's position with the currently selected database object's coordinates.
            //Returns:
            //LX200's - a "#" terminated string with the name of the object that was synced.
            //    Autostars & Autostar II - A static string: " M31 EX GAL MAG 3.5 SZ178.0'#"

            if (string.IsNullOrWhiteSpace(result))
                throw new InvalidOperationException("Unable to perform sync");

            // At least the classic LX200 low precision might not slew to the exact target position
            // This Requires to retrieve the aimed target ra de from the telescope
            double ra = RightAscension;
            if (_targetRightAscension != InvalidParameter &&
                _utilities.HoursToHMS(ra, ":", ":", ":", _digitsRa) != _utilities.HoursToHMS(_targetRightAscension, ":", ":", ":", _digitsRa))
            {
                LogMessage("SyncToTarget", $"differ RA real {ra} targeted {_targetRightAscension}");
                _targetRightAscension = ra;
            }
            double de = Declination;
            if (_targetDeclination != InvalidParameter &&
                _utilities.DegreesToDMS(de, "*", ":", ":", _digitsDe) != _utilities.DegreesToDMS(_targetDeclination, "*", ":", ":", _digitsDe))
            {
                LogMessage("SyncToTarget", $"differ DE real {de} targeted {_targetDeclination}");
                _targetDeclination = de;
            }
        }

        private double _targetDeclination = InvalidParameter;
        public double TargetDeclination
        {
            get
            {
                if (_targetDeclination.Equals(InvalidParameter))
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

                if (value > 90)
                    throw new InvalidValueException("Declination cannot be greater than 90.");

                if (value < -90)
                    throw new InvalidValueException("Declination cannot be less than -90.");

                var dms = "";
                if (IsLongFormat)
                    dms = _utilities.DegreesToDMS(value, "*", ":", ":", _digitsDe);
                else
                    dms = _utilities.DegreesToDM(value, "*", "", _digitsDe);

                var s = value < 0 ? string.Empty : "+";

                var command = $":Sd{s}{dms}#";

                LogMessage("TargetDeclination Set", $"{command}");
                var result = SharedResourcesWrapper.SendChar(command);
                //:SdsDD*MM#
                //Set target object declination to sDD*MM or sDD*MM:SS depending on the current precision setting
                //Returns:
                //1 - Dec Accepted
                //0 – Dec invalid

                if (result == "0")
                {
                    throw new InvalidOperationException("Target declination invalid");
                }

                _targetDeclination = _utilities.DMSToDegrees(dms);
            }
        }

        private double _targetRightAscension = InvalidParameter;
        public double TargetRightAscension
        {
            get
            {
                if (_targetRightAscension.Equals(InvalidParameter))
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

                var hms = "";
                if(IsLongFormat)
                    hms = _utilities.HoursToHMS(value, ":", ":", ":", _digitsRa);
                else
                    //meade protocol defines H:MM.T format
                    hms = _utilities.HoursToHM(value, ":", "", _digitsRa).Replace(',','.');

                var command = $":Sr{hms}#";
                LogMessage("TargetRightAscension Set", $"{command}");
                var response = SharedResourcesWrapper.SendChar(command);
                //:SrHH:MM.T#
                //:SrHH:MM:SS#
                //Set target object RA to HH:MM.T or HH: MM: SS depending on the current precision setting.
                //    Returns:
                //0 – Invalid
                //1 - Valid

                if (response == "0")
                    throw new InvalidOperationException("Failed to set TargetRightAscension.");

                _targetRightAscension = _utilities.HMSToHours(hms);
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
                LogMessage("Tracking Set", $"{value}");
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
                        SharedResourcesWrapper.SendBlind(":TQ#");
                        //:TQ# Selects sidereal tracking rate
                        //Returns: Nothing
                        break;
                    case DriveRates.driveLunar:
                        SharedResourcesWrapper.SendBlind(":TL#");
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
                    LogMessage("TrackingRates", "Get - " + driveRate);
                }
                return trackingRates;
            }
        }

        private TimeSpan GetUtcCorrection()
        {
            string utcOffSet = SharedResourcesWrapper.SendString(":GG#");
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

                var telescopeDateDetails = SharedResourcesWrapper.Lock(() =>
                {
                    var tdd = new TelescopeDateDetails
                    {
                        TelescopeDate = SharedResourcesWrapper.SendString(":GC#"),
                        //:GC# Get current date.
                        //Returns: MM/DD/YY#
                        //The current local calendar date for the telescope.
                        TelescopeTime = SharedResourcesWrapper.SendString(":GL#"),
                        //:GL# Get Local Time in 24 hour format
                        //Returns: HH:MM:SS#
                        //The Local Time in 24 - hour Format
                        UtcCorrection = GetUtcCorrection()
                    };

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

                SharedResourcesWrapper.Lock(() =>
                {
                    var utcCorrection = GetUtcCorrection();
                    var localDateTime = value - utcCorrection;

                    string localStingCommand = $":SL{localDateTime:HH:mm:ss}#";
                    var timeResult = SharedResourcesWrapper.SendChar(localStingCommand);
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
                    var dateResult = SharedResourcesWrapper.SendChar(localDateCommand);
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
                    SharedResourcesWrapper.ReadTerminated(); //Updating Planetary Data#
                    SharedResourcesWrapper.ReadTerminated(); //                       #
                });
            }
        }

        public void Unpark()
        {
            LogMessage("Unpark", "Unparking telescope");

            //todo make this return only work for LX-200 GPS telescopes
            if (!AtPark)
                return;

            SharedResourcesWrapper.SendChar(":I#");
            //:I# LX200 GPS Only - Causes the telescope to cease current operations and restart at its power on initialization.
            //Returns: X once the handset restart has completed

            var utcCorrection = GetUtcCorrection();
            var localDateTime = DateTime.UtcNow - utcCorrection;

            //localDateTime: HH: mm: ss
            SharedResourcesWrapper.SendBlind($":hI{localDateTime:yyMMddhhmmss}#");
            //:hIYYMMDDHHMMSS#
            //Bypass handbox entry of daylight savings, date and time.Use the values supplied in this command.This feature is
            //intended to allow use of the Autostar II from permanent installations where GPS reception is not possible, such as within
            //metal domes. This command must be issued while the telescope is waiting at the initial daylight savings prompt.
            //Returns: 1 – if command was accepted.

            AtPark = false;
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
            using (IProfileWrapper p = ProfileFactory.Create())
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

        private void WriteProfile()
        {
            var profileProperties = new ProfileProperties
            {
                TraceLogger = Tl.Enabled,
                ComPort = ComPort,
                GuideRateArcSecondsPerSecond = GuideRate
            };

            SharedResourcesWrapper.WriteProfile(profileProperties);
        }
        #endregion
    }
}

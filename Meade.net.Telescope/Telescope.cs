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
        private static readonly string DriverId =
            Marshal.GenerateProgIdForType(MethodBase.GetCurrentMethod().DeclaringType ??
                                          throw new System.InvalidOperationException());

        /// <summary>
        /// Private variable to hold an ASCOM Utilities object
        /// </summary>
        private readonly IUtil _utilities;

        private readonly IUtilExtra _utilitiesExtra;

        /// <summary>
        /// Private variable to hold an ASCOM AstroUtilities object to provide the <see cref="IAstroUtils.Range(double, double, bool, double, bool)"> method
        /// and <see cref="IAstroUtils.ConditionHA(double)"/>
        /// </summary>
        private readonly IAstroUtils _astroUtilities;

        private readonly IAstroMaths _astroMaths;

        private readonly IClock _clock;

        private readonly INOVAS31 _novas;

        /// <summary>
        /// Private variable to hold number of decimals for RA
        /// </summary>
        private int _digitsRa = 2;

        /// <summary>
        /// Private variable to hold number of decimals for Dec
        /// </summary>
        private int _digitsDe = 2;

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
                _novas = new NOVAS31();

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

        public Telescope(IUtil util, IUtilExtra utilExtra, IAstroUtils astroUtilities,
            ISharedResourcesWrapper sharedResourcesWrapper, IAstroMaths astroMaths, IClock clock, INOVAS31 novas) : base(
            sharedResourcesWrapper)
        {
            _clock = clock;
            _utilities = util; //Initialise util object
            _utilitiesExtra = utilExtra; //Initialise util object
            _astroUtilities = astroUtilities; // Initialise astro utilities object
            _astroMaths = astroMaths;
            _novas = novas;

            Initialise(nameof(Telescope));
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
                            var output = SharedResourcesWrapper.SendString("ED");
                            return output;

                        //top row of buttons
                        case "enter":
                            SharedResourcesWrapper.SendBlind("EK13");
                            break;
                        case "mode":
                            SharedResourcesWrapper.SendBlind("EK9");
                            break;
                        case "longmode":
                            SharedResourcesWrapper.SendBlind("EK11");
                            break;
                        case "goto":
                            SharedResourcesWrapper.SendBlind("EK24");
                            break;

                        case "0": //light and 0
                            SharedResourcesWrapper.SendBlind("EK48");
                            break;
                        case "1":
                            SharedResourcesWrapper.SendBlind("EK49");
                            break;
                        case "2":
                            SharedResourcesWrapper.SendBlind("EK50");
                            break;
                        case "3":
                            SharedResourcesWrapper.SendBlind("EK51");
                            break;
                        case "4":
                            SharedResourcesWrapper.SendBlind("EK52");
                            break;
                        case "5":
                            SharedResourcesWrapper.SendBlind("EK53");
                            break;
                        case "6":
                            SharedResourcesWrapper.SendBlind("EK54");
                            break;
                        case "7":
                            SharedResourcesWrapper.SendBlind("EK55");
                            break;
                        case "8":
                            SharedResourcesWrapper.SendBlind("EK56");
                            break;
                        case "9":
                            SharedResourcesWrapper.SendBlind("EK57");
                            break;

                        case "up":
                            SharedResourcesWrapper.SendBlind("EK94");
                            break;
                        case "down":
                            SharedResourcesWrapper.SendBlind("EK118");
                            break;
                        case "back":
                        case "left":
                            SharedResourcesWrapper.SendBlind("EK87");
                            break;
                        case "forward":
                        case "right":
                            SharedResourcesWrapper.SendBlind("EK69");
                            break;
                        case "scrollup":
                            SharedResourcesWrapper.SendBlind("EK85");
                            break;
                        case "scrolldown":
                            SharedResourcesWrapper.SendBlind("EK68");
                            break;
                        case "?":
                            SharedResourcesWrapper.SendBlind("EK63");
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
            LogMessage("CommandBlind", $"raw: {raw} command {command}");
            CheckConnected("CommandBlind");
            // Call CommandString and return as soon as it finishes
            //this.CommandString(command, raw);
            SharedResourcesWrapper.SendBlind(command, raw);
            // or
            //throw new ASCOM.MethodNotImplementedException("CommandBlind");
            // DO NOT have both these sections!  One or the other
            LogMessage("CommandBlind", "Completed");
        }

        public bool CommandBool(string command, bool raw)
        {
            LogMessage("CommandBool", $"raw: {raw} command {command}");
            CheckConnected("CommandBool");
            var result = SharedResourcesWrapper.SendBool(command, raw);
            LogMessage("CommandBool", $"Completed: {result}");
            return result;
        }

        public string CommandString(string command, bool raw)
        {
            LogMessage("CommandString", $"raw: {raw} command {command}");
            CheckConnected("CommandString");
            // it's a good idea to put all the low level communication with the device here,
            // then all communication calls this function
            // you need something to ensure that only one command is in progress at a time
            string result;
            // :GW# is not terminated with a # for some reason, see reported comment
            // https://bitbucket.org/cjdskunkworks/meadeautostar497/issues/24/get-set-tracking#comment-60586901
            if (command == (raw ? ":GW#" : "GW"))
            {
                result = SharedResourcesWrapper.SendChars(command, raw, count: 3);
            }
            else
            {
                result = SharedResourcesWrapper.SendString(command, raw);
            }
            LogMessage("CommandString", $"Completed: {result}");
            return result;
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

                        LogMessage("Connected Set", "Connecting to port {0}", _profileProperties.ComPort);
                        var connectionInfo = SharedResourcesWrapper.Connect("Serial", DriverId, Tl);
                        try
                        {
                            LogMessage("Connected Set",
                                $"Connected to port {_profileProperties.ComPort}. Product: {SharedResourcesWrapper.ProductName} Version:{SharedResourcesWrapper.FirmwareVersion}");

                            _userNewerPulseGuiding = IsNewPulseGuidingSupported();

                            LogMessage("Connected Set", $"New Pulse Guiding Supported: {_userNewerPulseGuiding}");
                            IsConnected = true;

                            if (connectionInfo.SameDevice == 1)
                            {
                                SharedResourcesWrapper.SetParked(false, null);
                                LogMessage("Connected Set", "Making first connection telescope adjustments");

                                LogMessage("Connected Set", $"Site Longitude: {SiteLongitude}");
                                LogMessage("Connected Set", $"Site Latitude: {SiteLatitude}");

                                //These settings are applied only when the first device connects to the telescope.
                                SetLongFormat(true);

                                if (CanSetGuideRates)
                                {
                                    SetNewGuideRate(_profileProperties.GuideRateArcSecondsPerSecond, "Connect");
                                }

                                SetTelescopePrecision("Connect");

                                // target RA, DEC and SideOfPier are set to default values
                                SharedResourcesWrapper.SideOfPier = PierSide.pierUnknown;
                                SharedResourcesWrapper.TargetDeclination = InvalidParameter;
                                SharedResourcesWrapper.TargetRightAscension = InvalidParameter;

                                LogMessage("Connected Set", $"SendDateTime: {_profileProperties.SendDateTime}");
                                if (_profileProperties.SendDateTime)
                                {
                                    if (SharedResourcesWrapper.ProductName == TelescopeList.LX200GPS)
                                    {
                                        LogMessage("Connected Set",
                                            $"LX200GPS Detecting if daylight savings message on screen: {_profileProperties.SendDateTime}");
                                        var displayText = Action("Handbox", "readdisplay");
                                        LogMessage("Connected Set", $"Current Handset display: {displayText}");
                                        if (displayText.Contains("Daylight"))
                                        {
                                            LogMessage("Connected Set",
                                                $"LX200GPS Setting Date time and bypassing settings screens: {_profileProperties.SendDateTime}");
                                            BypassHandboxEntryForAutostarII();
                                        }
                                        else
                                        {
                                            LogMessage("Connected Set",
                                                $"LX200GPS Sending current date and time: {_profileProperties.SendDateTime}");
                                            SendCurrentDateTime("Connect");
                                            LogMessage("Connected Set",
                                                $"LX200GPS Attempting manual bypass of prompts: {_profileProperties.SendDateTime}");
                                            ApplySkipAutoStarPrompts("Connect");
                                        }

                                    }
                                    else
                                    {
                                        LogMessage("Connected Set", "Autostar Attempting manual bypass of prompts");
                                        ApplySkipAutoStarPrompts("Connect");
                                        SendCurrentDateTime("Connect");
                                    }
                                }
                            }
                            else
                            {
                                LogMessage("Connected Set",
                                    $"Skipping first connection telescope adjustments (current connections: {connectionInfo.SameDevice})");
                                CheckParked();
                            }

                            if (!SharedResourcesWrapper.IsLongFormat)
                            {
                                // use low precision digits
                                _digitsRa = 1;
                                _digitsDe = 0;
                            }

                            var raAndDec = GetTelescopeRaAndDec();
                            LogMessage("Connected Set",
                                $"Connected OK.  Current RA = {_utilitiesExtra.HoursToHMS(raAndDec.RightAscension)} Dec = {_utilitiesExtra.DegreesToDMS(raAndDec.Declination)}");
                        }
                        catch (Exception)
                        {
                            SharedResourcesWrapper.Disconnect("Serial", DriverId);
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage("Connected Set", "Error connecting to port {0} - {1}", _profileProperties.ComPort, ex.Message);
                    }
                }
                else
                {
                    LogMessage("Connected Set", "Disconnecting from port {0}", _profileProperties.ComPort);
                    SharedResourcesWrapper.Disconnect("Serial", DriverId);
                    IsConnected = false;
                }
            }
        }

        private void SendCurrentDateTime(string connect)
        {
            if (_profileProperties.SendDateTime)
            {
                UTCDate = _clock.UtcNow;
            }
        }

        private void ApplySkipAutoStarPrompts(string connect)
        {
            if (SharedResourcesWrapper.ProductName == TelescopeList.LX200GPS)
            {
                var displayText = Action("Handbox", "readdisplay");

                if (displayText.Contains("Daylight"))
                {
                    for (var i = 0; i < 3; i++)
                    {
                        Action("Handbox", "enter");
                        _utilities.WaitForMilliseconds(2000);
                    }
                }
            }
            else if (SharedResourcesWrapper.ProductName == TelescopeList.Autostar497)
            {
                var i = 10;
                while (i > 0)
                {
                    var displayText = Action("Handbox", "readdisplay");
                    if (displayText.Contains("Align:"))
                    {
                        i = 0;
                        continue;
                    }

                    Action("Handbox", "mode");
                    _utilities.WaitForMilliseconds(500);
                    i--;
                }
            }
        }

        private void SetTelescopePrecision(string propertyName)
        {
            switch (_profileProperties.Precision.ToLower())
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
            switch (_profileProperties.GuidingStyle.ToLower())
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

        private bool IsGwCommandSupported()
        {
            switch (SharedResourcesWrapper.ProductName)
            {
                case TelescopeList.LX200CLASSIC:
                    return false;
                case TelescopeList.Autostar497:
                    return FirmwareIsGreaterThan(TelescopeList.Autostar497_43Eg);
                case TelescopeList.LX200GPS:
                    return FirmwareIsGreaterThan(TelescopeList.LX200GPS_42G);
                default:
                    return false;
            }
        }

        // true iff the mount will perform a meridian flip when required
        // According to "A User's Guide to the Meade LXD55 and LXD75 Telescopes" Autostar supports meridian flip so
        // we assume that for any telescope that supports the GW command and is not in Alt-Az mode then
        // meridian flip on slew is supported
        private bool IsMeridianFlipOnSlewSupported() => IsGwCommandSupported() && AlignmentMode == AlignmentModes.algGermanPolar;

        private bool FirmwareIsGreaterThan(string minVersion)
        {
            var currentVersion = SharedResourcesWrapper.FirmwareVersion;
            var comparison = string.Compare(currentVersion, minVersion, StringComparison.Ordinal);
            return comparison >= 0;
        }

        /// <summary>
        /// classic LX200 needs initial set of target coordinates, if it is slewing and the target RA DE coordinates are 0 and differ from the current coordinates
        /// </summary>
        private bool IsTargetCoordinateInitRequired()
        {
            if (SharedResourcesWrapper.ProductName != TelescopeList.LX200CLASSIC)
                return false;

            if (!SharedResourcesWrapper.IsTargetCoordinateInitRequired)
                return SharedResourcesWrapper.IsTargetCoordinateInitRequired;

            if (!IsConnected)
                return true;

            if (SharedResourcesWrapper.ProductName != TelescopeList.LX200CLASSIC)
            {
                SharedResourcesWrapper.IsTargetCoordinateInitRequired = false;
                return SharedResourcesWrapper.IsTargetCoordinateInitRequired;
            }

            const double eps = 0.00001d;

            double rightTargetAscension = RightAscension;
            //target RA == 0
            if (Math.Abs(rightTargetAscension) > eps)
            {
                SharedResourcesWrapper.IsTargetCoordinateInitRequired = false;
                return SharedResourcesWrapper.IsTargetCoordinateInitRequired;
            }

            double targetDeclination = Declination;
            //target DE == 0
            if (Math.Abs(targetDeclination) > eps)
            {
                SharedResourcesWrapper.IsTargetCoordinateInitRequired = false;
                return SharedResourcesWrapper.IsTargetCoordinateInitRequired;
            }

            //target coordinates are equal current coordinates
            if ((Math.Abs(RightAscension - rightTargetAscension) <= eps) &&
                (Math.Abs(Declination - targetDeclination) <= eps))
            {
                LogMessage("IsTargetCoordinateInitRequired", "0 diff -> false");
                SharedResourcesWrapper.IsTargetCoordinateInitRequired = false;
                return SharedResourcesWrapper.IsTargetCoordinateInitRequired;
            }

            LogMessage("IsTargetCoordinateInitRequired", $"{SharedResourcesWrapper.IsTargetCoordinateInitRequired}");
            return SharedResourcesWrapper.IsTargetCoordinateInitRequired;
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
                SharedResourcesWrapper.IsTargetCoordinateInitRequired = false;
            }
            catch (Exception ex)
            {
                LogMessage("InitTargetCoordinates", "Error sync telescope position", ex.Message);
            }
        }

        public void SetLongFormat(bool setLongFormat)
        {
            if (!IsLongFormatSupported())
            {
                LogMessage("SetLongFormat", "Long coordinate format not supported for this mount");

                SharedResourcesWrapper.IsLongFormat = false;
                return;
            }

            var result = SharedResourcesWrapper.SendString("GZ");
            LogMessage("SetLongFormat", $"Get - Azimuth {result}");
            //:GZ# Get telescope azimuth
            //Returns: DDD*MM.T or DDD*MM'SS#
            //The current telescope Azimuth depending on the selected precision.

            SharedResourcesWrapper.IsLongFormat = result.Length > 6;

            if (SharedResourcesWrapper.IsLongFormat != setLongFormat)
            {
                _utilities.WaitForMilliseconds(500);
                SharedResourcesWrapper.SendBlind("U");
                //:U# Toggle between low/hi precision positions
                //Low - RA displays and messages HH:MM.T sDD*MM
                //High - Dec / Az / El displays and messages HH:MM: SS sDD*MM:SS
                //    Returns Nothing
                result = SharedResourcesWrapper.SendString("GZ");
                SharedResourcesWrapper.IsLongFormat = result.Length > 6;
                LogMessage("SetLongFormat", $"Get - Azimuth {result}");
                if (SharedResourcesWrapper.IsLongFormat == setLongFormat)
                    LogMessage("SetLongFormat", $"Long coordinate format: {setLongFormat} ");
            }
            else
            {
                LogMessage("SetLongFormat", $"Long coordinate format: {setLongFormat} ");
            }

            LogMessage("SetLongFormat", $"Long coordinate format: {setLongFormat} ");
        }

        private bool TogglePrecision()
        {
            LogMessage("TogglePrecision", "Toggling slewing precision");
            var result = SharedResourcesWrapper.SendChar("P");
            //:P# Toggles High Precsion Pointing. When High precision pointing is enabled scope will first allow the operator to center a nearby bright star before moving to the actual target.
            //Returns: <string>
            //"HIGH PRECISION" Current setting after this command.
            //"LOW PRECISION" Current setting after this command.

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

            SharedResourcesWrapper.SendBlind($"W{site}");
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
                    command = $"SM{sitename}";
                    //:SM<string>#
                    //Set site 1's name to be<string>.LX200s only accept 3 character strings. Other scopes accept up to 15 characters.
                    //    Returns:
                    //0 - Invalid
                    //1 - Valid
                    break;
                case 2:
                    command = $"SN{sitename}";
                    //:SN<string>#
                    //Set site 2's name to be<string>.LX200s only accept 3 character strings. Other scopes accept up to 15 characters.
                    //    Returns:
                    //0 - Invalid
                    //1 - Valid
                    break;
                case 3:
                    command = $"SO{sitename}";
                    //:SO<string>#
                    //Set site 3's name to be<string>.LX200s only accept 3 character strings. Other scopes accept up to 15 characters.
                    //    Returns:
                    //0 - Invalid
                    //1 - Valid
                    break;
                case 4:
                    command = $"SP{sitename}";
                    //:SP<string>#
                    //Set site 4's name to be<string>.LX200s only accept 3 character strings. Other scopes accept up to 15 characters.
                    //    Returns:
                    //0 - Invalid
                    //1 - Valid
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(site), site,
                        Resources.Telescope_GetSiteName_Site_out_of_range);
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
                    return SharedResourcesWrapper.SendString("GM");
                //:GM# Get Site 1 Name
                //Returns: <string>#
                //A '#' terminated string with the name of the requested site.
                case 2:
                    return SharedResourcesWrapper.SendString("GN");
                //:GN# Get Site 2 Name
                //Returns: <string>#
                //A '#' terminated string with the name of the requested site.
                case 3:
                    return SharedResourcesWrapper.SendString("GO");
                //:GO# Get Site 3 Name
                //Returns: <string>#
                //A '#' terminated string with the name of the requested site.
                case 4:
                    return SharedResourcesWrapper.SendString("GP");
                //:GP# Get Site 4 Name
                //Returns: <string>#
                //A '#' terminated string with the name of the requested site.
                default:
                    throw new ArgumentOutOfRangeException(nameof(site), site,
                        Resources.Telescope_GetSiteName_Site_out_of_range);
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

                //var telescopeProduceName = _sharedResourcesWrapper.SendString("GVP");
                ////:GVP# Get Telescope Product Name
                ////Returns: <string>#

                //var firmwareVersion = _sharedResourcesWrapper.SendString("GVN");
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
            CheckParked();

            LogMessage("AbortSlew", "Aborting slew");
            SharedResourcesWrapper.SendBlind("Q");
            //:Q# Halt all current slewing
            //Returns:Nothing

            SharedResourcesWrapper.MovingPrimary = false;
            SharedResourcesWrapper.MovingSecondary = false;
            SetSlewingMinEndTime();
        }

        private void CheckParked()
        {
            if (AtPark)
                throw new ParkedException("Telescope is parked");
        }

        public AlignmentModes AlignmentMode
        {
            get
            {
                LogMessage("AlignmentMode Get", "Getting alignmode");

                CheckConnected("AlignmentMode Get");

                if (IsGwCommandSupported())
                {
                    var alignmentStatus = GetScopeAlignmentStatus();
                    return alignmentStatus.AlignmentMode;
                }
                else
                {
                    const char ack = (char)6;
                    //ACK <0x06> Query of alignment mounting mode.
                    //Returns:
                    //A If scope in AltAz Mode
                    //D If scope is currently in the Downloader[Autostar II & Autostar]
                    //L If scope in Land Mode
                    //P If scope in Polar Mode
                    var alignmentString = SharedResourcesWrapper.SendChar(ack.ToString());
                    AlignmentModes alignmentMode;
                    switch (alignmentString)
                    {
                        case "A":
                            alignmentMode = AlignmentModes.algAltAz;
                            break;
                        case "P":
                            alignmentMode = AlignmentModes.algPolar;
                            break;
                        //case "G":
                            //alignmentMode = AlignmentModes.algGermanPolar;
                            //break;
                        default:
                            throw new InvalidValueException(
                                $"unknown alignment returned from telescope: {alignmentString}");
                    }

                    LogMessage("AlignmentMode Get", $"alignmode = {alignmentMode}");
                    return alignmentMode;
                }
            }
            set
            {
                CheckConnected("AlignmentMode Set");

                //todo tidy this up into a better solution that means can :GW#, :AL#, :AA#, & :AP# and checked for Autostar properly
                if (!IsGwCommandSupported())
                    throw new PropertyNotImplementedException("AlignmentMode", true);

                switch (value)
                {
                    case AlignmentModes.algAltAz:
                        SharedResourcesWrapper.SendBlind("AA");
                        //:AA# Sets telescope the AltAz alignment mode
                        //Returns: nothing
                        break;
                    case AlignmentModes.algPolar:
                    case AlignmentModes.algGermanPolar:
                        SharedResourcesWrapper.SendBlind("AP");
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

        private AlignmentStatus GetScopeAlignmentStatus()
        {
            LogMessage("GetScopeAlignmentStatus", "Started");
            var alignmentString = CommandString("GW", false);
            //:GW# Get Scope Alignment Status
            //Returns: <mount><tracking><alignment>#
            //    where:
            //mount: A - AzEl mounted, P - Equatorially mounted, G - german mounted equatorial
            //tracking: T - tracking, N - not tracking, S - sleeping
            //alignment: 0 - needs alignment, 1 - one star aligned, 2 - two star aligned, 3 - three star aligned., H - Aligned on Home, P - Scope was parked
            // https://www.cloudynights.com/topic/72166-lx-200-gps-serial-commands/

            var alignmentStatus = new AlignmentStatus();
            switch (alignmentString[0])
            {
                case 'A':
                    alignmentStatus.AlignmentMode = AlignmentModes.algAltAz;
                    break;
                case 'P':
                    alignmentStatus.AlignmentMode = AlignmentModes.algPolar;
                    break;
                case 'G':
                    alignmentStatus.AlignmentMode = AlignmentModes.algGermanPolar;
                    break;
            }

            alignmentStatus.Tracking = alignmentString[1] == 'T';
            switch (alignmentString[2])
            {
                case '0':
                    alignmentStatus.Status = Alignment.NeedsAlignment;
                    break;
                case '1':
                    alignmentStatus.Status = Alignment.OneStarAligned;
                    break;
                case '2':
                    alignmentStatus.Status = Alignment.TwoStarAligned;
                    break;
                case '3':
                    alignmentStatus.Status = Alignment.ThreeStarAligned;
                    break;
                case 'H':
                    alignmentStatus.Status = Alignment.AlignedOnHome;
                    break;
                case 'P':
                    alignmentStatus.Status = Alignment.ScopeWasParked;
                    break;
            }

            LogMessage("GetScopeAlignmentStatus", $"Result {alignmentStatus}");
            return alignmentStatus;
        }

        public double Altitude
        {
            get
            {
                CheckConnected("Altitude Get");

                if (SharedResourcesWrapper.ProductName == TelescopeList.LX200GPS)
                {
                    try
                    {
                        CheckParked();

                        //firmware bug in 44Eg, :GA# is returning the dec, not the altitude!
                        var result = SharedResourcesWrapper.SendString("GA");
                        //:GA# Get Telescope Altitude
                        //Returns: sDD* MM# or sDD*MM'SS#
                        //The current scope altitude. The returned format depending on the current precision setting.

                        var alt = _utilities.DMSToDegrees(result);
                        LogMessage("Altitude", $"{alt}");
                        return alt;
                    }
                    catch (ParkedException)
                    {
                        var parkedPosition = SharedResourcesWrapper.ParkedPosition;
                        if (parkedPosition != null)
                            return parkedPosition.Altitude;

                        throw;
                    }
                }

                var altAz = CalcAltAzFromTelescopeEqData();
                LogMessage("Altitude", $"{altAz.Altitude}");
                return altAz.Altitude;
            }
        }

        private HorizonCoordinates CalcAltAzFromTelescopeEqData()
        {
            var altitudeData = new AltitudeData
            {
                UtcDateTime = UTCDate,
                SiteLongitude = SiteLongitude,
                SiteLatitude = SiteLatitude,
                EquatorialCoordinates = GetTelescopeRaAndDec()
            };

            double hourAngle = _astroMaths.RightAscensionToHourAngle(altitudeData.UtcDateTime,
                altitudeData.SiteLongitude,
                altitudeData.EquatorialCoordinates.RightAscension);
            var altAz = _astroMaths.ConvertEqToHoz(hourAngle, altitudeData.SiteLatitude,
                altitudeData.EquatorialCoordinates);
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
                var apertureArea = _profileProperties.ApertureArea / 1000;
                LogMessage("ApertureArea Get", $"{apertureArea}");
                return apertureArea;
            }
        }

        public double ApertureDiameter
        {
            get
            {
                var apertureDiameter = _profileProperties.ApertureDiameter / 1000;
                LogMessage("ApertureDiameter Get", $"{apertureDiameter}");
                return apertureDiameter;
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

        public bool AtPark
        {
            get
            {
                var atPark = SharedResourcesWrapper.IsParked;
                LogMessage("AtPark", "Get - " + atPark);
                return atPark;
            }
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

                if (SharedResourcesWrapper.ProductName == TelescopeList.LX200GPS)
                {
                    try
                    {
                        CheckParked();

                        var result = SharedResourcesWrapper.SendString("GZ");
                        //:GZ# Get telescope azimuth
                        //Returns: DDD*MM#T or DDD*MM'SS#
                        //The current telescope Azimuth depending on the selected precision.

                        double az = _utilities.DMSToDegrees(result);

                        LogMessage("Azimuth Get", $"{az}");
                        return az;
                    }
                    catch (ParkedException)
                    {
                        var parkedPosition = SharedResourcesWrapper.ParkedPosition;
                        if (parkedPosition != null)
                            return parkedPosition.Azimuth;

                        throw;
                    }
                }

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
                case TelescopeAxes.axisSecondary: return true; //DEC or Alt
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
                var canSetTracking = IsGwCommandSupported();
                LogMessage("CanSetTracking", "Get - " + canSetTracking);
                return canSetTracking;
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
                CheckConnected("CanUnpark");

                //todo make this return false for non LX-200 GPS telescopes
                LogMessage("CanUnpark", "Get - " + true);
                return SharedResourcesWrapper.ProductName == TelescopeList.LX200GPS;
            }
        }


        public double Declination
        {
            get
            {
                CheckConnected("Declination Get");
                try
                {
                    CheckParked();

                    var result = SharedResourcesWrapper.SendString("GD");
                    //:GD# Get Telescope Declination.
                    //Returns: sDD*MM# or sDD*MM'SS#
                    //Depending upon the current precision setting for the telescope.

                    double declination = _utilities.DMSToDegrees(result);

                    LogMessage("Declination", $"Get - {result} convert to {declination} {_utilitiesExtra.DegreesToDMS(declination, ":", ":")}");
                    return declination;
                }
                catch (ParkedException)
                {
                    var parkedPosition = SharedResourcesWrapper.ParkedPosition;
                    if (parkedPosition != null)
                        return parkedPosition.Declination;

                    throw;
                }
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
            CheckConnected("DestinationSideOfPier");

            double hourAngle = _astroUtilities.ConditionHA(SiderealTime - rightAscension);

            var destinationSOP = hourAngle > 0
                ? PierSide.pierEast :
                (hourAngle < 0 ? PierSide.pierWest : SharedResourcesWrapper.SideOfPier); // avoid pierUnknown while Slewing

            LogMessage("DestinationSideOfPier",
                $"Destination SOP of RA {rightAscension.ToString(CultureInfo.InvariantCulture)} is {destinationSOP}");

            return destinationSOP;
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
                var focalLength = _profileProperties.FocalLength / 1000;
                LogMessage("FocalLength Get", $"{focalLength}");
                return focalLength;
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
                throw new InvalidValueException(propertyName, value.ToString(CultureInfo.CurrentCulture), $"{0.ToString(CultureInfo.CurrentCulture)} to {15.0417.ToString(CultureInfo.CurrentCulture)}\"/sec");
            }

            LogMessage($"{propertyName} Set", $"Setting new guiderate {value.ToString(CultureInfo.CurrentCulture)} arc seconds/second ({value.ToString(CultureInfo.CurrentCulture)} degrees/second)");
            SharedResourcesWrapper.SendBlind($"Rg{value:00.0}");
            //:RgSS.S#
            //Set guide rate to +/ -SS.S to arc seconds per second.This rate is added to or subtracted from the current tracking
            //Rates when the CCD guider or handbox guider buttons are pressed when the guide rate is selected.Rate shall not exceed
            //sidereal speed(approx 15.0417"/sec)[Autostar II only]
            //Returns: Nothing

            //info from RickB says that 15.04107 is a better value for

            _profileProperties.GuideRateArcSecondsPerSecond = value;

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
                var degreesPerSecond = ArcSecondPerSecondToDegreesPerSecond(_profileProperties.GuideRateArcSecondsPerSecond);
                LogMessage("GuideRateDeclination Get", $"{_profileProperties.GuideRateArcSecondsPerSecond} arc seconds / second = {degreesPerSecond} degrees per second");
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
                double degreesPerSecond = ArcSecondPerSecondToDegreesPerSecond(_profileProperties.GuideRateArcSecondsPerSecond);
                LogMessage("GuideRateRightAscension Get", $"{_profileProperties.GuideRateArcSecondsPerSecond} arc seconds / second = {degreesPerSecond} degrees per second");
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
                var isGuiding = SharedResourcesWrapper.IsGuiding;
                LogMessage("IsPulseGuiding Get", $"result = {isGuiding}");
                return isGuiding;
                //throw new ASCOM.PropertyNotImplementedException("IsPulseGuiding", false);
            }
        }

        public void MoveAxis(TelescopeAxes axis, double rate)
        {
            LogMessage("MoveAxis", $"Axis={axis} rate={rate}");
            CheckConnected("MoveAxis");
            CheckParked();

            var absRate = Math.Abs(rate);

            switch (absRate)
            {
                case 0:
                    //do nothing, it's ok this time as we're halting the slew.
                    break;
                case 1:
                    SharedResourcesWrapper.SendBlind("RG");
                    //:RG# Set Slew rate to Guiding Rate (slowest)
                    //Returns: Nothing
                    break;
                case 2:
                    SharedResourcesWrapper.SendBlind("RC");
                    //:RC# Set Slew rate to Centering rate (2nd slowest)
                    //Returns: Nothing
                    break;
                case 3:
                    SharedResourcesWrapper.SendBlind("RM");
                    //:RM# Set Slew rate to Find Rate (2nd Fastest)
                    //Returns: Nothing
                    break;
                case 4:
                    SharedResourcesWrapper.SendBlind("RS");
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
                            if (!SharedResourcesWrapper.IsGuiding)
                            {
                                SetSlewingMinEndTime();
                            }

                            SharedResourcesWrapper.MovingPrimary = false;
                            SharedResourcesWrapper.SendBlind("Qe");
                            //:Qe# Halt eastward Slews
                            //Returns: Nothing
                            SharedResourcesWrapper.SendBlind("Qw");
                            //:Qw# Halt westward Slews
                            //Returns: Nothing
                            break;
                        case ComparisonResult.Greater:
                            SharedResourcesWrapper.SendBlind("Me");
                            //:Me# Move Telescope East at current slew rate
                            //Returns: Nothing
                            SharedResourcesWrapper.MovingPrimary = true;
                            break;
                        case ComparisonResult.Lower:
                            SharedResourcesWrapper.SendBlind("Mw");
                            //:Mw# Move Telescope West at current slew rate
                            //Returns: Nothing
                            SharedResourcesWrapper.MovingPrimary = true;
                            break;
                    }
                    break;
                case TelescopeAxes.axisSecondary:
                    switch (rate.Compare(0))
                    {
                        case ComparisonResult.Equals:
                            if (!SharedResourcesWrapper.IsGuiding)
                            {
                                SetSlewingMinEndTime();
                            }
                            SharedResourcesWrapper.MovingSecondary = false;
                            SharedResourcesWrapper.SendBlind("Qn");
                            //:Qn# Halt northward Slews
                            //Returns: Nothing
                            SharedResourcesWrapper.SendBlind("Qs");
                            //:Qs# Halt southward Slews
                            //Returns: Nothing
                            break;
                        case ComparisonResult.Greater:
                            SharedResourcesWrapper.SendBlind("Mn");
                            //:Mn# Move Telescope North at current slew rate
                            //Returns: Nothing
                            SharedResourcesWrapper.MovingSecondary = true;
                            break;
                        case ComparisonResult.Lower:
                            SharedResourcesWrapper.SendBlind("Ms");
                            //:Ms# Move Telescope South at current slew rate
                            //Returns: Nothing
                            SharedResourcesWrapper.MovingSecondary = true;
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

            ParkedPosition parkedPosition;
            switch (_profileProperties.ParkedBehaviour)
            {
                case ParkedBehaviour.LastGoodPosition:
                    parkedPosition = new ParkedPosition
                    {
                        Altitude = Altitude,
                        Azimuth = Azimuth,
                        RightAscension = RightAscension,
                        Declination = Declination,
                        SiteLatitude = SiteLatitude,
                        SiteLongitude = SiteLongitude
                    };
                    break;
                case ParkedBehaviour.ReportCoordinates:
                    var utcDateTime = UTCDate;
                    var latitude = SiteLatitude;
                    var longitude = SiteLongitude;
                    var parkedAltAz = new HorizonCoordinates()
                    {
                        Altitude = _profileProperties.ParkedAlt,
                        Azimuth = _profileProperties.ParkedAz
                    };
                    var raDec = _astroMaths.ConvertHozToEq(utcDateTime, latitude, longitude, parkedAltAz);

                    parkedPosition = new ParkedPosition
                    {
                        Altitude = parkedAltAz.Altitude,
                        Azimuth = parkedAltAz.Azimuth,
                        RightAscension = raDec.RightAscension,
                        Declination = raDec.Declination,
                        SiteLatitude = latitude,
                        SiteLongitude = longitude
                    };
                    break;
                default:
                    parkedPosition = null;
                    break;
            }

            //Setting park to true before sending the park command as the Autostar and Audiostar stop serial communications once the park command has been issued.
            SharedResourcesWrapper.SetParked(true, parkedPosition);
            SharedResourcesWrapper.SendBlind("hP");
            //:hP# Autostar, Autostar II and LX 16" Slew to Park Position
            //Returns: Nothing
        }

        private bool _userNewerPulseGuiding = true;

        public void PulseGuide(GuideDirections direction, int duration)
        {
            LogMessage("PulseGuide", $"pulse guide direction {direction} duration {duration}");
            try
            {
                CheckConnected("PulseGuide");
                CheckParked();
                if (IsSlewingToTarget())
                    throw new InvalidOperationException("Unable to PulseGuide whilst slewing to target.");

                SharedResourcesWrapper.IsGuiding = true;
                try
                {
                    if (SharedResourcesWrapper.MovingPrimary &&
                        (direction == GuideDirections.guideEast || direction == GuideDirections.guideWest))
                        throw new InvalidOperationException("Unable to PulseGuide while moving same axis.");

                    if (SharedResourcesWrapper.MovingSecondary &&
                        (direction == GuideDirections.guideNorth || direction == GuideDirections.guideSouth))
                        throw new InvalidOperationException("Unable to PulseGuide while moving same axis.");

                    var coordinatesBeforeMove = GetTelescopeRaAndDec();

                    if (_userNewerPulseGuiding)
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
                        SharedResourcesWrapper.SendBlind($"Mg{d}{duration:0000}");
                        //:MgnDDDD#
                        //:MgsDDDD#
                        //:MgeDDDD#
                        //:MgwDDDD#
                        //Guide telescope in the commanded direction(nsew) for the number of milliseconds indicated by the unsigned number
                        //passed in the command.These commands support serial port driven guiding.
                        //Returns - Nothing
                        //LX200   - Not Supported
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
                    SharedResourcesWrapper.IsGuiding = false;
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
        public double HmToHours(string hm)
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
                try
                {
                    CheckParked();

                    var result = SharedResourcesWrapper.SendString("GR");
                    //:GR# Get Telescope RA
                    //Returns: HH:MM.T# or HH:MM:SS#
                    //Depending which precision is set for the telescope

                    double rightAscension = HmToHours(result);

                    LogMessage("RightAscension", $"Get - {result} convert to {rightAscension} {_utilitiesExtra.HoursToHMS(rightAscension)}");
                    return rightAscension;
                }
                catch (ParkedException)
                {
                    var parkedPosition = SharedResourcesWrapper.ParkedPosition;
                    if (parkedPosition != null)
                        return parkedPosition.RightAscension;

                    throw;
                }

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
                if (!IsMeridianFlipOnSlewSupported())
                {
                    LogMessage("SideOfPier Get", "Not implemented");
                    throw new PropertyNotImplementedException("SideOfPier", false);
                }

                PierSide pierSide;
                if (Slewing)
                {
                    // because we update SideOfPier after initiating the slew command we return unknown while still slewing
                    pierSide = PierSide.pierUnknown;
                }
                else
                {
                    pierSide = SharedResourcesWrapper.SideOfPier;
                }

                LogMessage("SideOfPier", "Get - " + pierSide);
                return pierSide;
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
                CheckConnected("SiderealTime Get");

                // Now using NOVAS 3.1
                double siderealTime = 0.0;

                var jd = _utilities.DateUTCToJulian(_clock.UtcNow);
                var siderealTimeResult = _novas.SiderealTime(jd, 0, _novas.DeltaT(jd),
                    GstType.GreenwichApparentSiderealTime,
                    Method.EquinoxBased,
                    Accuracy.Reduced, ref siderealTime);

                if (siderealTimeResult != 0)
                {
                    throw new InvalidOperationException($"NOVAS 3.1 SiderealTime returned: {siderealTimeResult} in SiderealTime");
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
                CheckConnected("SiteElevation Get");

                LogMessage("SiteElevation",  $"Get {_profileProperties.SiteElevation}");
                return _profileProperties.SiteElevation;
            }
            // ReSharper disable once ValueParameterNotUsed
            set
            {
                CheckConnected("SiteElevation Set");

                LogMessage("SiteElevation", $"Set: {value}");
                if (Math.Abs(value - _profileProperties.SiteElevation) < 0.1)
                {
                    LogMessage("SiteElevation", "Set: no change detected");
                    return;
                }

                LogMessage("SiteElevation", $"Set: {value} was {_profileProperties.SiteElevation}");
                _profileProperties.SiteElevation = value;
                UpdateSiteElevation();
            }
        }

        public double SiteLatitude
        {
            get
            {
                CheckConnected("SiteLatitude Get");
                try
                {
                    CheckParked();

                    var latitude = SharedResourcesWrapper.SendString("Gt");
                    //:Gt# Get Current Site Latitude
                    //Returns: sDD* MM#
                    //The latitude of the current site. Positive inplies North latitude.

                    if (latitude != null)
                    {
                        var siteLatitude = _utilities.DMSToDegrees(latitude);
                        LogMessage("SiteLatitude Get", $"{_utilitiesExtra.DegreesToDMS(siteLatitude)}");
                        return siteLatitude;
                    }

                    throw new InvalidOperationException("unable to get site latitude from telescope.");
                }
                catch (ParkedException) when (_profileProperties.ParkedBehaviour != ParkedBehaviour.NoCoordinates && SharedResourcesWrapper.ParkedPosition is var parkedPosition)
                {
                    return parkedPosition.SiteLatitude;
                }
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
                var commandString = $"St{sign}{d:00}*{m:00}";

                var result = SharedResourcesWrapper.SendChar(commandString);
                //:StsDD*MM#
                //Sets the current site latitude to sDD* MM#
                //Returns:
                //0 - Invalid
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
                try
                {
                    CheckParked();

                    var longitude = SharedResourcesWrapper.SendString("Gg");
                    //:Gg# Get Current Site Longitude
                    //Returns: sDDD*MM#
                    //The current site Longitude. East Longitudes are expressed as negative
                    double siteLongitude = -_utilities.DMSToDegrees(longitude);

                    if (siteLongitude < -180)
                        siteLongitude += 360;

                    LogMessage("SiteLongitude Get", $"{_utilitiesExtra.DegreesToDMS(siteLongitude)}");

                    return siteLongitude;
                }
                catch (ParkedException) when (_profileProperties.ParkedBehaviour != ParkedBehaviour.NoCoordinates && SharedResourcesWrapper.ParkedPosition is var parkedPosition)
                {
                    return parkedPosition.SiteLongitude;
                }
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

                var commandstring = $"Sg{d:000}*{m:00}";

                var result = SharedResourcesWrapper.SendChar(commandstring);
                //:SgDDD*MM#
                //Set current site's longitude to DDD*MM an ASCII position string
                //Returns:
                //0 - Invalid
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
                LogMessage("SlewSettleTime Get", $"{SharedResourcesWrapper.SlewSettleTime} Seconds");
                return SharedResourcesWrapper.SlewSettleTime;
            }
            set
            {
                CheckConnected("SlewSettleTime Set");
                LogMessage("SlewSettleTime Set", $"Setting from {SharedResourcesWrapper.SlewSettleTime} to {value}");
                SharedResourcesWrapper.SlewSettleTime = value;
            }
        }

        public void SlewToAltAz(double azimuth, double altitude)
        {
            LogMessage("SlewToAltAz", $"Az=~{azimuth} Alt={altitude}");
            CheckConnected("SlewToAltAz");
            CheckParked();

            SlewToAltAzAsync(azimuth, altitude);

            while (Slewing) //wait for slew to complete
            {
                _utilities.WaitForMilliseconds(200); //be responsive to AbortSlew();
            }
        }

        public void SlewToAltAzAsync(double azimuth, double altitude)
        {
            CheckConnected("SlewToAltAzAsync");
            CheckParked();

            if (altitude > 90)
                throw new InvalidValueException("Altitude cannot be greater than 90.");

            if (altitude < 0)
                throw new InvalidValueException("Altitide cannot be less than 0.");

            if (azimuth >= 360)
                throw new InvalidValueException("Azimuth cannot be 360 or higher.");

            if (azimuth < 0)
                throw new InvalidValueException("Azimuth cannot be less than 0.");

            LogMessage("SlewToAltAzAsync", $"Az={azimuth} Alt={altitude}");

            HorizonCoordinates altAz = new HorizonCoordinates { Azimuth = azimuth, Altitude = altitude };

            var utcDateTime = UTCDate;
            var latitude = SiteLatitude;
            var longitude = SiteLongitude;

            var raDec = _astroMaths.ConvertHozToEq(utcDateTime, latitude, longitude, altAz);

            TargetRightAscension = raDec.RightAscension;
            TargetDeclination = raDec.Declination;

            DoSlewAsync(true);
        }

        private void DoSlewAsync(bool polar)
        {
            CheckConnected("DoSlewAsync");
            CheckParked();
            if (Slewing)
            {
                LogMessage("DoSlewAsync", "Cannot start a slew whilst slew is in progress.");
                throw new ASCOM.InvalidOperationException("Cannot start a slew whilst slew is in progress.");
            }

            switch (polar)
            {
                case true:
                    var response = SharedResourcesWrapper.SendChar("MS");
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

                            if (IsMeridianFlipOnSlewSupported())
                            {
                                // Update side of pier to destination side of pier
                                // Assumption: Mount will do meridian flip if required
                                SharedResourcesWrapper.SideOfPier =
                                    DestinationSideOfPier(TargetRightAscension, TargetDeclination);
                            }

                            SetSlewingMinEndTime();
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
                    var maResponse = SharedResourcesWrapper.SendChar("MA");
                    //:MA# Autostar, LX 16", Autostar II - Slew to target Alt and Az
                    //Returns:
                    //0 - No fault
                    //1 - Fault
                    //LX200 - Not supported

                    if (maResponse == "1")
                    {
                        throw new InvalidOperationException("fault");
                    }

                    SetSlewingMinEndTime();
                    break;
            }
        }

        public void SlewToCoordinates(double rightAscension, double declination)
        {
            LogMessage("SlewToCoordinates", $"Ra={rightAscension}, Dec={declination}");
            CheckConnected("SlewToCoordinates");
            CheckParked();

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
            CheckParked();

            TargetRightAscension = rightAscension;
            TargetDeclination = declination;
            DoSlewAsync(true);

            LogMessage("SlewToCoordinatesAsync", $"Completed Ra={rightAscension}, Dec={declination}");
        }

        public void SlewToTarget()
        {
            LogMessage("SlewToTarget", "Executing");
            CheckConnected("SlewToTarget");
            CheckParked();
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
            CheckParked();

            if (TargetDeclination.Equals(InvalidParameter) || TargetRightAscension.Equals(InvalidParameter))
                throw new InvalidOperationException("No target selected to slew to.");

            DoSlewAsync(true);
        }

        private bool MovingAxis()
        {
            if (SharedResourcesWrapper.IsGuiding)
            {
                LogMessage("MovingAxis", $"Result = false (guiding is true)");
                return false;
            }

            var movingAxis = SharedResourcesWrapper.MovingPrimary || SharedResourcesWrapper.MovingSecondary;

            LogMessage("MovingAxis", $"Result = {movingAxis} Primary={SharedResourcesWrapper.MovingPrimary} Secondary={SharedResourcesWrapper.MovingSecondary}");
            return movingAxis;
        }

        public bool Slewing
        {
            get
            {
                var isSlewing = GetSlewing();

                if (isSlewing)
                    SetSlewingMinEndTime();
                else if (_clock.UtcNow < SharedResourcesWrapper.EarliestNonSlewingTime)
                    isSlewing = true;

                LogMessage("Slewing", $"Result = {isSlewing}");
                return isSlewing;
            }
        }

        private void SetSlewingMinEndTime()
        {
            SharedResourcesWrapper.EarliestNonSlewingTime = _clock.UtcNow + GetTotalSlewingSettleTime();
        }

        private TimeSpan GetTotalSlewingSettleTime()
        {
            return TimeSpan.FromSeconds( SlewSettleTime + _profileProperties.SettleTime );
        }

        private bool GetSlewing()
        {
            var result = false;
            try
            {
                if (Connected)
                    result = MovingAxis() || IsSlewingToTarget();
            }
            finally
            {
                LogMessage("GetSlewing", $"Result = {result}");
            }

            return result;
        }

        private bool IsSlewingToTarget()
        {
            CheckConnected("IsSlewingToTarget");

            if (SharedResourcesWrapper.IsGuiding)
                return false;

            string result;
            try
            {
                result = SharedResourcesWrapper.SendString("D");
            }
            catch (TimeoutException)
            {
                result = string.Empty;
            }
            //:D# Requests a string of bars indicating the distance to the current target location.
            //Returns:
            //LX200's - a string of bar characters indicating the distance.
            //Autostars and Autostar II - a string containing one bar until a slew is complete, then a null string is returned.

            bool isSlewing = false;
            try
            {
                if (string.IsNullOrEmpty(result))
                {
                    // ReSharper disable once RedundantAssignment
                    isSlewing = false;
                    return isSlewing;
                }

                if (result.Contains("|"))
                {
                    isSlewing = true;
                    return isSlewing;
                }

                if (result.Contains("\u007f"))
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
            }
            finally
            {
                LogMessage("IsSlewingToTarget", $"IsSlewing = {isSlewing} : result = {result ?? "<null>"}");
            }

            return isSlewing;
        }

        public void SyncToAltAz(double azimuth, double altitude)
        {
            LogMessage("SyncToAltAz", "Not implemented");
            throw new MethodNotImplementedException("SyncToAltAz");
        }

        public void SyncToCoordinates(double rightAscension, double declination)
        {
            LogMessage("SyncToCoordinates", $"RA={rightAscension} Dec={declination}");
            LogMessage("SyncToCoordinates",
                $"RA={_utilitiesExtra.HoursToHMS(rightAscension)} Dec={_utilitiesExtra.HoursToHMS(declination)}");
            CheckConnected("SyncToCoordinates");
            CheckParked();

            TargetRightAscension = rightAscension;
            TargetDeclination = declination;

            SyncToTarget();
        }

        public void SyncToTarget()
        {
            LogMessage("SyncToTarget", "Executing");
            CheckConnected("SyncToTarget");
            CheckParked();

            var result = SharedResourcesWrapper.SendString("CM");
            //:CM# Synchronizes the telescope's position with the currently selected database object's coordinates.
            //Returns:
            //LX200's - a "#" terminated string with the name of the object that was synced.
            //    Autostars & Autostar II - A static string: " M31 EX GAL MAG 3.5 SZ178.0'#"

            if (string.IsNullOrWhiteSpace(result))
                throw new InvalidOperationException("Unable to perform sync");

            // At least the classic LX200 low precision might not slew to the exact target position
            // This Requires to retrieve the aimed target ra de from the telescope
            double targetRA = SharedResourcesWrapper.TargetRightAscension ?? InvalidParameter;
            double ra = RightAscension;
            if (Math.Abs(targetRA - InvalidParameter) > 0.1 &&
                _utilities.HoursToHMS(ra, ":", ":", ":", _digitsRa) != _utilities.HoursToHMS(targetRA, ":", ":", ":", _digitsRa))
            {
                LogMessage("SyncToTarget", $"differ RA real {ra} targeted {targetRA}");
                SharedResourcesWrapper.TargetRightAscension = ra;
            }
            double targetDEC = SharedResourcesWrapper.TargetDeclination ?? InvalidParameter;
            double de = Declination;
            if (Math.Abs(targetDEC - InvalidParameter) > 0.1 &&
                _utilities.DegreesToDMS(de, "*", ":", ":", _digitsDe) != _utilities.DegreesToDMS(targetDEC, "*", ":", ":", _digitsDe))
            {
                LogMessage("SyncToTarget", $"differ DE real {de} targeted {targetDEC}");
                SharedResourcesWrapper.TargetDeclination = de;
            }
        }

        public double TargetDeclination
        {
            get
            {
                var targetDeclination = SharedResourcesWrapper.TargetDeclination ?? InvalidParameter;
                if (targetDeclination.Equals(InvalidParameter))
                    throw new InvalidOperationException("Target not set");

                //var result = SerialPort.CommandTerminated(":Gd#", "#");
                ////:Gd# Get Currently Selected Object/Target Declination
                ////Returns: sDD* MM# or sDD*MM'SS#
                ////Depending upon the current precision setting for the telescope.

                //double targetDec = DmsToDouble(result);

                //return targetDec;
                LogMessage("TargetDeclination Get", $"{targetDeclination}");
                return targetDeclination;
            }
            set
            {
                LogMessage("TargetDeclination Set", $"{value}");

                CheckConnected("TargetDeclination Set");
                CheckParked();

                if (value > 90)
                    throw new InvalidValueException("Declination cannot be greater than 90.");

                if (value < -90)
                    throw new InvalidValueException("Declination cannot be less than -90.");

                var dms = SharedResourcesWrapper.IsLongFormat ?
                    _utilities.DegreesToDMS(value, "*", ":", ":", _digitsDe) :
                    _utilities.DegreesToDM(value, "*", "", _digitsDe);

                var s = value < 0 ? string.Empty : "+";

                var command = $"Sd{s}{dms}";

                LogMessage("TargetDeclination Set", $"{command}");
                var result = SharedResourcesWrapper.SendChar(command);
                //:SdsDD*MM#
                //Set target object declination to sDD*MM or sDD*MM:SS depending on the current precision setting
                //Returns:
                //1 - Dec Accepted
                //0 - Dec invalid

                if (result == "0")
                {
                    throw new InvalidOperationException("Target declination invalid");
                }

                SharedResourcesWrapper.TargetDeclination = _utilities.DMSToDegrees(dms);
            }
        }

        public double TargetRightAscension
        {
            get
            {
                var targetRightAscension = SharedResourcesWrapper.TargetRightAscension ?? InvalidParameter;
                if (targetRightAscension.Equals(InvalidParameter))
                    throw new InvalidOperationException("Target not set");

                //var result = SerialPort.CommandTerminated(":Gr#", "#");
                ////:Gr# Get current/target object RA
                ////Returns: HH: MM.T# or HH:MM:SS
                ////Depending upon which precision is set for the telescope

                //double targetRa = HmsToDouble(result);
                //return targetRa;

                LogMessage("TargetRightAscension Get", $"{targetRightAscension}");
                return targetRightAscension;
            }
            set
            {
                LogMessage("TargetRightAscension Set", $"{value}");
                CheckConnected("TargetRightAscension Set");
                CheckParked();

                if (value < 0)
                    throw new InvalidValueException("Right ascension value cannot be below 0");

                if (value >= 24)
                    throw new InvalidValueException("Right ascension value cannot be greater than 23:59:59");

                var hms = SharedResourcesWrapper.IsLongFormat ?
                    _utilities.HoursToHMS(value, ":", ":", ":", _digitsRa) :
                    _utilities.HoursToHM(value, ":", "", _digitsRa).Replace(',','.');

                var command = $"Sr{hms}";
                LogMessage("TargetRightAscension Set", $"{command}");
                var response = SharedResourcesWrapper.SendChar(command);
                //:SrHH:MM.T#
                //:SrHH:MM:SS#
                //Set target object RA to HH:MM.T or HH: MM: SS depending on the current precision setting.
                //    Returns:
                //0 - Invalid
                //1 - Valid

                if (response == "0")
                    throw new InvalidOperationException("Failed to set TargetRightAscension.");

                SharedResourcesWrapper.TargetRightAscension = _utilities.HMSToHours(hms);
            }
        }

        public bool Tracking
        {
            get
            {
                LogMessage("Tracking", "Get");
                bool isTracking = true;
                if (IsGwCommandSupported())
                {
                    var alignmentStatus = GetScopeAlignmentStatus();
                    isTracking = alignmentStatus.Tracking;
                }

                LogMessage("Tracking", $"Get = {isTracking}");
                return isTracking;
            }
            set
            {
                if (!CanSetTracking)
                {
                    throw new ASCOM.NotImplementedException("Tracking Set");
                }

                LogMessage("Tracking Set", $"{value}");
                SharedResourcesWrapper.SendBlind(value ? "AP" : "AL"); //todo need to route this to the real commands.
            }
        }

        public DriveRates TrackingRate
        {
            get
            {
                var rate = CommandString("GT", false);
                //:GT# Get tracking rate
                //Returns: TT.T#
                //Current Track Frequency expressed in hertz assuming a synchonous motor design where a 60.0 Hz motor clock
                //    would produce 1 revolution of the telescope in 24 hours.

                rate = rate.Replace("+",  string.Empty);

                var rateDouble = double.Parse(rate);

                DriveRates result;

                if (rateDouble.Equals(60.1))
                    result = DriveRates.driveSidereal;
                else if (rateDouble.Equals(60.0))
                    result = DriveRates.driveSolar;
                else if (rateDouble.Between(57.3, 58.9))
                    result = DriveRates.driveLunar;
                else
                    //If this is ever returned it is representing a fail condition.
                    //result = DriveRates.driveKing;
                    throw new ASCOM.InvalidValueException($"{rate} is not a supported tracking rate for meade mounts");

                LogMessage("TrackingRate Get", $"{rate} {result}");

                return result;
            }
            set
            {
                LogMessage("TrackingRate Set", $"{value}");
                CheckConnected("TrackingRate Set");
                CheckParked();

                if (SharedResourcesWrapper.ProductName == TelescopeList.LX200CLASSIC)
                {
                    throw new ASCOM.NotImplementedException("TrackingRate Set");
                }

                switch (value)
                {
                    case DriveRates.driveSidereal:
                        SharedResourcesWrapper.SendBlind("TQ");
                        //:TQ# Selects sidereal tracking rate
                        //Returns: Nothing
                        break;
                    case DriveRates.driveLunar:
                        SharedResourcesWrapper.SendBlind("TL");
                        //:TL# Set Lunar Tracking Rage
                        //Returns: Nothing
                        break;
                    case DriveRates.driveSolar:
                        SharedResourcesWrapper.SendBlind("TS");
                    //    //:TS# Select Solar tracking rate. [LS Only]
                    //    //Returns: Nothing
                        break;
                    //case DriveRates.driveKing:
                    //    SharedResourcesWrapper.SendBlind("TM");
                    //    //:TM# Select custom tracking rate [ no-op in Autostar II]
                    //    //Returns: Nothing
                    //    break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
            }
        }

        public ITrackingRates TrackingRates
        {
            get
            {
                ITrackingRates trackingRates = new TrackingRates(SharedResourcesWrapper.ProductName != TelescopeList.LX200CLASSIC);
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
            string utcOffSet = SharedResourcesWrapper.SendString("GG");
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
                try
                {
                    CheckParked();

                    var telescopeDateDetails = new TelescopeDateDetails
                    {
                        TelescopeDate = SharedResourcesWrapper.SendString("GC"),
                        //:GC# Get current date.
                        //Returns: MM/DD/YY#
                        //The current local calendar date for the telescope.
                        TelescopeTime = SharedResourcesWrapper.SendString("GL"),
                        //:GL# Get Local Time in 24 hour format
                        //Returns: HH:MM:SS#
                        //The Local Time in 24 - hour Format
                        UtcCorrection = GetUtcCorrection()
                    };

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
                catch (ParkedException)
                {
                    if (_profileProperties.ParkedBehaviour == ParkedBehaviour.NoCoordinates)
                        throw;

                    return _clock.UtcNow;
                }
            }
            set
            {
                LogMessage("UTCDate", "Set - " + value.ToString("MM/dd/yy HH:mm:ss"));

                CheckConnected("UTCDate Set");

                var utcCorrection = GetUtcCorrection();
                var localDateTime = value - utcCorrection;

                string localStingCommand = $"SL{localDateTime:HH:mm:ss}";
                var timeResult = SharedResourcesWrapper.SendChar(localStingCommand);
                //:SLHH:MM:SS#
                //Set the local Time
                //Returns:
                //0 - Invalid
                //1 - Valid
                if (timeResult != "1")
                {
                    throw new InvalidOperationException("Failed to set local time");
                }

                string localDateCommand = $"SC{localDateTime:MM/dd/yy}";
                var dateResult = SharedResourcesWrapper.SendChar(localDateCommand);
                //:SCMM/DD/YY#
                //Change Handbox Date to MM/DD/YY
                //Returns: <D><string>
                //D = '0' if the date is invalid. The string is the null string.
                //D = '1' for valid dates and the string is "Updating Planetary Data#                       #"
                //Note: For Autostar II this is the UTC data!
                if (dateResult != "1")
                {
                    throw new InvalidOperationException("Failed to set local date");
                }

                //throwing away these two strings which represent
                SharedResourcesWrapper.ReadTerminated(); //Updating Planetary Data#
                SharedResourcesWrapper.ReadTerminated(); //                       #
            }
        }

        public void Unpark()
        {
            LogMessage("Unpark", "Unparking telescope");
            CheckConnected("Unpark");

            if (SharedResourcesWrapper.ProductName != TelescopeList.LX200GPS)
                throw new InvalidOperationException("Unable to unpark this telescope type");

            if (!AtPark)
                return;

            SharedResourcesWrapper.SendChar("I");
            //:I# LX200 GPS Only - Causes the telescope to cease current operations and restart at its power on initialization.
            //Returns: X once the handset restart has completed

            BypassHandboxEntryForAutostarII();

            SharedResourcesWrapper.SetParked(false, null);

            // reset side of pier
            SideOfPier = PierSide.pierUnknown;
        }

        private bool BypassHandboxEntryForAutostarII()
        {
            var utcCorrection = GetUtcCorrection();
            var localDateTime = _clock.UtcNow - utcCorrection;

            //localDateTime: HH: mm: ss
            var result = SharedResourcesWrapper.SendChar($"hI{localDateTime:yyMMddHHmmss}");
            //:hIYYMMDDHHMMSS#
            //Bypass handbox entry of daylight savings, date and time.Use the values supplied in this command.This feature is
            //intended to allow use of the Autostar II from permanent installations where GPS reception is not possible, such as within
            //metal domes. This command must be issued while the telescope is waiting at the initial daylight savings prompt.
            //Returns: 1 - if command was accepted.
            return result == "1";
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
            var changed = false;

            var profileProperties = SharedResourcesWrapper.ReadProfile();


            if (Math.Abs(profileProperties.GuideRateArcSecondsPerSecond - _profileProperties.GuideRateArcSecondsPerSecond) > 0.0000001)
            {
                changed = true;
                profileProperties.GuideRateArcSecondsPerSecond = _profileProperties.GuideRateArcSecondsPerSecond;
            }

            if (changed)
                SharedResourcesWrapper.WriteProfile(profileProperties);
        }
        #endregion
    }
}
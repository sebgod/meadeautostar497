using System;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using ASCOM.Utilities.Interfaces;

namespace ASCOM.MeadeAutostar497.Controller
{
    //todo stop this being a singleton, and instead use a server to make only a single instance.
    public sealed class TelescopeController : ITelescopeController
    {
        private const double INVALID_PARAMETER = -1000;

        private static readonly Lazy<TelescopeController> Lazy = new Lazy<TelescopeController>();

        public static TelescopeController Instance => Lazy.Value;

        //todo remove this as it can cause problems in production
        private ISerialProcessor _serialPort;
        public ISerialProcessor SerialPort
        {
            get => _serialPort ?? (_serialPort = new SerialProcessor());
            set
            {
                if (_serialPort == value)
                    return;

                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                        throw new InvalidOperationException("Please disconnect before changing the serial engine.");
                }

                _serialPort = value;
            }
        }

        private IUtil _util;
        public IUtil Util
        {
            get => _util ?? (_util = new Util());
            set
            {
                if (Equals(_util, value))
                    return;

                _util = value;
            }
        }

        private string _port = "COM1";
        public string Port
        {
            get => _port;
            set
            {
                if (_port == value) return;

                if (Connected)
                    throw new InvalidOperationException("Please disconnect from the scope before changing port.");

                if (!ValidPort(value))
                    throw new InvalidOperationException($"Unable to select port {value} as it does not exist.");

                _port = value;
            }
        }

        private bool ValidPort(string value)
        {
            return SerialPort.GetPortNames().Contains(value);
        }

        public bool Connected
        {
            get => SerialPort.IsOpen;
            set
            {
                if (value == Connected)
                    return;

                if (value)
                {
                    //Connecting
                    try
                    {
                        AtPark = false;
                        SerialPort.DtrEnable = false;
                        SerialPort.RtsEnable = false;
                        SerialPort.BaudRate = 9600;
                        SerialPort.DataBits = 8;
                        SerialPort.StopBits = StopBits.One;
                        SerialPort.Parity = Parity.None;
                        SerialPort.PortName = Port;
                        SerialPort.Open();

                        TestConnectionActive();
                    }
                    catch (Exception)
                    {
                        if (SerialPort.IsOpen)
                            SerialPort.Close();
                        throw;
                    }
                }
                else
                {
                    //Disconnecting
                    SerialPort.Close();
                    AtPark = false;
                }
            }
        }

        private void TestConnectionActive()
        {
            var firmwareVersionNumber = SerialPort.CommandTerminated(":GVN#", "#");
            if (string.IsNullOrEmpty(firmwareVersionNumber))
            {
                throw new InvalidOperationException("Failed to communicate with telescope."); 
            }
        }

        public bool Slewing
        {
            get
            {
                if (!Connected) return false;


                if (movingAxis())
                    return true;

                var result = SerialPort.CommandTerminated(":D#", "#");
                return result != string.Empty;
            }
        }

        public DateTime utcDate
        {
            get
            {
                string telescopeDate = SerialPort.CommandTerminated(":GC#", "#");
                string telescopeTime = SerialPort.CommandTerminated(":GL#", "#");

                int month = telescopeDate.Substring(0, 2).ToInteger();
                int day = telescopeDate.Substring(3, 2).ToInteger();
                int year = telescopeDate.Substring(6, 2).ToInteger();

                if (year < 2000) //todo fix this hack that will create a Y2K100 bug
                {
                    year = year + 2000;
                }

                int hour = telescopeTime.Substring(0, 2).ToInteger();
                int minute = telescopeTime.Substring(3, 2).ToInteger();
                int second = telescopeTime.Substring(6, 2).ToInteger();

                var utcCorrection = GetUtcCorrection();

                //Todo is this telescope local time, or real utc?
                var newDate = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc) + utcCorrection;

                return newDate;
            }
            set
            {
                var utcCorrection = GetUtcCorrection();
                var localDateTime = value - utcCorrection;

                //Todo is this telescope local time, or real utc?
                var timeResult = SerialPort.CommandChar($":SL{localDateTime:HH:mm:ss}#");
                if (timeResult != '1')
                {
                    throw new InvalidOperationException("Failed to set local time");
                }

                SerialPort.Lock();
                try
                {
                    var dateResult = SerialPort.CommandChar($":SC{localDateTime:MM/dd/yy}#");
                    if (dateResult != '1')
                    {
                        throw new InvalidOperationException("Failed to set local date");
                    }

                    //throwing away these two strings which represent 
                    SerialPort.ReadTerminated("#"); //Updating Planetary Data#
                    SerialPort.ReadTerminated("#"); //                       #
                }
                finally
                {
                    SerialPort.Unlock();
                }
            }

        }

        private TimeSpan GetUtcCorrection()
        {
            string utcOffSet = SerialPort.CommandTerminated(":GG#", "#");
            double utcOffsetHours = double.Parse(utcOffSet);
            TimeSpan utcCorrection = TimeSpan.FromHours(utcOffsetHours);
            return utcCorrection;
        }

        public double SiteLatitude
        {
            get
            {
                var latitude = SerialPort.CommandTerminated( ":Gt#", "#");

                return DmsToDouble(latitude);
            }
            set
            {
                if (value > 90)
                    throw new  InvalidValueException("Latitude cannot be greater than 90 degrees.");

                if (value < -90)
                    throw new InvalidValueException("Latitude cannot be less than -90 degrees.");

                int d =  Convert.ToInt32(Math.Floor(value));
                int m = Convert.ToInt32(60 * (value - d));

                var result = SerialPort.CommandChar($":Sts{d:00}*{m:00}#");
                if (result != '1')
                    throw new InvalidOperationException("Failed to set site latitude.");
            }
        }

        private double DmsToDouble(string dms)
        {
            if (IsNumeric(dms[0]))
            {
                double l = int.Parse(dms.Substring(0, 3));
                l = l + double.Parse(dms.Substring(4, 2)) / 60;
                if (dms.Length == 9)
                    l = l + double.Parse(dms.Substring(7, 2)) / 60 / 60;

                return l;
            }

            double lat = int.Parse(dms.Substring(1, 2));
            lat = lat + double.Parse(dms.Substring(4, 2)) / 60;
            if (dms.Length == 9)
                lat = lat + double.Parse(dms.Substring(7, 2)) / 60 / 60;

            if (dms[0] == '-')
                lat = -lat;

            return lat;
        }

        private bool IsNumeric(char c)
        {
            char[] nums = new[] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};
            return nums.Contains(c);
        }

        public double SiteLongitude
        {
            get
            {
                var longitude = SerialPort.CommandTerminated(":Gg#", "#");
                double l = DmsToDouble(longitude);

                if (l > 180)
                    l = l - 360;

                return l;
            }
            set
            {
                if (value > 180)
                    throw new InvalidValueException("Longitude cannot be greater than 180 degrees.");

                if (value < -180)
                    throw new InvalidValueException("Longitude cannot be lower than -180 degrees.");

                int d = Convert.ToInt32(Math.Floor(value));
                int m = Convert.ToInt32(60 * (value - d));

                var result = SerialPort.CommandChar($":Sg{d:000}*{m:00}#");
                if (result != '1')
                    throw new InvalidOperationException("Failed to set site longitude.");
            }

        }

        public AlignmentModes AlignmentMode
        {
            get
            {
                const char ack = (char)6;
                //var alignmentString = SerialPort.CommandTerminated(":GW#", "#");
                var alignmentString = SerialPort.CommandChar(ack.ToString());
                //:GW# Get Scope Alignment Status
                //Returns: <mount><tracking><alignment>#
                //    where:
                //mount: A - AzEl mounted, P - Equatorially mounted, G - german mounted equatorial
                //tracking: T - tracking, N - not tracking
                //alignment: 0 - needs alignment, 1 - one star aligned, 2 - two star aligned, 3 - three star aligned.

                switch (alignmentString)
                {
                    case 'A': return AlignmentModes.algAltAz;
                    case 'P': return AlignmentModes.algPolar;
                    case 'G': return AlignmentModes.algGermanPolar;
                    default:
                        throw new InvalidValueException($"unknown alignment returned from telescope: {alignmentString}");
                }
            }
            set
            {
                switch (value)
                {
                    case AlignmentModes.algAltAz:
                        SerialPort.Command(":AA#");
                        //:AA# Sets telescope the AltAz alignment mode
                        //Returns: nothing
                        break;
                    case AlignmentModes.algPolar:
                    case AlignmentModes.algGermanPolar:
                        SerialPort.Command(":AP#");
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

        public bool AtPark { get; private set; }

        public double Azimuth
        {
            get
            {
                var result = SerialPort.CommandTerminated(":GZ#", "#");
                //:GZ# Get telescope azimuth
                //Returns: DDD*MM#T or DDD*MM’SS#
                //The current telescope Azimuth depending on the selected precision.

                double az = DmsToDouble(result);
                
                return az;
            }
        }

        public double RightAscension {
            get
            {
                var result = SerialPort.CommandTerminated(":GR#", "#");
                //:GR# Get Telescope RA
                //Returns: HH: MM.T# or HH:MM:SS#
                //Depending which precision is set for the telescope

                double ra = HmsToDouble(result);

                return ra;
            }
        }

        private double HmsToDouble(string hms)
        {
            return Util.HMSToHours(hms);
        }

        public double Declination
        {
            get
            {
                var result = SerialPort.CommandTerminated(":GD#", "#");
                //:GD# Get Telescope Declination.
                //Returns: sDD* MM# or sDD*MM’SS#
                //Depending upon the current precision setting for the telescope.

                double az = DmsToDouble(result);

                return az;
            }
        }

        private double _targetRightAscension = INVALID_PARAMETER;
        public double TargetRightAscension {
            get
            {
                if (_targetRightAscension == INVALID_PARAMETER)
                    throw new ASCOM.InvalidOperationException("Target not set");

                var result = SerialPort.CommandTerminated(":Gr#", "#");
                //:Gr# Get current/target object RA
                //Returns: HH: MM.T# or HH:MM:SS
                //Depending upon which precision is set for the telescope

                double targetRa = HmsToDouble(result);
                return targetRa;
            }
            set
            {
                if (value < 0)
                    throw new InvalidValueException("Right ascension value cannot be below 0");

                if (value >= 24)
                    throw new InvalidValueException("Right ascension value cannot be greater than 23:59:59");


                //todo implement the low precision version

                var hms = Util.HoursToHMS(value, ":", ":", ":", 2);
                var response = SerialPort.CommandChar($":Sr{hms}#");
                //:SrHH:MM.T#
                //:SrHH:MM:SS#
                //Set target object RA to HH:MM.T or HH: MM: SS depending on the current precision setting.
                //    Returns:
                //0 – Invalid
                //1 - Valid

                if (response == '0')
                    throw new InvalidOperationException("Failed to set TargetRightAscension.");

                _targetRightAscension = value;
            }
        }

        private double _targetDeclination = INVALID_PARAMETER;
        public double TargetDeclination {
            get
            {
                if (_targetDeclination == INVALID_PARAMETER)
                    throw new ASCOM.InvalidOperationException("Target not set");

                var result = SerialPort.CommandTerminated(":Gd#", "#");
                //:Gd# Get Currently Selected Object/Target Declination
                //Returns: sDD* MM# or sDD*MM’SS#
                //Depending upon the current precision setting for the telescope.

                double targetDec = DmsToDouble(result);

                return targetDec;

            }
            set
            {
                //todo implement low precision version of this.
                if (value > 90)
                    throw new ASCOM.InvalidValueException("Declination cannot be greater than 90.");

                if (value < -90)
                    throw new ASCOM.InvalidValueException("Declination cannot be less than -90.");


                var dms = Util.DegreesToDMS(value, "*", ":", ":", 2);
                var s = value < 0 ? '-' : '+';

                var result = SerialPort.CommandChar($":Sd{s}{dms}#");
                //:SdsDD*MM#
                //Set target object declination to sDD*MM or sDD*MM:SS depending on the current precision setting
                //Returns:
                //1 - Dec Accepted
                //0 – Dec invalid

                if (result == '0')
                {
                    throw new ASCOM.InvalidOperationException("Target declination invalid");
                }

                _targetDeclination = value;
            }
        }

        public double Altitude {
            get
            {
                var result = SerialPort.CommandTerminated(":GA#", "#");
                //:GA# Get Telescope Altitude
                //Returns: sDD* MM# or sDD*MM’SS#
                //The current scope altitude. The returned format depending on the current precision setting.
                return DmsToDouble(result);
            }
        }

        public void AbortSlew()
        {
            SerialPort.Command("#:Q#");
        }

        public void PulseGuide(GuideDirections direction, int duration)
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

            if (UserNewerPulseGuiding)
            {
                _serialPort.Command($":Mg{d}{duration:0000}#");
                Thread.Sleep(duration); //todo figure out if this is really needed
            }
            else
            {
                _serialPort.Command(":RG#"); //Make sure we are at guide rate
                _serialPort.Command($":M{d}#");
                Thread.Sleep(duration);
                _serialPort.Command($":Q{d}#");

                //classic only !!!, this is needed since once in a while one is not enough
                Thread.Sleep(200);
                _serialPort.Command($":Q{d}#");
            }
        }

        public void Park()
        {
            if (AtPark)
                return;

            AtPark = true;
            _serialPort.Command(":hP#");
        }

        public void SlewToCoordinates(double rightAscension, double declination)
        {
            SlewToCoordinatesAsync(rightAscension, declination);

            while (Slewing) //wait for slew to complete
            {
                Util.WaitForMilliseconds(200); //be responsive to AbortSlew();
            }
        }

        public void SlewToCoordinatesAsync(double rightAscension, double declination)
        {
            TargetRightAscension = rightAscension;
            TargetDeclination = declination;

            DoSlewAsync(true);
        }

        public void SlewToAltAz(double azimuth, double altitude)
        {
            SlewToAltAzAsync(azimuth, altitude);

            while (Slewing) //wait for slew to complete
            {
                Util.WaitForMilliseconds(200); //be responsive to AbortSlew();
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


                var dms = Util.DegreesToDMS(value, "*", "'", ".", 2);
                var s = value < 0 ? '-' : '+';

                var result = SerialPort.CommandChar($":Sa{s}{dms}#");
                //:SasDD*MM#
                //Set target object altitude to sDD*MM# or sDD*MM’SS# [LX 16”, Autostar, Autostar II]
                //Returns:
                //1 Object within slew range
                //0 Object out of slew range

                if (result == '0')
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

                var dms = Util.DegreesToDM(value, "*", ":", 2);

                var result = SerialPort.CommandChar($":Sd{dms}#");
                //:SzDDD*MM#
                //Sets the target Object Azimuth[LX 16” and Autostar II only]
                //Returns:
                //0 – Invalid
                //1 - Valid

                if (result == '0')
                    throw new ASCOM.InvalidOperationException("Target Azimuth out of slew range");

            }
        }

        public void SlewToAltAzAsync(double azimuth, double altitude)
        {
            TargetAltitude = altitude;
            TargetAzimuth = azimuth;

            DoSlewAsync(false);
        }

        public void SyncToTarget()
        {
            var result = SerialPort.CommandTerminated(":CM#", "#");
            //:CM# Synchronizes the telescope's position with the currently selected database object's coordinates.
            //Returns:
            //LX200's - a "#" terminated string with the name of the object that was synced.
            //    Autostars & Autostar II - At static string: " M31 EX GAL MAG 3.5 SZ178.0'#"

            if (result == string.Empty)
                throw new ASCOM.InvalidOperationException("Unable to perform sync");
        }

        public void SlewToTarget()
        {
            SlewToTargetAsync();

            while (Slewing)
            {
                Util.WaitForMilliseconds(200);
            }
        }

        public void SlewToTargetAsync()
        {
            if (TargetDeclination == INVALID_PARAMETER || TargetRightAscension == INVALID_PARAMETER )
                throw new ASCOM.InvalidOperationException("No target selected to slew to.");

            DoSlewAsync(true);
        }

        private bool movingAxis()
        {
            return _movingPrimary || _movingSecondary;
        }

        private bool _movingPrimary;
        private bool _movingSecondary;
        public void MoveAxis(TelescopeAxes axis, double rate)
        {
            var absrate = Math.Abs(rate);

            switch(absrate)
            {
                case 0:
                    //do nothing, it's ok this time as we're halting the slew.
                    break;
                case 1:
                    SerialPort.Command(":RG#");
                    //:RG# Set Slew rate to Guiding Rate (slowest)
                    //Returns: Nothing
                    break;                   
                case 2:
                    SerialPort.Command(":RC#");
                    //:RC# Set Slew rate to Centering rate (2nd slowest)
                    //Returns: Nothing
                    break;
                case 3:
                    SerialPort.Command(":RM#");
                    //:RM# Set Slew rate to Find Rate (2nd Fastest)
                    //Returns: Nothing
                    break;
                case 4:
                    SerialPort.Command(":RS#");
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
                        SerialPort.Command(":Qe#");
                        //:Qe# Halt eastward Slews
                        //Returns: Nothing
                        SerialPort.Command(":Qw#");
                        //:Qw# Halt westward Slews
                        //Returns: Nothing
                    }
                    else if (rate > 0)
                    {
                        SerialPort.Command(":Me#");
                        //:Me# Move Telescope East at current slew rate
                        //Returns: Nothing
                        _movingPrimary = true;
                    }
                    else
                    {
                        SerialPort.Command(":Mw#");
                        //:Mw# Move Telescope West at current slew rate
                        //Returns: Nothing
                        _movingPrimary = true;
                    }
                    break;
                case TelescopeAxes.axisSecondary:
                    if (rate == 0)
                    {
                        _movingSecondary = false;
                        SerialPort.Command(":Qn#");
                        //:Qn# Halt northward Slews
                        //Returns: Nothing
                        SerialPort.Command(":Qs#");
                        //:Qs# Halt southward Slews
                        //Returns: Nothing
                    }
                    else if (rate > 0)
                    {
                        SerialPort.Command(":Mn#");
                        //:Mn# Move Telescope North at current slew rate
                        //Returns: Nothing
                        _movingSecondary = true;
                    }
                    else
                    {
                        SerialPort.Command(":Ms#");
                        //:Ms# Move Telescope South at current slew rate
                        //Returns: Nothing
                        _movingSecondary = true;
                    }

                    break;
                default:
                    throw new ASCOM.MethodNotImplementedException("Can not move this axis.");
            }
        }

        //todo remove the polar parameter and split method into two.
        private void DoSlewAsync( bool polar) 
        {
            switch (polar)
            {
                case true:
                    var response = SerialPort.CommandChar(":MS#");
                    //:MS# Slew to Target Object
                    //Returns:
                    //0 Slew is Possible
                    //1<string># Object Below Horizon w/string message
                    //2<string># Object Below Higher w/string message

                    switch (response)
                    {
                        case '0':
                            //We're slewing everything should be working just fine.
                            break;
                        case '1':
                            //Below Horizon 
                            string belowHorizonMessage = SerialPort.ReadTerminated("#");
                            throw new ASCOM.InvalidOperationException(belowHorizonMessage);
                        case '2':
                            //Below Horizon 
                            string belowMinimumElevationMessage = SerialPort.ReadTerminated("#");
                            throw new ASCOM.InvalidOperationException(belowMinimumElevationMessage);
                        default:
                            throw new ASCOM.DriverException("This error should not happen");

                    }
                    break;
                case false:
                    var maResponse = SerialPort.CommandChar(":MA#");
                    //:MA# Autostar, LX 16”, Autostar II – Slew to target Alt and Az
                    //Returns:
                    //0 - No fault
                    //1 – Fault
                    //    LX200 – Not supported

                    if (maResponse == '1')
                    {
                        throw new ASCOM.InvalidOperationException("fault");
                    }
                    break;
            }            
        }

        public bool UserNewerPulseGuiding { get; set; } = true; //todo make this a device setting
    }
}

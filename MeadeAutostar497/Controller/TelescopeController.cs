﻿using System;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using ASCOM.DeviceInterface;

namespace ASCOM.MeadeAutostar497.Controller
{
    //todo stop this being a singleton, and instead use a server to make only a single instance.
    public sealed class TelescopeController : ITelescopeController
    {
        private static readonly Lazy<TelescopeController> lazy = new Lazy<TelescopeController>();

        public static TelescopeController Instance => lazy.Value;

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
                
                double lat = int.Parse(latitude.Substring(1, 2));
                lat = lat + double.Parse(latitude.Substring(4, 2)) / 60;
                if (latitude.Length == 9)
                    lat = lat + double.Parse(latitude.Substring(7, 2)) / 60 / 60;

                if (latitude[0] == '-')
                    lat = -lat;

                return lat;
            }
            set
            {
                if (value > 90)
                    throw new  ASCOM.InvalidValueException("Latitude cannot be greater than 90 degrees.");

                if (value < -90)
                    throw new ASCOM.InvalidValueException("Latitude cannot be less than -90 degrees.");

                int d =  Convert.ToInt32(Math.Floor(value));
                int m = Convert.ToInt32(60 * (value - d));

                var result = SerialPort.CommandChar($":Sts{d:00}*{m:00}#");
                if (result != '1')
                    throw new InvalidOperationException("Failed to set site latitude.");
            }
        }

        public double SiteLongitude
        {
            get
            {
                var longitude = SerialPort.CommandTerminated(":Gg#", "#");

                double l = int.Parse(longitude.Substring(0, 3));
                l = l + double.Parse(longitude.Substring(4, 2)) / 60;
                if (longitude.Length == 9)
                    l = l + double.Parse(longitude.Substring(7, 2)) / 60 / 60;

                if (l > 180)
                    l = l - 360;

                return l;
            }
            set
            {
                if (value > 180)
                    throw new ASCOM.InvalidValueException("Longitude cannot be greater than 180 degrees.");

                if (value < -180)
                    throw new ASCOM.InvalidValueException("Longitude cannot be lower than -180 degrees.");

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
                var alignmentString = SerialPort.CommandTerminated(":GW#", "#");

                switch (alignmentString[0])
                {
                    case 'A': return AlignmentModes.algAltAz;
                    case 'P': return AlignmentModes.algPolar;
                    case 'G': return AlignmentModes.algGermanPolar;
                    default:
                        throw new ASCOM.InvalidValueException($"unknown alignment returned from telescope: {alignmentString[0]}");
                }
                //:GW# Get Scope Alignment Status
                //Returns: <mount><tracking><alignment>#
                //    where:
                //mount: A - AzEl mounted, P - Equatorially mounted, G - german mounted equatorial
                //tracking: T - tracking, N - not tracking
                //alignment: 0 - needs alignment, 1 - one star aligned, 2 - two star aligned, 3 - three star aligned.
            }
            set
            {
                switch (value)
                {
                    case AlignmentModes.algAltAz: SerialPort.Command(":AA#");
                        break;
                    case AlignmentModes.algPolar:
                    case AlignmentModes.algGermanPolar:
                        SerialPort.Command(":AP#");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }

                //:AL# Sets telescope to Land alignment mode
                //Returns: nothing
                //:AP# Sets telescope to Polar alignment mode
                //Returns: nothing
                //:AA# Sets telescope the AltAz alignment mode
                //Returns: nothing
            }
        }

        private bool _parked = false;

        public bool AtPark => _parked;

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
            if (_parked)
                return;

            _parked = true;
            _serialPort.Command(":hP#");
        }

        public bool UserNewerPulseGuiding { get; set; } = true; //todo make this a device setting
    }
}

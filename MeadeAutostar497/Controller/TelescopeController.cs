using System;
using System.IO.Ports;
using System.Linq;

namespace ASCOM.MeadeAutostar497.Controller
{
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
            var firmwareVersionNumber = SerialPort.CommandTerminated("#:GVN#", "#");
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

                var result = SerialPort.CommandTerminated("#:D#", "#");
                return result != string.Empty;
            }
        }

        public DateTime utcDate
        {
            get
            {
                string telescopeDate = SerialPort.CommandTerminated("#:GC#", "#");
                string telescopeTime = SerialPort.CommandTerminated("#:GL#", "#");

                int month = telescopeDate.Substring(0, 2).ToInteger();
                int day = telescopeDate.Substring(3, 2).ToInteger();
                int year = telescopeDate.Substring(6, 2).ToInteger();

                if (year < 2000) //This is a hack that will work until the end of the century
                {
                    year = year + 2000;
                }

                int hour = telescopeTime.Substring(0, 2).ToInteger();
                int minute = telescopeTime.Substring(3, 2).ToInteger();
                int second = telescopeTime.Substring(6, 2).ToInteger();

                var newDate = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);

                return newDate;
            }
            set
            {
                //var result = SerialCommand(":SLHH:MM:SS#", true);
                var timeResult = SerialPort.CommandChar($"#:SL{value:hh:mm:ss}#");
                if (timeResult != '1')
                {
                    throw new InvalidOperationException("Failed to set local time");
                }

                var dateResult = SerialPort.CommandChar($"#:SC{value:MM/dd/yy}#");
                if (dateResult != '1')
                {
                    throw new InvalidOperationException("Failed to set local time");
                }

                //throwing away these two strings which represent 
                SerialPort.ReadTerminated("#");  //Updating Planetary Data#
                SerialPort.ReadTerminated("#");  //                       #
            }

        }

        public void AbortSlew()
        {
            SerialPort.Command("#:Q#");
        }
    }
}

using System;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using ASCOM.Utilities;
using ASCOM.Utilities.Interfaces;

namespace ASCOM.MeadeAutostar497.Controller
{
    public sealed class TelescopeController : ITelescopeController
    {
        private static readonly Lazy<TelescopeController> lazy = new Lazy<TelescopeController>();

        public static TelescopeController Instance => lazy.Value;

        private Mutex serialMutex = new Mutex();

        private ISerial _serialPort;
        public ISerial SerialPort
        {
            get => _serialPort ?? (_serialPort = new Serial());
            set
            {
                if (_serialPort == value)
                    return;

                if (_serialPort != null)
                {
                    if (_serialPort.Connected)
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
            return SerialPort.AvailableComPorts.Contains(value);
        }

        public bool Connected
        {
            get => SerialPort.Connected;
            set
            {
                if (value == Connected)
                    return;

                if (value)
                {
                    //Connecting
                    try
                    {
                        SerialPort.DTREnable = false;
                        SerialPort.RTSEnable = false;
                        SerialPort.Speed = SerialSpeed.ps9600;
                        SerialPort.DataBits = 8;
                        SerialPort.StopBits = SerialStopBits.One;
                        SerialPort.Parity = SerialParity.None;
                        SerialPort.PortName = Port;
                        SerialPort.Connected = true;

                        TestConnectionActive();
                    }
                    catch (Exception)
                    {
                        SerialPort.Connected = false;
                        throw;
                    }
                }
                else
                {
                    //Disconnecting
                    SerialPort.Connected = false;
                }
            }
        }

        private void TestConnectionActive()
        {
            var firmwareVersionNumber = CommandString("GVN");
            if (string.IsNullOrEmpty(firmwareVersionNumber))
            {
                throw new InvalidOperationException("Failed to communicate with telescope."); 
            }
        }

        public string CommandString(string command)
        {
            return CommandString($"#:{command}#", false);
        }

        public string CommandString(string command, bool raw)
        {
            // it's a good idea to put all the low level communication with the device here,
            // then all communication calls this function
            // you need something to ensure that only one command is in progress at a time
            return SerialCommand(command, true);
        }

        public bool Slewing
        {
            get
            {
                if (!Connected) return false;

                var result = CommandString("D");
                return result != string.Empty;
            }
        }

        public DateTime utcDate
        {
            get
            {
                string telescopeDate = CommandString("GC");
                string telescopeTime = CommandString("GL");

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
                var timeResult = SerialCommand($"#:SL{value:hh:mm:ss}#", true);
                if (timeResult != "1")
                {
                    throw new InvalidOperationException("Failed to set local time");
                }

                var dateResult = SerialCommand($"#:SC{value:MM/dd/yy}#", true);
                if (dateResult.Substring(0,1) != "1")
                {
                    throw new InvalidOperationException("Failed to set local time");
                }
            }

        }

        private string SerialCommand(string command, bool expectsResult )
        {
            serialMutex.WaitOne();
            try
            {
                SerialPort.Transmit(command);
                if (expectsResult)
                {
                    string result = SerialPort.ReceiveTerminated("#");

                    return result;
                }               
                return string.Empty;
            }
            finally
            {
                SerialPort.ClearBuffers();
                serialMutex.ReleaseMutex();
            }
        }
    }
}

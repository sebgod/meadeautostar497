using System;
using ASCOM.Utilities;
using ASCOM.Utilities.Interfaces;

namespace ASCOM.MeadeAutostar497.Controller
{
    public sealed class TelescopeController : ITelescopeController
    {
        private static readonly Lazy<TelescopeController> lazy = new Lazy<TelescopeController>();

        public static TelescopeController Instance => lazy.Value;

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
                        throw new InvalidOperationException("Please disconnect before changing the port.");
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

                _port = value;
            }
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
                    SerialPort.DTREnable = false;
                    SerialPort.RTSEnable = false;
                    SerialPort.Speed = SerialSpeed.ps9600;
                    SerialPort.DataBits = 8;
                    SerialPort.StopBits = SerialStopBits.One;
                    SerialPort.Parity = SerialParity.None;
                    SerialPort.PortName = Port;
                    SerialPort.Connected = true;

                    //todo perform test to ensure that connection has been made correctly.
                }
                else
                {
                    //Disconnecting
                    SerialPort.Connected = false;
                }
            }
        }

        public string CommandString(string command, bool raw)
        {
            // it's a good idea to put all the low level communication with the device here,
            // then all communication calls this function
            // you need something to ensure that only one command is in progress at a time

            throw new ASCOM.MethodNotImplementedException("CommandString");
        }
    }
}

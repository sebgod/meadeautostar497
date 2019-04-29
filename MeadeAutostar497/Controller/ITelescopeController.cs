using System;
using System.IO.Ports;

namespace ASCOM.MeadeAutostar497.Controller
{
    public interface ITelescopeController
    {
        ISerialProcessor SerialPort { get; set; }
        string Port { get; set; }
        bool Connected { get; set; }

        bool Slewing { get; }
        DateTime utcDate { get; set; }
    }
}
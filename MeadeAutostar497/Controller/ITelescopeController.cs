using ASCOM.Utilities.Interfaces;

namespace ASCOM.MeadeAutostar497.Controller
{
    public interface ITelescopeController
    {
        ISerial SerialPort { get; set; }
        string Port { get; set; }
        bool Connected { get; set; }

        string CommandString(string command, bool raw);
    }
}
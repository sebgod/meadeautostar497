using System.IO.Ports;
using System.Runtime.InteropServices;

namespace ASCOM.MeadeAutostar497.Controller
{
    [ComVisible(false)]
    public interface ISerialProcessor
    {
        bool IsOpen { get; }
        bool DtrEnable { get; set; }
        bool RtsEnable { get; set; }
        int BaudRate { get; set; }
        int DataBits { get; set; }
        StopBits StopBits { get; set; }
        Parity Parity { get; set; }
        string PortName { get; set; }
        string[]  GetPortNames();
        void Open();
        void Close();

        string CommandTerminated(string command, string terminator);
        char CommandChar(string command);
        string ReadTerminated(string terminator);
        void Command(string command);
    }
}
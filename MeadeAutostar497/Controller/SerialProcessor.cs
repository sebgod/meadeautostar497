using System;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;

namespace ASCOM.MeadeAutostar497.Controller
{
    [ComVisible(false)]
    public class SerialProcessor : ISerialProcessor
    {
        private SerialPort _serialPort = new SerialPort();
        private Mutex serialMutex = new Mutex();

        public bool IsOpen => _serialPort.IsOpen;

        public bool DtrEnable
        {
            get => _serialPort.DtrEnable;
            set => _serialPort.DtrEnable = value;
        }

        public bool RtsEnable
        {
            get => _serialPort.RtsEnable;
            set => _serialPort.RtsEnable = value;
        }

        public int BaudRate
        {
            get => _serialPort.BaudRate;
            set => _serialPort.BaudRate = value;
        }

        public int DataBits
        {
            get => _serialPort.DataBits;
            set => _serialPort.DataBits = value;
        }

        public StopBits StopBits
        {
            get => _serialPort.StopBits;
            set => _serialPort.StopBits = value;
        }

        public Parity Parity
        {
            get => _serialPort.Parity;
            set => _serialPort.Parity = value;
        }

        public string PortName
        {
            get => _serialPort.PortName;
            set => _serialPort.PortName = value;
        }

        public string[] GetPortNames()
        {
            return SerialPort.GetPortNames();
        }

        public void Open()
        {
            _serialPort.Open();
        }

        public void Close()
        {
            _serialPort.Close();
        }

        public string CommandTerminated(string command, string terminator)
        {
            Lock();
            try
            {
                _serialPort.Write(command);
                string result = _serialPort.ReadTo("#");
                return result;
            }
            finally
            {
                Unlock();
            }
        }

        public char CommandChar(string command)
        {
            Lock();
            try
            {
                _serialPort.Write(command);
                var result = _serialPort.ReadChar();
                return Convert.ToChar(result);
            }
            finally
            {
                Unlock();
            }
        }

        public string ReadTerminated(string terminator)
        {
            Lock();
            try
            {
                string result = _serialPort.ReadTo("#");
                return result;
            }
            finally
            {
                Unlock();
            }
        }

        public void Command(string command)
        {
            Lock();
            try
            {
                _serialPort.Write(command);
            }
            finally
            {
                Unlock();
            }
        }

        public void Lock()
        {
            serialMutex.WaitOne();
        }

        public void Unlock()
        {
            serialMutex.ReleaseMutex();
        }
    }
}

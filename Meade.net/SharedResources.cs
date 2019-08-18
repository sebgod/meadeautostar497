//
// ================
// Shared Resources
// ================
//
// This class is a container for all shared resources that may be needed
// by the drivers served by the Local Server. 
//
// NOTES:
//
//	* ALL DECLARATIONS MUST BE STATIC HERE!! INSTANCES OF THIS CLASS MUST NEVER BE CREATED!
//
// Written by:	Bob Denny	29-May-2007
// Modified by Chris Rowland and Peter Simpson to hamdle multiple hardware devices March 2011
//
using System;
using System.Collections.Generic;
using ASCOM.Utilities;

namespace ASCOM.Meade.net
{
    /// <summary>
    /// The resources shared by all drivers and devices, in this example it's a serial port with a shared SendMessage method
    /// an idea for locking the message and handling connecting is given.
    /// In reality extensive changes will probably be needed.
    /// Multiple drivers means that several applications connect to the same hardware device, aka a hub.
    /// Multiple devices means that there are more than one instance of the hardware, such as two focusers.
    /// In this case there needs to be multiple instances of the hardware connector, each with it's own connection count.
    /// </summary>
    public static class SharedResources
    {
        // object used for locking to prevent multiple drivers accessing common code at the same time
        private static readonly object LockObject = new object();

        // Shared serial port. This will allow multiple drivers to use one single serial port.
        private static Serial _sSharedSerial; // Shared serial port

        //
        // Public access to shared resources
        //

        #region single serial port connector

        //
        // this region shows a way that a single serial port could be connected to by multiple 
        // drivers.
        //
        // Connected is used to handle the connections to the port.
        //
        // SendMessage is a way that messages could be sent to the hardware without
        // conflicts between different drivers.
        //
        // All this is for a single connection, multiple connections would need multiple ports
        // and a way to handle connecting and disconnection from them - see the
        // multi driver handling section for ideas.
        //

        /// <summary>
        /// Shared serial port
        /// </summary>
        public static Serial SharedSerial => _sSharedSerial ?? (_sSharedSerial = new Serial());

        /// <summary>
        /// number of connections to the shared serial port
        /// </summary>
        public static int Connections { get; set; } = 0;

        public static void SendBlind(string message)
        {
            lock (LockObject)
            {
                SharedSerial.ClearBuffers();
                SharedSerial.Transmit(message);
            }
        }

        public static bool SendBool(string message)
        {
            SharedSerial.ClearBuffers();
            return SendChar(message) == "1";
        }

        /// <summary>
        /// Example of a shared SendMessage method, the lock
        /// prevents different drivers tripping over one another.
        /// It needs error handling and assumes that the message will be sent unchanged
        /// and that the reply will always be terminated by a "#" character.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string SendString(string message)
        {
            lock (LockObject)
            {
                SharedSerial.ClearBuffers();
                SharedSerial.Transmit(message);
                return SharedSerial.ReceiveTerminated("#").TrimEnd('#');
            }
        }

        public static string SendChar(string message)
        {
            lock (LockObject)
            {
                SharedSerial.ClearBuffers();
                SharedSerial.Transmit(message);
                return SharedSerial.ReceiveCounted(1);
            }
        }

        public static string ReadTerminated()
        {
            lock (LockObject)
            {
                return SharedSerial.ReceiveTerminated("#");
            }
        }

        public static string ReadCharacters(int throwAwayCharacters)
        {
            lock (LockObject)
            {
                return SharedSerial.ReceiveCounted(throwAwayCharacters);
            }
        }

        /// <summary>
        /// Example of handling connecting to and disconnection from the
        /// shared serial port.
        /// Needs error handling
        /// the port name etc. needs to be set up first, this could be done by the driver
        /// checking Connected and if it's false setting up the port before setting connected to true.
        /// It could also be put here.
        /// </summary>
        public static bool Connected
        {
            set
            {
                lock (LockObject)
                {
                    if (value)
                    {
                        if (Connections == 0)
                            SharedSerial.Connected = true;
                        Connections++;
                    }
                    else
                    {
                        Connections--;
                        if (Connections <= 0)
                        {
                            SharedSerial.Connected = false;
                        }
                    }
                }
            }
            get => SharedSerial.Connected;
        }

        #endregion

        #region Profile

        private const string DriverId = "ASCOM.MeadeGeneric.Telescope";

        // Constants used for Profile persistence
        private const string ComPortProfileName = "COM Port";
        private const string TraceStateProfileName = "Trace Level";
        private const string GuideRateProfileName = "Guide Rate Arc Seconds Per Second";
        private const string PrecisionProfileName = "Precision";

        public static void WriteProfile(ProfileProperties profileProperties)
        {
            lock (LockObject)
            {
                using (Profile driverProfile = new Profile())
                {
                    driverProfile.DeviceType = "Telescope";
                    driverProfile.WriteValue(DriverId, TraceStateProfileName, profileProperties.TraceLogger.ToString());
                    driverProfile.WriteValue(DriverId, ComPortProfileName, profileProperties.ComPort);
                    driverProfile.WriteValue(DriverId, GuideRateProfileName, profileProperties.GuideRateArcSecondsPerSecond.ToString());
                    driverProfile.WriteValue(DriverId, PrecisionProfileName, profileProperties.Precision);
                }
            }
        }

        private const string ComPortDefault = "COM1";
        private const string TraceStateDefault = "false";
        private const string GuideRateProfileNameDefault = "10.077939"; //67% of sidereal rate
        private const string PrecisionDefault = "Unchanged";

        public static ProfileProperties ReadProfile()
        {
            lock (LockObject)
            {
                ProfileProperties profileProperties = new ProfileProperties();
                using (Profile driverProfile = new Profile())
                {
                    driverProfile.DeviceType = "Telescope";
                    profileProperties.ComPort = driverProfile.GetValue(DriverId, ComPortProfileName, string.Empty, ComPortDefault);
                    profileProperties.TraceLogger = Convert.ToBoolean(driverProfile.GetValue(DriverId, TraceStateProfileName, string.Empty, TraceStateDefault));
                    profileProperties.GuideRateArcSecondsPerSecond = double.Parse(driverProfile.GetValue(DriverId, GuideRateProfileName, string.Empty, GuideRateProfileNameDefault));
                    profileProperties.Precision = driverProfile.GetValue(DriverId, PrecisionProfileName, string.Empty, PrecisionDefault);
                }

                return profileProperties;
            }
        }

        #endregion

        #region SetupDialog

        public static void SetupDialog()
        {
            // consider only showing the setup dialog if not connected
            // or call a different dialog if connected
            if (Connections > 0)
            {
                System.Windows.Forms.MessageBox.Show("Already connected, please disconnect before altering settings");
                return;
            }

            var profileProperties = ReadProfile();

            using (SetupDialogForm f = new SetupDialogForm())
            {
                f.SetProfile(profileProperties);
                
                var result = f.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    profileProperties = f.GetProfile();

                    WriteProfile(profileProperties); // Persist device configuration values to the ASCOM Profile store
                }
            }
        }

        #endregion
        
        #region Multi Driver handling

        public static string ProductName { get; private set; } = string.Empty;
        public static string FirmwareVersion { get; private set; } = string.Empty;

        // this section illustrates how multiple drivers could be handled,
        // it's for drivers where multiple connections to the hardware can be made and ensures that the
        // hardware is only disconnected from when all the connected devices have disconnected.

        // It is NOT a complete solution!  This is to give ideas of what can - or should be done.
        //
        // An alternative would be to move the hardware control here, handle connecting and disconnecting,
        // and provide the device with a suitable connection to the hardware.
        //
        /// <summary>
        /// dictionary carrying device connections.
        /// The Key is the connection number that identifies the device, it could be the COM port name,
        /// USB ID or IP Address, the Value is the DeviceHardware class
        /// </summary>
        private static readonly Dictionary<string, DeviceHardware> _connectedDevices = new Dictionary<string, DeviceHardware>();

        /// <summary>
        /// This is called in the driver Connect(true) property,
        /// it add the device id to the list of devices if it's not there and increments the device count.
        /// </summary>
        /// <param name="deviceId"></param>
        public static void Connect(string deviceId)
        {
            lock (LockObject)
            {
                if (!_connectedDevices.ContainsKey(deviceId))
                    _connectedDevices.Add(deviceId, new DeviceHardware());
                _connectedDevices[deviceId].Count++; // increment the value

                if (deviceId == "Serial")
                {
                    if (_connectedDevices[deviceId].Count == 1)
                    {
                        var profileProperties = ReadProfile();
                        SharedSerial.PortName = profileProperties.ComPort;
                        SharedSerial.DTREnable = false;
                        SharedSerial.RTSEnable = false;
                        SharedSerial.DataBits = 8;
                        SharedSerial.StopBits = SerialStopBits.One;
                        SharedSerial.Parity = SerialParity.None;
                        SharedSerial.Speed = SerialSpeed.ps9600;
                        SharedSerial.Handshake = SerialHandshake.None;
                        SharedSerial.Connected = true;

                        ProductName = SendString(":GVP#");
                        FirmwareVersion = SendString(":GVN#");
                    }
                }
            }
        }

        public static void Disconnect(string deviceId)
        {
            lock (LockObject)
            {
                if (_connectedDevices.ContainsKey(deviceId))
                {
                    _connectedDevices[deviceId].Count--;
                    if (_connectedDevices[deviceId].Count <= 0)
                    {
                        _connectedDevices.Remove(deviceId);
                        if (deviceId == "Serial")
                        {
                            SharedSerial.Connected = false;
                        }
                    }
                }
            }
        }

        public static bool IsConnected(string deviceId)
        {
            if (_connectedDevices.ContainsKey(deviceId))
                return (_connectedDevices[deviceId].Count > 0);
            else
                return false;
        }

        #endregion

        public static void Lock(Action action)
        {
            lock (LockObject)
            {
                action();
            }
        }

        public static T Lock<T>(Func<T> func)
        {
            lock (LockObject)
            {
                return func();
            }
        }

        /// <summary>
        /// Skeleton of a hardware class, all this does is hold a count of the connections,
        /// in reality extra code will be needed to handle the hardware in some way
        /// </summary>
        public class DeviceHardware
        {
            private int _count;

            internal int Count
            {
                set => _count = value;
                get => _count;
            }

            internal DeviceHardware()
            {
                Count = 0;
            }
        }

        //#region ServedClassName attribute
        ///// <summary>
        ///// This is only needed if the driver is targeted at  platform 5.5, it is included with Platform 6
        ///// </summary>
        //[global::System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
        //public sealed class ServedClassNameAttribute : Attribute
        //{
        //    // See the attribute guidelines at 
        //    //  http://go.microsoft.com/fwlink/?LinkId=85236

        //    /// <summary>
        //    /// Gets or sets the 'friendly name' of the served class, as registered with the ASCOM Chooser.
        //    /// </summary>
        //    /// <value>The 'friendly name' of the served class.</value>
        //    public string DisplayName { get; private set; }
        //    /// <summary>
        //    /// Initializes a new instance of the <see cref="ServedClassNameAttribute"/> class.
        //    /// </summary>
        //    /// <param name="servedClassName">The 'friendly name' of the served class.</param>
        //    public ServedClassNameAttribute(string servedClassName)
        //    {
        //        DisplayName = servedClassName;
        //    }
        //}
        //#endregion
    }
}
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
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using ASCOM.DeviceInterface;
using ASCOM.Meade.net.Wrapper;
using ASCOM.Utilities;
using ASCOM.Utilities.Interfaces;

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
        private static ISerial _sSharedSerial; // Shared serial port

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
        /// Shared serial port. Do not directly access this method.
        /// </summary>
        public static ISerial SharedSerial
        {
            get => _sSharedSerial ?? (_sSharedSerial = new Serial());
            set => _sSharedSerial = value;
        }

        public static IProfileFactory ProfileFactory
        {
            get => _profileFactory ?? (_profileFactory = new ProfileFactory());
            set => _profileFactory = value;
        }

        //todo add code to ensure that there is a minimum gap between commands. 5ms as default.
        public static void SendBlind(string message, bool raw = false)
        {
            lock (LockObject)
            {
                SharedSerial.ClearBuffers();
                var encodedMessage = raw ? message : $"#:{message}#";
                SharedSerial.Transmit(encodedMessage);
            }
        }

        /// <summary>
        /// Example of a shared SendMessage method, the lock
        /// prevents different drivers tripping over one another.
        /// It needs error handling and assumes that the message will be sent unchanged
        /// and that the reply will always be terminated by a "#" character.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="raw"></param>
        /// <returns></returns>
        public static string SendString(string message, bool raw = false)
        {
            lock (LockObject)
            {
                SharedSerial.ClearBuffers();

                var encodedMessage = raw ? message : $"#:{message}#";
                SharedSerial.Transmit(encodedMessage);

                try
                {
                    return SharedSerial.ReceiveTerminated("#").TrimEnd('#');
                }
                catch (COMException ex)
                {
                    if (ex.Message.Contains("Timed out waiting for received data"))
                        throw new TimeoutException(ex.Message, ex);

                    throw;
                }
            }
        }

        public static bool SendBool(string command, bool raw = false)
        {
            var result = SendChar(command, raw);

            return result == "1";
        }

        public static string SendChar(string command, bool raw = false)
        {
            return SendChars(command, raw, count: 1);
        }

        public static string SendChars(string command, bool raw = false, int count = 1)
        {
            lock (LockObject)
            {
                SharedSerial.ClearBuffers();

                var encodedMessage = raw ? command : $"#:{command}#";
                SharedSerial.Transmit(encodedMessage);

                try
                {
                    return SharedSerial.ReceiveCounted(count);
                }
                catch (COMException ex) when (ex.Message.Contains("Timed out waiting for received data"))
                {
                    throw new TimeoutException(ex.Message, ex);
                }
            }
        }

        public static string ReadTerminated()
        {
            lock (LockObject)
            {
                return SharedSerial.ReceiveTerminated("#");
            }
        }

        public static void ReadCharacters(int throwAwayCharacters)
        {
            lock (LockObject)
            {
                SharedSerial.ReceiveCounted(throwAwayCharacters);
            }
        }

        #endregion

        #region Profile

        private const string DriverId = "ASCOM.MeadeGeneric.Telescope";

        // Constants used for Profile persistence
        private const string ComPortProfileName = "COM Port";
        private const string RtsDtrProfileName = "Rts / Dtr";
        private const string TraceStateProfileName = "Trace Level";
        private const string GuideRateProfileName = "Guide Rate Arc Seconds Per Second";
        private const string PrecisionProfileName = "Precision";
        private const string GuidingStyleProfileName = "Guiding Style";
        private const string BacklashCompensationName = "Backlash Compensation";
        private const string ReverseFocusDirectionName = "Reverse Focuser Direction";
        private const string DynamicBreakingName = "Dynamic Breaking";
        private const string SiteElevationName = "Site Elevation";
        private const string SettleTimeName = "Settle Time";

        private const string SpeedName = "Speed";
        private const string DataBitsName = "Data Bits";
        private const string StopBitsName = "Stop Bits";
        private const string HandShakeName = "Hand Shake";
        private const string ParityName = "Parity";
        private const string SendDateTimeName = "Send Date and time on connect";
        private const string ParkedBehaviourName = "Parked Behaviour";
        private const string ParkedAltName = "Parked Altitude";
        private const string ParkedAzimuthName = "Parked Azimuth";

        public static void WriteProfile(ProfileProperties profileProperties)
        {
            lock (LockObject)
            {
                using (IProfileWrapper driverProfile = ProfileFactory.Create())
                {
                    driverProfile.DeviceType = "Telescope";
                    driverProfile.WriteValue(DriverId, TraceStateProfileName, profileProperties.TraceLogger.ToString());
                    driverProfile.WriteValue(DriverId, ComPortProfileName, profileProperties.ComPort);
                    driverProfile.WriteValue(DriverId, RtsDtrProfileName, profileProperties.RtsDtrEnabled.ToString());
                    driverProfile.WriteValue(DriverId, SpeedName, profileProperties.Speed.ToString(CultureInfo.InvariantCulture));
                    driverProfile.WriteValue(DriverId, DataBitsName, profileProperties.DataBits.ToString(CultureInfo.InvariantCulture));
                    driverProfile.WriteValue(DriverId, StopBitsName, profileProperties.StopBits);
                    driverProfile.WriteValue(DriverId, HandShakeName, profileProperties.Handshake);
                    driverProfile.WriteValue(DriverId, ParityName, profileProperties.Parity);
                    driverProfile.WriteValue(DriverId, GuideRateProfileName, profileProperties.GuideRateArcSecondsPerSecond.ToString(CultureInfo.InvariantCulture));
                    driverProfile.WriteValue(DriverId, PrecisionProfileName, profileProperties.Precision);
                    driverProfile.WriteValue(DriverId, GuidingStyleProfileName, profileProperties.GuidingStyle);
                    driverProfile.WriteValue(DriverId, BacklashCompensationName, profileProperties.BacklashCompensation.ToString());
                    driverProfile.WriteValue(DriverId, ReverseFocusDirectionName, profileProperties.ReverseFocusDirection.ToString());
                    driverProfile.WriteValue(DriverId, DynamicBreakingName, profileProperties.DynamicBreaking.ToString());
                    driverProfile.WriteValue(DriverId, SiteElevationName, profileProperties.SiteElevation.ToString(CultureInfo.InvariantCulture));
                    driverProfile.WriteValue(DriverId, SettleTimeName, profileProperties.SettleTime.ToString());
                    driverProfile.WriteValue(DriverId, SendDateTimeName, profileProperties.SendDateTime.ToString());
                    driverProfile.WriteValue(DriverId, ParkedBehaviourName, profileProperties.ParkedBehaviour.GetDescription());
                    driverProfile.WriteValue(DriverId, ParkedAltName, profileProperties.ParkedAlt.ToString(CultureInfo.InvariantCulture));
                    driverProfile.WriteValue(DriverId, ParkedAzimuthName, profileProperties.ParkedAz.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private const string ComPortDefault = "COM1";
        private const string RtsDtrDefault = "false";
        private const string TraceStateDefault = "false";
        private const string GuideRateProfileNameDefault = "10.077939"; //67% of sidereal rate
        private const string PrecisionDefault = "Unchanged";
        private const string GuidingStyleDefault = "Auto";
        private const string BacklashCompensationDefault = "3000";
        private const string ReverseFocuserDiectionDefault = "true";
        private const string DynamicBreakingDefault = "true";
        private const string SiteElevationDefault = "0";
        private const string SettleTimeDefault = "2";
        private const string SpeedDefault = "9600";
        private const string DataBitsDefault = "8";
        private const string StopBitsDefault = "One";
        private const string HandShakeDefault = "None";
        private const string ParityDefault = "None";
        private const string SendDateTimeDefault = "false";
        private const string ParkedBehaviourDefault = "No Coordinates";
        private const string ParkedAltDefault = "0";
        private const string ParkedAzimuthDefault = "180";

        public static ProfileProperties ReadProfile()
        {
            lock (LockObject)
            {
                ProfileProperties profileProperties = new ProfileProperties();
                using (IProfileWrapper driverProfile = ProfileFactory.Create())
                {
                    driverProfile.DeviceType = "Telescope";
                    profileProperties.ComPort = driverProfile.GetValue(DriverId, ComPortProfileName, string.Empty, ComPortDefault);
                    profileProperties.RtsDtrEnabled = Convert.ToBoolean(driverProfile.GetValue(DriverId, RtsDtrProfileName, string.Empty, RtsDtrDefault));
                    profileProperties.TraceLogger = Convert.ToBoolean(driverProfile.GetValue(DriverId, TraceStateProfileName, string.Empty, TraceStateDefault));
                    profileProperties.GuideRateArcSecondsPerSecond = double.Parse(driverProfile.GetValue(DriverId, GuideRateProfileName, string.Empty, GuideRateProfileNameDefault), NumberFormatInfo.InvariantInfo);
                    profileProperties.Precision = driverProfile.GetValue(DriverId, PrecisionProfileName, string.Empty, PrecisionDefault);
                    profileProperties.GuidingStyle = driverProfile.GetValue(DriverId, GuidingStyleProfileName, string.Empty, GuidingStyleDefault);
                    profileProperties.BacklashCompensation = Convert.ToInt32(driverProfile.GetValue(DriverId, BacklashCompensationName, string.Empty, BacklashCompensationDefault));
                    profileProperties.ReverseFocusDirection = Convert.ToBoolean(driverProfile.GetValue(DriverId, ReverseFocusDirectionName, string.Empty, ReverseFocuserDiectionDefault));
                    profileProperties.DynamicBreaking = Convert.ToBoolean(driverProfile.GetValue(DriverId, DynamicBreakingName, string.Empty, DynamicBreakingDefault));
                    profileProperties.SiteElevation = Convert.ToInt32(driverProfile.GetValue(DriverId, SiteElevationName, string.Empty, SiteElevationDefault));
                    profileProperties.SettleTime = Convert.ToInt16(driverProfile.GetValue(DriverId, SettleTimeName, string.Empty, SettleTimeDefault));
                    profileProperties.StopBits = driverProfile.GetValue(DriverId, StopBitsName, string.Empty, StopBitsDefault);
                    profileProperties.DataBits = Convert.ToInt32(driverProfile.GetValue(DriverId, DataBitsName, string.Empty, DataBitsDefault));
                    profileProperties.Handshake = driverProfile.GetValue(DriverId, HandShakeName, string.Empty, HandShakeDefault);
                    profileProperties.Speed = Convert.ToInt32(driverProfile.GetValue(DriverId, SpeedName, string.Empty, SpeedDefault));
                    profileProperties.Parity = driverProfile.GetValue(DriverId, ParityName, string.Empty, ParityDefault);
                    profileProperties.SendDateTime = Convert.ToBoolean(driverProfile.GetValue(DriverId, SendDateTimeName, string.Empty, SendDateTimeDefault));

                    profileProperties.ParkedBehaviour = EnumExtensionMethods.GetValueFromDescription<ParkedBehaviour>(driverProfile.GetValue(DriverId, ParkedBehaviourName, string.Empty, ParkedBehaviourDefault));
                    profileProperties.ParkedAlt = double.Parse(driverProfile.GetValue(DriverId, ParkedAltName, string.Empty, ParkedAltDefault), NumberFormatInfo.InvariantInfo);
                    profileProperties.ParkedAz = double.Parse(driverProfile.GetValue(DriverId, ParkedAzimuthName, string.Empty, ParkedAzimuthDefault), NumberFormatInfo.InvariantInfo);
                }

                return profileProperties;
            }
        }

        #endregion

        #region SetupDialog

        public static void SetupDialog()
        {
            try
            {
                var profileProperties = ReadProfile();

                using (SetupDialogForm f = new SetupDialogForm())
                {
                    f.SetProfile(profileProperties);

                    if (IsConnected())
                    {
                        f.SetReadOnlyMode();
                    }

                    var result = f.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        profileProperties = f.GetProfile();

                        WriteProfile(
                            profileProperties); // Persist device configuration values to the ASCOM Profile store
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
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
        private static readonly Dictionary<string, DeviceHardware> ConnectedDevices = new Dictionary<string, DeviceHardware>();

        private static readonly Dictionary<string, DeviceHardware> ConnectedDeviceIds = new Dictionary<string, DeviceHardware>();
        private static IProfileFactory _profileFactory;


        /// <summary>
        /// This is called in the driver Connect(true) property,
        /// it add the device id to the list of devices if it's not there and increments the device count.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="driverId"></param>
        /// <param name="traceLogger"></param>
        public static ConnectionInfo Connect(string deviceId, string driverId, ITraceLogger traceLogger)
        {
            lock (LockObject)
            {
                if (!ConnectedDevices.ContainsKey(deviceId))
                    ConnectedDevices.Add(deviceId, new DeviceHardware());

                if (!ConnectedDeviceIds.ContainsKey(driverId))
                    ConnectedDeviceIds.Add(driverId, new DeviceHardware());

                if (deviceId == "Serial")
                {
                    if (ConnectedDevices[deviceId].Count == 0)
                    {
                        var profileProperties = ReadProfile();
                        SharedSerial.PortName = profileProperties.ComPort;
                        SharedSerial.DTREnable = profileProperties.RtsDtrEnabled;
                        SharedSerial.RTSEnable = profileProperties.RtsDtrEnabled;
                        SharedSerial.DataBits = profileProperties.DataBits;
                        SharedSerial.StopBits = (SerialStopBits)Enum.Parse(typeof(SerialStopBits), profileProperties.StopBits);
                        SharedSerial.Parity = (SerialParity)Enum.Parse(typeof(SerialParity), profileProperties.Parity);
                        SharedSerial.Speed = (SerialSpeed)profileProperties.Speed;
                        SharedSerial.Handshake = (SerialHandshake)Enum.Parse(typeof(SerialHandshake), profileProperties.Handshake);
                        SharedSerial.Connected = true;

                        try
                        {
                            ProductName = SendString("GVP");
                            FirmwareVersion = SendString("GVN");
                        }
                        catch (Exception ex)
                        {
                            traceLogger.LogIssue("Connect", $"Error getting telescope information \"{ex.Message}\" setting to LX200 Classic mode.");
                            ProductName = TelescopeList.LX200CLASSIC;
                            FirmwareVersion = "Unknown";
                        }

                        if (ProductName == ":GVP")
                        {
                            traceLogger.LogIssue("Connect", "Serial port is looping back data, something is wrong with the hardware.");
                            //This means that the serial port is looping back what's been sent, something is very wrong.
                            SharedSerial.Connected = false;

                            throw new Exception("Serial port is looping back data, something is wrong with the hardware.");
                        }

                        try
                        {
                            string utcOffSet = SendString("GG");
                            //:GG# Get UTC offset time
                            //Returns: sHH# or sHH.H#
                            //The number of decimal hours to add to local time to convert it to UTC. If the number is a whole number the
                            //sHH# form is returned, otherwise the longer form is returned.
                            if (!double.TryParse(utcOffSet, out var utcOffsetHours))
                            {
                                var message = "Unable to decode response from the telescope, This is likely a hardware serial communications error.";
                                traceLogger.LogIssue("Connect", message);
                                throw new Exception(message);
                            }

                            traceLogger.LogMessage("Connect", $"Offset from UTC: {utcOffsetHours}", false);
                        }
                        catch (Exception)
                        {
                            SharedSerial.Connected = false;
                            throw;
                        }
                    }
                }
                else
                    throw new ArgumentException($"deviceId {deviceId} not currently supported");

                ConnectedDevices[deviceId].Count++; // increment the value
                ConnectedDeviceIds[driverId].Count++; // increment the value

                return new ConnectionInfo
                {
                    //Connections = ConnectedDevices[deviceId].Count,
                    SameDevice = ConnectedDeviceIds[driverId].Count
                };
            }
        }

        public static void Disconnect(string deviceId, string driverId)
        {
            lock (LockObject)
            {
                if (ConnectedDevices.ContainsKey(deviceId))
                {
                    ConnectedDevices[deviceId].Count--;
                    if (ConnectedDevices[deviceId].Count <= 0)
                    {
                        ConnectedDevices.Remove(deviceId);
                        if (deviceId == "Serial")
                        {
                            SharedSerial.Connected = false;
                        }
                    }
                }

                if (ConnectedDeviceIds.ContainsKey(driverId))
                {
                    ConnectedDeviceIds[driverId].Count--;
                }
            }
        }

        private static bool IsConnected()
        {
            foreach (var device in ConnectedDevices)
            {
                if (device.Value.Count > 0)
                    return true;
            }

            return false;
        }

        #endregion

        /// <summary>
        /// Skeleton of a hardware class, all this does is hold a count of the connections,
        /// in reality extra code will be needed to handle the hardware in some way
        /// </summary>
        private class DeviceHardware
        {
            internal int Count { set; get; }

            internal DeviceHardware()
            {
                Count = 0;
            }
        }

        public static void SetParked(bool atPark, ParkedPosition parkedPosition)
        {
            IsParked = atPark;
            ParkedPosition = parkedPosition;
        }

        private static readonly ThreadSafeValue<bool> _isParked = false;
        public static bool IsParked
        {
            get => _isParked;
            private set => _isParked.Set(value);
        }

        private static ParkedPosition _parkedPosition;
        public static ParkedPosition ParkedPosition
        {
            get => _parkedPosition;
            private set => Interlocked.Exchange(ref _parkedPosition, value);
        }

        private static readonly ThreadSafeValue<PierSide> _sideOfPier = PierSide.pierUnknown;
        /// <summary>
        /// Start with <see cref="PierSide.pierUnknown"/>.
        /// As we do not know the physical declination axis position, we have to keep track manually.
        /// </summary>
        public static PierSide SideOfPier
        {
            get => _sideOfPier;
            internal set => _sideOfPier.Set(value);
        }

        private static readonly ThreadSafeValue<double?> _targetRightAscension = null as double?;
        public static double? TargetRightAscension
        {
            get => _targetRightAscension;
            internal set => _targetRightAscension.Set(value);
        }

        private static readonly ThreadSafeValue<double?> _targetDeclination = null as double?;
        public static double? TargetDeclination
        {
            get => _targetDeclination;
            internal set => _targetDeclination.Set(value);
        }

        private static int _slewSettleTime;
        public static short SlewSettleTime
        {
            get => Convert.ToInt16(_slewSettleTime);
            internal set => Interlocked.Exchange(ref _slewSettleTime, value);
        }

        private static readonly ThreadSafeValue<bool> _isLongFormat = false;
        public static bool IsLongFormat
        {
            get => _isLongFormat;
            internal set => _isLongFormat.Set(value);
        }

        private static readonly ThreadSafeValue<bool> _movingPrimary = false;
        public static bool MovingPrimary
        {
            get => _movingPrimary;
            internal set => _movingPrimary.Set(value);
        }

        private static readonly ThreadSafeValue<bool> _movingSecondary = false;
        public static bool MovingSecondary
        {
            get => _movingSecondary;
            internal set => _movingSecondary.Set(value);
        }

        private static readonly ThreadSafeValue<DateTime> _earliestNonSlewingTime = DateTime.MinValue;
        public static DateTime EarliestNonSlewingTime
        {
            get => _earliestNonSlewingTime;
            internal set => _earliestNonSlewingTime.Set(value);
        }

        private static readonly ThreadSafeValue<bool> _isTargetCoordinateInitRequired = true;
        public static bool IsTargetCoordinateInitRequired
        {
            get => _isTargetCoordinateInitRequired;
            internal set => _isTargetCoordinateInitRequired.Set(value);
        }

        private static readonly ThreadSafeValue<bool> _isGuiding = false;
        public static bool IsGuiding
        {
            get => _isGuiding;
            internal set => _isGuiding.Set(value);
        }
    }
}
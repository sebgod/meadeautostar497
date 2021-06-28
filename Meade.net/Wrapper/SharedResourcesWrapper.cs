using System;
using ASCOM.DeviceInterface;
using ASCOM.Utilities.Interfaces;

namespace ASCOM.Meade.net.Wrapper
{
    public interface ISharedResourcesWrapper
    {
        ConnectionInfo Connect(string deviceId, string driverId, ITraceLogger traceLogger);
        void Disconnect(string deviceId, string driverId);

        string ProductName { get; }

        string FirmwareVersion { get; }

        void Lock(Action action);
        T Lock<T>(Func<T> func);

        string SendString(string message, bool raw = false);
        void SendBlind(string message, bool raw = false);
        bool SendBool(string command, bool raw = false);
        string SendChar(string message, bool raw = false);
        string SendChars(string message, bool raw = false, int count = 1);

        string ReadTerminated();

        ProfileProperties ReadProfile();

        void SetupDialog();
        void WriteProfile(ProfileProperties profileProperties);
        void ReadCharacters(int throwAwayCharacters);

        void SetParked(bool atPark, ParkedPosition parkedPosition);
        bool IsParked { get; }
        ParkedPosition ParkedPosition { get; }

        PierSide SideOfPier { get; set; }
        double? TargetRightAscension { get; set; }
        double? TargetDeclination { get; set; }

        short SlewSettleTime { get; set; }

        bool IsLongFormat { get; set; }

        bool MovingPrimary { get; set; }

        bool MovingSecondary { get; set; }

        DateTime EarliestNonSlewingTime { get; set; }

        bool IsTargetCoordinateInitRequired { get; set; }

        bool IsGuiding { get; set; }
    }

    public class SharedResourcesWrapper : ISharedResourcesWrapper
    {
        public ConnectionInfo Connect(string deviceId, string driverId, ITraceLogger traceLogger)
        {
            return SharedResources.Connect(deviceId, driverId, traceLogger);
        }

        public void Disconnect(string deviceId, string driverId)
        {
            SharedResources.Disconnect(deviceId, driverId);
        }

        public string ProductName => SharedResources.ProductName;

        public string FirmwareVersion => SharedResources.FirmwareVersion;

        public void Lock(Action action)
        {
            SharedResources.Lock(action);
        }

        public T Lock<T>(Func<T> func)
        {
            return SharedResources.Lock(func);
        }

        public string SendString(string message, bool raw = false)
        {
            return SharedResources.SendString(message, raw);
        }

        public void SendBlind(string message, bool raw = false)
        {
            SharedResources.SendBlind(message, raw);
        }

        public bool SendBool(string command, bool raw = false)
        {
            return SharedResources.SendBool(command, raw);
        }

        public string SendChar(string message, bool raw = false)
        {
            return SharedResources.SendChar(message, raw);
        }

        public string SendChars(string message, bool raw = false, int count = 1)
        {
            return SharedResources.SendChars(message, raw, count);
        }

        public string ReadTerminated()
        {
            return SharedResources.ReadTerminated();
        }

        public void ReadCharacters(int throwAwayCharacters)
        {
            SharedResources.ReadCharacters(throwAwayCharacters);
        }

        public ProfileProperties ReadProfile()
        {
            return SharedResources.ReadProfile();
        }

        public void SetupDialog()
        {
            SharedResources.SetupDialog();
        }

        public void WriteProfile(ProfileProperties profileProperties)
        {
            SharedResources.WriteProfile(profileProperties);
        }

        public void SetParked(bool atPark, ParkedPosition parkedPosition)
        {
            SharedResources.SetParked(atPark, parkedPosition);
        }

        public bool IsParked => SharedResources.IsParked;

        public ParkedPosition ParkedPosition => SharedResources.ParkedPosition;

        public PierSide SideOfPier
        {
            get => SharedResources.SideOfPier;
            set => SharedResources.SideOfPier = value;
        }

        public double? TargetRightAscension
        {
            get => SharedResources.TargetRightAscension;
            set => SharedResources.TargetRightAscension = value;
        }

        public double? TargetDeclination
        {
            get => SharedResources.TargetDeclination;
            set => SharedResources.TargetDeclination = value;
        }

        public short SlewSettleTime
        {
            get => SharedResources.SlewSettleTime;
            set => SharedResources.SlewSettleTime = value;
        }

        public bool IsLongFormat
        {
            get => SharedResources.IsLongFormat;
            set => SharedResources.IsLongFormat = value;
        }

        public bool MovingPrimary
        {
            get => SharedResources.MovingPrimary;
            set => SharedResources.MovingPrimary = value;
        }

        public bool MovingSecondary
        {
            get => SharedResources.MovingSecondary;
            set => SharedResources.MovingSecondary = value;
        }

        public DateTime EarliestNonSlewingTime
        {
            get => SharedResources.EarliestNonSlewingTime;
            set => SharedResources.EarliestNonSlewingTime = value;
        }

        public bool IsTargetCoordinateInitRequired
        {
            get => SharedResources.IsTargetCoordinateInitRequired;
            set => SharedResources.IsTargetCoordinateInitRequired = value;
        }

        public bool IsGuiding
        {
            get => SharedResources.IsGuiding;
            set => SharedResources.IsGuiding = value;
        }
    }
}

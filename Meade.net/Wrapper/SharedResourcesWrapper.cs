using System;

namespace ASCOM.Meade.net.Wrapper
{
    public interface ISharedResourcesWrapper
    {
        void Connect(string deviceId);
        void Disconnect(string deviceId);

        string ProductName { get; }

        string FirmwareVersion { get; }

        void Lock(Action action);
        T Lock<T>(Func<T> func);

        string SendString(string message);
        void SendBlind(string message);
        string SendChar(string message);

        string ReadTerminated();

        ProfileProperties ReadProfile();

        void SetupDialog();
        void WriteProfile(ProfileProperties profileProperties);
        string ReadCharacters(int throwAwayCharacters);
    }

    public class SharedResourcesWrapper : ISharedResourcesWrapper
    {
        public void Connect(string deviceId)
        {
            SharedResources.Connect( deviceId);
        }

        public void Disconnect(string deviceId)
        {
            SharedResources.Disconnect(deviceId);
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

        public string SendString(string message)
        {
            return SharedResources.SendString(message);
        }

        public void SendBlind(string message)
        {
            SharedResources.SendBlind(message);
        }

        public string SendChar(string message)
        {
            return SharedResources.SendChar(message);
        }

        public string ReadTerminated()
        {
            return SharedResources.ReadTerminated();
        }

        public string ReadCharacters(int throwAwayCharacters)
        {
            return SharedResources.ReadCharacters(throwAwayCharacters);
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
    }
}

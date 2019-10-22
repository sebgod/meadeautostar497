using System;
using System.Collections;
using ASCOM.Utilities;
using ASCOM.Utilities.Interfaces;

namespace ASCOM.Meade.net.Wrapper
{
    public interface IProfileWrapper : IProfile, IProfileExtra, IDisposable
    {

    }

    public class ProfileWrapper : IProfileWrapper
    {
        private readonly Profile _profile = new Profile();

        public ArrayList RegisteredDevices(string deviceType)
        {
            return _profile.RegisteredDevices(deviceType);
        }

        public bool IsRegistered(string driverId)
        {
            return _profile.IsRegistered(driverId);
        }

        public void Register(string driverId, string descriptiveName)
        {
            _profile.Register(driverId, descriptiveName);
        }

        public void Unregister(string driverId)
        {
            _profile.Unregister(driverId);
        }

        public string GetValue(string driverId, string name, string subKey, string defaultValue)
        {
            return _profile.GetValue(driverId, name, subKey, defaultValue);
        }

        public void WriteValue(string driverId, string name, string value, string subKey)
        {
            _profile.WriteValue(driverId, name, value);
        }

        public ArrayList Values(string driverId, string subKey)
        {
            return _profile.Values(driverId, subKey);
        }

        public void DeleteValue(string driverId, string name, string subKey)
        {
            _profile.DeleteValue(driverId, name, subKey);
        }

        public void CreateSubKey(string driverId, string subKey)
        {
            _profile.CreateSubKey(driverId, subKey);
        }

        public ArrayList SubKeys(string driverId, string subKey)
        {
            return _profile.SubKeys(driverId, subKey);
        }

        public void DeleteSubKey(string driverId, string subKey)
        {
            _profile.DeleteSubKey(driverId, subKey);
        }

        public string GetProfileXML(string deviceId)
        {
            return _profile.GetProfileXML(deviceId);
        }

        public void SetProfileXML(string deviceId, string xml)
        {
            _profile.SetProfileXML(deviceId, xml);
        }

        public string DeviceType
        {
            get => _profile.DeviceType;
            set => _profile.DeviceType = value;
        }
        public ArrayList RegisteredDeviceTypes => _profile.RegisteredDeviceTypes;

        public void MigrateProfile(string currentPlatformVersion)
        {
            _profile.MigrateProfile(currentPlatformVersion);
        }

        public void DeleteValue(string driverId, string name)
        {
            _profile.DeleteValue(driverId, name);
        }

        public string GetValue(string driverId, string name)
        {
            return _profile.GetValue(driverId, name);
        }

        public string GetValue(string driverId, string name, string subKey)
        {
            return _profile.GetValue(driverId, name, subKey);
        }

        public ArrayList SubKeys(string driverId)
        {
            return _profile.SubKeys(driverId);
        }

        public ArrayList Values(string driverId)
        {
            return _profile.Values(driverId);
        }

        public void WriteValue(string driverId, string name, string value)
        {
            _profile.WriteValue(driverId, name, value);
        }

        public ASCOMProfile GetProfile(string driverId)
        {
            return _profile.GetProfile(driverId);
        }

        public void SetProfile(string driverId, ASCOMProfile xmlProfileKey)
        {
            _profile.SetProfile(driverId, xmlProfileKey);
        }

        public void Dispose()
        {
            _profile.Dispose();
        }
    }
}

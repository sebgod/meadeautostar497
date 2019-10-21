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

        public ArrayList RegisteredDevices(string DeviceType)
        {
            return _profile.RegisteredDevices(DeviceType);
        }

        public bool IsRegistered(string DriverID)
        {
            return _profile.IsRegistered(DriverID);
        }

        public void Register(string DriverID, string DescriptiveName)
        {
            _profile.Register(DriverID, DescriptiveName);
        }

        public void Unregister(string DriverID)
        {
            _profile.Unregister(DriverID);
        }

        public string GetValue(string DriverID, string Name, string SubKey, string DefaultValue)
        {
            return _profile.GetValue(DriverID, Name, SubKey, DefaultValue);
        }

        public void WriteValue(string DriverID, string Name, string Value, string SubKey)
        {
            _profile.WriteValue(DriverID, Name, Value);
        }

        public ArrayList Values(string DriverID, string SubKey)
        {
            return _profile.Values(DriverID, SubKey);
        }

        public void DeleteValue(string DriverID, string Name, string SubKey)
        {
            _profile.DeleteValue(DriverID, Name, SubKey);
        }

        public void CreateSubKey(string DriverID, string SubKey)
        {
            _profile.CreateSubKey(DriverID, SubKey);
        }

        public ArrayList SubKeys(string DriverID, string SubKey)
        {
            return _profile.SubKeys(DriverID, SubKey);
        }

        public void DeleteSubKey(string DriverID, string SubKey)
        {
            _profile.DeleteSubKey(DriverID, SubKey);
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

        public void MigrateProfile(string CurrentPlatformVersion)
        {
            _profile.MigrateProfile(CurrentPlatformVersion);
        }

        public void DeleteValue(string DriverID, string Name)
        {
            _profile.DeleteValue(DriverID, Name);
        }

        public string GetValue(string DriverID, string Name)
        {
            return _profile.GetValue(DriverID, Name);
        }

        public string GetValue(string DriverID, string Name, string SubKey)
        {
            return _profile.GetValue(DriverID, Name, SubKey);
        }

        public ArrayList SubKeys(string DriverID)
        {
            return _profile.SubKeys(DriverID);
        }

        public ArrayList Values(string DriverID)
        {
            return _profile.Values(DriverID);
        }

        public void WriteValue(string DriverID, string Name, string Value)
        {
            _profile.WriteValue(DriverID, Name, Value);
        }

        public ASCOMProfile GetProfile(string DriverId)
        {
            return _profile.GetProfile(DriverId);
        }

        public void SetProfile(string DriverId, ASCOMProfile XmlProfileKey)
        {
            _profile.SetProfile(DriverId, XmlProfileKey);
        }

        public void Dispose()
        {
            _profile.Dispose();
        }
    }
}

﻿using System;
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

        string ReadTerminated();

        ProfileProperties ReadProfile();

        void SetupDialog();
        void WriteProfile(ProfileProperties profileProperties);
        void ReadCharacters(int throwAwayCharacters);
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

        public string SendChar(string message,bool raw = false)
        {
            return SharedResources.SendChar(message, raw);
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
    }
}

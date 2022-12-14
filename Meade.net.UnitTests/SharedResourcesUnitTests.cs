
using System;
using System.Globalization;
using ASCOM.DeviceInterface;
using ASCOM.Meade.net;
using ASCOM.Meade.net.Wrapper;
using ASCOM.Utilities.Interfaces;
using Moq;
using NUnit.Framework;

namespace Meade.net.UnitTests
{
    [TestFixture]
    public class SharedResourcesUnitTests
    {
        private Mock<ISerial> _serialMock;
        private Mock<ITraceLogger> _traceLoggerMock;

        [SetUp]
        public void Setup()
        {
            _serialMock = new Mock<ISerial>();
            _serialMock.SetupAllProperties();

            _traceLoggerMock = new Mock<ITraceLogger>();

            SharedResources.SharedSerial = _serialMock.Object;
        }

        [Test]
        public void CheckThatSerialPortIsSetToUseMock()
        {
            Assert.That(SharedResources.SharedSerial, Is.EqualTo(_serialMock.Object));
        }

        [TestCase(true, "Test")]
        [TestCase(false, "#:Test#")]
        public void SendBlind_WhenCalled_Then_ClearsBuffersAndSendsMessage(bool raw, string expectedMessage)
        {
            var sendMessage = "Test";
            SharedResources.SendBlind(sendMessage, raw);

            _serialMock.Verify(x=> x.ClearBuffers(), Times.Once);
            _serialMock.Verify(x=>x.Transmit(expectedMessage), Times.Once);
        }

        [TestCase(false, "#:Test#")]
        [TestCase(true, "Test")]
        public void SendChar_WhenCalled_ThenSendsMessageAndReadsExpectedNumberOfCharacters(bool raw, string expectedCommand)
        {
            var command = "Test";
            var expectedResult = "A";

            _serialMock.Setup(x => x.ReceiveCounted(1)).Returns(expectedResult);

            var result = SharedResources.SendChar(command, raw);

            _serialMock.Verify(x => x.ClearBuffers(), Times.Once);
            _serialMock.Verify(x => x.Transmit(expectedCommand), Times.Once);
            _serialMock.Verify(x => x.ReceiveCounted(1), Times.Once);
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [TestCase(true, "Test")]
        [TestCase(false, "#:Test#")]
        public void SendString_WhenCalled_ThenSendsMessageAndReadsResultUntilTerminatorFound(bool includePrefix, string expectedMessage)
        {
            var transmitMessage = "Test";
            var expectedResult = "TestMessage#";

            _serialMock.Setup(x => x.ReceiveTerminated("#")).Returns(expectedResult);

            var result = SharedResources.SendString(transmitMessage, includePrefix);

            _serialMock.Verify(x => x.ClearBuffers(), Times.Once);
            _serialMock.Verify(x => x.Transmit(expectedMessage), Times.Once);
            _serialMock.Verify(x => x.ReceiveTerminated("#"), Times.Once);
            Assert.That(result, Is.EqualTo(expectedResult.TrimEnd('#')));
        }

        [Test]
        public void ReadTerminated_WhenCalled_ThenReadsResultUntilTerminatorFound()
        {
            var expectedResult = "TestMessage#";

            _serialMock.Setup(x => x.ReceiveTerminated("#")).Returns(expectedResult);

            var result = SharedResources.ReadTerminated();

            _serialMock.Verify(x => x.ReceiveTerminated("#"), Times.Once);
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void ReadCharacters_WhenCalled_ThenReadsSpecificNumberOfCharacters()
        {
            var numberOfCharacters = 5;

            SharedResources.ReadCharacters(numberOfCharacters);

            _serialMock.Verify(x => x.ReceiveCounted(numberOfCharacters), Times.Once);
        }

        [Test]
        public void WriteProfile_WhenCalled_WritesExpectedProfileSettings()
        {
            string DriverId = "ASCOM.MeadeGeneric.Telescope";

            Mock<IProfileWrapper> profileWrapperMock = new Mock<IProfileWrapper>();
            profileWrapperMock.SetupAllProperties();

            IProfileWrapper profeWrapper = profileWrapperMock.Object;

            Mock<IProfileFactory> profileFactoryMock = new Mock<IProfileFactory>();
            profileFactoryMock.Setup(x => x.Create()).Returns(profileWrapperMock.Object);

            SharedResources.ProfileFactory = profileFactoryMock.Object;

            var profileProperties = new ProfileProperties
            {
                TraceLogger = false,
                ComPort = "TestComPort"
            };

            SharedResources.WriteProfile(profileProperties);

            Assert.That(profeWrapper.DeviceType, Is.EqualTo("Telescope"));
            profileWrapperMock.Verify( x => x.WriteValue(DriverId, "Trace Level", profileProperties.TraceLogger.ToString()), Times.Once);

            profileWrapperMock.Verify(x => x.WriteValue(DriverId, "COM Port", profileProperties.ComPort), Times.Once);
            profileWrapperMock.Verify(x => x.WriteValue(DriverId, "Speed", profileProperties.Speed.ToString()), Times.Once);
            profileWrapperMock.Verify(x => x.WriteValue(DriverId, "Data Bits", profileProperties.DataBits.ToString()), Times.Once);
            profileWrapperMock.Verify(x => x.WriteValue(DriverId, "Stop Bits", profileProperties.StopBits), Times.Once);
            profileWrapperMock.Verify(x => x.WriteValue(DriverId, "Hand Shake", profileProperties.Handshake), Times.Once);
            profileWrapperMock.Verify(x => x.WriteValue(DriverId, "Parity", profileProperties.Parity), Times.Once);
            profileWrapperMock.Verify(x => x.WriteValue(DriverId, "Rts / Dtr", profileProperties.RtsDtrEnabled.ToString()), Times.Once);

            profileWrapperMock.Verify(x => x.WriteValue(DriverId, "Guide Rate Arc Seconds Per Second", profileProperties.GuideRateArcSecondsPerSecond.ToString(CultureInfo.CurrentCulture)), Times.Once);
            profileWrapperMock.Verify(x => x.WriteValue(DriverId, "Precision", profileProperties.Precision), Times.Once);
            profileWrapperMock.Verify(x => x.WriteValue(DriverId, "Guiding Style", profileProperties.GuidingStyle), Times.Once);

            profileWrapperMock.Verify(x => x.WriteValue(DriverId, "Backlash Compensation", profileProperties.BacklashCompensation.ToString(CultureInfo.CurrentCulture)), Times.Once);
            profileWrapperMock.Verify(x => x.WriteValue(DriverId, "Reverse Focuser Direction", profileProperties.ReverseFocusDirection.ToString()), Times.Once);
        }

        [Test]
        public void ReadProfile_WhenCalled_ReturnsExpectedDefaultValues()
        {
            string DriverId = "ASCOM.MeadeGeneric.Telescope";

            string TraceStateDefault = "false";

            string ComPortDefault = "COM1";
            string SpeedDefault = "9600";
            string DataBitsDefault = "8";
            string StopBitsDefault = "One";
            string HandshakeDefault = "None";
            string ParityDefault = "None";
            string RtsDtrEnabledDefault = "true";

            string GuideRateProfileNameDefault = "10.077939"; //67% of sidereal rate
            string PrecisionDefault = "Unchanged";
            string GuidingStyleDefault = "Auto";

            string BacklashCompensationDefault = "3000";
            string ReverseFocuserDiectionDefault = "true";

            string SendDateTimeDefault = "true";
            string SkipPromptsDefault = "true";

            string ParkedBehaviourDefault = "No Coordinates";
            string ParkedAltDefault = "0";
            string ParkedAzimuthDefault = "180";

            Mock<IProfileWrapper> profileWrapperMock = new Mock<IProfileWrapper>();
            profileWrapperMock.SetupAllProperties();

            profileWrapperMock.Setup(x => x.GetValue(DriverId, "Trace Level", string.Empty, TraceStateDefault))
                .Returns(() =>
                    TraceStateDefault);
            profileWrapperMock.Setup(x => x.GetValue(DriverId, "COM Port", string.Empty, ComPortDefault))
                .Returns(ComPortDefault);
            profileWrapperMock
                .Setup(x => x.GetValue(DriverId, "Guide Rate Arc Seconds Per Second", string.Empty,
                    GuideRateProfileNameDefault)).Returns(GuideRateProfileNameDefault);
            profileWrapperMock.Setup(x => x.GetValue(DriverId, "Precision", string.Empty, PrecisionDefault))
                .Returns(PrecisionDefault);
            profileWrapperMock.Setup(x => x.GetValue(DriverId, "Guiding Style", string.Empty, GuidingStyleDefault))
                .Returns(GuidingStyleDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Backlash Compensation", string.Empty, BacklashCompensationDefault))
                .Returns(BacklashCompensationDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Reverse Focuser Direction", string.Empty, "true"))
                .Returns(() => ReverseFocuserDiectionDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Speed", string.Empty, SpeedDefault))
                .Returns(() => SpeedDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Data Bits", string.Empty, DataBitsDefault))
                .Returns(() => DataBitsDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Stop Bits", string.Empty, StopBitsDefault))
                .Returns(() => StopBitsDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Hand Shake", string.Empty, HandshakeDefault))
                .Returns(() => HandshakeDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parity", string.Empty, ParityDefault))
                .Returns(() => ParityDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Rts / Dtr", string.Empty, "false"))
                .Returns(() => RtsDtrEnabledDefault);

            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parked Behaviour", string.Empty, ParkedBehaviourDefault))
                .Returns(() => ParityDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parked Altitude", string.Empty, ParkedAltDefault))
                .Returns(() => ParkedAltDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parked Azimuth", string.Empty, ParkedAzimuthDefault))
                .Returns(() => ParkedAzimuthDefault);

            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Send Date and time on connect", string.Empty, "false"))
                .Returns(() => SendDateTimeDefault);

            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Skip date prompts on connect", string.Empty, "false"))
                .Returns(() => SkipPromptsDefault);



            IProfileWrapper profeWrapper = profileWrapperMock.Object;

            Mock<IProfileFactory> profileFactoryMock = new Mock<IProfileFactory>();
            profileFactoryMock.Setup(x => x.Create()).Returns(profileWrapperMock.Object);

            SharedResources.ProfileFactory = profileFactoryMock.Object;

            var profileProperties = SharedResources.ReadProfile();

            Assert.That(profeWrapper.DeviceType, Is.EqualTo("Telescope"));

            Assert.That(profileProperties.TraceLogger, Is.EqualTo(bool.Parse(TraceStateDefault)));

            Assert.That(profileProperties.ComPort, Is.EqualTo(ComPortDefault));

            Assert.That(profileProperties.GuideRateArcSecondsPerSecond,
                Is.EqualTo(double.Parse(GuideRateProfileNameDefault)));
            Assert.That(profileProperties.Precision, Is.EqualTo(PrecisionDefault));
            Assert.That(profileProperties.GuidingStyle, Is.EqualTo(GuidingStyleDefault));

            Assert.That(profileProperties.BacklashCompensation, Is.EqualTo(int.Parse(BacklashCompensationDefault)));
            Assert.That(profileProperties.ReverseFocusDirection, Is.EqualTo(bool.Parse(ReverseFocuserDiectionDefault)));

            Assert.That(profileProperties.Speed, Is.EqualTo(int.Parse(SpeedDefault)));
            Assert.That(profileProperties.DataBits, Is.EqualTo(int.Parse(DataBitsDefault)));
            Assert.That(profileProperties.StopBits, Is.EqualTo(StopBitsDefault));
            Assert.That(profileProperties.Handshake, Is.EqualTo(HandshakeDefault));
            Assert.That(profileProperties.Parity, Is.EqualTo(ParityDefault));
            Assert.That(profileProperties.RtsDtrEnabled, Is.EqualTo(bool.Parse(RtsDtrEnabledDefault)));

            Assert.That(profileProperties.SendDateTime, Is.EqualTo(bool.Parse(SendDateTimeDefault)));
        }

        [TestCase("TCP")]
        [TestCase("Carrier Pigeon")]
        public void Connect_WhenDeviceIdIsNotSerial_ThenThrowsException( string deviceId)
        {
            var result = Assert.Throws<ArgumentException>( () => { SharedResources.Connect(deviceId, string.Empty, _traceLoggerMock.Object); } );

            Assert.That( result.Message, Is.EqualTo($"deviceId {deviceId} not currently supported") );
        }

        [Test]
        public void Connect_WhenDeviceIdIsSerialButGVPEchos_ThenThrowsException()
        {
            string deviceId = "Serial";
            string DriverId = "ASCOM.MeadeGeneric.Telescope";

            string ComPortDefault = "COM1";
            string SpeedDefault = "9600";
            string DataBitsDefault = "8";
            string StopBitsDefault = "One";
            string HandshakeDefault = "None";
            string ParityDefault = "None";
            string RtsDtrEnabledDefault = "false";

            string TraceStateDefault = "false";
            string GuideRateProfileNameDefault = "10.077939"; //67% of sidereal rate
            string PrecisionDefault = "Unchanged";

            string ParkedBehaviourDefault = "No Coordinates";
            string ParkedAltDefault = "0";
            string ParkedAzimuthDefault = "180";

            Mock<IProfileWrapper> profileWrapperMock = new Mock<IProfileWrapper>();
            profileWrapperMock.SetupAllProperties();

            profileWrapperMock.Setup(x => x.GetValue(DriverId, "Trace Level", string.Empty, TraceStateDefault))
                .Returns(TraceStateDefault);
            profileWrapperMock.Setup(x => x.GetValue(DriverId, "COM Port", string.Empty, ComPortDefault))
                .Returns(ComPortDefault);
            profileWrapperMock.Setup(x =>
                x.GetValue(DriverId, "Speed", string.Empty, SpeedDefault))
                    .Returns(() => SpeedDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Data Bits", string.Empty, DataBitsDefault))
                .Returns(() => DataBitsDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Stop Bits", string.Empty, StopBitsDefault))
                .Returns(() => StopBitsDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Hand Shake", string.Empty, HandshakeDefault))
                .Returns(() => HandshakeDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parity", string.Empty, ParityDefault))
                .Returns(() => ParityDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Rts / Dtr", string.Empty, RtsDtrEnabledDefault))
                .Returns(() => RtsDtrEnabledDefault);

            profileWrapperMock
                .Setup(x => x.GetValue(DriverId, "Guide Rate Arc Seconds Per Second", string.Empty,
                    GuideRateProfileNameDefault)).Returns(GuideRateProfileNameDefault);
            profileWrapperMock.Setup(x => x.GetValue(DriverId, "Precision", string.Empty, PrecisionDefault))
                .Returns(PrecisionDefault);

            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parked Behaviour", string.Empty, ParkedBehaviourDefault))
                .Returns(() => ParityDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parked Altitude", string.Empty, ParkedAltDefault))
                .Returns(() => ParkedAltDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parked Azimuth", string.Empty, ParkedAzimuthDefault))
                .Returns(() => ParkedAzimuthDefault);

            Mock<IProfileFactory> profileFactoryMock = new Mock<IProfileFactory>();
            profileFactoryMock.Setup(x => x.Create()).Returns(profileWrapperMock.Object);

            SharedResources.ProfileFactory = profileFactoryMock.Object;

            string serialPortReturn = string.Empty;

            _serialMock.Setup(x => x.Transmit("#:GVP#")).Callback(() => { serialPortReturn = ":GVP#"; });
            _serialMock.Setup(x => x.Transmit("#:GG#")).Callback(() => { serialPortReturn = "0"; });
            _serialMock.Setup(x => x.ReceiveTerminated("#")).Returns( () => serialPortReturn);

            var result = Assert.Throws<Exception>(() => { SharedResources.Connect(deviceId, string.Empty, _traceLoggerMock.Object); });
            Assert.That(result.Message, Is.EqualTo("Serial port is looping back data, something is wrong with the hardware."));
        }

        [Test]
        public void Connect_WhenDeviceIdIsSerialButGVPNotSupported_ThenConnectsAndSetsProductToLX200Classic()
        {
            string deviceId = "Serial";
            string DriverId = "ASCOM.MeadeGeneric.Telescope";

            string TraceStateDefault = "false";

            string ComPortDefault = "COM1";
            string SpeedDefault = "9600";
            string DataBitsDefault = "8";
            string StopBitsDefault = "One";
            string HandshakeDefault = "None";
            string ParityDefault = "None";
            string RtsDtrEnabledDefault = "false";

            string GuideRateProfileNameDefault = "10.077939"; //67% of sidereal rate
            string PrecisionDefault = "Unchanged";

            string ParkedBehaviourDefault = "No Coordinates";
            string ParkedAltDefault = "0";
            string ParkedAzimuthDefault = "180";

            Mock<IProfileWrapper> profileWrapperMock = new Mock<IProfileWrapper>();
            profileWrapperMock.SetupAllProperties();

            profileWrapperMock.Setup(x => x.GetValue(DriverId, "Trace Level", string.Empty, TraceStateDefault))
                .Returns(TraceStateDefault);
            profileWrapperMock.Setup(x => x.GetValue(DriverId, "COM Port", string.Empty, ComPortDefault))
                .Returns(ComPortDefault);
            profileWrapperMock
                .Setup(x => x.GetValue(DriverId, "Guide Rate Arc Seconds Per Second", string.Empty,
                    GuideRateProfileNameDefault)).Returns(GuideRateProfileNameDefault);
            profileWrapperMock.Setup(x => x.GetValue(DriverId, "Precision", string.Empty, PrecisionDefault))
                .Returns(PrecisionDefault);
            profileWrapperMock.Setup(x =>
                x.GetValue(DriverId, "Speed", string.Empty, SpeedDefault))
                    .Returns(() => SpeedDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Data Bits", string.Empty, DataBitsDefault))
                .Returns(() => DataBitsDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Stop Bits", string.Empty, StopBitsDefault))
                .Returns(() => StopBitsDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Hand Shake", string.Empty, HandshakeDefault))
                .Returns(() => HandshakeDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parity", string.Empty, ParityDefault))
                .Returns(() => ParityDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Rts / Dtr", string.Empty, RtsDtrEnabledDefault))
                .Returns(() => RtsDtrEnabledDefault);

            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parked Behaviour", string.Empty, ParkedBehaviourDefault))
                .Returns(() => ParityDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parked Altitude", string.Empty, ParkedAltDefault))
                .Returns(() => ParkedAltDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parked Azimuth", string.Empty, ParkedAzimuthDefault))
                .Returns(() => ParkedAzimuthDefault);

            Mock<IProfileFactory> profileFactoryMock = new Mock<IProfileFactory>();
            profileFactoryMock.Setup(x => x.Create()).Returns(profileWrapperMock.Object);

            SharedResources.ProfileFactory = profileFactoryMock.Object;

            string serialPortReturn = string.Empty;

            _serialMock.Setup(x => x.Transmit("#:GVP#")).Callback(() => {
                serialPortReturn = string.Empty;
                throw new Exception("Testerror");
            });
            _serialMock.Setup(x => x.Transmit("#:GG#")).Callback(() => { serialPortReturn = "0"; });
            _serialMock.Setup(x => x.ReceiveTerminated("#")).Returns(() => serialPortReturn);

            var connectionResult = SharedResources.Connect(deviceId, string.Empty, _traceLoggerMock.Object);
            try
            {


                Assert.That(connectionResult.SameDevice, Is.EqualTo(1));
                Assert.That(SharedResources.ProductName, Is.EqualTo(TelescopeList.LX200CLASSIC));
            }
            finally
            {
                SharedResources.Disconnect(deviceId, String.Empty);
            }
        }

        [Test]
        public void Connect_WhenDeviceIdIsSerialButGVPIsAutostar_ThenConnectsAndSetsProductToAutostarAndFirmware()
        {
            string deviceId = "Serial";

            string DriverId = "ASCOM.MeadeGeneric.Telescope";

            string TraceStateDefault = "false";

            string ComPortDefault = "COM1";
            string SpeedDefault = "9600";
            string DataBitsDefault = "8";
            string StopBitsDefault = "One";
            string HandshakeDefault = "None";
            string ParityDefault = "None";
            string RtsDtrEnabledDefault = "false";

            string GuideRateProfileNameDefault = "10.077939"; //67% of sidereal rate
            string PrecisionDefault = "Unchanged";

            string ParkedBehaviourDefault = "No Coordinates";
            string ParkedAltDefault = "0";
            string ParkedAzimuthDefault = "180";

            Mock<IProfileWrapper> profileWrapperMock = new Mock<IProfileWrapper>();
            profileWrapperMock.SetupAllProperties();

            profileWrapperMock.Setup(x => x.GetValue(DriverId, "Trace Level", string.Empty, TraceStateDefault))
                .Returns(TraceStateDefault);
            profileWrapperMock.Setup(x => x.GetValue(DriverId, "COM Port", string.Empty, ComPortDefault))
                .Returns(ComPortDefault);
            profileWrapperMock
                .Setup(x => x.GetValue(DriverId, "Guide Rate Arc Seconds Per Second", string.Empty,
                    GuideRateProfileNameDefault)).Returns(GuideRateProfileNameDefault);
            profileWrapperMock.Setup(x => x.GetValue(DriverId, "Precision", string.Empty, PrecisionDefault))
                .Returns(PrecisionDefault);
            profileWrapperMock.Setup(x =>
               x.GetValue(DriverId, "Speed", string.Empty, SpeedDefault))
                   .Returns(() => SpeedDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Data Bits", string.Empty, DataBitsDefault))
                .Returns(() => DataBitsDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Stop Bits", string.Empty, StopBitsDefault))
                .Returns(() => StopBitsDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Hand Shake", string.Empty, HandshakeDefault))
                .Returns(() => HandshakeDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parity", string.Empty, ParityDefault))
                .Returns(() => ParityDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Rts / Dtr", string.Empty, RtsDtrEnabledDefault))
                .Returns(() => RtsDtrEnabledDefault);

            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parked Behaviour", string.Empty, ParkedBehaviourDefault))
                .Returns(() => ParityDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parked Altitude", string.Empty, ParkedAltDefault))
                .Returns(() => ParkedAltDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parked Azimuth", string.Empty, ParkedAzimuthDefault))
                .Returns(() => ParkedAzimuthDefault);

            Mock<IProfileFactory> profileFactoryMock = new Mock<IProfileFactory>();
            profileFactoryMock.Setup(x => x.Create()).Returns(profileWrapperMock.Object);

            SharedResources.ProfileFactory = profileFactoryMock.Object;

            string serialPortReturn = string.Empty;

            _serialMock.Setup(x => x.Transmit("#:GVP#")).Callback(() => { serialPortReturn = TelescopeList.Autostar497; });
            _serialMock.Setup(x => x.Transmit("#:GVN#")).Callback(() => { serialPortReturn = TelescopeList.Autostar497_43Eg; });
            _serialMock.Setup(x => x.Transmit("#:GG#")).Callback(() => { serialPortReturn = "0"; });
            _serialMock.Setup(x => x.ReceiveTerminated("#")).Returns(() => serialPortReturn);

            var connectionResult = SharedResources.Connect(deviceId, string.Empty, _traceLoggerMock.Object);
            try
            {
                Assert.That(connectionResult.SameDevice, Is.EqualTo(1));
                Assert.That(SharedResources.ProductName, Is.EqualTo(TelescopeList.Autostar497));
                Assert.That(SharedResources.FirmwareVersion, Is.EqualTo(TelescopeList.Autostar497_43Eg));
            }
            finally
            {
                SharedResources.Disconnect(deviceId, String.Empty);
            }
        }

        [Test]
        public void Connect_WhenSerialPortIsNotRespondingCorrectly_ThenExceptionThrown()
        {
            string deviceId = "Serial";

            string DriverId = "ASCOM.MeadeGeneric.Telescope";

            string TraceStateDefault = "false";

            string ComPortDefault = "COM1";
            string SpeedDefault = "9600";
            string DataBitsDefault = "8";
            string StopBitsDefault = "One";
            string HandshakeDefault = "None";
            string ParityDefault = "None";
            string RtsDtrEnabledDefault = "false";

            string GuideRateProfileNameDefault = "10.077939"; //67% of sidereal rate
            string PrecisionDefault = "Unchanged";

            string ParkedBehaviourDefault = "No Coordinates";
            string ParkedAltDefault = "0";
            string ParkedAzimuthDefault = "180";

            Mock<IProfileWrapper> profileWrapperMock = new Mock<IProfileWrapper>();
            profileWrapperMock.SetupAllProperties();

            profileWrapperMock.Setup(x => x.GetValue(DriverId, "Trace Level", string.Empty, TraceStateDefault))
                .Returns(TraceStateDefault);
            profileWrapperMock.Setup(x => x.GetValue(DriverId, "COM Port", string.Empty, ComPortDefault))
                .Returns(ComPortDefault);
            profileWrapperMock
                .Setup(x => x.GetValue(DriverId, "Guide Rate Arc Seconds Per Second", string.Empty,
                    GuideRateProfileNameDefault)).Returns(GuideRateProfileNameDefault);
            profileWrapperMock.Setup(x => x.GetValue(DriverId, "Precision", string.Empty, PrecisionDefault))
                .Returns(PrecisionDefault);
            profileWrapperMock.Setup(x =>
               x.GetValue(DriverId, "Speed", string.Empty, SpeedDefault))
                   .Returns(() => SpeedDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Data Bits", string.Empty, DataBitsDefault))
                .Returns(() => DataBitsDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Stop Bits", string.Empty, StopBitsDefault))
                .Returns(() => StopBitsDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Hand Shake", string.Empty, HandshakeDefault))
                .Returns(() => HandshakeDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parity", string.Empty, ParityDefault))
                .Returns(() => ParityDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Rts / Dtr", string.Empty, RtsDtrEnabledDefault))
                .Returns(() => RtsDtrEnabledDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parked Behaviour", string.Empty, ParkedBehaviourDefault))
                .Returns(() => ParityDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parked Altitude", string.Empty, ParkedAltDefault))
                .Returns(() => ParkedAltDefault);
            profileWrapperMock.Setup(x =>
                    x.GetValue(DriverId, "Parked Azimuth", string.Empty, ParkedAzimuthDefault))
                .Returns(() => ParkedAzimuthDefault);

            Mock<IProfileFactory> profileFactoryMock = new Mock<IProfileFactory>();
            profileFactoryMock.Setup(x => x.Create()).Returns(profileWrapperMock.Object);

            SharedResources.ProfileFactory = profileFactoryMock.Object;

            string serialPortReturn = string.Empty;

            _serialMock.Setup(x => x.Transmit("#:GVP#")).Callback(() => { serialPortReturn = TelescopeList.Autostar497; });
            _serialMock.Setup(x => x.Transmit("#:GVN#")).Callback(() => { serialPortReturn = TelescopeList.Autostar497_43Eg; });
            _serialMock.Setup(x => x.Transmit("#:GG#")).Callback(() => { serialPortReturn = ""; });
            _serialMock.Setup(x => x.ReceiveTerminated("#")).Returns(() => serialPortReturn);

            var result = Assert.Throws<Exception>(() =>
            {
                SharedResources.Connect(deviceId, string.Empty, _traceLoggerMock.Object);
            });
            Assert.That(result.Message, Is.EqualTo("Unable to decode response from the telescope, This is likely a hardware serial communications error."));

            _traceLoggerMock.Verify( x => x.LogIssue("Connect", "Unable to decode response from the telescope, This is likely a hardware serial communications error."), Times.Once);
        }

        [Test]
        public void CheckIsParkedIsFalseByDefault() => Assert.That(SharedResources.IsParked, Is.False);

        [Test]
        public void CheckParkedPositionIsNullByDefault() => Assert.That(SharedResources.ParkedPosition, Is.Null);

        [Test]
        public void CheckIsLongFormatIsFalseByDefault() => Assert.That(SharedResources.IsLongFormat, Is.False);

        [Test]
        public void CheckMovingPrimaryIsFalseBydefault() => Assert.That(SharedResources.MovingPrimary, Is.False);

        [Test]
        public void CheckMovingSecondaryIsFalseBydefault() => Assert.That(SharedResources.MovingSecondary, Is.False);

        [Test]
        public void CheckSideOfPierIsUnknownByDefault() => Assert.That(SharedResources.SideOfPier, Is.EqualTo(PierSide.pierUnknown));

        [Test]
        public void CheckSlewSettleTimeIsZeroByDefault() => Assert.That(SharedResources.SlewSettleTime, Is.EqualTo((short)0));

        [Test]
        public void CheckEarliestNonNonSlewingTimeIsMinValueByDefault() => Assert.That(SharedResources.EarliestNonSlewingTime, Is.EqualTo(DateTime.MinValue));

        [Test]
        public void CheckTargetDeclinationIsNullByDefault() => Assert.That(SharedResources.TargetDeclination.HasValue, Is.False);

        [Test]
        public void CheckTargetRightAscensionIsNullByDefault() => Assert.That(SharedResources.TargetRightAscension.HasValue, Is.False);

        [Test]
        public void CheckIsTargetCoordinateInitRequired() => Assert.That(SharedResources.IsTargetCoordinateInitRequired, Is.True);

        [Test]
        public void CheckIsGuiding() => Assert.That(SharedResources.IsGuiding, Is.False);
    }
}

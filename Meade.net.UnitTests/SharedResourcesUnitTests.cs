
using System;
using System.Globalization;
using System.Runtime.Remoting.Contexts;
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
            Assert.That(SharedResources.SharedSerial,Is.EqualTo(_serialMock.Object));
        }

        [Test]
        public void SendBlind_WhenCalled_Then_ClearsBuffersAndSendsMessage()
        {
            var expectedMessage = "Test";

            SharedResources.SendBlind(expectedMessage);

            _serialMock.Verify(x=> x.ClearBuffers(), Times.Once);
            _serialMock.Verify(x=>x.Transmit(expectedMessage), Times.Once);
        }

        [Test]
        public void SendChar_WhenCalled_ThenSendsMessageAndReadsExpectedNumberOfCharacters()
        {
            var expectedMessage = "Test";
            var expectedResult = "A";

            _serialMock.Setup(x => x.ReceiveCounted(1)).Returns(expectedResult);

            var result = SharedResources.SendChar(expectedMessage);

            _serialMock.Verify(x => x.ClearBuffers(), Times.Once);
            _serialMock.Verify(x => x.Transmit(expectedMessage), Times.Once);
            _serialMock.Verify(x => x.ReceiveCounted(1), Times.Once);
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void SendString_WhenCalled_ThenSendsMessageAndReadsResultUntilTerminatorFound()
        {
            var expectedMessage = "Test";
            var expectedResult = "TestMessage#";

            _serialMock.Setup(x => x.ReceiveTerminated("#")).Returns(expectedResult);

            var result = SharedResources.SendString(expectedMessage);

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
            profileWrapperMock.Verify(x => x.WriteValue(DriverId, "Guide Rate Arc Seconds Per Second", profileProperties.GuideRateArcSecondsPerSecond.ToString(CultureInfo.CurrentCulture)), Times.Once);
            profileWrapperMock.Verify(x => x.WriteValue(DriverId, "Precision", profileProperties.Precision), Times.Once);
            profileWrapperMock.Verify(x => x.WriteValue(DriverId, "Guiding Style", profileProperties.GuidingStyle), Times.Once);
        }

        [Test]
        public void ReadProfile_WhenCalled_ReturnsExpectedDefaultValues()
        {
            string DriverId = "ASCOM.MeadeGeneric.Telescope";

            string ComPortDefault = "COM1";
            string TraceStateDefault = "false";
            string GuideRateProfileNameDefault = "10.077939"; //67% of sidereal rate
            string PrecisionDefault = "Unchanged";
            string GuidingStyleDefault = "Auto";

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
            profileWrapperMock.Setup(x => x.GetValue(DriverId, "Guiding Style", string.Empty, GuidingStyleDefault))
                .Returns(GuidingStyleDefault);

            IProfileWrapper profeWrapper = profileWrapperMock.Object;

            Mock<IProfileFactory> profileFactoryMock = new Mock<IProfileFactory>();
            profileFactoryMock.Setup(x => x.Create()).Returns(profileWrapperMock.Object);

            SharedResources.ProfileFactory = profileFactoryMock.Object;

            var profileProperties = SharedResources.ReadProfile();

            Assert.That(profeWrapper.DeviceType, Is.EqualTo("Telescope"));
            Assert.That(profileProperties.ComPort, Is.EqualTo(ComPortDefault));
            Assert.That(profileProperties.GuideRateArcSecondsPerSecond,
                Is.EqualTo(double.Parse(GuideRateProfileNameDefault)));
            Assert.That(profileProperties.TraceLogger, Is.EqualTo(bool.Parse(TraceStateDefault)));
            Assert.That(profileProperties.Precision, Is.EqualTo(PrecisionDefault));
            Assert.That(profileProperties.GuidingStyle, Is.EqualTo(GuidingStyleDefault));
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

            string driverDriverId = "ASCOM.MeadeGeneric.Telescope";

            string ComPortDefault = "COM1";
            string TraceStateDefault = "false";
            string GuideRateProfileNameDefault = "10.077939"; //67% of sidereal rate
            string PrecisionDefault = "Unchanged";

            Mock<IProfileWrapper> profileWrapperMock = new Mock<IProfileWrapper>();
            profileWrapperMock.SetupAllProperties();

            profileWrapperMock.Setup(x => x.GetValue(driverDriverId, "Trace Level", string.Empty, TraceStateDefault))
                .Returns(TraceStateDefault);
            profileWrapperMock.Setup(x => x.GetValue(driverDriverId, "COM Port", string.Empty, ComPortDefault))
                .Returns(ComPortDefault);
            profileWrapperMock
                .Setup(x => x.GetValue(driverDriverId, "Guide Rate Arc Seconds Per Second", string.Empty,
                    GuideRateProfileNameDefault)).Returns(GuideRateProfileNameDefault);
            profileWrapperMock.Setup(x => x.GetValue(driverDriverId, "Precision", string.Empty, PrecisionDefault))
                .Returns(PrecisionDefault);

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

            string driverDriverId = "ASCOM.MeadeGeneric.Telescope";

            string ComPortDefault = "COM1";
            string TraceStateDefault = "false";
            string GuideRateProfileNameDefault = "10.077939"; //67% of sidereal rate
            string PrecisionDefault = "Unchanged";

            Mock<IProfileWrapper> profileWrapperMock = new Mock<IProfileWrapper>();
            profileWrapperMock.SetupAllProperties();

            profileWrapperMock.Setup(x => x.GetValue(driverDriverId, "Trace Level", string.Empty, TraceStateDefault))
                .Returns(TraceStateDefault);
            profileWrapperMock.Setup(x => x.GetValue(driverDriverId, "COM Port", string.Empty, ComPortDefault))
                .Returns(ComPortDefault);
            profileWrapperMock
                .Setup(x => x.GetValue(driverDriverId, "Guide Rate Arc Seconds Per Second", string.Empty,
                    GuideRateProfileNameDefault)).Returns(GuideRateProfileNameDefault);
            profileWrapperMock.Setup(x => x.GetValue(driverDriverId, "Precision", string.Empty, PrecisionDefault))
                .Returns(PrecisionDefault);

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

            string driverDriverId = "ASCOM.MeadeGeneric.Telescope";

            string ComPortDefault = "COM1";
            string TraceStateDefault = "false";
            string GuideRateProfileNameDefault = "10.077939"; //67% of sidereal rate
            string PrecisionDefault = "Unchanged";

            Mock<IProfileWrapper> profileWrapperMock = new Mock<IProfileWrapper>();
            profileWrapperMock.SetupAllProperties();

            profileWrapperMock.Setup(x => x.GetValue(driverDriverId, "Trace Level", string.Empty, TraceStateDefault))
                .Returns(TraceStateDefault);
            profileWrapperMock.Setup(x => x.GetValue(driverDriverId, "COM Port", string.Empty, ComPortDefault))
                .Returns(ComPortDefault);
            profileWrapperMock
                .Setup(x => x.GetValue(driverDriverId, "Guide Rate Arc Seconds Per Second", string.Empty,
                    GuideRateProfileNameDefault)).Returns(GuideRateProfileNameDefault);
            profileWrapperMock.Setup(x => x.GetValue(driverDriverId, "Precision", string.Empty, PrecisionDefault))
                .Returns(PrecisionDefault);

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

            string driverDriverId = "ASCOM.MeadeGeneric.Telescope";

            string ComPortDefault = "COM1";
            string TraceStateDefault = "false";
            string GuideRateProfileNameDefault = "10.077939"; //67% of sidereal rate
            string PrecisionDefault = "Unchanged";

            Mock<IProfileWrapper> profileWrapperMock = new Mock<IProfileWrapper>();
            profileWrapperMock.SetupAllProperties();

            profileWrapperMock.Setup(x => x.GetValue(driverDriverId, "Trace Level", string.Empty, TraceStateDefault))
                .Returns(TraceStateDefault);
            profileWrapperMock.Setup(x => x.GetValue(driverDriverId, "COM Port", string.Empty, ComPortDefault))
                .Returns(ComPortDefault);
            profileWrapperMock
                .Setup(x => x.GetValue(driverDriverId, "Guide Rate Arc Seconds Per Second", string.Empty,
                    GuideRateProfileNameDefault)).Returns(GuideRateProfileNameDefault);
            profileWrapperMock.Setup(x => x.GetValue(driverDriverId, "Precision", string.Empty, PrecisionDefault))
                .Returns(PrecisionDefault);

            Mock<IProfileFactory> profileFactoryMock = new Mock<IProfileFactory>();
            profileFactoryMock.Setup(x => x.Create()).Returns(profileWrapperMock.Object);

            SharedResources.ProfileFactory = profileFactoryMock.Object;

            string serialPortReturn = string.Empty;

            _serialMock.Setup(x => x.Transmit("#:GVP#")).Callback(() => { serialPortReturn = TelescopeList.Autostar497; });
            _serialMock.Setup(x => x.Transmit("#:GVN#")).Callback(() => { serialPortReturn = TelescopeList.Autostar497_43Eg; });
            _serialMock.Setup(x => x.Transmit("#:GG#")).Callback(() => { serialPortReturn = ""; });
            _serialMock.Setup(x => x.ReceiveTerminated("#")).Returns(() => serialPortReturn);

            var result = Assert.Throws<FormatException>(() => { var connectionResult = SharedResources.Connect(deviceId, string.Empty, _traceLoggerMock.Object); });
            Assert.That(result.Message, Is.EqualTo("Input string was not in a correct format."));

            _traceLoggerMock.Verify( x => x.LogIssue("Connect", "Unable to decode response from the telescope, This is likely a hardware serial communications error."), Times.Once);
        }
    }
}

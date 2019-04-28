using System.Collections.Generic;
using ASCOM;
using ASCOM.MeadeAutostar497.Controller;
using ASCOM.Utilities;
using ASCOM.Utilities.Interfaces;
using Moq;
using NUnit.Framework;

namespace MeadeAutostar497.UnitTests
{
    [TestFixture]
    public class TelescopeControllerUnitTests
    {
        private Mock<ISerial> serialMock;

        private readonly List<string> _availableComPorts = new List<string> { "COM1", "COM2", "COM3" };
        private TelescopeController _telescopeController;

        private string transmittedString;
        private string stringToRecieve;

        [SetUp]
        public void Setup()
        {
            transmittedString = string.Empty;
            stringToRecieve = string.Empty;

            serialMock = new Mock<ISerial>();
            serialMock.SetupAllProperties();

            serialMock.Setup(x => x.AvailableComPorts).Returns( () => _availableComPorts.ToArray());
            serialMock.Setup(X => X.Transmit(It.IsAny<string>())).Callback<string>(str => { transmittedString = str; });
            serialMock.Setup(X => X.Receive()).Returns(() => stringToRecieve);

            _telescopeController = TelescopeController.Instance;
            _telescopeController.SerialPort = serialMock.Object;
        }

        [TearDown]
        public void TearDown()
        {
            _telescopeController.Connected = false;
            _telescopeController.Port = "COM1";            
        }

        [Test]
        public void ImplementsExpectedInterfaces()
        {
            Assert.That(_telescopeController, Is.Not.Null); 
            Assert.That(_telescopeController, Is.AssignableTo<ITelescopeController>());
        }

        [Test]
        public void NotConnectedByDefault()
        {
            Assert.That(_telescopeController.Connected, Is.False);
        }

        [Test]
        public void ConnectedCanBeSetTrue()
        {
            stringToRecieve = "test#";

            _telescopeController.Connected = true;
            Assert.That(_telescopeController.Connected, Is.True);
        }

        [Test]
        public void EnsureThatTheSerialCommunicationsAreSetCorrectly()
        {
            Assert.That(serialMock.Object.Connected, Is.False);

            stringToRecieve = "test#";

            _telescopeController.Connected = true;
            Assert.That(_telescopeController.Connected, Is.True);

            Assert.That(serialMock.Object.DTREnable, Is.False);
            Assert.That(serialMock.Object.RTSEnable, Is.False);
            Assert.That(serialMock.Object.Speed, Is.EqualTo(SerialSpeed.ps9600));
            Assert.That(serialMock.Object.DataBits, Is.EqualTo(8));
            Assert.That(serialMock.Object.StopBits, Is.EqualTo(SerialStopBits.One));
            Assert.That(serialMock.Object.Parity, Is.EqualTo(SerialParity.None));
            Assert.That(serialMock.Object.PortName, Is.EqualTo(_telescopeController.Port));
            Assert.That(serialMock.Object.Connected, Is.True);
        }

        [Test]
        public void WhenOpensComPortToNonAutostarThrowException()
        {
            Assert.That(serialMock.Object.Connected, Is.False);
            var exception = Assert.Throws<InvalidOperationException>(() => { _telescopeController.Connected = true; });

            Assert.That(exception.Message, Is.EqualTo("Failed to communicate with telescope."));

            Assert.That(_telescopeController.Connected, Is.False);
        }

        [Test]
        public void CannotChangeSerialPortObjectWhenConnected()
        {
            stringToRecieve = "test#";

            _telescopeController.Connected = true;

            Mock<ISerial> newSerialMock = new Mock<ISerial>();

            var exception = Assert.Throws<InvalidOperationException>( () => { _telescopeController.SerialPort = newSerialMock.Object; });

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Message, Is.EqualTo("Please disconnect before changing the serial engine."));
        }

        [Test]
        public void PortIsSetToCom1ByDefault()
        {
            Assert.That(_telescopeController.Port, Is.EqualTo("COM1"));
        }

        [Test]
        public void SettingPortToValidPortAllowed()
        {   
            _telescopeController.Port = "COM2";

            Assert.That(_telescopeController.Port, Is.EqualTo("COM2"));
        }

        [Test]
        public void SettingPortToValidPortWhenConnectedFails()
        {
            stringToRecieve = "test#";

            _telescopeController.Connected = true;
            var exception = Assert.Throws<InvalidOperationException>( () => _telescopeController.Port = "COM2");

            Assert.That(exception.Message, Is.EqualTo("Please disconnect from the scope before changing port."));

            Assert.That(_telescopeController.Port, Is.EqualTo("COM1")); //port hasn't changed
        }

        [Test]
        public void SettingPortToInavalidPortFails()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => _telescopeController.Port = "COM5");

            Assert.That(exception.Message, Is.EqualTo("Unable to select port COM5 as it does not exist."));

            Assert.That(_telescopeController.Port, Is.EqualTo("COM1")); //port hasn't changed
        }
    }
}

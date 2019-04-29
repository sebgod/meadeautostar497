using System.Collections.Generic;
using System.IO.Ports;
using ASCOM;
using ASCOM.MeadeAutostar497.Controller;
using ASCOM.Utilities;
using Moq;
using NUnit.Framework;

namespace MeadeAutostar497.UnitTests
{
    [TestFixture]
    public class TelescopeControllerUnitTests
    {
        private Mock<ISerialProcessor> serialMock;

        private readonly List<string> _availableComPorts = new List<string> { "COM1", "COM2", "COM3" };
        private TelescopeController _telescopeController;

        private string _stringToRecieve = string.Empty;
        private bool _isConnected = false;

        [SetUp]
        public void Setup()
        {
            _stringToRecieve = string.Empty;
            _isConnected = false;

            serialMock = new Mock<ISerialProcessor>();
            serialMock.SetupAllProperties();
            serialMock.Setup(x => x.GetPortNames()).Returns( () => _availableComPorts.ToArray());
            serialMock.Setup(x => x.CommandTerminated(It.IsAny<string>(), It.IsAny<string>())).Returns(() => _stringToRecieve);
            serialMock.Setup(x => x.IsOpen).Returns(() => _isConnected);

            _telescopeController = TelescopeController.Instance;
            _telescopeController.SerialPort = serialMock.Object;
        }

        [TearDown]
        public void TearDown()
        {
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
            _stringToRecieve = "test#";
            _isConnected = true;

            _telescopeController.Connected = true;
            Assert.That(_telescopeController.Connected, Is.True);
        }

        [Test]
        public void EnsureThatTheSerialCommunicationsAreSetCorrectly()
        {
            Assert.That(serialMock.Object.IsOpen, Is.False);

            _stringToRecieve = "test#";
            _telescopeController.Connected = true;
            _isConnected = true;
            Assert.That(_telescopeController.Connected, Is.True);

            serialMock.Verify(x => x.Open(), Times.Once);

            Assert.That(serialMock.Object.DtrEnable, Is.False);
            Assert.That(serialMock.Object.RtsEnable, Is.False);
            Assert.That(serialMock.Object.BaudRate, Is.EqualTo(9600));
            Assert.That(serialMock.Object.DataBits, Is.EqualTo(8));
            Assert.That(serialMock.Object.StopBits, Is.EqualTo(StopBits.One));
            Assert.That(serialMock.Object.Parity, Is.EqualTo(Parity.None));
            Assert.That(serialMock.Object.PortName, Is.EqualTo(_telescopeController.Port));
            Assert.That(serialMock.Object.IsOpen, Is.True);
            
        }

        [Test]
        public void WhenOpensComPortToNonAutostarThrowException()
        {
            Assert.That(serialMock.Object.IsOpen, Is.False);
            var exception = Assert.Throws<InvalidOperationException>(() => { _telescopeController.Connected = true; });

            Assert.That(exception.Message, Is.EqualTo("Failed to communicate with telescope."));

            Assert.That(_telescopeController.Connected, Is.False);
        }

        [Test]
        public void CannotChangeSerialPortObjectWhenConnected()
        {
            _stringToRecieve = "test#";
            _isConnected = true;

            _telescopeController.Connected = true;

            Mock<ISerialProcessor> newSerialMock = new Mock<ISerialProcessor>();

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
            _stringToRecieve = "test#";
            _isConnected = true;

            _telescopeController.Connected = true;
            var exception = Assert.Throws<InvalidOperationException>( () => _telescopeController.Port = "COM2");

            Assert.That(exception.Message, Is.EqualTo("Please disconnect from the scope before changing port."));

            Assert.That(_telescopeController.Port, Is.EqualTo("COM1")); //port hasn't changed
        }

        [Test]
        public void SettingPortToInvalidPortFails()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => _telescopeController.Port = "COM5");

            Assert.That(exception.Message, Is.EqualTo("Unable to select port COM5 as it does not exist."));

            Assert.That(_telescopeController.Port, Is.EqualTo("COM1")); //port hasn't changed
        }

        [Test]
        public void AbortSlewWorks()
        {
            _isConnected = true;

            _telescopeController.Connected = true;

            _telescopeController.AbortSlew();

            serialMock.Verify(x => x.Command("#:Q#"), Times.Once);
        }
    }
}

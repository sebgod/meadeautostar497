using System;
using System.Reflection;
using ASCOM;
using ASCOM.DeviceInterface;
using ASCOM.Meade.net;
using ASCOM.Meade.net.Wrapper;
using ASCOM.Utilities.Interfaces;
using Moq;
using NUnit.Framework;

namespace Meade.net.Focuser.UnitTests
{
    [TestFixture]
    public class FocuserUnitTests
    {
        private Mock<IUtil> _utilMock;
        private Mock<ISharedResourcesWrapper> _sharedResourcesWrapperMock;

        private ProfileProperties _profileProperties;

        private ASCOM.Meade.net.Focuser _focuser;

        [SetUp]
        public void Setup()
        {
            _profileProperties = new ProfileProperties
            {
                TraceLogger = false,
                ComPort = "TestCom1",
                GuideRateArcSecondsPerSecond = 1.23,
                Precision = "Unchanged"
            };

            _utilMock = new Mock<IUtil>();

            _sharedResourcesWrapperMock = new Mock<ISharedResourcesWrapper>();

            _sharedResourcesWrapperMock.Setup(x => x.Lock(It.IsAny<Action>())).Callback<Action>(action => { action(); });

            _sharedResourcesWrapperMock.Setup(x => x.ReadProfile()).Returns(() => _profileProperties);

            _focuser = new ASCOM.Meade.net.Focuser(_utilMock.Object, _sharedResourcesWrapperMock.Object);
        }

        private void ConnectFocuser()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => TelescopeList.Autostar497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => TelescopeList.Autostar497_31Ee);
            _focuser.Connected = true;
        }

        [Test]
        public void CheckThatClassCreatedProperly()
        {
            Assert.That(_focuser, Is.Not.Null);
        }

        [Test]
        public void NotConnectedByDefault()
        {
            Assert.That(_focuser.Connected, Is.False);
        }

        [Test]
        public void SetupDialog()
        {
            _sharedResourcesWrapperMock.Verify(x => x.ReadProfile(), Times.Once);

            _focuser.SetupDialog();

            _sharedResourcesWrapperMock.Verify(x => x.SetupDialog(), Times.Once);
            _sharedResourcesWrapperMock.Verify(x => x.ReadProfile(), Times.Exactly(2));
        }

        [Test]
        public void SupportedActions()
        {
            var supportedActions = _focuser.SupportedActions;

            Assert.That(supportedActions, Is.Not.Null);
            Assert.That(supportedActions.Count, Is.EqualTo(0));
        }

        [Test]
        public void Action_WhenNotConnected_ThrowsNotConnectedException()
        {
            var actionName = "Action";

            Assert.Throws<ActionNotImplementedException>(() =>
            {
                var actualResult = _focuser.Action(actionName, string.Empty);
                Assert.Fail($"{actualResult} should not have a value");
            });
        }

        [Test]
        public void CommandBlind_WhenNotConnected_ThenThrowsNotConnectedException()
        {
            string expectedMessage = "test blind Message";
            var exception = Assert.Throws<NotConnectedException>(() => { _focuser.CommandBlind(expectedMessage, true); });

            Assert.That(exception.Message, Is.EqualTo("Not connected to focuser when trying to execute: CommandBlind"));
        }

        [Test]
        public void CommandBlind_WhenConnected_ThenSendsExpectedMessage()
        {
            string expectedMessage = "test blind Message";

            ConnectFocuser();

            _focuser.CommandBlind(expectedMessage, true);

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(expectedMessage), Times.Once);
        }

        [Test]
        public void CommandBool_WhenNotConnected_ThenThrowsNotConnectedException()
        {
            string expectedMessage = "test blind Message";
            var exception = Assert.Throws<NotConnectedException>(() => { _focuser.CommandBool(expectedMessage, true); });

            Assert.That(exception.Message, Is.EqualTo("Not connected to focuser when trying to execute: CommandBool"));
        }

        [Test]
        public void CommandBool_WhenConnected_ThenSendsExpectedMessage()
        {
            string expectedMessage = "test blind Message";

            ConnectFocuser();

            var exception = Assert.Throws<MethodNotImplementedException>(() => { _focuser.CommandBool(expectedMessage, true); });

            Assert.That(exception.Message, Is.EqualTo("Method CommandBool is not implemented in this driver."));
        }

        [Test]
        public void CommandString_WhenNotConnected_ThenThrowsNotConnectedException()
        {
            string expectedMessage = "test blind Message";
            var exception = Assert.Throws<NotConnectedException>(() => { _focuser.CommandString(expectedMessage, true); });

            Assert.That(exception.Message, Is.EqualTo("Not connected to focuser when trying to execute: CommandString"));
        }

        [Test]
        public void CommandString_WhenConnected_ThenSendsExpectedMessage()
        {
            string expectedMessage = "expected result message";
            string sendMessage = "test blind Message";

            ConnectFocuser();

            _sharedResourcesWrapperMock.Setup(x => x.SendString(sendMessage)).Returns(() => expectedMessage);

            var actualMessage = _focuser.CommandString(sendMessage, true);

            _sharedResourcesWrapperMock.Verify(x => x.SendString(sendMessage), Times.Once);
            Assert.That(actualMessage, Is.EqualTo(expectedMessage));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Connected_Get_ReturnsExpectedValue(bool expectedConnected)
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => TelescopeList.Autostar497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => TelescopeList.Autostar497_31Ee);
            _focuser.Connected = expectedConnected;

            Assert.That(_focuser.Connected, Is.EqualTo(expectedConnected));
        }

        [Test]
        public void Connected_Set_WhenConnecting_Then_ConnectsToSerialDevice()
        {
            var productName = "LX2001";
            var firmware = string.Empty;

            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(productName);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(firmware);
            _focuser.Connected = true;

            _sharedResourcesWrapperMock.Verify(x => x.Connect("Serial", It.IsAny<string>(), It.IsAny<ITraceLogger>()), Times.Once);
        }


        [Test]
        public void Connected_Set_SettingTrueWhenTrue_ThenDoesNothing()
        {
            ConnectFocuser();
            _sharedResourcesWrapperMock.Verify(x => x.Connect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ITraceLogger>()), Times.Once);

            //act
            _focuser.Connected = true;

            //assert
            _sharedResourcesWrapperMock.Verify(x => x.Connect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ITraceLogger>()), Times.Once);
        }

        [Test]
        public void Connected_Set_SettingFalseWhenTrue_ThenDisconnects()
        {
            ConnectFocuser();
            _sharedResourcesWrapperMock.Verify(x => x.Connect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ITraceLogger>()), Times.Once);

            //act
            _focuser.Connected = false;

            //assert
            _sharedResourcesWrapperMock.Verify(x => x.Disconnect(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        //Commented out for now as the catch after connect is currently unreachable code.
        //[Test]
        //public void Connected_Set_WhenFailsToConnect_ThenDisconnects()
        //{
        //    _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => TelescopeList.Autostar497);
        //    _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => TelescopeList.Autostar497_31Ee);

        //    _sharedResourcesWrapperMock.Setup(x => x.SendString(It.IsAny<string>())).Throws(new Exception("TestFailed"));

        //    //act
        //    _focuser.Connected = true;

        //    //assert
        //    _sharedResourcesWrapperMock.Verify(x => x.Disconnect(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        //}

        [Test]
        public void Description_Get()
        {
            var expectedDescription = "Meade Generic";

            var description = _focuser.Description;

            Assert.That(description, Is.EqualTo(expectedDescription));
        }

        [Test]
        public void DriverVersion_Get()
        {
            Version version = Assembly.GetAssembly(typeof(ASCOM.Meade.net.Focuser)).GetName().Version;

            string exptectedDriverInfo = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

            var driverVersion = _focuser.DriverVersion;

            Assert.That(driverVersion, Is.EqualTo(exptectedDriverInfo));
        }

        [Test]
        public void DriverInfo_Get()
        {
            string exptectedDriverInfo = $"{_focuser.Description} .net driver. Version: {_focuser.DriverVersion}";

            var driverInfo = _focuser.DriverInfo;

            Assert.That(driverInfo, Is.EqualTo(exptectedDriverInfo));
        }

        [Test]
        public void InterfaceVersion_Get()
        {
            var interfaceVersion = _focuser.InterfaceVersion;
            Assert.That(interfaceVersion, Is.EqualTo(3));

            Assert.That(_focuser, Is.AssignableTo<IFocuserV3>());
        }

        [Test]
        public void Name_Get()
        {
            string expectedName = "Meade Generic";

            var name = _focuser.Name;

            Assert.That(name, Is.EqualTo(expectedName));
        }

        [Test]
        public void Absolute_Get_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() =>
            {
                var result = _focuser.Absolute;
                Assert.Fail($"{result} should not have a value");
            });
            Assert.That(exception.Message, Is.EqualTo("Not connected to focuser when trying to execute: Absolute Get"));
        }

        [Test]
        public void Absolute_Get_WhenConnected_ThenReturnsFalse()
        {
            ConnectFocuser();
            var result = _focuser.Absolute;

            Assert.That(result, Is.False);
        }

        [Test]
        public void Halt_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { _focuser.Halt(); });
            Assert.That(exception.Message, Is.EqualTo("Not connected to focuser when trying to execute: Halt"));
        }

        [Test]
        public void Halt_WhenConnected_ThenSendsHaltCommand()
        {
            ConnectFocuser();

            _focuser.Halt();

            _sharedResourcesWrapperMock.Verify( x => x.SendBlind(":FQ#"), Times.AtLeastOnce);
        }

        [Test]
        public void IsMoving_WhenCalled_ThenReturnsFalse()
        {
            ConnectFocuser();

            var result = _focuser.IsMoving;

            Assert.That(result, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Link_Get_ReturnsSameValueAsConnected( bool connected)
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => TelescopeList.Autostar497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => TelescopeList.Autostar497_31Ee);
            _focuser.Connected = connected;

            Assert.That( _focuser.Link, Is.EqualTo(connected));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Link_Set_WhenSet_ThenSetsConnectedState(bool connected)
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => TelescopeList.Autostar497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => TelescopeList.Autostar497_31Ee);
            _focuser.Link = connected;

            Assert.That(_focuser.Link, Is.EqualTo(connected));
        }

        [Test]
        public void MaxIncrement_WhenCalled_ThenReturnsExpectedValue()
        {
            var result = _focuser.MaxIncrement;

            Assert.That(result, Is.EqualTo(7000));
        }

        [Test]
        public void MaxStep_WhenCalled_ThenReturnsExpectedValue()
        {
            var result = _focuser.MaxStep;

            Assert.That(result, Is.EqualTo(7000));
        }

        [Test]
        public void Move_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { _focuser.Move(0); });
            Assert.That(exception.Message, Is.EqualTo("Not connected to focuser when trying to execute: Move"));
        }

        [TestCase(-7001)]
        [TestCase(7001)]
        public void Move_WhenLargerThanMaxIncrement_ThenThrowsException(int position)
        {
            ConnectFocuser();

            var exception = Assert.Throws<InvalidValueException>(() => { _focuser.Move(position); });
            Assert.That(exception.Message, Is.EqualTo($"position out of range -{_focuser.MaxIncrement} < {position} < {_focuser.MaxIncrement}"));
        }

        [Test]
        public void Move_WhenIncrementIs0_ThenDoesNothing()
        {
            ConnectFocuser();

            _focuser.Move(0);

            _utilMock.Verify( x => x.WaitForMilliseconds(It.IsAny<int>()), Times.Never);
        }

        [TestCase(200)]
        [TestCase(-200)]
        public void Move_WhenIncrementIsNot0_ThenMovesFocuserAndStopsFocuser( int position)
        {
            _profileProperties.BacklashCompensation = 0;

            ConnectFocuser();

            _focuser.Move(position);

            if (position < 0)
            {
                _sharedResourcesWrapperMock.Verify( x => x.SendBlind("#:F-#"), Times.Once);
                _sharedResourcesWrapperMock.Verify(x => x.SendBlind("#:F+#"), Times.Never);
            }
            else
            {
                _sharedResourcesWrapperMock.Verify(x => x.SendBlind("#:F-#"), Times.Never);
                _sharedResourcesWrapperMock.Verify(x => x.SendBlind("#:F+#"), Times.Once);
            }

            _sharedResourcesWrapperMock.Verify( x => x.Lock(It.IsAny<Action>()), Times.Once);

            _utilMock.Verify(x => x.WaitForMilliseconds(Math.Abs(position)), Times.Once);
            _utilMock.Verify(x => x.WaitForMilliseconds(Math.Abs(_profileProperties.BacklashCompensation)), Times.Never);
            _utilMock.Verify(x => x.WaitForMilliseconds(100), Times.Once());
        }

        [TestCase(200)]
        [TestCase(-200)]
        public void Move_WhenIncrementIsNot0_ThenMovesFocuserAndStopsFocuserWithBacklashCompensation(int position)
        {
            _profileProperties.BacklashCompensation = 3000;

            ConnectFocuser();

            _focuser.Move(position);

            if (position < 0)
            {
                _sharedResourcesWrapperMock.Verify(x => x.SendBlind("#:F-#"), Times.Once);
                _sharedResourcesWrapperMock.Verify(x => x.SendBlind("#:F+#"), Times.Never);
                _utilMock.Verify(x => x.WaitForMilliseconds(Math.Abs(position)), Times.Once);
                _utilMock.Verify(x => x.WaitForMilliseconds(Math.Abs(_profileProperties.BacklashCompensation)), Times.Never);
                _utilMock.Verify(x => x.WaitForMilliseconds(100), Times.Exactly(1));
            }
            else
            {
                _sharedResourcesWrapperMock.Verify(x => x.SendBlind("#:F-#"), Times.Once);
                _sharedResourcesWrapperMock.Verify(x => x.SendBlind("#:F+#"), Times.Once);
                _utilMock.Verify(x => x.WaitForMilliseconds(Math.Abs(position) + _profileProperties.BacklashCompensation), Times.Once);
                _utilMock.Verify(x => x.WaitForMilliseconds(_profileProperties.BacklashCompensation), Times.Once);
                _utilMock.Verify(x => x.WaitForMilliseconds(100), Times.Exactly(2));
            }

            _sharedResourcesWrapperMock.Verify(x => x.Lock(It.IsAny<Action>()), Times.Once);
        }

        [Test]
        public void Position_WhenCalled_ThenThrowsException()
        {
            var exception = Assert.Throws<PropertyNotImplementedException>(() =>
            {
                var result = _focuser.Position;
                Assert.Fail($"{result} should not have a value");
            });
            Assert.That(exception.Message, Is.EqualTo("Property read Position is not implemented in this driver."));
        }

        [Test]
        public void StepSize_WhenCalled_ThenThrowsException()
        {
            var exception = Assert.Throws<PropertyNotImplementedException>(() =>
            {
                var result = _focuser.StepSize;
                Assert.Fail($"{result} should not have a value");
            });
            Assert.That(exception.Message, Is.EqualTo("Property read StepSize is not implemented in this driver."));
        }

        [Test]
        public void TempComp_WhenRead_ThenReturnsFalse()
        {
            var result = _focuser.TempComp;
            Assert.That(result, Is.False);
        }

        [Test]
        public void TempComp_WhenWrite_ThenThrowsException()
        {
            var exception = Assert.Throws<PropertyNotImplementedException>(() => { _focuser.TempComp = false; });
            Assert.That(exception.Message, Is.EqualTo("Property read TempComp is not implemented in this driver."));
        }

        [Test]
        public void TempCompAvailable_WhenRead_ThenReturnsFalse()
        {
            var result = _focuser.TempCompAvailable;
            Assert.That(result, Is.False);
        }

        [Test]
        public void Temperature_WhenCalled_ThenThrowsException()
        {
            var exception = Assert.Throws<PropertyNotImplementedException>(() =>
            {
                var result = _focuser.Temperature; 
                Assert.Fail($"{result} should not have a value");
            });
            Assert.That(exception.Message, Is.EqualTo("Property read Temperature is not implemented in this driver."));
        }
    }
}

﻿using System;
using System.Diagnostics.Eventing.Reader;
using ASCOM;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.DeviceInterface;
using ASCOM.Meade.net;
using ASCOM.Meade.net.AstroMaths;
using ASCOM.Meade.net.Wrapper;
using ASCOM.Utilities.Interfaces;
using Moq;
using NUnit.Framework;
using NotImplementedException = ASCOM.NotImplementedException;

namespace Meade.net.Telescope.UnitTests
{
    [TestFixture]
    public class TelescopeUnitTests
    {
        private ASCOM.Meade.net.Telescope _telescope;
        private Mock<IUtil> _utilMock;
        private Mock<IUtilExtra> _utilExtraMock;
        private Mock<IAstroUtils> _astroUtilsMock;
        private Mock<ISharedResourcesWrapper> _sharedResourcesWrapperMock;
        private Mock<IAstroMaths> _astroMathsMock;

        private ProfileProperties _profileProperties;

        [SetUp]
        public void Setup()
        {
            _profileProperties = new ProfileProperties();
            _profileProperties.TraceLogger = false;
            _profileProperties.ComPort = "TestCom1";

            _utilMock = new Mock<IUtil>();
            _utilExtraMock = new Mock<IUtilExtra>();
            _astroUtilsMock = new Mock<IAstroUtils>();

            _sharedResourcesWrapperMock = new Mock<ISharedResourcesWrapper>();
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GZ#")).Returns("DDD*MM’SS");
            _sharedResourcesWrapperMock.Setup(x => x.AUTOSTAR497).Returns(() => "AUTOSTAR");
            _sharedResourcesWrapperMock.Setup(x => x.AUTOSTAR497_31EE).Returns(() => "31Ee");
            _sharedResourcesWrapperMock.Setup(x => x.AUTOSTAR497_43EG) .Returns(() => "43Eg");

            _sharedResourcesWrapperMock.Setup(x => x.Lock(It.IsAny<Action>())).Callback<Action>(action => { action(); });
            
            _sharedResourcesWrapperMock.Setup(x => x.ReadProfile()).Returns(_profileProperties);

            _astroMathsMock = new Mock<IAstroMaths>();

            _telescope = new ASCOM.Meade.net.Telescope(_utilMock.Object, _utilExtraMock.Object, _astroUtilsMock.Object,
                _sharedResourcesWrapperMock.Object, _astroMathsMock.Object);
        }

        [Test]
        public void CheckThatClassCreatedProperly()
        {
            Assert.That(_telescope, Is.Not.Null);
        }

        [Test]
        public void NotConnectedByDefault()
        {
            Assert.That(_telescope.Connected, Is.False);
        }

        [Test]
        public void SetupDialog()
        {
            _sharedResourcesWrapperMock.Verify(x => x.ReadProfile(), Times.Once);

            _telescope.SetupDialog();

            _sharedResourcesWrapperMock.Verify(x => x.SetupDialog(), Times.Once);
            _sharedResourcesWrapperMock.Verify(x => x.ReadProfile(), Times.Exactly(2));
        }

        [Test]
        public void SupportedActions()
        {
            var supportedActions = _telescope.SupportedActions;

            Assert.That(supportedActions, Is.Not.Null);
            Assert.That(supportedActions.Count, Is.EqualTo(1));
            Assert.That(supportedActions.Contains("handbox"), Is.True);
        }

        [Test]
        public void Action_WhenNotConnected_ThrowsNotConnectedException()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);

            var exception = Assert.Throws<NotConnectedException>(() => { var actualResult = _telescope.Action(string.Empty, string.Empty); });
            Assert.That(exception.Message,Is.EqualTo("Not connected to telescope when trying to execute: Action"));
        }

        [Test]
        public void Action_Handbox_ReadDisplay()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);

            string expectedResult = "test result string";
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":ED#")).Returns(expectedResult);
            _telescope.Connected = true;

            

            var actualResult = _telescope.Action("handbox", "readdisplay");

            _sharedResourcesWrapperMock.Verify(x => x.SendString(":ED#"), Times.Once);
            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }

        [TestCase("enter", ":EK13#")]
        [TestCase("mode", ":EK9#")]
        [TestCase("longMode", ":EK11#")]
        [TestCase("goto", ":EK24#")]
        [TestCase("0", ":EK48#")]
        [TestCase("1", ":EK49#")]
        [TestCase("2", ":EK50#")]
        [TestCase("3", ":EK51#")]
        [TestCase("4", ":EK52#")]
        [TestCase("5", ":EK53#")]
        [TestCase("6", ":EK54#")]
        [TestCase("7", ":EK55#")]
        [TestCase("8", ":EK56#")]
        [TestCase("9", ":EK57#")]
        [TestCase("up", ":EK94#")]
        [TestCase("down", ":EK118#")]
        [TestCase("back", ":EK87#")]
        [TestCase("forward", ":EK69#")]
        [TestCase("?", ":EK63#")]
        public void Action_Handbox_blindCommands(string action, string expectedString)
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);

            _telescope.Connected = true;
            _telescope.Action("handbox", action);

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(expectedString), Times.Once);
        }

        [Test]
        public void Action_Handbox_nonExistantAction()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);
            _telescope.Connected = true;

            string actionName = "handbox";
            string actionParameters = "doesnotexist";
            var exception = Assert.Throws<ActionNotImplementedException>(() => { _telescope.Action(actionName, actionParameters); });

            Assert.That(exception.Message, Is.EqualTo($"Action {actionName}({actionParameters}) is not implemented in this driver is not implemented in this driver."));
        }

        [Test]
        public void Action_nonExistantAction()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);
            _telescope.Connected = true;

            string actionName = "doesnotexist";
            var exception = Assert.Throws<ActionNotImplementedException>(() => { _telescope.Action(actionName, string.Empty); });

            Assert.That(exception.Message, Is.EqualTo($"Action {actionName} is not implemented in this driver is not implemented in this driver."));
        }

        [Test]
        public void CommandBlind_WhenNotConnected_ThenThrowsNotConnectedException()
        {
            string expectedMessage = "test blind Message";
            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.CommandBlind(expectedMessage, true); });

            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: CommandBlind"));
        }

        [Test]
        public void CommandBlind_WhenConnected_ThenSendsExpectedMessage()
        {
            string expectedMessage = "test blind Message";

            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns( () => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);
            _telescope.Connected = true;

            _telescope.CommandBlind(expectedMessage, true);

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(expectedMessage), Times.Once);
        }

        [Test]
        public void CommandBool_WhenNotConnected_ThenThrowsNotConnectedException()
        {
            string expectedMessage = "test blind Message";
            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.CommandBool(expectedMessage, true); });

            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: CommandBool"));
        }

        [Test]
        public void CommandBool_WhenConnected_ThenSendsExpectedMessage()
        {
            string expectedMessage = "test blind Message";

            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);
            _telescope.Connected = true;

            var exception = Assert.Throws<MethodNotImplementedException>(() => { _telescope.CommandBool(expectedMessage, true); });

            Assert.That(exception.Message, Is.EqualTo("Method CommandBool is not implemented in this driver."));
        }

        [Test]
        public void CommandString_WhenNotConnected_ThenThrowsNotConnectedException()
        {
            string expectedMessage = "test blind Message";
            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.CommandString(expectedMessage, true); });

            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: CommandString"));
        }

        [Test]
        public void CommandString_WhenConnected_ThenSendsExpectedMessage()
        {
            string expectedMessage = "expected result message";
            string sendMessage = "test blind Message";

            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);
            _telescope.Connected = true;

            _sharedResourcesWrapperMock.Setup(x => x.SendString(sendMessage)).Returns(() => expectedMessage);

            var actualMessage = _telescope.CommandString(sendMessage, true);

            _sharedResourcesWrapperMock.Verify(x => x.SendString(sendMessage), Times.Once);
            Assert.That(actualMessage, Is.EqualTo(expectedMessage));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Connected_Get_ReturnsExpectedValue(bool expectedConnected)
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);
            _telescope.Connected = expectedConnected;

            Assert.That(_telescope.Connected, Is.EqualTo(expectedConnected));
        }


        [Test]
        public void Connected_Set_SettingTrueWhenTrue_ThenDoesNothing()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);
            _telescope.Connected = true;
            _sharedResourcesWrapperMock.Verify( x => x.Connect(It.IsAny<string>()),Times.Once);

            //act
            _telescope.Connected = true;

            //assert
            _sharedResourcesWrapperMock.Verify(x => x.Connect(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Connected_Set_SettingFalseWhenTrue_ThenDisconnects()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);
            _telescope.Connected = true;
            _sharedResourcesWrapperMock.Verify(x => x.Connect(It.IsAny<string>()), Times.Once);

            //act
            _telescope.Connected = false;

            //assert
            _sharedResourcesWrapperMock.Verify(x => x.Disconnect(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void Connected_Set_WhenFailsToConnect_ThenDisconnects()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);

            _sharedResourcesWrapperMock.Setup(x => x.SendString(It.IsAny<string>())).Throws(new Exception("TestFailed"));

            //act
            _telescope.Connected = true;

            //assert
            _sharedResourcesWrapperMock.Verify(x => x.Disconnect(It.IsAny<string>()), Times.Once());
        }

        [TestCase("AUTOSTAR", "30Ab", false)]
        [TestCase("AUTOSTAR","31Ee", true)]
        [TestCase("AUTOSTAR", "43Eg", true)]
        [TestCase("AUTOSTAR II", "", false)]
        public void IsNewPulseGuidingSupported_ThenIsSupported_ThenReturnsTrue(string productName, string firmware, bool isSupported)
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(productName);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(firmware);

            var result = _telescope.IsNewPulseGuidingSupported();

            Assert.That(result, Is.EqualTo(isSupported));
        }

        [Test]
        public void SetLongFormatFalse_WhenTelescopeReturnsShortFormat_ThenDoesNothing()
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GZ#")).Returns("DDD*MM");
            _telescope.SetLongFormat(false);

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":U#"),Times.Never);
        }

        [Test]
        public void SetLongFormatFalse_WhenTelescopeReturnsLongFormat_ThenTogglesPrecision()
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GZ#")).Returns("DDD*MM’SS");
            _telescope.SetLongFormat(false);

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":U#"), Times.Once);
        }

        [Test]
        public void SetLongFormatTrue_WhenTelescopeReturnsLongFormat_ThenDoesNothing()
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GZ#")).Returns("DDD*MM’SS");
            _telescope.SetLongFormat(true);

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":U#"), Times.Never);
        }

        [Test]
        public void SetLongFormatTrue_WhenTelescopeReturnsShortFormat_ThenTogglesPrecision()
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GZ#")).Returns("DDD*MM");
            _telescope.SetLongFormat(true);

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":U#"), Times.Once);
        }

        [Test]
        public void SelectSite_WhenNewSiteToLow_ThenThrowsException()
        {
            var site = 0;
            var result = Assert.Throws<ArgumentOutOfRangeException>(() => { _telescope.SelectSite(site); });

            Assert.That(result.Message, Is.EqualTo($"Site cannot be lower than 1\r\nParameter name: site\r\nActual value was {site}."));
        }

        [Test]
        public void SelectSite_WhenNewSiteToHigh_ThenThrowsException()
        {
            var site = 5;
            var result = Assert.Throws<ArgumentOutOfRangeException>(() => { _telescope.SelectSite(site); });

            Assert.That(result.Message, Is.EqualTo($"Site cannot be higher than 4\r\nParameter name: site\r\nActual value was {site}."));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void SelectSite_WhenNewSiteToHigh_ThenThrowsException(int site)
        {
            _telescope.SelectSite(site);

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind($":W{site}#"), Times.Once);
        }

        [Test]
        public void Description_Get()
        {
            var expectedDescription = "Meade Generic";

            var description = _telescope.Description;

            Assert.That(description, Is.EqualTo(expectedDescription));
        }

        [Test]
        public void DriverVersion_Get()
        {
            Version version = System.Reflection.Assembly.GetAssembly(typeof(ASCOM.Meade.net.Telescope)).GetName().Version;

            string exptectedDriverInfo = $"{version.Major}.{version.Minor}.{version.Revision}.{version.Build}";

            var driverVersion = _telescope.DriverVersion;

            Assert.That(driverVersion, Is.EqualTo(exptectedDriverInfo));
        }

        [Test]
        public void DriverInfo_Get()
        {
            Version version = System.Reflection.Assembly.GetAssembly(typeof(ASCOM.Meade.net.Telescope)).GetName().Version;

            string exptectedDriverInfo = $"{_telescope.Description} .net driver. Version: {_telescope.DriverVersion}";

            var driverInfo = _telescope.DriverInfo;

            Assert.That(driverInfo, Is.EqualTo(exptectedDriverInfo));
        }

        [Test]
        public void InterfaceVersion_Get()
        {
            var interfaceVersion = _telescope.InterfaceVersion;
            Assert.That(interfaceVersion, Is.EqualTo(3));

            Assert.That(_telescope, Is.AssignableTo<ITelescopeV3>());
        }

        [Test]
        public void Name_Get()
        {
            string expectedName = "Meade Generic";

            var name = _telescope.Name;

            Assert.That(name, Is.EqualTo(expectedName));
        }

        [Test]
        public void AlignmentMode_Get_WhenNotConnected_ThrowsException()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);

            var exception = Assert.Throws<NotConnectedException>(() => { var actualResult = _telescope.AlignmentMode; });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: AlignmentMode Get"));
        }


        [TestCase("A", AlignmentModes.algAltAz)]
        [TestCase("P", AlignmentModes.algPolar)]
        [TestCase("G", AlignmentModes.algGermanPolar)]
        public void AlignmentMode_Get_WhenScopeInAltAz_ReturnsAltAz(string telescopeMode, AlignmentModes alignmentMode)
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);
            _telescope.Connected = true;

            const char ack = (char)6;
            _sharedResourcesWrapperMock.Setup(x => x.SendChar(ack.ToString())).Returns(telescopeMode);

            var actualResult = _telescope.AlignmentMode;
            
            Assert.That(actualResult, Is.EqualTo(alignmentMode));
        }

        [Test]
        public void AlignmentMode_Get_WhenUnknownAlignmentMode_ThrowsException()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);
            _telescope.Connected = true;

            Assert.Throws<InvalidValueException>(() => { var actualResult = _telescope.AlignmentMode; });
        }

        [Test]
        public void AlignmentMode_Set_WhenNotConnected_ThrowsException()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);

            var exception = Assert.Throws<NotConnectedException>(() => {  _telescope.AlignmentMode = AlignmentModes.algAltAz; });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: AlignmentMode Set"));
        }

        [TestCase("AUTOSTAR", "43Eg", AlignmentModes.algAltAz, ":AA#")]
        [TestCase("AUTOSTAR", "43Eg", AlignmentModes.algPolar, ":AP#")]
        [TestCase("AUTOSTAR", "43Eg", AlignmentModes.algGermanPolar, ":AP#")]
        public void AlignmentMode_Set_WhenConnected_ThenSendsExpectedCommand(string productName, string firmware, AlignmentModes alignmentMode, string expectedCommand)
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(productName);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(firmware);
            _telescope.Connected = true;

            _telescope.AlignmentMode = alignmentMode;

            _sharedResourcesWrapperMock.Verify( x => x.SendBlind(expectedCommand), Times.Once);
        }

        [TestCase("AUTOSTAR", "43Ef")]
        public void AlignmentMode_Set_WhenAutostarFirmwareToLow_ThenThrowsException(string productName, string firmware )
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(productName);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(firmware);
            _telescope.Connected = true;

            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => { _telescope.AlignmentMode = AlignmentModes.algAltAz; });

            Assert.That(excpetion.Property, Is.EqualTo("AlignmentMode"));
            Assert.That(excpetion.AccessorSet, Is.True);
        }

        [Test]
        public void ApertureArea_Get_ThrowsNotImplementedException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => { var result = _telescope.ApertureArea; });

            Assert.That(excpetion.Property, Is.EqualTo("ApertureArea"));
            Assert.That(excpetion.AccessorSet, Is.False);
        }

        [Test]
        public void ApertureDiameter_Get_ThrowsNotImplementedException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => { var result = _telescope.ApertureDiameter; });

            Assert.That(excpetion.Property, Is.EqualTo("ApertureDiameter"));
            Assert.That(excpetion.AccessorSet, Is.False);
        }

        [Test]
        public void AtHome_Get_ReturnsFalse()
        {
            var result = _telescope.AtHome;

            Assert.That(result, Is.False);
        }

        [Test]
        public void AtPark_Get_WhenNotParked_ThenReturnsFalse()
        {
            var result = _telescope.AtPark;

            Assert.That(result, Is.False);
        }

        [Test]
        public void AtPark_Get_WhenParked_ThenReturnsTrue()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_43EG);
            _telescope.Connected = true;
            _telescope.Park();

            var result = _telescope.AtPark;

            Assert.That(result, Is.True);
        }

        [TestCase(TelescopeAxes.axisPrimary, 4)]
        [TestCase(TelescopeAxes.axisSecondary, 4)]
        [TestCase(TelescopeAxes.axisTertiary, 0)]
        public void AxisRates_ReturnsExpectedResult(TelescopeAxes axis, int expectedCount)
        {
            var result = _telescope.AxisRates(axis);

            Assert.That(result.Count, Is.EqualTo(expectedCount));
        }

        [Test]
        public void CanFindHome_Get_ReturnsFalse()
        {
            var result = _telescope.CanFindHome;

            Assert.That(result, Is.False);
        }

        [TestCase(TelescopeAxes.axisPrimary, true)]
        [TestCase(TelescopeAxes.axisSecondary, true)]
        [TestCase(TelescopeAxes.axisTertiary, false)]
        public void CanMoveAxis_ReturnsExpectedResult(TelescopeAxes axis, bool expected)
        {
            var result = _telescope.CanMoveAxis(axis);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void CanPark_Get_ReturnsTrue()
        {
            var result = _telescope.CanPark;

            Assert.That(result, Is.True);
        }

        [Test]
        public void CanPulseGuide_Get_ReturnsTrue()
        {
            var result = _telescope.CanPulseGuide;

            Assert.That(result, Is.True);
        }

        [Test]
        public void CanSetDeclinationRate_Get_ReturnsFalse()
        {
            var result = _telescope.CanSetDeclinationRate;

            Assert.That(result, Is.False);
        }

        [Test]
        public void CanSetGuideRates_Get_ReturnsFalse()
        {
            var result = _telescope.CanSetGuideRates;

            Assert.That(result, Is.False);
        }

        [Test]
        public void CanSetPark_Get_ReturnsFalse()
        {
            var result = _telescope.CanSetPark;

            Assert.That(result, Is.False);
        }

        [Test]
        public void CanSetPierSide_Get_ReturnsFalse()
        {
            var result = _telescope.CanSetPierSide;

            Assert.That(result, Is.False);
        }

        [Test]
        public void CanSetRightAscensionRate_Get_ReturnsFalse()
        {
            var result = _telescope.CanSetRightAscensionRate;

            Assert.That(result, Is.False);
        }

        [Test]
        public void CanSetTracking_Get_ReturnsTrue()
        {
            var result = _telescope.CanSetTracking;

            Assert.That(result, Is.True);
        }

        [Test]
        public void CanSlew_Get_ReturnsTrue()
        {
            var result = _telescope.CanSlew;

            Assert.That(result, Is.True);
        }

        [Test]
        public void CanSlewAltAz_Get_ReturnsTrue()
        {
            var result = _telescope.CanSlewAltAz;

            Assert.That(result, Is.True);
        }

        [Test]
        public void CanSlewAltAzAsync_Get_ReturnsTrue()
        {
            var result = _telescope.CanSlewAltAzAsync;

            Assert.That(result, Is.True);
        }

        [Test]
        public void CanSlewAsync_Get_ReturnsTrue()
        {
            var result = _telescope.CanSlewAsync;

            Assert.That(result, Is.True);
        }

        [Test]
        public void CanSync_Get_ReturnsTrue()
        {
            var result = _telescope.CanSync;

            Assert.That(result, Is.True);
        }

        [Test]
        public void CanSyncAltAz_Get_ReturnsFalse()
        {
            var result = _telescope.CanSyncAltAz;

            Assert.That(result, Is.False);
        }

        [Test]
        public void CanUnpark_Get_ReturnsFalse()
        {
            var result = _telescope.CanUnpark;

            Assert.That(result, Is.False);
        }

        [Test]
        public void Declination_Get_WhenNotConnected_ThenThrowsException()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);

            var exception = Assert.Throws<NotConnectedException>(() => { var actualResult = _telescope.Declination; });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: Declination Get"));
        }

        [TestCase("s12*34")]
        [TestCase("s12*34’56")]
        public void Declination_Get_WhenConnected_ThenReadsValueFromScope(string declincationString)
        {
            var expectedResult = 12.34;
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);
            _telescope.Connected = true;

            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GD#")).Returns(declincationString);
            _utilMock.Setup(x => x.DMSToDegrees(declincationString)).Returns(expectedResult);
            
            var actualResult = _telescope.Declination;
            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }

        [Test]
        public void DeclinationRate_Get_ThenReturns0()
        {
            var actualResult = _telescope.DeclinationRate;
            
            Assert.That(actualResult, Is.EqualTo(0));
        }

        [Test]
        public void DeclinationRate_Set_ThenThrowsException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => { _telescope.DeclinationRate = 0; });

            Assert.That(excpetion.Property, Is.EqualTo("DeclinationRate"));
            Assert.That(excpetion.AccessorSet, Is.True);
        }

        [Test]
        public void DestinationSideOfPier_ThenThrowsException()
        {
            var excpetion = Assert.Throws<MethodNotImplementedException>(() => { var result = _telescope.DestinationSideOfPier(0,0); });

            Assert.That(excpetion.Method, Is.EqualTo("DestinationSideOfPier"));
        }

        [Test]
        public void DoesRefraction_Get_ThenThrowsException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => { var result = _telescope.DoesRefraction; });

            Assert.That(excpetion.Property, Is.EqualTo("DoesRefraction"));
            Assert.That(excpetion.AccessorSet, Is.False);
        }

        [Test]
        public void DoesRefraction_Set_ThenThrowsException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => { _telescope.DoesRefraction = true; });

            Assert.That(excpetion.Property, Is.EqualTo("DoesRefraction"));
            Assert.That(excpetion.AccessorSet, Is.True);
        }

        [Test]
        public void EquatorialSystem_Get_ReturnsExpectedValue()
        {
            var actualResult = _telescope.EquatorialSystem;

            Assert.That(actualResult, Is.EqualTo(EquatorialCoordinateType.equTopocentric));
        }

        [Test]
        public void FindHome_ThenThrowsException()
        {
            var excpetion = Assert.Throws<MethodNotImplementedException>(() => { _telescope.FindHome();});

            Assert.That(excpetion.Method, Is.EqualTo("FindHome"));
        }

        [Test]
        public void FocalLength_Get_ThenThrowsException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => { var result = _telescope.FocalLength; });

            Assert.That(excpetion.Property, Is.EqualTo("FocalLength"));
            Assert.That(excpetion.AccessorSet, Is.False);
        }

        [Test]
        public void GuideRateDeclination_Get_ThenThrowsException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => { var result = _telescope.GuideRateDeclination; });

            Assert.That(excpetion.Property, Is.EqualTo("GuideRateDeclination"));
            Assert.That(excpetion.AccessorSet, Is.False);
        }

        [Test]
        public void GuideRateDeclination_Set_ThenThrowsException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => { _telescope.GuideRateDeclination = 0; });

            Assert.That(excpetion.Property, Is.EqualTo("GuideRateDeclination"));
            Assert.That(excpetion.AccessorSet, Is.True);
        }

        [Test]
        public void GuideRateRightAscension_Get_ThenThrowsException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => { var result = _telescope.GuideRateRightAscension; });

            Assert.That(excpetion.Property, Is.EqualTo("GuideRateRightAscension"));
            Assert.That(excpetion.AccessorSet, Is.False);
        }

        [Test]
        public void GuideRateRightAscension_Set_ThenThrowsException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => { _telescope.GuideRateRightAscension = 0; });

            Assert.That(excpetion.Property, Is.EqualTo("GuideRateRightAscension"));
            Assert.That(excpetion.AccessorSet, Is.True);
        }

        [Test]
        public void IsPulseGuiding_Get_ReturnsFalse()
        {
            var result = _telescope.IsPulseGuiding;

            Assert.That(result, Is.False);
        }


        [Test]
        public void MoveAxis_WhenNotConnected_ThenThrowsException()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);

            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.MoveAxis(TelescopeAxes.axisPrimary, 0); });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: MoveAxis"));
        }

        [TestCase( 0, "", TelescopeAxes.axisPrimary)]
        [TestCase( 1, ":RG#", TelescopeAxes.axisPrimary)]
        [TestCase(-1, ":RG#", TelescopeAxes.axisPrimary)]
        [TestCase( 2, ":RC#", TelescopeAxes.axisPrimary)]
        [TestCase(-2, ":RC#", TelescopeAxes.axisPrimary)]
        [TestCase( 3, ":RM#", TelescopeAxes.axisPrimary)]
        [TestCase(-3, ":RM#", TelescopeAxes.axisPrimary)]
        [TestCase( 4, ":RS#", TelescopeAxes.axisPrimary)]
        [TestCase(-4, ":RS#", TelescopeAxes.axisPrimary)]
        [TestCase(0, "", TelescopeAxes.axisSecondary)]
        [TestCase(1, ":RG#", TelescopeAxes.axisSecondary)]
        [TestCase(-1, ":RG#", TelescopeAxes.axisSecondary)]
        [TestCase(2, ":RC#", TelescopeAxes.axisSecondary)]
        [TestCase(-2, ":RC#", TelescopeAxes.axisSecondary)]
        [TestCase(3, ":RM#", TelescopeAxes.axisSecondary)]
        [TestCase(-3, ":RM#", TelescopeAxes.axisSecondary)]
        [TestCase(4, ":RS#", TelescopeAxes.axisSecondary)]
        [TestCase(-4, ":RS#", TelescopeAxes.axisSecondary)]
        public void MoveAxis_WhenConnected_ThenExecutesCorrectCommandSequence(double rate, string slewRateCommand, TelescopeAxes axis)
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);
            _telescope.Connected = true;

            _telescope.MoveAxis(axis, rate);

            if (slewRateCommand != string.Empty)
                _sharedResourcesWrapperMock.Verify( x => x.SendBlind(slewRateCommand), Times.Once);
            else
            {
                _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":RG#"), Times.Never);
                _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":RC#"), Times.Never);
                _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":RM#"), Times.Never);
                _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":RS#"), Times.Never);
            }

            switch (axis)
            {
                case TelescopeAxes.axisPrimary:
                    if (rate == 0)
                    {
                        _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":Qe#"), Times.Once);
                        _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":Qw#"), Times.Once);
                    }
                    else if (rate > 0)
                    {
                        _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":Me#"), Times.Once);
                    }
                    else
                    {
                        _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":Mw#"), Times.Once);
                    }
                    break;
                case TelescopeAxes.axisSecondary:
                    if (rate == 0)
                    {
                        _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":Qn#"), Times.Once);
                        _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":Qs#"), Times.Once);
                    }
                    else if (rate > 0)
                    {
                        _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":Mn#"), Times.Once);
                    }
                    else
                    {
                        _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":Ms#"), Times.Once);
                    }
                    break;
                default:
                    Assert.Fail("This should never happen");
                    break;
            }
        }

        [Test]
        public void MoveAxis_WhenRateTooHigh_ThenThrowsException()
        {
            var testRate = 5;

            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);
            _telescope.Connected = true;

            var exception = Assert.Throws<InvalidValueException>( () => { _telescope.MoveAxis(TelescopeAxes.axisTertiary, testRate); });

            Assert.That(exception.Message, Is.EqualTo($"Rate {testRate} not supported"));
        }

        [Test]
        public void MoveAxis_WhenTertiaryAxis_ThenThrowsException()
        {
            var testRate = 0;

            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);
            _telescope.Connected = true;

            var exception = Assert.Throws<InvalidValueException>(() => { _telescope.MoveAxis(TelescopeAxes.axisTertiary, testRate); });

            Assert.That(exception.Message, Is.EqualTo($"Can not move this axis."));
        }

        [Test]
        public void Park_WhenNotConnected_ThenThrowsException()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);

            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.Park(); });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: Park"));
        }

        [Test]
        public void Park_WhenNotParked_ThenSendsParkCommand()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);
            _telescope.Connected = true;
            Assert.That(_telescope.AtPark, Is.False);
            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":hP#"), Times.Never);

            _telescope.Park();

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":hP#"), Times.Once);
            Assert.That(_telescope.AtPark, Is.True);
        }

        [Test]
        public void Park_WhenParked_ThenDoesNothing()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);
            _telescope.Connected = true;
            
            _telescope.Park();

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":hP#"), Times.Once);
            Assert.That(_telescope.AtPark, Is.True);


            //act 
            _telescope.Park();

            //no change from previous state.
            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":hP#"), Times.Once);
            Assert.That(_telescope.AtPark, Is.True);
        }
    }
}

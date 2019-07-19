using System;
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
using InvalidOperationException = ASCOM.InvalidOperationException;
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
            _sharedResourcesWrapperMock.Setup(x => x.Lock(It.IsAny<Func<ASCOM.Meade.net.Telescope.TelescopeDateDetails>>())).Returns<Func<ASCOM.Meade.net.Telescope.TelescopeDateDetails>>( (func) => func());
            _sharedResourcesWrapperMock.Setup(x => x.Lock(It.IsAny<Func<AltitudeData>>())).Returns<Func<AltitudeData>>((func) => func());


            _sharedResourcesWrapperMock.Setup(x => x.ReadProfile()).Returns(_profileProperties);

            _astroMathsMock = new Mock<IAstroMaths>();

            _telescope = new ASCOM.Meade.net.Telescope(_utilMock.Object, _utilExtraMock.Object, _astroUtilsMock.Object,
                _sharedResourcesWrapperMock.Object, _astroMathsMock.Object);
        }

        private void ConnectTelescope()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497_31EE);
            _telescope.Connected = true;
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
            var exception = Assert.Throws<NotConnectedException>(() => { var actualResult = _telescope.Action(string.Empty, string.Empty); });
            Assert.That(exception.Message,Is.EqualTo("Not connected to telescope when trying to execute: Action"));
        }

        [Test]
        public void Action_Handbox_ReadDisplay()
        {
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
            ConnectTelescope();

            _telescope.Action("handbox", action);

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(expectedString), Times.Once);
        }

        [Test]
        public void Action_Handbox_nonExistantAction()
        {
            ConnectTelescope();

            string actionName = "handbox";
            string actionParameters = "doesnotexist";
            var exception = Assert.Throws<ActionNotImplementedException>(() => { _telescope.Action(actionName, actionParameters); });

            Assert.That(exception.Message, Is.EqualTo($"Action {actionName}({actionParameters}) is not implemented in this driver is not implemented in this driver."));
        }

        [Test]
        public void Action_nonExistantAction()
        {
            ConnectTelescope();

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

            ConnectTelescope();

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

            ConnectTelescope();

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

            ConnectTelescope();

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
            ConnectTelescope();
            _sharedResourcesWrapperMock.Verify( x => x.Connect(It.IsAny<string>()),Times.Once);

            //act
            _telescope.Connected = true;

            //assert
            _sharedResourcesWrapperMock.Verify(x => x.Connect(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Connected_Set_SettingFalseWhenTrue_ThenDisconnects()
        {
            ConnectTelescope();
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
            var exception = Assert.Throws<NotConnectedException>(() => { var actualResult = _telescope.AlignmentMode; });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: AlignmentMode Get"));
        }


        [TestCase("A", AlignmentModes.algAltAz)]
        [TestCase("P", AlignmentModes.algPolar)]
        [TestCase("G", AlignmentModes.algGermanPolar)]
        public void AlignmentMode_Get_WhenScopeInAltAz_ReturnsAltAz(string telescopeMode, AlignmentModes alignmentMode)
        {
            ConnectTelescope();

            const char ack = (char)6;
            _sharedResourcesWrapperMock.Setup(x => x.SendChar(ack.ToString())).Returns(telescopeMode);

            var actualResult = _telescope.AlignmentMode;
            
            Assert.That(actualResult, Is.EqualTo(alignmentMode));
        }

        [Test]
        public void AlignmentMode_Get_WhenUnknownAlignmentMode_ThrowsException()
        {
            ConnectTelescope();

            Assert.Throws<InvalidValueException>(() => { var actualResult = _telescope.AlignmentMode; });
        }

        [Test]
        public void AlignmentMode_Set_WhenNotConnected_ThrowsException()
        {
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
            ConnectTelescope();
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
            var exception = Assert.Throws<NotConnectedException>(() => { var actualResult = _telescope.Declination; });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: Declination Get"));
        }

        [TestCase("s12*34")]
        [TestCase("s12*34’56")]
        public void Declination_Get_WhenConnected_ThenReadsValueFromScope(string declincationString)
        {
            var expectedResult = 12.34;
            ConnectTelescope();

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
            ConnectTelescope();

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

            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>( () => { _telescope.MoveAxis(TelescopeAxes.axisTertiary, testRate); });

            Assert.That(exception.Message, Is.EqualTo($"Rate {testRate} not supported"));
        }

        [Test]
        public void MoveAxis_WhenTertiaryAxis_ThenThrowsException()
        {
            var testRate = 0;

            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => { _telescope.MoveAxis(TelescopeAxes.axisTertiary, testRate); });

            Assert.That(exception.Message, Is.EqualTo($"Can not move this axis."));
        }

        [Test]
        public void Park_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.Park(); });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: Park"));
        }

        [Test]
        public void Park_WhenNotParked_ThenSendsParkCommand()
        {
            ConnectTelescope();
            Assert.That(_telescope.AtPark, Is.False);
            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":hP#"), Times.Never);

            _telescope.Park();

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":hP#"), Times.Once);
            Assert.That(_telescope.AtPark, Is.True);
        }

        [Test]
        public void Park_WhenParked_ThenDoesNothing()
        {
            ConnectTelescope();

            _telescope.Park();

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":hP#"), Times.Once);
            Assert.That(_telescope.AtPark, Is.True);


            //act 
            _telescope.Park();

            //no change from previous state.
            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":hP#"), Times.Once);
            Assert.That(_telescope.AtPark, Is.True);
        }

        [Test]
        public void PulseGuide_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.PulseGuide(GuideDirections.guideEast,0); });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: PulseGuide"));
        }

        [TestCase(GuideDirections.guideEast)]
        [TestCase(GuideDirections.guideWest)]
        [TestCase(GuideDirections.guideNorth)]
        [TestCase(GuideDirections.guideSouth)]
        public void PulseGuide_WhenConnectedAndNewerPulseGuidingAvailable_ThenSendsNewCommandsAndWaits(GuideDirections direction)
        {
            var duration = 0;
            ConnectTelescope();

            _telescope.PulseGuide(direction, 0);

            string d = string.Empty;
            switch (direction)
            {
                case GuideDirections.guideEast:
                    d = "e";
                    break;
                case GuideDirections.guideWest:
                    d = "w";
                    break;
                case GuideDirections.guideNorth:
                    d = "n";
                    break;
                case GuideDirections.guideSouth:
                    d = "s";
                    break;
            }

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind($":Mg{d}{duration:0000}#"));
            _utilMock.Verify( x => x.WaitForMilliseconds(duration), Times.Once);
        }

        [TestCase(GuideDirections.guideEast)]
        [TestCase(GuideDirections.guideWest)]
        [TestCase(GuideDirections.guideNorth)]
        [TestCase(GuideDirections.guideSouth)]
        public void PulseGuide_WhenConnectedAndNewerPulseGuidingNotAvailable_ThenSendsOldCommandsAndWaits(GuideDirections direction)
        {
            var duration = 0;
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => _sharedResourcesWrapperMock.Object.AUTOSTAR497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => "31Ed");
            _telescope.Connected = true;

            _telescope.PulseGuide(direction, 0);

            string d = string.Empty;
            switch (direction)
            {
                case GuideDirections.guideEast:
                    d = "e";
                    break;
                case GuideDirections.guideWest:
                    d = "w";
                    break;
                case GuideDirections.guideNorth:
                    d = "n";
                    break;
                case GuideDirections.guideSouth:
                    d = "s";
                    break;
            }

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(":RG#"));
            _sharedResourcesWrapperMock.Verify(x => x.SendBlind($":M{d}#"));
            _utilMock.Verify(x => x.WaitForMilliseconds(duration), Times.Once);
            _sharedResourcesWrapperMock.Verify(x => x.SendBlind($":Q{d}#"));
        }

        [Test]
        public void RightAscension_Get_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { var result = _telescope.RightAscension; });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: RightAscension Get"));
        }

        [Test]
        public void RightAscension_Get_WhenConnected_ThenReturnsExpectedResult()
        {
            var telescopeRaResult = "HH:MM:SS";
            var hmsResult = 1.2;

            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GR#")).Returns(telescopeRaResult);
            _utilMock.Setup(x => x.HMSToHours(telescopeRaResult)).Returns(hmsResult);

            var result = _telescope.RightAscension;

            _sharedResourcesWrapperMock.Verify( x => x.SendString(":GR#"), Times.Once);
            _utilMock.Verify( x => x.HMSToHours(telescopeRaResult), Times.Once);

            Assert.That(result,Is.EqualTo(hmsResult));
        }

        [Test]
        public void RightAscensionRate_Get_ThenReturns0()
        {
            var result = _telescope.RightAscensionRate;

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void RightAscensionRate_Set_ThenThrowsException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => { _telescope.RightAscensionRate = 1; });

            Assert.That(excpetion.Property, Is.EqualTo("RightAscensionRate"));
            Assert.That(excpetion.AccessorSet, Is.True);
        }

        [Test]
        public void SetPark_ThenThrowsException()
        {
            var excpetion = Assert.Throws<MethodNotImplementedException>(() => { _telescope.SetPark(); });

            Assert.That(excpetion.Method, Is.EqualTo("SetPark"));
        }

        [Test]
        public void SideOfPier_Get_ThenThrowsException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => { var result = _telescope.SideOfPier; });

            Assert.That(excpetion.Property, Is.EqualTo("SideOfPier"));
            Assert.That(excpetion.AccessorSet, Is.False);
        }

        [Test]
        public void SideOfPier_Set_ThenThrowsException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => { _telescope.SideOfPier = 0; });

            Assert.That(excpetion.Property, Is.EqualTo("SideOfPier"));
            Assert.That(excpetion.AccessorSet, Is.True);
        }

        [Test]
        public void SiteElevation_Get_ThenThrowsException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => { var result = _telescope.SiteElevation; });

            Assert.That(excpetion.Property, Is.EqualTo("SiteElevation"));
            Assert.That(excpetion.AccessorSet, Is.False);
        }

        [Test]
        public void SiteElevation_Set_ThenThrowsException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => { _telescope.SiteElevation = 0; });

            Assert.That(excpetion.Property, Is.EqualTo("SiteElevation"));
            Assert.That(excpetion.AccessorSet, Is.True);
        }

        [Test]
        public void SlewSettleTime_Get_ThenThrowsException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => { var result = _telescope.SlewSettleTime; });

            Assert.That(excpetion.Property, Is.EqualTo("SlewSettleTime"));
            Assert.That(excpetion.AccessorSet, Is.False);
        }

        [Test]
        public void SlewSettleTime_Set_ThenThrowsException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => { _telescope.SlewSettleTime = 0; });

            Assert.That(excpetion.Property, Is.EqualTo("SlewSettleTime"));
            Assert.That(excpetion.AccessorSet, Is.True);
        }

        [Test]
        public void Unpark_ThenThrowsException()
        {
            var excpetion = Assert.Throws<MethodNotImplementedException>(() => { _telescope.Unpark(); });

            Assert.That(excpetion.Method, Is.EqualTo("Unpark"));
        }

        [Test]
        public void SiteLatitude_Get_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { var result = _telescope.SiteLatitude; });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SiteLatitude Get"));
        }

        [Test]
        public void SiteLatitude_Get_WhenConnected_ThenRetrievesAndReturnsExpectedValue()
        {
            var siteLatitudeString = "testLatString";
            var siteLatitudeValue = 123.45;

            ConnectTelescope();
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":Gt#")).Returns(siteLatitudeString);
            _utilMock.Setup(x => x.DMSToDegrees(siteLatitudeString)).Returns(siteLatitudeValue);

            var result = _telescope.SiteLatitude;
            
            _sharedResourcesWrapperMock.Verify( x => x.SendString(":Gt#"), Times.Once);

            Assert.That(result,Is.EqualTo(siteLatitudeValue));
        }

        [Test]
        public void SiteLatitude_Set_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.SiteLatitude = 123.45; });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SiteLatitude Set"));
        }

        [Test]
        public void SiteLatitude_Set_WhenConnectedAndLatitudeIsGreaterThan90_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => { _telescope.SiteLatitude = 90.01; });
            Assert.That(exception.Message, Is.EqualTo("Latitude cannot be greater than 90 degrees."));
        }

        [Test]
        public void SiteLatitude_Set_WhenConnectedAndLatitudeIsLessThanNegative90_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => { _telescope.SiteLatitude = -90.01; });
            Assert.That(exception.Message, Is.EqualTo("Latitude cannot be less than -90 degrees."));
        }

        [TestCase(-10.5)]
        [TestCase(20.75)]
        public void SiteLatitude_Set_WhenValueSetAndTelescopRejects_ThenExceptionThrown(double siteLatitude)
        {
            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendChar(It.IsAny<string>())).Returns("0");

            var exception = Assert.Throws<ASCOM.InvalidOperationException>(() => { _telescope.SiteLatitude = siteLatitude; });

            Assert.That(exception.Message, Is.EqualTo("Failed to set site latitude."));
        }

        [TestCase(-10.5, ":St-10*30#")]
        [TestCase(20.75, ":St+20*45#")]
        public void SiteLatitude_Set_WhenValidValues_ThenValueSentToTelescope(double siteLatitude, string expectedCommand)
        {
            ConnectTelescope();
            
            _sharedResourcesWrapperMock.Setup(x => x.SendChar(expectedCommand)).Returns("1");

            _telescope.SiteLatitude = siteLatitude;

            _sharedResourcesWrapperMock.Verify(x => x.SendChar(expectedCommand), Times.Once);
        }

        [Test]
        public void SiteLongitude_Get_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { var result = _telescope.SiteLongitude; });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SiteLongitude Get"));
        }


        //todo figure out if this is right.  don't feel right to me
        [TestCase("5", 5, -5)]
        [TestCase("-5", -5, 5)]
        [TestCase("185", 185, 175)]
        [TestCase("350", 350, 10)]
        public void SiteLongitude_Get_WhenConnected_ThenRetrivesAndReturnsExpectedValue(string telescopelongitudeString, double telescopeLongitudeValue, double expectedResult)
        {
            var telescopeLongitude = "testLongitude";

            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendString(":Gg#")).Returns(telescopeLongitude);
            _utilMock.Setup(x => x.DMSToDegrees(telescopeLongitude)).Returns(telescopeLongitudeValue);

            var result = _telescope.SiteLongitude;

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void SiteLongitude_Set_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.SiteLongitude = 123.45; });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SiteLongitude Set"));
        }

        [Test]
        public void SiteLongitude_Set_WhenConnectedAndGreaterThan180_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => { _telescope.SiteLongitude = 180.1; });
            Assert.That(exception.Message, Is.EqualTo("Longitude cannot be greater than 180 degrees."));
        }

        [Test]
        public void SiteLongitude_Set_WhenConnectedAndLessThanNegative180_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => { _telescope.SiteLongitude = -180.1; });
            Assert.That(exception.Message, Is.EqualTo("Longitude cannot be lower than -180 degrees."));
        }

        [Test]
        public void SiteLongitude_Set_WhenConnectedAndTelescopeFails_ThenThrowsException()
        {
            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendChar(It.IsAny<string>())).Returns("0");

            var exception = Assert.Throws<InvalidOperationException>(() => { _telescope.SiteLongitude = 10; });
            Assert.That(exception.Message, Is.EqualTo("Failed to set site longitude."));
        }

        [TestCase(10, ":Sg350*00#")]
        public void SiteLongitude_Set_WhenConnectedAndTelescopeFails_ThenThrowsException(double longitude, string expectedCommand)
        {
            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendChar(expectedCommand)).Returns("1");

            _telescope.SiteLongitude = longitude;

            _sharedResourcesWrapperMock.Verify(x => x.SendChar(expectedCommand), Times.Once);
        }

        [Test]
        public void SyncToAltAz_WhenConnected_ThenSendsExpectedMessage()
        {
            string expectedMessage = "test blind Message";

            ConnectTelescope();

            var exception = Assert.Throws<MethodNotImplementedException>(() => { _telescope.SyncToAltAz(0,0); });

            Assert.That(exception.Message, Is.EqualTo("Method SyncToAltAz is not implemented in this driver."));
        }

        [Test]
        public void SyncToTarget_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.SyncToTarget(); });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SyncToTarget"));
        }

        [Test]
        public void SyncToTarget_WhenSyncToTargetFails_ThenThrowsException()
        {
            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendString(":CM#")).Returns(string.Empty);

            var exception = Assert.Throws<InvalidOperationException>(() => { _telescope.SyncToTarget(); } );

            Assert.That(exception.Message, Is.EqualTo("Unable to perform sync"));
            _sharedResourcesWrapperMock.Verify(x => x.SendString(":CM#"), Times.Once);
        }

        [Test]
        public void SyncToTarget_WhenSyncToTargetWorks_ThennoExceptionThrown()
        {
            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendString(":CM#")).Returns(" M31 EX GAL MAG 3.5 SZ178.0'#");

            Assert.DoesNotThrow(() => { _telescope.SyncToTarget(); });

            _sharedResourcesWrapperMock.Verify(x => x.SendString(":CM#"), Times.Once);
        }

        [Test]
        public void TargetDeclination_Set_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.TargetDeclination = 0; });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: TargetDeclination Set"));
        }

        [Test]
        public void TargetDeclination_Set_WhenValueTooHigh_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => { _telescope.TargetDeclination = 90.1; });
            Assert.That(exception.Message, Is.EqualTo("Declination cannot be greater than 90."));
        }

        [Test]
        public void TargetDeclination_Set_WhenValueTooLow_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => { _telescope.TargetDeclination = -90.1; });
            Assert.That(exception.Message, Is.EqualTo("Declination cannot be less than -90."));
        }

        [Test]
        public void TargetDeclination_Set_WhenTelescopeReportsInvalidDec_ThenThrowsException()
        {
            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendChar(It.IsAny<string>())).Returns("0");

            var exception = Assert.Throws<InvalidOperationException>(() => { _telescope.TargetDeclination = 50; });
            Assert.That(exception.Message, Is.EqualTo("Target declination invalid"));
        }

        [TestCase(-30.5, "-30*30:00", ":Sd-30*30:00#")]
        [TestCase(30.5, "30*30:00", ":Sd+30*30:00#")]
        [TestCase(-75.25, "-75*15:00", ":Sd-75*15:00#")]
        [TestCase(50, "50*00:00", ":Sd+50*00:00#")]
        public void TargetDeclination_Set_WhenValueOK_ThenSetsNewTargetDeclination( double declination,string decstring, string commandString)
        {
            ConnectTelescope();

            _utilMock.Setup(x => x.DegreesToDMS(declination, "*", ":", ":", 2)).Returns(decstring);
            _sharedResourcesWrapperMock.Setup(x => x.SendChar(commandString)).Returns("1");

            _telescope.TargetDeclination = declination;

            _sharedResourcesWrapperMock.Verify(x => x.SendChar(commandString),Times.Once);
        }

        [Test]
        public void TargetDeclination_Get_WhenTargetNotSet_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidOperationException>(() => { var result = _telescope.TargetDeclination; });
            Assert.That(exception.Message, Is.EqualTo("Target not set"));
        }

        [TestCase(50, "50*00:00", ":Sd+50*00:00#")]
        public void TargetDeclination_Get_WhenValueOK_ThenSetsNewTargetDeclination(double declination, string decstring, string commandString)
        {
            ConnectTelescope();

            _utilMock.Setup(x => x.DegreesToDMS(declination, "*", ":", ":", 2)).Returns(decstring);
            _sharedResourcesWrapperMock.Setup(x => x.SendChar(commandString)).Returns("1");

            _telescope.TargetDeclination = declination;

            var result = _telescope.TargetDeclination;

            Assert.That(result, Is.EqualTo(declination));
        }

        [Test]
        public void TargetRightAscension_Set_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.TargetRightAscension = 0; });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: TargetRightAscension Set"));
        }

        [Test]
        public void TargetRightAscension_Set_WhenValueTooHigh_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => { _telescope.TargetRightAscension = 24; });
            Assert.That(exception.Message, Is.EqualTo("Right ascension value cannot be greater than 23:59:59"));
        }

        [Test]
        public void TargetRightAscension_Set_WhenValueTooLow_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => { _telescope.TargetRightAscension = -0.1; });
            Assert.That(exception.Message, Is.EqualTo("Right ascension value cannot be below 0"));
        }

        [Test]
        public void TargetRightAscension_Set_WhenTelescopeReportsInvalidRA_ThenThrowsException()
        {
            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendChar(It.IsAny<string>())).Returns("0");

            var exception = Assert.Throws<InvalidOperationException>(() => { _telescope.TargetRightAscension = 1; });
            Assert.That(exception.Message, Is.EqualTo("Failed to set TargetRightAscension."));
        }

        [TestCase(5.5, "05:30:00", ":Sr05:30:00#")]
        [TestCase(10, "10:00:00", ":Sr10:00:00#")]
        public void TargetRightAscension_Set_WhenValueOK_ThenSetsNewTargetDeclination(double rightAscension, string hms, string commandString)
        {
            ConnectTelescope();

            _utilMock.Setup(x => x.HoursToHMS(rightAscension, ":", ":", ":", 2)).Returns(hms);
            _sharedResourcesWrapperMock.Setup(x => x.SendChar(commandString)).Returns("1");

            _telescope.TargetRightAscension = rightAscension;

            _sharedResourcesWrapperMock.Verify(x => x.SendChar(commandString), Times.Once);
        }

        [Test]
        public void TargetRightAscension_Get_WhenTargetNotSet_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidOperationException>(() => { var result = _telescope.TargetRightAscension; });
            Assert.That(exception.Message, Is.EqualTo("Target not set"));
        }

        [TestCase(15, "15:00:00", ":Sr15:00:00#")]
        public void TargetRightAscension_Get_WhenValueOK_ThenSetsNewTargetDeclination(double rightAscension, string hms, string commandString)
        {
            ConnectTelescope();

            _utilMock.Setup(x => x.HoursToHMS(rightAscension, ":", ":", ":", 2)).Returns(hms);
            _sharedResourcesWrapperMock.Setup(x => x.SendChar(commandString)).Returns("1");

            _telescope.TargetRightAscension = rightAscension;

            var result = _telescope.TargetRightAscension;

            Assert.That(result, Is.EqualTo(rightAscension));
        }

        [Test]
        public void Tracking_Get_WhenDefault_ThenIsTrue()
        {
            Assert.That(_telescope.Tracking, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Tracking_SetAndGet_WhenValueSet_ThenCanGetNewValue(bool tracking)
        {
            _telescope.Tracking = tracking;

            Assert.That(_telescope.Tracking, Is.EqualTo( tracking));
        }

        [Test]
        public void TrackingRate_Set_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.TrackingRate = DriveRates.driveSidereal; });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: TrackingRate Set"));
        }

        [TestCase(DriveRates.driveSidereal, ":TQ#")]
        [TestCase(DriveRates.driveLunar, ":TL#")]
        public void TrackingRate_Set_WhenConnected_ThenSendsCommandToTelescope(DriveRates rate, string commandString)
        {
            ConnectTelescope();

            _telescope.TrackingRate = rate;

            _sharedResourcesWrapperMock.Verify( x => x.SendBlind(commandString), Times.Once);
        }

        [Test]
        public void TrackingRate_Set_WhenUnSupportedRateSet_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<ArgumentOutOfRangeException>( () => { _telescope.TrackingRate = DriveRates.driveKing; });

            Assert.That(exception.Message, Is.EqualTo("Exception of type 'System.ArgumentOutOfRangeException' was thrown.\r\nParameter name: value\r\nActual value was driveKing."));
        }

        [Test]
        public void TrackingRage_Get_WhenReadongDefaultValue_ThenAssumesSidereal()
        {
            ConnectTelescope();

            var result = _telescope.TrackingRate;

            Assert.That(result, Is.EqualTo(DriveRates.driveSidereal));
        }

        [TestCase(DriveRates.driveSidereal)]
        [TestCase(DriveRates.driveLunar)]
        public void TrackingRate_Get_WhenConnected_ThenSendsCommandToTelescope(DriveRates rate)
        {
            ConnectTelescope();

            _telescope.TrackingRate = rate;

            var result = _telescope.TrackingRate;

            Assert.That(result, Is.EqualTo(rate));
        }

        [Test]
        public void TrackingRates_Get_ReturnsExpectedType()
        {
            var result = _telescope.TrackingRates;

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.AssignableTo<TrackingRates>());
        }

        [Test]
        public void UTCDate_Get_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { var result = _telescope.UTCDate; });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: UTCDate Get"));
        }

        [TestCase("10/15/20", "20:15:10", "-1.0", 2020, 10, 15, 19, 15, 10)]
        [TestCase("12/03/15", "21:30:45", "+0.0", 2015, 12, 3, 21, 30, 45)]
        public void UTCDate_Get_WhenConnected_ThenReturnsUTCDateTime(string telescopeDate, string telescopeTime,
            string telescopeUtcCorrection, int year, int month, int day, int hour, int min, int second)
        {
            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GC#")).Returns(telescopeDate);
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GL#")).Returns(telescopeTime);
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GG#")).Returns(telescopeUtcCorrection);

            var result = _telescope.UTCDate;

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.AssignableTo<DateTime>());
            Assert.That(result.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(result.Year, Is.EqualTo(year));
            Assert.That(result.Month, Is.EqualTo(month));
            Assert.That(result.Day, Is.EqualTo(day));
            Assert.That(result.Hour, Is.EqualTo(hour));
            Assert.That(result.Minute, Is.EqualTo(min));
            Assert.That(result.Second, Is.EqualTo(second));
        }

        [Test]
        public void UTCDate_Set_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.UTCDate = new DateTime(2010,10,15,16,42,32, DateTimeKind.Utc); });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: UTCDate Set"));
        }

        [TestCase("10/15/20", "20:15:10", "-1.0", 2020, 10, 15, 19, 15, 10)]
        [TestCase("12/03/15", "21:30:45", "+0.0", 2015, 12, 3, 21, 30, 45)]
        public void UTCDate_Set_WhenFailsToSetTelescopeTime_ThenThrowsException(string telescopeDate, string telescopeTime, string telescopeUtcCorrection, int year, int month, int day, int hour, int min, int second)
        {
            double utcOffsetHours = double.Parse(telescopeUtcCorrection);
            TimeSpan utcCorrection = TimeSpan.FromHours(utcOffsetHours);

            var newDate = new DateTime(year, month, day, hour, min, second, DateTimeKind.Local) + utcCorrection;

            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GG#")).Returns(telescopeUtcCorrection);
            _sharedResourcesWrapperMock.Setup(x => x.SendChar($":SL{telescopeTime}#")).Returns("0");

            var exception = Assert.Throws<InvalidOperationException>(() => { _telescope.UTCDate = newDate; } );

            Assert.That(exception.Message, Is.EqualTo("Failed to set local time"));
        }

        [TestCase("10/15/20", "20:15:10", "-1.0", 2020, 10, 15, 20, 15, 10)]
        [TestCase("12/03/15", "21:30:45", "+0.0", 2015, 12, 3, 21, 30, 45)]
        public void UTCDate_Set_WhenFailsToSetTelescopeDate_ThenThrowsException(string telescopeDate, string telescopeTime, string telescopeUtcCorrection, int year, int month, int day, int hour, int min, int second)
        {
            double utcOffsetHours = double.Parse(telescopeUtcCorrection);
            TimeSpan utcCorrection = TimeSpan.FromHours(utcOffsetHours);

            var newDate = new DateTime(year, month, day, hour, min, second, DateTimeKind.Local) + utcCorrection;

            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GG#")).Returns(telescopeUtcCorrection);
            _sharedResourcesWrapperMock.Setup(x => x.SendChar($":SL{telescopeTime}#")).Returns("1");
            _sharedResourcesWrapperMock.Setup(x => x.SendChar($":SC{newDate:MM/dd/yy}#")).Returns("0");

            var exception = Assert.Throws<InvalidOperationException>(() => { _telescope.UTCDate = newDate; });

            Assert.That(exception.Message, Is.EqualTo("Failed to set local date"));
        }

        [TestCase("10/15/20", "20:15:10", "-1.0", 2020, 10, 15, 20, 15, 10)]
        [TestCase("12/03/15", "21:30:45", "+0.0", 2015, 12, 3, 21, 30, 45)]
        public void UTCDate_Set_WhenSucceeds_ThenReadsTwoStringsFromTelescope(string telescopeDate,
            string telescopeTime, string telescopeUtcCorrection, int year, int month, int day, int hour, int min,
            int second)
        {
            double utcOffsetHours = double.Parse(telescopeUtcCorrection);
            TimeSpan utcCorrection = TimeSpan.FromHours(utcOffsetHours);

            var newDate = new DateTime(year, month, day, hour, min, second, DateTimeKind.Local) + utcCorrection;

            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GG#")).Returns(telescopeUtcCorrection);
            _sharedResourcesWrapperMock.Setup(x => x.SendChar($":SL{telescopeTime}#")).Returns("1");
            _sharedResourcesWrapperMock.Setup(x => x.SendChar($":SC{telescopeDate}#")).Returns("1");

            _telescope.UTCDate = newDate;

            _sharedResourcesWrapperMock.Verify(x => x.ReadTerminated(), Times.Exactly(2));
        }

        [Test]
        public void SyncToCoordinates_WhenNotConnected_ThenThrowsException()
        {
            double rightAscension = 5.5;
            string hms = "05:30:00";

            double declination = -30.5;
            string dec = "-30*30:00";

            ConnectTelescope();

            _utilMock.Setup(x => x.HoursToHMS(rightAscension, ":", ":", ":", 2)).Returns(hms);
            _sharedResourcesWrapperMock.Setup(x => x.SendChar($":Sr{hms}#")).Returns("1");

            _utilMock.Setup(x => x.DegreesToDMS(declination, "*", ":", ":", 2)).Returns(dec);
            _sharedResourcesWrapperMock.Setup(x => x.SendChar($":Sd{dec}#")).Returns("1");

            _telescope.SyncToCoordinates(rightAscension, declination);

            _sharedResourcesWrapperMock.Verify( x => x.SendString(":CM#"), Times.Once);
            Assert.That(_telescope.TargetRightAscension, Is.EqualTo(rightAscension));
            Assert.That(_telescope.TargetDeclination, Is.EqualTo(declination));
        }

        [Test]
        public void Slewing_WhenNotConnected_ThenReturnsFalse()
        {
            var result = _telescope.Slewing;

            Assert.That(result, Is.False);

            _sharedResourcesWrapperMock.Verify(x => x.SendString(":D#"), Times.Never);
        }

        [Test]
        public void Slewing_WhenConnectedAndTelescopeFails_ThenReturnsFalse()
        {
            ConnectTelescope();

            var result = _telescope.Slewing;

            Assert.That(result, Is.False);

            _sharedResourcesWrapperMock.Verify(x => x.SendString(":D#"), Times.Once);
        }

        [Test]
        public void Slewing_WhenTelescopeIsSlewing_ThenReturnsTrue()
        {
            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendString(":D#")).Returns("|");

            var result = _telescope.Slewing;

            Assert.That(result, Is.True);

            _sharedResourcesWrapperMock.Verify(x => x.SendString(":D#"),Times.Once);
        }

        [TestCase(1, TelescopeAxes.axisPrimary)]
        [TestCase(-1, TelescopeAxes.axisPrimary)]
        [TestCase(1, TelescopeAxes.axisSecondary)]
        [TestCase(-1, TelescopeAxes.axisSecondary)]
        public void Slewing_WhenTelescopeIsMoving_ThenDoesNotSendCommandAndReturnsTrue(int rate, TelescopeAxes axis)
        {
            ConnectTelescope();

            _telescope.MoveAxis(axis, rate);

            var result = _telescope.Slewing;

            Assert.That(result, Is.True);
            _sharedResourcesWrapperMock.Verify(x => x.SendString(":D#"), Times.Never);
        }


        [Test]
        public void SlewToTargetAsync_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.SlewToTargetAsync(); });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SlewToTargetAsync"));
        }

        [Test]
        public void SlewToTargetAsync_WhenTargetDeclinationNotSet_ThenThrowsException()
        {
            ConnectTelescope();

            _telescope.TargetRightAscension = 1;

            var exception = Assert.Throws<InvalidOperationException>(() => { _telescope.SlewToTargetAsync(); });
            Assert.That(exception.Message, Is.EqualTo("Target not set"));
        }

        [Test]
        public void SlewToTargetAsync_WhenTargetRightAscensionNotSet_ThenThrowsException()
        {
            ConnectTelescope();

            _telescope.TargetDeclination = 1;

            var exception = Assert.Throws<InvalidOperationException>(() => { _telescope.SlewToTargetAsync(); });
            Assert.That(exception.Message, Is.EqualTo("Target not set"));
        }

        [Test]
        public void SlewToTargetAsync_WhenTargetSet_ThenAttemptsSlew()
        {
            ConnectTelescope();

            _telescope.TargetRightAscension = 2;
            _telescope.TargetDeclination = 1;

            var exception = Assert.Throws<DriverException>(() => { _telescope.SlewToTargetAsync(); });
            Assert.That(exception.Message, Is.EqualTo("This error should not happen"));
        }

        [Test]
        public void SlewToTargetAsync_WhenTargetSetAndSlewIsPossible_ThenAttemptsSlew()
        {
            ConnectTelescope();

            _telescope.TargetRightAscension = 2;
            _telescope.TargetDeclination = 1;

            _sharedResourcesWrapperMock.Setup(x => x.SendChar(":MS#")).Returns("0");

            _telescope.SlewToTargetAsync();

            _sharedResourcesWrapperMock.Verify(x => x.SendChar(":MS#"), Times.Once);
        }

        [Test]
        public void SlewToTargetAsync_WhenTargetBelowHorizon_ThenThrowsException()
        {
            ConnectTelescope();

            _telescope.TargetRightAscension = 2;
            _telescope.TargetDeclination = 1;

            _sharedResourcesWrapperMock.Setup(x => x.SendChar(":MS#")).Returns("1");

            _sharedResourcesWrapperMock.Setup(x => x.ReadTerminated()).Returns("Below horizon");

            var exception = Assert.Throws<InvalidOperationException>(() => { _telescope.SlewToTargetAsync(); });
            Assert.That(exception.Message, Is.EqualTo("Below horizon"));
        }

        [Test]
        public void SlewToTargetAsync_WhenTargetBelowElevation_ThenThrowsException()
        {
            ConnectTelescope();

            _telescope.TargetRightAscension = 2;
            _telescope.TargetDeclination = 1;

            _sharedResourcesWrapperMock.Setup(x => x.SendChar(":MS#")).Returns("2");

            _sharedResourcesWrapperMock.Setup(x => x.ReadTerminated()).Returns("Above below elevation");

            var exception = Assert.Throws<InvalidOperationException>(() => { _telescope.SlewToTargetAsync(); });
            Assert.That(exception.Message, Is.EqualTo("Above below elevation"));
        }

        [Test]
        public void SlewToTarget_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.SlewToTarget(); });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SlewToTarget"));
        }

        [Test]
        public void SlewToTarget_WhenSlewing_ThenWaitsForTheSlewToComplete()
        {
            ConnectTelescope();

            _telescope.TargetRightAscension = 2;
            _telescope.TargetDeclination = 1;

            _sharedResourcesWrapperMock.Setup(x => x.SendChar(":MS#")).Returns("0");

            var slewCounter = 0;
            var iterations = 10;
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":D#")).Returns(() =>
            {
                slewCounter++;
                if (slewCounter <= iterations)
                    return "|";
                else
                    return "";
            });

            _telescope.SlewToTarget();

            _utilMock.Verify( x => x.WaitForMilliseconds(It.IsAny<int>()), Times.Exactly(iterations));
        }

        [Test]
        public void SlewToCoordinatesAsync_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.SlewToCoordinatesAsync(0,0); });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SlewToCoordinatesAsync"));
        }

        [Test]
        public void SlewToCoordinatesAsync_WhenCalled_ThenSetsTargetAndSlews()
        {
            var rightAscension = 1;
            var declination = 2;

            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendChar(":MS#")).Returns("0");

            //var slewCounter = 0;
            //var iterations = 10;
            //_sharedResourcesWrapperMock.Setup(x => x.SendString(":D#")).Returns(() =>
            //{
            //    slewCounter++;
            //    if (slewCounter <= iterations)
            //        return "|";
            //    else
            //        return "";
            //});

            _telescope.SlewToCoordinatesAsync(rightAscension, declination);

            //_utilMock.Verify(x => x.WaitForMilliseconds(It.IsAny<int>()), Times.Exactly(iterations));
            Assert.That(_telescope.TargetRightAscension, Is.EqualTo(rightAscension));
            Assert.That(_telescope.TargetDeclination, Is.EqualTo(declination));
            _sharedResourcesWrapperMock.Verify( x => x.SendChar(":MS#"), Times.Once);
        }

        [Test]
        public void SlewToCoordinates_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.SlewToCoordinates(0, 0); });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SlewToCoordinates"));
        }

        [Test]
        public void SlewToCoordinates_WhenCalled_ThenSetsTargetAndSlews()
        {
            var rightAscension = 1;
            var declination = 2;

            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendChar(":MS#")).Returns("0");

            var slewCounter = 0;
            var iterations = 10;
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":D#")).Returns(() =>
            {
                slewCounter++;
                if (slewCounter <= iterations)
                    return "|";
                else
                    return "";
            });

            _telescope.SlewToCoordinates(rightAscension, declination);
            Assert.That(_telescope.TargetRightAscension, Is.EqualTo(rightAscension));
            Assert.That(_telescope.TargetDeclination, Is.EqualTo(declination));
            _sharedResourcesWrapperMock.Verify(x => x.SendChar(":MS#"), Times.Once);

            _utilMock.Verify(x => x.WaitForMilliseconds(It.IsAny<int>()), Times.Exactly(iterations));
        }

        [Test]
        public void SlewToAltAzAsync_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.SlewToAltAzAsync(0, 0); });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SlewToAltAzAsync"));
        }

        [Test]
        public void SlewToAltAzAsync_WhenAltitudeGreaterThan90_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => { _telescope.SlewToAltAzAsync(0, 90.1); });
            Assert.That(exception.Message, Is.EqualTo("Altitude cannot be greater than 90."));
        }

        [Test]
        public void SlewToAltAzAsync_WhenAltitudeLowerThan0_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => { _telescope.SlewToAltAzAsync(0, -0.1); });
            Assert.That(exception.Message, Is.EqualTo("Altitide cannot be less than 0."));
        }

        [Test]
        public void SlewToAltAzAsync_WhenAzimuth360OrHigher_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => { _telescope.SlewToAltAzAsync(360, 0); });
            Assert.That(exception.Message, Is.EqualTo("Azimuth cannot be 360 or higher."));
        }

        [Test]
        public void SlewToAltAzAsync_WhenAzimuthLowerThan0_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => { _telescope.SlewToAltAzAsync(-0.1, 0); });
            Assert.That(exception.Message, Is.EqualTo("Azimuth cannot be less than 0."));
        }

        [Test]
        public void SlewToAltAzAsync_WhenAltAndAzValid_ThenConvertsToRADec()
        {
            var altitude = 30;
            var azimuth = 45;
            var rightAscension = 20;
            var declination = 10;

            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GC#")).Returns("10/15/20");
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GL#")).Returns("20:15:10");
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GG#")).Returns("-1.0");

            _astroMathsMock
                .Setup(x => x.ConvertHozToEq(It.IsAny<DateTime>(), It.IsAny<double>(), It.IsAny<double>(),
                    It.IsAny<HorizonCoordinates>())).Returns(new EquatorialCoordinates(){ Declination = declination, RightAscension = rightAscension });

            _sharedResourcesWrapperMock.Setup(x => x.SendChar(":MS#")).Returns("0");

            _telescope.SlewToAltAzAsync(azimuth, altitude);
            
            Assert.That(_telescope.TargetRightAscension, Is.EqualTo(rightAscension));
            Assert.That(_telescope.TargetDeclination, Is.EqualTo(declination));
            _sharedResourcesWrapperMock.Verify(x => x.SendChar(":MS#"), Times.Once);
        }

        [Test]
        public void SlewToAltAz_WhenAzimuthLowerThan0_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.SlewToAltAz(0, 0); });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SlewToAltAz"));
        }

        [Test]
        public void SlewToAltAz_WhenCalled_ThenSetsTargetAndSlews()
        {
            var rightAscension = 10;
            var declination = 20;
            var azimuth = 30;
            var altitude = 40;

            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GC#")).Returns("10/15/20");
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GL#")).Returns("20:15:10");
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GG#")).Returns("-1.0");

            _astroMathsMock
                .Setup(x => x.ConvertHozToEq(It.IsAny<DateTime>(), It.IsAny<double>(), It.IsAny<double>(),
                    It.IsAny<HorizonCoordinates>())).Returns(new EquatorialCoordinates() { Declination = declination, RightAscension = rightAscension });

            _sharedResourcesWrapperMock.Setup(x => x.SendChar(":MS#")).Returns("0");

            var slewCounter = 0;
            var iterations = 10;
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":D#")).Returns(() =>
            {
                slewCounter++;
                if (slewCounter <= iterations)
                    return "|";
                else
                    return "";
            });

            _telescope.SlewToAltAz( azimuth, altitude);

            Assert.That(_telescope.TargetRightAscension, Is.EqualTo(rightAscension));
            Assert.That(_telescope.TargetDeclination, Is.EqualTo(declination));
            _sharedResourcesWrapperMock.Verify(x => x.SendChar(":MS#"), Times.Once);
            _utilMock.Verify(x => x.WaitForMilliseconds(It.IsAny<int>()), Times.Exactly(iterations));
        }

        [Test]
        public void Azimuth_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { var result = _telescope.Azimuth; });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: Azimuth Get"));
        }

        [Test]
        public void Azimuth_WhenConnected_ThenReturnsTelescopeAzumith()
        {
            var expectedAzimuth = 200;

            var telescopeLongitude = "350";
            var telescopeLongitudeValue = 350;

            var telescopeLatitude = "HH:MM:SS";
            var telescopeLatitudeValue = 1.2;

            var mockHourAngle = 3;

            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GC#")).Returns("10/15/20");
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GL#")).Returns("20:15:10");
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GG#")).Returns("-1.0");

            _sharedResourcesWrapperMock.Setup(x => x.SendString(":Gg#")).Returns(telescopeLongitude);
            _utilMock.Setup(x => x.DMSToDegrees(telescopeLongitude)).Returns(telescopeLongitudeValue);

            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GR#")).Returns(telescopeLatitude);
            _utilMock.Setup(x => x.HMSToHours(telescopeLatitude)).Returns(telescopeLatitudeValue);

            _astroMathsMock.Setup(x => x.RightAscensionToHourAngle(It.IsAny<DateTime>(), It.IsAny<double>(), It.IsAny<double>())).Returns(mockHourAngle);

            _astroMathsMock.Setup(x => x.ConvertEqToHoz(mockHourAngle, It.IsAny<double>(), It.IsAny<EquatorialCoordinates>())).Returns( new HorizonCoordinates{ Altitude = 45, Azimuth = expectedAzimuth });

            var result = _telescope.Azimuth;

            Assert.That(result,Is.EqualTo(expectedAzimuth));
        }

        [Test]
        public void Altitude_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { var result = _telescope.Altitude; });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: Altitude Get"));
        }

        [Test]
        public void Altitude_WhenConnected_ThenReturnsTelescopeAltitude()
        {
            var expectedAltitude = 45;

            var telescopeLongitude = "350";
            var telescopeLongitudeValue = 350;

            var telescopeLatitude = "HH:MM:SS";
            var telescopeLatitudeValue = 1.2;

            var mockHourAngle = 3;

            ConnectTelescope();

            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GC#")).Returns("10/15/20");
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GL#")).Returns("20:15:10");
            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GG#")).Returns("-1.0");

            _sharedResourcesWrapperMock.Setup(x => x.SendString(":Gg#")).Returns(telescopeLongitude);
            _utilMock.Setup(x => x.DMSToDegrees(telescopeLongitude)).Returns(telescopeLongitudeValue);

            _sharedResourcesWrapperMock.Setup(x => x.SendString(":GR#")).Returns(telescopeLatitude);
            _utilMock.Setup(x => x.HMSToHours(telescopeLatitude)).Returns(telescopeLatitudeValue);

            _astroMathsMock.Setup(x => x.RightAscensionToHourAngle(It.IsAny<DateTime>(), It.IsAny<double>(), It.IsAny<double>())).Returns(mockHourAngle);

            _astroMathsMock.Setup(x => x.ConvertEqToHoz(mockHourAngle, It.IsAny<double>(), It.IsAny<EquatorialCoordinates>())).Returns(new HorizonCoordinates { Altitude = expectedAltitude, Azimuth = 200 });

            var result = _telescope.Altitude;

            Assert.That(result, Is.EqualTo(expectedAltitude));
        }

        [Test]
        public void AbortSlew_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => { _telescope.AbortSlew(); });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: AbortSlew"));
        }

        [Test]
        public void AbortSlew_WhenConnected_ThenSendsStopSlewingToTelescope()
        {
            ConnectTelescope();

            _telescope.AbortSlew();

            _sharedResourcesWrapperMock.Verify( x => x.SendBlind(":Q#"),Times.Once);

            var isSloSlewing = _telescope.Slewing;

            Assert.That(isSloSlewing, Is.False);
            _sharedResourcesWrapperMock.Verify( x => x.SendString(":D#"), Times.Once);
        }
    }
}
;
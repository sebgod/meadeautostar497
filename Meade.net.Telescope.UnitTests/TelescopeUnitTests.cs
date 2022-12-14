using System;
using System.Globalization;
using System.Reflection;
using ASCOM;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Astrometry.NOVAS;
using ASCOM.DeviceInterface;
using ASCOM.Meade.net;
using ASCOM.Meade.net.AstroMaths;
using ASCOM.Meade.net.Wrapper;
using ASCOM.Utilities.Interfaces;
using Moq;
using NUnit.Framework;
using InvalidOperationException = ASCOM.InvalidOperationException;

namespace Meade.net.Telescope.UnitTests
{
    public class TestProperties
    {
        internal string telescopeRaResult { get; set; } = "HH:MM:SS";
        internal double rightAscension { get; set; } = 1.2;
        internal double declination { get; set; } = 45;

        internal string SiteLatitudeString { get; set; } = "testLatString";
        internal double SiteLatitudeValue { get; set; } = 123.45;

        internal string telescopeDate { get; set; } = "10/15/20";
        internal string telescopeTime { get; set; } = "20:15:10";
        internal string telescopeUtcCorrection { get; set; } = "-1.0";

        internal double hourAngle { get; set; }
        internal int telescopeAltitude { get; set; } = 45;
        internal int telescopeAzimuth { get; set; } = 200;

        internal char[] AlignmentStatus { get; set; }
        internal string TrackingRate { get; set; }
    }

    [TestFixture]
    public class TelescopeUnitTests
    {
        private ASCOM.Meade.net.Telescope _telescope;
        private Mock<IUtil> _utilMock;
        private Mock<IUtilExtra> _utilExtraMock;
        private Mock<IAstroUtils> _astroUtilsMock;
        private Mock<ISharedResourcesWrapper> _sharedResourcesWrapperMock;
        private Mock<IAstroMaths> _astroMathsMock;
        private Mock<IClock> _clockMock;
        private Mock<INOVAS31> _novasMock;

        private ProfileProperties _profileProperties;
        private ConnectionInfo _connectionInfo;

        private TestProperties _testProperties;

        private bool _isParked;
        private ParkedPosition _parkedPosition;
        private string _siderealTrackingRate;

        [SetUp]
        public void Setup()
        {
            _isParked = false;
            _parkedPosition = null;
            _siderealTrackingRate = "+60.1";

            _testProperties = new TestProperties();

            _profileProperties = new ProfileProperties
            {
                TraceLogger = false,
                ComPort = "TestCom1",
                Speed = 9600,
                Parity = "None",
                Handshake = "None",
                StopBits = "One",
                DataBits = 8,

                GuideRateArcSecondsPerSecond = 1.23,
                Precision = "Unchanged",
                GuidingStyle = "Auto",

                SendDateTime = false,
                ParkedBehaviour = ParkedBehaviour.NoCoordinates,
                ParkedAlt = 0,
                ParkedAz = 180
            };

            _utilMock = new Mock<IUtil>();
            _utilExtraMock = new Mock<IUtilExtra>();
            _astroUtilsMock = new Mock<IAstroUtils>();

            _sharedResourcesWrapperMock = new Mock<ISharedResourcesWrapper>();
            _sharedResourcesWrapperMock.Setup(x => x.SendString("GZ", false)).Returns("DDD*MM'SS");

            _sharedResourcesWrapperMock.Setup(x => x.ReadProfile()).Returns(() =>_profileProperties);

            _connectionInfo = new ConnectionInfo
            {
                //Connections = 1,
                SameDevice = 1
            };

            _sharedResourcesWrapperMock.Setup(x => x.Connect("Serial", It.IsAny<string>(), It.IsAny<ITraceLogger>())).Returns( () => _connectionInfo);

            _sharedResourcesWrapperMock.Setup(x => x.ReadProfile()).Returns(_profileProperties);

            _sharedResourcesWrapperMock
                .SetupProperty(x => x.SideOfPier)
                .SetupProperty(x => x.TargetRightAscension)
                .SetupProperty(x => x.TargetDeclination)
                .SetupProperty(x => x.SlewSettleTime)
                .SetupProperty(x => x.IsLongFormat);

            _astroMathsMock = new Mock<IAstroMaths>();

            _clockMock = new Mock<IClock>();

            _novasMock = new Mock<INOVAS31>();

            _telescope = new ASCOM.Meade.net.Telescope(_utilMock.Object, _utilExtraMock.Object, _astroUtilsMock.Object,
                _sharedResourcesWrapperMock.Object, _astroMathsMock.Object, _clockMock.Object, _novasMock.Object);
        }

        private void ConnectTelescope(string productName = TelescopeList.Autostar497, string firmwareVersion = TelescopeList.Autostar497_31Ee, string alignmentStatus = "GT0")
        {
            _testProperties.AlignmentStatus = alignmentStatus.ToCharArray();

            _sharedResourcesWrapperMock.Setup(x => x.SendChars("GW", false, 3)).Returns(() => new string(_testProperties.AlignmentStatus));
            _sharedResourcesWrapperMock.Setup(x => x.SendBlind("AP", false)).Callback(() => _testProperties.AlignmentStatus[1] = 'T');
            _sharedResourcesWrapperMock.Setup(x => x.SendBlind("AL", false)).Callback(() => _testProperties.AlignmentStatus[1] = 'N');

            _sharedResourcesWrapperMock.Setup(x => x.SendString("Gt", false)).Returns( () => _testProperties.SiteLatitudeString);
            _utilMock.Setup(x => x.DMSToDegrees(_testProperties.SiteLatitudeString)).Returns( () => _testProperties.SiteLatitudeValue);

            _sharedResourcesWrapperMock.Setup(x => x.SendString("GR", false)).Returns( () =>_testProperties.telescopeRaResult);
            _utilMock.Setup(x => x.HMSToHours(_testProperties.telescopeRaResult)).Returns( () => _testProperties.rightAscension);

            _sharedResourcesWrapperMock.Setup(x => x.SendString("GC", false)).Returns(() => _testProperties.telescopeDate);
            _sharedResourcesWrapperMock.Setup(x => x.SendString("GL", false)).Returns(() => _testProperties.telescopeTime);
            _sharedResourcesWrapperMock.Setup(x => x.SendString("GG", false)).Returns(() => _testProperties.telescopeUtcCorrection);

            _testProperties.TrackingRate = _siderealTrackingRate;
            _sharedResourcesWrapperMock.Setup(x => x.SendString("GT", false)).Returns(() => _testProperties.TrackingRate);
            _sharedResourcesWrapperMock.Setup(x => x.SendBlind("TL", false)).Callback(() => _testProperties.TrackingRate = "57.9");
            _sharedResourcesWrapperMock.Setup(x => x.SendBlind("TQ", false)).Callback(() => _testProperties.TrackingRate = _siderealTrackingRate);

            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => productName);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => firmwareVersion);

            _sharedResourcesWrapperMock.Setup(x => x.SetParked(It.IsAny<bool>(), It.IsAny<ParkedPosition>())).Callback<bool,ParkedPosition>((isParked, parkedPostion) => {
                _isParked = isParked;
                _parkedPosition = parkedPostion;
            });

            _sharedResourcesWrapperMock.SetupGet(x => x.IsParked).Returns(() => _isParked);
            _sharedResourcesWrapperMock.SetupGet(x => x.ParkedPosition).Returns(() => _parkedPosition);

            _sharedResourcesWrapperMock.SetupProperty(x => x.IsGuiding);

            _astroMathsMock
                .Setup(x => x.ConvertHozToEq(It.IsAny<DateTime>(), It.IsAny<double>(), It.IsAny<double>(),
                    It.IsAny<HorizonCoordinates>())).Returns(() => new EquatorialCoordinates { Declination = _testProperties.declination, RightAscension = _testProperties.rightAscension });

            _astroMathsMock.Setup(x => x.RightAscensionToHourAngle(It.IsAny<DateTime>(), It.IsAny<double>(), It.IsAny<double>())).Returns(() =>_testProperties.hourAngle);

            _astroMathsMock.Setup(x => x.ConvertEqToHoz(_testProperties.hourAngle, It.IsAny<double>(), It.IsAny<EquatorialCoordinates>())).Returns(new HorizonCoordinates { Altitude = _testProperties.telescopeAltitude, Azimuth = _testProperties.telescopeAzimuth });

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
            Assert.That(supportedActions.Count, Is.EqualTo(2));
            Assert.That(supportedActions.Contains("handbox"), Is.True);
            Assert.That(supportedActions.Contains("site"), Is.True);
        }

        [Test]
        public void Action_WhenNotConnected_ThrowsNotConnectedException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.Action(string.Empty, string.Empty) );
            Assert.That(exception.Message,Is.EqualTo("Not connected to telescope when trying to execute: Action"));
        }

        [Test]
        public void Action_Handbox_ReadDisplay()
        {
            string expectedResult = "test result string";
            _sharedResourcesWrapperMock.Setup(x => x.SendString("ED", false)).Returns(expectedResult);
            _telescope.Connected = true;



            var actualResult = _telescope.Action("handbox", "readdisplay");

            _sharedResourcesWrapperMock.Verify(x => x.SendString("ED", false), Times.Once);
            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }

        [TestCase("enter", "EK13")]
        [TestCase("mode", "EK9")]
        [TestCase("longMode", "EK11")]
        [TestCase("goto", "EK24")]
        [TestCase("0", "EK48")]
        [TestCase("1", "EK49")]
        [TestCase("2", "EK50")]
        [TestCase("3", "EK51")]
        [TestCase("4", "EK52")]
        [TestCase("5", "EK53")]
        [TestCase("6", "EK54")]
        [TestCase("7", "EK55")]
        [TestCase("8", "EK56")]
        [TestCase("9", "EK57")]
        [TestCase("up", "EK94")]
        [TestCase("down", "EK118")]
        [TestCase("back", "EK87")]
        [TestCase("forward", "EK69")]
        [TestCase("left", "EK87")]
        [TestCase("right", "EK69")]
        [TestCase("scrollup", "EK85")]
        [TestCase("scrolldown", "EK68")]
        [TestCase("?", "EK63")]
        public void Action_Handbox_WhenCalling_ThenSendsAppropriateBlindCommands(string action, string expectedString)
        {
            ConnectTelescope();

            _telescope.Action("handbox", action);

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(expectedString, false), Times.Once);
        }

        [TestCase("1")]
        [TestCase("2")]
        [TestCase("3")]
        [TestCase("4")]
        public void Action_Site_WhenCallingWithValidValues_ThenSelectsCorrectSite(string site)
        {
            ConnectTelescope();

            string parameters = $"select {site}";
            _telescope.Action("site", parameters);

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind($"W{site}", false), Times.Once);
        }

        [TestCase("0")]
        [TestCase("5")]
        public void Action_Site_WhenCallingWithInCorrectValues_ThenThrowsException(string site)
        {
            ConnectTelescope();

            string parameters = $"select {site}";
            var exception = Assert.Throws<InvalidValueException>(() => _telescope.Action("site", parameters) );

            Assert.That(exception.Message, Is.EqualTo($"Site {parameters} not allowed, must be between 1 and 4"));
        }

        [TestCase("1", "GM", "Home")]
        [TestCase("2", "GN", "Club")]
        [TestCase("3", "GO", "GPS")]
        [TestCase("4", "GP", "Parents")]
        public void Action_Site_GetName_WhenCallingWithValidValues_ThenSelectsCorrectSite(string site, string telescopeCommand, string siteName)
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendString(telescopeCommand, false)).Returns(siteName);

            ConnectTelescope();

            string parameters = $"GetName {site}";
            var result = _telescope.Action("site", parameters);

            _sharedResourcesWrapperMock.Verify(x => x.SendString(telescopeCommand, false), Times.Once);
            Assert.That(result, Is.EqualTo(siteName));
        }

        [TestCase("0")]
        [TestCase("5")]
        public void Action_Site_GetName_WhenCallingWithInCorrectValues_ThenThrowsException(string site)
        {
            ConnectTelescope();

            string parameters = $"GetName {site}";
            var exception = Assert.Throws<InvalidValueException>(() => _telescope.Action("site", parameters) );

            Assert.That(exception.Message, Is.EqualTo($"Site {parameters} not allowed, must be between 1 and 4"));
        }

        [Test]
        public void Action_Site_Count_WhenCalling_ThenReturnsFour()
        {
            ConnectTelescope();

            string parameters = "Count";
            var result = _telescope.Action("site", parameters);

            Assert.That(result, Is.EqualTo("4"));
        }

        [TestCase("1", "SMHome", "Home")]
        [TestCase("2", "SNClub", "Club")]
        [TestCase("3", "SOGPS Site", "GPS Site")]
        [TestCase("4", "SPParents", "Parents")]
        public void Action_Site_SetName_WhenCallingWithValidValues_ThenSelectsCorrectSite(string site, string telescopeCommand, string siteName)
        {

            _sharedResourcesWrapperMock.Setup(x => x.SendChar(telescopeCommand, false)).Returns("1");

            ConnectTelescope();

            string parameters = $"SetName {site} {siteName}";
            _telescope.Action("site", parameters);

            _sharedResourcesWrapperMock.Verify(x => x.SendChar(telescopeCommand, false), Times.Once);
        }

        [TestCase("0")]
        [TestCase("5")]
        public void Action_Site_SetName_WhenCallingWithInCorrectValues_ThenThrowsException(string site)
        {
            ConnectTelescope();

            string parameters = $"SetName {site}";
            var exception = Assert.Throws<InvalidValueException>(() => _telescope.Action("site", parameters));

            Assert.That(exception.Message, Is.EqualTo($"Site {parameters} not allowed, must be between 1 and 4"));
        }

        [Test]
        public void Action_Site_WhenCallingUnknownParam_ThenThrowsException()
        {
            ConnectTelescope();

            string parameters = "unknown";
            var exception = Assert.Throws<InvalidValueException>(() => _telescope.Action("site", parameters));

            Assert.That(exception.Message, Is.EqualTo($"Site parameters {parameters} not known"));
        }

        [Test]
        public void Action_Handbox_nonExistantAction()
        {
            ConnectTelescope();

            string actionName = "handbox";
            string actionParameters = "doesnotexist";
            var exception = Assert.Throws<ActionNotImplementedException>(() => _telescope.Action(actionName, actionParameters));

            Assert.That(exception.Message, Is.EqualTo($"Action {actionName}({actionParameters}) is not implemented in this driver is not implemented in this driver."));
        }

        [Test]
        public void Action_nonExistantAction()
        {
            ConnectTelescope();

            string actionName = "doesnotexist";
            var exception = Assert.Throws<ActionNotImplementedException>(() => _telescope.Action(actionName, string.Empty));

            Assert.That(exception.Message, Is.EqualTo($"Action {actionName} is not implemented in this driver is not implemented in this driver."));
        }

        [Test]
        public void CommandBlind_WhenNotConnected_ThenThrowsNotConnectedException()
        {
            string expectedMessage = "test blind Message";
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.CommandBlind(expectedMessage, true));

            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: CommandBlind"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CommandBlind_WhenConnected_ThenSendsExpectedMessage(bool raw)
        {
            string expectedMessage = "test blind Message";

            ConnectTelescope();

            _telescope.CommandBlind(expectedMessage, raw);

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(expectedMessage, raw), Times.Once);
        }

        [Test]
        public void CommandBool_WhenNotConnected_ThenThrowsNotConnectedException()
        {
            string expectedMessage = "test blind Message";
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.CommandBool(expectedMessage, true));

            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: CommandBool"));
        }


        [TestCase(false)]
        [TestCase(true)]
        public void CommandBool_WhenConnected_ThenSendsExpectedMessage(bool raw)
        {
            string expectedMessage = "test blind Message";
            _sharedResourcesWrapperMock.Setup(x => x.SendBool(expectedMessage, raw)).Returns(true);

            ConnectTelescope();

            var result = _telescope.CommandBool(expectedMessage, raw);

            _sharedResourcesWrapperMock.Verify(x => x.SendBool(expectedMessage, raw), Times.Once);
            Assert.That(result, Is.True);
        }

        [Test]
        public void CommandString_WhenNotConnected_ThenThrowsNotConnectedException()
        {
            string expectedMessage = "test blind Message";
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.CommandString(expectedMessage, true));

            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: CommandString"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CommandString_WhenConnected_ThenSendsExpectedMessage(bool raw)
        {
            string expectedMessage = "expected result message";
            string sendMessage = "test blind Message";

            _sharedResourcesWrapperMock.Setup(x => x.SendString(sendMessage, raw)).Returns(() => expectedMessage);

            ConnectTelescope();

            var actualMessage = _telescope.CommandString(sendMessage, raw);

            _sharedResourcesWrapperMock.Verify(x => x.SendString(sendMessage, raw), Times.Once);
            Assert.That(actualMessage, Is.EqualTo(expectedMessage));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Connected_Get_ReturnsExpectedValue(bool expectedConnected)
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => TelescopeList.Autostar497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => TelescopeList.Autostar497_31Ee);

            if (expectedConnected)
                ConnectTelescope();

            Assert.That(_telescope.Connected, Is.EqualTo(expectedConnected));

            if (expectedConnected)
            {
                _sharedResourcesWrapperMock.Verify(x => x.SendString("GZ", false), Times.Once);
                _sharedResourcesWrapperMock.Verify(x => x.SendBlind($"Rg{_profileProperties.GuideRateArcSecondsPerSecond:00.0}", false), Times.Never);
            }
        }

        [Test]
        public void Connected_Set_WhenConnectingLX200GPS_Then_ConnectsToSerialDevice()
        {
            ConnectTelescope(TelescopeList.LX200GPS, string.Empty);

            _sharedResourcesWrapperMock.Verify(x => x.Connect("Serial", It.IsAny<string>(), It.IsAny<ITraceLogger>()), Times.Once);
            _sharedResourcesWrapperMock.Verify(x => x.SendString("GZ", false), Times.Once);

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind($"Rg{_profileProperties.GuideRateArcSecondsPerSecond:00.0}", false), Times.Once);
        }

        [Test]
        public void Connected_WhenConnectingLX200GPSAndSendDateTimeIsTrue_Then_SpecialStartupInstructionSendOnFirstConnect()
        {
            _profileProperties.SendDateTime = true;
            _testProperties.telescopeUtcCorrection = "0";
            DateTime testNow = DateTime.ParseExact("2021-10-03T20:36:25", "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

            _clockMock.Setup(x => x.UtcNow).Returns(() => testNow);

            string setDateCommand = $"hI{testNow:yyMMddHHmmss}";

            string expectedResult = "Daylight Savings Time:";
            _sharedResourcesWrapperMock.Setup(x => x.SendString("ED", false)).Returns(expectedResult);
            _sharedResourcesWrapperMock.Setup(x => x.SendChar(setDateCommand, false)).Returns("1");

            ConnectTelescope(TelescopeList.LX200GPS, string.Empty);

            _sharedResourcesWrapperMock.Verify(x => x.SendChar(setDateCommand, false), Times.Once);
        }

        [Test]
        public void Connected_WhenConnectingLX200GPSAndSendDateTimeIsTrue_Then_ByPassDisplaysWhenNotOnDaylightScreen()
        {
            _profileProperties.SendDateTime = true;

            string telescopeTime = "20:36:25";
            string telescopeDate = "10/03/21";
            _testProperties.telescopeUtcCorrection = "0";
            DateTime endSlewingDatetime = DateTime.ParseExact("2021-10-03T20:36:25", "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

            _clockMock.Setup(x => x.UtcNow).Returns(() => endSlewingDatetime);

            string setDateCommand = $"hI{endSlewingDatetime:yyMMddHHmmss}";

            string expectedResult = "Align";
            _sharedResourcesWrapperMock.Setup(x => x.SendString("ED", false)).Returns(expectedResult);

            _sharedResourcesWrapperMock.Setup(x => x.SendChar($"SL{telescopeTime}", false)).Returns("1");
            _sharedResourcesWrapperMock.Setup(x => x.SendChar($"SC{telescopeDate}", false)).Returns("1");

            ConnectTelescope(TelescopeList.LX200GPS, string.Empty);

            _sharedResourcesWrapperMock.Verify(x => x.SendChar(setDateCommand, false), Times.Never);
            _sharedResourcesWrapperMock.Verify(x => x.ReadTerminated(), Times.Exactly(2));
        }

        [Test]
        public void Connected_WhenConnectingAutostarAndSendDateTimeIsTrue_Then_ByPassDisplaysWhenNotOnDaylightScreen()
        {
            _profileProperties.SendDateTime = true;

            string telescopeTime = "20:36:25";
            string telescopeDate = "10/03/21";
            _testProperties.telescopeUtcCorrection = "0";
            DateTime endSlewingDatetime = DateTime.ParseExact("2021-10-03T20:36:25", "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

            _clockMock.Setup(x => x.UtcNow).Returns(() =>
            {
                return endSlewingDatetime;
            });

            string setDateCommand = $"hI{endSlewingDatetime:yyMMddHHmmss}";

            string expectedResult = "Align";
            _sharedResourcesWrapperMock.Setup(x => x.SendString("ED", false)).Returns(expectedResult);


            _sharedResourcesWrapperMock.Setup(x => x.SendChar($"SL{telescopeTime}", false)).Returns("1");
            _sharedResourcesWrapperMock.Setup(x => x.SendChar($"SC{telescopeDate}", false)).Returns("1");

            ConnectTelescope();

            _sharedResourcesWrapperMock.Verify(x => x.SendChar(setDateCommand, false), Times.Never);
            _sharedResourcesWrapperMock.Verify(x => x.ReadTerminated(), Times.Exactly(2));
        }

        [Test]
        public void Connected_Set_WhenConnectingToLX200EMC_Then_ConnectsToSerialDevice()
        {
            var productName = TelescopeList.LX200CLASSIC;
            var firmware = string.Empty;

            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(productName);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(firmware);
            _telescope.Connected = true;

            _sharedResourcesWrapperMock.Verify(x => x.Connect("Serial", It.IsAny<string>(), It.IsAny<ITraceLogger>()), Times.Once);
            _sharedResourcesWrapperMock.Verify(x => x.SendString("GZ", false), Times.Never);
            _sharedResourcesWrapperMock.Verify(x => x.SendBlind($"Rg{_profileProperties.GuideRateArcSecondsPerSecond:00.0}", false), Times.Never);
        }


        [Test]
        public void Connected_Set_SettingTrueWhenTrue_ThenDoesNothing()
        {
            ConnectTelescope();
            _sharedResourcesWrapperMock.Verify(x => x.Connect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ITraceLogger>()), Times.Once);

            //act
            _telescope.Connected = true;

            //assert
            _sharedResourcesWrapperMock.Verify(x => x.Connect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ITraceLogger>()), Times.Once);
        }

        [Test]
        public void Connected_Set_SettingFalseWhenTrue_ThenDisconnects()
        {
            ConnectTelescope();
            _sharedResourcesWrapperMock.Verify(x => x.Connect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ITraceLogger>()), Times.Once);

            //act
            _telescope.Connected = false;

            //assert
            _sharedResourcesWrapperMock.Verify(x => x.Disconnect(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void Connected_Set_WhenFailsToConnect_ThenDisconnects()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => TelescopeList.Autostar497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => TelescopeList.Autostar497_31Ee);

            _sharedResourcesWrapperMock.Setup(x => x.SendString(It.IsAny<string>(), It.IsAny<bool>())).Throws(new Exception("TestFailed"));

            //act
            _telescope.Connected = true;

            //assert
            _sharedResourcesWrapperMock.Verify(x => x.Disconnect(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [TestCase("Auto", "Autostar", "30Ab", false)]
        [TestCase("Auto", "Autostar", "31Ee", true)]
        [TestCase("Auto", "Autostar", "43Eg", true)]
        [TestCase("Auto", "Autostar", "A4S4", true)]
        [TestCase("Auto", "Autostar II", "", false)]
        [TestCase("Auto", "LX2001", "", true)]
        [TestCase("Auto", ":GVP", "", false)] //LX200 Classic
        [TestCase("Guide Rate Slew", "LX2001", "", false)] //force old style
        [TestCase("Pulse Guiding", ":GVP", "", true)] //force new style

        public void IsNewPulseGuidingSupported_ThenIsSupported_ThenReturnsTrue(string guidingStyle, string productName, string firmware, bool isSupported)
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(productName);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(firmware);

            _profileProperties.GuidingStyle = guidingStyle;

            _telescope.Connected = true;

            var result = _telescope.IsNewPulseGuidingSupported();

            Assert.That(result, Is.EqualTo(isSupported));
        }

        [Test]
        public void SetLongFormatFalse_WhenTelescopeReturnsShortFormat_ThenDoesNothing()
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendString("GZ", false)).Returns("DDD*MM");
            _telescope.SetLongFormat(false);

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("U", false), Times.Never);
        }

        [Test]
        public void SetLongFormatFalse_WhenTelescopeReturnsLongFormat_ThenTogglesPrecision()
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendString("GZ", false)).Returns("DDD*MM’SS");
            _telescope.SetLongFormat(false);

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("U", false), Times.Once);
        }

        [Test]
        public void SetLongFormatTrue_WhenTelescopeReturnsLongFormat_ThenDoesNothing()
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendString("GZ", false)).Returns("DDD*MM’SS");
            _telescope.SetLongFormat(true);

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("U", false), Times.Never);
        }

        [Test]
        public void SetLongFormatTrue_WhenTelescopeReturnsShortFormat_ThenTogglesPrecision()
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendString("GZ", false)).Returns("DDD*MM");
            _telescope.SetLongFormat(true);

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("U", false), Times.Once);
        }

        [Test]
        public void SelectSite_Get_WhenNotConnected_ThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.SelectSite(1));
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SelectSite"));
        }

        [Test]
        public void SelectSite_WhenNewSiteToLow_ThenThrowsException()
        {
            ConnectTelescope();

            var site = 0;
            var result = Assert.Throws<ArgumentOutOfRangeException>(() => _telescope.SelectSite(site));

            Assert.That(result.Message, Is.EqualTo($"Site cannot be lower than 1\r\nParameter name: site\r\nActual value was {site}."));
        }

        [Test]
        public void SelectSite_WhenNewSiteToHigh_ThenThrowsException()
        {
            ConnectTelescope();

            var site = 5;
            var result = Assert.Throws<ArgumentOutOfRangeException>(() => _telescope.SelectSite(site));

            Assert.That(result.Message, Is.EqualTo($"Site cannot be higher than 4\r\nParameter name: site\r\nActual value was {site}."));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void SelectSite_WhenNewSiteToHigh_ThenThrowsException(int site)
        {
            ConnectTelescope();

            _telescope.SelectSite(site);

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind($"W{site}", false), Times.Once);
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
            Version version = Assembly.GetAssembly(typeof(ASCOM.Meade.net.Telescope)).GetName().Version;

            string exptectedDriverInfo = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

            var driverVersion = _telescope.DriverVersion;

            Assert.That(driverVersion, Is.EqualTo(exptectedDriverInfo));
        }

        [Test]
        public void DriverInfo_Get()
        {
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
            var exception = Assert.Throws<NotConnectedException>(() =>
            {
                var actualResult = _telescope.AlignmentMode;
                Assert.Fail($"{actualResult} should not have returned");
            });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: AlignmentMode Get"));
        }


        [TestCase("A", AlignmentModes.algAltAz, TelescopeList.Autostar497, TelescopeList.Autostar497_31Ee)]
        [TestCase("P", AlignmentModes.algPolar, TelescopeList.Autostar497, TelescopeList.Autostar497_31Ee)]
        [TestCase("A", AlignmentModes.algAltAz, TelescopeList.Autostar497, TelescopeList.Autostar497_43Eg)]
        [TestCase("P", AlignmentModes.algPolar, TelescopeList.Autostar497, TelescopeList.Autostar497_43Eg)]
        [TestCase("G", AlignmentModes.algGermanPolar, TelescopeList.Autostar497, TelescopeList.Autostar497_43Eg)]
        public void AlignmentMode_Get_WhenScopeInAltAz_ReturnsAltAz(string telescopeMode, AlignmentModes alignmentMode, string productName, string firmware)
        {
            const char ack = (char)6;
            _sharedResourcesWrapperMock.Setup(x => x.SendChar(ack.ToString(), false)).Returns(telescopeMode);

            ConnectTelescope(productName, firmware, $"{telescopeMode}N0");

            var actualResult = _telescope.AlignmentMode;

            Assert.That(actualResult, Is.EqualTo(alignmentMode));
        }

        [Test]
        public void AlignmentMode_Get_WhenUnknownAlignmentMode_ThrowsException()
        {
            ConnectTelescope();

            Assert.Throws<InvalidValueException>(() =>
            {
                var actualResult = _telescope.AlignmentMode;
                Assert.Fail($"{actualResult} should not have returned");
            });
        }

        [Test]
        public void AlignmentMode_Set_WhenNotConnected_ThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.AlignmentMode = AlignmentModes.algAltAz);
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: AlignmentMode Set"));
        }

        [TestCase(TelescopeList.Autostar497, TelescopeList.Autostar497_43Eg, AlignmentModes.algAltAz, "AA")]
        [TestCase(TelescopeList.Autostar497, TelescopeList.Autostar497_43Eg, AlignmentModes.algPolar, "AP")]
        [TestCase(TelescopeList.Autostar497, TelescopeList.Autostar497_43Eg, AlignmentModes.algGermanPolar, "AP")]
        public void AlignmentMode_Set_WhenConnected_ThenSendsExpectedCommand(string productName, string firmware, AlignmentModes alignmentMode, string expectedCommand)
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(productName);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(firmware);
            _telescope.Connected = true;

            _telescope.AlignmentMode = alignmentMode;

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(expectedCommand, false), Times.Once);
        }

        [TestCase("AUTOSTAR", "43Ef")]
        public void AlignmentMode_Set_WhenAutostarFirmwareToLow_ThenThrowsException(string productName, string firmware)
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(productName);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(firmware);
            _telescope.Connected = true;

            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => _telescope.AlignmentMode = AlignmentModes.algAltAz);

            Assert.That(excpetion.Property, Is.EqualTo("AlignmentMode"));
            Assert.That(excpetion.AccessorSet, Is.True);
        }

        [Test]
        public void ApertureArea_Get_ThrowsNotImplementedException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() =>
            {
                var result = _telescope.ApertureArea;
                Assert.Fail($"{result} should not have returned");
            });

            Assert.That(excpetion.Property, Is.EqualTo("ApertureArea"));
            Assert.That(excpetion.AccessorSet, Is.False);
        }

        [Test]
        public void ApertureDiameter_Get_ThrowsNotImplementedException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() =>
            {
                var result = _telescope.ApertureDiameter;
                Assert.Fail($"{result} should not have returned");
            });

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
        public void CanSetGuideRates_Get_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() =>
            {
                var result = _telescope.CanSetGuideRates;
                Assert.Fail($"{result} should not have returned");
            });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: CanSetGuideRates Get"));
        }

        [Test]
        public void CanSetGuideRates_Get_WhenConnectedToAutostar_ThenReturnsFalse()
        {
            ConnectTelescope();

            var result = _telescope.CanSetGuideRates;

            Assert.That(result, Is.False);
        }

        [Test]
        public void CanSetGuideRates_Get_WhenConnectedToLX200GPS_ThenReturnsTrue()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => TelescopeList.LX200GPS);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => TelescopeList.LX200GPS_42G);
            _telescope.Connected = true;

            var result = _telescope.CanSetGuideRates;

            Assert.That(result, Is.True);
        }

        [Test]
        public void Precision_Set_WhenConnectedAndPrecisionSetUnChanged_ThenDoesNotSetPrecision()
        {
            _telescope.Connected = true;

            _sharedResourcesWrapperMock.Verify(x => x.SendString("P", false), Times.Never);
        }

        [TestCase("High", false, true)]
        [TestCase("High", true, true)]
        [TestCase("Low", false, false)]
        [TestCase("Low", true, false)]
        public void Precision_Set_WhenConnectedAndPrecisionSetHighScopeIsLow_ThenTelescopePrecisionChanged(string desiredPresision, bool telescopePrecision, bool finalPrecision)
        {
            _profileProperties.Precision = desiredPresision;
            var currentPrecision = telescopePrecision;

            _sharedResourcesWrapperMock.Setup(x => x.SendChar("P", false)).Returns(() =>
            {
                currentPrecision = !currentPrecision;

                switch (currentPrecision)
                {
                    case true:
                        return "H";
                    default:
                        return "L";
                }
            });

            ConnectTelescope();

            Assert.That(currentPrecision, Is.EqualTo(finalPrecision));
            _sharedResourcesWrapperMock.Verify(x => x.SendChar("P", false), Times.AtLeastOnce);
        }

        [TestCase("High")]
        [TestCase("Low")]
        public void Precision_Set_WhenSecondConnectionMade_ThenTelescopePrecisionNotChanged(string desiredPresision)
        {
            var isLongFormat = desiredPresision == "High"
                || (desiredPresision == "Low"
                        ? false
                        : throw new ArgumentOutOfRangeException(nameof(desiredPresision), desiredPresision, "Should be High or Low"));
            _sharedResourcesWrapperMock.SetupProperty(x => x.IsLongFormat, isLongFormat);
            _sharedResourcesWrapperMock.Setup(x => x.SendString("GR", false)).Returns(() => _testProperties.telescopeRaResult);
            _utilMock.Setup(x => x.HMSToHours(_testProperties.telescopeRaResult)).Returns(() => _testProperties.rightAscension);

            _profileProperties.Precision = desiredPresision;

            _connectionInfo.SameDevice = 2;
            //_connectionInfo.Connections = 2;


            _telescope.Connected = true;

            _sharedResourcesWrapperMock.Verify(x => x.SendChar("P", false), Times.Never);
            _sharedResourcesWrapperMock.Verify(x => x.IsLongFormat, Times.Once);
        }

        [Test]
        public void IsLongFormat_WhenHighPrecisionNotSupportedAndSecondConnectionMade_ThenDigitPrecisionValuesArePreserved()
        {
            var ra = 12d;
            var dec = 34d;

            _utilMock.Setup(x => x.HoursToHM(ra, ":", "", 1)).Returns(ra + "HM");
            _utilMock.Setup(x => x.DegreesToDM(dec, "*", "", 0)).Returns(dec + "DM");

            ConnectTelescope(TelescopeList.LX200CLASSIC);

            _telescope.TargetRightAscension = ra;
            _telescope.TargetDeclination = dec;

            Assert.That(_connectionInfo.SameDevice, Is.EqualTo(1));

            var secondTelescopeInstance =
                new ASCOM.Meade.net.Telescope(_utilMock.Object, _utilExtraMock.Object, _astroUtilsMock.Object,
                    _sharedResourcesWrapperMock.Object, _astroMathsMock.Object, _clockMock.Object, _novasMock.Object);

            Assert.That(secondTelescopeInstance.Connected, Is.False);

            _connectionInfo.SameDevice = 2;
            secondTelescopeInstance.Connected = true;

            secondTelescopeInstance.TargetRightAscension = ra;
            secondTelescopeInstance.TargetDeclination = dec;

            _utilMock.Verify(x => x.HoursToHM(ra, ":", "", 1), Times.Exactly(2));
            _utilMock.Verify(x => x.DegreesToDM(dec, "*", "", 0), Times.Exactly(2));
            _utilMock.Verify(x => x.HoursToHMS(It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 2), Times.Never);
            _utilMock.Verify(x => x.DegreesToDMS(It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 2), Times.Never);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void SlewSettleTime_WhenSecondConnectionMade_ThenSlewSettleTimeIsPreserved(short slewSettleTime)
        {
            ConnectTelescope();

            _telescope.SlewSettleTime = slewSettleTime;

            Assert.That(_connectionInfo.SameDevice, Is.EqualTo(1));

            var secondTelescopeInstance =
                new ASCOM.Meade.net.Telescope(_utilMock.Object, _utilExtraMock.Object, _astroUtilsMock.Object,
                    _sharedResourcesWrapperMock.Object, _astroMathsMock.Object, _clockMock.Object, _novasMock.Object);

            Assert.That(secondTelescopeInstance.Connected, Is.False);

            _connectionInfo.SameDevice = 2;
            secondTelescopeInstance.Connected = true;

            Assert.That(secondTelescopeInstance.SlewSettleTime, Is.EqualTo(slewSettleTime));
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

        [TestCase(TelescopeList.Autostar497_30Ee, false)]
        [TestCase(TelescopeList.Autostar497_43Eg, true)]
        public void CanSetTracking_Get_ReturnsTrueIffGWCommandIsSupported(string firmware, bool supported)
        {
            ConnectTelescope(firmwareVersion: firmware);

            var result = _telescope.CanSetTracking;

            Assert.That(result, Is.EqualTo(supported));
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
        public void CanUnpark_NotConnected_ThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() =>
            {
                var result = _telescope.CanUnpark;
            });

            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: CanUnpark"));
        }

        [TestCase(TelescopeList.LX200GPS, TelescopeList.LX200GPS_42G, true)]
        [TestCase(TelescopeList.Autostar497, TelescopeList.Autostar497_43Eg, false)]
        public void CanUnpark_Get_ReturnsExpectedValue(string productVersion, string firmware, bool expectedResult)
        {
            ConnectTelescope(productVersion, firmware);

            var result = _telescope.CanUnpark;

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void Unpark_NotConnect_ThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() =>
            {
                _telescope.Unpark();
            });

            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: Unpark"));
        }

        [TestCase(TelescopeList.LX200GPS, TelescopeList.LX200GPS_42G, true)]
        [TestCase(TelescopeList.Autostar497, TelescopeList.Autostar497_43Eg, false)]
        public void Unpark_ThenDoesNotThrowException(string productVersion, string firmware, bool canUnPark)
        {
            ConnectTelescope(productVersion, firmware);

            if (canUnPark)
                Assert.DoesNotThrow(() => _telescope.Unpark());
            else
            {
                var exception = Assert.Throws<ASCOM.InvalidOperationException>(() => _telescope.Unpark());

                Assert.That(exception.Message, Is.EqualTo("Unable to unpark this telescope type"));
            }
        }

        [Test]
        public void Declination_Get_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() =>
            {
                var actualResult = _telescope.Declination;
                Assert.Fail($"{actualResult} should not have returned");
            });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: Declination Get"));
        }

        [TestCase("s12*34")]
        [TestCase("s12*34’56")]
        public void Declination_Get_WhenConnected_ThenReadsValueFromScope(string declincationString)
        {
            var expectedResult = 12.34;
            _sharedResourcesWrapperMock.Setup(x => x.SendString("GD", false)).Returns(declincationString);
            _utilMock.Setup(x => x.DMSToDegrees(declincationString)).Returns(expectedResult);

            ConnectTelescope();

            var actualResult = _telescope.Declination;
            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }

        [Test]
        public void Declination_Get_WhenConnected_ThenReturnsExpectedResult()
        {
            var telescopeDecResult = "s12*34’56";
            var dmsResult = 1.2;

            _sharedResourcesWrapperMock.Setup(x => x.SendString("GD", false)).Returns(telescopeDecResult);
            _utilMock.Setup(x => x.DMSToDegrees(telescopeDecResult)).Returns(dmsResult);

            ConnectTelescope();

            var result = _telescope.Declination;

            _sharedResourcesWrapperMock.Verify(x => x.SendString("GD", false), Times.Exactly(2));
            _utilMock.Verify(x => x.DMSToDegrees(telescopeDecResult), Times.Exactly(2));

            Assert.That(result, Is.EqualTo(dmsResult));
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
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => _telescope.DeclinationRate = 0);

            Assert.That(excpetion.Property, Is.EqualTo("DeclinationRate"));
            Assert.That(excpetion.AccessorSet, Is.True);
        }

        [Test]
        public void DestinationSideOfPier_WhenNotConnected_ThenThrowsException()
        {
            var excpetion = Assert.Throws<NotConnectedException>(() =>
            {
                var result = _telescope.DestinationSideOfPier(0, 0);
                Assert.Fail($"{result} should not have returned");
            });

            Assert.That(excpetion.Message, Is.EqualTo("Not connected to telescope when trying to execute: DestinationSideOfPier"));
        }

        [TestCase(1, 1, 4, PierSide.pierEast)]
        [TestCase(1, -1, 4, PierSide.pierEast)]
        [TestCase(4, 1, 1, PierSide.pierWest)]
        [TestCase(4, -1, 1, PierSide.pierWest)]
        [TestCase(0, 0, 0, PierSide.pierUnknown)]
        [TestCase(5, 0, 5, PierSide.pierUnknown)]
        [TestCase(23.8, 1, 23.9, PierSide.pierEast)]
        [TestCase(23.8, -1, 23.9, PierSide.pierEast)]
        [TestCase(23.9, 1, 1, PierSide.pierEast)]
        [TestCase(23.9, -1, 1, PierSide.pierEast)]
        [TestCase(1, 1, 23.9, PierSide.pierWest)]
        [TestCase(1, -1, 23.9, PierSide.pierWest)]
        public void DestinationSideOfPier_WhenHASiderealTimeDiffIsNotNull_ThenSideOfPierIsCalculated(double ra, double dec, double siderealTime, PierSide expectedDSOP)
        {
            // given

            // deterministic start
            _sharedResourcesWrapperMock.Object.SideOfPier = PierSide.pierUnknown;

            // SideralTime uses ConditionRA to normalize to [0..24h), so we use it to mock the property
            _astroUtilsMock.Setup(x => x.ConditionRA(It.IsAny<double>())).Returns(siderealTime);

            var ha = siderealTime - ra;
            // normalized hour angle range is [-12h..12h]
            var normalisedHA = ha > 12 ? ha - 24 : ha < -12 ? ha + 24 : ha;
            _astroUtilsMock.Setup(x => x.ConditionHA(It.Is<double>(v => v == ha))).Returns(normalisedHA);

            ConnectTelescope();

            // when
            var actualDSOP = _telescope.DestinationSideOfPier(ra, dec);

            // then
            Assert.That(siderealTime, Is.InRange(0, 24));
            Assert.That(normalisedHA, Is.InRange(-12, 12 + double.Epsilon));
            Assert.That(actualDSOP, Is.EqualTo(expectedDSOP));

            _astroUtilsMock.Verify(x => x.ConditionRA(It.IsAny<double>()), Times.Once);
            _astroUtilsMock.Verify(x => x.ConditionHA(It.Is<double>(v => v == ha)), Times.Once);
        }

        [Test]
        public void SiderealTime_Get_WhenNotConnected_ThenThrowsException()
        {
            // when
            var exception = Assert.Throws<NotConnectedException>(() =>
            {
                var result = _telescope.SiderealTime;
                Assert.Fail($"{result} should not have returned");
            });

            // then
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SiderealTime Get"));
        }

        [Test]
        public void SiderealTime_Get_WhenNOVASErrors_ThenThrowsException()
        {
            // given
            double gst = 0.0;
            _novasMock
                .Setup(x => x.SiderealTime(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), ASCOM.Astrometry.GstType.GreenwichApparentSiderealTime, ASCOM.Astrometry.Method.EquinoxBased, ASCOM.Astrometry.Accuracy.Reduced, ref gst))
                .Returns(3)
                .Verifiable();

            ConnectTelescope();

            // when
            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                var result = _telescope.SiderealTime;
                Assert.Fail($"{result} should not have returned");
            });

            // then
            Assert.That(exception.Message, Is.EqualTo("NOVAS 3.1 SiderealTime returned: 3 in SiderealTime"));
            _novasMock.Verify();
        }

        [Test]
        public void DoesRefraction_Get_ThenThrowsException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() =>
            {
                var result = _telescope.DoesRefraction;
                Assert.Fail($"{result} should not have returned");
            });

            Assert.That(excpetion.Property, Is.EqualTo("DoesRefraction"));
            Assert.That(excpetion.AccessorSet, Is.False);
        }

        [Test]
        public void DoesRefraction_Set_ThenThrowsException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => _telescope.DoesRefraction = true);

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
            var excpetion = Assert.Throws<MethodNotImplementedException>(() => _telescope.FindHome());

            Assert.That(excpetion.Method, Is.EqualTo("FindHome"));
        }

        [Test]
        public void FocalLength_Get_ThenThrowsException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() =>
            {
                var result = _telescope.FocalLength;
                Assert.Fail($"{result} should not have returned");
            });

            Assert.That(excpetion.Property, Is.EqualTo("FocalLength"));
            Assert.That(excpetion.AccessorSet, Is.False);
        }

        [Test]
        public void GuideRateDeclination_Get_ThenThrowsException()
        {
            var result = _telescope.GuideRateDeclination;

            Assert.That(result, Is.EqualTo(0.00034166666666666666));
        }

        [Test]
        public void GuideRateDeclination_Set_WhenNotSupported_ThenThrowsException()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => TelescopeList.Autostar497);

            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => _telescope.GuideRateDeclination = 0);

            Assert.That(excpetion.Property, Is.EqualTo("GuideRateDeclination"));
            Assert.That(excpetion.AccessorSet, Is.True);
        }

        [Test]
        public void GuideRateDeclination_Set_WhenIsSupported_ThenSetsNewGuideRate()
        {
            var newGuideRate = 0.00034166666666666666;

            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => TelescopeList.LX200GPS);

            _telescope.GuideRateDeclination = newGuideRate;

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("Rg01.2", false), Times.Once);

            Assert.That(_telescope.GuideRateDeclination, Is.EqualTo(newGuideRate));
        }

        [Test]
        public void GuideRateRightAscension_Get_ThenThrowsException()
        {
            var result = _telescope.GuideRateRightAscension;

            Assert.That(result, Is.EqualTo(0.00034166666666666666));
        }

        [Test]
        public void GuideRateRightAscension_Set_WhenNotSupported_ThenThrowsException()
        {
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => TelescopeList.Autostar497);

            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => _telescope.GuideRateRightAscension = 0);

            Assert.That(excpetion.Property, Is.EqualTo("GuideRateRightAscension"));
            Assert.That(excpetion.AccessorSet, Is.True);
        }

        [Test]
        public void GuideRateRightAscension_Set_WhenIsSupported_ThenSetsNewGuideRate()
        {
            var newGuideRate = 0.00034166666666666666;

            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => TelescopeList.LX200GPS);

            _telescope.GuideRateRightAscension = newGuideRate;

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("Rg01.2", false), Times.Once);

            Assert.That(_telescope.GuideRateDeclination, Is.EqualTo(newGuideRate));
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
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.MoveAxis(TelescopeAxes.axisPrimary, 0));
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: MoveAxis"));
        }

        [TestCase(0, "", TelescopeAxes.axisPrimary)]
        [TestCase(1, "RG", TelescopeAxes.axisPrimary)]
        [TestCase(-1, "RG", TelescopeAxes.axisPrimary)]
        [TestCase(2, "RC", TelescopeAxes.axisPrimary)]
        [TestCase(-2, "RC", TelescopeAxes.axisPrimary)]
        [TestCase(3, "RM", TelescopeAxes.axisPrimary)]
        [TestCase(-3, "RM", TelescopeAxes.axisPrimary)]
        [TestCase(4, "RS", TelescopeAxes.axisPrimary)]
        [TestCase(-4, "RS", TelescopeAxes.axisPrimary)]
        [TestCase(0, "", TelescopeAxes.axisSecondary)]
        [TestCase(1, "RG", TelescopeAxes.axisSecondary)]
        [TestCase(-1, "RG", TelescopeAxes.axisSecondary)]
        [TestCase(2, "RC", TelescopeAxes.axisSecondary)]
        [TestCase(-2, "RC", TelescopeAxes.axisSecondary)]
        [TestCase(3, "RM", TelescopeAxes.axisSecondary)]
        [TestCase(-3, "RM", TelescopeAxes.axisSecondary)]
        [TestCase(4, "RS", TelescopeAxes.axisSecondary)]
        [TestCase(-4, "RS", TelescopeAxes.axisSecondary)]
        public void MoveAxis_WhenConnected_ThenExecutesCorrectCommandSequence(double rate, string slewRateCommand, TelescopeAxes axis)
        {
            ConnectTelescope();

            _telescope.MoveAxis(axis, rate);

            if (slewRateCommand != string.Empty)
                _sharedResourcesWrapperMock.Verify(x => x.SendBlind(slewRateCommand, false), Times.Once);
            else
            {
                _sharedResourcesWrapperMock.Verify(x => x.SendBlind("RG", false), Times.Never);
                _sharedResourcesWrapperMock.Verify(x => x.SendBlind("RC", false), Times.Never);
                _sharedResourcesWrapperMock.Verify(x => x.SendBlind("RM", false), Times.Never);
                _sharedResourcesWrapperMock.Verify(x => x.SendBlind("RS", false), Times.Never);
            }

            switch (axis)
            {
                case TelescopeAxes.axisPrimary:
                    switch (rate.Compare(0))
                    {
                        case ComparisonResult.Equals:
                            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("Qe", false), Times.Once);
                            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("Qw", false), Times.Once);
                            break;
                        case ComparisonResult.Greater:
                            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("Me", false), Times.Once);
                            break;
                        case ComparisonResult.Lower:
                            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("Mw", false), Times.Once);
                            break;
                    }
                    break;
                case TelescopeAxes.axisSecondary:
                    switch (rate.Compare(0))
                    {
                        case ComparisonResult.Equals:
                            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("Qn", false), Times.Once);
                            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("Qs", false), Times.Once);
                            break;
                        case ComparisonResult.Greater:
                            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("Mn", false), Times.Once);
                            break;
                        case ComparisonResult.Lower:
                            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("Ms", false), Times.Once);
                            break;
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

            var exception = Assert.Throws<InvalidValueException>(() => _telescope.MoveAxis(TelescopeAxes.axisTertiary, testRate));

            Assert.That(exception.Message, Is.EqualTo($"Rate {testRate} not supported"));
        }

        [Test]
        public void MoveAxis_WhenTertiaryAxis_ThenThrowsException()
        {
            var testRate = 0;

            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => _telescope.MoveAxis(TelescopeAxes.axisTertiary, testRate));

            Assert.That(exception.Message, Is.EqualTo("Can not move this axis."));
        }

        [Test]
        public void Park_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.Park());
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: Park"));
        }

        [Test]
        public void Park_WhenNotParked_ThenSendsParkCommand()
        {
            ConnectTelescope();
            Assert.That(_telescope.AtPark, Is.False);
            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("hP", false), Times.Never);

            _telescope.Park();

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("hP", false), Times.Once);
            Assert.That(_telescope.AtPark, Is.True);
        }

        [Test]
        public void Park_WhenParked_ThenDoesNothing()
        {
            ConnectTelescope();

            _telescope.Park();

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("hP", false), Times.Once);
            Assert.That(_telescope.AtPark, Is.True);


            //act
            _telescope.Park();

            //no change from previous state.
            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("hP", false), Times.Once);
            Assert.That(_telescope.AtPark, Is.True);
        }

        [Test]
        public void PulseGuide_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.PulseGuide(GuideDirections.guideEast, 0));
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

            _telescope.PulseGuide(direction, duration);

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

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind($"Mg{d}{duration:0000}", false));
            _utilMock.Verify(x => x.WaitForMilliseconds(duration), Times.Once);
        }

        [TestCase(GuideDirections.guideEast)]
        [TestCase(GuideDirections.guideWest)]
        [TestCase(GuideDirections.guideNorth)]
        [TestCase(GuideDirections.guideSouth)]
        public void PulseGuide_WhenSlewingAndPulseGuideAttempted_ThenThrowsExpectedException(GuideDirections direction)
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendString("D", false)).Returns("|");

            var duration = 0;
            ConnectTelescope();

            var exception = Assert.Throws<InvalidOperationException>(() => _telescope.PulseGuide(direction, duration));

            Assert.That(exception.Message, Is.EqualTo("Unable to PulseGuide whilst slewing to target."));
        }

        [TestCase(GuideDirections.guideEast, TelescopeAxes.axisPrimary)]
        [TestCase(GuideDirections.guideWest, TelescopeAxes.axisPrimary)]
        [TestCase(GuideDirections.guideNorth, TelescopeAxes.axisSecondary)]
        [TestCase(GuideDirections.guideSouth, TelescopeAxes.axisSecondary)]
        public void PulseGuide_WhenMovingAxisAndPulseGuideAttempted_ThenThrowsExpectedException(GuideDirections direction, TelescopeAxes axes)
        {
            _sharedResourcesWrapperMock.SetupProperty(x => x.MovingPrimary);
            _sharedResourcesWrapperMock.SetupProperty(x => x.MovingSecondary);
            _sharedResourcesWrapperMock.SetupProperty(x => x.EarliestNonSlewingTime, DateTime.MinValue);
            _sharedResourcesWrapperMock.Setup(x => x.SendString("D", false)).Returns("");

            var duration = 0;
            ConnectTelescope();

            _telescope.MoveAxis(axes, 1);

            var exception = Assert.Throws<InvalidOperationException>(() => _telescope.PulseGuide(direction, duration));

            Assert.That(exception.Message, Is.EqualTo("Unable to PulseGuide while moving same axis."));
            Assert.That(_sharedResourcesWrapperMock.Object.MovingPrimary, Is.EqualTo(axes == TelescopeAxes.axisPrimary));
            Assert.That(_sharedResourcesWrapperMock.Object.MovingSecondary, Is.EqualTo(axes == TelescopeAxes.axisSecondary));
        }

        [TestCase(GuideDirections.guideEast)]
        [TestCase(GuideDirections.guideWest)]
        [TestCase(GuideDirections.guideNorth)]
        [TestCase(GuideDirections.guideSouth)]
        public void PulseGuide_WhenConnectedAndNewerPulseGuidingNotAvailable_ThenIsSlewingRespondsFalse(GuideDirections direction)
        {
            var telescopeDecResult = "s12*34’56";
            var dmsResult = 1.2;

            _testProperties.rightAscension = 1.3;

            _sharedResourcesWrapperMock.Setup(x => x.SendString("GD", false)).Returns(telescopeDecResult);
            _utilMock.Setup(x => x.DMSToDegrees(telescopeDecResult)).Returns(dmsResult);

            var duration = 0;
            _sharedResourcesWrapperMock.Setup(x => x.ProductName).Returns(() => TelescopeList.Autostar497);
            _sharedResourcesWrapperMock.Setup(x => x.FirmwareVersion).Returns(() => TelescopeList.Autostar497_30Ee);

            var isSlewing = true;
            _utilMock.Setup(x => x.WaitForMilliseconds(duration)).Callback(() =>
                {
                    isSlewing = _telescope.Slewing;
                });

            ConnectTelescope();

            _telescope.PulseGuide(direction, duration);

            Assert.That(isSlewing, Is.False);
        }

        [TestCase(GuideDirections.guideEast)]
        [TestCase(GuideDirections.guideWest)]
        [TestCase(GuideDirections.guideNorth)]
        [TestCase(GuideDirections.guideSouth)]
        public void PulseGuide_WhenConnectedAndNewerPulseGuidingNotAvailable_ThenSendsOldCommandsAndWaits(GuideDirections direction)
        {
            var telescopeDecResult = "s12*34’56";
            var dmsResult = 1.2;
            var duration = 0;

            _sharedResourcesWrapperMock.Setup(x => x.SendString("GD", false)).Returns(telescopeDecResult);
            _utilMock.Setup(x => x.DMSToDegrees(telescopeDecResult)).Returns(dmsResult);

            ConnectTelescope(TelescopeList.Autostar497, TelescopeList.Autostar497_30Ee);

            _telescope.PulseGuide(direction, duration);

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

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("RG", false));
            _sharedResourcesWrapperMock.Verify(x => x.SendBlind($"M{d}", false));
            _utilMock.Verify(x => x.WaitForMilliseconds(duration), Times.Once);
            _sharedResourcesWrapperMock.Verify(x => x.SendBlind($"Q{d}", false));
        }

        [TestCase(GuideDirections.guideEast)]
        [TestCase(GuideDirections.guideWest)]
        [TestCase(GuideDirections.guideNorth)]
        [TestCase(GuideDirections.guideSouth)]
        public void PulseGuide_WhenConnectedAndNewerPulseGuidingNotAvailable_ThenSendsOldCommandsAndDoesNotWaitForExtraSettleTime(GuideDirections direction)
        {
            short slewSettleTime = 10;
            _profileProperties.SettleTime = slewSettleTime;

            var duration = 0;
            var telescopeDecResult = "s12*34’56";
            var dmsResult = 1.2;

            _sharedResourcesWrapperMock.Setup(x => x.SendString("GD", false)).Returns(telescopeDecResult);
            _utilMock.Setup(x => x.DMSToDegrees(telescopeDecResult)).Returns(dmsResult);

            ConnectTelescope(TelescopeList.Autostar497, TelescopeList.Autostar497_30Ee);

            _telescope.PulseGuide(direction, duration);

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

            _clockMock.Verify(x => x.UtcNow, Times.Never);
        }

        [TestCase(GuideDirections.guideEast)]
        [TestCase(GuideDirections.guideWest)]
        [TestCase(GuideDirections.guideNorth)]
        [TestCase(GuideDirections.guideSouth)]
        public void PulseGuide_WhenConnectedAndNewerPulseGuidingAvailableButDurationTooLong_ThenSendsOldCommandsAndWaits(GuideDirections direction)
        {
            var duration = 10000;
            ConnectTelescope(TelescopeList.Autostar497, TelescopeList.Autostar497_30Ee);

            _telescope.PulseGuide(direction, duration);

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

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("RG", false));
            _sharedResourcesWrapperMock.Verify(x => x.SendBlind($"M{d}", false));
            _utilMock.Verify(x => x.WaitForMilliseconds(duration), Times.Once);
            _sharedResourcesWrapperMock.Verify(x => x.SendBlind($"Q{d}", false));
        }

        [Test]
        public void RightAscension_Get_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() =>
            {
                var result = _telescope.RightAscension;
                Assert.Fail($"{result} should not have returned");
            });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: RightAscension Get"));
        }

        [Test]
        public void RightAscension_Get_WhenConnected_ThenReturnsExpectedResult()
        {
            ConnectTelescope();

            var result = _telescope.RightAscension;

            Assert.That(result, Is.EqualTo(_testProperties.rightAscension));
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
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => _telescope.RightAscensionRate = 1);

            Assert.That(excpetion.Property, Is.EqualTo("RightAscensionRate"));
            Assert.That(excpetion.AccessorSet, Is.True);
        }

        [Test]
        public void SetPark_ThenThrowsException()
        {
            var excpetion = Assert.Throws<MethodNotImplementedException>(() => _telescope.SetPark());

            Assert.That(excpetion.Method, Is.EqualTo("SetPark"));
        }

        [TestCase(TelescopeList.LX200CLASSIC, null)]
        [TestCase(TelescopeList.Autostar497, TelescopeList.Autostar497_31Ee)]
        [TestCase(TelescopeList.LX200GPS, TelescopeList.LX200GPS_42F)]
        public void SideOfPier_Get_WhenMeridianFlipNotSupported_ThenThrowsException(string model, string firmware)
        {
            ConnectTelescope(model, firmware);

            var excpetion = Assert.Throws<PropertyNotImplementedException>(() =>
            {
                var result = _telescope.SideOfPier;
                Assert.Fail($"{result} should not have returned");
            });

            Assert.That(excpetion.Property, Is.EqualTo("SideOfPier"));
            Assert.That(excpetion.AccessorSet, Is.False);
        }

        [TestCase(TelescopeList.Autostar497, TelescopeList.Autostar497_43Eg)]
        [TestCase(TelescopeList.LX200GPS, TelescopeList.LX200GPS_42G)]
        public void SideOfPier_Get_WhenMeridianFlipSupported_ThenReturnsResult(string model, string firmware)
        {
            ConnectTelescope(model, firmware);

            Assert.DoesNotThrow(() => { var result = _telescope.SideOfPier; });
        }

        [TestCase(TelescopeList.Autostar497, TelescopeList.Autostar497_43Eg, AlignmentModes.algAltAz, 'A')]
        [TestCase(TelescopeList.Autostar497, TelescopeList.Autostar497_43Eg, AlignmentModes.algPolar, 'P')]
        public void SideOfPier_Get_WhenMeridianFlipNotSupportedByAlignementMode_ThenThrowsException(string model, string firmware, AlignmentModes alignmode, char alignmentStatus)
        {
            ConnectTelescope(model, firmware);
            _testProperties.AlignmentStatus = new[] { alignmentStatus, 'T', '1' };

            var excpetion = Assert.Throws<PropertyNotImplementedException>(() =>
            {
                var result = _telescope.SideOfPier;
                Assert.Fail($"{result} should not have returned");
            });

            Assert.That(excpetion.Property, Is.EqualTo("SideOfPier"));
            Assert.That(excpetion.AccessorSet, Is.False);
        }

        [TestCase(TelescopeList.Autostar497, TelescopeList.Autostar497_43Eg, AlignmentModes.algGermanPolar, 'G')]
        public void SideOfPier_Get_WhenMeridianFlipSupportedByAlignementMode_ThenDoesNotThrow(string model, string firmware, AlignmentModes alignmode, char alignmentStatus)
        {
            ConnectTelescope(model, firmware);
            _testProperties.AlignmentStatus = new[] { alignmentStatus, 'T', '1' };

            Assert.DoesNotThrow(() => { var result = _telescope.SideOfPier; });
        }

        [Test]
        public void SideOfPier_Set_ThenThrowsException()
        {
            var excpetion = Assert.Throws<PropertyNotImplementedException>(() => _telescope.SideOfPier = 0);

            Assert.That(excpetion.Property, Is.EqualTo("SideOfPier"));
            Assert.That(excpetion.AccessorSet, Is.True);
        }

        [TestCase(0, 34, PierSide.pierEast)]
        [TestCase(12, 34, PierSide.pierEast)]
        [TestCase(23.4, 34, PierSide.pierWest)]
        public void SideOfPier_WhenSecondConnectionMade_ThenValueIsPreserved(double ra, double dec, PierSide expectedPierSide)
        {
            var isSlewing = new[] { false };
            _sharedResourcesWrapperMock.Setup(x => x.SendChar("MS", false)).Returns("0").Callback(() => { isSlewing[0] = true; });
            _sharedResourcesWrapperMock.Setup(x => x.SendString("D", false)).Returns(() => isSlewing[0] ? "|" : "");
            _utilMock.Setup(x => x.HMSToHours(null)).Returns(ra);
            _utilMock.Setup(x => x.DMSToDegrees(null)).Returns(dec);
            _astroUtilsMock.Setup(x => x.ConditionHA(It.IsAny<double>())).Returns<double>(pHA => pHA < -12 ? pHA + 12 : pHA > 12 ? pHA - 12 : pHA);
            _astroUtilsMock.Setup(x => x.ConditionRA(It.IsAny<double>())).Returns<double>(pRA => pRA < 0 ? pRA + 24 : pRA >= 24 ? pRA - 24 : pRA);

            ConnectTelescope(firmwareVersion: TelescopeList.Autostar497_43Eg);
            Assert.That(_connectionInfo.SameDevice, Is.EqualTo(1));

            _testProperties.rightAscension = ra;
            _testProperties.declination = dec;
            _telescope.SlewToCoordinatesAsync(ra, dec);
            isSlewing[0] = false;
            var sideOfPierAfterSlew = _telescope.SideOfPier;

            Assert.That(sideOfPierAfterSlew, Is.EqualTo(expectedPierSide));

            var secondTelescopeInstance =
                new ASCOM.Meade.net.Telescope(_utilMock.Object, _utilExtraMock.Object, _astroUtilsMock.Object,
                    _sharedResourcesWrapperMock.Object, _astroMathsMock.Object, _clockMock.Object, _novasMock.Object);

            Assert.That(secondTelescopeInstance.Connected, Is.False);

            _connectionInfo.SameDevice = 2;
            secondTelescopeInstance.Connected = true;

            Assert.That(secondTelescopeInstance.SideOfPier, Is.EqualTo(sideOfPierAfterSlew));

            _sharedResourcesWrapperMock.Verify(x => x.SendChar("MS", false), Times.Once);
            _sharedResourcesWrapperMock.Verify(x => x.SendString("D", false), Times.AtLeast(2));
            _utilMock.Verify(x => x.HMSToHours(null), Times.Once);
            _utilMock.Verify(x => x.DMSToDegrees(null), Times.AtLeast(2));
            _astroUtilsMock.Verify(x => x.ConditionHA(It.IsAny<double>()), Times.Once);
            _astroUtilsMock.Verify(x => x.ConditionRA(It.IsAny<double>()), Times.Once);
        }

        delegate void NovasSiderealTimeDelegate(double jdHigh, double jdLow, double jdDelta, GstType gstType, Method method, Accuracy accuracy, ref double sideralTime);

        /// <summary>
        /// Test cases obtained via .NET telescope simulator
        /// </summary>
        [TestCase(9.4337648353882, -76.7178112042103, "2021-06-07T05:23:41.7610000Z", 8.13556526999591, 145.166333333333, 2d, PierSide.pierWest, PierSide.pierEast)]
        [TestCase(10.1581570159006, 11.8639491368916, "2021-06-07T10:59:19.7000000Z", 9.72726050605156, 145.166333333333, 1d, PierSide.pierWest, PierSide.pierEast)]
        [TestCase(9.66583199112222, 81.2310578173083, "2021-06-07T11:19:24.5540000Z", 7.73744116673785, 110.285166666667, 2d, PierSide.pierWest, PierSide.pierEast)]
        [TestCase(8.32978972808615, -29.816491813155, "2021-06-07T11:29:33.7040000Z", 7.90712206850482, 110.285166666667, 1d, PierSide.pierWest, PierSide.pierEast)]
        [TestCase(1.76405553984887, 60.7756226366989, "2021-06-07T11:34:07.5270000Z", 0.214893775091689, -6.24266666666667, 2d, PierSide.pierWest, PierSide.pierEast)]
        [TestCase(0.523375885742411, -33.1288722052936, "2021-06-07T11:38:25.1670000Z", 0.286661722506396, -6.24266666666667, 0.5d, PierSide.pierWest, PierSide.pierEast)]
        public void SideOfPier_WhenTrackingThroughMeridianAfterSubsequentGoto_ThenAMeridianFlipIsPerformed(
            double ra,
            double dec,
            string jnowTimeStr, /* JNOW of object before transit */
            double jnowSiderealTime,
            double siteLongitude,
            double trackingTimeHours,
            PierSide pierSideBeforeTransit,
            PierSide pierSideAfterRetargeting
        )
        {
            // given
            var jnowTime = DateTimeOffset.ParseExact(jnowTimeStr, "o", CultureInfo.InvariantCulture).UtcDateTime;
            var trackingTimeDiff = TimeSpan.FromHours(trackingTimeHours);
            var timeAfterTracking = jnowTime + trackingTimeDiff;
            var raAsHMS = ra + "HMS";
            var decAsDMS = dec + "DMS";
            var currentTime = jnowTime;
            var isSlewing = new[] { false };

            _clockMock.Setup(x => x.UtcNow).Returns(() => currentTime);

            // setup slewing
            _sharedResourcesWrapperMock.Setup(x => x.SendChar("MS", false)).Returns("0").Callback(() => { SetSlewing(true); });
            _sharedResourcesWrapperMock.Setup(x => x.SendString("D", false)).Returns(() => isSlewing[0] ? "|" : "");
            void SetSlewing(bool slewing) => isSlewing[0] = slewing;

            // setup for RA
            _utilMock.Setup(x => x.HoursToHMS(ra, ":", ":", ":", 2)).Returns(raAsHMS);
            _utilMock.Setup(x => x.HMSToHours(raAsHMS)).Returns(ra);

            // setup for DEC
            _utilMock.Setup(x => x.DMSToDegrees(decAsDMS)).Returns(dec);
            _utilMock.Setup(x => x.DegreesToDMS(dec, "*", ":", ":", 2)).Returns(decAsDMS);

            // setup for SiteLongitude
            var siteLongitudeResult = siteLongitude + "Gg";
            _sharedResourcesWrapperMock.Setup(x => x.SendString("Gg", false)).Returns(siteLongitudeResult);
            // remember to invert longitude
            _utilMock.Setup(x => x.DMSToDegrees(siteLongitudeResult)).Returns(-siteLongitude);

            // setup for SideralTime
            var siteLongitudeAdj = siteLongitude / 360.0 * 24.0;
            var jnowSiderealTimeWithoutLongAdj = jnowSiderealTime - siteLongitudeAdj;
            var afterTrackingSiderealTimeWithoutLongAdj = jnowSiderealTimeWithoutLongAdj + trackingTimeHours;

            _utilMock.Setup(x => x.DateUTCToJulian(It.IsAny<DateTime>())).Returns<DateTime>(ExpectedJD);

            _novasMock
                .Setup(x => x.SiderealTime(
                    It.Is<double>(v => v == Math.Floor(v)),
                    It.Is<double>(v => v > 0 && v < 1),
                    0d,
                    GstType.GreenwichApparentSiderealTime,
                    Method.EquinoxBased,
                    Accuracy.Reduced,
                    ref It.Ref<double>.IsAny))
                .Callback(new NovasSiderealTimeDelegate(NovasSiderealTime))
                .Returns(0);

            _astroUtilsMock.Setup(x => x.ConditionRA(It.IsAny<double>())).Returns<double>(pRA => pRA < 0 ? pRA + 24 : pRA >= 24 ? pRA - 24 : pRA);

            void NovasSiderealTime(double pJDHigh, double pJDLow, double pJDDelta, GstType pGSTType, Method pMethod, Accuracy pAccuracy, ref double pSideralTime)
            {
                if (IsExpectedJD(pJDHigh, pJDLow, jnowTime))
                {
                    pSideralTime = jnowSiderealTimeWithoutLongAdj;
                }
                else if (IsExpectedJD(pJDHigh, pJDLow, timeAfterTracking))
                {
                    pSideralTime = afterTrackingSiderealTimeWithoutLongAdj;
                }
                else
                {
                    Assert.Fail($"No sideral time defined for {pJDHigh}");
                }
            }

            // Setup DestinationSideOfPier
            _astroUtilsMock.Setup(x => x.ConditionHA(It.IsAny<double>())).Returns<double>(pHA => pHA < -12 ? pHA + 12 : pHA > 12 ? pHA - 12 : pHA);

            // Use firmware that supports GW
            ConnectTelescope(firmwareVersion: TelescopeList.Autostar497_43Eg);

            // when
            _testProperties.rightAscension = ra;
            _testProperties.declination = dec;
            _telescope.SlewToCoordinatesAsync(ra, dec);
            SetSlewing(false);
            var actualSideOfPierAfterSlew = _telescope.SideOfPier;
            // simulate tracking time
            currentTime += trackingTimeDiff;
            var actualSideOfPierAfterTracking = _telescope.SideOfPier;
            _telescope.SlewToTargetAsync();
            SetSlewing(false);
            var actualSideOfPierAfterRetargeting = _telescope.SideOfPier;

            // then
            Assert.That(_telescope.TargetRightAscension, Is.EqualTo(ra));
            Assert.That(_telescope.TargetDeclination, Is.EqualTo(dec));
            Assert.That(actualSideOfPierAfterSlew, Is.EqualTo(pierSideBeforeTransit));
            Assert.That(actualSideOfPierAfterTracking, Is.EqualTo(pierSideBeforeTransit));
            Assert.That(actualSideOfPierAfterRetargeting, Is.EqualTo(pierSideAfterRetargeting));

            _clockMock.Verify(x => x.UtcNow, Times.AtLeast(2));

            foreach (var time in new[] { jnowTime, timeAfterTracking })
            {
                _utilMock.Verify(x => x.DateUTCToJulian(time));

                var (high, low) = ExpectedHighAndLowJD(time);
                _novasMock
                    .Verify(x => x.SiderealTime(
                        high,
                        low,
                        0d,
                        GstType.GreenwichApparentSiderealTime,
                        Method.EquinoxBased,
                        Accuracy.Reduced,
                        ref It.Ref<double>.IsAny),
                    Times.Once);
            }

            _sharedResourcesWrapperMock.Verify(x => x.SendString("Gg", false), Times.Exactly(3));
            _sharedResourcesWrapperMock.Verify(x => x.SendChar("MS", false), Times.Exactly(2));
            _sharedResourcesWrapperMock.Verify(x => x.SendString("D", false), Times.AtLeast(3));

            double ExpectedJD(DateTime pExpectedTime) => pExpectedTime.Ticks * 0.0000001;
            (double High, double Low) ExpectedHighAndLowJD(DateTime pExpectedTime)
            {
                var expectedJD = ExpectedJD(pExpectedTime);
                var high = Math.Floor(expectedJD);
                return (high, expectedJD - high);
            }

            bool IsExpectedJD(double pJDHigh, double pJDLow, DateTime pExpectedTime)
            {
                var (high, low) = ExpectedHighAndLowJD(pExpectedTime);

                return pJDHigh == high && pJDLow == low;
            }
        }

        [Test]
        public void SiteElevation_Get_WhenNotConnectedThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() =>
            {
                var elevation = _telescope.SiteElevation;
            });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SiteElevation Get"));
        }

        [Test]
        public void SiteElevation_Get_WhenConnectedReturnsExpectedValue()
        {
            double expectedValue = 2000;

            _profileProperties.SiteElevation = expectedValue;

            ConnectTelescope();

            var result = _telescope.SiteElevation;
            Assert.That(result, Is.EqualTo(expectedValue));
        }

        [Test]
        public void SiteElevation_Set_WhenNotConnectedThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() =>
            {
                _telescope.SiteElevation = 1000;
            });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SiteElevation Set"));
        }

        [Test]
        public void SiteElevation_Set_WhenConnectedCanPersistNewValue()
        {
            double newElevation = 1000;

            double writtenSiteElevation = 0;
            _sharedResourcesWrapperMock.Setup(x => x.WriteProfile(It.IsAny<ProfileProperties>())).Callback<ProfileProperties>(
                profile =>
                {
                    writtenSiteElevation = profile.SiteElevation;
                });

            ConnectTelescope();

            _telescope.SiteElevation = newElevation;

            Assert.That(_telescope.SiteElevation, Is.EqualTo(newElevation));
            _sharedResourcesWrapperMock.Verify(x => x.WriteProfile(It.IsAny<ProfileProperties>()), Times.Once);
            Assert.That(writtenSiteElevation, Is.EqualTo(newElevation));
        }

        [Test]
        public void SlewSettleTime_Get_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() =>
            {
                var result = _telescope.SlewSettleTime;
                Assert.Fail($"{result} should not have returned");
            });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SlewSettleTime Get"));
        }

        [Test]
        public void SlewSettleTime_Set_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() =>
            {
                _telescope.SlewSettleTime = 13;
                Assert.Fail($"should not have returned");
            });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SlewSettleTime Set"));
        }

        [Test]
        public void SlewSettleTime_Get_ReturnsExpectedValue()
        {
            ConnectTelescope();

            var result = _telescope.SlewSettleTime;

            Assert.That(result, Is.EqualTo(0));
        }

        [TestCase(8)]
        [TestCase(12)]
        [TestCase(3)]
        public void SlewSettleTime_Set_ThenReturnsNewSettleTime(short settleTime)
        {
            _profileProperties.SettleTime = 0;

            ConnectTelescope();

            _telescope.SlewSettleTime = settleTime;

            var result = _telescope.SlewSettleTime;

            Assert.That(result, Is.EqualTo(settleTime));
        }

        [Test]
        public void SiteLatitude_Get_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() =>
            {
                var result = _telescope.SiteLatitude;
                Assert.Fail($"{result} should not have returned");
            });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SiteLatitude Get"));
        }

        [Test]
        public void SiteLatitude_Get_WhenConnected_ThenRetrievesAndReturnsExpectedValue()
        {
            ConnectTelescope();

            var result = _telescope.SiteLatitude;

            _sharedResourcesWrapperMock.Verify(x => x.SendString("Gt", false), Times.AtLeastOnce);

            Assert.That(result, Is.EqualTo(_testProperties.SiteLatitudeValue));
        }

        [Test]
        public void SiteLatitude_Set_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.SiteLatitude = 123.45);
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SiteLatitude Set"));
        }

        [Test]
        public void SiteLatitude_Set_WhenConnectedAndLatitudeIsGreaterThan90_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => _telescope.SiteLatitude = 90.01);
            Assert.That(exception.Message, Is.EqualTo("Latitude cannot be greater than 90 degrees."));
        }

        [Test]
        public void SiteLatitude_Set_WhenConnectedAndLatitudeIsLessThanNegative90_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => _telescope.SiteLatitude = -90.01);
            Assert.That(exception.Message, Is.EqualTo("Latitude cannot be less than -90 degrees."));
        }

        [TestCase(-10.5)]
        [TestCase(20.75)]
        public void SiteLatitude_Set_WhenValueSetAndTelescopRejects_ThenExceptionThrown(double siteLatitude)
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendChar(It.IsAny<string>(), false)).Returns("0");

            ConnectTelescope();

            var exception = Assert.Throws<InvalidOperationException>(() => _telescope.SiteLatitude = siteLatitude);

            Assert.That(exception.Message, Is.EqualTo("Failed to set site latitude."));
        }

        [TestCase(-10.5, "St-10*30")]
        [TestCase(20.75, "St+20*45")]
        public void SiteLatitude_Set_WhenValidValues_ThenValueSentToTelescope(double siteLatitude, string expectedCommand)
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendChar(expectedCommand, false)).Returns("1");

            ConnectTelescope();

            _telescope.SiteLatitude = siteLatitude;

            _sharedResourcesWrapperMock.Verify(x => x.SendChar(expectedCommand, false), Times.Once);
        }

        [Test]
        public void SiteLongitude_Get_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() =>
            {
                var result = _telescope.SiteLongitude;
                Assert.Fail($"{result} should not have returned");
            });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SiteLongitude Get"));
        }


        [TestCase("5", 5, -5)]
        [TestCase("-5", -5, 5)]
        [TestCase("185", 185, 175)]
        [TestCase("350", 350, 10)]
        public void SiteLongitude_Get_WhenConnected_ThenRetrivesAndReturnsExpectedValue(string telescopelongitudeString, double telescopeLongitudeValue, double expectedResult)
        {
            var telescopeLongitude = "testLongitude";

            _sharedResourcesWrapperMock.Setup(x => x.SendString("Gg", false)).Returns(telescopeLongitude);
            _utilMock.Setup(x => x.DMSToDegrees(telescopeLongitude)).Returns(telescopeLongitudeValue);

            ConnectTelescope();

            var result = _telescope.SiteLongitude;

            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void SiteLongitude_Set_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.SiteLongitude = 123.45);
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SiteLongitude Set"));
        }

        [Test]
        public void SiteLongitude_Set_WhenConnectedAndGreaterThan180_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => _telescope.SiteLongitude = 180.1);
            Assert.That(exception.Message, Is.EqualTo("Longitude cannot be greater than 180 degrees."));
        }

        [Test]
        public void SiteLongitude_Set_WhenConnectedAndLessThanNegative180_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => _telescope.SiteLongitude = -180.1);
            Assert.That(exception.Message, Is.EqualTo("Longitude cannot be lower than -180 degrees."));
        }

        [Test]
        public void SiteLongitude_Set_WhenConnectedAndTelescopeFails_ThenThrowsException()
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendChar(It.IsAny<string>(), false)).Returns("0");

            ConnectTelescope();


            var exception = Assert.Throws<InvalidOperationException>(() => _telescope.SiteLongitude = 10);
            Assert.That(exception.Message, Is.EqualTo("Failed to set site longitude."));
        }

        [TestCase(10, "Sg350*00")]
        public void SiteLongitude_Set_WhenConnectedAndTelescopeFails_ThenThrowsException(double longitude, string expectedCommand)
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendChar(expectedCommand, false)).Returns("1");

            ConnectTelescope();

            _telescope.SiteLongitude = longitude;

            _sharedResourcesWrapperMock.Verify(x => x.SendChar(expectedCommand, false), Times.Once);
        }

        [Test]
        public void SyncToAltAz_WhenConnected_ThenSendsExpectedMessage()
        {
            ConnectTelescope();

            var exception = Assert.Throws<MethodNotImplementedException>(() => _telescope.SyncToAltAz(0, 0));

            Assert.That(exception.Message, Is.EqualTo("Method SyncToAltAz is not implemented in this driver."));
        }

        [Test]
        public void SyncToTarget_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.SyncToTarget());
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SyncToTarget"));
        }

        [Test]
        public void SyncToTarget_WhenSyncToTargetFails_ThenThrowsException()
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendString("CM", false)).Returns(string.Empty);

            ConnectTelescope();

            var exception = Assert.Throws<InvalidOperationException>(() => _telescope.SyncToTarget());

            Assert.That(exception.Message, Is.EqualTo("Unable to perform sync"));
            _sharedResourcesWrapperMock.Verify(x => x.SendString("CM", false), Times.Once);
        }

        [Test]
        public void SyncToTarget_WhenSyncToTargetWorks_ThennoExceptionThrown()
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendString("CM", false)).Returns(" M31 EX GAL MAG 3.5 SZ178.0'#");

            ConnectTelescope();

            Assert.DoesNotThrow(() => _telescope.SyncToTarget());

            _sharedResourcesWrapperMock.Verify(x => x.SendString("CM", false), Times.Once);
        }

        [Test]
        public void TargetDeclination_Set_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.TargetDeclination = 0);
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: TargetDeclination Set"));
        }

        [Test]
        public void TargetDeclination_Set_WhenValueTooHigh_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => _telescope.TargetDeclination = 90.1);
            Assert.That(exception.Message, Is.EqualTo("Declination cannot be greater than 90."));
        }

        [Test]
        public void TargetDeclination_Set_WhenValueTooLow_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => _telescope.TargetDeclination = -90.1);
            Assert.That(exception.Message, Is.EqualTo("Declination cannot be less than -90."));
        }

        [Test]
        public void TargetDeclination_Set_WhenTelescopeReportsInvalidDec_ThenThrowsException()
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendChar(It.IsAny<string>(), false)).Returns("0");

            ConnectTelescope();

            var exception = Assert.Throws<InvalidOperationException>(() => _telescope.TargetDeclination = 50);
            Assert.That(exception.Message, Is.EqualTo("Target declination invalid"));
        }

        [TestCase(-30.5, "-30*30:00", "Sd-30*30:00")]
        [TestCase(30.5, "30*30:00", "Sd+30*30:00")]
        [TestCase(-75.25, "-75*15:00", "Sd-75*15:00")]
        [TestCase(50, "50*00:00", "Sd+50*00:00")]
        public void TargetDeclination_Set_WhenValueOK_ThenSetsNewTargetDeclination(double declination, string decstring, string commandString)
        {

            _utilMock.Setup(x => x.DegreesToDMS(declination, "*", ":", ":", 2)).Returns(decstring);
            _sharedResourcesWrapperMock.Setup(x => x.SendChar(commandString, false)).Returns("1");

            ConnectTelescope();

            _telescope.TargetDeclination = declination;

            _sharedResourcesWrapperMock.Verify(x => x.SendChar(commandString, false), Times.Once);
        }

        [Test]
        public void TargetDeclination_Get_WhenTargetNotSet_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                var result = _telescope.TargetDeclination;
                Assert.Fail($"{result} should not have returned");
            });
            Assert.That(exception.Message, Is.EqualTo("Target not set"));
        }

        [TestCase(50, "50*00:00", "Sd+50*00:00")]
        public void TargetDeclination_Get_WhenValueOK_ThenSetsNewTargetDeclination(double declination, string decstring, string commandString)
        {
            var digitsRA = 2;
            var telescopeDecResult = "s12*34’56";

            _utilMock.Setup(x => x.DegreesToDMS(declination, "*", ":", ":", digitsRA)).Returns(telescopeDecResult);
            _utilMock.Setup(x => x.DegreesToDMS(declination, "*", ":", ":", 2)).Returns(decstring);
            _utilMock.Setup(x => x.DMSToDegrees(decstring)).Returns(declination);

            _sharedResourcesWrapperMock.Setup(x => x.SendChar(commandString, false)).Returns("1");

            ConnectTelescope();

            _telescope.TargetDeclination = declination;

            var result = _telescope.TargetDeclination;

            Assert.That(result, Is.EqualTo(declination));
        }

        [TestCase(-90d)]
        [TestCase(-45d)]
        [TestCase(0d)]
        [TestCase(45d)]
        [TestCase(90d)]
        public void TargetDeclination_Set_WhenSecondConnectionMade_ThenValueIsPreserved(double targetDeclination)
        {
            var targetDeclinationDMS = targetDeclination + "DMS";
            var sign = targetDeclination >= 0 ? "+" : string.Empty;
            var command = $"Sd{sign}{targetDeclinationDMS}";

            _utilMock.Setup(x => x.DegreesToDMS(targetDeclination, "*", ":", ":", 2)).Returns(targetDeclinationDMS);
            _utilMock.Setup(x => x.DMSToDegrees(targetDeclinationDMS)).Returns(targetDeclination);
            _sharedResourcesWrapperMock.Setup(x => x.SendChar(command, false)).Returns("1");

            ConnectTelescope();
            Assert.That(_connectionInfo.SameDevice, Is.EqualTo(1));
            Assert.That(_sharedResourcesWrapperMock.Object.IsLongFormat, Is.True);

            _telescope.TargetDeclination = targetDeclination;

            Assert.That(_telescope.TargetDeclination, Is.EqualTo(targetDeclination));

            var secondTelescopeInstance =
                new ASCOM.Meade.net.Telescope(_utilMock.Object, _utilExtraMock.Object, _astroUtilsMock.Object,
                    _sharedResourcesWrapperMock.Object, _astroMathsMock.Object, _clockMock.Object, _novasMock.Object);

            Assert.That(secondTelescopeInstance.Connected, Is.False);

            _connectionInfo.SameDevice = 2;
            secondTelescopeInstance.Connected = true;

            Assert.That(_sharedResourcesWrapperMock.Object.IsLongFormat, Is.True);
            Assert.That(secondTelescopeInstance.TargetDeclination, Is.EqualTo(targetDeclination));

            _utilMock.Verify(x => x.DegreesToDMS(targetDeclination, "*", ":", ":", 2), Times.Once);
            _utilMock.Verify(x => x.DMSToDegrees(targetDeclinationDMS), Times.Once);
            _sharedResourcesWrapperMock.Verify(x => x.SendChar(command, false), Times.Once);
        }

        [Test]
        public void TargetRightAscension_Set_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.TargetRightAscension = 0);
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: TargetRightAscension Set"));
        }

        [Test]
        public void TargetRightAscension_Set_WhenValueTooHigh_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => _telescope.TargetRightAscension = 24);
            Assert.That(exception.Message, Is.EqualTo("Right ascension value cannot be greater than 23:59:59"));
        }

        [Test]
        public void TargetRightAscension_Set_WhenValueTooLow_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => _telescope.TargetRightAscension = -0.1);
            Assert.That(exception.Message, Is.EqualTo("Right ascension value cannot be below 0"));
        }

        [Test]
        public void TargetRightAscension_Set_WhenTelescopeReportsInvalidRA_ThenThrowsException()
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendChar(It.IsAny<string>(), false)).Returns("0");

            ConnectTelescope();

            var exception = Assert.Throws<InvalidOperationException>(() => _telescope.TargetRightAscension = 1);
            Assert.That(exception.Message, Is.EqualTo("Failed to set TargetRightAscension."));
        }

        [TestCase(5.5, "05:30:00", "Sr05:30:00")]
        [TestCase(10, "10:00:00", "Sr10:00:00")]
        public void TargetRightAscension_Set_WhenValueOK_ThenSetsNewTargetDeclination(double rightAscension, string hms, string commandString)
        {
            _utilMock.Setup(x => x.HoursToHMS(rightAscension, ":", ":", ":", 2)).Returns(hms);
            _sharedResourcesWrapperMock.Setup(x => x.SendChar(commandString, false)).Returns("1");

            ConnectTelescope();

            _telescope.TargetRightAscension = rightAscension;

            _sharedResourcesWrapperMock.Verify(x => x.SendChar(commandString, false), Times.Once);
        }

        [Test]
        public void TargetRightAscension_Get_WhenTargetNotSet_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                var result = _telescope.TargetRightAscension;
                Assert.Fail($"{result} should not have returned");
            });
            Assert.That(exception.Message, Is.EqualTo("Target not set"));
        }

        [TestCase(15, "15:00:00", "Sr15:00:00")]
        public void TargetRightAscension_Get_WhenValueOK_ThenSetsNewTargetDeclination(double rightAscension, string hms, string commandString)
        {
            var digitsRA = 2;

            _utilMock.Setup(x => x.HoursToHMS(rightAscension, ":", ":", ":", digitsRA)).Returns(hms);
            _utilMock.Setup(x => x.HMSToHours(hms)).Returns(rightAscension);

            _sharedResourcesWrapperMock.Setup(x => x.SendChar(commandString, false)).Returns("1");

            ConnectTelescope();

            _telescope.TargetRightAscension = rightAscension;

            var result = _telescope.TargetRightAscension;

            Assert.That(result, Is.EqualTo(rightAscension));
        }

        [TestCase(0d)]
        [TestCase(6d)]
        [TestCase(12d)]
        [TestCase(23.599d)]
        public void TargetRightAscension_Set_WhenSecondConnectionMade_ThenValueIsPreserved(double targetRightAscension)
        {
            var targetRightAscensionHMS = targetRightAscension + "HMS";
            var command = $"Sr{targetRightAscensionHMS}";

            _utilMock.Setup(x => x.HoursToHMS(targetRightAscension, ":", ":", ":", 2)).Returns(targetRightAscensionHMS);
            _utilMock.Setup(x => x.HMSToHours(targetRightAscensionHMS)).Returns(targetRightAscension);
            _sharedResourcesWrapperMock.Setup(x => x.SendChar(command, false)).Returns("1");

            ConnectTelescope();
            Assert.That(_connectionInfo.SameDevice, Is.EqualTo(1));
            Assert.That(_sharedResourcesWrapperMock.Object.IsLongFormat, Is.True);

            _telescope.TargetRightAscension = targetRightAscension;

            Assert.That(_telescope.TargetRightAscension, Is.EqualTo(targetRightAscension));

            var secondTelescopeInstance =
                new ASCOM.Meade.net.Telescope(_utilMock.Object, _utilExtraMock.Object, _astroUtilsMock.Object,
                    _sharedResourcesWrapperMock.Object, _astroMathsMock.Object, _clockMock.Object, _novasMock.Object);

            Assert.That(secondTelescopeInstance.Connected, Is.False);

            _connectionInfo.SameDevice = 2;
            secondTelescopeInstance.Connected = true;

            Assert.That(_sharedResourcesWrapperMock.Object.IsLongFormat, Is.True);
            Assert.That(secondTelescopeInstance.TargetRightAscension, Is.EqualTo(targetRightAscension));

            _utilMock.Verify(x => x.HoursToHMS(targetRightAscension, ":", ":", ":", 2), Times.Once);
            _utilMock.Verify(x => x.HMSToHours(targetRightAscensionHMS), Times.Once);
            _sharedResourcesWrapperMock.Verify(x => x.SendChar(command, false), Times.Once);
        }

        [Test]
        public void Tracking_Get_WhenDefault_ThenIsTrue()
        {
            Assert.That(_telescope.Tracking, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Tracking_Set_WhenCanSetTrackingIsFalse_ThenThrowsNotImplementedException(bool tracking)
        {
            // GW is not supported, so CanSetTracking is false
            ConnectTelescope(firmwareVersion: TelescopeList.Autostar497_30Ee);

            Assert.Throws<ASCOM.NotImplementedException>( () => { _telescope.Tracking = tracking; } );
        }

        [TestCase(true, "AP")]
        [TestCase(false, "AL")]
        public void Tracking_Set_WhenCanSetTrackingIsTrue_ThenValueIsUpdated(bool tracking, string alignmentCommand)
        {
            // GW is supported, so CanSetTracking is true
            ConnectTelescope(firmwareVersion: TelescopeList.Autostar497_43Eg);

            _telescope.Tracking = tracking;

            Assert.That(_telescope.Tracking, Is.EqualTo(tracking));

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(alignmentCommand, false), Times.Once);
        }

        [Test]
        public void TrackingRate_Set_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.TrackingRate = DriveRates.driveSidereal);
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: TrackingRate Set"));
        }

        [TestCase(DriveRates.driveSidereal, "TQ")]
        [TestCase(DriveRates.driveLunar, "TL")]
        public void TrackingRate_Set_WhenConnected_ThenSendsCommandToTelescope(DriveRates rate, string commandString)
        {
            string productName = TelescopeList.Autostar497;
            string firmwareVersion = TelescopeList.Autostar497_43Eg;

            ConnectTelescope(productName, firmwareVersion);

            _telescope.TrackingRate = rate;

            Assert.That(_telescope.TrackingRate, Is.EqualTo(rate));

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(commandString, false), Times.Once);
            _sharedResourcesWrapperMock.Verify(x => x.SendString("GT", false), Times.Once);
        }

        [Test]
        public void TrackingRate_Set_WhenUnSupportedRateSet_ThenThrowsException()
        {
            string productName = TelescopeList.Autostar497;
            string firmwareVersion = TelescopeList.Autostar497_43Eg;

            ConnectTelescope(productName, firmwareVersion);

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _telescope.TrackingRate = DriveRates.driveKing);

            Assert.That(exception.Message, Is.EqualTo("Exception of type 'System.ArgumentOutOfRangeException' was thrown.\r\nParameter name: value\r\nActual value was driveKing."));
        }

        [TestCase("60.1")]
        [TestCase("+60.1")]
        public void TrackingRage_Get_WhenReadingDefaultValue_ThenAssumesSidereal(string trackingRate)
        {
            ConnectTelescope();

            var result = _telescope.TrackingRate;

            Assert.That(result, Is.EqualTo(DriveRates.driveSidereal));
        }

        [TestCase(DriveRates.driveSidereal, "60.1")]
        [TestCase(DriveRates.driveSolar, "60.0")]
        [TestCase(DriveRates.driveLunar, "57.9")]
        [TestCase(DriveRates.driveSidereal, "+60.1")]
        [TestCase(DriveRates.driveSolar, "+60.0")]
        [TestCase(DriveRates.driveLunar, "+57.9")]
        [TestCase(DriveRates.driveLunar, "57.3")]
        [TestCase(DriveRates.driveLunar, "58.9")]
        public void TrackingRate_Get_WhenConnected_ThenSendsCommandToTelescope(DriveRates rate, string trackingRate)
        {
            _siderealTrackingRate = trackingRate;

            string productName = TelescopeList.Autostar497;
            string firmwareVersion = TelescopeList.Autostar497_43Eg;

            ConnectTelescope(productName, firmwareVersion);

            _telescope.TrackingRate = rate;

            var result = _telescope.TrackingRate;

            Assert.That(result, Is.EqualTo(rate));
        }

        [TestCase(DriveRates.driveSidereal, "60.1")]
        [TestCase(DriveRates.driveLunar, "60.1")]
        [TestCase(DriveRates.driveSidereal, "+60.1")]
        [TestCase(DriveRates.driveLunar, "+60.1")]
        public void TrackingRate_Set_WhenConnectedToLX200_ThenThrowsException(DriveRates rate, string trackingRate)
        {
            string productName = TelescopeList.LX200CLASSIC;
            string firmwareVersion = string.Empty;

            ConnectTelescope(productName, firmwareVersion);

            var result = Assert.Throws<ASCOM.NotImplementedException>( () =>  _telescope.TrackingRate = rate );

            Assert.That(result.Message, Is.EqualTo("TrackingRate Set is not implemented in this driver."));
        }

        [TestCase(TelescopeList.Autostar497, TelescopeList.Autostar497_43Eg, true )]
        [TestCase(TelescopeList.LX200CLASSIC, "", false)]
        public void TrackingRates_Get_ReturnsExpectedType(string productName, string firmwareVersion, bool supportsLunar)
        {
            ConnectTelescope(productName, firmwareVersion);

            var result = _telescope.TrackingRates;

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.AssignableTo<TrackingRates>());

            if (supportsLunar)
            {
                Assert.That(result.Count, Is.EqualTo(2));
            }
            else
            {
                Assert.That(result.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void UTCDate_Get_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() =>
            {
                var result = _telescope.UTCDate;
                Assert.Fail($"{result} should not have returned");
            });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: UTCDate Get"));
        }

        [TestCase("10/15/20", "20:15:10", "-1.0", 2020, 10, 15, 19, 15, 10)]
        [TestCase("12/03/15", "21:30:45", "+0.0", 2015, 12, 3, 21, 30, 45)]
        public void UTCDate_Get_WhenConnected_ThenReturnsUTCDateTime(string telescopeDate, string telescopeTime,
            string telescopeUtcCorrection, int year, int month, int day, int hour, int min, int second)
        {
            _testProperties.telescopeDate = telescopeDate;
            _testProperties.telescopeTime = telescopeTime;
            _testProperties.telescopeUtcCorrection = telescopeUtcCorrection;

            ConnectTelescope();

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
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.UTCDate = new DateTime(2010, 10, 15, 16, 42, 32, DateTimeKind.Utc));
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: UTCDate Set"));
        }

        [TestCase("10/15/20", "20:15:10", "-1.0", 2020, 10, 15, 19, 15, 10)]
        [TestCase("12/03/15", "21:30:45", "+0.0", 2015, 12, 3, 21, 30, 45)]
        public void UTCDate_Set_WhenFailsToSetTelescopeTime_ThenThrowsException(string telescopeDate, string telescopeTime, string telescopeUtcCorrection, int year, int month, int day, int hour, int min, int second)
        {
            double utcOffsetHours = double.Parse(telescopeUtcCorrection);
            TimeSpan utcCorrection = TimeSpan.FromHours(utcOffsetHours);

            var newDate = new DateTime(year, month, day, hour, min, second, DateTimeKind.Local) + utcCorrection;

            _sharedResourcesWrapperMock.Setup(x => x.SendString("GG", false)).Returns(telescopeUtcCorrection);
            _sharedResourcesWrapperMock.Setup(x => x.SendChar($"SL{telescopeTime}", false)).Returns("0");

            ConnectTelescope();

            var exception = Assert.Throws<InvalidOperationException>(() => _telescope.UTCDate = newDate);

            Assert.That(exception.Message, Is.EqualTo("Failed to set local time"));
        }

        [TestCase("10/15/20", "20:15:10", "-1.0", 2020, 10, 15, 20, 15, 10)]
        [TestCase("12/03/15", "21:30:45", "+0.0", 2015, 12, 3, 21, 30, 45)]
        public void UTCDate_Set_WhenFailsToSetTelescopeDate_ThenThrowsException(string telescopeDate, string telescopeTime, string telescopeUtcCorrection, int year, int month, int day, int hour, int min, int second)
        {
            _testProperties.telescopeUtcCorrection = telescopeUtcCorrection;
            double utcOffsetHours = double.Parse(telescopeUtcCorrection);
            TimeSpan utcCorrection = TimeSpan.FromHours(utcOffsetHours);

            var newDate = new DateTime(year, month, day, hour, min, second, DateTimeKind.Local) + utcCorrection;

            _sharedResourcesWrapperMock.Setup(x => x.SendChar($"SL{telescopeTime}", false)).Returns("1");
            _sharedResourcesWrapperMock.Setup(x => x.SendChar($"SC{newDate:MM/dd/yy}", false)).Returns("0");

            ConnectTelescope();

            var exception = Assert.Throws<InvalidOperationException>(() => _telescope.UTCDate = newDate);

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

            _testProperties.telescopeUtcCorrection = telescopeUtcCorrection;

            var newDate = new DateTime(year, month, day, hour, min, second, DateTimeKind.Local) + utcCorrection;

            _sharedResourcesWrapperMock.Setup(x => x.SendChar($"SL{telescopeTime}", false)).Returns("1");
            _sharedResourcesWrapperMock.Setup(x => x.SendChar($"SC{telescopeDate}", false)).Returns("1");

            ConnectTelescope();

            _telescope.UTCDate = newDate;

            _sharedResourcesWrapperMock.Verify(x => x.ReadTerminated(), Times.Exactly(2));
        }

        [Test]
        public void SyncToCoordinates_WhenNotConnected_ThenThrowsException()
        {
            double rightAscension = 5.5;
            double declination = -30.5;

            var exception = Assert.Throws<NotConnectedException>(() =>
            {
                _telescope.SyncToCoordinates(rightAscension, declination);
            });

            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SyncToCoordinates"));
        }

        [Test]
        public void SyncToCoordinates_WhenConnected_ThenReturnsExpectedResult()
        {
            var telescopeDecResult = "s12*34’56";

            string hms = "05:30:00";
            _testProperties.rightAscension = 5.5;

            double declination = -30.5;
            string dec = "-30*30:00";

            var digitsRA = 2;

            _sharedResourcesWrapperMock.Setup(x => x.SendChar($"Sr{_testProperties.telescopeRaResult}", false)).Returns("1");

            _utilMock.Setup(x => x.HoursToHMS(_testProperties.rightAscension, ":", ":", ":", digitsRA)).Returns(_testProperties.telescopeRaResult);
            _utilMock.Setup(x => x.HMSToHours(hms)).Returns(_testProperties.rightAscension);
            _utilMock.Setup(x => x.DegreesToDMS(declination, "*", ":", ":", digitsRA)).Returns(telescopeDecResult);
            _utilMock.Setup(x => x.DMSToDegrees(telescopeDecResult)).Returns(declination);

            _utilMock.Setup(x => x.DMSToDegrees(dec)).Returns(declination);

            _utilMock.Setup(x => x.HoursToHMS(_testProperties.rightAscension, ":", ":", ":", 2)).Returns(hms);
            _utilMock.Setup(x => x.DegreesToDMS(declination, "*", ":", ":", digitsRA)).Returns(dec);
            _sharedResourcesWrapperMock.Setup(x => x.SendChar($"Sr{hms}", false)).Returns("1");
            _sharedResourcesWrapperMock.Setup(x => x.SendChar($"Sd{dec}", false)).Returns("1");

            _sharedResourcesWrapperMock.Setup(x => x.SendString($"CM", false)).Returns("M31 EX GAL MAG 3.5 SZ178.0'#");

            _sharedResourcesWrapperMock.Setup(x => x.SendString("GD", false)).Returns(telescopeDecResult);

            ConnectTelescope();

            _telescope.SyncToCoordinates(_testProperties.rightAscension, declination);

            _sharedResourcesWrapperMock.Verify(x => x.SendString("CM", false), Times.Once);
            Assert.That(_telescope.TargetRightAscension, Is.EqualTo(_testProperties.rightAscension));
            Assert.That(_telescope.TargetDeclination, Is.EqualTo(declination));
        }

        [Test]
        public void Slewing_WhenNotConnected_ThenReturnsFalse()
        {
            var result = _telescope.Slewing;

            Assert.That(result, Is.False);

            _sharedResourcesWrapperMock.Verify(x => x.SendString("D", false), Times.Never);
        }

        [Test]
        public void Slewing_WhenConnectedAndTelescopeFails_ThenReturnsFalse()
        {
            ConnectTelescope();

            var result = _telescope.Slewing;

            Assert.That(result, Is.False);

            _sharedResourcesWrapperMock.Verify(x => x.SendString("D", false), Times.Once);
        }

        [Test]
        public void Slewing_WhenTelescopeIsSlewing_ThenReturnsTrue()
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendString("D", false)).Returns("|");

            ConnectTelescope();

            var result = _telescope.Slewing;

            Assert.That(result, Is.True);

            _sharedResourcesWrapperMock.Verify(x => x.SendString("D", false), Times.Once);
        }

        [TestCase(0, 0, "2021-10-03T20:36:00", "2021-10-03T20:36:01", false)]
        [TestCase(5, 0, "2021-10-03T20:36:00", "2021-10-03T20:36:01", true)]
        [TestCase(5, 0, "2021-10-03T20:36:00", "2021-10-03T20:36:06", false)]
        [TestCase(10, 0, "2021-10-03T20:36:00", "2021-10-03T20:36:06", true)]
        [TestCase(10, 0, "2021-10-03T20:36:00", "2021-10-03T20:36:09", true)]
        [TestCase(10, 0, "2021-10-03T20:36:00", "2021-10-03T20:36:10", false)]
        [TestCase(0, 5, "2021-10-03T20:36:00", "2021-10-03T20:36:01", true)]
        [TestCase(0, 5, "2021-10-03T20:36:00", "2021-10-03T20:36:05", false)]
        [TestCase(0, 10, "2021-10-03T20:36:00", "2021-10-03T20:36:05", true)]
        [TestCase(0, 10, "2021-10-03T20:36:00", "2021-10-03T20:36:10", false)]
        [TestCase(15, 10, "2021-10-03T20:36:00", "2021-10-03T20:36:10", true)]
        [TestCase(15, 10, "2021-10-03T20:36:00", "2021-10-03T20:36:24", true)]
        [TestCase(15, 10, "2021-10-03T20:36:00", "2021-10-03T20:36:25", false)]
        public void Slewing_WhenTelescopeIsSlewing_ThenReturnsExpectedValueForSettleTime(short settleTime, short profileSettleTime, string startSlewing, string endSlewing, bool isSlewing)
        {
            _sharedResourcesWrapperMock.SetupProperty(x => x.EarliestNonSlewingTime, DateTime.MinValue);
            _profileProperties.SettleTime = profileSettleTime;

            var timescalled = 0;
            DateTime startSlewingDateTime = DateTime.ParseExact(startSlewing, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
            DateTime endSlewingDatetime = DateTime.ParseExact(endSlewing, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

            _clockMock.Setup(x => x.UtcNow).Returns(() =>
            {
                if (timescalled == 0)
                {
                    timescalled++;
                    return startSlewingDateTime;
                }

                return endSlewingDatetime;
            });

            var slewingText = "|";
            var notSlewingText = String.Empty;

            _sharedResourcesWrapperMock.Setup(x => x.SendString("D", false)).Returns(() =>
            {
                if (timescalled == 0)
                {
                    return slewingText;
                }

                return notSlewingText;
            });

            ConnectTelescope();

            _telescope.SlewSettleTime = settleTime;

            var result = _telescope.Slewing;

            Assert.That(result, Is.EqualTo(true));

            result = _telescope.Slewing;

            Assert.That(result, Is.EqualTo(isSlewing));
        }

        [TestCase(TelescopeList.LX200CLASSIC, "", "|", true)]
        [TestCase(TelescopeList.LX200CLASSIC, "", "||||||||", true)]
        [TestCase(TelescopeList.LX200CLASSIC, "", "", false)]
        [TestCase(TelescopeList.LX200CLASSIC, "", "[FF][FF][FF][FF][FF][FF][FF][FF][FF][FF][FF][FF][FF][FF]  [FF][FF][FF][FF][FF][FF]", false)]   //The test case below is this same string encoded to return exactly what the telescope will return.
        [TestCase(TelescopeList.LX200CLASSIC, "", "\x00ff\x00ff\x00ff\x00ff\x00ff\x00ff\x00ff\x00ff\x00ff\x00ff\x00ff\x00ff\x00ff\x00ff  \x00ff\x00ff\x00ff\x00ff\x00ff\x00ff", false)]
        [TestCase(TelescopeList.Autostar497, TelescopeList.Autostar497_43Eg, "|", true)]
        [TestCase(TelescopeList.Autostar497, TelescopeList.Autostar497_43Eg, "\x007f", true)]
        [TestCase(TelescopeList.Autostar497, TelescopeList.Autostar497_43Eg, "", false)]
        public void Slewing_WhenTelescopeNotSlewing_ThenReturnsFalse(string productName, string firmwareVersion, string response, bool isSlewing)
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendString("D", false)).Returns(response);

            ConnectTelescope(productName, firmwareVersion);

            var result = _telescope.Slewing;

            Assert.That(result, Is.EqualTo(isSlewing));

            _sharedResourcesWrapperMock.Verify(x => x.SendString("D", false), Times.Once);
        }

        [TestCase(1, TelescopeAxes.axisPrimary)]
        [TestCase(-1, TelescopeAxes.axisPrimary)]
        [TestCase(1, TelescopeAxes.axisSecondary)]
        [TestCase(-1, TelescopeAxes.axisSecondary)]
        public void Slewing_WhenTelescopeIsMoving_ThenDoesNotSendCommandAndReturnsTrue(int rate, TelescopeAxes axis)
        {
            _sharedResourcesWrapperMock.SetupProperty(x => x.MovingPrimary);
            _sharedResourcesWrapperMock.SetupProperty(x => x.MovingSecondary);
            _sharedResourcesWrapperMock.SetupProperty(x => x.EarliestNonSlewingTime, DateTime.MinValue);

            ConnectTelescope();

            _telescope.MoveAxis(axis, rate);

            var result = _telescope.Slewing;

            Assert.That(result, Is.True);
            Assert.That(_sharedResourcesWrapperMock.Object.MovingPrimary, Is.EqualTo(axis == TelescopeAxes.axisPrimary));
            Assert.That(_sharedResourcesWrapperMock.Object.MovingSecondary, Is.EqualTo(axis == TelescopeAxes.axisSecondary));

            _sharedResourcesWrapperMock.Verify(x => x.SendString("D", false), Times.Never);
        }

        [TestCase(1, TelescopeAxes.axisPrimary, 0, 0, false, false)]
        [TestCase(-1, TelescopeAxes.axisPrimary, 0, 0, false, false)]
        [TestCase(1, TelescopeAxes.axisSecondary, 0, 0, false, false)]
        [TestCase(-1, TelescopeAxes.axisSecondary, 0, 0, false, false)]

        [TestCase(1, TelescopeAxes.axisPrimary, 10, 0, true, false)]
        [TestCase(-1, TelescopeAxes.axisPrimary, 10, 0, true, false)]
        [TestCase(1, TelescopeAxes.axisSecondary, 10, 0, true, false)]
        [TestCase(-1, TelescopeAxes.axisSecondary, 10, 0, true, false)]

        [TestCase(1, TelescopeAxes.axisPrimary, 10, 20, true, true)]
        [TestCase(-1, TelescopeAxes.axisPrimary, 10, 20, true, true)]
        [TestCase(1, TelescopeAxes.axisSecondary, 10, 20, true, true)]
        [TestCase(-1, TelescopeAxes.axisSecondary, 10, 20, true, true)]
        public void Slewing_WhenTelescopeStops_ThenWaitsForSettleTime(int rate, TelescopeAxes axis, short profileSettleTime, short driverSettleTime, bool expectedResultInWaitingPeriod, bool afterProfileSettleTimeUp)
        {
            _sharedResourcesWrapperMock.SetupProperty(x => x.MovingPrimary);
            _sharedResourcesWrapperMock.SetupProperty(x => x.MovingSecondary);
            _sharedResourcesWrapperMock.SetupProperty(x => x.SlewSettleTime);
            _sharedResourcesWrapperMock.SetupProperty(x => x.EarliestNonSlewingTime, DateTime.MinValue);

            _profileProperties.SettleTime = profileSettleTime;

            DateTime currentTime = MakeTime("2021-01-23T22:02:10");

            _clockMock.Setup(x => x.UtcNow).Returns(() => currentTime);

            ConnectTelescope();

            _telescope.SlewSettleTime = driverSettleTime;

            _telescope.MoveAxis(axis, rate);

            var result = _telescope.Slewing;
            Assert.That(result, Is.True);

            _telescope.MoveAxis(axis, 0);

            currentTime += TimeSpan.FromSeconds(profileSettleTime / 2);

            result = _telescope.Slewing;
            Assert.That(result, Is.EqualTo(expectedResultInWaitingPeriod));

            currentTime += TimeSpan.FromSeconds(profileSettleTime / 2);

            result = _telescope.Slewing;
            Assert.That(result, Is.EqualTo(afterProfileSettleTimeUp));

            currentTime += TimeSpan.FromSeconds(driverSettleTime);

            result = _telescope.Slewing;
            Assert.That(result, Is.False);
        }

        private DateTime MakeTime(string dateTimeString)
        {
            return DateTime.ParseExact(dateTimeString, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
        }


        [Test]
        public void SlewToTargetAsync_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.SlewToTargetAsync());
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SlewToTargetAsync"));
        }

        [Test]
        public void SlewToTargetAsync_WhenTargetDeclinationNotSet_ThenThrowsException()
        {
            ConnectTelescope();

            _telescope.TargetRightAscension = 1;

            var exception = Assert.Throws<InvalidOperationException>(() => _telescope.SlewToTargetAsync());
            Assert.That(exception.Message, Is.EqualTo("Target not set"));
        }

        [Test]
        public void SlewToTargetAsync_WhenTargetRightAscensionNotSet_ThenThrowsException()
        {
            ConnectTelescope();

            _telescope.TargetDeclination = 1;

            var exception = Assert.Throws<InvalidOperationException>(() => _telescope.SlewToTargetAsync());
            Assert.That(exception.Message, Is.EqualTo("Target not set"));
        }

        [Test]
        public void SlewToTargetAsync_WhenTargetSet_ThenAttemptsSlew()
        {
            ConnectTelescope();

            _telescope.TargetRightAscension = 2;
            _telescope.TargetDeclination = 1;

            var exception = Assert.Throws<DriverException>(() => _telescope.SlewToTargetAsync());
            Assert.That(exception.Message, Is.EqualTo("This error should not happen"));
        }

        [Test]
        public void SlewToTargetAsync_WhenTargetSetAndSlewIsPossible_ThenAttemptsSlew()
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendChar("MS", false)).Returns("0");

            ConnectTelescope();

            _telescope.TargetRightAscension = 2;
            _telescope.TargetDeclination = 1;


            _telescope.SlewToTargetAsync();

            _sharedResourcesWrapperMock.Verify(x => x.SendChar("MS", false), Times.Once);
        }

        [Test]
        public void SlewToTargetAsync_WhenTargetBelowHorizon_ThenThrowsException()
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendChar("MS", false)).Returns("1");
            _sharedResourcesWrapperMock.Setup(x => x.ReadTerminated()).Returns("Below horizon");

            ConnectTelescope();

            _telescope.TargetRightAscension = 2;
            _telescope.TargetDeclination = 1;

            var exception = Assert.Throws<InvalidOperationException>(() => _telescope.SlewToTargetAsync());
            Assert.That(exception.Message, Is.EqualTo("Below horizon"));
        }

        [Test]
        public void SlewToTargetAsync_WhenTargetBelowElevation_ThenThrowsException()
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendChar("MS", false)).Returns("2");
            _sharedResourcesWrapperMock.Setup(x => x.ReadTerminated()).Returns("Above below elevation");

            ConnectTelescope();

            _telescope.TargetRightAscension = 2;
            _telescope.TargetDeclination = 1;

            var exception = Assert.Throws<InvalidOperationException>(() => _telescope.SlewToTargetAsync());
            Assert.That(exception.Message, Is.EqualTo("Above below elevation"));
        }

        [Test]
        public void SlewToTargetAsync_WhenTelescopeCanHitTripod_ThenThrowsException()
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendChar("MS", false)).Returns("3");
            _sharedResourcesWrapperMock.Setup(x => x.ReadTerminated()).Returns("the telescope can hit the tripod");

            ConnectTelescope();

            _telescope.TargetRightAscension = 2;
            _telescope.TargetDeclination = 1;

            var exception = Assert.Throws<InvalidOperationException>(() => _telescope.SlewToTargetAsync());
            Assert.That(exception.Message, Is.EqualTo("the telescope can hit the tripod"));
        }

        [Test]
        public void SlewToTarget_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.SlewToTarget());
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SlewToTarget"));
        }

        [Test]
        public void SlewToTarget_WhenSlewing_ThenWaitsForTheSlewToComplete()
        {
            _sharedResourcesWrapperMock.Setup(x => x.SendChar("MS", false)).Returns("0");

            var preTestItterations = 1;
            var slewCounter = 0;
            var iterations = 10;
            _sharedResourcesWrapperMock.Setup(x => x.SendString("D", false)).Returns(() =>
            {
                slewCounter++;
                if (slewCounter <= preTestItterations)
                    return "";
                else if (slewCounter <= iterations)
                    return "|";
                return "";
            });

            ConnectTelescope();

            _telescope.TargetRightAscension = 2;
            _telescope.TargetDeclination = 1;

            _telescope.SlewToTarget();

            _utilMock.Verify(x => x.WaitForMilliseconds(It.IsAny<int>()), Times.Exactly(iterations - preTestItterations));
        }

        [Test]
        public void SlewToCoordinatesAsync_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.SlewToCoordinatesAsync(0, 0));
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SlewToCoordinatesAsync"));
        }

        [Test]
        public void SlewToCoordinatesAsync_WhenCalled_ThenSetsTargetAndSlews()
        {
            var digitsRA = 2;

            _testProperties.rightAscension = 1;

            var declination = 2;

            var telescopeDecResult = "s12*34’56";

            _sharedResourcesWrapperMock.Setup(x => x.SendChar("MS", false)).Returns("0");

            _sharedResourcesWrapperMock.Setup(x => x.SendChar($"Sr{_testProperties.telescopeRaResult}", false)).Returns("1");
            _sharedResourcesWrapperMock.Setup(x => x.SendString("GD", false)).Returns(telescopeDecResult);
            _utilMock.Setup(x => x.HoursToHMS(_testProperties.rightAscension, ":", ":", ":", digitsRA)).Returns(_testProperties.telescopeRaResult);

            _utilMock.Setup(x => x.DMSToDegrees(telescopeDecResult)).Returns(declination);
            _utilMock.Setup(x => x.DegreesToDMS(declination, "*", ":", ":", digitsRA)).Returns(telescopeDecResult);

            //var slewCounter = 0;
            //var iterations = 10;
            //_sharedResourcesWrapperMock.Setup(x => x.SendString("D", false)).Returns(() =>
            //{
            //    slewCounter++;
            //    if (slewCounter <= iterations)
            //        return "|";
            //    else
            //        return "";
            //});

            ConnectTelescope();

            _telescope.SlewToCoordinatesAsync(_testProperties.rightAscension, declination);

            //_utilMock.Verify(x => x.WaitForMilliseconds(It.IsAny<int>()), Times.Exactly(iterations));
            Assert.That(_telescope.TargetRightAscension, Is.EqualTo(_testProperties.rightAscension));
            Assert.That(_telescope.TargetDeclination, Is.EqualTo(declination));
            _sharedResourcesWrapperMock.Verify(x => x.SendChar("MS", false), Times.Once);
        }

        [Test]
        public void SlewToCoordinates_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.SlewToCoordinates(0, 0));
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SlewToCoordinates"));
        }

        [Test]
        public void SlewToCoordinates_WhenCalled_ThenSetsTargetAndSlews()
        {
            _testProperties.rightAscension = 1;
            var declination = 2;

            var telescopeDecResult = "s12*34’56";
            var dmsResult = 1.2;
            var digitsRA = 2;

            _sharedResourcesWrapperMock.Setup(x => x.SendString("GD", false)).Returns(telescopeDecResult);
            _sharedResourcesWrapperMock.Setup(x => x.SendChar($"Sr{_testProperties.telescopeRaResult}", false)).Returns("1");

            _utilMock.Setup(x => x.HoursToHMS(_testProperties.rightAscension, ":", ":", ":", digitsRA)).Returns(_testProperties.telescopeRaResult);
            _utilMock.Setup(x => x.DMSToDegrees(telescopeDecResult)).Returns(dmsResult);
            _utilMock.Setup(x => x.DegreesToDMS(declination, "*", ":", ":", digitsRA)).Returns(telescopeDecResult);

            _sharedResourcesWrapperMock.Setup(x => x.SendChar("MS", false)).Returns("0");

            var preTestItterations = 1;
            var slewCounter = 0;
            var iterations = 10;
            _sharedResourcesWrapperMock.Setup(x => x.SendString("D", false)).Returns(() =>
            {
                slewCounter++;
                if (slewCounter <= preTestItterations)
                    return "";
                else if (slewCounter <= iterations)
                    return "|";
                return "";
            });

            ConnectTelescope();

            _telescope.SlewToCoordinates(_testProperties.rightAscension, declination);
            Assert.That(_telescope.TargetRightAscension, Is.EqualTo(_testProperties.rightAscension));
            Assert.That(_telescope.TargetDeclination, Is.EqualTo(dmsResult));
            _sharedResourcesWrapperMock.Verify(x => x.SendChar("MS", false), Times.Once);

            _utilMock.Verify(x => x.WaitForMilliseconds(It.IsAny<int>()), Times.Exactly(iterations - preTestItterations));
        }

        [Test]
        public void SlewToAltAzAsync_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.SlewToAltAzAsync(0, 0));
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SlewToAltAzAsync"));
        }

        [Test]
        public void SlewToAltAzAsync_WhenAltitudeGreaterThan90_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => _telescope.SlewToAltAzAsync(0, 90.1));
            Assert.That(exception.Message, Is.EqualTo("Altitude cannot be greater than 90."));
        }

        [Test]
        public void SlewToAltAzAsync_WhenAltitudeLowerThan0_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => _telescope.SlewToAltAzAsync(0, -0.1));
            Assert.That(exception.Message, Is.EqualTo("Altitide cannot be less than 0."));
        }

        [Test]
        public void SlewToAltAzAsync_WhenAzimuth360OrHigher_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => _telescope.SlewToAltAzAsync(360, 0));
            Assert.That(exception.Message, Is.EqualTo("Azimuth cannot be 360 or higher."));
        }

        [Test]
        public void SlewToAltAzAsync_WhenAzimuthLowerThan0_ThenThrowsException()
        {
            ConnectTelescope();

            var exception = Assert.Throws<InvalidValueException>(() => _telescope.SlewToAltAzAsync(-0.1, 0));
            Assert.That(exception.Message, Is.EqualTo("Azimuth cannot be less than 0."));
        }

        [Test]
        public void SlewToAltAzAsync_WhenAltAndAzValid_ThenConvertsToRADec()
        {
            _testProperties.rightAscension = 20;
            _testProperties.declination = 10;

            var altitude = 30;
            var azimuth = 45;

            _sharedResourcesWrapperMock.Setup(x => x.SendString("GG", false)).Returns("-1.0");

            _sharedResourcesWrapperMock.Setup(x => x.SendChar("MS", false)).Returns("0");

            var telescopeRaResult = "HH:MM:SS";
            var telescopeDecResult = "s12*34’56";
            var digitsRA = 2;

            _sharedResourcesWrapperMock.Setup(x => x.SendChar($"Sr{telescopeRaResult}", false)).Returns("1");

            _utilMock.Setup(x => x.HoursToHMS(_testProperties.rightAscension, ":", ":", ":", digitsRA)).Returns(telescopeRaResult);
            _utilMock.Setup(x => x.HMSToHours(telescopeRaResult)).Returns(_testProperties.rightAscension);
            _utilMock.Setup(x => x.DegreesToDMS(_testProperties.declination, "*", ":", ":", digitsRA)).Returns(telescopeDecResult);
            _utilMock.Setup(x => x.DMSToDegrees(telescopeDecResult)).Returns(_testProperties.declination);

            ConnectTelescope();

            _telescope.SlewToAltAzAsync(azimuth, altitude);

            Assert.That(_telescope.TargetRightAscension, Is.EqualTo(_testProperties.rightAscension));
            Assert.That(_telescope.TargetDeclination, Is.EqualTo(_testProperties.declination));
            _sharedResourcesWrapperMock.Verify(x => x.SendChar("MS", false), Times.Once);
        }

        [Test]
        public void SlewToAltAz_WhenAzimuthLowerThan0_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.SlewToAltAz(0, 0));
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: SlewToAltAz"));
        }

        [Test]
        public void SlewToAltAz_WhenCalled_ThenSetsTargetAndSlews()
        {
            _testProperties.rightAscension = 10.0;
            _testProperties.declination = 20;
            var azimuth = 30;
            var altitude = 40;

            _utilMock.Setup(x => x.HoursToHMS(_testProperties.rightAscension, ":", ":", ":", 2)).Returns(_testProperties.telescopeRaResult);
            _utilMock.Setup(x => x.DegreesToDMS(_testProperties.declination, "*", ":", ":", 2)).Returns(_testProperties.telescopeRaResult);
            _utilMock.Setup(x => x.DMSToDegrees(_testProperties.telescopeRaResult)).Returns(_testProperties.declination);

            _sharedResourcesWrapperMock.Setup(x => x.SendString("GG", false)).Returns("-1.0");
            _sharedResourcesWrapperMock.Setup(x => x.SendChar("Sd+HH:MM:SS", false)).Returns("1");

            _sharedResourcesWrapperMock.Setup(x => x.SendChar("MS", false)).Returns("0");

            var preTestItterations = 1;
            var slewCounter = 0;
            var iterations = 10;
            _sharedResourcesWrapperMock.Setup(x => x.SendString("D", false)).Returns(() =>
            {
                slewCounter++;
                if (slewCounter <= preTestItterations)
                    return "";
                else if (slewCounter <= iterations)
                    return "|";
                return "";
            });

            ConnectTelescope();

            _telescope.SlewToAltAz(azimuth, altitude);

            Assert.That(_telescope.TargetRightAscension, Is.EqualTo(_testProperties.rightAscension));
            Assert.That(_telescope.TargetDeclination, Is.EqualTo(_testProperties.declination));
            _sharedResourcesWrapperMock.Verify(x => x.SendChar("MS", false), Times.Once);
            _utilMock.Verify(x => x.WaitForMilliseconds(It.IsAny<int>()), Times.Exactly(iterations - preTestItterations));
        }

        [Test]
        public void Azimuth_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() =>
            {
                var result = _telescope.Azimuth;
                Assert.Fail($"{result} should not have returned");
            });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: Azimuth Get"));
        }

        [Test]
        public void Azimuth_WhenConnected_ThenReturnsTelescopeAzumith()
        {
            var telescopeLongitude = "350";
            var telescopeLongitudeValue = 350;

            _testProperties.telescopeAltitude = 45;
            _testProperties.telescopeAzimuth = 200;
            _testProperties.hourAngle = 3;

            _sharedResourcesWrapperMock.Setup(x => x.SendString("GG", false)).Returns("-1.0");

            _sharedResourcesWrapperMock.Setup(x => x.SendString("Gg", false)).Returns(telescopeLongitude);
            _utilMock.Setup(x => x.DMSToDegrees(telescopeLongitude)).Returns(telescopeLongitudeValue);


            ConnectTelescope();

            var result = _telescope.Azimuth;

            Assert.That(result, Is.EqualTo(_testProperties.telescopeAzimuth));
        }

        [Test]
        public void Altitude_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() =>
            {
                var result = _telescope.Altitude;
                Assert.Fail($"{result} should not have returned");
            });
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: Altitude Get"));
        }

        [Test]
        public void Altitude_WhenConnected_ThenReturnsTelescopeAltitude()
        {
            var expectedAltitude = 45;

            var telescopeLongitude = "350";
            var telescopeLongitudeValue = 350;

            _sharedResourcesWrapperMock.Setup(x => x.SendString("GG", false)).Returns("-1.0");

            _utilMock.Setup(x => x.DMSToDegrees(telescopeLongitude)).Returns(telescopeLongitudeValue);

            _astroMathsMock.Setup(x => x.RightAscensionToHourAngle(It.IsAny<DateTime>(), It.IsAny<double>(), It.IsAny<double>())).Returns(_testProperties.hourAngle);

            _astroMathsMock.Setup(x => x.ConvertEqToHoz(_testProperties.hourAngle, It.IsAny<double>(), It.IsAny<EquatorialCoordinates>())).Returns(new HorizonCoordinates { Altitude = expectedAltitude, Azimuth = 200 });

            ConnectTelescope();

            var result = _telescope.Altitude;

            Assert.That(result, Is.EqualTo(expectedAltitude));
        }

        [Test]
        public void AbortSlew_WhenNotConnected_ThenThrowsException()
        {
            var exception = Assert.Throws<NotConnectedException>(() => _telescope.AbortSlew());
            Assert.That(exception.Message, Is.EqualTo("Not connected to telescope when trying to execute: AbortSlew"));
        }

        [Test]
        public void AbortSlew_WhenConnected_ThenSendsStopSlewingToTelescope()
        {
            ConnectTelescope();

            _telescope.AbortSlew();

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind("Q", false), Times.Once);

            var isSloSlewing = _telescope.Slewing;

            Assert.That(isSloSlewing, Is.False);
            _sharedResourcesWrapperMock.Verify(x => x.SendString("D", false), Times.Once);
        }

        [Test]
        public void AbortSlew_WhenParked_ThenThrowsParkedException()
        {
            ConnectTelescope();
            _telescope.Park();

            Assert.Throws<ParkedException>(() => _telescope.AbortSlew());
        }

        [Test]
        public void MoveAxis_WhenParked_ThenThrowsParkedException()
        {
            ConnectTelescope();
            _telescope.Park();

            Assert.Throws<ParkedException>(() => _telescope.MoveAxis(TelescopeAxes.axisPrimary, 0));
        }

        [Test]
        public void PulseGuide_WhenParked_ThenThrowsParkedException()
        {
            ConnectTelescope();
            _telescope.Park();

            Assert.Throws<ParkedException>(() => _telescope.PulseGuide(GuideDirections.guideEast, 0));
        }

        [Test]
        public void SlewToAltAz_WhenParked_ThenThrowsParkedException()
        {
            ConnectTelescope();
            _telescope.Park();

            Assert.Throws<ParkedException>(() => _telescope.SlewToAltAz(0, 0));
        }

        [Test]
        public void SlewToAltAzAsync_WhenParked_ThenThrowsParkedException()
        {
            ConnectTelescope();
            _telescope.Park();

            Assert.Throws<ParkedException>(() => _telescope.SlewToAltAzAsync(0, 0));
        }

        [Test]
        public void SlewToCoordinates_WhenParked_ThenThrowsParkedException()
        {
            ConnectTelescope();
            _telescope.Park();

            Assert.Throws<ParkedException>(() => _telescope.SlewToCoordinates(0, 0));
        }

        [Test]
        public void SlewToCoordinatesAsync_WhenParked_ThenThrowsParkedException()
        {
            ConnectTelescope();
            _telescope.Park();

            Assert.Throws<ParkedException>(() => _telescope.SlewToCoordinatesAsync(0, 0));
        }

        [Test]
        public void SlewToTarget_WhenParked_ThenThrowsParkedException()
        {
            ConnectTelescope();
            _telescope.Park();

            Assert.Throws<ParkedException>(() => _telescope.SlewToTarget());
        }

        [Test]
        public void SlewToTargetAsync_WhenParked_ThenThrowsParkedException()
        {
            ConnectTelescope();
            _telescope.Park();

            Assert.Throws<ParkedException>(() => _telescope.SlewToTargetAsync());
        }

        [Test]
        public void SyncToCoordinates_WhenParked_ThenThrowsParkedException()
        {
            ConnectTelescope();
            _telescope.Park();

            Assert.Throws<ParkedException>(() => _telescope.SyncToCoordinates(0, 0));
        }

        [Test]
        public void SyncToTarget_WhenParked_ThenThrowsParkedException()
        {
            ConnectTelescope();
            _telescope.Park();

            Assert.Throws<ParkedException>(() => _telescope.SyncToTarget());
        }

        [Test]
        public void TargetDeclination_WhenParked_ThenThrowsParkedException()
        {
            ConnectTelescope();
            _telescope.Park();

            Assert.Throws<ParkedException>(() => _telescope.TargetDeclination = 1);
        }

        [Test]
        public void TargetRightAscension_WhenParked_ThenThrowsParkedException()
        {
            ConnectTelescope();
            _telescope.Park();

            Assert.Throws<ParkedException>(() => _telescope.TargetRightAscension = 1);
        }

        [Test]
        public void TrackingRate_WhenParked_ThenThrowsParkedException()
        {
            ConnectTelescope();
            _telescope.Park();

            Assert.Throws<ParkedException>(() => _telescope.TrackingRate = DriveRates.driveLunar);
        }


        [TestCase(ParkedBehaviour.NoCoordinates, true)]
        [TestCase(ParkedBehaviour.LastGoodPosition, false)]
        [TestCase(ParkedBehaviour.ReportCoordinates, false)]
        public void UTCDate_WhenParked_ReturnsExpectedResult(ParkedBehaviour behaviour, bool throwsException)
        {
            _profileProperties.ParkedBehaviour = behaviour;
            DateTime testNow = DateTime.ParseExact("2021-10-03T20:36:25", "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
            _clockMock.Setup(x => x.UtcNow).Returns(() => testNow);

            ConnectTelescope();
            _telescope.Park();

            if (throwsException)
                Assert.Throws<ParkedException>(() => { var date = _telescope.UTCDate; });
            else
            {
                DateTime date = DateTime.MinValue;
                Assert.DoesNotThrow(() => { date = _telescope.UTCDate; });

                Assert.That(date, Is.EqualTo(testNow));
            }
        }

        [TestCase(ParkedBehaviour.NoCoordinates, true)]
        [TestCase(ParkedBehaviour.LastGoodPosition, false)]
        [TestCase(ParkedBehaviour.ReportCoordinates, false)]
        public void SiteLatitude_WhenParked_ThenThrowsParkedException(ParkedBehaviour behaviour, bool throwsParkedException)
        {
            _profileProperties.ParkedBehaviour = behaviour;

            ConnectTelescope();
            var siteLatitude = _telescope.SiteLatitude;
            _telescope.Park();

            if (throwsParkedException)
                Assert.Throws<ParkedException>(() => { var lat = _telescope.SiteLatitude; });
            else
            {
                var lat = _telescope.SiteLatitude;
                Assert.That(lat, Is.EqualTo(siteLatitude));
            }
        }

        [TestCase(ParkedBehaviour.NoCoordinates, true)]
        [TestCase(ParkedBehaviour.LastGoodPosition, false)]
        [TestCase(ParkedBehaviour.ReportCoordinates, false)]
        public void SiteLongitude_WhenParked_ThenThrowsParkedException(ParkedBehaviour behaviour, bool throwsParkedException)
        {
            _profileProperties.ParkedBehaviour = behaviour;

            ConnectTelescope();
            var siteLongitude = _telescope.SiteLongitude;
            _telescope.Park();

            if (throwsParkedException)
                Assert.Throws<ParkedException>(() => { var siteLong = _telescope.SiteLongitude; });
            else
            {
                var l = _telescope.SiteLongitude;
                Assert.That(l, Is.EqualTo(siteLongitude));
            }

        }

        [TestCase(ParkedBehaviour.NoCoordinates)]
        [TestCase(ParkedBehaviour.LastGoodPosition)]
        [TestCase(ParkedBehaviour.ReportCoordinates)]
        public void Declination_WhenParked_ThenThrowsParkedException(ParkedBehaviour behaviour)
        {
            _profileProperties.ParkedBehaviour = behaviour;
            _profileProperties.ParkedAlt = 0;
            _profileProperties.ParkedAz = 180;
            DateTime testNow = DateTime.ParseExact("2021-10-03T20:36:25", "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

            _testProperties.declination = 45;

            _clockMock.Setup(x => x.UtcNow).Returns(() => testNow);

            ConnectTelescope();
            _telescope.Park();

            switch (_profileProperties.ParkedBehaviour)
            {
                case ParkedBehaviour.LastGoodPosition:
                    var lastGoodDec = _telescope.Declination;
                    Assert.That(lastGoodDec, Is.EqualTo(0));
                    break;
                case ParkedBehaviour.ReportCoordinates:
                    var dec = _telescope.Declination;
                    Assert.That(dec, Is.EqualTo(_testProperties.declination));
                    break;
                default:
                    Assert.Throws<ParkedException>(() => { var d = _telescope.Declination; });
                    break;
            }
        }

        [TestCase(ParkedBehaviour.NoCoordinates)]
        [TestCase(ParkedBehaviour.LastGoodPosition)]
        [TestCase(ParkedBehaviour.ReportCoordinates)]
        public void RightAscension_WhenParked_ThenThrowsParkedException(ParkedBehaviour behaviour)
        {
            _profileProperties.ParkedBehaviour = behaviour;
            _profileProperties.ParkedAlt = 0;
            _profileProperties.ParkedAz = 180;
            DateTime testNow = DateTime.ParseExact("2021-10-03T20:36:25", "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

            var declination = 45;

            _clockMock.Setup(x => x.UtcNow).Returns(() => testNow);

            _astroMathsMock
                .Setup(x => x.ConvertHozToEq(It.IsAny<DateTime>(), It.IsAny<double>(), It.IsAny<double>(),
                It.IsAny<HorizonCoordinates>())).Returns(new EquatorialCoordinates { Declination = declination, RightAscension = _testProperties.rightAscension });

            ConnectTelescope();
            _telescope.Park();

            switch (_profileProperties.ParkedBehaviour)
            {
                case ParkedBehaviour.LastGoodPosition:
                    var lastGoodRa = _telescope.RightAscension;
                    Assert.That(lastGoodRa, Is.EqualTo(1.2));
                    break;
                case ParkedBehaviour.ReportCoordinates:
                    var reportRa = _telescope.RightAscension;
                    Assert.That(reportRa, Is.EqualTo(_testProperties.rightAscension));
                    break;
                default:
                    Assert.Throws<ParkedException>(() => { var ra = _telescope.RightAscension; });
                    break;
            }
        }
    }
}
using ASCOM;
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
            _profileProperties = new ProfileProperties();
            _profileProperties.TraceLogger = false;
            _profileProperties.ComPort = "TestCom1";
            _profileProperties.GuideRateArcSecondsPerSecond = 1.23;
            _profileProperties.Precision = "Unchanged";

            _utilMock = new Mock<IUtil>();

            _sharedResourcesWrapperMock = new Mock<ISharedResourcesWrapper>();
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

            var exception = Assert.Throws<ActionNotImplementedException>(() => { var actualResult = _focuser.Action(actionName, string.Empty); });
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
    }
}

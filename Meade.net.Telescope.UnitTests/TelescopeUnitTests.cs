using System.Xml.Serialization;
using ASCOM;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Meade.net;
using ASCOM.Meade.net.AstroMaths;
using ASCOM.Meade.net.Wrapper;
using ASCOM.Utilities.Interfaces;
using Moq;
using NUnit.Framework;

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
        public void Action_Handbox_ReadDisplay()
        {
            string expectedResult = "test result string";
            _sharedResourcesWrapperMock.Setup(x => x.SendString(It.IsAny<string>())).Returns(expectedResult);

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
            _telescope.Action("handbox", action);

            _sharedResourcesWrapperMock.Verify(x => x.SendBlind(expectedString), Times.Once);
        }

        [Test]
        public void Action_Handbox_nonExistantAction()
        {
            string actionName = "handbox";
            string actionParameters = "doesnotexist";
            var exception = Assert.Throws<ActionNotImplementedException>(() => { _telescope.Action(actionName, actionParameters); });

            Assert.That(exception.Message, Is.EqualTo($"Action {actionName}({actionParameters}) is not implemented in this driver is not implemented in this driver."));
        }

        [Test]
        public void Action_nonExistantAction()
        {
            string actionName = "doesnotexist";
            var exception = Assert.Throws<ActionNotImplementedException>(() => { _telescope.Action(actionName, string.Empty); });

            Assert.That(exception.Message, Is.EqualTo($"Action {actionName} is not implemented in this driver is not implemented in this driver."));
        }
    }
}

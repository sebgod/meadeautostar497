using ASCOM.Astrometry.AstroUtils;
using ASCOM.Meade.net;
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

            _telescope = new ASCOM.Meade.net.Telescope(_utilMock.Object, _utilExtraMock.Object, _astroUtilsMock.Object, _sharedResourcesWrapperMock.Object, _astroMathsMock.Object);
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

            _sharedResourcesWrapperMock.Verify( x => x.SetupDialog(), Times.Once);
            _sharedResourcesWrapperMock.Verify(x => x.ReadProfile(), Times.Exactly(2));
        }
    }
}

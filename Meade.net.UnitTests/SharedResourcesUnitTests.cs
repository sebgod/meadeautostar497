
using ASCOM.Meade.net;
using ASCOM.Utilities.Interfaces;
using Moq;
using NUnit.Framework;

namespace Meade.net.UnitTests
{
    [TestFixture]
    public class SharedResourcesUnitTests
    {
        private Mock<ISerial> _serialMock;

        [SetUp]
        public void Setup()
        {
            _serialMock = new Mock<ISerial>();

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
    }
}

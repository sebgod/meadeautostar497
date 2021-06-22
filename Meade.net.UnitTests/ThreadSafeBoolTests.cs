using ASCOM.Meade.net;
using NUnit.Framework;

namespace Meade.net.UnitTests
{
    public class ThreadSafeBoolTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void WhenConvertedValueIsSame(bool value)
        {
            // given
            ThreadSafeBool sut = value;

            // when
            bool actual = sut;

            // then
            Assert.That(actual, Is.EqualTo(value));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void WhenSetValueIsChanged(bool value, bool setValue)
        {
            // given
            ThreadSafeBool sut = value;

            // when
            sut.Set(setValue);
            bool afterset = sut;

            // then
            Assert.That(afterset, Is.EqualTo(setValue));
        }
    }
}

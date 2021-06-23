using ASCOM.Meade.net;
using NUnit.Framework;

namespace Meade.net.UnitTests
{
    public class ThreadSafeBoolTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void When_Assigned_ThenValueIsSame(bool value)
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
        public void When_SetValue_ThenValueIsUpdated(bool initialValue, bool setValue)
        {
            // given
            ThreadSafeBool sut = initialValue;

            // when
            sut.Set(setValue);
            bool afterset = sut;

            // then
            Assert.That(afterset, Is.EqualTo(setValue));
        }
    }
}

using ASCOM.Meade.net;
using NUnit.Framework;

namespace Meade.net.UnitTests
{
    public class ThreadSafeNullableDoubleTests
    {
        [TestCase(0.1d)]
        [TestCase(-12.34d)]
        [TestCase(0d)]
        [TestCase(null)]
        public void When_Assigned_ThenValueIsSame(double? value)
        {
            // given
            ThreadSafeNullableDouble sut = value;

            // when
            double? actual = sut;

            // then
            Assert.That(actual, Is.EqualTo(value));
        }

        [TestCase(0.1d, 0.2d)]
        [TestCase(-12.34d, 5d)]
        [TestCase(0d, 1d)]
        [TestCase(null, 2d)]
        [TestCase(0.1d, null)]
        [TestCase(-12.34d, null)]
        [TestCase(0d, null)]
        [TestCase(null, null)]
        public void When_SetValue_ThenValueIsUpdated(double? initialValue, double? setValue)
        {
            // given
            ThreadSafeNullableDouble sut = initialValue;

            // when
            sut.Set(setValue);
            double? afterset = sut;

            // then
            Assert.That(afterset, Is.EqualTo(setValue));
        }
    }
}

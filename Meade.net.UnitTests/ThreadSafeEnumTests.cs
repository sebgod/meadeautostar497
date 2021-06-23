using ASCOM.DeviceInterface;
using ASCOM.Meade.net;
using NUnit.Framework;

namespace Meade.net.UnitTests
{
    public class ThreadSafeEnumTests
    {
        [TestCase(PierSide.pierUnknown)]
        [TestCase(PierSide.pierEast)]
        [TestCase(PierSide.pierWest)]
        public void When_Assigned_ThenValueIsSame(PierSide value)
        {
            // given
            ThreadSafeEnum<PierSide> sut = value;

            // when
            PierSide actual = sut;

            // then
            Assert.That(actual, Is.EqualTo(value));
        }

        [TestCase(PierSide.pierUnknown, PierSide.pierUnknown)]
        [TestCase(PierSide.pierUnknown, PierSide.pierEast)]
        [TestCase(PierSide.pierUnknown, PierSide.pierWest)]
        [TestCase(PierSide.pierEast, PierSide.pierUnknown)]
        [TestCase(PierSide.pierEast, PierSide.pierEast)]
        [TestCase(PierSide.pierEast, PierSide.pierWest)]
        [TestCase(PierSide.pierWest, PierSide.pierUnknown)]
        [TestCase(PierSide.pierWest, PierSide.pierEast)]
        [TestCase(PierSide.pierWest, PierSide.pierWest)]
        public void When_SetValue_ThenValueIsUpdated(PierSide initialValue, PierSide setValue)
        {
            // given
            ThreadSafeEnum<PierSide> sut = initialValue;

            // when
            sut.Set(setValue);
            PierSide afterset = sut;

            // then
            Assert.That(afterset, Is.EqualTo(setValue));
        }
    }
}

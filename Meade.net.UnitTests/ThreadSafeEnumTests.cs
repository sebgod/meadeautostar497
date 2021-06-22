using ASCOM.DeviceInterface;
using ASCOM.Meade.net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meade.net.UnitTests
{
    public class ThreadSafeEnumTests
    {
        [TestCase(PierSide.pierUnknown)]
        [TestCase(PierSide.pierEast)]
        [TestCase(PierSide.pierWest)]
        public void WhenConvertedValueIsSame(PierSide value)
        {
            // given
            ThreadSafeEnum<PierSide> sut = value;

            // when
            PierSide actual = sut;

            // then
            Assert.That(actual, Is.EqualTo(value));
        }

        [TestCase(PierSide.pierUnknown, PierSide.pierUnknown)]
        [TestCase(PierSide.pierUnknown, PierSide.pierUnknown)]
        [TestCase(PierSide.pierUnknown, PierSide.pierUnknown)]
        [TestCase(PierSide.pierEast, PierSide.pierUnknown)]
        [TestCase(PierSide.pierEast, PierSide.pierWest)]
        [TestCase(PierSide.pierEast, PierSide.pierEast)]
        [TestCase(PierSide.pierWest, PierSide.pierUnknown)]
        [TestCase(PierSide.pierWest, PierSide.pierWest)]
        [TestCase(PierSide.pierWest, PierSide.pierEast)]
        public void WhenSetValueIsChanged(PierSide value, PierSide setValue)
        {
            // given
            ThreadSafeEnum<PierSide> sut = value;

            // when
            sut.Set(setValue);
            PierSide afterset = sut;

            // then
            Assert.That(afterset, Is.EqualTo(setValue));
        }
    }
}

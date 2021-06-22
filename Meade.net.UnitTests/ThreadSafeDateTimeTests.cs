using ASCOM.Meade.net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meade.net.UnitTests
{
    public class ThreadSafeDateTimeTests
    {
        [TestCaseSource(nameof(DateTimeSource))]
        public void WhenConvertedValueIsSame(DateTime value)
        {
            // given
            ThreadSafeDateTime sut = value;

            // when
            DateTime actual = sut;

            // then
            Assert.That(actual, Is.EqualTo(value));
        }

        [TestCaseSource(nameof(DateTimeSetSource))]
        public void WhenSetValueIsChanged(DateTime value, DateTime setValue)
        {
            // given
            ThreadSafeDateTime sut = value;

            // when
            sut.Set(setValue);
            DateTime afterset = sut;

            // then
            Assert.That(afterset, Is.EqualTo(setValue));
        }

        static readonly DateTime Example1 = DateTimeOffset.Parse("2012-05-09T02:10:31.296761Z", CultureInfo.InvariantCulture).UtcDateTime;
        static readonly DateTime Example2 = DateTimeOffset.Parse("2051-03-09T23:15:11.556081Z", CultureInfo.InvariantCulture).UtcDateTime;

        static IEnumerable<DateTime> DateTimeSource => new[]
        {
            DateTime.MinValue,
            Example1,
            Example2
        };

        static IEnumerable<TestCaseData> DateTimeSetSource => new[]
        {
            new TestCaseData(DateTime.MinValue, Example1),
            new TestCaseData(DateTime.MinValue, Example2),
            new TestCaseData(DateTime.MinValue, DateTime.MinValue),
            new TestCaseData(Example1, Example1),
            new TestCaseData(Example1, Example2),
            new TestCaseData(Example1, DateTime.MinValue),
            new TestCaseData(Example2, Example1),
            new TestCaseData(Example2, Example2),
            new TestCaseData(Example2, DateTime.MinValue)
        };
    }
}

using ASCOM.Meade.net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Meade.net.UnitTests
{
    public class ThreadSafeDateTimeTests
    {
        [TestCaseSource(nameof(DateTimeSource))]
        public void When_Assigned_ThenValueIsSame(DateTime value)
        {
            // given
            ThreadSafeValue<DateTime> sut = value;

            // when
            DateTime actual = sut;

            // then
            Assert.That(actual, Is.EqualTo(value));
        }

        [TestCaseSource(nameof(DateTimeSetSource))]
        public void When_SetValue_ThenValueIsUpdated(DateTime initialValue, DateTime setValue)
        {
            // given
            ThreadSafeValue<DateTime> sut = initialValue;

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

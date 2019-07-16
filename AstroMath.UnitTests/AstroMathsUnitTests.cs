using System;
using ASCOM.Meade.net;
using ASCOM.Meade.net.AstroMaths;
using NUnit.Framework;

namespace AstroMath.UnitTests
{
    [TestFixture]
    public class AstroMathsUnitTests
    {
        private AstroMaths _astroMath;

        [SetUp]
        public void Setup()
        {
            _astroMath = new AstroMaths();
        }

        [Test]
        public void DegreesToRadians()
        {
            var radians = _astroMath.DegreesToRadians(90);
            Assert.That(radians, Is.EqualTo(1.5707963267948966));
        }

        [Test]
        public void RadiansToDegrees()
        {
            var degrees = _astroMath.RadiansToDegrees(1.5707963267948966);
            Assert.That(degrees, Is.EqualTo(90));
        }


        [Test]
        public void DateTimeToDecimalHours_book()
        {
            DateTime dateTime = new DateTime(2019, 05, 18, 18, 31, 27, DateTimeKind.Utc);
            var decimalHours = _astroMath.DateTimeToDecimalHours(dateTime);

            Assert.That(decimalHours, Is.EqualTo(18.524166666666666));
        }

        [Test]
        public void DateTimeToDecimalHours()
        {
            DateTime dateTime = new DateTime(2019, 05, 18, 22, 26, 15, DateTimeKind.Utc);
            var decimalHours = _astroMath.DateTimeToDecimalHours(dateTime);

            Assert.That(decimalHours, Is.EqualTo(22.4375));
        }

        [Test]
        public void UTtoGST_book()
        {
            DateTime dateTime = new DateTime(1980, 04, 22, 14, 36, 51, 670, DateTimeKind.Utc);
            double gst = _astroMath.UTtoGST(dateTime);

            Assert.That(gst, Is.EqualTo(4.667932706211154));
        }

        [Test]
        public void UTtoGST()
        {
            DateTime dateTime = new DateTime(2019, 05, 18, 22, 26, 15, DateTimeKind.Utc);
            double gst = _astroMath.UTtoGST(dateTime);

            Assert.That(gst, Is.EqualTo(14.191879687876451));
        }

        [Test]
        public void GSTtoLST_book()
        {
            double gst = 4.668119;
            var longitude = -64;
            var lst = _astroMath.GSTtoLST(gst, longitude);
            Assert.That(lst, Is.EqualTo(0.4014523333333333));
        }

        [Test]
        public void GSTtoLST()
        {
            double gst = 14.257589512545053;
            var longitude = -1.7833333333333332;
            var lst = _astroMath.GSTtoLST(gst, longitude);
            Assert.That(lst, Is.EqualTo(14.138700623656163));
        }

        [Test]
        public void RightAscensionToHourAngle_book()
        {
            DateTime dateTime = new DateTime(1980, 04, 22, 18, 36, 51,670, DateTimeKind.Utc);
            var longitude = -64;
            var rightAscension = 18.539166666666667;//18:32'21"

            //var declination = 30.0019444444444
            var hourAngle = _astroMath.RightAscensionToHourAngle(dateTime, longitude, rightAscension);
            Assert.That(hourAngle, Is.EqualTo(9.8730510088778161));
        }

        [Test]
        public void RightAscensionToHourAngle()
        {
            DateTime dateTime = new DateTime(2019, 05, 18, 22, 26, 15, DateTimeKind.Utc);
            var longitude = -1.7833333333333332;
            var rightAscension = 4.15361111111111;

            var hourAngle = _astroMath.RightAscensionToHourAngle(dateTime, longitude, rightAscension);
            Assert.That(hourAngle, Is.EqualTo(9.9193796878764502));
        }


        [Test]
        public void ConvertEqToHoz_book()
        {
            var latitude = 52.0;

            EquatorialCoordinates equatorialCoordinates = new EquatorialCoordinates();
            equatorialCoordinates.RightAscension = 5.862222222222222;//5 51' 44"
            equatorialCoordinates.Declination = 23.21944444444444;//23 13' 10"

            var hourAngle = 5.682222;

            var altAz = _astroMath.ConvertEqToHoz(hourAngle, latitude, equatorialCoordinates);

            Assert.That(altAz.Altitude, Is.EqualTo(20.958562421092779));
            Assert.That(altAz.Azimuth, Is.EqualTo(281.2728706962269));
        }

        [Test]
        public void ConvertEqToHoz()
        {
            DateTime dateTime = new DateTime(2019, 05, 18, 22, 26, 15, DateTimeKind.Utc);
            var longitude = -1.7833333333333332;
            var latitude = 52.0;
            EquatorialCoordinates equatorialCoordinates = new EquatorialCoordinates();
            equatorialCoordinates.RightAscension = 4.15361111111111;
            equatorialCoordinates.Declination = 30.0019444444444;

            var hourAngle = _astroMath.RightAscensionToHourAngle(dateTime, longitude, equatorialCoordinates.RightAscension);
            var altaz = _astroMath.ConvertEqToHoz(hourAngle, latitude, equatorialCoordinates);

            Assert.That(altaz.Altitude, Is.EqualTo(-3.5534402923925872));
            Assert.That(altaz.Azimuth, Is.EqualTo(333.2819484462679));
        }

        //[Test]
        //public void ConvertHozToEq_book()
        //{
        //    HorizonCoordinates hc = new HorizonCoordinates();
        //    hc.Altitude = 19.33434444;
        //    hc.Azimuth = 283.271028;
        //    var lat = 52;
        //    var raDec = _astroMath.ConvertHozToEq(lat, hc);

        //    Assert.That(raDec.RightAscension, Is.EqualTo(5.8622222973512992));
        //    Assert.That(raDec.Declination, Is.EqualTo(23.219444300552407));
        //}


        //[Test]
        //public void ConvertHozToEq()
        //{
        //    HorizonCoordinates hc = new HorizonCoordinates();
        //    hc.Altitude = 50;
        //    hc.Azimuth = 150;
        //    var lat = 53.5;
        //    var raDec = _astroMath.ConvertHozToEq(lat, hc);

        //    Assert.That(raDec.RightAscension, Is.EqualTo(22.69408899548845));
        //    Assert.That(raDec.Declination, Is.EqualTo(16.539114529888948));
        //}
    }
}

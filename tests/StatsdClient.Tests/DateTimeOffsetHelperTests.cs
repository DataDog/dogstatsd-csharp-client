using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using StatsdClient;
using Tests.Utils;

namespace Tests
{
    [TestFixture]
    public class DateTimeOffsetHelperTests
    {
        [Test]
        public void Conversions()
        {
            // a date time offset without timezone (UTC+0)
            var dto = new DateTimeOffset(2017, 01, 01, 00, 00, 00, new TimeSpan(0, 0, 0));
            long timestamp = DateTimeOffsetHelper.ToUnixTimeSeconds(dto);
            Assert.AreEqual(1483228800, timestamp);

            // a date time offset with a timezone (UTC+2)
            dto = new DateTimeOffset(2017, 01, 01, 00, 00, 00,  new TimeSpan(2, 0, 0));
            timestamp = DateTimeOffsetHelper.ToUnixTimeSeconds(dto);
            Assert.AreEqual(1483228800 - (3600 * 2), timestamp);

            // a date time offset with another timezone (UTC-2)
            dto = new DateTimeOffset(2017, 01, 01, 00, 00, 00,  new TimeSpan(-2, 0, 0));
            timestamp = DateTimeOffsetHelper.ToUnixTimeSeconds(dto);
            Assert.AreEqual(1483228800 + (3600 * 2), timestamp);
        }
    }
}
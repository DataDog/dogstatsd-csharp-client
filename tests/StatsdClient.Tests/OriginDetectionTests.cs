using System;
using System.Collections;
using NUnit.Framework;
using StatsdClient;

namespace Tests
{
    [TestFixture]
    public class OriginDetectionTests
    {
        [Test]
        public void ExternalDataNullOrEmpty()
        {
            var originDetectionNull = new OriginDetection(null);
            Assert.Null(originDetectionNull.ExternalData);

            var originDetectionEmpty = new OriginDetection(string.Empty);
            Assert.Null(originDetectionEmpty.ExternalData);
        }

        [Test]
        public void ExternalDataValid()
        {
            var expectedExternalData = "test";
            var originDetection = new OriginDetection(expectedExternalData);
            Assert.AreEqual(expectedExternalData, originDetection.ExternalData);
        }

        [TestCaseSource(typeof(ExternalDataSanitizeData), nameof(ExternalDataSanitizeData.TestCases))]
        public string ExternalDataInvalidCharactersSanitized(string rawExternalData)
        {
            var originDetection = new OriginDetection(rawExternalData);
            return originDetection.ExternalData;
        }
    }

    public class ExternalDataSanitizeData
    {
        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData("weee").Returns("weee");
                yield return new TestCaseData(" weee ").Returns("weee");
                yield return new TestCaseData("weee ").Returns("weee");
                yield return new TestCaseData(" weee").Returns("weee");
                yield return new TestCaseData("weee|").Returns("weee");
                yield return new TestCaseData("\t\n\rweee").Returns("weee");
            }
        }
    }
}

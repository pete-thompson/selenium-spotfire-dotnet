using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Selenium.Spotfire.Tests
{
    // This test is useful for running a quick smoke test to make sure nothing is catastrophically broken
    [TestClass]
    public class SimpleDriverTest
    {
        public TestContext TestContext { get; set; }

        private string[] SpotfireServerUrls
        {
            get
            {
                return TestContext.Properties.Cast<KeyValuePair<string, object>>().Where(i => i.Key.StartsWith("SpotfireServerURL")).Select(i => i.Value.ToString()).ToArray();
            }
        }
        private string TestFile
        {
            get
            {
                return TestContext.Properties["SpotfireTestDriverTestFile"].ToString();
            }
        }

        [TestCategory("SpotfireDriver Test")]
        [TestMethod]
        public void SimpleTest()
        {
            using (SpotfireDriver spotfire = SpotfireDriver.GetDriverForSpotfire())
            {
                spotfire.OpenSpotfireAnalysis(SpotfireServerUrls[0], TestFile);
                IReadOnlyCollection<string> pages = spotfire.GetPages();
                Assert.AreEqual(6, pages.Count, "We expect 6 pages");
            }
        }
    }
}

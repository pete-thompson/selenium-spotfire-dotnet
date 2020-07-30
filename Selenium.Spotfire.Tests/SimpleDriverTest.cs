using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Selenium.Spotfire.MSTest;

namespace Selenium.Spotfire.Tests
{
    // This test is useful for running a quick smoke test to make sure nothing is catastrophically broken
    [TestClass]
    public class SimpleDriverTest
    {
        public TestContext TestContext { get; set; }

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
            using (SpotfireTestDriver spotfire = SpotfireTestDriver.GetDriverForSpotfire(TestContext))
            {
                spotfire.ConfigureFromContext(1);
                spotfire.OpenSpotfireAnalysis(TestFile);
                IReadOnlyCollection<string> pages = spotfire.GetPages();
                Assert.AreEqual(6, pages.Count, "We expect 6 pages");
            }
        }
    }
}

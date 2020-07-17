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
        private string[] SpotfireUsernames
        {
            get
            {
                return TestContext.Properties.Cast<KeyValuePair<string, object>>().Where(i => i.Key.StartsWith("SpotfireUsername")).Select(i => i.Value.ToString()).ToArray();
            }
        }
        private string[] SpotfirePasswords
        {
            get
            {
                return TestContext.Properties.Cast<KeyValuePair<string, object>>().Where(i => i.Key.StartsWith("SpotfirePassword")).Select(i => i.Value.ToString()).ToArray();
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
            using (SpotfireDriver spotfire = SpotfireDriver.GetDriverForSpotfire((TestContext.Properties["ChromeHeadless"] ?? "").ToString().Length>0,
                                                                                 (TestContext.Properties["IncludeChromeLogs"] ?? "").ToString().Length>0))
            {
                if (SpotfireUsernames.Count()>0)
                {
                    spotfire.SetCredentials(SpotfireUsernames[0], SpotfirePasswords[0]);
                }
                spotfire.OpenSpotfireAnalysis(SpotfireServerUrls[0], TestFile);
                IReadOnlyCollection<string> pages = spotfire.GetPages();
                Assert.AreEqual(6, pages.Count, "We expect 6 pages");
            }
        }
    }
}

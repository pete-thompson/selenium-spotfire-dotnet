using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Selenium.Spotfire.MSTest;

namespace Selenium.Spotfire.Tests
{
    // This test is useful for running a quick smoke test to make sure nothing is catastrophically broken
    [TestClass]
    public class NoServerURLTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestNoServerURL() 
        {
            using (SpotfireTestDriver spotfire = SpotfireTestDriver.GetDriverForSpotfire(TestContext))
            {
                try
                {
                    spotfire.OpenSpotfireAnalysis("Dummy path");
                    Assert.Fail("We expected to fail because no Spotfire server URL was provided.");
                }
                catch (NoServerURLException)
                {
                    // all good
                }

            }
        }
    }
}

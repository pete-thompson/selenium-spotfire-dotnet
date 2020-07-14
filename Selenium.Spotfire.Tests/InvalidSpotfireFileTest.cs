using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Selenium.Spotfire.TestHelpers;

namespace Selenium.Spotfire.Tests
{
    [TestClass]
    public class InvalidSpotfireFileTest
    {
        public TestContext TestContext { get; set; }

        private string[] SpotfireServerUrls
        {
            get
            {
                return TestContext.Properties.Cast<KeyValuePair<string, object>>().Where(i => i.Key.StartsWith("SpotfireServerURL")).Select(i => i.Value.ToString()).ToArray();
            }
        }

        [TestMethod]
        public void TestInvalidFile()
        {
            MultipleAsserts checks = new MultipleAsserts();

            foreach (string spotfireURL in SpotfireServerUrls)
            {
                using (SpotfireDriver spotfire = SpotfireDriver.GetDriverForSpotfire())
                {
                    try
                    { 
                        spotfire.OpenSpotfireAnalysis(SpotfireServerUrls[0], "garbage path");
                        checks.CheckErrors(() => Assert.Fail("We expected an error when requesting a non-existing file"));
                    }
                    catch (SpotfireAPIException)
                    {
                        // ignore - we expect it
                    }
                }
            }

            checks.AssertEmpty();
        }
    }
}

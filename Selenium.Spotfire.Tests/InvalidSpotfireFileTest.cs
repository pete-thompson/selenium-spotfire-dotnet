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

        [TestMethod]
        public void TestInvalidFile()
        {
            MultipleAsserts checks = new MultipleAsserts();

            foreach (string spotfireURL in SpotfireServerUrls)
            {
                using (SpotfireDriver spotfire = SpotfireDriver.GetDriverForSpotfire())
                {
                    if (SpotfireUsernames.Count() > 0)
                    {
                        spotfire.SetCredentials(SpotfireUsernames[0], SpotfirePasswords[0]);
                    }

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

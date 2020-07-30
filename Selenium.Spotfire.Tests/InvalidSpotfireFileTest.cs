using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Selenium.Spotfire.MSTest;
using Selenium.Spotfire.TestHelpers;

namespace Selenium.Spotfire.Tests
{
    [TestClass]
    public class InvalidSpotfireFileTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestInvalidFile()
        {
            MultipleAsserts checks = new MultipleAsserts();

            int configuredCount = SpotfireTestDriver.ContextConfigurationCount(TestContext);
            for(int counter = 0; counter<configuredCount; counter++)
            {
                using (SpotfireTestDriver spotfire = SpotfireTestDriver.GetDriverForSpotfire(TestContext))
                {
                    spotfire.ConfigureFromContext(counter+1);

                    try
                    { 
                        spotfire.OpenSpotfireAnalysis("garbage path");
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

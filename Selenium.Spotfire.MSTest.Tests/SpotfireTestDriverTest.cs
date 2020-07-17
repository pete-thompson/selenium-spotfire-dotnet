using System;
using Selenium.Spotfire.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using Selenium.Spotfire.TestHelpers;

namespace Selenium.Spotfire.MSTest.Tests
{
    [TestClass]
    public class SpotfireTestDriverTest
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

        [TestMethod]
        public void TestScreenShots()
        {
            MultipleAsserts checks = new MultipleAsserts();

            TestingTestContext context = new TestingTestContext(TestContext);
            using (SpotfireTestDriver spotfire1 = SpotfireTestDriver.GetDriverForSpotfire(context))
            using (SpotfireTestDriver spotfire2 = SpotfireTestDriver.GetDriverForSpotfire(context))
            {
                if (SpotfireUsernames.Count()>0)
                {
                    spotfire1.SetCredentials(SpotfireUsernames[0], SpotfirePasswords[0]);
                    spotfire2.SetCredentials(SpotfireUsernames[0], SpotfirePasswords[0]);
                }
                spotfire1.OpenSpotfireAnalysis(SpotfireServerUrls[0], TestFile);
                spotfire1.CaptureScreenShot("example");
                spotfire2.OpenSpotfireAnalysis(SpotfireServerUrls[0], TestFile);
                spotfire2.CaptureScreenShot("example");
            }

            // Check that we have the expected output files
            checks.CheckErrors( () => Assert.AreEqual(4, context.ResultFileNames.Count));
            string baseFilename = TestContext.TestDir + "\\" + TestContext.FullyQualifiedTestClassName + "-" + TestContext.TestName;
            checks.CheckErrors(() => Assert.AreEqual(baseFilename + "-00000-00000-example.png", context.ResultFileNames[0]));
            checks.CheckErrors(() => Assert.AreEqual(baseFilename + "-00001-00000-example.png", context.ResultFileNames[1]));
            checks.CheckErrors(() => Assert.AreEqual(baseFilename + "-00001-00001-Final.png", context.ResultFileNames[2]));
            checks.CheckErrors(() => Assert.AreEqual(baseFilename + "-00000-00001-Final.png", context.ResultFileNames[3]));

            checks.AssertEmpty();
        }

        [TestMethod]
        public void TestExceptionDuringDispose()
        {
            TestingTestContext context = new TestingTestContext(TestContext);
            using (SpotfireTestDriver spotfire = SpotfireTestDriver.GetDriverForSpotfire(context))
            {
                spotfire.OpenSpotfireAnalysis(SpotfireServerUrls[0], TestFile);
                spotfire.CaptureScreenShot("example");
                context.ThrowErrorOnAddResult = true;
            }

            // Check that we have the expected output file
            Assert.AreEqual(1, context.ResultFileNames.Count);
            string baseFilename = TestContext.TestDir + "\\" + TestContext.FullyQualifiedTestClassName + "-" + TestContext.TestName;
            Assert.AreEqual(baseFilename + "-00000-00000-example.png", context.ResultFileNames[0]);
        }

        [TestMethod]
        public void CheckNoOutput()
        {
            TestingTestContext context = new TestingTestContext(TestContext);
            using (SpotfireTestDriver spotfire = SpotfireTestDriver.GetDriverForSpotfire(context))
            {
                // Do nothing
            }
            Assert.AreEqual(0, context.Lines.Count);
        }

        [TestMethod]
        public void CheckOutput()
        {
            TestingTestContext context = new TestingTestContext(TestContext);
            using (SpotfireTestDriver spotfire = SpotfireTestDriver.GetDriverForSpotfire(context))
            {
                spotfire.OpenSpotfireAnalysis(SpotfireServerUrls[0], TestFile);
            }
            Assert.AreNotEqual(0, context.Lines.Count);
        }
    }
}

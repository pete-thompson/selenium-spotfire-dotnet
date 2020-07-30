using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Selenium.Spotfire;

namespace Selenium.Spotfire.MSTest
{
    /// <summary>
    /// A Selenium "driver" for running tests against Spotfire
    /// * We always use Chromium - we're not worried about cross browser testing and using a specific driver simplifies things
    /// * We require a TestContext object because we write out standard messages and create attachments, simplifying test authoring by enforcing standards
    /// * There are lots of assumptions made about Spotfire's internals, which means tests may fail on future versions (which is probably OK anyway!)
    /// * We're integrated with IQVIA's standard usage tracking capture so that we can mark automated tests in the usage data but tests will work regardless of whether the usage tracking is present.
    /// </summary>
    public class SpotfireTestDriver : SpotfireDriver
    {
        // Tracks how many drivers are associated with a test context, used to keep the screenshots separate
        private int DriverNumber;

        // Flag: Has Dispose already been called?
        bool disposed;

        // Allows us to ensure screenshot filenames are ordered
        private int ScreenshotCounter;

        // The TestContext associated with the current test
        private TestContext TestContext { get; set; }

        private static Dictionary<TestContext, int> ContextDriverCounter = new Dictionary<TestContext, int>();

        // Constructors are private - we construct through the static method GetDriverForSpotfire
        protected SpotfireTestDriver(ChromeDriverService service, ChromeOptions options, TimeSpan commandTimeout) : base(service, options, commandTimeout)
        {
        }

        // Protected implementation of Dispose pattern.
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    try
                    {
                        CaptureScreenShot("Final");
                    }
                    catch
                    {
                        // ignore - just want to make sure that we call the quit method
                    }
                }

                disposed = true;
                // Call base class implementation.
                base.Dispose(disposing);
            }
        }


        /// <summary>
        /// Get a Selenium driver that we can use for testing Spotfire
        /// </summary>
        /// <param name="testContext">The current test context</param>
        /// <returns></returns>
        public static SpotfireTestDriver GetDriverForSpotfire(TestContext testContext)
        {
            SpotfireTestDriver driver;

            if ((testContext.Properties["DownloadChromeDriver"]?? "").ToString().Length>0) 
            {
                SpotfireDriver.GetChromeDriver();
            }

            driver = GetDriverForSpotfire<SpotfireTestDriver>((testContext.Properties["ChromeHeadless"] ?? "").ToString().Length>0,
                                                              (testContext.Properties["IncludeChromeLogs"] ?? "").ToString().Length>0);

            driver.TestContext = testContext;

            if (ContextDriverCounter.ContainsKey(testContext))
            {
                driver.DriverNumber = ++ContextDriverCounter[testContext];
            }
            else
            {
                driver.DriverNumber = 0;
                ContextDriverCounter.Add(testContext, 0);
            }

            driver.SetDownloadFolder(testContext.TestDir);

            return driver;
        }

        /// <summary>
        /// Counts how many Spotfire serves we have configured in the TestContext
        /// </summary>
        public static int ContextConfigurationCount(TestContext testContext)
        {
            return testContext.Properties.Cast<KeyValuePair<string, object>>().Where(i => i.Key.StartsWith("SpotfireServerURL")).Select(i => i.Value.ToString()).ToArray().Length;
        }

        /// <summary>
        /// Read configuration from the TestContext
        /// If available, we'll read the URL, username and password from the TestContext
        /// </summary>
        public void ConfigureFromContext(int serverNumber)
        {
            if (TestContext.Properties.Contains(string.Format("SpotfireServerURL{0}", serverNumber)))
            {
                SetServerUrl(TestContext.Properties[string.Format("SpotfireServerURL{0}", serverNumber)].ToString());
                if ((TestContext.Properties.Contains(string.Format("SpotfireUsername{0}", serverNumber))) &&
                    (TestContext.Properties.Contains(string.Format("SpotfirePassword{0}", serverNumber))))
                {
                    SetCredentials(TestContext.Properties[string.Format("SpotfireUsername{0}", serverNumber)].ToString(),
                                   TestContext.Properties[string.Format("SpotfirePassword{0}", serverNumber)].ToString());
                }
            }
        }

        /// <summary>
        /// Capture a screenshot and attach to the test results.
        /// Screenshots will be automatically named using the test case name, a counter representing how many drivers have been used by the test case, then the screenshot number for this driver.
        /// </summary>
        public void CaptureScreenShot(string stepName)
        {
            SetDownloadFolder(TestContext.TestDir);
            Screenshot ss = ((ITakesScreenshot)this).GetScreenshot();
            string path = TestContext.TestDir + Path.DirectorySeparatorChar + TestContext.FullyQualifiedTestClassName + "-" + TestContext.TestName + 
                "-" + DriverNumber.ToString("00000") + "-" + ScreenshotCounter.ToString("00000") + "-" + stepName + ".png";
            ss.SaveAsFile(path);
            this.TestContext.AddResultFile(path);
            ScreenshotCounter++;
        }

        /// <summary>
        /// Output a status message to the test context.
        /// </summary>
        /// <param name="message"></param>
        public override void OutputStatusMessage(string message)
        {
            TestContext.WriteLine(message);
            base.OutputStatusMessage(message);
        }
    }
}

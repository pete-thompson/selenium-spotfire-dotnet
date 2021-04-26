using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Selenium.Spotfire;
using Selenium.Spotfire.TestHelpers;

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
        /// Generate a path for a file to add to the test context results.
        /// The file will be created in the test context folder and will be 
        /// named with the test class name, the test name, a number to separate
        /// different Spotfire drivers from each other (if more than one driver is
        /// associated with a test context) and then the chosen suffix
        /// </summary>
        public string ResultFilePath(string suffix)
        {
            string driverNumber = (DriverNumber == 0) ? "" : "-" + DriverNumber.ToString("00000");
            string filename = TestContext.FullyQualifiedTestClassName + "-" + TestContext.TestName +  driverNumber
                 + "-" + suffix;
            string path = Path.Combine(TestContext.TestDir, filename);
            return path;
        }

        /// <summary>
        /// Capture a screenshot and attach to the test results.
        /// Screenshots will be automatically named using the test case name, a counter representing how many drivers have been used by the test case, then the screenshot number for this driver.
        /// </summary>
        public void CaptureScreenShot(string stepName)
        {
            SetDownloadFolder(TestContext.TestDir);
            Screenshot ss = ((ITakesScreenshot)this).GetScreenshot();
            string path = ResultFilePath(ScreenshotCounter.ToString("00000") + "-" + stepName + ".png");
            ss.SaveAsFile(path);
            this.TestContext.AddResultFile(path);
            OutputStatusMessage(string.Format("Screenshot captured to {0}", path));
            ScreenshotCounter++;
        }

        /// <summary>
        /// Output a status message to the test context.
        /// </summary>
        /// <param name="message"></param>
        public override void OutputStatusMessage(string message)
        {
            if (!MessagesSuppressed)
            {
                TestContext.WriteLine(message);
            }
            base.OutputStatusMessage(message);
        }

        /// <summary>
        /// Test the contents of the current analysis against expected pages/visuals/images etc.
        /// See the readme file for more details.
        /// <summary>
        public void TestAnalysisContents(List<ExpectedPage> expectedPages, MultipleAsserts checks, string imagesFolder = null, string dataFilesFolder = null, bool ignoreExtraPages = false)
        {
            string instanceMessage = (DriverNumber == 0) ? "" : string.Format(", instance {0}", DriverNumber);

            if (imagesFolder == null) 
            {
                imagesFolder = Environment.GetEnvironmentVariable("images_folder");
            }
            if (dataFilesFolder == null)
            {
                dataFilesFolder = Environment.GetEnvironmentVariable("datafiles_folder");
            }

            IReadOnlyCollection<string> pages = GetPages();
            if (!ignoreExtraPages)
            {
                checks.CheckErrors(() => Assert.AreEqual(expectedPages.Count, pages.Count, "Mismatching number of pages{0}", instanceMessage));
            }

            foreach (ExpectedPage page in expectedPages)
            {
                if (!pages.Contains(page.Title))
                {
                    checks.CheckErrors(() => Assert.Fail("Page {0} is missing{1}", page.Title, instanceMessage));
                }
                else
                {
                    SetActivePage(page.Title, 60);
                    RestoreVisualLayout();
                    CaptureScreenShot("Page-" + page.Title);

                    List<Visual> visuals = GetVisuals();
                    if (!page.IgnoreExtraVisuals) 
                    {
                        checks.CheckErrors(() => Assert.AreEqual(page.Visuals.Count, visuals.Count, "Mismatching number of visuals on page {0}{1}", page.Title, instanceMessage));
                    }

                    foreach (ExpectedVisual expectedVisual in page.Visuals)
                    {
                        Visual visual = visuals.Find(x => x.Title == expectedVisual.Title);
                        if (visual == null)
                        {
                            checks.CheckErrors(() => Assert.Fail("Visual {0} was not found on page {1}{2}", expectedVisual.Title, page.Title, instanceMessage));
                        }
                        else 
                        {
                            TestContext.WriteLine("Page {0}, visual title {1}{2}", page.Title, visual.Title, instanceMessage);

                            ExpectedVisual.Type actualType = visual.IsTextType ? ExpectedVisual.Type.Textual : (visual.IsImageType ? ExpectedVisual.Type.Image : ExpectedVisual.Type.Tabular);

                            checks.CheckErrors(() => Assert.AreEqual(expectedVisual.VisualType, actualType, "Visual type did not match expected value. Page {0}, visual {1}{2}", page.Title, expectedVisual.Title, instanceMessage));

                            string expectedFile = string.Format("{0}-{1}-{2}-{3}", TestContext.FullyQualifiedTestClassName, TestContext.TestName, page.Title, visual.Title);

                            if (visual.IsTextType)
                            {
                                if (dataFilesFolder != null)
                                {
                                    string expectedDataPath = Path.Combine(dataFilesFolder, string.Format("{0}.txt", expectedFile));
                                    if (File.Exists(expectedDataPath))
                                    {
                                        // Files are created on Windows system but might be used for tests run under Linux
                                        string expected = File.ReadAllText(expectedDataPath).Replace("\r\n", Environment.NewLine);
                                        checks.CheckErrors(() => Assert.AreEqual(expected, visual.Text, "Text for visual {0} on page {1} does not match expected{2}.", visual.Title, page.Title, instanceMessage));
                                    }
                                    else
                                    {
                                        TestContext.WriteLine("File {0} not found, so text comparison not performed{1}.", expectedFile, instanceMessage);
                                    }
                                }
                                else
                                {
                                    TestContext.WriteLine("No datafiles folder specified, so text comparison not performed{0}.", instanceMessage);
                                }
                                string path = ResultFilePath(page.Title + "-" + visual.Title + ".txt");
                                File.WriteAllText(path,visual.Text);
                                this.TestContext.AddResultFile(path);
                            }
                            else if (visual.IsImageType)
                            {
                                TestContext.WriteLine(string.Format("Image visual, content size: ({0},{1})", visual.Content.Size.Width, visual.Content.Size.Height));

                                if (imagesFolder != null)
                                {
                                    Dictionary<string, Bitmap> imageComparisons = new Dictionary<string, Bitmap>();

                                    bool anyMatch = VisualCompare.CompareVisualImages(visual, 
                                                                                    imagesFolder,
                                                                                    expectedFile,
                                                                                    imageComparisons);

                                    // If there's no match we need to write out the mismatches
                                    if (!anyMatch)
                                    {
                                        TestContext.WriteLine("Images didn't match, check the test results folder for the new image along with images showing comparison with existing possibilities.");
                                        foreach(KeyValuePair<string, Bitmap> imageToSave in imageComparisons)
                                        {
                                            string filename = ResultFilePath(page.Title + "-" + visual.Title + imageToSave.Key);
                                            imageToSave.Value.Save(filename);
                                            this.TestContext.AddResultFile(filename);
                                        }
                                    }
                                    checks.CheckErrors(() => Assert.IsTrue(anyMatch, "Image for visual {0} on page {1} does not match a possible expected image{2}", visual.Title, page.Title, instanceMessage));
                                }
                                else
                                {
                                    TestContext.WriteLine("No image files folder specified, so image comparison not performed{0}.", instanceMessage);
                                    string filename = ResultFilePath(page.Title + "-" + visual.Title + ".png");
                                    visual.GetImage().Save(filename);
                                    this.TestContext.AddResultFile(filename);
                                }
                            }
                            else if (visual.IsTabularType)
                            {
                                using (TableData data = visual.GetTableData())
                                {
                                    string path = ResultFilePath(page.Title + "-" + visual.Title + ".txt");
                                    data.SaveToFile(path);
                                    this.TestContext.AddResultFile(path);

                                    if (dataFilesFolder != null)
                                    {
                                        string expectedDataPath = Path.Combine(dataFilesFolder, string.Format("{0}.txt", expectedFile));
                                        if (File.Exists(expectedDataPath))
                                        {
                                            using (TableData expectedData = new TableDataFromDelimitedFile(expectedDataPath))
                                            {
                                                TestContext.WriteLine(string.Format("Tabular data, {0} columns", data.Columns.Length));
                                                checks.CheckErrors(() => Assert.IsTrue(CompareUtilities.AreEqual(expectedData, data), "Data for visual {0} on page {1} does not match expected data{2}", visual.Title, page.Title, instanceMessage));
                                            }
                                        }
                                        else
                                        {
                                            TestContext.WriteLine("File {0} not found, so table comparison not performed{1}.", expectedFile, instanceMessage);
                                        }
                                    }
                                    else
                                    {
                                        TestContext.WriteLine("No datafiles folder specified, so table comparison not performed{0}.", instanceMessage);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

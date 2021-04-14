using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

[assembly: InternalsVisibleTo("Selenium.Spotfire.Tests")]
namespace Selenium.Spotfire
{
    /// <summary>
    /// A Selenium "driver" for running tests against Spotfire
    /// * We always use Chromium - we're not worried about cross browser testing and using a specific driver simplifies things
    /// * There are lots of assumptions made about Spotfire's internals, which means tests may fail on future versions (which is probably OK anyway!)
    /// * We're integrated with IQVIA's standard usage tracking capture so that we can mark automated tests in the usage data but tests will work regardless of whether the usage tracking is present.
    /// </summary>
    public class SpotfireDriver : ChromeDriver
    {
        /// <summary>
        /// The OutputStatusMessage method will write to the console if this flag is set to true.
        /// </summary>
        public bool OutputToConsole { get; set; }

        /// <summary>
        ///  The chrome service - allows us to send configuration commands
        /// </summary>
        private ChromeDriverService DriverService;

        // Flag: Has Dispose already been called?
        bool disposed;

        /// <summary>
        /// The URL of the server we're connected to
        /// </summary>
        private string ServerURL = "";

        /// <summary>
        /// Spotfire's localization strings
        /// </summary>
        private Dictionary<string, string> Localization;

        /// <summary>
        /// Cache whether a server is Spotfire 10 or not
        /// </summary>
        private static Dictionary<string, bool> IsSpotfire10Cache = new Dictionary<string, bool>();
        /// <summary>
        /// Cache whether a server is Spotfire 10.3 or not
        /// </summary>
        private static Dictionary<string, bool> IsSpotfire103Cache = new Dictionary<string, bool>();
        /// <summary>
        /// Cache whether a server is Spotfire 10.10 or not
        /// </summary>
        private static Dictionary<string, bool> IsSpotfire1010Cache = new Dictionary<string, bool>();

        private string TemporaryChromeExtensionsFolder;
        private string ChromeLog;

        // Whether we've loaded our JS wrapper yet
        private bool WrapperLoaded = false;

        // These constructors are private - we construct through the static method GetDriverForSpotfire
        protected SpotfireDriver(ChromeDriverService service, ChromeOptions options, TimeSpan commandTimeout) : base(service, options, commandTimeout)
        {
        }

        // Protected implementation of Dispose pattern.
        protected override void Dispose(bool disposing)
        {

            if (!disposed && disposing)
            {
                try
                {
                    // Dispose the driver service
                    DriverService.Dispose();

                    CleanUpTempFiles(ChromeLog, TemporaryChromeExtensionsFolder);

                    ChromeLog = null;
                    TemporaryChromeExtensionsFolder = null;
                }
                catch
                {
                    // ignore 
                }
            }

            disposed = true;
            // Call base class implementation.
            base.Dispose(disposing);
        }

        private static void CleanUpTempFiles(string chromeLog, string temporaryChromeExtensionsFolder) 
        {
            if (chromeLog != null)
            {
                if (File.Exists(chromeLog))
                {
                    Console.WriteLine("Chrome log file:");
                    foreach (String line in File.ReadAllLines(chromeLog)) 
                    {
                        Console.WriteLine(line);
                    }
                    File.Delete(chromeLog);
                }
                // Check if the parent folder is now empty and we can delete it
                if (Directory.GetParent(Path.GetDirectoryName(chromeLog)).GetDirectories().Length == 0)
                {
                    Directory.GetParent(Path.GetDirectoryName(chromeLog)).Delete();
                }
            }
            if (temporaryChromeExtensionsFolder != null)
            {
                // Delete the ChromeExtensions temporary files
                Directory.Delete(temporaryChromeExtensionsFolder, true);
                // Check if the parent folder is now empty and we can delete it
                if (Directory.GetParent(temporaryChromeExtensionsFolder).GetDirectories().Length == 0)
                {
                    Directory.GetParent(temporaryChromeExtensionsFolder).Delete();
                }
            }
        }

        /// <summary>
        /// Unpack our chrome extensions into the test folder ready for use
        /// </summary>
        /// <returns>A list of extensions</returns>
        private static string[] UnpackChromeExtensions(string temporaryChromeExtensionsFolder)
        {
            List<string> answer = new List<string>();

            foreach (string file in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                if (file.Contains(".ChromeExtensions."))
                {
                    Regex rex = new Regex(@"\.ChromeExtensions\.(.*)\.([^\.]*)\.([^\.]*)$");
                    Match match = rex.Match(file);
                    string dirPart = temporaryChromeExtensionsFolder + Path.DirectorySeparatorChar + match.Groups[1].Value;
                    string filePart = match.Groups[2].Value + "." + match.Groups[3].Value;

                    if (!Directory.Exists(dirPart))
                    {
                        Directory.CreateDirectory(dirPart);
                    }
                    if (!answer.Contains(dirPart))
                    {
                        answer.Add(dirPart);
                    }

                    Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(file);
                    using (var reader = new StreamReader(stream))
                    {
                        string fileContent = reader.ReadToEnd();
                        File.WriteAllBytes(dirPart + Path.DirectorySeparatorChar + filePart, Encoding.UTF8.GetBytes(fileContent));
                    }
                }
            }

            return answer.ToArray();
        }

        ///<summary>
        /// Fetch the latest version of the ChromeDriver.
        /// Use this if you're running on a machine that has the latest version of Chrome but may not have ChromeDriver installed
        /// </summary>
        public static void GetChromeDriver()
        {
            new DriverManager().SetUpDriver(new ChromeConfig());
        }


        /// <summary>
        /// Get a Selenium driver that we can use for testing Spotfire
        /// </summary>
        /// <typeparam name="TDriver">The type of driver to use (any subclass of SpotfireDriver)</typeparam>
        /// <param name="headless">Whether Chrome will run 'headless' or not (i.e. no visible window)</param>
        /// <returns></returns>
        public static TDriver GetDriverForSpotfire<TDriver>(bool headless = false, bool includeChromeLogs = false) where TDriver: SpotfireDriver
        {
            TDriver driver=null;

            // If we're running in a container we run with --no-sandbox since we're likely running as root
            // The DotNet docker images set this environment variable for us
            bool inDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

            // Set up Chrome's options
            var chromeOptions = new ChromeOptions();
            if (headless)
            {
                chromeOptions.AddArgument("--headless");
            }

            if (inDocker)
            {
                // Our Docker container runs as root, so we need to use the less secure no-sandbox mode - but that shouldn't be an issue because
                // (a) we're only opening controlled Spotfire sites and (b) we're using Docker containers that will be torn down after use
                chromeOptions.AddArgument("--no-sandbox");
                // Ideally we'd ask that the --shm-size parameter be used to give Chrome more shared memory to work with, but sometimes that isn't possible (e.g. under GitLab)
                chromeOptions.AddArgument("--disable-dev-shm-usage");
            }

            chromeOptions.AddArgument("--auth-server-whitelist=*");
            chromeOptions.AddArgument("--window-size=1920,1080");

            string chromeLog = Path.Combine(Path.GetTempPath(), "SpotfireDriverChrome","chrome.log");
            if (includeChromeLogs) 
            {
                chromeOptions.AddArgument("--enable-logging");
                chromeOptions.AddArgument("--log-file=" + chromeLog);
                chromeOptions.AddArgument("--log-level=0");
            }
 
            string temporaryChromeExtensionsFolder = Path.Combine(Path.GetTempPath(), "SpotfireDriverChrome", Path.GetRandomFileName());
            string[] extensions = UnpackChromeExtensions(temporaryChromeExtensionsFolder);
            if (extensions.Length > 0)
            {
                chromeOptions.AddArgument("--load-extension=" + string.Join(",", extensions));
            }

            Console.WriteLine("Chrome options: {0}", chromeOptions.ToString());

            var driverService = ChromeDriverService.CreateDefaultService();
            try
            {
                driver = (TDriver)Activator.CreateInstance(typeof(TDriver),
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.CreateInstance | BindingFlags.Instance,
                    null,
                    new object[] { driverService, chromeOptions, TimeSpan.FromMinutes(30) },
                    CultureInfo.InvariantCulture);
                driverService.LogPath = chromeLog;
                if (includeChromeLogs)
                {
                    driverService.EnableVerboseLogging = true;
                }
                driver.DriverService = driverService;
                driver.TemporaryChromeExtensionsFolder = temporaryChromeExtensionsFolder;
                driver.ChromeLog = chromeLog;
            }
            catch
            {
                // Write out diagnostic information
                Console.WriteLine("Failed to start ChromeDriver");
                // Clean anything up here - our object wasn't created so doesn't dispose
                CleanUpTempFiles(chromeLog, temporaryChromeExtensionsFolder);
                // Chrome hasn't started (perhaps a mismatch in versions?), so stop the driver process
                driverService.Dispose();
                throw;
            }

            return driver;
        }

        /// <summary>
        /// Get a Selenium driver that we can use for testing Spotfire
        /// </summary>
        /// <param name="headless">Whether Chrome will run 'headless' or not (i.e. no visible window)</param>
        /// <returns></returns>
        public static SpotfireDriver GetDriverForSpotfire(bool headless = false,  bool includeChromeLogs = false)
        {
            return GetDriverForSpotfire<SpotfireDriver>(headless, includeChromeLogs);
        }

        /// <summary>
        /// Set the folder where download files will be stored
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public async Task SetDownloadFolderAsync(string folder)
        {
            var param = new Dictionary<string, string>
            {
                { "behavior", "allow" },
                { "downloadPath", folder }
            };

            var cmdParam = new Dictionary<string, object>
            {
                { "cmd", "Page.setDownloadBehavior" },
                { "params", param }
            };

            var content = new StringContent(JsonConvert.SerializeObject(cmdParam), Encoding.UTF8, "application/json");

            var url = DriverService.ServiceUrl + "session/" + SessionId + "/chromium/send_command";
            var httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.PostAsync(url, content);
            await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Set the folder where download files will be stored
        /// </summary>
        /// <param name="folder"></param>
        public void SetDownloadFolder(string folder)
        {
            SetDownloadFolderAsync(folder).Wait();
        }

        /// <summary>
        /// Ensure that our JS wrapper has been loaded into the browser
        /// </summary>
        private void EnsureWrapperLoaded()
        {
            if (!WrapperLoaded)
            {
                SpotfireWrapperServer.StartupServer();
                this.Navigate().GoToUrl("http://localhost:" + SpotfireWrapperServer.Port + "/SpotfireWrapper.html");
                SpotfireWrapperServer.StopServer();

                WrapperLoaded = true;
            }
        }

        /// <summary>
        /// Set credentials to be used if the browser receives a 'challenge' for username/password (e.g. when using Integrated Windows Authentication)
        /// </summary>
        /// <param name="username">Username to use. Blank values are ignored.</param>
        /// <param name="password">Password to use</param>
        public void SetCredentials(string username, string password)
        {
            EnsureWrapperLoaded();

            if (username.Length > 0)
            {
                ((IJavaScriptExecutor)this).ExecuteScript("SpotfireTestWrapper.setCredentials(arguments[0],arguments[1])", username, password);
            }
        }

        /// <summary>
        /// Set the URL to use for the Spotfire server
        /// </summary>
        /// <param name="serverUrl">The URL to the server</param>
        public void SetServerUrl(string serverUrl) 
        {
            ServerURL = serverUrl;
        }

        /// <summary>
        /// Open a Spotfire analysis and wait for it to fully open
        /// </summary>
        /// <param name="filePath">The path to the Spotfire analysis file</param>
        /// <param name="configurationBlock">The configuration block to pass to Spotfire</param>
        /// <param name="waitForCompletion">Whether to wait for the report to finish opening or not</param>
        /// <param name="timeoutInSeconds">How long to wait before throwing a timeout error</param>
        public void OpenSpotfireAnalysis(string filePath, string configurationBlock = "", bool waitForCompletion = true, int timeoutInSeconds = 120)
        {
            if (ServerURL == "") 
            {
                OutputStatusMessage(string.Format("No server URL provided when attempting to open report. Path {0}. Configuration Block: {1}", filePath, configurationBlock));
                throw new NoServerURLException();
            }

            OutputStatusMessage(String.Format("Opening Spotfire analysis from server {0}. Path {1}. Configuration Block: {2}", ServerURL, filePath, configurationBlock));

            EnsureWrapperLoaded();

            const string OpenScript = "SpotfireTestWrapper.startSpotfire(arguments[0],arguments[1],arguments[2]);";

            ((IJavaScriptExecutor)this).ExecuteScript(OpenScript, ServerURL, filePath, configurationBlock);

            if (waitForCompletion)
            {
                WaitForAnalysisToOpen(timeoutInSeconds);
            }
        }

        /// <summary>
        /// Wait for an analysis to complete opening (assumes that we have first called OpenSpotfireAnalysis
        /// </summary>
        /// <param name="timeoutInSeconds">How long to wait before throwing a timeout error</param>
        public void WaitForAnalysisToOpen(int timeoutInSeconds = 120)
        {
            WebDriverWait wait = new WebDriverWait(this, TimeSpan.FromSeconds(timeoutInSeconds));

            // Wait until Spotfire reports that it is complete
            bool started = wait.Until(drv => ((IJavaScriptExecutor)drv).ExecuteScript("return SpotfireTestWrapper.isOpened() || SpotfireTestWrapper.hadError()").Equals(true));

            // Throw any errors (e.g. file not found)
            GetApiErrorsAndThrow();

            // Switch over to the Spotfire content so that all selectors work as you might expect - we'll switch up to the parent as and when we need to call our API
            this.SwitchTo().Frame(0);

            // Make sure our usage tracking knows this is automated
            if (started)
            {
                ((IJavaScriptExecutor)this).ExecuteScript("if (window.SpotfireUsageTracking) window.SpotfireUsageTracking.IsAutomatedTesting()");
            }

            // Grab localization information
            IReadOnlyDictionary<string, object> answer = (IReadOnlyDictionary<string, object>)((IJavaScriptExecutor)this).ExecuteScript("return Spotfire.Localization.resources");
            Localization = answer
                .ToDictionary(x => x.Key,
                              x => (x.Value == null ? "" : (x.Value is IEnumerable<object> ? String.Join(", ", x.Value as IEnumerable<object>) : x.Value.ToString())));

            SetWindowSizeForMatchingSizes(false);
        }

        /// <summary>
        /// Check if Spotfire is ready
        /// </summary>
        /// <returns></returns>
        public bool IsSpotfireReady()
        {
            bool answer=true;

            // First check if the 'Ready' indicator is present
            if (!IsSpotfire1010OrAbove())
            {
                if (IsSpotfire10OrAbove())
                {
                    answer = FindElements(By.CssSelector(string.Format("div[class*='sfx_top-bar'] div[title='{0}']", Localization["Ready"]))).Count > 0;
                }
                else
                {
                    answer = FindElements(By.CssSelector(string.Format(".sf-element-status-bar div[title='{0} ']", Localization["Ready"]))).Count > 0;
                }
            }

            // Now check if any visualisations are busy (e.g. loading images)
            answer = answer && (bool)ExecuteScript("return Spotfire.Busy.idle()");

            return answer;
        }

        /// <summary>
        /// Wait until Spotfire has finished processing
        /// </summary>
        /// <param name="driver">The Selenium WebDriver</param>
        /// <param name="testContext">The test context object</param>
        /// <param name="timeout">A timeout</param>
        public void WaitUntilSpotfireReady(int timeoutInSeconds = 30)
        {
            OutputStatusMessage("Waiting for Spotfire to finish processing");

            bool isReallyReady = false;
            while (!isReallyReady)
            {
                WebDriverWait wait = new WebDriverWait(this, TimeSpan.FromSeconds(timeoutInSeconds));

                wait.Until((drv) => IsSpotfireReady());

                // Take a slight pause and make sure Spotfire hasn't started doing something else
                Thread.Sleep(500);
                isReallyReady = IsSpotfireReady();
            }
        }

        /// <summary>
        /// Check for Spotfire 10.0
        /// </summary>
        /// <returns></returns>
        public bool IsSpotfire10OrAbove()
        {
            bool answer;

            if (ServerURL.Length > 0 && IsSpotfire10Cache.ContainsKey(ServerURL))
            {
                answer = IsSpotfire10Cache[ServerURL];
            }
            else
            {
                int version = Convert.ToInt16(((IJavaScriptExecutor)this).ExecuteScript("return Spotfire.AppIntegration.version()"));

                answer = version >= 5;

                if (ServerURL.Length > 0)
                {
                    IsSpotfire10Cache.Add(ServerURL, answer);
                }
            }

            return answer;
        }

        /// <summary>
        /// Check for Spotfire 10.3
        /// </summary>
        /// <returns></returns>
        public bool IsSpotfire103OrAbove()
        {
            bool answer;

            if (ServerURL.Length > 0 && IsSpotfire103Cache.ContainsKey(ServerURL))
            {
                answer = IsSpotfire103Cache[ServerURL];
            }
            else
            {
                string notificationsPanelExists = (((IJavaScriptExecutor)this).ExecuteScript("return Spotfire.Localization.resources.Notifications_ClearAll?'yes':'no'")).ToString();

                answer = notificationsPanelExists == "yes";

                if (ServerURL.Length > 0)
                {
                    IsSpotfire103Cache.Add(ServerURL, answer);
                }
            }

            return answer;
        }

        /// <summary>
        /// Check for Spotfire 10.10
        /// </summary>
        /// <returns></returns>
        public bool IsSpotfire1010OrAbove()
        {
            bool answer;

            if (ServerURL.Length > 0 && IsSpotfire1010Cache.ContainsKey(ServerURL))
            {
                answer = IsSpotfire1010Cache[ServerURL];
            }
            else
            {
                string readyTextExists = (((IJavaScriptExecutor)this).ExecuteScript("return Spotfire.Localization.resources.Ready?'yes':'no'")).ToString();

                answer = readyTextExists == "no";

                if (ServerURL.Length > 0)
                {
                    IsSpotfire1010Cache.Add(ServerURL, answer);
                }
            }

            return answer;
        }

        /// <summary>
        /// Finds the element and optionally waits for a short time for it to be there
        /// </summary>
        /// <param name="driver">The Selenium WebDriver</param>
        /// <param name="testContext">The test context object</param>
        /// <param name="description">The description to include in the log message</param>
        /// <param name="by">A selector</param>
        /// <param name="timeoutInSeconds">How long we're prepared to wait for the element to appear</param>
        /// <returns></returns>
        public IWebElement FindElement(string description, By by, int timeoutInSeconds = 5)
        {
            OutputStatusMessage(String.Format("Looking for element: {0}", description));
            if (timeoutInSeconds > 0)
            {
                var wait = new WebDriverWait(this, TimeSpan.FromSeconds(timeoutInSeconds));
                return wait.Until(drv => drv.FindElement(by));
            }
            return this.FindElement(by);
        }

        public void WaitUntilElementDisappears(string description, By by, int timeoutInSeconds = 5)
        {
            OutputStatusMessage(String.Format("Waiting for element to disappear: {0}", description));
            var wait = new WebDriverWait(this, TimeSpan.FromSeconds(timeoutInSeconds));
            wait.Until(drv =>
            {
                bool gone;
                try
                {
                    gone = !drv.FindElement(by).Displayed;
                }
                catch (NoSuchElementException)
                {
                    gone = true;
                }
                return gone;
            });
        }

        /// <summary>
        /// Resize the main window such that visualisations match sizes in 7.x and 10.x
        /// </summary>
        /// <param name="maximized"></param>
        public void SetWindowSizeForMatchingSizes(bool maximized)
        {
            Manage().Window.Size = IsSpotfire10OrAbove() ? (maximized ? new Size(1936, 1105) : new Size(1936, 1106)) : new Size(1920, 1080);
        }

        /// <summary>
        /// Get Spotfire's localization information - a dictionary that maps IDs to the text that will appear on screen in the local language
        /// Use this when searching for text on the page that could have been localized
        /// Unfortunately, this only includes text that is generated on the client side - certain parts of the user interface are generated by the server
        /// and could also be localized, but that information isn't available to the client.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyDictionary<string, string> GetLocalization()
        {
            return Localization;
        }

        /// <summary>
        /// Get any notifications shown by Spotfire
        /// </summary>
        /// <returns>The notifications concatenated into a single string</returns>
        public string GetNotifications()
        {
            string answer = "";

            OutputStatusMessage("Checking for notifications");

            // Look for the warning icon
            IWebElement notificationsButton = null;

            if (IsSpotfire10OrAbove())
            {
                try
                {
                    // Note we don't care whether there are any notifications - Spotfire lets us click it and get a blank dialog
                    notificationsButton = FindElementByCssSelector("div[class^='sfx_button'][title='Notifications']");
                }
                catch
                {
                    // ignore
                }
            }
            else
            {
                try
                {
                    notificationsButton = FindElementByCssSelector("div.sfc-warning-button");
                }
                catch
                {
                    try
                    {
                        notificationsButton = FindElementByCssSelector("div.sfc-error-button");
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
            if (notificationsButton != null)
            {
                // Click the notifications button
                notificationsButton.Click();

                if (!IsSpotfire103OrAbove())
                {
                    IWebElement dialog = FindElement("Messages dialog", By.CssSelector("div.sf-modal-dialog-content[title='Messages']"));
                    answer = dialog.Text;

                    // Click the dismiss button in the dialog
                    dialog.FindElement(By.XPath("../..")).FindElement(By.CssSelector("button")).Click();
                }
                else
                {
                    // Spotfire 10.3 and beyond
                    IWebElement notifications = FindElement("Notification panel", By.CssSelector("div[class^='sfx_notification-panel']"));
                    answer = "";
                    // Wait for the transition to finish to give time for elements to appear (0.25s transition in 10.3)
                    Thread.Sleep(2000);

                    foreach (IWebElement text in notifications.FindElements(By.CssSelector("div[class^='sfx_text-wrapper']")))
                    {
                        text.Click();
                        IWebElement dialog = FindElement("Notification details dialog", By.CssSelector("div.sf-element-modal-dialog-body textarea"));
                        // Remove the extra blank lines so the notifications match pre-10.3 text
                        answer += dialog.GetAttribute("value").Replace(Environment.NewLine + Environment.NewLine,Environment.NewLine);
                        FindElement(By.CssSelector("div.sf-element-modal-dialog  button:not(.sfpc-secondary)")).Click();
                        WaitUntilElementDisappears("Notification details dialog", By.CssSelector("div.sf-element-modal-dialog"));
                    }

                    // Remove trailing newline so we match previous versions of Spotfire
                    answer = Regex.Replace(answer, "(" + Environment.NewLine + ")+$", "");

                    // Click dismiss all button
                    notifications.FindElement(By.CssSelector("div[class^='sfx_notification-clear']")).Click();
                    // Click the toolbar button again to hide the panel
                    notificationsButton.Click();
                    WaitUntilElementDisappears("Notification panel", By.CssSelector("div[class^='sfx_notification-panel']"));
                }
            }

            OutputStatusMessage(string.Format("Notifications found: {0}", answer));

            return answer;
        }

        /// <summary>
        /// Throw an exception containing any errors returned from the Spotfire JavaScript API
        /// </summary>
        private void GetApiErrorsAndThrow()
        {
            OutputStatusMessage("Checking for API errors");
            IReadOnlyCollection<object> checkErrors = (IReadOnlyCollection<object>)((IJavaScriptExecutor)this).ExecuteScript("return SpotfireTestWrapper.popErrors()");
            if (checkErrors.Count > 0)
            {
                string errors = string.Join(Environment.NewLine, checkErrors.Select(x => x == null ? "" : x.ToString()));
                OutputStatusMessage(string.Format("Errors found: {0}", errors));
                throw new SpotfireAPIException(errors);
            }
            OutputStatusMessage("No API errors found");
        }

        /// <summary>
        /// Execute a script on our wrapper.
        /// Ensures that we select the correct frame and that we leave the current frame as the Spotfire frame
        /// </summary>
        /// <param name="script">The script to execute</param>
        /// <param name="parameters">Aay parameters for the script</param>
        /// <returns>The return value from the script</returns>
        internal object ExecuteAsyncWrapperScript(string script, params object[] parameters)
        {
            object answer = null;

            this.SwitchTo().ParentFrame();
            try
            {
                answer = ((IJavaScriptExecutor)this).ExecuteAsyncScript(script, parameters);
            }
            catch
            {
                GetApiErrorsAndThrow();
                // Some other error, throw it
                throw;
            }
            finally
            {
                this.SwitchTo().Frame(0);
            }

            return answer;
        }

        /// <summary>
        /// Execute a script on our wrapper.
        /// Ensures that we select the correct frame and that we leave the current frame as the Spotfire frame
        /// </summary>
        /// <param name="script">The script to execute</param>
        /// <param name="parameters">Aay parameters for the script</param>
        /// <returns>The return value from the script</returns>
        internal object ExecuteWrapperScript(string script, params object[] parameters)
        {
            object answer;

            this.SwitchTo().ParentFrame();
            try
            {
                answer = ((IJavaScriptExecutor)this).ExecuteScript(script, parameters);
            }
            catch
            {
                GetApiErrorsAndThrow();
                // Some other error, throw it
                throw;
            }
            finally
            {
                this.SwitchTo().Frame(0);
            }

            return answer;
        }

        /// <summary>
        /// Get a list of the data tables
        /// </summary>
        /// <returns></returns>
        public IReadOnlyCollection<string> GetTableNames()
        {
            IReadOnlyCollection<string> tables;
            ReadOnlyCollection<object> answer;

            OutputStatusMessage("Fetching list of table names");

            answer = (ReadOnlyCollection<object>)ExecuteAsyncWrapperScript("SpotfireTestWrapper.tableNames(arguments[arguments.length-1])");

            tables = answer.Select(x => (x == null ? "" : x.ToString())).ToList<String>();

            OutputStatusMessage(string.Format("Found tables: {0}", String.Join(", ", tables)));

            return tables;
        }

        /// <summary>
        /// Get data table properties
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetTableProperties(string tableName)
        {
            Dictionary<string, string> properties;
            Dictionary<string, object> answer;

            OutputStatusMessage(string.Format("Fetching table properties for table: {0}", tableName));

            answer = (Dictionary<string, object>)ExecuteAsyncWrapperScript("SpotfireTestWrapper.tableProperties(arguments[0],arguments[1])", tableName);

            properties = answer
                .ToDictionary(x => x.Key,
                              x => (x.Value == null ? "" : (x.Value is IEnumerable<object> ? String.Join(", ", x.Value as IEnumerable<object>) : x.Value.ToString())));

            OutputStatusMessage(string.Format("Table '{0}' contains properties: {1}", tableName, String.Join(", ", properties)));

            return properties;
        }

        /// <summary>
        /// Get a list of the columns within a table
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public IReadOnlyCollection<string> GetColumnNames(string tableName)
        {
            IReadOnlyCollection<string> columnNames;
            ReadOnlyCollection<object> answer;

            OutputStatusMessage(string.Format("Fetching column names for table: {0}", tableName));

            answer = (ReadOnlyCollection<object>)ExecuteAsyncWrapperScript("SpotfireTestWrapper.tableColumnNames(arguments[0],arguments[1])", tableName);

            columnNames = answer.Select(x => (x == null ? "" : x.ToString())).ToList<String>();

            OutputStatusMessage(string.Format("Table '{0}' contains columns: {1}", tableName, String.Join(", ", columnNames)));

            return columnNames;
        }

        /// <summary>
        /// Get data table column data type
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public string GetColumnDataType(string tableName, string columnName)
        {
            string columnDataType;
            object answer;

            OutputStatusMessage(string.Format("Fetching column type for column: {0}.{1}", tableName, columnName));

            answer = ExecuteAsyncWrapperScript("SpotfireTestWrapper.columnDataType(arguments[0],arguments[1],arguments[2])", tableName, columnName);

            columnDataType = (answer == null ? "" : answer.ToString());

            OutputStatusMessage(string.Format("Column '{0}.{1}' is of type: {2}", tableName, columnName, columnDataType));

            return columnDataType;
        }

        /// <summary>
        /// Get data table column properties
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetColumnProperties(string tableName, string columnName)
        {
            Dictionary<string, string> columnProperties;
            Dictionary<string, object> answer;

            OutputStatusMessage(string.Format("Fetching column properties for column: {0}.{1}", tableName, columnName));

            answer = (Dictionary<string, object>)ExecuteAsyncWrapperScript("SpotfireTestWrapper.columnProperties(arguments[0],arguments[1],arguments[2])", tableName, columnName);

            columnProperties = answer
                .ToDictionary(x => x.Key,
                              x => (x.Value == null ? "" : (x.Value is IEnumerable<object> ? String.Join(", ", x.Value as IEnumerable<object>) : x.Value.ToString())));

            OutputStatusMessage(string.Format("Column '{0}.{1}' contains properties: {2}", tableName, columnName, String.Join(", ", columnProperties)));

            return columnProperties;
        }

        /// <summary>
        /// Get number of distinct values in a column
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <param name="startIndex"></param>
        /// <param name="responseLimit"></param>
        /// <returns></returns>
        public long GetColumnDistinctValueCount(string tableName, string columnName)
        {
            long distinctCount;
            object answer;

            OutputStatusMessage(string.Format("Fetching column distinct value count for column: {0}.{1}", tableName, columnName));

            try
            {
                answer = ExecuteAsyncWrapperScript("SpotfireTestWrapper.columnDistinctValueCount(arguments[0],arguments[1],arguments[2])", tableName, columnName);

                distinctCount = Convert.ToInt64(answer);
            }
            catch (SpotfireAPIException ex)
            {
                if (ex.Message.ToLower(CultureInfo.InvariantCulture).Contains("not supported"))
                {
                    // eat this - it means we can't get the answer
                    distinctCount = -1;
                }
                else
                {
                    throw;
                }
            }

            if (distinctCount > -1)
            {
                OutputStatusMessage(string.Format("Column '{0}.{1}' contains {2} distinct values", tableName, columnName, distinctCount));
            }
            else
            {
                OutputStatusMessage(string.Format("Spotfire doesn't support fetching distinct values for column '{0}.{1}' due to its data type.", tableName, columnName));
            }

            return distinctCount;
        }

        /// <summary>
        /// Spotfire pre 10.0 can return values in the form \uxx or \r \n, whereas post 10.0 it returns the proper values. This function will replace escaped text
        /// </summary>
        /// <param name="escaped"></param>
        /// <returns></returns>
        private string UnescapeUnicode(string escaped)
        {
            return Regex.Replace(escaped, @"\\[Uu]([0-9A-Fa-f]{4})", m => char.ToString((char)ushort.Parse(m.Groups[1].Value, NumberStyles.AllowHexSpecifier)))
                .Replace("\\r", "\r")
                .Replace("\\n", "\n")
                .Replace("\\\"", "\"")
                .Replace("\\t","\t");
        }

        /// <summary>
        /// Get distinct values in a column
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <param name="startIndex"></param>
        /// <param name="responseLimit"></param>
        /// <returns></returns>
        public IReadOnlyCollection<string> GetColumnDistinctValues(string tableName, string columnName, long startIndex, long responseLimit)
        {
            IReadOnlyCollection<string> distinctValues;
            ReadOnlyCollection<object> answer;

            OutputStatusMessage(string.Format("Fetching column distinct values for column: {0}.{1}, startIndex: {2}, responseLimit: {3}", tableName, columnName, startIndex, responseLimit));

            try
            {
                answer = (ReadOnlyCollection<object>)ExecuteAsyncWrapperScript("SpotfireTestWrapper.columnDistinctValues(arguments[0],arguments[1],arguments[2],arguments[3],arguments[4])",
                    tableName, 
                    columnName, 
                    startIndex, 
                    responseLimit);

                // Note - Spotfire pre 10.0 encodes certain characters, so let's decode them
                distinctValues = answer.Select(x => x == null ? "" : UnescapeUnicode(x.ToString())).ToList();
            }
            catch (SpotfireAPIException ex)
            {
                if (ex.Message.ToLower(CultureInfo.InvariantCulture).Contains("not supported"))
                {
                    // eat this - it means we can't get the answer
                    distinctValues = new string[] { };
                }
                else
                {
                    throw;
                }
            }

            OutputStatusMessage(string.Format("Distinct values in column '{0}.{1}': {2}", tableName, columnName, String.Join(", ", distinctValues)));

            return distinctValues;
        }

        /// <summary>
        /// Get the list of pages
        /// </summary>
        /// <returns></returns>
        public IReadOnlyCollection<string> GetPages()
        {
            IReadOnlyCollection<string> pages;
            ReadOnlyCollection<object> answer;

            OutputStatusMessage("Fetching list of pages");

            answer = (ReadOnlyCollection<object>)ExecuteAsyncWrapperScript("SpotfireTestWrapper.pages(arguments[0])");

            pages = answer.Select(x => (x == null ? "" : x.ToString())).ToList<String>();

            OutputStatusMessage(string.Format("Pages: {0}", string.Join(", ", pages)));

            return pages;
        }

        /// <summary>
        /// Set the active page
        /// </summary>
        /// <param name="pageName"></param>
        /// <param name="timeoutInSeconds"></param>
        public void SetActivePage(string pageName, int timeoutInSeconds = 30)
        {
            OutputStatusMessage(string.Format("Setting page to: {0}", pageName));
            ExecuteWrapperScript("SpotfireTestWrapper.application.analysisDocument.setActivePage(arguments[0])", pageName);
            WaitUntilSpotfireReady(timeoutInSeconds);
            var pageState = (Dictionary<string, object>)ExecuteAsyncWrapperScript("SpotfireTestWrapper.application.analysisDocument.getActivePage(arguments[0])");
            if (pageState["pageTitle"].ToString() != pageName)
            {
                string message = string.Format("Page wasn't successfully changed. Current page is: {0}", pageState["pageTitle"]);
                OutputStatusMessage(message);
                throw new PageNotChangedException(message);
            }
        }

        /// <summary>
        /// Get a list of visuals on the page. The list is sorted for elements from top left, across then down to facilate comparisons from one run to another (there is no guaranteed identifier).
        /// </summary>
        /// <returns></returns>
        public List<Visual> GetVisuals()
        {
            List<Visual> answer;

            OutputStatusMessage("Finding visuals on the page");

            IReadOnlyCollection<IWebElement> visualElements = FindElementsByClassName("sf-element-visual");

            answer = visualElements.Select(x => new Visual(this, x)).ToList();

            answer.Sort((a, b) =>
            {
                int ax = a.Element.Location.X;
                int ay = a.Element.Location.Y;
                int bx = b.Element.Location.X;
                int by = b.Element.Location.Y;

                return ay == by ? (ax - bx) : (ay - by);
            });

            OutputStatusMessage(string.Format("Visuals on page: {0}", string.Join(Environment.NewLine, answer)));

            return answer;
        }

        /// <summary>
        /// Restore visuals to normal layout (i.e. nothing maximized)
        /// </summary>
        public void RestoreVisualLayout()
        {
            bool clicked = false;

            OutputStatusMessage("Restoring visual layout to normal.");

            IReadOnlyCollection<IWebElement> restoreButtons = FindElementsByCssSelector(".sfc-maximized-visual-button");

            foreach (IWebElement restoreButton in restoreButtons.TakeWhile((a,b) => !clicked))
            {
                if (restoreButton.GetAttribute("title").ToLower(CultureInfo.InvariantCulture).Contains("restore"))
                {
                    new Actions(this).MoveToElement(restoreButton).Perform();
                    restoreButton.Click();
                    clicked = true;
                }
            }

            if (clicked)
            {
                SetWindowSizeForMatchingSizes(false);

                WaitUntilSpotfireReady();
            }

            OutputStatusMessage("Layout restored");
        }

        /// <summary>
        /// Get a list of marking names
        /// </summary>
        /// <returns></returns>
        public IReadOnlyCollection<string> GetMarkingNames()
        {
            IReadOnlyCollection<string> markingNames;
            ReadOnlyCollection<object> answer;

            OutputStatusMessage("Fetching marking names");

            answer = (ReadOnlyCollection<object>)ExecuteAsyncWrapperScript("SpotfireTestWrapper.markingNames(arguments[0])");

            markingNames = answer.Select(x => (x == null ? "" : x.ToString())).ToList<String>();

            OutputStatusMessage(string.Format("Marking names: {0}", String.Join(", ", markingNames)));

            return markingNames;
        }

        /// <summary>
        /// Clear all the markings
        /// </summary>
        public void ClearAllMarkings()
        {
            OutputStatusMessage("Clearing all markings");
            ExecuteAsyncWrapperScript("SpotfireTestWrapper.clearAllMarkings(arguments[0])");
            WaitUntilSpotfireReady();
            OutputStatusMessage("Markings cleared");
        }

        /// <summary>
        /// Set a marking
        /// </summary>
        /// <param name="markingName"></param>
        /// <param name="tableName"></param>
        /// <param name="whereClause"></param>
        /// <param name="markingOperation"></param>
        public void SetMarking(string markingName, string tableName, string whereClause, string markingOperation)
        {
            OutputStatusMessage(string.Format("Setting marking. Name: {0}, Table: {1}, Where: {2}, Operation: {3}", markingName, tableName, whereClause, markingOperation));
            ExecuteWrapperScript("SpotfireTestWrapper.application.analysisDocument.marking.setMarking(arguments[0], arguments[1], arguments[2], eval(arguments[3]))", 
                markingName, 
                tableName, 
                whereClause, 
                markingOperation);
            WaitUntilSpotfireReady();
            OutputStatusMessage("Marking set");
        }

        /// <summary>
        /// Get currently marked rows
        /// </summary>
        /// <param name="markingName"></param>
        /// <param name="tableName"></param>
        /// <param name="columnNames"></param>
        /// <param name="maxRows"></param>
        /// <returns></returns>
        public TableData GetMarking(string markingName, string tableName, IReadOnlyCollection<string> columnNames, int maxRows)
        {
            TableData table;

            OutputStatusMessage(string.Format("Fetching marking. Name: {0}, Table: {1}, Columns: {2}, maxRows: {3}", markingName, tableName, columnNames, maxRows));

            Dictionary<string, object> answer = (Dictionary<string, object>)ExecuteAsyncWrapperScript(
                "SpotfireTestWrapper.application.analysisDocument.marking.getMarking(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4])", 
                markingName, 
                tableName, 
                columnNames, 
                maxRows);

            Dictionary<string, object> data = new Dictionary<string, object>();

            // Note - Spotfire pre 10.0 encodes certain characters, so let's decode them
            foreach (KeyValuePair<string, object> column in answer)
            {
                IReadOnlyCollection<object> columnValues = (IReadOnlyCollection<object>)column.Value;
                List<string> values = columnValues.Select(x => x == null ? "" : UnescapeUnicode(x.ToString())).ToList();
                data.Add(column.Key, values);
            }

            table = new TableDataFromColumns(data);

            OutputStatusMessage(string.Format("Downloaded marking. {0} columns.", table.Columns.Length));

            return table;
        }

        public void SetDocumentProperty(string propertyName, string value)
        {
            OutputStatusMessage("Setting document property {0} to {1}", propertyName, value);
            ExecuteWrapperScript("SpotfireTestWrapper.application.analysisDocument.setDocumentProperty(arguments[0], arguments[1])", propertyName, value);
            WaitUntilSpotfireReady();
        }

        /// <summary>
        /// Output a status message to the console.
        /// </summary>
        /// <param name="message"></param>
        [SuppressMessage(
         "We allow use of the console because the calling application could be a console application - we just don't know",
         "S2228: Remove logging statement"
        )]
        public virtual void OutputStatusMessage(string message)
        {
            if (OutputToConsole)
            {
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// Output a status message with parameters
        /// </summary>
        /// <param name="message"></param>
        /// <param name="parameters"></param>
        public void OutputStatusMessage(string message, params object[] parameters)
        {
            OutputStatusMessage(string.Format(message, parameters));
        }
    }
}

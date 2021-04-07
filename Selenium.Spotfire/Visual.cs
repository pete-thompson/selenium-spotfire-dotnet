using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;

namespace Selenium.Spotfire
{
    /// <summary>
    /// A class to encapsulate information about a visual on the page
    /// </summary>
    public class Visual
    {
        /// <summary>
        /// The Element that contains the entire visualisation
        /// </summary>
        public IWebElement Element
        {
            get
            {
                bool cacheValid = false;
                if (CachedElement != null)
                {
                    try
                    {
                        // We access the Enabled property to force Selenium to check if the element is still valid
#pragma warning disable S1481 // Unused local variables should be removed
                        bool ignore = CachedElement.Enabled;
#pragma warning restore S1481 // Unused local variables should be removed
                        cacheValid = true;
                    }
                    catch (StaleElementReferenceException)
                    {
                        // Very likely someone has maximized something
                    }
                }

                if (!cacheValid)
                {
                    CachedElement = Driver.FindElementById(ElementId);
                }
                return CachedElement;
            }
        }
        /// <summary>
        /// The Element that contains the content (e.g. chart, text area etc. - excludes the header)
        /// </summary>
        public IWebElement Content
        {
            get
            {
                bool cacheValid = false;
                if (CachedContent != null)
                {
                    try
                    {
                        // We access the Enabled property to force Selenium to check if the element is still valid
#pragma warning disable S1481 // Unused local variables should be removed
                        bool ignore = CachedContent.Enabled;
#pragma warning restore S1481 // Unused local variables should be removed
                        cacheValid = true;
                    }
                    catch (StaleElementReferenceException)
                    {
                        // Very likely someone has maximized something
                    }
                }

                if (!cacheValid)
                {
                    CachedContent = Driver.FindElementById(ContentId);
                }
                return CachedContent;
            }
        }
        /// <summary>
        /// The title for the visual (can be blank if there is no title)
        /// </summary>
        public string Title { get; private set; } = "";
        /// <summary>
        /// The type of visual - e.g. 'text area', 'line chart' etc.
        /// </summary>
        public string Type { get; private set; } = "";
        /// <summary>
        /// Whether the content within the visual can be treated as text (e.g. a text area)
        /// </summary>
        public bool IsTextType { get; private set; }
        /// <summary>
        /// The text within the visual
        /// </summary>
        public string Text
        {
            get
            {
                object textObject = ((IJavaScriptExecutor)Driver).ExecuteScript("return $('#' + arguments[0])[0].outerText", Content.GetAttribute("id"));
                return textObject == null ? "" : textObject.ToString();
            }
        }
        /// <summary>
        /// Whether the content should be treated as an image (e.g. bar charts, line charts)
        /// </summary>
        public bool IsImageType { get; private set; }

        /// <summary>
        /// Whether the content can be treated as a table
        /// </summary>
        public bool IsTabularType { get; private set; }
        /// <summary>
        /// The driver associated with the visual
        /// </summary>
        private readonly SpotfireDriver Driver;
        /// <summary>
        /// We cache the Element and Content because maximize/restore causes the element to be deleted and recreated
        /// </summary>
        private readonly string ElementId;
        private readonly string ContentId;
        private IWebElement CachedElement;
        private IWebElement CachedContent;

        internal Visual(SpotfireDriver driver, IWebElement element)
        {
            CachedElement = element;
            ElementId = element.GetAttribute("id");
            Driver = driver;

            try
            {
                IWebElement titleElement = Element.FindElement(By.CssSelector(".sf-element-visual-title"));

                Title = titleElement.Text.Trim();
            }
            catch (NoSuchElementException)
            {
                // no title
            }

            try
            {
                CachedContent = Element.FindElement(By.CssSelector(".sf-element-visual-content"));
            }
            catch (NoSuchElementException)
            {
                CachedContent = Element;
            }
            ContentId = CachedContent.GetAttribute("id");

            FigureOutType();
        }

        private void FigureOutType()
        {
            // Figure out the type
            string[] classes = Element.GetAttribute("class").Split(' ');

            foreach (string className in classes)
            {
                if (className.StartsWith("sfc-") && className != "sfc-trellis-visualization")
                {
                    Type = className.Replace("sfc-", "").Replace("-", " ");
                }
            }

            if (Type.Length == 0)
            {
                // Probably JSViz since it doesn't add a class
                try
                {
                    Element.FindElement(By.CssSelector("iframe"));
                    Type = "jsviz";
                }
                catch
                {
                    Type = "unknown";
                }
            }

            // Grab the relevant content
            IsTabularType = (Type == "cross table") || (Type == "graphical table") || (Type == "summary table") || (Type == "table");
            IsTextType = Type == "text area";
            IsImageType = !IsTabularType && !IsTextType;
        }

        private bool IsElementInDom()
        {
            bool answer = false;

            try
            {
                Driver.FindElement(By.Id(ElementId));
                answer = true;
            }
            catch (NoSuchElementException)
            {
                // Ignore
            }

            return answer;
        }

        /// <summary>
        /// Maximize this visual
        /// </summary>
        public void Maximize()
        {
            bool changed = false;
            Driver.OutputStatusMessage("Searching for maximize button");
            try
            {
                IWebElement button = Element.FindElement(By.ClassName("sfc-maximize-visual-button"));

                if (button.GetAttribute("title").ToLower(CultureInfo.InvariantCulture).Contains("maximize"))
                {
                    Driver.OutputStatusMessage("Clicking maximize button");
                    new Actions(Driver).MoveToElement(button).Perform();
                    button.Click();
                    Driver.SetWindowSizeForMatchingSizes(true);
                    Driver.WaitUntilSpotfireReady();
                    changed = true;
                }
            }
            catch (NoSuchElementException)
            {
                try
                {
                    // Find a maximize on a different visual and then move forward until this one is maximized
                    IWebElement button = Driver.FindElement(By.ClassName("sfc-maximize-visual-button"));
                    if (button.GetAttribute("title").ToLower(CultureInfo.InvariantCulture).Contains("maximize"))
                    {
                        Driver.OutputStatusMessage("Clicking maximize/restore button");
                        new Actions(Driver).MoveToElement(button).Perform();
                        button.Click();
                        Driver.SetWindowSizeForMatchingSizes(true);
                        Driver.WaitUntilSpotfireReady();
                    }

                    // Keep moving to next one until the element reappears in the DOM
                    while (!IsElementInDom())
                    {
                        string cssClassForNext = "sfc-maximized-visual-button";
                        if (Driver.IsSpotfire1010OrAbove()) 
                        {
                            cssClassForNext = "sf-element-maximized-visual-button";
                        }
                        IWebElement nextButton = Driver.FindElement("The next visual button", By.CssSelector("." + cssClassForNext + "[title='" + Driver.GetLocalization()["Next"] + "']"));
                        new Actions(Driver).MoveToElement(nextButton).Perform();
                        nextButton.Click();
                        Driver.WaitUntilSpotfireReady();
                    }

                    changed = true;
                }
                catch (NoSuchElementException)
                {
                    throw new VisualCannotBeMaximizedException("Visual cannot be maximized");
                }
            }

            if (changed)
            {
                // Make the toolbar disappear
                ((IJavaScriptExecutor)Driver).ExecuteScript("$('.sfc-maximized-visual-control').css('opacity',0).css('transition','opacity 0s linear')");
                IWebElement maximizedVisualControl = Driver.FindElement("Maximized visual toolbar", By.CssSelector(".sfc-maximized-visual-control"), timeoutInSeconds: 0);
                WebDriverWait wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(5));
                wait.Until<IWebElement>((d) =>
                {
                    if (maximizedVisualControl.GetCssValue("opacity") == "0")
                    {
                        return maximizedVisualControl;
                    }
                  return null;
                });
            }
        }

        /// <summary>
        /// Restore the layout to normal
        /// </summary>
        public void Restore()
        {
            Driver.RestoreVisualLayout();
        }

        /// <summary>
        /// Check if we can maximize the visual - the button doesn't exist if there's no title bar
        /// </summary>
        /// <returns></returns>
        public bool CanMaximize()
        {
            IReadOnlyCollection<IWebElement> buttons = Driver.FindElements(By.ClassName("sfc-maximize-visual-button"));
            IReadOnlyCollection<IWebElement> buttons2 = Driver.FindElements(By.ClassName("sfc-maximized-visual-button"));
            return buttons.Count > 0 || buttons2.Count > 0 ;
        }

        /// <summary>
        /// Is the visual maximized?
        /// </summary>
        /// <returns></returns>
        public bool IsMaximized()
        {
            bool answer = false;
            try
            {
                IWebElement button = Element.FindElement(By.ClassName("sfc-maximize-visual-button"));

                answer = button.GetAttribute("title").ToLower(CultureInfo.InvariantCulture).Contains("restore");
            }
            catch (NoSuchElementException)
            {
                // ignore - not maximized
            }
            return answer;
        }

        /// <summary>
        /// Is the visual minimized?
        /// </summary>
        /// <returns></returns>
        public bool IsMinimized()
        {
            return !IsElementInDom();
        }

        /// <summary>
        /// Fetch tabular data for the visual
        /// </summary>
        public TableData GetTableData(int timeoutSeconds = 500)
        {
            Driver.OutputStatusMessage(string.Format("Fetching the tabular data for: {0}", Title));

            // Create a temporary folder to receive the file
            string tempDirectory = Path.Combine(Path.GetTempPath(), "SpotfireDriverData", Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);

            try
            {

                // Right click and choose to export
                Actions actions = new Actions(Driver);
                actions.ContextClick(Content).Perform();

                Driver.FindElement("Export menu", By.CssSelector(".contextMenu .contextMenuItemLabel[title='Export']"), 15).Click();
                IWebElement exportOption = Driver.FindElement("Export table option", By.CssSelector(".contextMenu .contextMenuItemLabel[title='Export table']"), 15);
                IWebElement exportParent = exportOption.FindElement(By.XPath(".."));

                if (exportParent.GetAttribute("class").Contains("contextMenuItemDisabled"))
                {
                    // We can't export the data, so return nothing (and make the menu go away by clicking)
                    Driver.OutputStatusMessage("The table properties disable exporting, so we can't download the data");
                    Directory.Delete(tempDirectory, true);
                    new Actions(Driver).MoveToElement(Content).MoveByOffset(-1, 0).Click().Perform();
                    return new TableDataFromRows(new string[] { }, new string[][] { });
                }

                // Request the download
                Driver.SetDownloadFolder(tempDirectory);
                exportOption.Click();

                // Wait for the CSV file to appear, read it in and delete it
                Driver.OutputStatusMessage("Waiting for file to download");
                var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds));
                using (FileStream file = wait.Until(drv =>
                {
                    FileStream answer = null;
                    string[] csvFiles = Directory.GetFiles(tempDirectory, "*.csv");
                    if (csvFiles.Length > 0)
                    {
                        try
                        {
                            answer = new FileStream(csvFiles[0], FileMode.Open, FileAccess.Read);
                        }
                        catch
                        {
                            // ignore - we'll keep trying until timeout
                        }
                    }
                    return answer;
                }))
                {
                    Driver.OutputStatusMessage("Found a file, creating TableData object to process it.");
                }
                return new TableDataFromTemporaryFile(tempDirectory);
            }
            catch
            {
                // something went wrong, delete the temporary folder since the TableData object won't have the opportunity to do so.
                try
                {
                    // Delete the temporary folder
                    Directory.Delete(tempDirectory, true);
                }
                catch
                {
                    // ignore - temporary files will eventually get cleaned up anyway
                }

                // Now throw the original error
                throw;
            }
        }

        /// <summary>
        /// The bitmap for image visuals
        /// </summary>
        public Bitmap GetImage()
        {
            Bitmap screenshot;

            Driver.OutputStatusMessage(string.Format("Capturing image for visual {0}", Title));

            byte[] byteArray = ((ITakesScreenshot)Driver).GetScreenshot().AsByteArray;
            using (MemoryStream ms = new MemoryStream(byteArray))
            {
                screenshot = new Bitmap(ms);
            }
            Rectangle croppedImage = new Rectangle(Content.Location.X, Content.Location.Y, Content.Size.Width, Content.Size.Height);
            return screenshot.Clone(croppedImage, screenshot.PixelFormat);
        }

        /// <summary>
        /// Resize the Spotfire page so that the visual matches the requested size
        /// </summary>
        /// <param name="size"></param>
        public void ResizeContent(Size size)
        {
            Size current = Driver.Manage().Window.Size;
            Size original = current;
            Size newSize = new Size(current.Width + size.Width - Content.Size.Width, current.Height + size.Height - Content.Size.Height);
            Size lastContentSize = Content.Size;
            // We can't know exactly how much to change in size because Spotfire will size visualisations based on sharing the page
            // So we'll resize based on the assumption that this is the only visual on the page and then keep trying until we get to the desired size
            while (current != newSize)
            {
                Driver.Manage().Window.Size = newSize;
                Driver.WaitUntilSpotfireReady();

                Size currentContentSize = Content.Size;
                double widthRatio = (currentContentSize.Width == lastContentSize.Width) || (newSize.Width == current.Width) ? 1.0 : (Convert.ToDouble(currentContentSize.Width - lastContentSize.Width) / Convert.ToDouble(newSize.Width - current.Width));
                double heightRatio = ((currentContentSize.Height == lastContentSize.Height) || (newSize.Height == current.Height) ? 1.0 : (Convert.ToDouble(currentContentSize.Height - lastContentSize.Height) / Convert.ToDouble(newSize.Height - current.Height)));

                current = Driver.Manage().Window.Size;
                // We should have at least changed - failure to change indicates that the browser won't go any larger
                if (current != newSize)
                {
                     throw new VisualResizeFailed(string.Format("Unable to resize the visual to the desired size of {0}. It is likely that the screen isn't large enough. Browser size is {1}, visual size is {2}", size, Driver.Manage().Window.Size, Content.Size));
                }
                // Next attempt - use the ratio of how much we changed the size vs. how much the content changed to help guess how Spotfire will resize
                newSize = new Size(current.Width + Convert.ToInt32(Math.Truncate((size.Width - currentContentSize.Width) / widthRatio)), current.Height + Convert.ToInt32(Math.Truncate((size.Height - currentContentSize.Height) / heightRatio)));
                lastContentSize = Content.Size;
            }
            if (size != original)
            {
                Driver.OutputStatusMessage("Resized browser to size {0} from {1} so that visual {2} is of size {3}", newSize, original, Title, size);
            }
        }

        /// <summary>
        /// Obtain a string representation of the object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("Visual. Type: {0}. Title: {1}", Type, Title);
        }
    }
}

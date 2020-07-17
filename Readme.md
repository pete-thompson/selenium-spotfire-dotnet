# Spotfire Selenium Helpers #

The goal of this project is to make automating Spotfire through Web Player using Selenium and .NET easier. 
There are several use cases, with different parts of the project focusing on each:

* Automating tasks that use Spotfire Web Player - e.g. opening an analytic and exporting data.
* Running tests against Spotfire analytics - e.g. to automate a test for a specific analytic template.
* Comparing the output of different Spotfire environments - e.g. to automate testing of a new version of Spotfire.

Three NuGet packages are available:

* Spotfire.Selenium - Classes to automate Spotfire through Selenium and Chrome.
* Spotfire.Selenium.TestHelpers - Helpers for all types of tests
* Spotfire.Selenium.MSTest - Support for the creation of MSTest based unit tests.

Note, this project does not contain any Spotfire software, it can only be used to automate an existing 
Spotfire installation.

## Installation

Installed through NuGet.org. 

For example, at the package manager console on Visual Studio, enter following command
to install the automation package:

```
PM> Install-Package Selenium.Spotfire
```

For example, at the package manager console on Visual Studio, enter following command
to install the test helper package:

```
PM> Install-Package Selenium.Spotfire.TestHelpers
```

For example, at the package manager console on Visual Studio, enter following command
to install the MSTest helper package:

```
PM> Install-Package Selenium.Spotfire.MSTest
```

## Usage

The starting point for automating Spotfire, or running an automated test, is to instantiate a 'driver'. The driver
controls an instance of the Chrome browser interacting with Spotfire. Two different drivers are provided:

* Spotfire.Selenium.SpotfireDriver - A general-purpose driver with features for controlling Spotfire.
* Spotfire.Selenium.MSTest.SpotfireTestDriver - A special purpose driver that includes features to help with unit testing.

Both driver classes implement the Disposeable pattern to help ensure that Chrome is closed automatically. 

```c#
// Create a driver, ensuring that it gets cleaned up automatically
using (SpotfireDriver spotfire = SpotfireDriver.GetDriverForSpotfire())
{
    // Open a file from Spotfire
    spotfire.OpenSpotfireAnalysis("https://SpotfireServer", "/path to the file");
    IReadOnlyCollection<string> pages = spotfire.GetPages();

    // Move to the first page
    spotfire.SetPage(pages.ElementAt(0));
}
```

The general pattern for using the driver classes is:

* Create a driver using the GetDriverForSpotfire method.
* Open a Spotfire analysis.
* Interact with the analysis.
* Dispose of the driver.

## Features of the driver

The general purpose driver (Spotfire.Selenium.SpotfireDriver) incorporates most of the features implemented by this project.
The following sections show examples of how to call the driver to achieve certain requirements, they are not intended
to be functional examples (e.g. you would pick one of the methods of opening an analytic, not all 4).

### Obtaining a driver

```c#
// Run Chrome 'Headless' - i.e. no visible window
using (SpotfireDriver spotfire = SpotfireDriver.GetDriverForSpotfire(true))
{
}

// Run Chrome 'Headless' and capture Chrome's logs
using (SpotfireDriver spotfire = SpotfireDriver.GetDriverForSpotfire(true, true))
{
}

// Run Chrome visibly
using (SpotfireDriver spotfire = SpotfireDriver.GetDriverForSpotfire())
{
}
```

### Debug logging

The driver generates debug log messages which can be sent to the console. Sub-classes of the driver can capture these messages
and route them somewhere more appropriate (e.g. the MSTest driver sends them to the Test Context object).

```c#
// Send messages to the console
spotfire.OutputToConsole = true;
```

### Exceptions

The driver includes several custom Exception classes:

* SpotfireAPIException - thrown whenever Spotfire's JavaScript API returns an error. E.g. attempts to open files that don't 
exist will result in this error.
* PageNotChangedException - thrown if an attempt to change page fails (e.g. because the page doesn't exist).
* VisualCannotBeMaximizedException - thrown if an attempt is made to maximize a visual that can't be maximized.

### Opening a Spotfire analytic

Each driver can open a single analytic. If you want to run multiple simultaneously you'll need to instantiate multiple
driver objects (which is perfectly valid - one of our primary use cases involves opening the same analytic on two
servers simultaneously and comparing the results).

```c#
// Open an analytic
spotfire.OpenSpotfireAnalysis("https://SpotfireServer", "/path to the file");

// Open an analytic passing a configuration block
spotfire.OpenSpotfireAnalysis("https://SpotfireServer", "/path to the file", configuationBlock: "some config block");

// Open an analytic, but don't wait for it to open
spotfire.OpenSpotfireAnalysis("https://SpotfireServer", "/path to the file", waitForCompletion: false);

// Open an analytic and wait up to 5 minutes for it to be ready (default is 2 minutes)
spotfire.OpenSpotfireAnalysis("https://SpotfireServer", "/path to the file", timeoutInSeconds: 600);
```

### Waiting for Spotfire

```c#
// Wait for analysis to finish opening (if prior call to OpenSpotfireAnalysis specified to not wait)
spotfire.WaitForAnalysisToOpen();
spotfire.WaitForAnalysisToOpen(timeoutInSeconds: 600);

// Check if Spotfire is 'ready' - i.e. no processing happening, all images downloaded
spotfire.IsSpotfireReady()

// Wait until Spotfire is ready
spotfire.WaitUntilSpotfireReady()

// Wait until Spotfire is ready for 2 minutes (default wait is 30 seconds)
spotfire.WaitUntilSpotfireReady(timeoutInSeconds: 120)
```

### Handling Spotfire 10 (X) differences

```c#
if (spotfire.IsSpotfire10OrAbove()) 
{
	// Do something 10.x specific
}
else 
{
	// Do something 7.x specific
}
if (spotfire.IsSpotfire103OrAbove()) 
{
	// Do something 10.3 specific
}
```

### Finding HTML elements

The FindElement method adds capabilities to Selenium's built-in method - the primary difference is that it 
generates log messages, but it can also wait for elements to become present.

```c#
// Find an element, waiting up to 5 seconds for it to appear
FindElement(string description, By.CssSelector("#anID"));

// Find an element, but don't wait
FindElement(string description, By.CssSelector("#anID"), timeoutInSeconds: 0);

// Find an element, waiting up to 2 minutes
FindElement(string description, By.CssSelector("#anID"), timeoutInSeconds: 120);
```

### Checking for notifications

```c#
string notifications = spotfire.GetNotifications();

if (notifications.Length>0) {
	// Do something with the notifications
}
```

### Handling downloads

Data downloaded from Spotfire can be saved to a folder controlled by the driver. The driver automatically clears messages
about downloads from the Chrome window, ensuring that visuals size the same when capturing screenshots. The driver does not
include any methods for downloading from Spotfire, but download can be achieved by sending click requests to the export
menu options.

```c#
spotfire.SetDownloadFolder("C:\temp");
```

### Getting localization information

Spotfire stores a table of 'localization' information that maps text that appears on screen within Web Player to the local
language. Unfortunately, this table only includes localization text for messages generated within the browser
so isn't a complete set of messages (most on-screen messages are generated within the server and those mappings aren't
available).

```c#
IReadOnlyDictionary<string, string> localization = spotfire.GetLocalization();
string readyTextInLocalLanguage = localization["Ready"];
```

### Reading data

These methods interact with the Spotfire JavaScript API. The JavaScript API was written with the intention of supporting
operations like filtering and marking, so the data retrieval API can be used for fetching distinct values in a column
rather than fetching entire rows of data. The advantage of this API is that it will return a smaller quantity of data vs.
that which is returned when reading entire tables (due to the fact that data is likely repeated across rows). If full row
level data is required then explore using a table visualisation on a page, or using the Spotfire export menus.

```c#
IReadOnlyCollection<string> tables = spotfire.GetTableNames();

foreach (string tableName in tables)
{
    Dictionary<string, string> properties = spotfire.GetTableProperties(tableName);

    IReadOnlyCollection<string> columns = spotfire.GetColumnNames(tableName);

    foreach (string columnName in columns)
    {
        spotfire.GetColumnDataType(tableName, columnName);

        Dictionary<string, string> columnProperties = spotfire.GetColumnProperties(tableName, columnName);

        int valueCount = spotfire.GetColumnDistinctValueCount(tableName, columnName);

        for (long startIndex = 0; startIndex < valueCount; startIndex += 1000)
        {
            IReadOnlyCollection<string> distinctValues = spotfire.GetColumnDistinctValues(tableName, columnName, startIndex, 1000);
        }
    }
}
```

### Markings

The Spotfire JavaScript API allows fetching of marking contents in full row/column tabular form, but it will only 
allow for the collection of a fixed number of rows (it isn't possible to page through the data fetching all rows). Regardless,
these methods are useful for setting markings and checking the impact.

```c#
IReadOnlyCollection<string> markingNames = spotfire.GetMarkingNames();
IReadOnlyCollection<string> tableNames = spotfire.GetTableNames();

spotfire.ClearAllMarkings();

foreach (string markingName in markingNames)
{
    foreach (string tableName in tableNames)
    {
        IReadOnlyCollection<string> columnNames = spotfire.GetColumnNames(tableName);
        using (TableData table = spotfire.GetMarking(markingName, tableName, columnNames, 100))
        {
            // Do something with the data
        }

        // Toggle the marking
        spotfire.SetMarking(markingName, tableName, "1=1", MarkingOperation.Toggle);

        using (TableData table = spotfire.GetMarking(markingName, tableName, columnNames, 100))
        {
            // Do something with the data
        }
    }
}
```

### Pages

```c#
IReadOnlyCollection<string> pages = spotfire.GetPages();
foreach (string page in pages)
{
    spotfire.SetActivePage(page);
}
```

### Visuals

Visual types are captured from the 'CSS class' associated with the visual element within Spotfire's HTML, thus we see values
like 'text area' or 'map chart'. The driver classifies these visual types into 3 different types of content - text content 
(text areas), table content (tables, cross tables etc.) and images (maps, bar charts, lines, pies etc.). The content for
each type can be retrieved to facilitate comparison in test cases.

```c#
// Restore all visuals to normal size
spotfire.RestoreVisualLayout();

List<Visual> visuals = spotfire.GetVisuals();

foreach (Visual visual in visuals)
{
    string title = visual.Title;
    string type = visual.Type;
    bool isMaximized = visual.IsMaximized();
    bool isMinimized = visual.IsMinimized();

    if (visual.IsTextType)
    {
        // A 'text' visual - fetch the text
        string theText = visual.Text;
    }
    else if (visual.IsImageType)
    {
        // An 'image' visual - fetch the bitmap (maximizing if we can)
        if (visual.CanMaximize()) visual.Maximize();
        Bitmap image = visual.GetImage();
        if (visual.CanMaximize()) visual.Restore();
        // Capture an image at a specific size
        visual.ResizeContent(new Size(400,200));
        Bitmap image2 = visual.GetImage();
    }
    else if (visual.IsTabularType)
    {
        // A 'table' visual - fetch the data
        using (TableData data = visual.GetTableData())
        {
        }
    }
}
```

### Tabular data

The Selenium.Spotfire.TableData class provides methods for reading data returned from Spotfire (either from a table visual 
or from marking data). The class intentionally processes data a row at a time so as to avoid problems processing large
tables of data - it is up to the caller to place limits on the amount of data that will be processed (bear in mind that
Spotfire is particularly good at processing large datasets, so it's possible to download data containing millions of rows). 
The example code below can be used to extract the table information and write it out to the Test Context.

```c#
private void WriteOutTable(TableData table)
{
    // Header
    StringBuilder line = new StringBuilder();
    for (int i = 0; i < table.Columns.Length; i++)
    {
        string s = table.Columns[i];
        line.Append(String.Format("{0,-20} | ", s));
    }
    TestContext.WriteLine(line.ToString());
    line.Clear();
    for (int i = 0; i < table.Columns.Length; i++)
    {
        line.Append("---------------------|-");
    }
    TestContext.WriteLine(line.ToString());
    line.Clear();

    // Data
    while (!table.EndOfData)
    {
        foreach (string val in table.ReadARow())
        {
            string s = val;
            if (s.Length > 20) s = s.Substring(0, 17) + "...";
            line.Append(String.Format("{0,-20} | ", s));
        }
        TestContext.WriteLine(line.ToString());
        line.Clear();
    }
    for (int i = 0; i < table.Columns.Length; i++)
    {
        line.Append("---------------------|-");
    }
    TestContext.WriteLine(line.ToString());
    line.Clear();
}
```

## Additional features of the MSTest driver

### Obtaining a driver

The GetDriverForSpotfire method will set whether Chrome is used 'headless' or not (i.e. without a visible window) based
on the presence of the "ChromeHeadless" property in the test context. This allows configuration of the headless property
in the .runSettings file.

Similarly, the presence of a value for "IncludeChromeLogs" in the test context will enable the capture of logs from Chrome.

```c#
// Open Spotfire
using (SpotfireTestDriver spotfire = SpotfireTestDriver.GetDriverForSpotfire(testContext))
{
}
```


Run settings to show the Chrome window:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <TestRunParameters>
  	<Parameter name="ChromeHeadless" value="" />
  	<Parameter name="IncludeChromeLogs" value="" />
...
  <TestRunParameters> 
...
<RunSettings>
```

Run settings for headless:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <TestRunParameters>
  	<Parameter name="ChromeHeadless" value="headless" />
  	<Parameter name="IncludeChromeLogs" value="" />
...
  <TestRunParameters>
...
<RunSettings>
```

Run settings for capturing Chrome logs:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <TestRunParameters>
  	<Parameter name="ChromeHeadless" value="" />
  	<Parameter name="IncludeChromeLogs" value="logs" />
...
  <TestRunParameters>
...
<RunSettings>
```

### Capturing screenshots

The MSTest driver can be used to capture screenshots and attach them to the test results. Files are automatically 
named using the test name and incrementing counters. If multiple Spotfire drivers are instantiated within a single test
case the driver will automatically number the files to keep them in sequence (a driver counter is included in the 
filename). A final screenshot is automatically captured when the driver object is disposed, thus ensuring that screenshot
is captured even if exceptions are thrown.

```c#
spotfire.CaptureScreenshot("First step");
```

### Logging to the Test Context

All debug messages from the driver are automatically logged to the Test Context using WriteLine and will appear
in the test results.

## Test helpers

The test helpers are intended to simplify the authoring of tests.

### Comparing images

The comparison tools ease the task of comparing the content of Spotfire visuals. The image comparison ignores situations
where images have different sized borders, or tiny differences in colour - thus allowing comparison of images from Spotfire 10.x and 7.x.

```c#
bool imagesEqual = true;

// We maximize the visuals where possible to eliminate differences due to layout differences
if (oldVisual.CanMaximize()) oldVisual.Maximize();
Bitmap oldbitmap = oldVisual.GetImage();
if (oldVisual.CanMaximize()) oldVisual.Restore();
if (newVisual.CanMaximize()) newVisual.Maximize();
Bitmap newbitmap = newVisual.GetImage();
if (newVisual.CanMaximize()) newVisual.Restore();

imagesEqual = CompareUtilities.AreEqual(oldbitmap, newbitmap);
```

The comparison tools can also modify bitmaps to highlight the differences between the images.

```c#
CompareUtilities.GenerateImageDifference(expectedImage, actual);
// Both bitmaps will be updated with Red highlighting where differences are found
// Here we're going to save an image and attach to the MSTest context
path = TestContext.TestDir + "\\" + TestContext.FullyQualifiedTestClassName + "Image differences.png";
actual.Save(path);
this.TestContext.AddResultFile(path);
```

#### Comparing tables

The comparison tools also allow for checking data in table visuals.
Note use of the disposable pattern to ensure that temporary data associated with the tables is cleaned up.

```c#
bool tablesEqual = true;
using (TableData oldData = oldVisual.GetTableData())
using (TableData newData = newVisual.GetTableData())
{
    tablesEqual = CompareUtilities.AreEqual(oldData, newData);
}
```

### Multiple asserts

A common pattern in unit testing is to check a single assertion in each test. But, when testing Spotfire it might be
preferable to write a single unit test that includes many checks and to only fail the checks after they have all been
performed. The MultipleAsserts object can collect multiple assertions before eventually failing them at the end of the test.

```c#
MultipleAsserts errors = new MultipleAsserts();

IReadOnlyCollection<string> pages = spotfire.GetPages();
spotfire.SetActivePage(pages.ElementAt(1));

List<Visual> visuals = spotfire.GetVisuals();

visuals.ElementAt(1).Maximize();
errors.CheckErrors(() => Assert.IsTrue(visuals.ElementAt(1).IsMaximized(), "Visual should be maximized"));
errors.CheckErrors(() => Assert.IsFalse(visuals.ElementAt(1).IsMinimized(), "Visual should be not minimized"));
errors.CheckErrors(() => Assert.IsFalse(visuals.ElementAt(3).IsMaximized(), "Visual should be not maximized"));
errors.CheckErrors(() => Assert.IsTrue(visuals.ElementAt(3).IsMinimized(), "Visual should be minimized"));
visuals.ElementAt(1).Maximize();
errors.CheckErrors(() => Assert.IsTrue(visuals.ElementAt(1).IsMaximized(), "Visual should be maximized"));
errors.CheckErrors(() => Assert.IsFalse(visuals.ElementAt(1).IsMinimized(), "Visual should be not minimized"));
errors.CheckErrors(() => Assert.IsFalse(visuals.ElementAt(3).IsMaximized(), "Visual should be not maximized"));
errors.CheckErrors(() => Assert.IsTrue(visuals.ElementAt(3).IsMinimized(), "Visual should be minimized"));
visuals.ElementAt(3).Maximize();
errors.CheckErrors(() => Assert.IsFalse(visuals.ElementAt(1).IsMaximized(), "Visual should be not maximized"));
errors.CheckErrors(() => Assert.IsTrue(visuals.ElementAt(1).IsMinimized(), "Visual should be minimized"));
errors.CheckErrors(() => Assert.IsTrue(visuals.ElementAt(3).IsMaximized(), "Visual should be maximized"));
errors.CheckErrors(() => Assert.IsFalse(visuals.ElementAt(3).IsMinimized(), "Visual should be not minimized"));
visuals.ElementAt(1).Maximize();
errors.CheckErrors(() => Assert.IsTrue(visuals.ElementAt(1).IsMaximized(), "Visual should be maximized"));
errors.CheckErrors(() => Assert.IsFalse(visuals.ElementAt(1).IsMinimized(), "Visual should be not minimized"));
errors.CheckErrors(() => Assert.IsFalse(visuals.ElementAt(3).IsMaximized(), "Visual should be not maximized"));
errors.CheckErrors(() => Assert.IsTrue(visuals.ElementAt(3).IsMinimized(), "Visual should be minimized"));
spotfire.RestoreVisualLayout();

errors.AssertEmpty();
```

## Other things you can do

You can of course use any of the standard features of the Selenium Chrome Driver. E.g. you can enter values into controls 
in text areas, click on buttons etc. - but you'll need to be able to locate the relevant elements within the Spotfire
page. You can make your life simpler by wrapping any such elements in Text Areas with DIV controls and assigning specific 
IDs to those elements - thus allowing easier selection with the FindElement method.

## How it works (or doesn't!)

The basic approach used is to wrap the connection to Spotfire within a page that includes the Spotfire JavaScript API 
and control Spotfire through the API wherever possible. Unfortunately, the API only allows control over a limited
subset of Spotfire's capabilities, so the driver uses 'smelly' approaches where no API exists. For example, interactions
with visuals on a page (downloading data, maximizing, capturing images etc.) all rely on the way that Spotfire renders
visuals using HTML - there's every risk that Spotfire might change these in a future version and break the driver.

Ideas have been submitted on the Tibco Spotfire Ideas Portal to extend the API to 
support replacement of the smelly implementations - please feel free to vote for them.

The following table covers whether the Driver methods are considered smelly or not. All methods on visuals objects 
are considered to be smelly (https://ideas.tibco.com/ideas/TS-I-7007).

Driver Method | API or 'Smelly'? | Link to Idea
---|---|---
SetDownloadFolder | N/A
OpenSpotfireAnalysis | API
WaitForAnalysisToOpen | API
IsSpotfireReady | Smelly | https://ideas.tibco.com/ideas/TS-I-7002
WaitUntilSpotfireReady | Smelly | https://ideas.tibco.com/ideas/TS-I-7002
IsSpotfire10OrAbove | Smelly | https://ideas.tibco.com/ideas/TS-I-7003
IsSpotfire103OrAbove | Smelly | https://ideas.tibco.com/ideas/TS-I-7003
FindElement | N/A
SetWindowSizeForMatchingSizes | Smelly | No realistic idea can be submitted here - window sizing is unlikely to be something that Tibco would choose to tie themselves to.
GetLocalization | Smelly | This is always likely to be smelly given that Tibco will not support the internal structure of the localization table.
GetNotifications | Smelly | https://ideas.tibco.com/ideas/TS-I-7006
GetTableNames | API
GetTableProperties | API
GetColumnNames | API
GetColumnDataType | API
GetColumnProperties | API
GetColumnDistinctValueCount | API [^1] | https://ideas.tibco.com/ideas/TS-I-7004
GetColumnDistinctValues | API [^1] | https://ideas.tibco.com/ideas/TS-I-7004
GetPages | API
SetActivePage | API
GetVisuals | Smelly | https://ideas.tibco.com/ideas/TS-I-5491
RestoreVisualLayout | Smelly |  https://ideas.tibco.com/ideas/TS-I-7005
GetMarkingNames | API
ClearAllMarkings | API
SetMarking | API
GetMarking | API

[^1]: Spotfire will not return data for certain types of columns but doesn't provide an API to check if 
the column type is supported, thus there is a slight 'smelliness' to the implementation


## Contributing

Any contributions are welcome. Please submit issues, pull requests etc. on the GitHub project: https://github.com/pete-thompson/selenium-spotfire-dotnet

## License

The project is licensed under the "MIT" license. See license.txt for details.

Copyright 2020 IQVIA
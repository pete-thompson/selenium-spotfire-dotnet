using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using Selenium.Spotfire.MSTest;
using Selenium.Spotfire.TestHelpers;
using System.IO;
using System.Reflection;
using OpenQA.Selenium;
using System.Threading;

namespace Selenium.Spotfire.Tests
{
    /// <summary>
    /// The tests in this class rely on a specific file being present on the Spotfire server. The Spotfire file contains examples of each visual along with a predictable set of data
    /// </summary>
    [TestClass]
    public class FullDriverTest
    {
        public TestContext TestContext { get; set; }

        private SpotfireTestDriver[] Spotfires;

        private string TestFile
        {
            get
            {
                return TestContext.Properties["SpotfireTestDriverTestFile"].ToString();
            }
        }

        [TestInitialize]
        public void Startup()
        {
            if (Spotfires != null)
            {
                foreach (SpotfireDriver spotfire in Spotfires)
                {
                    spotfire.Dispose();
                }
            }

            if ((TestContext.Properties["DownloadChromeDriver"] ?? "").ToString().Length > 0)
            {
                SpotfireDriver.GetChromeDriver();
            }

            int configuredCount = SpotfireTestDriver.ContextConfigurationCount(TestContext);
            Spotfires = new SpotfireTestDriver[configuredCount];
            for(int counter = 0; counter<configuredCount; counter++)
            {
                Spotfires[counter] = SpotfireTestDriver.GetDriverForSpotfire(TestContext);
                Spotfires[counter].OutputToConsole = true;
                Spotfires[counter].ConfigureFromContext(counter+1);

                Spotfires[counter].OpenSpotfireAnalysis(TestFile);

                counter++;
            }
        }

        [TestCleanup]
        public void Stop()
        {
            if (Spotfires != null)
            {
                foreach (SpotfireDriver spotfire in Spotfires)
                {
                    if (spotfire != null) spotfire.Dispose();
                }
            }
        }

        [TestMethod]
        public void DataTests()
        {
            MultipleAsserts checks = new MultipleAsserts();

            var expectedTableProperties = new Dictionary<string, Dictionary<string, string>>()
            {
                {
                    "Data Table",
                    new Dictionary<string, string>()
                    {
                        { "Transformation", "" },
                        { "Keywords", "" },
                        { "Description", "" },
                        { "ExternalId", "" },
                        { "MapChart.GeometryType", "" },
                        { "MapChart.IsGeocodingTable", "False" },
                        { "MapChart.GeocodingColumns", "" },
                        { "MapChart.GeocodingHierarchyName", "" },
                        { "MapChart.GeocodingHierarchyPriority", "0" },
                        { "MapChart.GeocodingAutoload", "False" },
                        { "MapChart.GeocodingHierarchyVersion", "" },
                        { "MapChart.GeographicCrs", "" },
                        { "MapChart.IsGeocodingEnabled", "" }
                    }
                },
                {
                    "World Countries",
                    new Dictionary<string, string>()
                    {
                        { "Transformation", "" },
                        { "Keywords", "Geocoding" },
                        { "Description", "" },
                        { "ExternalId", "?" },
                        { "MapChart.GeometryType", "Polygon" },
                        { "MapChart.IsGeocodingTable", "True" },
                        { "MapChart.GeocodingColumns", "?" },
                        { "MapChart.GeocodingHierarchyName", "World" },
                        { "MapChart.GeocodingHierarchyPriority", "?" },
                        { "MapChart.GeocodingAutoload", "True" },
                        { "MapChart.GeocodingHierarchyVersion", "?" },
                        { "MapChart.GeographicCrs", "EPSG:4326" },
                        { "MapChart.IsGeocodingEnabled", "True" }
                    }
                }
            };

            var expectedColumns = new Dictionary<string, Dictionary<string, Tuple<string,int>>>()
            {
                {
                    "Data Table",
                    new Dictionary<string, Tuple<string,int>>()
                    {
                        { "Country", Tuple.Create("String", 217 ) },
                        { "1960 (thousand)" , Tuple.Create("Real", -1 ) },
                        { "2017 (thousand)" , Tuple.Create( "Real", -1) }
                    }
                },
                {
                    "World Countries",
                    new Dictionary<string, Tuple<string,int>>()
                    {
                        { "Geometry", Tuple.Create("Binary", -1) },
                        { "Continent", Tuple.Create("String", 6) },
                        { "Region", Tuple.Create("String", 6) },
                        { "Subregion", Tuple.Create("String", 23) },
                        { "Country", Tuple.Create("String", 233) },
                        { "Country ISO2", Tuple.Create("String", 231) },
                        { "Country ISO3", Tuple.Create("String", 231) },
                        { "País", Tuple.Create("String", 233) },
                        { "Pays", Tuple.Create("String", 232) },
                        { "Country_DE", Tuple.Create("String", 233) },
                        { "Country_PT", Tuple.Create("String", 232) },
                        { "Capital", Tuple.Create("String", 226) },
                        { "XCenter", Tuple.Create("Real", -1) },
                        { "YCenter", Tuple.Create("Real", -1) },
                        { "XMin", Tuple.Create("Real", -1) },
                        { "XMax", Tuple.Create("Real", -1) },
                        { "YMin", Tuple.Create("Real", -1) },
                        { "YMax", Tuple.Create("Real", -1) },
                        { "XCenter (2)", Tuple.Create("Real", -1) },
                        { "YCenter (2)", Tuple.Create("Real", -1) },
                        { "Geography Hierarchy", Tuple.Create("String", -1) }
                    }
                }
            };

            foreach (SpotfireDriver spotfire in Spotfires)
            {
                IReadOnlyCollection<string> tables = spotfire.GetTableNames();
                checks.CheckErrors(() => Assert.AreEqual(expectedTableProperties.Count, tables.Count, "Unexpected number of tables"));

                foreach (string tableName in expectedTableProperties.Keys)
                {
                    checks.CheckErrors(() => Assert.IsTrue(tables.Contains(tableName), string.Format("Expected table {0} is missing", tableName)));

                    Dictionary<string, string> properties = spotfire.GetTableProperties(tableName);
                    checks.CheckErrors(() => Assert.AreEqual(expectedTableProperties[tableName].Count, properties.Count, string.Format("Mismatch in properties for table {0}", tableName)));

                    foreach (string propertyName in expectedTableProperties[tableName].Keys)
                    {
                        checks.CheckErrors(() => Assert.IsTrue(properties.ContainsKey(propertyName), string.Format("Property {0} is missing for table {1}", propertyName, tableName)));
                        if (properties.ContainsKey(propertyName) && (expectedTableProperties[tableName][propertyName] != "?"))
                        {
                            checks.CheckErrors(() => Assert.AreEqual(expectedTableProperties[tableName][propertyName], properties[propertyName], string.Format("Property {0} for table {1} has wrong value", propertyName, tableName)));
                        }
                    }

                    IReadOnlyCollection<string> columns = spotfire.GetColumnNames(tableName);
                    checks.CheckErrors(() => Assert.AreEqual(expectedColumns[tableName].Count, columns.Count, string.Format("Mismatched columns for table {0}", tableName)));

                    foreach (string columnName in expectedColumns[tableName].Keys)
                    {
                        checks.CheckErrors(() => Assert.IsTrue(columns.Contains(columnName), string.Format("Column {0}.{1} is missing", tableName, columnName)));
                        if (columns.Contains(columnName))
                        {
                            string columnType = spotfire.GetColumnDataType(tableName, columnName);
                            checks.CheckErrors(() => Assert.AreEqual(expectedColumns[tableName][columnName].Item1, columnType, string.Format("Column {0}.{1} isn't of expected type", tableName, columnName)));

                            Dictionary<string, string> columnProperties = spotfire.GetColumnProperties(tableName, columnName);
                            checks.CheckErrors(() => Assert.AreEqual(columnName, columnProperties["Name"], "We expect the column property 'Name' to match the column name."));

                            long count = spotfire.GetColumnDistinctValueCount(tableName, columnName);
                            checks.CheckErrors(() => Assert.AreEqual(expectedColumns[tableName][columnName].Item2, count, string.Format("Column {0}.{1} doesn't have expected number of unique values", tableName, columnName)));

                            IReadOnlyCollection<string> distinctValues = spotfire.GetColumnDistinctValues(tableName, columnName, 0, 100);
                            checks.CheckErrors(() => Assert.AreEqual(Math.Max(Math.Min(expectedColumns[tableName][columnName].Item2, 100), 0), distinctValues.Count, string.Format("Column {0}.{1} doesn't have expected number of unique values", tableName, columnName)));
                        }
                    }
                }
            }
            checks.AssertEmpty();
        }

        [TestMethod]
        public void InvalidTableName()
        {
            MultipleAsserts checks = new MultipleAsserts();

            foreach (SpotfireDriver spotfire in Spotfires)
            {

                Dictionary<string, string> properties = spotfire.GetTableProperties("garbage table");
                checks.CheckErrors(() => Assert.AreEqual(0, properties.Count, "We expect no properties on invalid table"));

                IReadOnlyCollection<string> columns = spotfire.GetColumnNames("garbage table");
                checks.CheckErrors(() => Assert.AreEqual(0, columns.Count, "We expect no columns on invalid table"));

                IReadOnlyCollection<string> tables = spotfire.GetTableNames();

                properties = spotfire.GetColumnProperties("garbage table", "garbage column");
                checks.CheckErrors(() => Assert.AreEqual(0, properties.Count, "We expect no properties on invalid column"));

                properties = spotfire.GetColumnProperties(tables.First(), "garbage column");
                checks.CheckErrors(() => Assert.AreEqual(0, properties.Count, "We expect no properties on invalid column in real table"));

                string columnType = spotfire.GetColumnDataType("garbage table", "garbage column");
                checks.CheckErrors(() => Assert.AreEqual("", columnType, "We expect no data type on invalid column"));

                columnType = spotfire.GetColumnDataType(tables.First(), "garbage column");
                checks.CheckErrors(() => Assert.AreEqual("", columnType, "We expect no data type on invalid column in real table"));

                long count = spotfire.GetColumnDistinctValueCount("garbage table", "garbage column");
                checks.CheckErrors(() => Assert.AreEqual(-1, count, "We expect no unique values on invalid column"));

                count = spotfire.GetColumnDistinctValueCount(tables.First(), "garbage column");
                checks.CheckErrors(() => Assert.AreEqual(-1, count, "We expect no unique values on invalid column in real table"));

                IReadOnlyCollection<string> distinctValues = spotfire.GetColumnDistinctValues("garbage table", "garbage column", 0, 100);
                checks.CheckErrors(() => Assert.AreEqual(1, distinctValues.Count, "We expect no unique values on invalid column"));
                checks.CheckErrors(() => Assert.AreEqual("column doesn't exist", distinctValues.First(), "We expect error response for unique values on invalid column"));

                distinctValues = spotfire.GetColumnDistinctValues(tables.First(), "garbage column", 0, 100);
                checks.CheckErrors(() => Assert.AreEqual(1, distinctValues.Count, "We expect no unique values on invalid column in real table"));
                checks.CheckErrors(() => Assert.AreEqual("column doesn't exist", distinctValues.First(), "We expect error response for unique values on invalid column in real table"));
            }

            checks.AssertEmpty();
        }

        [TestMethod]
        public void VisualTests()
        {
            MultipleAsserts checks = new MultipleAsserts();

            var expectedVisuals = new List<Tuple<string, List<Tuple<string, string, bool>>>>()
            {
                Tuple.Create(
                    "Tables",
                    new List<Tuple<string, string, bool>>()
                    {
                        Tuple.Create("Summary table", "tabular", true),
                        Tuple.Create("Graphical table", "tabular", true),
                        Tuple.Create("Cross table", "tabular", true),
                        Tuple.Create("Table", "tabular", true)
                    }
                ),
                Tuple.Create(
                    "Charts",
                    new List<Tuple<string, string, bool>>()
                    {
                        Tuple.Create("Waterfall", "image", true),
                        Tuple.Create("Line", "image", true),
                        Tuple.Create("Bar", "image", true),
                        Tuple.Create("Pie", "image", true),
                        Tuple.Create("Combination", "image", true),
                        Tuple.Create("Scatter", "image", true)
                    }
                ),
                Tuple.Create(
                    "More charts",
                    new List<Tuple<string, string, bool>>()
                    {
                        Tuple.Create("KPI Chart", "image", true),
                        Tuple.Create("Parallel Coordinate Plot", "image", true),
                        Tuple.Create("Heat map", "image", true),
                        Tuple.Create("Tree", "image", true),
                        Tuple.Create("Box Plot", "image", true)
                    }
                ),
                Tuple.Create(
                    "Map",
                    new List<Tuple<string, string, bool>>()
                    {
                        Tuple.Create("Map Chart", "image", true)
                    }
                ),
                Tuple.Create(
                    "Text + web",
                    new List<Tuple<string, string, bool>>()
                    {
                        Tuple.Create("Text Area", "textual", true),
                        Tuple.Create("JavaScript Visualization", "image", true)
                    }
                ),
                Tuple.Create(
                    "No title, maximize or export",
                    new List<Tuple<string, string, bool>>()
                    {
                        Tuple.Create("", "tabular", false)
                    }
                )
            };

            int spotfireInstanceCount = 0;

            foreach (SpotfireDriver spotfire in Spotfires)
            {
                IReadOnlyCollection<string> pages = spotfire.GetPages();
                checks.CheckErrors(() => Assert.AreEqual(expectedVisuals.Count, pages.Count, "Mismatching number of pages, instance {0}", spotfireInstanceCount));

                foreach (Tuple<string, List<Tuple<string, string, bool>>> page in expectedVisuals)
                {
                    if (!pages.Contains(page.Item1))
                    {
                        checks.CheckErrors(() => Assert.Fail("Page {0} is missing, instance {1}", page.Item1, spotfireInstanceCount));
                    }
                    else
                    {
                        spotfire.SetActivePage(page.Item1, 60);
                        spotfire.RestoreVisualLayout();

                        List<Visual> visuals = spotfire.GetVisuals();
                        checks.CheckErrors(() => Assert.AreEqual(page.Item2.Count, visuals.Count, "Mismatching number of visuals on page {0} instance {1}", page.Item1, spotfireInstanceCount));

                        for (int visualNumber = 0; visualNumber < visuals.Count && visualNumber < page.Item2.Count; visualNumber++)
                        {
                            Visual visual = visuals[visualNumber];
                            Tuple<string, string, bool> expectedVisual = page.Item2[visualNumber];

                            TestContext.WriteLine("Page {0}, visual number {1}, title {2}", page.Item1, visualNumber, visual.Title);

                            checks.CheckErrors(() => Assert.AreEqual(expectedVisual.Item1, visual.Title, "Visual title did not match expected value. Page {0}, visual number {1}, instance {2}", page.Item1, visualNumber, spotfireInstanceCount));
#pragma warning disable S3358 // Ternary operators should not be nested
                            string actualType = visual.IsTextType ? "textual" : (visual.IsImageType ? "image" : "tabular");
#pragma warning restore S3358 // Ternary operators should not be nested
                            checks.CheckErrors(() => Assert.AreEqual(expectedVisual.Item2, actualType, "Visual type did not match expected value. Page {0}, visual number {1}, instance {2}", page.Item1, visualNumber, spotfireInstanceCount));
                            checks.CheckErrors(() => Assert.AreEqual(expectedVisual.Item3, visual.CanMaximize(), "CanMaximize  did not match expected value. Page {0}, visual number {1}, instance {2}", page.Item1, visualNumber, spotfireInstanceCount));

                            if (visual.IsTextType)
                            {
                                string expectedDataPath = Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DataFiles"), string.Format("ExpectedText-{0}-{1}.txt", page.Item1, visual.Title));
                                // Files are created on Windows system but might be used for tests run under Linux
                                string expected = File.ReadAllText(expectedDataPath).Replace("\r\n", Environment.NewLine);
                                TestContext.WriteLine(string.Format("Text visual, content: {0}", visual.Text));
                                checks.CheckErrors(() => Assert.AreEqual(expected, visual.Text, "Text for visual {0} on page {1} for instance {2} does not match expected.", visual.Title, page.Item1, spotfireInstanceCount));
                            }
                            else if (visual.IsImageType)
                            {
                                TestContext.WriteLine(string.Format("Image visual, content size: ({0},{1})", visual.Content.Size.Width, visual.Content.Size.Height));

                                // Test resizing
                                Size current = visual.Content.Size;
                                Size shrink = new Size(current.Width / 2, current.Height / 2);
                                visual.ResizeContent(shrink);
                                checks.CheckErrors(() => Assert.AreEqual(shrink, visual.Content.Size, "Resizing visual {0} failed", visual.Title));
                                visual.ResizeContent(current);
                                checks.CheckErrors(() => Assert.AreEqual(current, visual.Content.Size, "Resizing visual {0} failed", visual.Title));

                                Bitmap image = visual.GetImage();
                                string instancePrefix = (spotfireInstanceCount > 0) ? spotfireInstanceCount.ToString("000") + "-" : "";
                                string path = TestContext.TestDir + Path.DirectorySeparatorChar + instancePrefix + TestContext.FullyQualifiedTestClassName + "-"+ TestContext.TestName + "-" + page.Item1 + "-" + visual.Title + ".png";
                                image.Save(path);
                                this.TestContext.AddResultFile(path);

                                Dictionary<string, Bitmap> imageComparisons = new Dictionary<string, Bitmap>();

                                bool anyMatch = VisualCompare.CompareVisualImages(visual, Environment.GetEnvironmentVariable("images_folder"),string.Format("{0}-{1}-{2}-{3}", TestContext.FullyQualifiedTestClassName, TestContext.TestName, page.Item1, visual.Title),imageComparisons);

                                // If there's no match we need to write out the mismatches
                                if (!anyMatch)
                                {
                                    TestContext.WriteLine("Images didn't match, check the test results folder for the new image along with images showing comparison with existing possibilities.");
                                    foreach(KeyValuePair<string, Bitmap> imageToSave in imageComparisons)
                                    {
                                        string filename = Path.Combine(TestContext.TestDir, instancePrefix + TestContext.FullyQualifiedTestClassName + "-" + TestContext.TestName + "-" + page.Item1 + "-" + visual.Title + "-" + imageToSave.Key);
                                        imageToSave.Value.Save(filename);
                                        this.TestContext.AddResultFile(filename);
                                    }
                                }
                                checks.CheckErrors(() => Assert.IsTrue(anyMatch, "Image for visual {0} on page {1} for instance {2} does not match a possible expected image", visual.Title, page.Item1, spotfireInstanceCount));
                            }
                            else if (visual.IsTabularType)
                            {
                                string expectedDataPath = Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DataFiles"), string.Format("ExpectedData-{0}-{1}.txt", page.Item1, visual.Title));
                                using (TableData data = visual.GetTableData())
                                using (TableData expectedData = new TableDataFromDelimitedFile(expectedDataPath))
                                {
                                    TestContext.WriteLine(string.Format("Tabular data, {0} columns", data.Columns.Length));
                                    checks.CheckErrors(() => Assert.IsTrue(CompareUtilities.AreEqual(expectedData, data), "Data for visual {0} on page {1} for instance {2} does not match expected data", visual.Title, page.Item1, spotfireInstanceCount));
                                    string path = TestContext.TestDir + Path.DirectorySeparatorChar + TestContext.FullyQualifiedTestClassName + "-" + spotfireInstanceCount.ToString("000") + "-" + TestContext.TestName + "-" + page.Item1 + "-" + visual.Title + ".txt";
                                    data.SaveToFile(path);
                                    this.TestContext.AddResultFile(path);
                                }
                            }
                        }
                    }
                }

                try
                {
                    spotfire.SetActivePage("garbage page");
                    checks.CheckErrors(() => Assert.Fail("We expected an error when changing to a page that doesn't exist, instance {0}", spotfireInstanceCount));
                }
                catch (PageNotChangedException)
                {
                    // all good
                }

                spotfireInstanceCount++;
            }

            checks.AssertEmpty();
        }

        [TestMethod]
        public void MaximizeTests()
        {
            MultipleAsserts errors = new MultipleAsserts();

            foreach (SpotfireDriver spotfire in Spotfires)
            {
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

                spotfire.SetActivePage(pages.ElementAt(5));
                visuals = spotfire.GetVisuals();
                errors.CheckErrors(() => Assert.IsFalse(visuals.First().CanMaximize(), "The visual on page {0} shouldn't be maximizable", pages.ElementAt(5)));
                try
                {
                    visuals.First().Maximize();
                    errors.CheckErrors(() => Assert.Fail("We should have received an exception when attempting to maximize the visual on page {0}", pages.ElementAt(5)));
                }
                catch (VisualCannotBeMaximizedException)
                {
                    // all good - expected
                }
                catch (Exception e)
                {
                    errors.CheckErrors(() => Assert.Fail("Unexpected exception when attempting to maximize the visual on page {0}. {1}", pages.ElementAt(5), e.ToString()));
                }
            }

            errors.AssertEmpty();
        }

        [TestMethod]
        public void MarkingTests()
        {
            MultipleAsserts checks = new MultipleAsserts();

            // A collection of what tables to check, what columns to check and how many distinct values we expect when we ask for 100 rows of data
            // Note we only ask for a subset of columns because some columns make no sense (geometry) or cause Spotfire to throw errors due to bugs in Spotfire (hierarchies)
            var tablesColumns = new Dictionary<string, string[]>()
            {
                {
                    "Data Table",
                    new string [] { "Country", "1960 (thousand)", "2017 (thousand)" }
                },
                {
                    "World Countries",
                    new string[] { "Continent", "Region", "Subregion", "Country" }
                }
            };

            var expectedMarkings = new string[] { "Marking" };
            string testFileFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DataFiles");

            foreach (SpotfireDriver spotfire in Spotfires)
            {

                IReadOnlyCollection<string> markingNames = spotfire.GetMarkingNames();

                spotfire.ClearAllMarkings();
                checks.CheckErrors(() => Assert.AreEqual(expectedMarkings.Count(), markingNames.Count, "Markings not as expected"));

                foreach (string markingName in expectedMarkings)
                {
                    checks.CheckErrors(() => Assert.IsTrue(markingNames.Contains(markingName), "Marking {0} is missing", markingName));
                    if (markingNames.Contains(markingName))
                    {
                        foreach (string tableName in tablesColumns.Keys)
                        {
                            using (TableData table = spotfire.GetMarking(markingName, tableName, tablesColumns[tableName], 1000))
                            {
                                if(!table.EndOfData)
                                {
                                    checks.CheckErrors(() => Assert.IsTrue(table.EndOfData, "We expect marking to be empty after clearing"));
                                    TestContext.WriteLine(string.Format("Unexpected data found for marking name {0} for table {1} before toggle:", markingName, tableName));
                                    table.DumpOutData(TestContext.WriteLine);
                                }
                            }

                            // Toggle the marking
                            spotfire.SetMarking(markingName, tableName, "1=1", MarkingOperation.Toggle);

                            using (TableData table = spotfire.GetMarking(markingName, tableName, tablesColumns[tableName], 1000))
                            using (TableData expected = new TableDataFromDelimitedFile(Path.Combine(testFileFolder, "ExpectedMarking" + tableName.Replace(" ","") + ".txt")))
                            {
                                if (!CompareUtilities.AreEqual(expected,table))
                                {
                                    checks.CheckErrors(() => Assert.Fail("Data does not match expected contents for table {0}, marking {1}", tableName, markingName));
                                    TestContext.WriteLine(string.Format("Mismatched data for marking name {0} for table {1}after toggle:", markingName, tableName));
                                    TestContext.WriteLine("Expected contents:");
                                    expected.DumpOutData(TestContext.WriteLine);
                                    TestContext.WriteLine("Actual contents:");
                                    table.DumpOutData(TestContext.WriteLine);
                                }
                            }
                        }
                    }
                }
            }

            checks.AssertEmpty();
        }

        [TestMethod]
        public void LocalizationTests()
        {
            MultipleAsserts checks = new MultipleAsserts();
            foreach (SpotfireDriver spotfire in Spotfires)
            {
                IReadOnlyDictionary<string, string> answer = spotfire.GetLocalization();
                checks.CheckErrors(() => Assert.AreNotEqual(0, answer.Count, "We expect some localization values"));
                checks.CheckErrors(() => Assert.IsTrue(answer.ContainsKey("Ready"), "We expect a 'Ready' value in localization"));
            }
            checks.AssertEmpty();
        }

        [TestMethod]
        public void NotificationsTest()
        {
            MultipleAsserts checks = new MultipleAsserts();
            foreach (SpotfireDriver spotfire in Spotfires)
            {
                string answer = spotfire.GetNotifications();
                checks.CheckErrors(() => Assert.AreEqual("", answer, "We don't expect any notifications"));

                spotfire.SetDocumentProperty("TriggerWarning", "A test warning");
                answer = spotfire.GetNotifications();
                checks.CheckErrors(() => Assert.AreEqual("Test warning" + Environment.NewLine + "A test warning" + Environment.NewLine + "A test warning", answer, "Test warning failed"));

                spotfire.SetDocumentProperty("TriggerError", "A test error");
                answer = spotfire.GetNotifications();
                checks.CheckErrors(() => Assert.AreEqual("Test error" + Environment.NewLine + "A test error" + Environment.NewLine + "A test error", answer, "Test error failed"));
            }
            checks.AssertEmpty();
        }

        [TestMethod]
        public void ValidateConsoleOutput()
        {
            MultipleAsserts checks = new MultipleAsserts();

            foreach (SpotfireDriver spotfire in Spotfires)
            {
                using (StringWriter sw = new StringWriter())
                {
                    Console.SetOut(sw);

                    spotfire.OutputToConsole = true;
                    spotfire.OutputStatusMessage("test");
                    spotfire.OutputToConsole = false;
                    checks.CheckErrors(() => Assert.AreEqual("test" + Environment.NewLine, sw.ToString()));
                }
            }

            checks.AssertEmpty();
        }

        [TestMethod]
        public void GetElementTest()
        {
            MultipleAsserts checks = new MultipleAsserts();

            foreach (SpotfireDriver spotfire in Spotfires)
            {
                IWebElement element = spotfire.FindElement("An element, with no timeout", By.CssSelector("div"), timeoutInSeconds: 0);
                checks.CheckErrors(() => Assert.AreNotEqual(null, element, "No element found"));

                element = spotfire.FindElement("An element, with with timeout", By.CssSelector("div"), timeoutInSeconds: 10);
                checks.CheckErrors(() => Assert.AreNotEqual(null, element, "No element found"));
            }

            checks.AssertEmpty();
        }

        [TestMethod]
        public void ErrorConditionsTest()
        {
            MultipleAsserts checks = new MultipleAsserts();

            foreach (SpotfireDriver spotfire in Spotfires)
            {
                try
                {
                    spotfire.ExecuteAsyncWrapperScript("throw new Error()");
                    checks.CheckErrors(() => Assert.Fail("We expected an error from ExecuteAsyncWrapperScript"));
                }
                catch (Exception ex)
                {
                    checks.CheckErrors(() => Assert.AreEqual(typeof(WebDriverException), ex.GetType(), "We expected a WebDriverException from ExecuteAsyncWrapperScript, but got: {0}", ex.Message));
                }

                try
                {
                    spotfire.ExecuteAsyncWrapperScript("SpotfireTestWrapper.application.analysisDocument.setActivePage(-1)");
                    checks.CheckErrors(() => Assert.Fail("We expected an error from ExecuteAsyncWrapperScript"));
                }
                catch (Exception ex)
                {
                    checks.CheckErrors(() => Assert.AreEqual(typeof(SpotfireAPIException), ex.GetType(), "We expected a SpotfireAPIException from ExecuteAsyncWrapperScript, but got: {0}", ex.Message));
                }

                try
                {
                    spotfire.ExecuteWrapperScript("throw new Error()");
                    checks.CheckErrors(() => Assert.Fail("We expected an error from ExecuteWrapperScript"));
                }
                catch (Exception ex)
                {
                    checks.CheckErrors(() => Assert.AreEqual(typeof(WebDriverException), ex.GetType(), "We expected a WebDriverException from ExecuteWrapperScript, but got: {0}", ex.Message));
                }

                try
                {
                    spotfire.ExecuteWrapperScript("SpotfireTestWrapper.application.analysisDocument.setActivePage(-1)" );
                    // The API error will happen asynchronously, so wait 10 seconds and then check for errors
                    Thread.Sleep(10000);
                    spotfire.ExecuteWrapperScript("throw new Error()");
                    checks.CheckErrors(() => Assert.Fail("We expected an error from ExecuteWrapperScript"));
                }
                catch (Exception ex)
                {
                    checks.CheckErrors(() => Assert.AreEqual(typeof(SpotfireAPIException), ex.GetType(), "We expected a SpotfireAPIException from ExecuteWrapperScript, but got: {0}", ex.Message));
                }
            }

            checks.AssertEmpty();
        }
    }
}

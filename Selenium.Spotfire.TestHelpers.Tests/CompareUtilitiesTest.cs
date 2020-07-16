using System;
using Selenium.Spotfire;
using Selenium.Spotfire.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace Selenium.Spotfire.TestHelpers.Tests
{
    [TestClass]
    public class CompareUtilitiesTest
    {
        [TestMethod]
        public void MatchingTables()
        {
            Dictionary<string, object> testData = new Dictionary<string, object>()
            {
                { "column1" , new string[] {"column1row1", "column1row2" } },
                { "column2", new string[] {"column2row1", "column2row2"} }
            };
            string[] columns = { "column1", "column2" };
            var rows = new[] { new[] { "column1row1", "column2row1" }, new[] { "column1row2", "column2row2" } };

            using (TableData table1 = new TableDataFromRows(columns, rows))
            using (TableData table2 = new TableDataFromColumns(testData))
            {
                Assert.IsTrue(CompareUtilities.AreEqual(table1, table2));
            }
        }

        [TestMethod]
        public void MatchingTablesIdenticalObject()
        {
            Dictionary<string, object> testData = new Dictionary<string, object>()
            {
                { "column1" , new string[] {"column1row1", "column1row2" } },
                { "column2", new string[] {"column2row1", "column2row2"} }
            };

            using (TableData table = new TableDataFromColumns(testData))
            {
                Assert.IsTrue(CompareUtilities.AreEqual(table, table));
            }
        }

        [TestMethod]
        public void EmptyTables()
        {
            using (TableData table1 = new TableDataFromRows(new string[] { }, new string[][] { }))
            using (TableData table2 = new TableDataFromColumns(new Dictionary<string, object>()))
            {
                Assert.IsTrue(CompareUtilities.AreEqual(table1, table2));
            }
        }
        
        [TestMethod]
        public void MismatchingTablesDifferentData()
        {
            Dictionary<string, object> testData = new Dictionary<string, object>()
            {
                { "column1" , new string[] {"column1row1", "column1row2" } },
                { "column2", new string[] {"column2row1", "column2row2"} }
            };
            string[] columns = { "column1", "column2" };
            var rows = new[] { new[] { "column1row1", "column2row1" }, new[] { "column1row2", "column2row2mistake" } };

            using (TableData table1 = new TableDataFromRows(columns, rows))
            using (TableData table2 = new TableDataFromColumns(testData))
            {
                Assert.IsFalse(CompareUtilities.AreEqual(table1, table2));
            }
        }

        [TestMethod]
        public void MismatchingTablesDifferentColumns()
        {
            Dictionary<string, object> testData = new Dictionary<string, object>()
            {
                { "column1" , new string[] {"column1row1", "column1row2" } },
                { "column2", new string[] {"column2row1", "column2row2"} }
            };
            string[] columns = { "column1" };
            var rows = new[] { new[] { "column1row1" }, new[] { "column1row2" } };

            using (TableData table1 = new TableDataFromRows(columns, rows))
            using (TableData table2 = new TableDataFromColumns(testData))
            {
                Assert.IsFalse(CompareUtilities.AreEqual(table1, table2));
            }
        }

        [TestMethod]
        public void MismatchingTablesTable1Longer()
        {
            Dictionary<string, object> testData = new Dictionary<string, object>()
            {
                { "column1" , new string[] {"column1row1", "column1row2" } },
                { "column2", new string[] {"column2row1", "column2row2"} }
            };
            string[] columns = { "column1", "column2" };
            var rows = new[] { new[] { "column1row1", "column2row1" } };

            using (TableData table1 = new TableDataFromRows(columns, rows))
            using (TableData table2 = new TableDataFromColumns(testData))
            {
                Assert.IsFalse(CompareUtilities.AreEqual(table1, table2));
            }
        }

        [TestMethod]
        public void MismatchingTablesTable2Longer()
        {
            Dictionary<string, object> testData = new Dictionary<string, object>()
            {
                { "column1" , new string[] {"column1row1", "column1row2" } }
            };
            string[] columns = { "column1", "column2" };
            var rows = new[] { new[] { "column1row1", "column2row1" }, new[] { "column1row2", "column2row2" } };

            using (TableData table1 = new TableDataFromRows(columns, rows))
            using (TableData table2 = new TableDataFromColumns(testData))
            {
                Assert.IsFalse(CompareUtilities.AreEqual(table1, table2));
            }
        }

        [TestMethod]
        public void MismatchingTablesTable1Null()
        {
            string[] columns = { "column1", "column2" };
            var rows = new[] { new[] { "column1row1", "column2row1" }, new[] { "column1row2", "column2row2" } };

            using (TableData table = new TableDataFromRows(columns, rows))
            {
                Assert.IsFalse(CompareUtilities.AreEqual(null, table));
            }
        }

        [TestMethod]
        public void MismatchingTablesTable2Null()
        {
            string[] columns = { "column1", "column2" };
            var rows = new[] { new[] { "column1row1", "column2row1" }, new[] { "column1row2", "column2row2" } };

            using (TableData table = new TableDataFromRows(columns, rows))
            {
                Assert.IsFalse(CompareUtilities.AreEqual(table, null));
            }
        }

        [TestMethod]
        public void MatchingImages()
        {
            Bitmap bitmap1 = new Bitmap(10, 10);
            Graphics graphics = Graphics.FromImage(bitmap1);
            graphics.FillRectangle(Brushes.Red, 1, 1, 5, 5);
            Bitmap bitmap2 = new Bitmap(bitmap1);

            Assert.IsTrue(CompareUtilities.AreEqual(bitmap1, bitmap2));
        }

        [TestMethod]
        public void MatchingImagesOffsetDownRight()
        {
            Bitmap bitmap1 = new Bitmap(10, 10);
            Graphics graphics = Graphics.FromImage(bitmap1);
            graphics.FillRectangle(Brushes.Red, 1, 1, 5, 5);
            Bitmap bitmap2 = new Bitmap(10, 10);
            Graphics graphics2 = Graphics.FromImage(bitmap2);
            graphics2.FillRectangle(Brushes.Red, 3, 3, 5, 5);

            Assert.IsTrue(CompareUtilities.AreEqual(bitmap1, bitmap2));
        }

        [TestMethod]
        public void MatchingImagesOffsetUpLeft()
        {
            Bitmap bitmap1 = new Bitmap(10, 10);
            Graphics graphics = Graphics.FromImage(bitmap1);
            graphics.FillRectangle(Brushes.Red, 3, 3, 5, 5);
            Bitmap bitmap2 = new Bitmap(10, 10);
            Graphics graphics2 = Graphics.FromImage(bitmap2);
            graphics2.FillRectangle(Brushes.Red, 1, 1, 5, 5);

            Assert.IsTrue(CompareUtilities.AreEqual(bitmap1, bitmap2));
        }

        [TestMethod]
        public void MatchingImagesOffsetDownLeft()
        {
            Bitmap bitmap1 = new Bitmap(10, 10);
            Graphics graphics = Graphics.FromImage(bitmap1);
            graphics.FillRectangle(Brushes.Red, 3, 0, 5, 5);
            Bitmap bitmap2 = new Bitmap(10, 10);
            Graphics graphics2 = Graphics.FromImage(bitmap2);
            graphics2.FillRectangle(Brushes.Red, 0, 3, 5, 5);

            Assert.IsTrue(CompareUtilities.AreEqual(bitmap1, bitmap2));
        }

        [TestMethod]
        public void MatchingImagesOffsetUpRight()
        {
            Bitmap bitmap1 = new Bitmap(10, 10);
            Graphics graphics = Graphics.FromImage(bitmap1);
            graphics.FillRectangle(Brushes.Red, 0, 3, 5, 5);
            Bitmap bitmap2 = new Bitmap(10, 10);
            Graphics graphics2 = Graphics.FromImage(bitmap2);
            graphics2.FillRectangle(Brushes.Red, 3, 0, 5, 5);

            Assert.IsTrue(CompareUtilities.AreEqual(bitmap1, bitmap2));
        }

        [TestMethod]
        public void MismatchingImagesDifferent()
        {
            Bitmap bitmap1 = new Bitmap(10, 10);
            Graphics graphics = Graphics.FromImage(bitmap1);
            graphics.FillRectangle(Brushes.Red, 0, 3, 5, 5);
            Bitmap bitmap2 = new Bitmap(10, 10);
            Graphics graphics2 = Graphics.FromImage(bitmap2);
            graphics2.FillRectangle(Brushes.Red, 3, 0, 6, 6);

            Assert.IsFalse(CompareUtilities.AreEqual(bitmap1, bitmap2));
        }

        [TestMethod]
        public void MismatchingImagesSecondTruncated()
        {
            Bitmap bitmap1 = new Bitmap(10, 10);
            Graphics graphics = Graphics.FromImage(bitmap1);
            graphics.FillRectangle(Brushes.Red, 1, 1, 5, 5);
            Bitmap bitmap2 = new Bitmap(10, 10);
            Graphics graphics2 = Graphics.FromImage(bitmap2);
            graphics2.FillRectangle(Brushes.Red, 6, 6, 4, 4);

            Assert.IsFalse(CompareUtilities.AreEqual(bitmap1, bitmap2));
        }

        [TestMethod]
        public void MismatchingImagesFirstTruncated()
        {
            Bitmap bitmap1 = new Bitmap(10, 10);
            Graphics graphics = Graphics.FromImage(bitmap1);
            graphics.FillRectangle(Brushes.Red, 6, 6, 4, 4);
            Bitmap bitmap2 = new Bitmap(10, 10);
            Graphics graphics2 = Graphics.FromImage(bitmap2);
            graphics2.FillRectangle(Brushes.Red, 1, 1, 5, 5);

            Assert.IsFalse(CompareUtilities.AreEqual(bitmap1, bitmap2));
        }

        [TestMethod]
        public void MismatchingImagesFirstBlank()
        {
            Bitmap bitmap1 = new Bitmap(10, 10);
            Bitmap bitmap2 = new Bitmap(10, 10);
            Graphics graphics2 = Graphics.FromImage(bitmap2);
            graphics2.FillRectangle(Brushes.Red, 3, 0, 6, 6);

            Assert.IsFalse(CompareUtilities.AreEqual(bitmap1, bitmap2));
        }

        [TestMethod]
        public void MismatchingImagesSecondBlank()
        {
            Bitmap bitmap1 = new Bitmap(10, 10);
            Graphics graphics = Graphics.FromImage(bitmap1);
            graphics.FillRectangle(Brushes.Red, 0, 3, 5, 5);
            Bitmap bitmap2 = new Bitmap(10, 10);

            Assert.IsFalse(CompareUtilities.AreEqual(bitmap1, bitmap2));
        }

        [TestMethod]
        public void MismatchingImagesDifferentPixelDepth()
        {
            Bitmap bitmap1 = new Bitmap(10, 10, PixelFormat.Format32bppPArgb);
            Bitmap bitmap2 = new Bitmap(10, 10, PixelFormat.Format32bppArgb);

            Assert.IsFalse(CompareUtilities.AreEqual(bitmap1, bitmap2));
        }

        /// <summary>
        /// This test will look for files in the Images folder of the project. It will compare pairs to ensure they match. Files should be named 'Image-x-y-z.png' where x is the image number,y is 0/1 and z is a descriptive name- e.g. image-0-0 will be matched with image-0-1
        /// </summary>
        [TestMethod]
        public void ImagesFromFileTest()
        {
            string imageFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Images");
            for (int imageCount = 0; Directory.GetFiles(imageFolder, string.Format("Image-{0}-0-*.png", imageCount)).Length>0; imageCount++)
            {
                string filename0 = Directory.GetFiles(imageFolder, string.Format("Image-{0}-0-*.png", imageCount))[0];
                string filename1 = Directory.GetFiles(imageFolder, string.Format("Image-{0}-1-*.png", imageCount))[0];
                using (Bitmap image0 = new Bitmap(filename0))
                using (Bitmap image1 = new Bitmap(filename1))
                {
                    Assert.IsTrue(CompareUtilities.AreEqual(image0, image1), "Image {0} failed to match, names '{1}' '{2}", imageCount, filename0, filename1);
                }
            }
        }

        [TestMethod]
        public void ImageDifference()
        {
            Bitmap bitmap1 = new Bitmap(10, 10);
            Graphics graphics = Graphics.FromImage(bitmap1);
            graphics.FillRectangle(Brushes.Red, 0, 3, 5, 5);
            Bitmap bitmap2 = new Bitmap(10, 10);
            Graphics graphics2 = Graphics.FromImage(bitmap2);
            graphics2.FillRectangle(Brushes.Red, 0, 0, 6, 0);

            CompareUtilities.GenerateImageDifference(bitmap1, bitmap2);

            Bitmap bitmap3 = new Bitmap(10, 10);
            Graphics graphics3 = Graphics.FromImage(bitmap3);
            graphics3.FillRectangle(new SolidBrush(Color.FromArgb(20, Color.Red)), 0, 3, 5, 5);
            graphics3.FillRectangle(Brushes.Red, 0, 0, 6, 0);

            Assert.IsFalse(CompareUtilities.AreEqual(bitmap1, bitmap3));
        }
    }
}

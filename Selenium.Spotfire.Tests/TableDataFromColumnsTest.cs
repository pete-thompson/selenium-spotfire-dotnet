using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Selenium.Spotfire.Tests
{
    [TestClass]
    public class TableDataFromColumnsTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestEmptyCollection()
        {
            using (TableDataFromColumns table = new TableDataFromColumns(new Dictionary<string, object>()))
            {
                Assert.AreEqual(0, table.Columns.Length);
                Assert.IsTrue(table.EndOfData);
                table.DumpOutData(TestContext.WriteLine);
            }
        }

        [TestMethod]
        public void TestSomeData()
        {
            Dictionary<string, object> testData = new Dictionary<string, object>()
            {
                { "column1" , new string[] {"column1row1", "column1row2" } },
                { "column2", new string[] {"column2row1", "column2row2"} }
            };

            using (TableDataFromColumns table = new TableDataFromColumns(testData))
            {
                Assert.AreEqual("column1", table.Columns[0]);
                Assert.AreEqual("column2", table.Columns[1]);

                Assert.AreEqual(2, table.Columns.Length);
                Assert.IsFalse(table.EndOfData);

                string[] row = table.ReadARow();
                Assert.AreEqual(((string[])testData["column1"])[0], row[0]);
                Assert.AreEqual(((string[])testData["column2"])[0], row[1]);
                Assert.IsFalse(table.EndOfData);

                row = table.ReadARow();
                Assert.AreEqual(((string[])testData["column1"])[1], row[0]);
                Assert.AreEqual(((string[])testData["column2"])[1], row[1]);
                Assert.IsTrue(table.EndOfData);

                table.ReturnToStart();
                row = table.ReadARow();
                Assert.AreEqual(((string[])testData["column1"])[0], row[0]);
                Assert.AreEqual(((string[])testData["column2"])[0], row[1]);
                Assert.IsFalse(table.EndOfData);

                table.DumpOutData(TestContext.WriteLine);
            }
        }
    }
}

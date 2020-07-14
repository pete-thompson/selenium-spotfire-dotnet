using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Selenium.Spotfire.Tests
{
    [TestClass]
    public class TableDataFromRowsTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestEmptyCollection()
        {
            using (TableDataFromRows table = new TableDataFromRows(new string[0], new string[0][]))
            {
                Assert.AreEqual(0, table.Columns.Length);
                Assert.IsTrue(table.EndOfData);
                table.DumpOutData(TestContext.WriteLine);
            }
        }

        [TestMethod]
        public void TestSomeData()
        {
            string[] columns = { "column1", "column2" };
            var rows = new []{  new []{ "column1row1", "column2row1" }, new[] { "column1row2", "column2row2" } };

            using (TableDataFromRows table = new TableDataFromRows(columns, rows))
            {
                Assert.AreEqual(2, table.Columns.Length);
                Assert.IsFalse(table.EndOfData);

                Assert.AreEqual("column1", table.Columns[0]);
                Assert.AreEqual("column2", table.Columns[1]);

                string[] row = table.ReadARow();
                Assert.AreEqual(rows[0][0], row[0]);
                Assert.AreEqual(rows[0][1], row[1]);
                Assert.IsFalse(table.EndOfData);

                row = table.ReadARow();
                Assert.AreEqual(rows[1][0], row[0]);
                Assert.AreEqual(rows[1][1], row[1]);
                Assert.IsTrue(table.EndOfData);

                table.ReturnToStart();
                row = table.ReadARow();
                Assert.AreEqual(rows[0][0], row[0]);
                Assert.AreEqual(rows[0][1], row[1]);
                Assert.IsFalse(table.EndOfData);

                table.DumpOutData(TestContext.WriteLine);
            }
        }
    }
}

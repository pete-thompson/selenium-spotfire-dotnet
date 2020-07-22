using Selenium.Spotfire;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Reflection;

namespace Selenium.Spotfire.Tests
{
    [TestClass]
    public class TableDataFromDelimitedFileTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void EmptyFile()
        {
            string testFile = Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DataFiles"), "EmptyDataFile.txt");
            using (TableDataFromDelimitedFile table = new Spotfire.TableDataFromDelimitedFile(testFile))
            {
                Assert.AreEqual(0, table.Columns.Length);
                Assert.IsTrue(table.EndOfData);
                table.DumpOutData(TestContext.WriteLine);
            }
        }
        [TestMethod]
        public void SimpleFile()
        {
            string testFile = Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DataFiles"), "SimpleDataFile.txt");
            using (TableDataFromDelimitedFile table = new Spotfire.TableDataFromDelimitedFile(testFile))
            {
                Assert.AreEqual("column1", table.Columns[0]);
                Assert.AreEqual("column2", table.Columns[1]);

                Assert.AreEqual(2, table.Columns.Length);
                Assert.IsFalse(table.EndOfData);

                string[] row = table.ReadARow();
                Assert.AreEqual("column1,row1", row[0]);
                Assert.AreEqual("column2,row1", row[1]);
                Assert.IsFalse(table.EndOfData);

                row = table.ReadARow();
                Assert.AreEqual("column1,row2", row[0]);
                Assert.AreEqual("column2,row2", row[1]);
                Assert.IsTrue(table.EndOfData);

                table.ReturnToStart();
                row = table.ReadARow();
                Assert.AreEqual("column1,row1", row[0]);
                Assert.AreEqual("column2,row1", row[1]);
                Assert.IsFalse(table.EndOfData);

                table.DumpOutData(TestContext.WriteLine);

                string filename = TestContext.TestDir + Path.DirectorySeparatorChar + TestContext.FullyQualifiedTestClassName + "-" + TestContext.TestName + ".txt";
                table.SaveToFile(filename);
                this.TestContext.AddResultFile(filename);

                filename = TestContext.TestDir + Path.DirectorySeparatorChar + TestContext.FullyQualifiedTestClassName + "-" + TestContext.TestName + ".csv";
                table.SaveToFile(filename, ',');
                this.TestContext.AddResultFile(filename);
            }
        }
    }
}

using System;
using Selenium.Spotfire;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Selenium.Spotfire.Tests
{
    [TestClass]
    public class MarkingOperationTest
    {
        [TestMethod]
        public void BasicTest()
        {
            Assert.AreEqual("spotfire.webPlayer.markingOperation.ADD", MarkingOperation.Add);
            Assert.AreEqual("spotfire.webPlayer.markingOperation.CLEAR", MarkingOperation.Clear);
            Assert.AreEqual("spotfire.webPlayer.markingOperation.INTERSECT", MarkingOperation.Intersect);
            Assert.AreEqual("spotfire.webPlayer.markingOperation.REPLACE", MarkingOperation.Replace);
            Assert.AreEqual("spotfire.webPlayer.markingOperation.SUBTRACT", MarkingOperation.Subtract);
            Assert.AreEqual("spotfire.webPlayer.markingOperation.TOGGLE", MarkingOperation.Toggle);
        }
    }
}

using System;
using Selenium.Spotfire;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Selenium.Spotfire.Tests
{
    [TestClass]
    public class CustomExceptionsTest
    {
        [TestMethod]
        public void TestVisualCannotBeMaximizedException()
        {
            VisualCannotBeMaximizedException ex1 = new VisualCannotBeMaximizedException();
            VisualCannotBeMaximizedException ex2 = new VisualCannotBeMaximizedException("test");
            VisualCannotBeMaximizedException ex3 = new VisualCannotBeMaximizedException("test2", ex2);
            Assert.AreEqual("Exception of type 'Selenium.Spotfire.VisualCannotBeMaximizedException' was thrown.", ex1.Message);
            Assert.AreEqual("test", ex2.Message);
            Assert.AreEqual("test2", ex3.Message);
            Assert.AreEqual(ex2, ex3.InnerException);
        }

        [TestMethod]
        public void TestSpotfireAPIException()
        {
            SpotfireAPIException ex1 = new SpotfireAPIException();
            SpotfireAPIException ex2 = new SpotfireAPIException("test");
            SpotfireAPIException ex3 = new SpotfireAPIException("test2", ex2);
            Assert.AreEqual("Exception of type 'Selenium.Spotfire.SpotfireAPIException' was thrown.", ex1.Message);
            Assert.AreEqual("test", ex2.Message);
            Assert.AreEqual("test2", ex3.Message);
            Assert.AreEqual(ex2, ex3.InnerException);
        }

        [TestMethod]
        public void TestPageNotChangedException()
        {
            PageNotChangedException ex1 = new PageNotChangedException();
            PageNotChangedException ex2 = new PageNotChangedException("test");
            PageNotChangedException ex3 = new PageNotChangedException("test2", ex2);
            Assert.AreEqual("Exception of type 'Selenium.Spotfire.PageNotChangedException' was thrown.", ex1.Message);
            Assert.AreEqual("test", ex2.Message);
            Assert.AreEqual("test2", ex3.Message);
            Assert.AreEqual(ex2, ex3.InnerException);
        }

        [TestMethod]
        public void TestNoServerURLException()
        {
            NoServerURLException ex1 = new NoServerURLException();
            NoServerURLException ex2 = new NoServerURLException("test");
            NoServerURLException ex3 = new NoServerURLException("test2", ex2);
            Assert.AreEqual("Exception of type 'Selenium.Spotfire.NoServerURLException' was thrown.", ex1.Message);
            Assert.AreEqual("test", ex2.Message);
            Assert.AreEqual("test2", ex3.Message);
            Assert.AreEqual(ex2, ex3.InnerException);
        }
    }
}

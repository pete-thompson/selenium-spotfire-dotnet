using System;
using Selenium.Spotfire.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Selenium.Spotfire.TestHelpers.Tests
{
    [TestClass]
    public class MultipleAssertsTest
    {
        [TestMethod]
        public void NoErrors()
        {
            MultipleAsserts ms = new MultipleAsserts();
            ms.CheckErrors(() => { });
            ms.CheckErrors(() => { });
            ms.AssertEmpty();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void AnError()
        {
            MultipleAsserts ms = new MultipleAsserts();
            ms.CheckErrors(() => { Assert.Fail(); });
            ms.AssertEmpty();
        }
    }
}

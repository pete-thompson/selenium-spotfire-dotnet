using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Selenium.Spotfire.MSTest.Tests
{
    // Wrap a regular test context so that we can intercept lines written and files added and check that they are as expected
    internal class TestingTestContext : TestContext
    {
        private readonly TestContext Original;
        public bool ThrowErrorOnAddResult;
        public readonly List<string> Lines;
        public readonly List<string> ResultFileNames;

        public TestingTestContext(TestContext original)
        {
            Original = original;
            Lines = new List<string>();
            ResultFileNames = new List<string>();
        }

        public override IDictionary Properties => Original.Properties;

        public override void AddResultFile(string fileName)
        {
            if (ThrowErrorOnAddResult)
            {
                throw new ApplicationException("dummy exception for testing");
            }
            ResultFileNames.Add(fileName);
            Original.AddResultFile(fileName);
        }

        public override void WriteLine(string message)
        {
            Lines.Add(message);
            Original.WriteLine(message);
        }

        public override void WriteLine(string format, params object[] args)
        {
            WriteLine(string.Format(format, args));
        }

        public override void Write(string message)
        {
            WriteLine(message);
        }

        public override void Write(string format, params object[] args)
        {
            WriteLine(string.Format(format, args));
        }
    }
}

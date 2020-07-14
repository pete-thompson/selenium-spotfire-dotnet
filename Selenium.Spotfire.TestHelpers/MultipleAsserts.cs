using System;
using System.Collections.Generic;
using System.Linq;

namespace Selenium.Spotfire.TestHelpers
{
    /// <summary>
    /// Helper to allow for multiple assertion checks during a single test
    /// </summary>
    public class MultipleAsserts : List<string>
    {
        public delegate void AssertDelegate();

        /// <summary>
        /// Run an assertion, but instead of throwing exceptions capture any errors to our list of errors
        /// This allows us to run many checks in a single test
        /// </summary>
        /// <param name="assert"></param>
        public void CheckErrors(AssertDelegate assert)
        {
            try
            {
                assert();
            }
            catch (Exception e)
            {
                Add(e.Message);
            }
        }

        /// <summary>
        /// Assert that the list of errors is empty
        /// </summary>
        /// <param name="message"></param>
        public void AssertEmpty(string message = "Errors happened during the test: {0}{1}")
        {
            if (Count > 0)
            {
                string errors = this.Aggregate((i, j) => i + Environment.NewLine + j);
                throw new Exception(string.Format(message, Environment.NewLine, errors));
            }
        }
    }
}

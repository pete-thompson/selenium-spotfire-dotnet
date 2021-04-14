using System;

namespace Selenium.Spotfire
{
    /// <summary>
    /// An attempt has been made to open Spotfire without specifying a server URL
    /// </summary>
    [Serializable]
    public class NoServerURLException : Exception
    {
        public NoServerURLException()
        {
        }

        public NoServerURLException(string message)
            : base(message)
        {
        }

        public NoServerURLException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}

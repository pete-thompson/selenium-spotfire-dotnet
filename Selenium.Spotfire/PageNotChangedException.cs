using System;

namespace Selenium.Spotfire
{
    /// <summary>
    /// An attempt has been made to change page, but the page hasn't been changed (maybe because it doesn't exist?).
    /// </summary>
    [Serializable]
    public class PageNotChangedException : Exception
    {
        public PageNotChangedException()
        {
        }

        public PageNotChangedException(string message)
            : base(message)
        {
        }

        public PageNotChangedException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}

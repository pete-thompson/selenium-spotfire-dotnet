using System;

namespace Selenium.Spotfire
{
    /// <summary>
    /// An exception from the Spotfire JavaScript API
    /// </summary>
    [Serializable]
    public class SpotfireAPIException : Exception
    {
        public SpotfireAPIException()
        {
        }

        public SpotfireAPIException(string message)
            : base(message)
        {
        }

        public SpotfireAPIException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

}

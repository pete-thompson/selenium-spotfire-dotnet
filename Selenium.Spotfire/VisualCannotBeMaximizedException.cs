using System;

namespace Selenium.Spotfire
{
    /// <summary>
    /// A visual cannot be maximized, but an attempt has been made to maximize it
    /// </summary>
    [Serializable]
    public class VisualCannotBeMaximizedException : Exception
    {
        public VisualCannotBeMaximizedException()
        {
        }

        public VisualCannotBeMaximizedException(string message)
            : base(message)
        {
        }

        public VisualCannotBeMaximizedException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}

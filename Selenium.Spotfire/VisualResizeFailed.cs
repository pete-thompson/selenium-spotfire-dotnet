using System;

namespace Selenium.Spotfire
{
    /// <summary>
    /// A visual cannot be sized to the requested size (likely requires that the browser be larger than available)
    /// </summary>
    [Serializable]
    public class VisualResizeFailed : Exception
    {
        public VisualResizeFailed()
        {
        }

        public VisualResizeFailed(string message)
            : base(message)
        {
        }

        public VisualResizeFailed(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}

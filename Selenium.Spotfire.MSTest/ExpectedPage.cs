using System.Collections.Generic;

namespace Selenium.Spotfire.MSTest
{
    /// <summary>
    /// A simple object to define expected page characteristics to check
    /// </summary>
    public class ExpectedPage
    {
        public string Title;
        public List<ExpectedVisual> Visuals;

        public bool IgnoreExtraVisuals = false;
    }
}
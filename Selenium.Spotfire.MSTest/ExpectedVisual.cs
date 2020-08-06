namespace Selenium.Spotfire.MSTest
{
    /// <summary>
    /// A simple object to define expected visual characteristics to check
    /// </summary>
    public class ExpectedVisual
    {
        public string Title;
        public enum Type 
        {
            Textual, Tabular, Image
        };
        public Type VisualType;
    }
}
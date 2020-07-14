using System.Collections.Generic;
using System.Linq;

namespace Selenium.Spotfire
{
    /// <summary>
    /// This class implements the TableData class using a collection of data organised in a dictionary of columns
    /// e.g.:
    /// {
    ///     { "column1" , new string[] {"column1row1", "column1row2" } },
    ///     { "column2", new string[] {"column2row1", "column2row2"} }
    /// }
    /// </summary>
    sealed public class TableDataFromColumns : TableData
    {
        /// <summary>
        /// Have we reached the end of the data?
        /// </summary>
        public override bool EndOfData
        {
            get
            {
                if (Collection.Count > 0)
                {
                    IReadOnlyCollection<object> column = (IReadOnlyCollection<object>)Collection.Values.First();
                    return RowNumber >= column.Count;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Read a row of data from the table.
        /// </summary>
        /// <returns></returns>
        public override string[] ReadARow()
        {
            List<string> answer = new List<string>();

            foreach (KeyValuePair<string, object> column in Collection)
            {
                IReadOnlyCollection<object> columnValues = (IReadOnlyCollection<object>)column.Value;
                if (RowNumber < columnValues.Count)
                {
                    answer.Add(columnValues.ElementAt(RowNumber).ToString());
                }
            }

            RowNumber++;

            return answer.ToArray<string>() ;
        }

        /// <summary>
        /// Start again, reading from the first row
        /// </summary>
        public override void ReturnToStart()
        {
            RowNumber = 0;
        }

        private int RowNumber;
        private readonly Dictionary<string, object> Collection;

        /// <summary>
        /// Construct the table using data stored in text separated format in a file
        /// </summary>
        /// <param name="filename"></param>
        public TableDataFromColumns(Dictionary<string, object> collection)
        {
            Columns = collection.Keys.ToArray<string>();

            Collection = collection;
            RowNumber = 0;
        }
    }
}

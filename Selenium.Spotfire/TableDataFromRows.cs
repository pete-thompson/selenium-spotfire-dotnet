using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selenium.Spotfire
{
    /// <summary>
    /// This class implements the TableData class using a collection of data organised in an array of rows
    /// </summary>
    public class TableDataFromRows : TableData
    {
        public override bool EndOfData => CurrentRow >= Rows.Length;

        public override string[] ReadARow()
        {
            CurrentRow++;
            return Rows[CurrentRow - 1];
        }

        public override void ReturnToStart()
        {
            CurrentRow = 0;
        }

        readonly string[][] Rows;
        long CurrentRow;

        /// <summary>
        /// Construct a TableData object
        /// </summary>
        /// <param name="columnNames">An array containing column names</param>
        /// <param name="rowData">An array of arrays containing the rows</param>
        public TableDataFromRows(string[] columnNames, string[][] rowData)
        {
            Columns = columnNames;
            Rows = rowData;
            CurrentRow = 0;
        }
    }
}

using System;
using System.IO;
using System.Text;

namespace Selenium.Spotfire
{
    /// <summary>
    /// This class can be used to read data downloaded from Spotfire
    /// The general pattern for reading data is:
    /// 
    /// using (TableData data=visual.GetTableData())
    /// {
    ///     string[] columns = data.Columns();
    ///     while (!data.EndOfData)
    ///     {
    ///         string[] columnValues = data.ReadARow();
    ///     }
    /// }
    /// </summary>
    public abstract class TableData : IDisposable
    {
        /// <summary>
        /// The columns in the table.
        /// </summary>
        public virtual string[] Columns { get; protected set; } = new string[] { };

        /// <summary>
        /// Have we reached the end of the data?
        /// </summary>
        public abstract bool EndOfData { get; }

        /// <summary>
        /// Read a row of data from the table.
        /// </summary>
        /// <returns></returns>
        public abstract string[] ReadARow();

        /// <summary>
        /// Start again, reading from the first row
        /// </summary>
        public abstract void ReturnToStart();

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Delegate used when dumping out data
        /// </summary>
        /// <param name="line"></param>
        public delegate void WriteLineDelegate(string line);

        private void WriteSeparator(WriteLineDelegate writeLineDelegate, int columnWidth)
        {
            StringBuilder line = new StringBuilder();
            line.Append("|");
            for (int i = 0; i < Columns.Length; i++)
            {
                line.Append(new String('-', columnWidth + 2) + "|");
            }
            writeLineDelegate(line.ToString());
        }

        /// <summary>
        /// Dump out the content of the table.
        /// </summary>
        /// <param name="writeLineDelegate">The method used to write a line - e.g. Console.WriteLine or TestContext.WriteLine</param>
        public virtual void DumpOutData(WriteLineDelegate writeLineDelegate, int columnWidth = 20)
        {
            if (Columns.Length == 0)
            {
                writeLineDelegate("The table is empty");
                return;
            }

            ReturnToStart();

            // Header
            WriteSeparator(writeLineDelegate, columnWidth);
            StringBuilder line = new StringBuilder();
            line.Append("|");
            for (int i = 0; i < Columns.Length; i++)
            {
                string s = Columns[i];
                line.Append(String.Format(" {0,-" + columnWidth.ToString() + "} |", s));
            }
            writeLineDelegate(line.ToString());
            WriteSeparator(writeLineDelegate, columnWidth);

            // Data
            while (!EndOfData)
            {
                line.Clear();
                line.Append("|");
                foreach (string val in ReadARow())
                {
                    string s = val;
                    if (s.Length > columnWidth)
                    {
                        s = s.Substring(0, columnWidth - 3) + "...";
                    }
                    line.Append(String.Format(" {0,-" + columnWidth.ToString() + "} |", s));
                }
                writeLineDelegate(line.ToString());
            }
            WriteSeparator(writeLineDelegate, columnWidth);
        }

        /// <summary>
        /// Write the table out to a file
        /// The data will be tab separated, using quotes around values as necessary
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="delimiter">The delimiter to use (defaults to a tab)</param>
        /// <param name="fieldsEnclosedInQuotes">Whether to enclose values that contain the delimiter in quotes (defaults to true)</param>
        public virtual void SaveToFile(string filename, char delimiter='\t', bool fieldsEnclosedInQuotes = true)
        {
            string combine(string[] values)
            {
                if (fieldsEnclosedInQuotes)
                {
                    for (int i = 0; i < values.Length; i++)
                    {
                        if (values[i].Contains(delimiter.ToString()))
                        {
                            values[i] = "\"" + values[i] + "\"";
                        }
                    }
                }
                return string.Join(delimiter.ToString(), values);
            }

            using (StreamWriter sw = new StreamWriter(filename))
            {
                ReturnToStart();

                sw.WriteLine(combine(Columns));

                while (!EndOfData)
                {
                    sw.WriteLine(combine(ReadARow()));
                }
            }
        }
    }
}

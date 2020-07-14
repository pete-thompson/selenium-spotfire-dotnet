using NotVisualBasic.FileIO;
using System;
using System.IO;

namespace Selenium.Spotfire
{
    public class TableDataFromDelimitedFile : TableData
    {
        /// <summary>
        /// Have we reached the end of the data?
        /// </summary>
        public override bool EndOfData
        {
            get
            {
                return OurParser.EndOfData;
            }
        }

        /// <summary>
        /// Read a row of data from the table.
        /// </summary>
        /// <returns></returns>
        public override string[] ReadARow()
        {
            return OurParser.ReadFields();
        }

        /// <summary>
        /// Start again, reading from the first row
        /// </summary>
        public override void ReturnToStart()
        {
            OpenTheFile();
            if (!OurParser.EndOfData)
            {
                // Skip column headers
                OurParser.ReadFields();
            }
        }

        private void OpenTheFile()
        {
            if (OurParser != null)
            {
                OurParser.Close();
                OurParser.Dispose();
                OurReader.Dispose();
                OurStream.Dispose();
            }
            OurStream = new FileStream(Filename, FileMode.Open, FileAccess.Read);
            OurReader = new StreamReader(OurStream);
            OurParser = new CsvTextFieldParser(OurReader);

            OurParser.SetDelimiter(Delimiter);
            OurParser.HasFieldsEnclosedInQuotes = HasFieldsEnclosedInQuotes;
        }

        /// <summary>
        /// Save the data to a file
        /// </summary>
        /// <param name="filename"></param>
        public override void SaveToFile(string filename, char delimiter = '\t', bool fieldsEnclosedInQuotes = true)
        {
            if (Delimiter == delimiter && HasFieldsEnclosedInQuotes == fieldsEnclosedInQuotes)
            {
                // We can improve performance for large datasets by simply copying the file
                File.Copy(Filename, filename);
            }
            else
            {
                base.SaveToFile(filename, delimiter, fieldsEnclosedInQuotes);
            }
        }

        private readonly string Filename;
        private readonly char Delimiter;
        private readonly bool HasFieldsEnclosedInQuotes;
        private FileStream OurStream;
        private TextReader OurReader;
        private CsvTextFieldParser OurParser;

        public TableDataFromDelimitedFile(string filename, char delimiter = '\t', bool hasFieldsEnclosedInQuotes = true)
        {
            Filename = filename;
            Delimiter = delimiter;
            HasFieldsEnclosedInQuotes = hasFieldsEnclosedInQuotes;
            OpenTheFile();
            if (!OurParser.EndOfData)
            {
                Columns = OurParser.ReadFields();
            }
            else
            {
                Columns = new string[] { };
            }
        }

        // Flag: Has Dispose already been called?
        private bool disposed;

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Close everything
                    OurParser.Dispose();
                    OurReader.Dispose();
                    OurStream.Dispose();
                }

                disposed = true;
                base.Dispose(disposing);
            }
        }

    }
}

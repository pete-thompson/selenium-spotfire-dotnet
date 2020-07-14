using NotVisualBasic.FileIO;
using System.IO;

namespace Selenium.Spotfire
{
    /// <summary>
    /// Implementation of TableData class when data has been downloaded into a text separated temporary file
    /// </summary>
    sealed internal class TableDataFromTemporaryFile : TableDataFromDelimitedFile
    {
        private readonly string TemporaryDirectory;

        private static string FindTemporaryFileInDirectory(string tempDirectory)
        {
            string[] csvFiles = Directory.GetFiles(tempDirectory, "*.csv");
            return csvFiles[0];
        }

        /// <summary>
        /// Construct the table using data stored in text separated format in a file
        /// </summary>
        /// <param name="filename"></param>
        internal TableDataFromTemporaryFile(string tempDirectory): base (FindTemporaryFileInDirectory(tempDirectory), '\t', true)
        {
            TemporaryDirectory = tempDirectory;
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
                base.Dispose(disposing);

                // Delete the temporary directory
                try
                {
                    // Delete the temporary folder
                    Directory.Delete(TemporaryDirectory, true);
                    // Check if the parent folder is now empty and we can delete it
                    if (Directory.GetParent(TemporaryDirectory).GetDirectories().Length == 0)
                    {
                        Directory.GetParent(TemporaryDirectory).Delete();
                    }

                }
                catch
                {
                    // ignore - temporary files will eventually get cleaned up anyway
                }

                disposed = true;
            }
        }
    }
}

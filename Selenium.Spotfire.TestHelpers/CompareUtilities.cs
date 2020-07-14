using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Selenium.Spotfire.TestHelpers
{
    public static class CompareUtilities
    {
        /// <summary>
        /// Compare two bitmaps for equality
        /// Bitmaps are considered to be equal if they contain the same image, ignoring any "blank" areas around the image
        /// The background colour is taken from the top left pixel in the first image - hence it is essential that the first point is background
        /// We also allow for tiny colour differences since 10.3 somehow creates an almost white left border on images
        /// </summary>
        /// <param name="image1"></param>
        /// <param name="image2"></param>
        /// <returns></returns>
        public static bool AreEqual(Bitmap image1, Bitmap image2)
        {
            return DoComparison(image1, image2, Color.Red, true);
        }

        /// <summary>
        /// Generate images that highlights the differences between two images
        /// Uses the same algorithm as the comparison check
        /// </summary>
        /// <param name="image1">First image - will be modified to highlight differences</param>
        /// <param name="image2">Second image - will be modified to highlight differences</param>
        /// <param name="highlightColor">Color to use to show differences. Defaults to Red</param>
        /// <returns>Boolean indicating if images are equal</returns>
        public static bool GenerateImageDifference(Bitmap image1, Bitmap image2, Color? highlightColor = null)
        {
            return DoComparison(image1, image2, highlightColor ?? Color.Red, false);
        }

        /// <summary>
        /// Compare images and/or generate differences
        /// </summary>
        private static bool DoComparison(Bitmap image1, Bitmap image2, Color highlightColor, bool justCompare)
        { 
            bool imagesEqual = true;
            Bitmap[] bitmap = new Bitmap[2];
            bitmap[0] = image1;
            bitmap[1] = image2;

            if (!bitmap[0].PixelFormat.Equals(bitmap[1].PixelFormat))
            {
                imagesEqual = false;
            }
            else
            {
                int pixelBytes = Image.GetPixelFormatSize(bitmap[0].PixelFormat) / 8;
                int[] byteCount = new int[2];
                int[] bytesPerRow = new int[2];
                byte[][] bytes = new byte[2][];
                BitmapData[] bitmapData = new BitmapData[2];

                // Extract the byte data from the images
                for (int imageNumber = 0; imageNumber < 2; imageNumber++)
                {
                    byteCount[imageNumber] = bitmap[imageNumber].Width * bitmap[imageNumber].Height * pixelBytes;
                    bytesPerRow[imageNumber] = bitmap[imageNumber].Width * pixelBytes;
                    bytes[imageNumber] = new byte[byteCount[imageNumber]];
                    bitmapData[imageNumber] = bitmap[imageNumber].LockBits(new Rectangle(0, 0, bitmap[imageNumber].Width, bitmap[imageNumber].Height),
                        ImageLockMode.ReadOnly, 
                        bitmap[imageNumber].PixelFormat);
                    Marshal.Copy(bitmapData[imageNumber].Scan0, bytes[imageNumber], 0, byteCount[imageNumber]);
                    bitmap[imageNumber].UnlockBits(bitmapData[imageNumber]);
                }

                // Grab top left pixel of first image and treat it as background colour
                byte[] background = new byte[pixelBytes];
                Array.Copy(bytes[0], background, pixelBytes);

                // Find the point at which the background ends
                int[] offsetRow = new int[2];
                int[] offsetColumn = new int[2];
                ScanBackground(bitmap, pixelBytes, background, bytes, byteCount, offsetRow, offsetColumn);

                // Now compare the rest of the images to see if they're the same
                imagesEqual = ActualImagesEqual(bitmap, pixelBytes, background, bytes, bytesPerRow, offsetRow, offsetColumn, highlightColor, justCompare);
            }

            return imagesEqual;
        }

        /// <summary>
        /// Do we accept that the two values are close enough?
        /// </summary>
        private static bool ByteNearEnough(byte byte0, byte byte1)
        {
            return Math.Abs(byte0 - byte1) <= 1;
        }

        /// <summary>
        /// Find the point at which the background ends
        /// </summary>
        private static void ScanBackground(Bitmap[] bitmap, int pixelBytes, byte[] background, byte[][] bytes, int[] byteCount, int[] offsetRow, int[] offsetColumn)
        {
            // Now scan until we find something that isn't background
            for (int imageNumber = 0; imageNumber < 2; imageNumber++)
            {
                int offset;
                bool isBackground = true;
                for (offset = 0; offset < byteCount[imageNumber] && isBackground; offset += pixelBytes)
                {
                    for (int n = 0; n < pixelBytes && isBackground; n++)
                    {
                        isBackground = ByteNearEnough(bytes[imageNumber][offset + n],background[n]);
                    }
                }

                if (!isBackground)
                {
                    offset -= pixelBytes;
                }
                offsetRow[imageNumber] = offset / pixelBytes / bitmap[imageNumber].Width;
                offsetColumn[imageNumber] = (offset / pixelBytes) % bitmap[imageNumber].Width;
            }
        }

        /// <summary>
        /// Set pixel colours if we're generating the difference bitmaps
        /// </summary>
        private static void SetPixelColor(Bitmap bitmap, int x, int y, Color highlightColor, bool justCompare, bool bytesMatch, int byteNumber)
        {
            if (!justCompare)
            {
                if (!bytesMatch)
                {
                    bitmap.SetPixel(x, y, highlightColor);
                }
                else if (byteNumber == 0)
                {
                    // Set to a transparent version - we might overwrite with red later
                    bitmap.SetPixel(x, y, Color.FromArgb(20, bitmap.GetPixel(x, y)));
                }
                else
                {
                    // nothing to do - we set the pixel transparent initially and don't want to accidentally overwrite any highlighted updates
                }
            }
        }

        /// <summary>
        /// Do the work of the actual comparison
        /// </summary>
#pragma warning disable S107 // Too many parameters
        private static bool ActualImagesEqual(Bitmap[] bitmap, int pixelBytes, byte[] background, byte[][] bytes, int[] bytesPerRow, int[] offsetRow, int[] offsetColumn,
            Color highlightColor, bool justCompare)
#pragma warning restore S107 // Too many parameters
        {
            bool imagesEqual = true;

            int widest = Math.Max(bitmap[0].Width, bitmap[1].Width);
            int horizShift = (offsetColumn[1] - offsetColumn[0]) * pixelBytes;
            int vertShift = offsetRow[1] - offsetRow[0];
            int lastRow = Math.Max(bitmap[0].Height, bitmap[1].Height - vertShift);
            for (int rowNumber = offsetRow[0]; rowNumber < lastRow && (imagesEqual || !justCompare); rowNumber++)
            {
                for (int colByte = rowNumber==offsetRow[0] ? -horizShift : 0; colByte < widest * pixelBytes && (imagesEqual || !justCompare) ; colByte++)
                {
                    int comparible = colByte + horizShift;
                    bool outsideFirst = (colByte < 0) || (colByte >= bitmap[0].Width * pixelBytes) || (rowNumber >= bitmap[0].Height);
                    bool outsideSecond = (comparible < 0) || (comparible >= bitmap[1].Width * pixelBytes) || (rowNumber + vertShift >= bitmap[1].Height);
                    bool bytesMatch = true;

                    if (outsideFirst && outsideSecond)
                    {
                        // Outside both images, move on...
                    }
                    else if (outsideFirst)
                    {
                        // We're outside the first image - check for background on second one (account for colByte possibly being negative)
                        bytesMatch = ByteNearEnough(bytes[1][(rowNumber + vertShift) * bytesPerRow[1] + comparible], background[((colByte % pixelBytes) + pixelBytes) % pixelBytes]);
                        SetPixelColor(bitmap[1], comparible / pixelBytes, rowNumber + vertShift, highlightColor, justCompare, bytesMatch, colByte % pixelBytes);
                    }
                    else if (outsideSecond)
                    {
                        // we're outside the second image - check for background
                        bytesMatch = ByteNearEnough(bytes[0][rowNumber * bytesPerRow[0] + colByte], background[colByte % pixelBytes]);
                        SetPixelColor(bitmap[0], colByte / pixelBytes, rowNumber, highlightColor, justCompare, bytesMatch, colByte % pixelBytes);
                    }
                    else
                    {
                        // Check images match
                        bytesMatch = ByteNearEnough(bytes[0][rowNumber * bytesPerRow[0] + colByte], bytes[1][(rowNumber + vertShift) * bytesPerRow[1] + comparible]);
                        SetPixelColor(bitmap[0], colByte / pixelBytes, rowNumber, highlightColor, justCompare, bytesMatch, colByte % pixelBytes);
                        SetPixelColor(bitmap[1], comparible / pixelBytes, rowNumber + vertShift, highlightColor, justCompare, bytesMatch, colByte % pixelBytes);
                    }
                    imagesEqual = imagesEqual && bytesMatch;
                }
            }
            return imagesEqual;
        }

        /// <summary>
        /// Check if two tables contain the same data
        /// </summary>
        /// <param name="table1"></param>
        /// <param name="table2"></param>
        /// <returns></returns>
        public static bool AreEqual(TableData table1, TableData table2)
        {
            bool tablesEqual = true;

            if (table1 == table2)
            {
                tablesEqual = true;
            }
            else if (table1 == null || table2 == null)
            {
                tablesEqual = false;
            }
            else if (table1.Columns.Length != table2.Columns.Length)
            {
                tablesEqual = false;
            }
            else
            {
                table1.ReturnToStart();
                table2.ReturnToStart();

                for (int i = 0; i < table1.Columns.Length && tablesEqual; i++)
                {
                    tablesEqual = table1.Columns[i] == table2.Columns[i];
                }
                while (!table1.EndOfData && !table2.EndOfData && tablesEqual)
                {
                    string[] oldRow = table1.ReadARow();
                    string[] newRow = table2.ReadARow();

                    for (int i = 0; i < table1.Columns.Length && tablesEqual; i++)
                    {
                        tablesEqual = oldRow[i] == newRow[i];
                    }
                }

                if (table1.EndOfData != table2.EndOfData)
                {
                    tablesEqual = false;
                }
            }
            return tablesEqual;
        }
    }
}

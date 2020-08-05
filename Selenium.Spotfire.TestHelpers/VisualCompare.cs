using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using Selenium.Spotfire;

namespace Selenium.Spotfire.TestHelpers
{
    public static class VisualCompare
    {
        /// <summary>
        /// Compare the image content of a visual against files that have previously been captured containing matching images
        /// </summary>
        /// <param name="visual">The Visual to compare</param>
        /// <param name="imagesFolder">The folder containing previously saved image files to compare against</param>
        /// <param name="imagesFilePrefix">The filename prefix to search within the folder</param>
        /// <param name="imagesComparisons">A dictionary to receive a set of comparisons showing differences between the visual and the saved images</param>
        /// <returns>A boolean indicating if any of the files match the visual image</returns>
        public static bool CompareVisualImages(Visual visual, string imagesFolder, string imageFilePrefix, Dictionary<string, Bitmap> imageComparisons)
        {
            bool anyMatch = false;
            foreach (string filename in Directory.GetFiles(imagesFolder, string.Format("{0}-*.png", imageFilePrefix)))
            {
                Bitmap expectedImage = new Bitmap(filename);

                visual.ResizeContent(expectedImage.Size);
                Bitmap difference = visual.GetImage();

                bool thisMatch = CompareUtilities.GenerateImageDifference(expectedImage, difference);
                anyMatch = anyMatch || thisMatch;
                if (!thisMatch)
                {
                    Regex pattern = new Regex(@"([^-]*)\.png$");
                    Match match = pattern.Match(filename);
                    imageComparisons.Add("difference vs. " + match.Groups[1].Value + ".png", difference);
                }
            }
            if (!anyMatch) {
                imageComparisons.Add(".png", visual.GetImage());
            }

            return anyMatch;
        }
    }
}
using System.Net;

namespace Aero_Console_Sharp
{
    class Commons
    {
        public static float GetDisplayScale()
        {
            // Get the primary screen
            Screen primaryScreen = Screen.PrimaryScreen;

            // Retrieve the DPI (dots per inch)
            float dpiX = primaryScreen.Bounds.Width / primaryScreen.WorkingArea.Width;
            float dpiY = primaryScreen.Bounds.Height / primaryScreen.WorkingArea.Height;

            // Calculate the display magnification level
            float magnificationX = dpiX / 96.0f * 100.0f;
            float magnificationY = dpiY / 96.0f * 100.0f;

            // Use the average magnification level for both X and Y axes
            float averageMagnification = (magnificationX + magnificationY) / 2.0f;

            //return averageMagnification;
            return 1;
        }

        public static void DownloadFile(string url, string destinationPath)
        {
            string directory = Path.GetDirectoryName(destinationPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (WebClient client = new WebClient())
            {
                client.DownloadFile(url, destinationPath);
            }
        }

        public static string AddExportSuffix(string filename, string suffix)
        {
            // Check if the filename has an extension
            if (Path.HasExtension(filename))
            {
                // Insert "_exported" before the extension
                string exportedFilename = Path.Combine(
                    Path.GetDirectoryName(filename),
                    Path.GetFileNameWithoutExtension(filename) + suffix + Path.GetExtension(filename)
                );
                return exportedFilename;
            }
            else
            {
                // If the filename has no extension, just add "_exported"
                return filename + suffix;
            }
        }


        public static string GetFileFilter(string[] extensions)
        {
            // Create a list of file filters
            List<string> filters = new List<string>();
            // Add a filter for each extension
            foreach (string extension in extensions)
            {
                filters.Add(extension.ToUpper() + " files (*." + extension + ")|*." + extension);
            }
            // Combine all filters into a single string
            string filter = string.Join("|", filters);
            // Add an "All files" filter
            filter += "|All files (*.*)|*.*";
            return filter;
        }

        public static IEnumerable<System.Windows.Forms.Control> GetAllControls(System.Windows.Forms.Control control)
        {
            foreach (System.Windows.Forms.Control childControl in control.Controls)
            {
                // Return the child control itself
                yield return childControl;

                // Recursively get all child controls of the child control
                foreach (System.Windows.Forms.Control grandchildControl in GetAllControls(childControl))
                {
                    yield return grandchildControl;
                }
            }
        }

        public static bool IsFileLocked(string filePath)
        {
            if (!File.Exists(filePath))
                return false;
            try
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                // The file is locked by another program
                return true;
            }

            // The file is not locked
            return false;
        }

        public static System.Windows.Forms.Label AddLabelToTable(string text, TableLayoutPanel table, int column, int row, int columnspan, DockStyle dockStyle, ContentAlignment contentAlignment)
        {
            var lbl = new System.Windows.Forms.Label
            {
                Text = text,
                Dock = dockStyle,
                TextAlign = contentAlignment,
                Margin = new System.Windows.Forms.Padding(0)

            };

            table.Controls.Add(lbl, column, row);
            table.SetColumnSpan(lbl, columnspan);

            return lbl;
        }

        public static System.Windows.Forms.Button AddButtonToTable(string text, TableLayoutPanel table, int column, int row, int columnspan, DockStyle dockStyle, ContentAlignment contentAlignment)
        {
            var btn = new System.Windows.Forms.Button
            {
                Text = text,
                Dock = dockStyle,
                TextAlign = contentAlignment,
                Margin = new System.Windows.Forms.Padding(0)

            };

            table.Controls.Add(btn, column, row);
            table.SetColumnSpan(btn, columnspan);

            return btn;
        }


    }
}

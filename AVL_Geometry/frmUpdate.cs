using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AERO_Console
{

    public partial class frmUpdate
    {

        // Configuration
        private const string GITHUB_BASE_URL = "https://github.com/drarahimi/AERO_Console/releases/latest";
        private const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AERO_Console_Updater";

        // State
        private string _downloadPath;
        private HttpClient _httpClient;

        public frmUpdate()
        {
            InitializeComponent();
            // Initialize HttpClient once to prevent socket exhaustion
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
        }

        private async void frmUpdate_Load(object sender, EventArgs e)
        {
            // 1. Setup UI and Paths
            Icon = My.MyProject.Forms.frmMain.Icon;
            _downloadPath = My.MyProject.Forms.frmMain.updatedpath;

            var theme = UI.Theme.Current;
            UI.Theme.UseImmersiveDarkMode(Handle, theme.IsDark);
            theme.ApplyTo(this);
            theme.StyleCard(pnlCard, UI.Theme.SpacingMd);
            theme.StyleButton(btnClose);
            Label2.ForeColor = theme.Accent;

            // 2. Clean up old updates if they exist
            CleanUpOldFiles(_downloadPath);

            // 3. Start the process
            await CheckAndDownloadUpdateAsync();
        }

        private async Task CheckAndDownloadUpdateAsync()
        {
            try
            {
                lblStat.Text = "Checking for updates...";

                // --- STEP 1: Get Redirect URL (Latest Version) ---
                var response = await _httpClient.GetAsync(GITHUB_BASE_URL, HttpCompletionOption.ResponseHeadersRead);
                string finalUrl = response.RequestMessage.RequestUri.ToString();

                // Extract version tag (assuming url ends in /tag/v1.2.3)
                string latestTag = finalUrl.Split('/').Last();
                // Remove 'v' if present for cleaner parsing (e.g. v1.0 -> 1.0)
                string cleanVersionStr = latestTag.Replace("v", "").Trim();

                Version serverVersion = null;
                var localVersion = My.MyProject.Application.Info.Version;

                // Safely parse version
                Version.TryParse(cleanVersionStr, out serverVersion);

                Debug.WriteLine($"Local: {localVersion} | Server: {serverVersion}");

                // --- STEP 2: Compare Versions ---
                bool isUpdateAvailable = serverVersion is not null && serverVersion > localVersion;

                lblStat.Text = $"Local: {localVersion} | Server: {serverVersion}" + Environment.NewLine + (isUpdateAvailable ? "Update found!" : "You are up to date.");

                await Task.Delay(1500); // Short pause for user readability

                if (!isUpdateAvailable)
                {
                    CloseFormSafe();
                    return;
                }

                // --- STEP 3: Download ---
                // Construct the direct download link based on your logic: replace 'tag' with 'download' + filename
                string downloadUrl = finalUrl.Replace("tag", "download") + "/AERO_Console.exe";

                lblStat.Text = "Downloading update...";
                var progressIndicator = new Progress<int>(p =>
                {
                    lblStat.Text = $"Downloading... {p}%";
                    if (pbUpdate is not null) pbUpdate.Value = Math.Min(100, Math.Max(0, p));
                });

                await DownloadFileAsync(downloadUrl, _downloadPath, progressIndicator);

                // --- STEP 4: Apply Update ---
                lblStat.Text = "Download complete. Restarting...";
                await Task.Delay(1000);

                // Hide file attribute if needed
                var fi = new FileInfo(_downloadPath);
                fi.Attributes = FileAttributes.Hidden;

                My.MySettingsProperty.Settings.appUpdateNeeded = true;
                My.MySettingsProperty.Settings.Save();

                My.MyProject.Forms.frmMain.Close(); // Triggers app shutdown logic
            }

            catch (Exception ex)
            {
                lblStat.Text = "Error: " + ex.Message;
                Debug.WriteLine("Update Error: " + ex.ToString());
                // Await Task.Delay(3000)
                CloseFormSafe();
            }
        }

        /// <summary>
    /// Downloads a file with progress reporting using HttpClient.
    /// </summary>
        private async Task DownloadFileAsync(string url, string destinationPath, IProgress<int> progress)
        {
            using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength.GetValueOrDefault(-1L);
                bool canReportProgress = totalBytes != -1;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {

                    long totalRead = 0L;
                    var buffer = new byte[8192];
                    bool isMoreToRead = true;

                    do
                    {
                        int read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                        if (read == 0)
                        {
                            isMoreToRead = false;
                        }
                        else
                        {
                            await fileStream.WriteAsync(buffer, 0, read);
                            totalRead += read;
                            if (canReportProgress)
                            {
                                progress.Report((int)Math.Round(totalRead * 100L / (double)totalBytes));
                            }
                        }
                    }
                    while (isMoreToRead);
                }
            }
        }

        private void CleanUpOldFiles(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Could not delete old temporary file: " + ex.Message);
                }
            }
        }

        private void CloseFormSafe()
        {
            if (!IsDisposed)
                Close();
        }

        /// <summary>
    /// Recursively sets fonts, cleaned up for recursion efficiency.
    /// </summary>
        public static void ApplyFontRecursive(Control.ControlCollection ctrls, Font font)
        {
            foreach (Control ctrl in ctrls)
            {
                ctrl.Font = font;
                if (ctrl.HasChildren)
                {
                    ApplyFontRecursive(ctrl.Controls, font);
                }
            }
        }

        // Cleanup HttpClient when form closes
        private void frmUpdate_FormClosed(object sender, FormClosedEventArgs e)
        {
            _httpClient?.Dispose();
        }

    }
}
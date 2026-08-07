using System;
using System.Collections.Generic;

using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace AERO_Console
{

    public partial class frmMain
    {
        public Process p;
        private bool Leaving;
        private string logtext = "";
        private Thread bt;
        public string updatedpath = Path.Combine(Application.StartupPath, "update.exe");
        public string originalpath = Path.Combine(Application.StartupPath, "AERO_Console.exe");
        public bool appUpdateNeeded = false;
        public bool appUpdated = false;
        public string curApp = "avl";
        public bool firstLoad = false;
        public static Font systemFont = new Font("Consolas", 12f);
        public FileSystemWatcher fw = new FileSystemWatcher();
        private readonly ToolTip _tt = new ToolTip();
        // Starts blank, not "test" - SetPlaceholder() deliberately bypasses txtName_TextChanged
        // while showing the placeholder (so it doesn't stomp a real name mid-sync), which means
        // this field is never touched by that path. A hardcoded "test" default left a fresh,
        // untouched window internally thinking a project named "test" was active - showing the
        // "using default 'test' project" warning - even though the visible box was just the
        // empty placeholder. See the same fix on frmGeometry's projectName.
        public string projectName = "";
        public static bool IsSyncingProject = false;
        private ToolStripComboBox cbEngine = null;
        private ToolStripButton btnClosePlot = null;
        private readonly System.Text.StringBuilder _logBuffer = new System.Text.StringBuilder();
        private readonly object _logBufferLock = new object();
        private System.Windows.Forms.Timer _logFlushTimer;
        private const int DESKTOPVERTRES = 0x75;
        private const int DESKTOPHORZRES = 0x76;

        public frmMain()
        {
            _logFlushTimer = new System.Windows.Forms.Timer() { Interval = 75 };
            _logFlushTimer.Tick += _logFlushTimer_Tick; // VB "Handles _logFlushTimer.Tick" — not auto-wired by the converter
            InitializeComponent();
        }
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(nint hdc, int nIndex);
        private void ReadThread()
        {
            try
            {
                var buffer = new char[4097];
                while (!Leaving && !(p is null) && !p.HasExited)
                {
                    int bytesRead = p.StandardOutput.Read(buffer, 0, buffer.Length);
                    if (bytesRead <= 0)
                        break;

                    string textChunk = new string(buffer, 0, bytesRead);

                    lock (_logBufferLock)
                        _logBuffer.Append(textChunk);
                }
            }
            catch
            {
                // Process exited or stream closed
            }
        }

        private void _logFlushTimer_Tick(object sender, EventArgs e)
        {
            string pending = null;
            lock (_logBufferLock)
            {
                if (_logBuffer.Length > 0)
                {
                    pending = _logBuffer.ToString();
                    _logBuffer.Clear();
                }
            }

            if (pending is null)
                return;

            // txtLog is purely read-only output - command entry lives in txtCommand
            // (a separate control) precisely so a flood of output here (e.g. an XFOIL
            // "aseq" polar sweep) can never block or corrupt the user's ability to type
            // and submit the next command.
            txtLog.AppendText(pending);
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }
        public void loadConsole()
        {
            // 1. Cleanup old process if it exists
            if (p is not null)
            {
                try
                {
                    if (!p.HasExited)
                        p.Kill();
                    p.Dispose();
                }
                catch
                {
                }
                p = null;
            }

            // 2. Setup New Process
            p = new Process();
            var startinfo = new ProcessStartInfo();
            string appPath = "";

            if (curApp.ToLower() == "avl")
            {
                appPath = Path.Combine(Application.StartupPath, "appdata", "avl.exe");
            }
            else if (curApp.ToLower() == "xfoil")
            {
                appPath = Path.Combine(Application.StartupPath, "appdata", "xfoil.exe");
            }

            startinfo.FileName = appPath;
            startinfo.Arguments = "";
            startinfo.WorkingDirectory = Application.StartupPath;
            startinfo.RedirectStandardError = false;
            startinfo.RedirectStandardOutput = true;
            startinfo.RedirectStandardInput = true;
            startinfo.UseShellExecute = false;
            startinfo.CreateNoWindow = true;
            startinfo.EnvironmentVariables["GFORTRAN_UNBUFFERED_ALL"] = "y";
            startinfo.EnvironmentVariables["GFORTRAN_UNBUFFERED_PRECONNECTED"] = "y";

            p.StartInfo = startinfo;
            p.EnableRaisingEvents = true;

            // 3. Start the Process
            p.Start();
            if (lblStatus is not null)
            {
                lblStatus.Text = $"Status: Running {curApp.ToUpper()}";
            }

            if (p.HasExited)
            {
                AppMessageBox.Show("Process exited immediately! Exit Code: " + p.ExitCode);
                // If this pops up, it means the EXE path is wrong,
                // or it's blocked by antivirus, or missing a DLL.
            }

            // 4. Start the Thread (No need to abort the old one, it died when we did p.Kill)
            bt = new Thread(ReadThread);
            bt.IsBackground = true;
            bt.Start();

            txtLog.Clear();
            txtCommand.Clear();
            txtCommand.Focus();

        }



        public void findAVLs(string path)
        {
            // 1. Thread Safety: Ensure this runs on the UI thread
            if (InvokeRequired)
            {
                Invoke(() => findAVLs(path));
                return;
            }

            // 2. Get the new list of files
            string[] files = Directory.GetFiles(path, "*.avl", SearchOption.TopDirectoryOnly);
            var newItems = new List<string>();
            foreach (string FileName in files)
                newItems.Add(Path.GetFileNameWithoutExtension(FileName));

            // 3. Define a helper action to update any given control
            // (This allows us to use the exact same logic for 'Me' and other forms)
            Action<object> updateControl = ctrl =>
        {
            // Change 'ComboBox' to 'ListBox' if your control is a ListBox
            ToolStripComboBox box = ctrl as ToolStripComboBox;
            if (box is null)
                return;

            // A. Check if the items in the ComboBox are already identical to newItems
            bool itemsChanged = false;
            if (box.Items.Count != newItems.Count)
            {
                itemsChanged = true;
            }
            else
            {
                for (int i = 0, loopTo = newItems.Count - 1; i <= loopTo; i++)
                {
                    if (!box.Items[i].ToString().Equals(newItems[i]))
                    {
                        itemsChanged = true;
                        break;
                    }
                }
            }

            // If items are unchanged, do nothing (completely prevents redraw/flicker)
            if (!itemsChanged)
                return;

            // B. Save the current selection
            string currentSelection = box.Text;

            // C. Update the list (using ComboBox.BeginUpdate/EndUpdate avoids drawing artifacts)
            box.ComboBox.BeginUpdate();
            box.Items.Clear();
            foreach (var item in newItems)
                box.Items.Add(item);

            // D. Restore selection only if the file still exists
            if (box.Items.Contains(currentSelection))
            {
                box.Text = currentSelection;
            }
            else
            {
                // Optional: Select the first item or clear selection
                box.SelectedIndex = -1;
            }
            box.ComboBox.EndUpdate();
        };

            // 4. Update the control on THIS form
            updateControl(txtName);

            // 5. Find and update 'txtName' on other open frmGeometry forms
            foreach (Form frm in Application.OpenForms)
            {
                // Check if the form is frmGeometry (and not the one we just updated)
                if (frm is frmGeometry && !ReferenceEquals(frm, this))
                {
                    frmGeometry geoForm = (frmGeometry)frm;
                    // Pass the other form's txtName control to our helper
                    updateControl(geoForm.txtName);
                }
            }
        }

        // ===================== Drag-and-drop file import =====================
        // Lets users drop .avl/.mass/.run/airfoil files, whole folders, or .zip
        // archives onto the main window; everything gets flattened (no subfolder
        // structure preserved) and copied straight into Application.StartupPath,
        // which is where the app actually looks for project files. The existing
        // FileSystemWatcher (fw) already picks up newly-created .avl/.mass/.run
        // files and refreshes the project dropdown, so no manual refresh is needed.

        // Runtime files that must never be clobbered by a careless drop (e.g.
        // someone dragging in a whole folder that happens to contain other stuff).
        private static readonly string[] DropImportExcludedExtensions = new[] { ".exe", ".dll", ".pdb", ".config", ".json", ".manifest" };

        private void InitializeDropZone()
        {
            var pnlDropZone = new Panel();
            pnlDropZone.Dock = DockStyle.Fill;
            pnlDropZone.BackColor = Color.FromArgb(45, 45, 48);
            pnlDropZone.BorderStyle = BorderStyle.FixedSingle;
            pnlDropZone.AllowDrop = true;
            pnlDropZone.Margin = new Padding(0, 5, 0, 0);

            var lblDropHint = new Label();
            lblDropHint.Text = "Drop .avl / .mass / .run / airfoil files, folders, or .zip archives here to copy them into the project folder";
            lblDropHint.Dock = DockStyle.Fill;
            lblDropHint.TextAlign = ContentAlignment.MiddleCenter;
            lblDropHint.ForeColor = Color.Gainsboro;
            lblDropHint.AllowDrop = true;
            pnlDropZone.Controls.Add(lblDropHint);

            pnlDropZone.DragEnter += DropZone_DragEnter;
            pnlDropZone.DragDrop += DropZone_DragDrop;
            lblDropHint.DragEnter += DropZone_DragEnter;
            lblDropHint.DragDrop += DropZone_DragDrop;

            // Added as a new row of the EXISTING LayoutTable (rather than a
            // Dock=Bottom sibling of it) so there's no docking-order ambiguity
            // between this and StatusStrip1 - a table's row layout is unambiguous.
            // Row 0 (txtLog, Percent 100) and row 1 (txtCommand, Absolute) are
            // defined in the designer; this adds row 2 for the drop zone.
            LayoutTable.RowCount = 3;
            LayoutTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 56.0f));
            LayoutTable.Controls.Add(pnlDropZone, 0, 2);
        }

        private void DropZone_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void DropZone_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;
            string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths is null || paths.Length == 0)
                return;
            ImportDroppedPaths(paths);
        }

        private void ImportDroppedPaths(string[] paths)
        {
            string destDir = Application.StartupPath;
            var tempExtractDirs = new List<string>();
            var toCopy = new List<Tuple<string, string>>();   // (sourceFile, destFileName)
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (var droppedPath in paths)
                {
                    try
                    {
                        if (Directory.Exists(droppedPath))
                        {
                            foreach (var f in Directory.GetFiles(droppedPath, "*", SearchOption.AllDirectories))
                                AddDropCandidate(f, toCopy, usedNames);
                        }
                        else if (File.Exists(droppedPath))
                        {
                            if (Path.GetExtension(droppedPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                string tempDir = Path.Combine(Path.GetTempPath(), "avlimport_" + Path.GetRandomFileName());
                                Directory.CreateDirectory(tempDir);
                                tempExtractDirs.Add(tempDir);
                                ZipFile.ExtractToDirectory(droppedPath, tempDir);
                                foreach (var f in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
                                    AddDropCandidate(f, toCopy, usedNames);
                            }
                            else
                            {
                                AddDropCandidate(droppedPath, toCopy, usedNames);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AppMessageBox.Show($"Could not read \"{droppedPath}\":" + Constants.vbCrLf + ex.Message, "Import Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (toCopy.Count == 0)
                {
                    AppMessageBox.Show("No usable files were found in what was dropped.", "Import Files", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int existingCount = 0;
                foreach (var t in toCopy)
                {
                    if (File.Exists(Path.Combine(destDir, t.Item2)))
                        existingCount += 1;
                }

                string msg = $"{toCopy.Count} file(s) will be copied to:" + Constants.vbCrLf + destDir;
                if (existingCount > 0)
                {
                    msg += Constants.vbCrLf + Constants.vbCrLf + $"{existingCount} file(s) already exist there and will be overwritten.";
                }
                msg += Constants.vbCrLf + Constants.vbCrLf + "Continue?";
                if (AppMessageBox.Show(msg, "Import Files", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    return;

                int copied = 0;
                var errors = new List<string>();
                foreach (var t in toCopy)
                {
                    try
                    {
                        File.Copy(t.Item1, Path.Combine(destDir, t.Item2), true);
                        copied += 1;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{t.Item2}: {ex.Message}");
                    }
                }

                string summary = $"Copied {copied} of {toCopy.Count} file(s) to:" + Constants.vbCrLf + destDir;
                if (errors.Count > 0)
                {
                    summary += Constants.vbCrLf + Constants.vbCrLf + "Errors:" + Constants.vbCrLf + string.Join(Constants.vbCrLf, errors);
                }
                AppMessageBox.Show(summary, "Import Files", MessageBoxButtons.OK, errors.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            finally
            {
                foreach (var d in tempExtractDirs)
                {
                    try
                    {
                        Directory.Delete(d, true);
                    }
                    catch
                    {
                    }
                }
            }
        }

        // Filters out runtime/junk files and flattens the destination name,
        // auto-disambiguating (name_2.ext, name_3.ext, ...) if two different
        // source files in the SAME drop would otherwise land on the same name.
        private void AddDropCandidate(string f, List<Tuple<string, string>> toCopy, HashSet<string> usedNames)
        {
            string name = Path.GetFileName(f);
            if (name.StartsWith("._") || name.Equals(".DS_Store", StringComparison.OrdinalIgnoreCase))
                return;
            if (f.Contains("__MACOSX"))
                return;

            string ext = Path.GetExtension(f).ToLowerInvariant();
            if (Array.IndexOf(DropImportExcludedExtensions, ext) >= 0)
                return;

            string destName = name;
            if (usedNames.Contains(destName))
            {
                string baseName = Path.GetFileNameWithoutExtension(name);
                int n = 2;
                while (usedNames.Contains($"{baseName}_{n}{ext}"))
                    n += 1;
                destName = $"{baseName}_{n}{ext}";
            }
            usedNames.Add(destName);
            toCopy.Add(Tuple.Create(f, destName));
        }

        private void frmMain_Load(object sender, EventArgs e)
        {

            _tt.SetToolTip(txtLog, "Live console output from the running AVL/XFOIL process");

            _logFlushTimer.Start();

            if (Application.ExecutablePath.EndsWith("update.exe"))
            {

                File.Copy(updatedpath, originalpath, true);
                var fi = new FileInfo(originalpath);
                fi.Attributes = FileAttributes.Normal;
                My.MySettingsProperty.Settings.appUpdated = true;
                My.MySettingsProperty.Settings.Save();
                Close();
            }
            else
            {
                try
                {
                    File.Delete(updatedpath);
                }
                catch
                {
                }
            }


            // Dim FileVer As String = Assembly.LoadFrom(Application.ExecutablePath).GetName.Version.ToString 'FileVersionInfo.GetVersionInfo(Application.ExecutablePath).FileVersion  'FileVersionInfo.GetVersionInfo(Application.ExecutablePath).SpecialBuild
            Text = My.MyProject.Application.Info.AssemblyName.ToString().Replace("_", " ") + " - " + My.MyProject.Application.Info.Version.ToString();

            Width = (int)Math.Round(Screen.PrimaryScreen.WorkingArea.Width * 0.8d);
            Height = (int)Math.Round(Screen.PrimaryScreen.WorkingArea.Height * 0.8d);
            Left = (int)Math.Round((Screen.PrimaryScreen.WorkingArea.Width - Width) / 2d);
            Top = (int)Math.Round((Screen.PrimaryScreen.WorkingArea.Height - Height) / 2d);

            // 1. Configure the Watcher
            fw.Path = Application.StartupPath;
            fw.NotifyFilter = NotifyFilters.FileName; // We only care if files are added/removed/renamed
            fw.Filter = "*.*"; // Watch all files, we will filter specific extensions in the code below
            fw.EnableRaisingEvents = true;

            // 2. Add Handlers for all relevant events
            // Note: We point them all to the same update logic for simplicity
            fw.Created += OnFileChanged;
            fw.Deleted += OnFileChanged;
            fw.Renamed += OnFileRenamed;

            try
            {
                var fi = new FileInfo(Path.Combine(Application.StartupPath, "appdata.zip"));
                byte[] b = My.Resources.Resources.appdata;
                if (fi.Exists)
                    fi.Delete();
                File.WriteAllBytes(fi.FullName, b);
                fi.Attributes = FileAttributes.Hidden;

                txtLog.Dock = DockStyle.Fill;

                string d = Path.Combine(Application.StartupPath, "appdata");
                if (File.Exists(fi.FullName))
                {
                    if (Directory.Exists(d))
                    {
                        Directory.Delete(d, true);
                    }
                    ZipFile.ExtractToDirectory(fi.FullName, d);
                    var DirectoryInfo = new DirectoryInfo(d);
                    DirectoryInfo.Attributes = FileAttributes.Hidden;
                    // Directory
                    // End If
                }
                fi.Delete();
                loadConsole();
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Error: " + ex.Message);
            }

            // Initialize engine selection dynamically
            var lblEngine = new ToolStripLabel("Engine:");
            lblEngine.ForeColor = Color.DimGray;

            cbEngine = new ToolStripComboBox("cbEngine");
            cbEngine.Items.AddRange(new object[] { "AVL", "XFOIL" });
            cbEngine.SelectedIndex = 0; // AVL by default
            cbEngine.DropDownStyle = ComboBoxStyle.DropDownList;
            cbEngine.SelectedIndexChanged += cbEngine_SelectedIndexChanged;

            // Find the index of btnGeometry to insert before it
            int insertIndex = ToolStrip2.Items.IndexOf(btnGeometry);
            if (insertIndex >= 0)
            {
                ToolStrip2.Items.Insert(insertIndex, lblEngine);
                ToolStrip2.Items.Insert(insertIndex + 1, cbEngine);
                ToolStrip2.Items.Insert(insertIndex + 2, new ToolStripSeparator());
            }
            else
            {
                ToolStrip2.Items.Add(lblEngine);
                ToolStrip2.Items.Add(cbEngine);
                ToolStrip2.Items.Add(new ToolStripSeparator());
            }

            // Initialize Close Plot button dynamically
            btnClosePlot = new ToolStripButton("Close Plot");
            btnClosePlot.Name = "btnClosePlot";
            btnClosePlot.ToolTipText = "Dismiss a stuck AVL/XFOIL graphics window (Trefftz Plane, geometry plot, root-locus, etc.)";
            btnClosePlot.Visible = false;
            btnClosePlot.Click += btnClosePlot_Click;

            int designerIndex = ToolStrip2.Items.IndexOf(btnDesigner);
            if (designerIndex >= 0)
            {
                ToolStrip2.Items.Insert(designerIndex, btnClosePlot);
            }
            else
            {
                ToolStrip2.Items.Add(btnClosePlot);
            }

            // Initialize warning label dynamically
            var lblWarning = new ToolStripLabel();
            lblWarning.Name = "lblWarning";
            lblWarning.ForeColor = Color.OrangeRed;
            lblWarning.Font = new Font(ToolStrip1.Font, FontStyle.Bold);
            lblWarning.Text = "⚠️ Warning: Using default 'test' project. Changes will be overwritten!";
            ToolStrip1.Items.Add(lblWarning);

            // Initialize placeholder behavior for txtName
            txtName.ComboBox.GotFocus += (s, ev) => ClearPlaceholder();
            txtName.ComboBox.LostFocus += (s, ev) => SetPlaceholder();
            SetPlaceholder();

            UpdateTitleAndButtons();
            UpdateProjectWarning();

            if (My.MySettingsProperty.Settings.appFont is not null)
            {
                systemFont = My.MySettingsProperty.Settings.appFont;
            }

            SetAllControlsFont(Controls, systemFont);
            firstLoad = true;
            findAVLs(Environment.CurrentDirectory);
            InitializeDropZone();

            // Enable double-buffering recursively on all controls (including toolbars) to prevent hover/draw flicker
            EnableDoubleBuffering(this);

            // Set ComboBox FlatStyle and double-buffering directly
            try
            {
                var dbProp = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dbProp is not null)
                {
                    if (txtName is not null && txtName.ComboBox is not null)
                    {
                        dbProp.SetValue(txtName.ComboBox, true, (object[])null);
                        txtName.ComboBox.FlatStyle = FlatStyle.Flat;
                    }
                    if (cbEngine is not null && cbEngine.ComboBox is not null)
                    {
                        dbProp.SetValue(cbEngine.ComboBox, true, (object[])null);
                        cbEngine.ComboBox.FlatStyle = FlatStyle.Flat;
                    }
                }
            }
            catch
            {
            }


            // Using g As Graphics = Graphics.FromHwnd(IntPtr.Zero)
            // Dim hdc As IntPtr = g.GetHdc
            // Dim TrueScreenSize As New Size(GetDeviceCaps(hdc, DESKTOPHORZRES), GetDeviceCaps(hdc, DESKTOPVERTRES))
            // Dim sclX As Single = CSng(Math.Round((TrueScreenSize.Width / Screen.PrimaryScreen.Bounds.Width), 2))
            // Dim sclY As Single = CSng(Math.Round((TrueScreenSize.Height / Screen.PrimaryScreen.Bounds.Height), 2))
            // g.ReleaseHdc(hdc)

            // 'show the true screen size
            // Dim DPIstr = "Screen Width:  " & TrueScreenSize.Width.ToString & vbLf &
            // "Screen Height: " & TrueScreenSize.Height.ToString & vbLf & vbLf &
            // "Scale X: " & sclX.ToString & vbLf &
            // "Scale Y: " & sclY.ToString
            // If (sclX <> 1 Or sclY <> 1) Then
            // AppMessageBox.Show($"Your displace scale factor is {sclX * 100}% in X and {sclY * 100}% in Y direction. Please note that your editor may experience display issues if your display scale factor is not at 100%. Please fix the scale factor before continuing." + vbNewLine + vbNewLine + "See this for help: https://bit.ly/3LzMotW")
            // End If

            // End Using

            // frmGeometry.Show()


        }

        /// <summary>
    /// Handles Created and Deleted events
    /// </summary>
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (IsTargetFile(e.FullPath))
            {
                RefreshFileList();
            }
        }

        /// <summary>
    /// Handles Renamed events. 
    /// We need to check if the file was renamed TO a target type OR FROM a target type.
    /// </summary>
        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            // Update if the old name WAS a target, or the new name IS a target
            if (IsTargetFile(e.FullPath) || IsTargetFile(e.OldFullPath))
            {
                RefreshFileList();
            }
        }

        /// <summary>
    /// Checks if the file ends in .avl, .mass, or .run
    /// </summary>
        private bool IsTargetFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            return ext == ".avl" || ext == ".mass" || ext == ".run";
        }

        /// <summary>
    /// Updates the UI safely from the background thread
    /// </summary>
        private void RefreshFileList()
        {
            // We must use Invoke because FileSystemWatcher runs on a different thread
            if (InvokeRequired)
            {
                Invoke(() => UpdateListBoxLogic());
            }
            else
            {
                UpdateListBoxLogic();
            }
        }

        /// <summary>
    /// The actual logic to re-read the directory and fill your list
    /// </summary>
        private void UpdateListBoxLogic()
        {
            findAVLs(Environment.CurrentDirectory);
            UpdateTitleAndButtons();
        }

        private void AirplaneDesignToolStripMenuItem_Click(object sender, EventArgs e)
        {
            My.MyProject.Forms.frmGeometry.WindowState = FormWindowState.Maximized;
            My.MyProject.Forms.frmGeometry.Show();
        }

        private void XfoilAnalysisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            My.MyProject.Forms.frmXfoilAnalysis.Show();
            My.MyProject.Forms.frmXfoilAnalysis.Activate();
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            My.MyProject.Forms.frmAbout.Show();
        }

        private void OpenCurrentDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Application.StartupPath);
        }

        /// <summary>
    /// Zips the bare-minimum set of files needed to launch AERO Console (the exe, its IL assembly,
    /// the third-party FastColoredTextBox dependency, and the .NET runtime config/deps files) and drops
    /// it on the Desktop, ready to attach to a GitHub Release. Deliberately an allow-list of runtime
    /// file types directly in the install folder (non-recursive) so debug symbols (.pdb), API doc XML,
    /// sample/working project files (test.avl/.mas/.run), the "appdata" runtime cache (rebuilt
    /// automatically from an embedded resource plus the XFOIL auto-downloader on first run), and stray
    /// build artifacts like the ClickOnce "publish" or self-contained "win-x64" subfolders never get swept in.
    /// </summary>
        private void PackageForReleaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var oldCursor = Cursor;
            try
            {
                string sourceDir = Application.StartupPath;
                string versionStr = My.MyProject.Application.Info.Version.ToString();
                string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string zipPath = Path.Combine(desktopDir, $"AERO_Console_v{versionStr}.zip");

                if (File.Exists(zipPath))
                {
                    var overwrite = AppMessageBox.Show($"\"{Path.GetFileName(zipPath)}\" already exists on the Desktop. Overwrite it?", "Package for GitHub Release", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (overwrite != DialogResult.Yes)
                        return;
                    File.Delete(zipPath);
                }

                // Only these runtime file types are needed to launch the app; everything else (debug symbols,
                // doc XML, sample project files, downloaded appdata, alternate build outputs) is left out.
                var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".exe", ".dll", ".json", ".config" };
                var excludedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "update.exe" };

                Cursor = Cursors.WaitCursor;
                lblStatus.Text = "Status: Packaging release zip...";
                Application.DoEvents();

                int fileCount = 0;
                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    foreach (var filePath in Directory.EnumerateFiles(sourceDir, "*", SearchOption.TopDirectoryOnly))
                    {
                        string fileName = Path.GetFileName(filePath);

                        if (!allowedExtensions.Contains(Path.GetExtension(filePath)))
                            continue;
                        if (excludedFileNames.Contains(fileName))
                            continue;

                        zip.CreateEntryFromFile(filePath, fileName, CompressionLevel.Optimal);
                        fileCount += 1;
                    }
                }

                Cursor = oldCursor;
                lblStatus.Text = "Status: Release package created.";

                var openFolder = AppMessageBox.Show($"Release package created ({fileCount} files):{Environment.NewLine}{zipPath}{Environment.NewLine}{Environment.NewLine}Open containing folder?", "Package for GitHub Release", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (openFolder == DialogResult.Yes)
                {
                    Process.Start("explorer.exe", $"/select,\"{zipPath}\"");
                }
            }
            catch (Exception ex)
            {
                Cursor = oldCursor;
                AppMessageBox.Show("Failed to create release package: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
    /// Publishes a single, self-sufficient exe (.NET single-file publish, framework-dependent, ReadyToRun)
    /// and drops it on the Desktop, ready to attach to a GitHub Release. Single-file publish already bundles
    /// AERO_Console.dll and FastColoredTextBox.dll into the one exe (extracted to a runtime temp cache
    /// automatically by .NET itself), so no separate "install to a folder" step is needed - the published
    /// exe already runs standalone from anywhere a user puts it, same as double-clicking it today, just
    /// without needing the sibling .dll/.json files a normal build/zip requires.
    /// Requires the .NET SDK (this is a maintainer/dev-machine action, not something end users run).
    /// </summary>
        private async void PackageStandaloneExeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var oldCursor = Cursor;
            try
            {
                string projectPath = FindProjectFile();
                if (projectPath is null)
                {
                    AppMessageBox.Show("Could not locate AERO_Console.vbproj by walking up from the running exe's folder." + Environment.NewLine + "This feature must be run from a normal Debug/Release build inside the source repo.", "Package as Standalone Exe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string versionStr = My.MyProject.Application.Info.Version.ToString();
                string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string destExePath = Path.Combine(desktopDir, $"AERO_Console_v{versionStr}.exe");

                if (File.Exists(destExePath))
                {
                    var overwrite = AppMessageBox.Show($"\"{Path.GetFileName(destExePath)}\" already exists on the Desktop. Overwrite it?", "Package as Standalone Exe", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (overwrite != DialogResult.Yes)
                        return;
                }

                Cursor = Cursors.WaitCursor;
                lblStatus.Text = "Status: Publishing standalone exe (this can take a minute)...";

                string projectDir = Path.GetDirectoryName(projectPath);
                string publishDir = Path.Combine(projectDir, "bin", "Release", "net10.0-windows", "publish", "win-x64");

                // Arguments are added one-by-one via ArgumentList (not a single manually-quoted .Arguments
                // string) so paths are escaped correctly. A hand-built "...\"" argument is misparsed by
                // Windows' command-line rules - a backslash immediately before a closing quote escapes the
                // quote itself instead of being a literal path separator, corrupting everything after it.
                // Passed as explicit properties (matching My Project\PublishProfiles\FolderProfile.pubxml) rather
                // than "-p:PublishProfile=FolderProfile" - the SDK's profile-name lookup does not resolve pubxml
                // files under the VB "My Project" folder here, so it silently falls back to a non-single-file publish.
                // (PublishDir is safe to override this way; BaseOutputPath/BaseIntermediateOutputPath are NOT -
                // MSBuild only honors those reliably when set before the Sdk import, so overriding them via -p:
                // on the command line produced duplicate generated AssemblyInfo files and a hard compile error.)
                var psi = new ProcessStartInfo("dotnet.exe")
                {
                    WorkingDirectory = projectDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                psi.ArgumentList.Add("publish");
                psi.ArgumentList.Add(projectPath);
                psi.ArgumentList.Add("-c");
                psi.ArgumentList.Add("Release");
                psi.ArgumentList.Add($@"-p:PublishDir={publishDir}\");
                psi.ArgumentList.Add("-p:SelfContained=false");
                psi.ArgumentList.Add("-p:RuntimeIdentifier=win-x64");
                psi.ArgumentList.Add("-p:PublishSingleFile=true");
                psi.ArgumentList.Add("-p:PublishReadyToRun=true");
                psi.ArgumentList.Add("--nologo");
                psi.ArgumentList.Add("-v:q");

                string output;
                int exitCode;
                using (var proc = new Process())
                {
                    proc.StartInfo = psi;
                    proc.Start();
                    var stdOutTask = proc.StandardOutput.ReadToEndAsync();
                    var stdErrTask = proc.StandardError.ReadToEndAsync();
                    await proc.WaitForExitAsync();
                    output = await stdOutTask + Environment.NewLine + await stdErrTask;
                    exitCode = proc.ExitCode;
                }

                if (exitCode != 0)
                {
                    Cursor = oldCursor;
                    lblStatus.Text = "Status: Publish failed.";
                    AppMessageBox.Show("dotnet publish failed:" + Environment.NewLine + output, "Package as Standalone Exe", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string[] publishedExeCandidates = Directory.Exists(publishDir) ? Directory.GetFiles(publishDir, "*.exe") : Array.Empty<string>();
                if (publishedExeCandidates.Length == 0)
                {
                    Cursor = oldCursor;
                    lblStatus.Text = "Status: Publish output not found.";
                    AppMessageBox.Show($"Publish succeeded but no .exe was found in:{Environment.NewLine}{publishDir}", "Package as Standalone Exe", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string publishedExePath = publishedExeCandidates[0];

                File.Copy(publishedExePath, destExePath, overwrite: true);

                Cursor = oldCursor;
                lblStatus.Text = "Status: Standalone exe created.";

                var openFolder = AppMessageBox.Show($"Standalone exe created:{Environment.NewLine}{destExePath}{Environment.NewLine}{Environment.NewLine}" + "It runs on its own from anywhere - no other files needed." + Environment.NewLine + Environment.NewLine + "Open containing folder?", "Package as Standalone Exe", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (openFolder == DialogResult.Yes)
                {
                    Process.Start("explorer.exe", $"/select,\"{destExePath}\"");
                }
            }
            catch (Exception ex)
            {
                Cursor = oldCursor;
                AppMessageBox.Show("Failed to create standalone exe: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
    /// Walks up from the running exe's folder (e.g. AVL_Geometry\bin\Release\net10.0-windows\) looking
    /// for the AERO_Console.vbproj that produced it, so packaging features can shell out to "dotnet publish".
    /// </summary>
        private string FindProjectFile()
        {
            var dir = new DirectoryInfo(Application.StartupPath);
            for (int i = 0; i <= 7; i++)
            {
                if (dir is null)
                    break;
                string candidate = Path.Combine(dir.FullName, "AERO_Console.vbproj");
                if (File.Exists(candidate))
                    return candidate;
                dir = dir.Parent;
            }
            return null;
        }

        private void RestartConsoleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!(p == null))
            {
                try
                {
                    if (!p.HasExited)
                        p.Kill();
                    p = null;
                    txtLog.Text = "";
                    if (lblStatus is not null)
                        lblStatus.Text = "Status: Restarting...";
                    loadConsole();
                }
                catch
                {
                }
            }
        }

        private void frmMain_Closing(object sender, CancelEventArgs e)
        {
            _logFlushTimer.Stop();

            foreach (Form F in Application.OpenForms)
                F.Hide();
            // frmPrompt.Show()

            if (Leaving == false)
            {
                Leaving = true;
                try
                {
                    while (!(p == null) && !p.HasExited)
                    {
                        if (!(p == null))
                        {
                            if (!p.HasExited)
                                p.Kill();
                            p = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppMessageBox.Show("Error: " + ex.Message);
                }
                // e.Cancel = True
                // Else
                string d = Application.StartupPath + @"\appdata";
                // Return
                if (Directory.Exists(d))
                {
                    while (Directory.Exists(d))
                    {
                        try
                        {
                            Directory.Delete(d, true);
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }

            }
        }

        /// <summary>
    /// Command entry lives in its own control (txtCommand) rather than being spliced
    /// into txtLog's read-only output history. A merged prompt-in-log design means a
    /// flood of output (e.g. an XFOIL "aseq -5 23 0.1" polar sweep, which can emit
    /// hundreds of lines while also redrawing its plot) races the same control the user
    /// is trying to type into, and previously left the console unable to accept new
    /// input until the app was restarted. Splitting it into a separate always-editable
    /// box means bulk output arriving in txtLog can never block or corrupt the user's
    /// ability to type and submit the next command.
    /// </summary>
        private void txtCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Return)
                return;
            e.SuppressKeyPress = true;

            // Previously this just silently did nothing whenever the AVL process had
            // died - which reads to the user as "the console stops working until I
            // restart the whole app". A common way to reach this state: frmGeometry's
            // Trefftz/Loads analyses (RunTrefftzAnalysisAsync/RunLoadsAnalysisAsync)
            // write raw command sequences straight to this same process's stdin with
            // no recovery of their own if AVL crashes/exits mid-sequence - so coming
            // back to frmMain afterward can land on a dead `p` with no indication why.
            // Self-heal instead: relaunch AVL the same way SaveAVL/SaveMass already
            // recover from a crash (loadConsole), and let the command through on the
            // fresh process, so typing "just works" again without an app restart -
            // at the cost of losing whatever was loaded into the old AVL session.
            if (p is null || p.HasExited)
            {
                AppToast.Show("AVL had stopped responding - restarting it");
                loadConsole();
                if (p is null || p.HasExited)
                    return;
            }

            string command = txtCommand.Text;
            try
            {
                p.StandardInput.WriteLine(command);
                p.StandardInput.Flush();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error sending command: " + ex.Message);
            }

            // Echo the submitted command into the output log, same as a real terminal,
            // since it no longer appears there automatically the way it did when typed
            // directly into txtLog.
            txtLog.AppendText("> " + command + Environment.NewLine);
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();

            txtCommand.Clear();
        }

        private void CheckForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            My.MyProject.Forms.frmUpdate.Show();
        }

        private void frmMain_Closed(object sender, EventArgs e)
        {
        }

        private void btnDesigner_Click(object sender, EventArgs e)
        {
            AirplaneDesignToolStripMenuItem.PerformClick();
        }

        private void FontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fd1.Font = txtLog.Font;
            if (fd1.ShowDialog() == DialogResult.OK)
            {
                // txtLog.Font = fd1.Font
                systemFont = fd1.Font;
                My.MySettingsProperty.Settings.appFont = systemFont;
                My.MySettingsProperty.Settings.Save();
                SetAllControlsFont(Controls, systemFont);
            }
        }

        public static void SetAllControlsFont(Control.ControlCollection ctrls, Font font)
        {
            Debug.WriteLine("I am in the set font for controls");
            foreach (Control ctrl in ctrls)
            {

                if (ctrl.Controls is not null)
                {
                    SetAllControlsFont(ctrl.Controls, font);
                }
                Debug.WriteLine(ctrl.Name);
                ctrl.Font = font; // New Font("Impact", ctrl.Font.Size - 4)
            }
        }

        private void AVLHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            My.MyProject.Forms.frmHelp.Show();
            My.MyProject.Forms.frmHelp.txt1.Text = My.MyProject.Forms.frmGeometry.readLines(My.MyProject.Forms.frmGeometry.help, 1, 2388);
        }

        /// <summary>
    /// AVL's "load"/"mass"/"case" commands are only recognized at its top-level menu -
    /// if the user is still sitting inside a submenu (OPER, MODE, etc., e.g. from typing
    /// commands directly into the console), sending one of these straight to stdin is
    /// silently misinterpreted as submenu input and does nothing. Sending a run of blank
    /// lines first backs out of any submenu depth (each blank line pops one level; extra
    /// blank lines at the top level are harmless no-ops), matching the same pattern
    /// frmGeometry uses before its own "load " commands.
    /// </summary>
        private void ReturnToTopMenu()
        {
            for (int i = 1; i <= 7; i++)
                p.StandardInput.WriteLine();
        }

        private void btnGeometry_Click(object sender, EventArgs e)
        {
            if (p is null || p.HasExited)
                return;
            try
            {
                if (curApp.ToLower() == "avl")
                {
                    string f = $"{projectName}.avl";
                    ReturnToTopMenu();
                    p.StandardInput.WriteLine($"load {f}");
                }
                else
                {
                    string cmd = "";
                    if (Information.IsNumeric(projectName) && projectName.Length == 4)
                    {
                        cmd = $"naca {projectName}";
                    }
                    else if (projectName.ToLower().StartsWith("naca"))
                    {
                        cmd = projectName;
                    }
                    else
                    {
                        cmd = $"load {projectName}.dat";
                    }
                    p.StandardInput.WriteLine(cmd);
                }
                p.StandardInput.Flush();
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Error: " + ex.Message);
            }
        }

        private string GetPlaceholderText()
        {
            if (curApp.ToLower() == "avl")
            {
                return "Enter AVL Project (e.g. glider)";
            }
            else
            {
                return "Enter NACA (e.g. 2412) or dat file";
            }
        }

        private void SetPlaceholder()
        {
            if (string.IsNullOrEmpty(txtName.Text) || txtName.Text == "Enter AVL Project (e.g. glider)" || txtName.Text == "Enter NACA (e.g. 2412) or dat file")
            {
                txtName.TextChanged -= txtName_TextChanged;
                txtName.Text = GetPlaceholderText();
                txtName.ComboBox.ForeColor = Color.Gray;
                txtName.TextChanged += txtName_TextChanged;
            }
        }

        private void ClearPlaceholder()
        {
            if (txtName.Text == "Enter AVL Project (e.g. glider)" || txtName.Text == "Enter NACA (e.g. 2412) or dat file")
            {
                txtName.TextChanged -= txtName_TextChanged;
                txtName.Text = "";
                txtName.ComboBox.ForeColor = Color.Black;
                txtName.TextChanged += txtName_TextChanged;
            }
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            // This form's own state (projectName/title/warning) must stay in sync with its own
            // txtName text regardless of WHY that text changed - including when it was just set by
            // a frmGeometry window syncing its project name into us (IsSyncingProject = True at that
            // point). Only the re-broadcast loop below needs to skip during a sync, to avoid an
            // infinite ping-pong between forms - otherwise this form's warning label (e.g. "using
            // default 'test' project") kept showing the stale pre-sync project after switching
            // projects from a Geometry window, since this whole handler used to bail out early.
            string txt = txtName.Text;
            if (txt == "Enter AVL Project (e.g. glider)" || txt == "Enter NACA (e.g. 2412) or dat file")
            {
                projectName = "";
            }
            else
            {
                projectName = txt;
            }

            UpdateTitleAndButtons();
            UpdateProjectWarning();

            if (IsSyncingProject)
                return;

            // Sync with open frmGeometry forms
            IsSyncingProject = true;
            try
            {
                foreach (Form frm in Application.OpenForms)
                {
                    if (frm is frmGeometry)
                    {
                        frmGeometry geoForm = (frmGeometry)frm;
                        if ((geoForm.txtName.Text ?? "") != (txtName.Text ?? ""))
                        {
                            geoForm.txtName.Text = txtName.Text;
                        }
                    }
                }
            }
            finally
            {
                IsSyncingProject = false;
            }
        }

        private async void cbEngine_SelectedIndexChanged(object sender, EventArgs e)
        {
            ToolStripComboBox cb = (ToolStripComboBox)sender;
            string selectedEngine = cb.SelectedItem.ToString().ToLower();

            if (selectedEngine == "xfoil")
            {
                bool downloaded = await DownloadXfoilAsync();
                if (!downloaded)
                {
                    cb.SelectedIndexChanged -= cbEngine_SelectedIndexChanged;
                    cb.SelectedIndex = 0; // Fallback to AVL
                    cb.SelectedIndexChanged += cbEngine_SelectedIndexChanged;
                    return;
                }
            }

            curApp = selectedEngine;
            if (lblStatus is not null)
            {
                lblStatus.Text = $"Status: Switching to {selectedEngine.ToUpper()}...";
            }

            // Update placeholder if active
            if (txtName.Text == "Enter AVL Project (e.g. glider)" || txtName.Text == "Enter NACA (e.g. 2412) or dat file")
            {
                txtName.TextChanged -= txtName_TextChanged;
                txtName.Text = GetPlaceholderText();
                txtName.ComboBox.ForeColor = Color.Gray;
                txtName.TextChanged += txtName_TextChanged;
            }

            loadConsole();
            UpdateTitleAndButtons();
        }

        /// <summary>
    /// Downloads a file with progress reported to the status-bar progress bar. The file is
    /// written to a ".download" sibling of destPath first and only moved into place on success,
    /// so a failed/cancelled download never leaves a half-written exe where the app expects one.
    /// </summary>
        private async Task<bool> DownloadFileWithProgressAsync(string url, string destPath, string displayName)
        {
            string oldStatus = lblStatus is not null ? lblStatus.Text : "";
            string tempPath = destPath + ".download";
            try
            {
                ShowDownloadProgress($"Status: Downloading {displayName}...");

                using (var client = new System.Net.Http.HttpClient())
                {
                    using (var response = await client.GetAsync(url, System.Net.Http.HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        var totalBytes = response.Content.Headers.ContentLength;

                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        {
                            using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                var buffer = new byte[81920];
                                long totalRead = 0L;
                                do
                                {
                                    int bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                                    if (bytesRead == 0)
                                        break;
                                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                                    totalRead += bytesRead;

                                    if (totalBytes.HasValue && totalBytes.Value > 0L)
                                    {
                                        int pct = (int)Math.Round(totalRead * 100L / (double)totalBytes.Value);
                                        UpdateDownloadProgress(pct, $"Status: Downloading {displayName}... {pct}%");
                                    }
                                    else
                                    {
                                        UpdateDownloadProgress(-1, $"Status: Downloading {displayName}... {totalRead / 1024L}KB");
                                    }
                                }
                                while (true);
                            }
                        }
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                File.Copy(tempPath, destPath, true);
                return true;
            }
            catch (Exception ex)
            {
                AppMessageBox.Show($"Failed to download {displayName}: {ex.Message}", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                }
                HideDownloadProgress(oldStatus);
            }
        }

        private void ShowDownloadProgress(string statusText)
        {
            if (downloadProgressBar is not null)
            {
                downloadProgressBar.Style = ProgressBarStyle.Marquee;
                downloadProgressBar.Value = 0;
                downloadProgressBar.Visible = true;
            }
            if (lblStatus is not null)
                lblStatus.Text = statusText;
        }

        private void UpdateDownloadProgress(int percent, string statusText)
        {
            if (downloadProgressBar is not null)
            {
                if (percent >= 0)
                {
                    downloadProgressBar.Style = ProgressBarStyle.Continuous;
                    downloadProgressBar.Value = Math.Min(100, Math.Max(0, percent));
                }
                else
                {
                    downloadProgressBar.Style = ProgressBarStyle.Marquee;
                }
            }
            if (lblStatus is not null)
                lblStatus.Text = statusText;
        }

        private void HideDownloadProgress(string restoreStatusText)
        {
            if (downloadProgressBar is not null)
                downloadProgressBar.Visible = false;
            if (lblStatus is not null)
                lblStatus.Text = restoreStatusText;
        }

        private void OpenExternalLink(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Could not open link: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DownloadAvlPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenExternalLink("https://web.mit.edu/drela/Public/web/avl/");
        }

        private void DownloadXfoilPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenExternalLink("https://web.mit.edu/drela/Public/web/xfoil/");
        }

        private async void DownloadAvlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await DownloadAvlToAsync(Path.Combine(Application.StartupPath, "appdata"), DownloadAvlToolStripMenuItem);
        }

        private async void DownloadAvlToConsoleFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await DownloadAvlToAsync(Application.StartupPath, DownloadAvlToConsoleFolderToolStripMenuItem);
        }

        private async Task DownloadAvlToAsync(string destDir, ToolStripMenuItem menuItem)
        {
            string destPath = Path.Combine(destDir, "avl.exe");

            if (File.Exists(destPath))
            {
                var resp = AppMessageBox.Show("AVL is already installed. Re-download the latest version and overwrite it?", "Download AVL", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (resp != DialogResult.Yes)
                    return;
            }

            menuItem.Enabled = false;
            try
            {
                // Windows keeps a running exe's image file locked, so if AVL is the active
                // engine the overwrite below fails with "process cannot access the file".
                bool wasRunningEngine = StopEngineProcessIfMatches("avl");
                if (await DownloadFileWithProgressAsync("https://web.mit.edu/drela/Public/web/avl/avl352.exe", destPath, "AVL"))
                {
                    AppToast.Show("AVL downloaded and installed");
                    if (wasRunningEngine)
                        loadConsole();
                }
                else if (wasRunningEngine)
                {
                    loadConsole();
                }
            }
            finally
            {
                menuItem.Enabled = true;
            }
        }

        /// <summary>
    /// If the given engine is currently running as the active console process, kills it so its
    /// exe file is no longer locked and a subsequent download/overwrite can replace it. Returns
    /// True if it stopped a running process (so the caller knows to restart the console after).
    /// </summary>
        private bool StopEngineProcessIfMatches(string engine)
        {
            if ((curApp.ToLower() ?? "") != (engine.ToLower() ?? ""))
                return false;
            if (p is null)
                return false;
            try
            {
                if (!p.HasExited)
                {
                    p.Kill();
                    p.WaitForExit(2000);
                }
                p.Dispose();
            }
            catch
            {
            }
            p = null;
            return true;
        }

        private async void DownloadXfoilToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DownloadXfoilToolStripMenuItem.Enabled = false;
            try
            {
                await DownloadXfoilAsync(forcePrompt: true);
            }
            finally
            {
                DownloadXfoilToolStripMenuItem.Enabled = true;
            }
        }

        private async void DownloadXfoilToConsoleFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DownloadXfoilToConsoleFolderToolStripMenuItem.Enabled = false;
            try
            {
                await DownloadXfoilAsync(forcePrompt: true, appDataDir: Application.StartupPath);
            }
            finally
            {
                DownloadXfoilToConsoleFolderToolStripMenuItem.Enabled = true;
            }
        }

        /// <summary>
    /// forcePrompt:=False (the cbEngine auto-switch path) silently reuses an existing xfoil.exe.
    /// forcePrompt:=True (the explicit "Download XFOIL..." menu item) always asks, even when
    /// XFOIL is already installed, so the menu item is actually useful for grabbing a fresh copy.
    /// </summary>
        internal async Task<bool> DownloadXfoilAsync(bool forcePrompt = false, string appDataDir = null)
        {
            if (appDataDir is null)
                appDataDir = Path.Combine(Application.StartupPath, "appdata");
            string xfoilPath = Path.Combine(appDataDir, "xfoil.exe");
            bool alreadyInstalled = File.Exists(xfoilPath);

            if (alreadyInstalled && !forcePrompt)
                return true;

            string promptMsg = alreadyInstalled ? "XFOIL is already installed. Re-download the latest version from MIT's official repository and overwrite it?" : "XFOIL is not found in the appdata folder. Would you like to download it from MIT's official repository now?";
            var response = AppMessageBox.Show(promptMsg, "Download XFOIL", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (response != DialogResult.Yes)
                return alreadyInstalled;

            string zipPath = Path.Combine(appDataDir, "XFOIL6.99.zip");
            string extractDir = Path.Combine(appDataDir, "xfoil_temp");

            if (!await DownloadFileWithProgressAsync("https://web.mit.edu/drela/Public/web/xfoil/XFOIL6.99.zip", zipPath, "XFOIL"))
            {
                return false;
            }

            // Windows keeps a running exe's image file locked, so if XFOIL is the active engine the
            // File.Copy below fails with "process cannot access the file" unless we stop it first.
            bool wasRunningEngine = StopEngineProcessIfMatches("xfoil");
            try
            {
                if (Directory.Exists(extractDir))
                    Directory.Delete(extractDir, true);
                ExtractZipSafely(zipPath, extractDir);

                string[] foundFiles = Directory.GetFiles(extractDir, "xfoil.exe", SearchOption.AllDirectories);
                if (foundFiles.Length == 0)
                {
                    AppMessageBox.Show("Downloaded XFOIL but couldn't find xfoil.exe inside the archive.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                File.Copy(foundFiles[0], xfoilPath, true);

                AppToast.Show("XFOIL downloaded and installed");
                return true;
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Failed to install XFOIL: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                try
                {
                    File.Delete(zipPath);
                    if (Directory.Exists(extractDir))
                        Directory.Delete(extractDir, true);
                }
                catch
                {
                }
                if (wasRunningEngine)
                    loadConsole();
            }
        }

        /// <summary>
    /// MIT's XFOIL6.99.zip is an old DOS/Windows-style archive whose entry names use backslashes
    /// instead of the zip-standard forward slash. .NET's ZipFile.ExtractToDirectory normalizes
    /// only forward slashes before its path-traversal check, so it misreads those backslash-named
    /// entries as escaping the destination and throws "would have resulted in a file outside the
    /// specified destination directory" even though nothing is actually malicious. Extracting
    /// manually lets us normalize separators ourselves before validating and writing each entry.
    /// </summary>
        private void ExtractZipSafely(string zipPath, string destDir)
        {
            string destRoot = Path.GetFullPath(destDir);
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    string relativePath = entry.FullName.Replace('\\', '/');
                    if (string.IsNullOrEmpty(relativePath))
                        continue;

                    string destPath = Path.GetFullPath(Path.Combine(destRoot, relativePath));
                    if (!destPath.StartsWith(destRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // skip anything that would still land outside destRoot
                    }

                    if (relativePath.EndsWith("/") || string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(destPath);
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                        entry.ExtractToFile(destPath, true);
                    }
                }
            }
        }

        public void UpdateTitleAndButtons()
        {
            string versionStr = My.MyProject.Application.Info.Version.ToString();
            Text = $"AERO Console (v{versionStr}) - Project: <{projectName}>";

            if (curApp.ToLower() == "avl")
            {
                btnGeometry.Text = "Load Geometry";
                btnGeometry.ToolTipText = "Load geometry file (.avl) into AVL";
                btnMass.Text = "Load Mass";
                btnMass.ToolTipText = "Load mass file (.mass) into AVL";
                btnRun.Text = "Load Run";
                btnRun.ToolTipText = "Load run case file (.run) into AVL";
                btnDesigner.Visible = true;
            }
            else
            {
                btnGeometry.Text = "Load Airfoil";
                btnGeometry.ToolTipText = "Load airfoil file (.dat) or NACA profile into XFOIL";
                btnMass.Text = "Init Polar";
                btnMass.ToolTipText = "Accumulate airfoil polar results (PACC)";
                btnRun.Text = "Run Alpha";
                btnRun.ToolTipText = "Calculate operational point at specified alpha (ALFA)";
                btnDesigner.Visible = false;
            }
            // AVL and XFOIL share the same underlying plot library, so the same "send blank
            // Enter keystrokes to dismiss whatever plot window is open" trick applies to both -
            // this button is the escape hatch for AVL's own native graphics windows (Trefftz
            // Plane "t", geometry "g", root-locus "n", etc.) that this app's UI doesn't drive
            // and otherwise stay stuck open until the console is fully reset.
            if (btnClosePlot is not null)
                btnClosePlot.Visible = true;

            bool hasProject = !string.IsNullOrEmpty(projectName);
            if (!hasProject)
            {
                string prompt = "Select or enter a project name above first";
                btnGeometry.Enabled = false;
                btnMass.Enabled = false;
                btnRun.Enabled = false;
                btnGeometry.ToolTipText = prompt;
                btnMass.ToolTipText = prompt;
                btnRun.ToolTipText = prompt;
                return;
            }

            if (curApp.ToLower() == "avl")
            {
                // Each button only has a task to perform once its associated file actually
                // exists on disk - "Load Mass"/"Load Run" have nothing to load until the
                // matching .mass/.run file for the current project shows up (e.g. via the
                // drop zone or an external editor), so keep them disabled with an explanatory
                // tooltip rather than letting the user click into a no-op "File not found" error.
                string avlPath = Path.Combine(Application.StartupPath, $"{projectName}.avl");
                string massPath = Path.Combine(Application.StartupPath, $"{projectName}.mass");
                string runPath = Path.Combine(Application.StartupPath, $"{projectName}.run");

                bool hasAvl = File.Exists(avlPath);
                bool hasMass = File.Exists(massPath);
                bool hasRun = File.Exists(runPath);

                btnGeometry.Enabled = hasAvl;
                btnGeometry.ToolTipText = hasAvl ? "Load geometry file (.avl) into AVL" : $"No \"{projectName}.avl\" file found for this project";

                btnMass.Enabled = hasMass;
                btnMass.ToolTipText = hasMass ? "Load mass file (.mass) into AVL" : $"No \"{projectName}.mass\" file found for this project";

                btnRun.Enabled = hasRun;
                btnRun.ToolTipText = hasRun ? "Load run case file (.run) into AVL" : $"No \"{projectName}.run\" file found for this project";
            }
            else
            {
                btnGeometry.Enabled = true;
                btnMass.Enabled = true;
                btnRun.Enabled = true;
            }
        }

        public void UpdateProjectWarning()
        {
            ToolStripLabel lbl = ToolStrip1.Items["lblWarning"] as ToolStripLabel;
            if (lbl is not null)
            {
                // lbl.Text is a fixed string set once at creation ("Using default 'test'
                // project...") - it was also being shown for an empty projectName, which is
                // actively misleading (an empty name isn't "test"). Match frmGeometry's
                // UpdateProjectWarning(): only the literal "test" case is what this message
                // actually describes.
                lbl.Visible = !string.IsNullOrEmpty(projectName) && projectName.ToLower() == "test";
            }
        }

        private void btnMass_Click(object sender, EventArgs e)
        {
            if (p is null || p.HasExited)
                return;
            try
            {
                if (curApp.ToLower() == "avl")
                {
                    string f = $"{projectName}.mass";
                    ReturnToTopMenu();
                    p.StandardInput.WriteLine($"mass {f}");
                }
                else
                {
                    p.StandardInput.WriteLine("oper");
                    p.StandardInput.WriteLine("pacc");
                    p.StandardInput.WriteLine($"{projectName}.pol");
                    p.StandardInput.WriteLine("");
                } // default dump file
                p.StandardInput.Flush();
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Error: " + ex.Message);
            }

        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            if (p is null || p.HasExited)
                return;
            try
            {
                if (curApp.ToLower() == "avl")
                {
                    string f = $"{projectName}.run";
                    ReturnToTopMenu();
                    p.StandardInput.WriteLine($"case {f}");
                }
                else
                {
                    string alpha = Interaction.InputBox("Enter Angle of Attack (alpha) for analysis:", "XFOIL Analysis", "5");
                    if (!string.IsNullOrEmpty(alpha))
                    {
                        txtLog.AppendText(Environment.NewLine + "[AERO Console] Plot window opened. Click the 'Close Plot' button or press Enter in the command box to dismiss it." + Environment.NewLine);
                        txtLog.SelectionStart = txtLog.Text.Length;
                        txtLog.ScrollToCaret();

                        p.StandardInput.WriteLine("oper");
                        p.StandardInput.WriteLine($"alfa {alpha}");
                    }
                }
                p.StandardInput.Flush();
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Error: " + ex.Message);
            }

        }

        /// <summary>
    /// AVL's/XFOIL's graphics windows (Trefftz Plane, geometry plot, root-locus, polar
    /// plot...) read keyboard/mouse input through the Windows console input API, which
    /// only exists for a process that owns a real console. This app launches AVL/XFOIL
    /// with CreateNoWindow=True and fully redirected pipes so it never gets one (that's
    /// what lets its text output be captured into txtLog) - which means once a plot window
    /// is open, there is no channel left for anything (stdin text, WM_CLOSE, synthetic
    /// keystrokes) to reach it. It is well and truly stuck. The only thing that has ever
    /// worked elsewhere in this app (see CaptureApplication in frmGeometry.vb and
    /// RestartConsoleToolStripMenuItem_Click) is killing the process outright. So instead
    /// of pretending we can dismiss just the plot window, kill-and-restart AVL/XFOIL and
    /// automatically re-load the current project's files, so from the user's perspective
    /// this reads as "clear the stuck plot" rather than a disruptive full reset.
    /// </summary>
        private void btnClosePlot_Click(object sender, EventArgs e)
        {
            if (p is null)
                return;
            try
            {
                string engine = curApp.ToLower();
                string name = projectName;

                txtLog.AppendText(Environment.NewLine + "[AERO Console] Clearing stuck plot window (restarting " + engine.ToUpper() + ")..." + Environment.NewLine);
                if (lblStatus is not null)
                    lblStatus.Text = "Status: Clearing stuck plot...";

                loadConsole();

                if (!string.IsNullOrEmpty(name))
                {
                    if (engine == "avl")
                    {
                        string avlPath = Path.Combine(Application.StartupPath, $"{name}.avl");
                        string massPath = Path.Combine(Application.StartupPath, $"{name}.mass");
                        string runPath = Path.Combine(Application.StartupPath, $"{name}.run");

                        if (File.Exists(avlPath))
                            p.StandardInput.WriteLine($"load {name}.avl");
                        if (File.Exists(massPath))
                            p.StandardInput.WriteLine($"mass {name}.mass");
                        if (File.Exists(runPath))
                            p.StandardInput.WriteLine($"case {name}.run");
                        p.StandardInput.Flush();
                    }
                }

                if (lblStatus is not null)
                    lblStatus.Text = $"Status: Running {engine.ToUpper()}";
            }
            catch
            {
            }
        }

        // Enable double-buffering on Form controls
        private void EnableDoubleBuffering(Control ctrl)
        {
            try
            {
                var dbProp = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dbProp is not null)
                {
                    dbProp.SetValue(ctrl, true, (object[])null);
                }
            }
            catch
            {
            }

            foreach (Control child in ctrl.Controls)
                EnableDoubleBuffering(child);
        }

    }
}
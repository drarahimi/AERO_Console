using System.Diagnostics;

namespace Aero_Console_Sharp
{
    public partial class frmMain : Form
    {
        private Process? p;
        private bool Leaving = false;
        private string logtext = string.Empty;
        private Thread bt;
        private string updatedPath = Application.StartupPath + "\\update.exe";
        private string originalPath = Application.StartupPath + "\\AERO_Console.exe";
        private bool appUpdateNeeded = false;
        private bool appUpdated = false;
        private string curApp = "avl";
        private bool firstLoad = false;
        public static Font systemFont = new Font("Consolas", 14);
        private string projectName = "test";
        string avlFilePath = Path.Combine(Application.StartupPath, "appdata", "avl.exe");
        string avldownloadUrl = "https://web.mit.edu/drela/Public/web/avl/avl3.40_execs/WIN64/avl.exe";

        TextBox txtLog = new System.Windows.Forms.TextBox();
        TextBox txtCommand = new TextBox();

        public frmMain()
        {
            InitializeComponent();
            loadComponents();
        }

        private void loadComponents()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Load += (sender, e) =>
            {
                if (!firstLoad)
                {
                    firstLoad = true;
                    Debug.WriteLine("loading console");
                    //Process.Start("explorer.exe", Application.StartupPath);
                    LoadConsole();
                }
            };
            this.FormClosing += (sender, e) =>
            {
                Leaving = true;
                if (p != null)
                {
                    p.Close();
                    while (!p.HasExited)
                    {
                        Application.DoEvents();
                    }
                }
            };

            this.Controls.Clear();

            //this.FormClosing += FrmSyllabus_FormClosing;

            var table1 = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
            };
            table1.ColumnCount = 10;
            var rowIndex = 0;
            var rowHeight = 25;
            var scale = Commons.GetDisplayScale();
            table1.Dock = DockStyle.Fill;
            for (int i = 0; i < table1.ColumnCount; i++)
                table1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));

            table1.Margin = new Padding(0, 0, 0, 0);
            table1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, rowHeight * scale));
            Commons.AddLabelToTable("Semester", table1, 0, rowIndex, 2, DockStyle.Fill, ContentAlignment.MiddleLeft).Margin = new Padding(0, 0, 0, 0);

            //Row
            rowIndex++;
            table1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, rowHeight * scale));
            Commons.AddLabelToTable("Start", table1, 0, rowIndex, 1, DockStyle.Fill, ContentAlignment.MiddleLeft);

            //Row
            rowIndex++;
            table1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 90));
            txtLog = new TextBox()
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.Black,
                Font = new System.Drawing.Font("Consolas", 12.0f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point),
                ForeColor = System.Drawing.Color.White,
                Multiline = true,
                Name = "txtLog",
                ReadOnly = true,
                ScrollBars = System.Windows.Forms.ScrollBars.Vertical,
                WordWrap = false
            };
            table1.Controls.Add(txtLog, 0, rowIndex);
            table1.SetColumnSpan(txtLog, 10);

            //Row
            rowIndex++;
            table1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, scale * rowHeight * 1.5f));
            txtCommand = new TextBox()
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.Black,
                Font = new System.Drawing.Font("Consolas", 12.0f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point),
                ForeColor = System.Drawing.Color.White,
                PlaceholderText = "Enter command here...",
            };
            table1.Controls.Add(txtCommand, 0, rowIndex);
            table1.SetColumnSpan(txtCommand, 10);

            txtCommand.KeyDown += (sender, e) =>
            {

                if (e.KeyCode == Keys.Return)
                {
                    e.SuppressKeyPress = true;
                    p.StandardInput.WriteLine(txtCommand.Text);
                    txtCommand.Text = "";
                    txtCommand.Select();
                }
            };

            this.Controls.Add(table1);





        }


        private void ReadThread()
        {
            Debug.WriteLine("Read thread called");
            while (!Leaving)
            {
                if (p == null)
                {
                    Debug.WriteLine("Error in ReadThread: Process is null");
                    continue;
                }

                try
                {
                    int rLine = p.StandardOutput.Read();
                    logtext += Convert.ToChar(rLine);
                    //Debug.WriteLine(logtext);
                    //if (p.StandardOutput.Peek() == -1)
                    {
                        Invoke(new Action(() => txtLog.AppendText(logtext)));
                        logtext = string.Empty;
                    }
                }
                catch (ThreadAbortException ex)
                {
                    Debug.WriteLine("Error in ReadThread while reading input: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error in ReadThread while reading input: " + ex.Message);
                }
                //Application.DoEvents();
            }

            try
            {
                Debug.WriteLine("Closing form");
                //Invoke(new Action(() => FrmMain_Closing(this, new CancelEventArgs(false))));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in ReadThread while closing form: " + ex.Message);
            }
        }

        public void LoadConsole()
        {
            if (p == null)
            {
                p = new Process();
                var startInfo = new ProcessStartInfo
                {
                    WorkingDirectory = Application.StartupPath,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };


                if (!File.Exists(avlFilePath))
                {
                    Debug.WriteLine("AVL executable not found. Downloading...");
                    Commons.DownloadFile(avldownloadUrl, avlFilePath);
                    Debug.WriteLine("Download complete.");
                }


                startInfo.FileName = avlFilePath;

                p.StartInfo = startInfo;
                p.EnableRaisingEvents = true;

                bt = new Thread(ReadThread) { IsBackground = true };
                p.Start();
                bt.Start();
            }
            else
            {
                try
                {
                    p.Close();
                    while (!p.HasExited)
                    {
                        Application.DoEvents();
                    }

                    p = null;
                    LoadConsole();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error while closing process: " + ex.Message);
                }
            }
        }


    }
}

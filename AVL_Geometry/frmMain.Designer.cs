using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace AERO_Console
{
    [Microsoft.VisualBasic.CompilerServices.DesignerGenerated()]
    public partial class frmMain : Form
    {

        // Form overrides dispose to clean up the component list.
        [DebuggerNonUserCode()]
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && components is not null)
                {
                    components.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        // Required by the Windows Form Designer
        private System.ComponentModel.IContainer components;

        // NOTE: The following procedure is required by the Windows Form Designer
        // It can be modified using the Windows Form Designer.  
        // Do not modify it using the code editor.
        [DebuggerStepThrough()]
        private void InitializeComponent()
        {
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            MenuStrip1 = new MenuStrip();
            FileToolStripMenuItem = new ToolStripMenuItem();
            OpenCurrentDirectoryToolStripMenuItem = new ToolStripMenuItem();
            OpenCurrentDirectoryToolStripMenuItem.Click += new EventHandler(OpenCurrentDirectoryToolStripMenuItem_Click);
            FileToolStripSeparator1 = new ToolStripSeparator();
            PackageForReleaseToolStripMenuItem = new ToolStripMenuItem();
            PackageForReleaseToolStripMenuItem.Click += new EventHandler(PackageForReleaseToolStripMenuItem_Click);
            PackageStandaloneExeToolStripMenuItem = new ToolStripMenuItem();
            PackageStandaloneExeToolStripMenuItem.Click += new EventHandler(PackageStandaloneExeToolStripMenuItem_Click);
            ToolsToolStripMenuItem = new ToolStripMenuItem();
            AirplaneDesignToolStripMenuItem = new ToolStripMenuItem();
            AirplaneDesignToolStripMenuItem.Click += new EventHandler(AirplaneDesignToolStripMenuItem_Click);
            XfoilAnalysisToolStripMenuItem = new ToolStripMenuItem();
            XfoilAnalysisToolStripMenuItem.Click += new EventHandler(XfoilAnalysisToolStripMenuItem_Click);
            RestartConsoleToolStripMenuItem = new ToolStripMenuItem();
            RestartConsoleToolStripMenuItem.Click += new EventHandler(RestartConsoleToolStripMenuItem_Click);
            DownloadToolStripMenuItem = new ToolStripMenuItem();
            DownloadAvlPageToolStripMenuItem = new ToolStripMenuItem();
            DownloadAvlPageToolStripMenuItem.Click += new EventHandler(DownloadAvlPageToolStripMenuItem_Click);
            DownloadXfoilPageToolStripMenuItem = new ToolStripMenuItem();
            DownloadXfoilPageToolStripMenuItem.Click += new EventHandler(DownloadXfoilPageToolStripMenuItem_Click);
            DownloadMenuSeparator1 = new ToolStripSeparator();
            DownloadAvlToolStripMenuItem = new ToolStripMenuItem();
            DownloadAvlToolStripMenuItem.Click += new EventHandler(DownloadAvlToolStripMenuItem_Click);
            DownloadXfoilToolStripMenuItem = new ToolStripMenuItem();
            DownloadXfoilToolStripMenuItem.Click += new EventHandler(DownloadXfoilToolStripMenuItem_Click);
            DownloadMenuSeparator2 = new ToolStripSeparator();
            DownloadAvlToConsoleFolderToolStripMenuItem = new ToolStripMenuItem();
            DownloadAvlToConsoleFolderToolStripMenuItem.Click += new EventHandler(DownloadAvlToConsoleFolderToolStripMenuItem_Click);
            DownloadXfoilToConsoleFolderToolStripMenuItem = new ToolStripMenuItem();
            DownloadXfoilToConsoleFolderToolStripMenuItem.Click += new EventHandler(DownloadXfoilToConsoleFolderToolStripMenuItem_Click);
            DisplayToolStripMenuItem = new ToolStripMenuItem();
            FontToolStripMenuItem = new ToolStripMenuItem();
            FontToolStripMenuItem.Click += new EventHandler(FontToolStripMenuItem_Click);
            HelpToolStripMenuItem = new ToolStripMenuItem();
            AVLHelpToolStripMenuItem = new ToolStripMenuItem();
            AVLHelpToolStripMenuItem.Click += new EventHandler(AVLHelpToolStripMenuItem_Click);
            AboutToolStripMenuItem = new ToolStripMenuItem();
            AboutToolStripMenuItem.Click += new EventHandler(AboutToolStripMenuItem_Click);
            CheckForUpdatesToolStripMenuItem = new ToolStripMenuItem();
            CheckForUpdatesToolStripMenuItem.Click += new EventHandler(CheckForUpdatesToolStripMenuItem_Click);
            ToolStrip1 = new ToolStrip();
            ToolStripLabel1 = new ToolStripLabel();
            txtName = new ToolStripComboBox();
            txtName.TextChanged += new EventHandler(txtName_TextChanged);
            btnGeometry = new ToolStripButton();
            btnGeometry.Click += new EventHandler(btnGeometry_Click);
            btnMass = new ToolStripButton();
            btnMass.Click += new EventHandler(btnMass_Click);
            btnRun = new ToolStripButton();
            btnRun.Click += new EventHandler(btnRun_Click);
            btnDesigner = new ToolStripButton();
            btnDesigner.Click += new EventHandler(btnDesigner_Click);
            fd1 = new FontDialog();
            LayoutTable = new TableLayoutPanel();
            txtLog = new TextBox();
            txtCommand = new TextBox();
            txtCommand.KeyDown += new KeyEventHandler(txtCommand_KeyDown);
            StatusStrip1 = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            downloadProgressBar = new ToolStripProgressBar();
            ToolStrip2 = new ToolStrip();
            MenuStrip1.SuspendLayout();
            ToolStrip1.SuspendLayout();
            LayoutTable.SuspendLayout();
            StatusStrip1.SuspendLayout();
            ToolStrip2.SuspendLayout();
            SuspendLayout();
            // 
            // MenuStrip1
            // 
            MenuStrip1.BackColor = SystemColors.Menu;
            MenuStrip1.Items.AddRange(new ToolStripItem[] { FileToolStripMenuItem, ToolsToolStripMenuItem, DownloadToolStripMenuItem, DisplayToolStripMenuItem, HelpToolStripMenuItem });
            MenuStrip1.Location = new Point(0, 0);
            MenuStrip1.Name = "MenuStrip1";
            MenuStrip1.RenderMode = ToolStripRenderMode.Professional;
            // MenuStrip overrides ToolStrip's ShowItemToolTips default (True) down to False -
            // without this, every ToolTipText set on a menu item below (File/Tools/Download/
            // Display/Help and their dropdown items) is silently never shown.
            MenuStrip1.ShowItemToolTips = true;
            MenuStrip1.Size = new Size(1026, 24);
            MenuStrip1.TabIndex = 2;
            MenuStrip1.Text = "MenuStrip1";
            // 
            // FileToolStripMenuItem
            // 
            FileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { OpenCurrentDirectoryToolStripMenuItem, FileToolStripSeparator1, PackageForReleaseToolStripMenuItem, PackageStandaloneExeToolStripMenuItem });
            FileToolStripMenuItem.Name = "FileToolStripMenuItem";
            FileToolStripMenuItem.Size = new Size(37, 20);
            FileToolStripMenuItem.Text = "&File";
            // 
            // OpenCurrentDirectoryToolStripMenuItem
            // 
            OpenCurrentDirectoryToolStripMenuItem.Name = "OpenCurrentDirectoryToolStripMenuItem";
            OpenCurrentDirectoryToolStripMenuItem.Size = new Size(197, 22);
            OpenCurrentDirectoryToolStripMenuItem.Text = "Open Current Directory";
            OpenCurrentDirectoryToolStripMenuItem.ToolTipText = "Open the current project's working folder in File Explorer";
            // 
            // FileToolStripSeparator1
            // 
            FileToolStripSeparator1.Name = "FileToolStripSeparator1";
            // 
            // PackageForReleaseToolStripMenuItem
            // 
            PackageForReleaseToolStripMenuItem.Name = "PackageForReleaseToolStripMenuItem";
            PackageForReleaseToolStripMenuItem.Size = new Size(251, 22);
            PackageForReleaseToolStripMenuItem.Text = "Package as Zip (Portable)...";
            PackageForReleaseToolStripMenuItem.ToolTipText = "Zip the bare-minimum runtime files and save it to the Desktop, ready to attach to a GitHub Release.";
            // 
            // PackageStandaloneExeToolStripMenuItem
            // 
            PackageStandaloneExeToolStripMenuItem.Name = "PackageStandaloneExeToolStripMenuItem";
            PackageStandaloneExeToolStripMenuItem.Size = new Size(251, 22);
            PackageStandaloneExeToolStripMenuItem.Text = "Package as Standalone Exe...";
            PackageStandaloneExeToolStripMenuItem.ToolTipText = "Publish a single self-sufficient exe (no separate DLLs needed) and save it to the Desktop, ready to attach to a GitHub Release.";
            // 
            // ToolsToolStripMenuItem
            // 
            ToolsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { AirplaneDesignToolStripMenuItem, XfoilAnalysisToolStripMenuItem, RestartConsoleToolStripMenuItem });
            ToolsToolStripMenuItem.Name = "ToolsToolStripMenuItem";
            ToolsToolStripMenuItem.Size = new Size(47, 20);
            ToolsToolStripMenuItem.Text = "&Tools";
            // 
            // AirplaneDesignToolStripMenuItem
            // 
            AirplaneDesignToolStripMenuItem.Name = "AirplaneDesignToolStripMenuItem";
            AirplaneDesignToolStripMenuItem.Size = new Size(175, 22);
            AirplaneDesignToolStripMenuItem.Text = "Geometry Designer";
            AirplaneDesignToolStripMenuItem.ToolTipText = "Open the visual editor for building/editing this project's .avl geometry file";
            // 
            // XfoilAnalysisToolStripMenuItem
            // 
            XfoilAnalysisToolStripMenuItem.Name = "XfoilAnalysisToolStripMenuItem";
            XfoilAnalysisToolStripMenuItem.Size = new Size(175, 22);
            XfoilAnalysisToolStripMenuItem.Text = "XFOIL Analysis";
            XfoilAnalysisToolStripMenuItem.ToolTipText = "Open the standalone XFOIL window for polar sweeps, Cp, and boundary-layer plots";
            // 
            // RestartConsoleToolStripMenuItem
            // 
            RestartConsoleToolStripMenuItem.Name = "RestartConsoleToolStripMenuItem";
            RestartConsoleToolStripMenuItem.Size = new Size(175, 22);
            RestartConsoleToolStripMenuItem.Text = "Restart Console";
            RestartConsoleToolStripMenuItem.ToolTipText = "Kill and relaunch the AVL/XFOIL engine process";
            // 
            // DownloadToolStripMenuItem
            // 
            DownloadToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { DownloadAvlPageToolStripMenuItem, DownloadXfoilPageToolStripMenuItem, DownloadMenuSeparator1, DownloadAvlToolStripMenuItem, DownloadXfoilToolStripMenuItem, DownloadMenuSeparator2, DownloadAvlToConsoleFolderToolStripMenuItem, DownloadXfoilToConsoleFolderToolStripMenuItem });
            DownloadToolStripMenuItem.Name = "DownloadToolStripMenuItem";
            DownloadToolStripMenuItem.Size = new Size(78, 20);
            DownloadToolStripMenuItem.Text = "&Download";
            // 
            // DownloadAvlPageToolStripMenuItem
            // 
            DownloadAvlPageToolStripMenuItem.Name = "DownloadAvlPageToolStripMenuItem";
            DownloadAvlPageToolStripMenuItem.Size = new Size(260, 22);
            DownloadAvlPageToolStripMenuItem.Text = "AVL Homepage...";
            DownloadAvlPageToolStripMenuItem.ToolTipText = "Open MIT's official AVL page in your browser";
            // 
            // DownloadXfoilPageToolStripMenuItem
            // 
            DownloadXfoilPageToolStripMenuItem.Name = "DownloadXfoilPageToolStripMenuItem";
            DownloadXfoilPageToolStripMenuItem.Size = new Size(260, 22);
            DownloadXfoilPageToolStripMenuItem.Text = "XFOIL Homepage...";
            DownloadXfoilPageToolStripMenuItem.ToolTipText = "Open MIT's official XFOIL page in your browser";
            // 
            // DownloadMenuSeparator1
            // 
            DownloadMenuSeparator1.Name = "DownloadMenuSeparator1";
            // 
            // DownloadAvlToolStripMenuItem
            // 
            DownloadAvlToolStripMenuItem.Name = "DownloadAvlToolStripMenuItem";
            DownloadAvlToolStripMenuItem.Size = new Size(260, 22);
            DownloadAvlToolStripMenuItem.Text = "Download AVL...";
            DownloadAvlToolStripMenuItem.ToolTipText = "Download the latest AVL executable into the appdata folder";
            // 
            // DownloadXfoilToolStripMenuItem
            // 
            DownloadXfoilToolStripMenuItem.Name = "DownloadXfoilToolStripMenuItem";
            DownloadXfoilToolStripMenuItem.Size = new Size(260, 22);
            DownloadXfoilToolStripMenuItem.Text = "Download XFOIL...";
            DownloadXfoilToolStripMenuItem.ToolTipText = "Download the latest XFOIL executable into the appdata folder";
            // 
            // DownloadMenuSeparator2
            // 
            DownloadMenuSeparator2.Name = "DownloadMenuSeparator2";
            // 
            // DownloadAvlToConsoleFolderToolStripMenuItem
            // 
            DownloadAvlToConsoleFolderToolStripMenuItem.Name = "DownloadAvlToConsoleFolderToolStripMenuItem";
            DownloadAvlToConsoleFolderToolStripMenuItem.Size = new Size(260, 22);
            DownloadAvlToConsoleFolderToolStripMenuItem.Text = "Download AVL to AeroConsole folder...";
            DownloadAvlToConsoleFolderToolStripMenuItem.ToolTipText = "Download the latest AVL executable into the same folder as AeroConsole.exe";
            // 
            // DownloadXfoilToConsoleFolderToolStripMenuItem
            // 
            DownloadXfoilToConsoleFolderToolStripMenuItem.Name = "DownloadXfoilToConsoleFolderToolStripMenuItem";
            DownloadXfoilToConsoleFolderToolStripMenuItem.Size = new Size(260, 22);
            DownloadXfoilToConsoleFolderToolStripMenuItem.Text = "Download XFOIL to AeroConsole folder...";
            DownloadXfoilToConsoleFolderToolStripMenuItem.ToolTipText = "Download the latest XFOIL executable into the same folder as AeroConsole.exe";
            // 
            // DisplayToolStripMenuItem
            // 
            DisplayToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { FontToolStripMenuItem });
            DisplayToolStripMenuItem.Name = "DisplayToolStripMenuItem";
            DisplayToolStripMenuItem.Size = new Size(57, 20);
            DisplayToolStripMenuItem.Text = "&Display";
            // 
            // FontToolStripMenuItem
            // 
            FontToolStripMenuItem.Name = "FontToolStripMenuItem";
            FontToolStripMenuItem.Size = new Size(98, 22);
            FontToolStripMenuItem.Text = "Font";
            FontToolStripMenuItem.ToolTipText = "Change the font used across the whole app (console log, dialogs, etc.)";
            // 
            // HelpToolStripMenuItem
            // 
            HelpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { AVLHelpToolStripMenuItem, AboutToolStripMenuItem, CheckForUpdatesToolStripMenuItem });
            HelpToolStripMenuItem.Name = "HelpToolStripMenuItem";
            HelpToolStripMenuItem.Size = new Size(44, 20);
            HelpToolStripMenuItem.Text = "&Help";
            // 
            // AVLHelpToolStripMenuItem
            // 
            AVLHelpToolStripMenuItem.Name = "AVLHelpToolStripMenuItem";
            AVLHelpToolStripMenuItem.Size = new Size(171, 22);
            AVLHelpToolStripMenuItem.Text = "AVL Help";
            AVLHelpToolStripMenuItem.ToolTipText = "Open AVL's built-in help/documentation text";
            // 
            // AboutToolStripMenuItem
            // 
            AboutToolStripMenuItem.Name = "AboutToolStripMenuItem";
            AboutToolStripMenuItem.Size = new Size(171, 22);
            AboutToolStripMenuItem.Text = "About";
            // 
            // CheckForUpdatesToolStripMenuItem
            // 
            CheckForUpdatesToolStripMenuItem.Name = "CheckForUpdatesToolStripMenuItem";
            CheckForUpdatesToolStripMenuItem.Size = new Size(171, 22);
            CheckForUpdatesToolStripMenuItem.Text = "Check for Updates";
            CheckForUpdatesToolStripMenuItem.ToolTipText = "Check GitHub for a newer release and offer to download/install it";
            // 
            // ToolStrip1
            // 
            ToolStrip1.Items.AddRange(new ToolStripItem[] { ToolStripLabel1, txtName });
            ToolStrip1.Location = new Point(0, 24);
            ToolStrip1.Name = "ToolStrip1";
            ToolStrip1.Padding = new Padding(5, 0, 1, 0);
            ToolStrip1.RenderMode = ToolStripRenderMode.Professional;
            ToolStrip1.Size = new Size(1026, 25);
            ToolStrip1.TabIndex = 3;
            ToolStrip1.Text = "ToolStrip1";
            // 
            // ToolStripLabel1
            // 
            ToolStripLabel1.ForeColor = Color.DimGray;
            ToolStripLabel1.Name = "ToolStripLabel1";
            ToolStripLabel1.Size = new Size(80, 22);
            ToolStripLabel1.Text = "Project name:";
            // 
            // txtName
            // 
            txtName.Name = "txtName";
            txtName.Size = new Size(500, 25);
            txtName.ToolTipText = "AVL mode: project base name (loads name.avl/.mass/.run). XFOIL mode: a NACA code (e.g. 2412) or a .dat file path.";
            // 
            // btnGeometry
            // 
            btnGeometry.Image = (Image)resources.GetObject("btnGeometry.Image");
            btnGeometry.ImageTransparentColor = Color.Magenta;
            btnGeometry.Margin = new Padding(5, 1, 0, 2);
            btnGeometry.Name = "btnGeometry";
            btnGeometry.Size = new Size(108, 22);
            btnGeometry.Text = "Load Geometry";
            btnGeometry.ToolTipText = "Load this project's .avl geometry (or NACA/.dat airfoil in XFOIL mode) into the running engine";
            // 
            // btnMass
            // 
            btnMass.Image = (Image)resources.GetObject("btnMass.Image");
            btnMass.ImageTransparentColor = Color.Magenta;
            btnMass.Name = "btnMass";
            btnMass.Size = new Size(83, 22);
            btnMass.Text = "Load Mass";
            btnMass.ToolTipText = "Load this project's .mass file (AVL), or start a polar accumulation (XFOIL)";
            // 
            // btnRun
            // 
            btnRun.Image = (Image)resources.GetObject("btnRun.Image");
            btnRun.ImageTransparentColor = Color.Magenta;
            btnRun.Name = "btnRun";
            btnRun.Size = new Size(77, 22);
            btnRun.Text = "Load Run";
            btnRun.ToolTipText = "Load this project's .run case file (AVL), or prompt for an alpha to run (XFOIL)";
            // 
            // btnDesigner
            // 
            btnDesigner.Alignment = ToolStripItemAlignment.Right;
            btnDesigner.Image = (Image)resources.GetObject("btnDesigner.Image");
            btnDesigner.ImageTransparentColor = Color.Magenta;
            btnDesigner.Name = "btnDesigner";
            btnDesigner.Size = new Size(128, 22);
            btnDesigner.Text = "Geometry Designer";
            btnDesigner.ToolTipText = "Open the visual editor for building/editing this project's .avl geometry file";
            // 
            // LayoutTable
            // 
            LayoutTable.ColumnCount = 1;
            LayoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            LayoutTable.Controls.Add(txtLog, 0, 0);
            LayoutTable.Controls.Add(txtCommand, 0, 1);
            LayoutTable.Dock = DockStyle.Fill;
            LayoutTable.Location = new Point(0, 74);
            LayoutTable.Name = "LayoutTable";
            LayoutTable.Padding = new Padding(5);
            LayoutTable.RowCount = 2;
            LayoutTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            LayoutTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
            LayoutTable.Size = new Size(1026, 459);
            LayoutTable.TabIndex = 4;
            // 
            // txtLog
            // 
            txtLog.BackColor = Color.FromArgb(30, 30, 30);
            txtLog.BorderStyle = BorderStyle.None;
            txtLog.Dock = DockStyle.Fill;
            txtLog.Font = new Font("Consolas", 11.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtLog.ForeColor = Color.Gainsboro;
            txtLog.Location = new Point(5, 5);
            txtLog.Margin = new Padding(0, 0, 0, 5);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(1016, 415);
            txtLog.TabIndex = 1;
            txtLog.TabStop = false;
            txtLog.WordWrap = false;
            // 
            // txtCommand
            // 
            txtCommand.BackColor = Color.FromArgb(45, 45, 48);
            txtCommand.BorderStyle = BorderStyle.FixedSingle;
            txtCommand.Dock = DockStyle.Fill;
            txtCommand.Font = new Font("Consolas", 11.25f, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtCommand.ForeColor = Color.White;
            txtCommand.Location = new Point(5, 425);
            txtCommand.Margin = new Padding(0);
            txtCommand.Name = "txtCommand";
            txtCommand.PlaceholderText = "Type your commands here...";
            txtCommand.Size = new Size(1016, 24);
            txtCommand.TabIndex = 0;
            // 
            // StatusStrip1
            // 
            StatusStrip1.BackColor = Color.FromArgb(28, 28, 28);
            StatusStrip1.ForeColor = Color.White;
            StatusStrip1.Items.AddRange(new ToolStripItem[] { lblStatus, downloadProgressBar });
            StatusStrip1.Location = new Point(0, 533);
            StatusStrip1.Name = "StatusStrip1";
            StatusStrip1.RenderMode = ToolStripRenderMode.Professional;
            StatusStrip1.Size = new Size(1026, 22);
            StatusStrip1.TabIndex = 5;
            StatusStrip1.Text = "StatusStrip1";
            // 
            // downloadProgressBar
            // 
            downloadProgressBar.Alignment = ToolStripItemAlignment.Right;
            downloadProgressBar.Name = "downloadProgressBar";
            downloadProgressBar.Size = new Size(150, 16);
            downloadProgressBar.Visible = false;
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(64, 17);
            lblStatus.Text = "Status: Idle";
            // 
            // ToolStrip2
            // 
            ToolStrip2.Items.AddRange(new ToolStripItem[] { btnGeometry, btnMass, btnRun, btnDesigner });
            ToolStrip2.Location = new Point(0, 49);
            ToolStrip2.Name = "ToolStrip2";
            ToolStrip2.Padding = new Padding(5, 0, 1, 0);
            ToolStrip2.RenderMode = ToolStripRenderMode.Professional;
            ToolStrip2.Size = new Size(1026, 25);
            ToolStrip2.TabIndex = 6;
            ToolStrip2.Text = "ToolStrip2";
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1026, 555);
            Controls.Add(LayoutTable);
            Controls.Add(StatusStrip1);
            Controls.Add(ToolStrip2);
            Controls.Add(ToolStrip1);
            Controls.Add(MenuStrip1);
            Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point, 0);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = MenuStrip1;
            Name = "frmMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "AVL - Console";
            MenuStrip1.ResumeLayout(false);
            MenuStrip1.PerformLayout();
            ToolStrip1.ResumeLayout(false);
            ToolStrip1.PerformLayout();
            LayoutTable.ResumeLayout(false);
            LayoutTable.PerformLayout();
            StatusStrip1.ResumeLayout(false);
            StatusStrip1.PerformLayout();
            ToolStrip2.ResumeLayout(false);
            ToolStrip2.PerformLayout();
            Load += new EventHandler(frmMain_Load);
            Closing += new System.ComponentModel.CancelEventHandler(frmMain_Closing);
            Closed += new EventHandler(frmMain_Closed);
            ResumeLayout(false);
            PerformLayout();

        }
        internal MenuStrip MenuStrip1;
        internal ToolStripMenuItem FileToolStripMenuItem;
        internal ToolStripMenuItem ToolsToolStripMenuItem;
        internal ToolStripMenuItem AirplaneDesignToolStripMenuItem;
        internal ToolStripMenuItem XfoilAnalysisToolStripMenuItem;
        internal ToolStripMenuItem HelpToolStripMenuItem;
        internal ToolStripMenuItem AboutToolStripMenuItem;
        internal ToolStripMenuItem OpenCurrentDirectoryToolStripMenuItem;
        internal ToolStripSeparator FileToolStripSeparator1;
        internal ToolStripMenuItem PackageForReleaseToolStripMenuItem;
        internal ToolStripMenuItem PackageStandaloneExeToolStripMenuItem;
        internal ToolStripMenuItem RestartConsoleToolStripMenuItem;
        internal ToolStripMenuItem CheckForUpdatesToolStripMenuItem;
        internal ToolStrip ToolStrip1;
        internal ToolStripButton btnDesigner;
        internal ToolStripMenuItem DisplayToolStripMenuItem;
        internal ToolStripMenuItem FontToolStripMenuItem;
        internal FontDialog fd1;
        internal ToolStripMenuItem AVLHelpToolStripMenuItem;
        internal ToolStripLabel ToolStripLabel1;
        internal ToolStripComboBox txtName;
        internal ToolStripButton btnGeometry;
        internal ToolStripButton btnMass;
        internal ToolStripButton btnRun;
        internal TableLayoutPanel LayoutTable;
        internal TextBox txtLog;
        internal TextBox txtCommand;
        internal StatusStrip StatusStrip1;
        internal ToolStripStatusLabel lblStatus;
        internal ToolStripProgressBar downloadProgressBar;
        internal ToolStrip ToolStrip2;
        internal ToolStripMenuItem DownloadToolStripMenuItem;
        internal ToolStripMenuItem DownloadAvlPageToolStripMenuItem;
        internal ToolStripMenuItem DownloadXfoilPageToolStripMenuItem;
        internal ToolStripSeparator DownloadMenuSeparator1;
        internal ToolStripMenuItem DownloadAvlToolStripMenuItem;
        internal ToolStripMenuItem DownloadXfoilToolStripMenuItem;
        internal ToolStripSeparator DownloadMenuSeparator2;
        internal ToolStripMenuItem DownloadAvlToConsoleFolderToolStripMenuItem;
        internal ToolStripMenuItem DownloadXfoilToConsoleFolderToolStripMenuItem;
    }
}
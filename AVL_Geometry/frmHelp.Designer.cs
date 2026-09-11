using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace AERO_Console
{
    [Microsoft.VisualBasic.CompilerServices.DesignerGenerated()]
    public partial class frmHelp : Form
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
            components = new System.ComponentModel.Container();
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(frmHelp));
            pnlHeader = new Panel();
            lblHeader = new Label();
            btnClose = new Button();
            btnZoomIn = new Button();
            btnZoomOut = new Button();
            txt1 = new ModernFastColoredTextBox();
            ((System.ComponentModel.ISupportInitialize)txt1).BeginInit();
            pnlHeader.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 40;
            pnlHeader.Padding = new Padding(12, 6, 12, 6);
            pnlHeader.Controls.Add(lblHeader);
            pnlHeader.Controls.Add(btnZoomOut);
            pnlHeader.Controls.Add(btnZoomIn);
            pnlHeader.Controls.Add(btnClose);
            // 
            // lblHeader
            // 
            lblHeader.Dock = DockStyle.Left;
            lblHeader.Text = "AVL Reference Documentation";
            lblHeader.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblHeader.TextAlign = ContentAlignment.MiddleLeft;
            lblHeader.AutoSize = true;
            // 
            // btnClose
            // 
            btnClose.Dock = DockStyle.Right;
            btnClose.Text = "Close";
            btnClose.Width = 70;
            btnClose.Height = 28;
            btnClose.Click += (s, e) => Close();
            // 
            // btnZoomIn
            // 
            btnZoomIn.Dock = DockStyle.Right;
            btnZoomIn.Text = "A+";
            btnZoomIn.Width = 38;
            btnZoomIn.Height = 28;
            btnZoomIn.Click += (s, e) => { if (txt1.Zoom < 200) txt1.Zoom += 10; };
            // 
            // btnZoomOut
            // 
            btnZoomOut.Dock = DockStyle.Right;
            btnZoomOut.Text = "A-";
            btnZoomOut.Width = 38;
            btnZoomOut.Height = 28;
            btnZoomOut.Click += (s, e) => { if (txt1.Zoom > 50) txt1.Zoom -= 10; };
            // 
            // txt1
            // 
            txt1.AutoCompleteBrackets = true;
            txt1.AutoCompleteBracketsList = new char[] { '(', ')', '{', '}', '[', ']', '"', '"', '\'', '\'' };
            txt1.AutoIndentCharsPatterns = @"^\s*[\w\.]+(\s\w+)?\s*(?<range>=)\s*(?<range>[^;=]+);" + '\n' + @"^\s*(case|default)\s*[^:]*(" + @"?<range>:)\s*(?<range>[^;]+);";
            txt1.AutoScrollMinSize = new Size(47, 18);
            txt1.BackBrush = null;
            txt1.BookmarkColor = Color.Red;
            txt1.CharHeight = 18;
            txt1.CharWidth = 9;
            txt1.CommentPrefix = "!|#";
            txt1.Cursor = Cursors.IBeam;
            txt1.DisabledColor = Color.FromArgb(100, 180, 180, 180);
            txt1.Font = new Font("Consolas", 11.0f);
            txt1.HighlightingRangeType = FastColoredTextBoxNS.HighlightingRangeType.VisibleRange;
            txt1.Dock = DockStyle.Fill;
            txt1.IsReplaceMode = false;
            txt1.Location = new Point(0, 40);
            txt1.Name = "txt1";
            txt1.Paddings = new Padding(16, 12, 16, 12);
            txt1.ReservedCountOfLineNumberChars = 3;
            txt1.SelectionColor = Color.FromArgb(60, 0, 0, 255);
            txt1.ShowFoldingLines = true;
            txt1.Size = new Size(740, 560);
            txt1.TabIndex = 3;
            txt1.Zoom = 100;
            // 
            // frmHelp
            // 
            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(740, 600);
            Controls.Add(txt1);
            Controls.Add(pnlHeader);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "frmHelp";
            Text = "Help from AVL Documentation";
            ((System.ComponentModel.ISupportInitialize)txt1).EndInit();
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            Load += new EventHandler(frmHelp_Load);
            ResumeLayout(false);

        }
        internal Panel pnlHeader;
        internal Label lblHeader;
        internal Button btnZoomIn;
        internal Button btnZoomOut;
        internal Button btnClose;
        internal ModernFastColoredTextBox txt1;
    }
}
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace AERO_Console
{
    [Microsoft.VisualBasic.CompilerServices.DesignerGenerated()]
    public partial class frmInfo : Form
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

        [DebuggerStepThrough()]
        private void InitializeComponent()
        {
            pnlContainer = new Panel();
            pnlMain = new Panel();
            lblTitle = new Label();
            lblSubtitle = new Label();
            tlpShortcuts = new TableLayoutPanel();
            lblViewHeader = new Label();
            lblViewKeys = new Label();
            lblZoomHeader = new Label();
            lblZoomKeys = new Label();
            pnlBottom = new Panel();
            lblNote = new Label();
            btnClose = new Button();
            lb1 = new Label();
            pnlContainer.SuspendLayout();
            pnlMain.SuspendLayout();
            tlpShortcuts.SuspendLayout();
            pnlBottom.SuspendLayout();
            SuspendLayout();
            // 
            // pnlContainer
            // 
            pnlContainer.Dock = DockStyle.Fill;
            pnlContainer.Padding = new Padding(12);
            pnlContainer.Controls.Add(pnlMain);
            // 
            // pnlMain
            // 
            pnlMain.Dock = DockStyle.Fill;
            pnlMain.Padding = new Padding(16, 12, 16, 12);
            pnlMain.Controls.Add(tlpShortcuts);
            pnlMain.Controls.Add(lblSubtitle);
            pnlMain.Controls.Add(lblTitle);
            pnlMain.Controls.Add(pnlBottom);
            // 
            // lblTitle
            // 
            lblTitle.Dock = DockStyle.Top;
            lblTitle.Font = new Font("Segoe UI", 11.0f, FontStyle.Bold);
            lblTitle.Text = "AVL Graphics Keystroke Reference";
            lblTitle.Height = 24;
            // 
            // lblSubtitle
            // 
            lblSubtitle.Dock = DockStyle.Top;
            lblSubtitle.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblSubtitle.ForeColor = SystemColors.GrayText;
            lblSubtitle.Text = "Type these keystrokes while the AVL graphics window has focus:";
            lblSubtitle.Height = 22;
            // 
            // tlpShortcuts
            // 
            tlpShortcuts.Dock = DockStyle.Fill;
            tlpShortcuts.ColumnCount = 2;
            tlpShortcuts.RowCount = 2;
            tlpShortcuts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlpShortcuts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlpShortcuts.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
            tlpShortcuts.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tlpShortcuts.Margin = new Padding(0, 8, 0, 8);
            tlpShortcuts.Controls.Add(lblViewHeader, 0, 0);
            tlpShortcuts.Controls.Add(lblZoomHeader, 1, 0);
            tlpShortcuts.Controls.Add(lblViewKeys, 0, 1);
            tlpShortcuts.Controls.Add(lblZoomKeys, 1, 1);
            // 
            // lblViewHeader
            // 
            lblViewHeader.Dock = DockStyle.Fill;
            lblViewHeader.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblViewHeader.Text = "VIEW & ROTATION";
            // 
            // lblZoomHeader
            // 
            lblZoomHeader.Dock = DockStyle.Fill;
            lblZoomHeader.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblZoomHeader.Text = "ZOOM & PAN";
            // 
            // lblViewKeys
            // 
            lblViewKeys.Dock = DockStyle.Fill;
            lblViewKeys.Font = new Font("Consolas", 9.5f, FontStyle.Regular);
            lblViewKeys.Text = "[L] Left    /  [R] Right\n[U] Up      /  [D] Down\n[C] Clear view";
            // 
            // lblZoomKeys
            // 
            lblZoomKeys.Dock = DockStyle.Fill;
            lblZoomKeys.Font = new Font("Consolas", 9.5f, FontStyle.Regular);
            lblZoomKeys.Text = "[P] Pan from cursor\n[Z] Zoom on cursor\n[I] Ingress  /  [O] Outgress\n[E] Expand   /  [N] Normal";
            // 
            // pnlBottom
            // 
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 36;
            pnlBottom.Controls.Add(lblNote);
            pnlBottom.Controls.Add(btnClose);
            // 
            // lblNote
            // 
            lblNote.Dock = DockStyle.Left;
            lblNote.AutoSize = true;
            lblNote.Text = "Close this window to close plot";
            lblNote.TextAlign = ContentAlignment.MiddleLeft;
            lblNote.Location = new Point(0, 6);
            // 
            // btnClose
            // 
            btnClose.Dock = DockStyle.Right;
            btnClose.Text = "Close";
            btnClose.Width = 75;
            btnClose.Height = 28;
            btnClose.Click += (s, e) => Close();
            // 
            // lb1
            // 
            lb1.Visible = false;
            // 
            // frmInfo
            // 
            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(540, 310);
            Controls.Add(pnlContainer);
            Controls.Add(lb1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmInfo";
            StartPosition = FormStartPosition.Manual;
            Text = "AVL Keystroke Reference";
            FormClosing += new FormClosingEventHandler(frmInfo_FormClosing);
            Load += new EventHandler(frmInfo_Load);
            pnlContainer.ResumeLayout(false);
            pnlMain.ResumeLayout(false);
            tlpShortcuts.ResumeLayout(false);
            pnlBottom.ResumeLayout(false);
            ResumeLayout(false);
        }

        internal Panel pnlContainer;
        internal Panel pnlMain;
        internal Label lblTitle;
        internal Label lblSubtitle;
        internal TableLayoutPanel tlpShortcuts;
        internal Label lblViewHeader;
        internal Label lblViewKeys;
        internal Label lblZoomHeader;
        internal Label lblZoomKeys;
        internal Panel pnlBottom;
        internal Label lblNote;
        internal Button btnClose;
        internal Label lb1;
    }
}
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace AERO_Console
{
    [Microsoft.VisualBasic.CompilerServices.DesignerGenerated()]
    public partial class frmUpdate : Form
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
            pnlCard = new Panel();
            Label2 = new Label();
            lblStat = new Label();
            pbUpdate = new ProgressBar();
            pnlBottom = new Panel();
            btnClose = new Button();
            pnlContainer.SuspendLayout();
            pnlCard.SuspendLayout();
            pnlBottom.SuspendLayout();
            SuspendLayout();
            // 
            // pnlContainer
            // 
            pnlContainer.Dock = DockStyle.Fill;
            pnlContainer.Padding = new Padding(16);
            pnlContainer.Controls.Add(pnlCard);
            // 
            // pnlCard
            // 
            pnlCard.Dock = DockStyle.Fill;
            pnlCard.Padding = new Padding(18, 14, 18, 14);
            pnlCard.Controls.Add(pnlBottom);
            pnlCard.Controls.Add(pbUpdate);
            pnlCard.Controls.Add(lblStat);
            pnlCard.Controls.Add(Label2);
            // 
            // Label2
            // 
            Label2.Dock = DockStyle.Top;
            Label2.Font = new Font("Segoe UI", 11.0f, FontStyle.Bold);
            Label2.Location = new Point(18, 14);
            Label2.Name = "Label2";
            Label2.Size = new Size(432, 28);
            Label2.TabIndex = 4;
            Label2.Text = "Software Update";
            Label2.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblStat
            // 
            lblStat.Dock = DockStyle.Top;
            lblStat.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            lblStat.Location = new Point(18, 42);
            lblStat.Name = "lblStat";
            lblStat.Size = new Size(432, 42);
            lblStat.TabIndex = 11;
            lblStat.Text = "Checking for updates...";
            lblStat.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pbUpdate
            // 
            pbUpdate.Dock = DockStyle.Top;
            pbUpdate.Height = 6;
            pbUpdate.Location = new Point(18, 84);
            pbUpdate.Name = "pbUpdate";
            pbUpdate.Size = new Size(432, 6);
            pbUpdate.TabIndex = 12;
            pbUpdate.Value = 0;
            // 
            // pnlBottom
            // 
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 32;
            pnlBottom.Controls.Add(btnClose);
            // 
            // btnClose
            // 
            btnClose.Dock = DockStyle.Right;
            btnClose.Text = "Cancel";
            btnClose.Width = 80;
            btnClose.Height = 28;
            btnClose.Click += (s, e) => Close();
            // 
            // frmUpdate
            // 
            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(500, 195);
            Controls.Add(pnlContainer);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmUpdate";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Software Update";
            Load += new EventHandler(frmUpdate_Load);
            FormClosed += new FormClosedEventHandler(frmUpdate_FormClosed);
            pnlContainer.ResumeLayout(false);
            pnlCard.ResumeLayout(false);
            pnlBottom.ResumeLayout(false);
            ResumeLayout(false);
        }

        internal Panel pnlContainer;
        internal Panel pnlCard;
        internal Label Label2;
        internal Label lblStat;
        internal ProgressBar pbUpdate;
        internal Panel pnlBottom;
        internal Button btnClose;
    }
}
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

        // NOTE: The following procedure is required by the Windows Form Designer
        // It can be modified using the Windows Form Designer.  
        // Do not modify it using the code editor.
        [DebuggerStepThrough()]
        private void InitializeComponent()
        {
            bg1 = new System.ComponentModel.BackgroundWorker();
            Label2 = new Label();
            SaveFileDialog1 = new SaveFileDialog();
            lblStat = new Label();
            bg2 = new System.ComponentModel.BackgroundWorker();
            bg3 = new System.ComponentModel.BackgroundWorker();
            SuspendLayout();
            // 
            // Label2
            // 
            Label2.Dock = DockStyle.Top;
            Label2.Location = new Point(0, 0);
            Label2.Name = "Label2";
            Label2.Size = new Size(610, 24);
            Label2.TabIndex = 4;
            Label2.Text = "Status";
            Label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblStat
            // 
            lblStat.Dock = DockStyle.Fill;
            lblStat.Location = new Point(0, 24);
            lblStat.Name = "lblStat";
            lblStat.Size = new Size(610, 107);
            lblStat.TabIndex = 11;
            lblStat.Text = "Pending";
            lblStat.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // bg2
            // 
            bg2.WorkerReportsProgress = true;
            bg2.WorkerSupportsCancellation = true;
            // 
            // bg3
            // 
            bg3.WorkerReportsProgress = true;
            bg3.WorkerSupportsCancellation = true;
            // 
            // frmUpdate
            // 
            AutoScaleDimensions = new SizeF(6.0f, 13.0f);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(610, 131);
            Controls.Add(lblStat);
            Controls.Add(Label2);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmUpdate";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Checking for Update";
            Load += new EventHandler(frmUpdate_Load);
            FormClosed += new FormClosedEventHandler(frmUpdate_FormClosed);
            ResumeLayout(false);

        }

        internal System.ComponentModel.BackgroundWorker bg1;
        internal Label Label2;
        internal SaveFileDialog SaveFileDialog1;
        internal Label lblStat;
        internal System.ComponentModel.BackgroundWorker bg2;
        internal System.ComponentModel.BackgroundWorker bg3;
    }
}
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
            Label2 = new Label();
            lblStat = new Label();
            SuspendLayout();
            // 
            // Label2
            // 
            Label2.Dock = DockStyle.Top;
            Label2.Font = new Font("Segoe UI", 10.0f, FontStyle.Bold, GraphicsUnit.Point, 0);
            Label2.Location = new Point(0, 0);
            Label2.Name = "Label2";
            Label2.Size = new Size(610, 32);
            Label2.TabIndex = 4;
            Label2.Text = "Status";
            Label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblStat
            // 
            lblStat.Dock = DockStyle.Fill;
            lblStat.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblStat.Location = new Point(0, 32);
            lblStat.Name = "lblStat";
            lblStat.Size = new Size(610, 108);
            lblStat.TabIndex = 11;
            lblStat.Text = "Pending";
            lblStat.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // frmUpdate
            // 
            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(610, 140);
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

        internal Label Label2;
        internal Label lblStat;
    }
}
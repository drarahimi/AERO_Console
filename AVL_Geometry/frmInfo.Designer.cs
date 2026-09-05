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

        // NOTE: The following procedure is required by the Windows Form Designer
        // It can be modified using the Windows Form Designer.  
        // Do not modify it using the code editor.
        [DebuggerStepThrough()]
        private void InitializeComponent()
        {
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(frmInfo));
            lb1 = new Label();
            SuspendLayout();
            // 
            // lb1
            // 
            lb1.BackColor = Color.Black;
            lb1.Dock = DockStyle.Fill;
            lb1.Font = new Font("Consolas", 12.0f, FontStyle.Regular, GraphicsUnit.Point, 0);
            lb1.ForeColor = Color.White;
            lb1.Location = new Point(0, 0);
            lb1.Name = "lb1";
            lb1.Padding = new Padding(12);
            lb1.Size = new Size(520, 260);
            lb1.TabIndex = 0;
            lb1.Text = resources.GetString("lb1.Text");
            // 
            // frmInfo
            // 
            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(520, 260);
            Controls.Add(lb1);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "frmInfo";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Key Input Options";
            FormClosing += new FormClosingEventHandler(frmInfo_FormClosing);
            Load += new EventHandler(frmInfo_Load);
            ResumeLayout(false);

        }

        internal Label lb1;
    }
}
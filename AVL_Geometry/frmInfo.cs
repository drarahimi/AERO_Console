using System;
using System.Windows.Forms;

namespace AERO_Console
{
    public partial class frmInfo
    {
        public frmInfo()
        {
            InitializeComponent();
        }
        private void frmInfo_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.None)
            {
                My.MyProject.Forms.frmMain.RestartConsoleToolStripMenuItem.PerformClick();
            }
        }

        private void frmInfo_Load(object sender, EventArgs e)
        {
            lb1.Dock = DockStyle.Fill;
            Icon = My.MyProject.Forms.frmMain.Icon;
            Left = Screen.PrimaryScreen.WorkingArea.Width - Width;
            UI.Theme.Current.ApplyTo(this);
        }
    }
}
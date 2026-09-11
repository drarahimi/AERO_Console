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
            Icon = My.MyProject.Forms.frmMain.Icon;
            Left = Math.Max(20, Screen.PrimaryScreen.WorkingArea.Width - Width - 30);
            Top = Math.Max(40, Screen.PrimaryScreen.WorkingArea.Height - Height - 80);

            var theme = UI.Theme.Current;
            UI.Theme.UseImmersiveDarkMode(Handle, theme.IsDark);
            theme.ApplyTo(this);
            theme.StyleCard(pnlMain, UI.Theme.SpacingMd);
            theme.StyleButton(btnClose);
            lblTitle.ForeColor = theme.Accent;
            lblViewHeader.ForeColor = theme.Accent;
            lblZoomHeader.ForeColor = theme.Accent;
            theme.StyleBadge(lblNote);
        }
    }
}
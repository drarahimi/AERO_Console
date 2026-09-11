using System;
using System.Drawing;
using System.Windows.Forms;

namespace AERO_Console
{
    public partial class frmHelp
    {
        public frmHelp()
        {
            InitializeComponent();
        }
        private void frmHelp_Load(object sender, EventArgs e)
        {
            Icon = My.MyProject.Forms.frmMain.Icon;
            var theme = UI.Theme.Current;
            UI.Theme.UseImmersiveDarkMode(Handle, theme.IsDark);
            theme.ApplyTo(this);
            theme.StyleCard(pnlHeader, 0);
            theme.StyleButton(btnZoomIn);
            theme.StyleButton(btnZoomOut);
            theme.StyleButton(btnClose);
            lblHeader.ForeColor = theme.Foreground;

            if (theme.IsDark)
            {
                txt1.BackColor = Color.FromArgb(28, 28, 30);
                txt1.ForeColor = Color.Gainsboro;
                txt1.LineNumberColor = Color.FromArgb(110, 110, 110);
                txt1.IndentBackColor = Color.FromArgb(34, 34, 36);
            }
            else
            {
                txt1.BackColor = Color.White;
                txt1.ForeColor = Color.FromArgb(30, 30, 30);
                txt1.LineNumberColor = Color.FromArgb(140, 140, 140);
                txt1.IndentBackColor = Color.FromArgb(245, 245, 247);
            }
        }
    }
}
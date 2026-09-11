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
            txt1.Dock = DockStyle.Fill;
            if (!(frmMain.systemFont == null))
            {
                frmMain.SetAllControlsFont(Controls, frmMain.systemFont);
            }
            else
            {
                var newFont = new Font(FontFamily.GenericMonospace, My.MyProject.Forms.frmMain.Font.Size);
                frmMain.SetAllControlsFont(Controls, newFont);
            }

            var theme = UI.Theme.Current;
            theme.ApplyTo(this);
            if (theme.IsDark)
            {
                txt1.BackColor = Color.FromArgb(30, 30, 30);
                txt1.ForeColor = Color.Gainsboro;
                txt1.LineNumberColor = Color.FromArgb(100, 100, 100);
                txt1.IndentBackColor = Color.FromArgb(37, 37, 38);
            }
        }
    }
}
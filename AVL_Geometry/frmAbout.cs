using System;
using System.Drawing;
using System.Windows.Forms;

namespace AERO_Console
{
    public sealed partial class frmAbout
    {
        public frmAbout()
        {
            InitializeComponent();
        }

        private void frmAbout_Load(object sender, EventArgs e)
        {
            Icon = My.MyProject.Forms.frmMain.Icon;
            // Set the title of the form.
            string ApplicationTitle;
            if (!string.IsNullOrEmpty(My.MyProject.Application.Info.Title))
            {
                ApplicationTitle = My.MyProject.Application.Info.Title;
            }
            else
            {
                ApplicationTitle = System.IO.Path.GetFileNameWithoutExtension(My.MyProject.Application.Info.AssemblyName);
            }
            Text = string.Format("About {0}", ApplicationTitle);
            // Initialize all of the text displayed on the About Box.
            // TODO: Customize the application's assembly information in the "Application" pane of the project 
            // properties dialog (under the "Project" menu).
            LabelProductName.Text = My.MyProject.Application.Info.ProductName;
            LabelVersion.Text = string.Format("Version {0}", My.MyProject.Application.Info.Version.ToString());
            LabelCopyright.Text = My.MyProject.Application.Info.Copyright;
            LabelCompanyName.Text = My.MyProject.Application.Info.CompanyName;
            TextBoxDescription.Text = My.MyProject.Application.Info.Description;

            if (My.MySettingsProperty.Settings.DarkTheme)
            {
                BackColor = Color.FromArgb(32, 32, 32);
                ForeColor = Color.White;
                LabelProductName.ForeColor = Color.White;
                LabelVersion.ForeColor = Color.Gainsboro;
                LabelCopyright.ForeColor = Color.DarkGray;
                LabelCompanyName.ForeColor = Color.DarkGray;
                TextBoxDescription.BackColor = Color.FromArgb(40, 40, 40);
                TextBoxDescription.ForeColor = Color.Gainsboro;
                OKButton.BackColor = Color.FromArgb(50, 50, 54);
                OKButton.ForeColor = Color.White;
                OKButton.FlatStyle = FlatStyle.Flat;
                OKButton.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 74);
            }
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            Close();
        }

    }
}
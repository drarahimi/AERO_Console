using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace AERO_Console
{
    [Microsoft.VisualBasic.CompilerServices.DesignerGenerated()]
    public partial class frmAbout : Form
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

        internal TableLayoutPanel TableLayoutPanel;
        internal PictureBox LogoPictureBox;
        internal Label LabelProductName;
        internal Label LabelVersion;
        internal Label LabelCompanyName;
        internal TextBox TextBoxDescription;
        internal Button OKButton;
        internal Label LabelCopyright;

        // Required by the Windows Form Designer
        private System.ComponentModel.IContainer components;

        // NOTE: The following procedure is required by the Windows Form Designer
        // It can be modified using the Windows Form Designer.  
        // Do not modify it using the code editor.
        [DebuggerStepThrough()]
        private void InitializeComponent()
        {
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(frmAbout));
            TableLayoutPanel = new TableLayoutPanel();
            LogoPictureBox = new PictureBox();
            LabelProductName = new Label();
            LabelVersion = new Label();
            LabelCopyright = new Label();
            LabelCompanyName = new Label();
            TextBoxDescription = new TextBox();
            OKButton = new Button();
            OKButton.Click += new EventHandler(OKButton_Click);
            TableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)LogoPictureBox).BeginInit();
            SuspendLayout();
            // 
            // TableLayoutPanel
            // 
            TableLayoutPanel.BackColor = Color.Transparent;
            TableLayoutPanel.ColumnCount = 2;
            TableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.0f));
            TableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 67.0f));
            TableLayoutPanel.Controls.Add(LogoPictureBox, 0, 0);
            TableLayoutPanel.Controls.Add(LabelProductName, 1, 0);
            TableLayoutPanel.Controls.Add(LabelVersion, 1, 1);
            TableLayoutPanel.Controls.Add(LabelCopyright, 1, 2);
            TableLayoutPanel.Controls.Add(LabelCompanyName, 1, 3);
            TableLayoutPanel.Controls.Add(TextBoxDescription, 1, 4);
            TableLayoutPanel.Controls.Add(OKButton, 1, 5);
            TableLayoutPanel.Dock = DockStyle.Fill;
            TableLayoutPanel.Location = new Point(24, 24);
            TableLayoutPanel.Name = "TableLayoutPanel";
            TableLayoutPanel.RowCount = 6;
            TableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36.0f));
            TableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24.0f));
            TableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22.0f));
            TableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22.0f));
            TableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f));
            TableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42.0f));
            TableLayoutPanel.Size = new Size(492, 312);
            TableLayoutPanel.TabIndex = 0;
            // 
            // LogoPictureBox
            // 
            LogoPictureBox.Dock = DockStyle.Fill;
            LogoPictureBox.Image = My.Resources.Resources.logo;
            LogoPictureBox.Location = new Point(3, 3);
            LogoPictureBox.Name = "LogoPictureBox";
            TableLayoutPanel.SetRowSpan(LogoPictureBox, 6);
            LogoPictureBox.Size = new Size(156, 306);
            LogoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            LogoPictureBox.TabIndex = 12;
            LogoPictureBox.TabStop = false;
            // 
            // LabelProductName
            // 
            LabelProductName.Dock = DockStyle.Fill;
            LabelProductName.Font = new Font("Segoe UI", 13.0f, FontStyle.Bold, GraphicsUnit.Point, 0);
            LabelProductName.ForeColor = Color.FromArgb(64, 64, 64);
            LabelProductName.Location = new Point(168, 0);
            LabelProductName.Margin = new Padding(6, 0, 3, 0);
            LabelProductName.Name = "LabelProductName";
            LabelProductName.Size = new Size(321, 36);
            LabelProductName.TabIndex = 19;
            LabelProductName.Text = "Product Name";
            LabelProductName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // LabelVersion
            // 
            LabelVersion.Dock = DockStyle.Fill;
            LabelVersion.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold, GraphicsUnit.Point, 0);
            LabelVersion.Location = new Point(168, 36);
            LabelVersion.Margin = new Padding(6, 0, 3, 0);
            LabelVersion.Name = "LabelVersion";
            LabelVersion.Size = new Size(321, 24);
            LabelVersion.TabIndex = 0;
            LabelVersion.Text = "Version";
            LabelVersion.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // LabelCopyright
            // 
            LabelCopyright.Dock = DockStyle.Fill;
            LabelCopyright.Font = new Font("Segoe UI", 8.75f, FontStyle.Regular, GraphicsUnit.Point, 0);
            LabelCopyright.ForeColor = SystemColors.GrayText;
            LabelCopyright.Location = new Point(168, 60);
            LabelCopyright.Margin = new Padding(6, 0, 3, 0);
            LabelCopyright.Name = "LabelCopyright";
            LabelCopyright.Size = new Size(321, 22);
            LabelCopyright.TabIndex = 21;
            LabelCopyright.Text = "Copyright";
            LabelCopyright.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // LabelCompanyName
            // 
            LabelCompanyName.Dock = DockStyle.Fill;
            LabelCompanyName.Font = new Font("Segoe UI", 8.75f, FontStyle.Regular, GraphicsUnit.Point, 0);
            LabelCompanyName.ForeColor = SystemColors.GrayText;
            LabelCompanyName.Location = new Point(168, 82);
            LabelCompanyName.Margin = new Padding(6, 0, 3, 0);
            LabelCompanyName.Name = "LabelCompanyName";
            LabelCompanyName.Size = new Size(321, 22);
            LabelCompanyName.TabIndex = 22;
            LabelCompanyName.Text = "Company Name";
            LabelCompanyName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // TextBoxDescription
            // 
            TextBoxDescription.BackColor = Color.White;
            TextBoxDescription.BorderStyle = BorderStyle.None;
            TextBoxDescription.Dock = DockStyle.Fill;
            TextBoxDescription.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point, 0);
            TextBoxDescription.Location = new Point(168, 110);
            TextBoxDescription.Margin = new Padding(6, 6, 3, 6);
            TextBoxDescription.Multiline = true;
            TextBoxDescription.Name = "TextBoxDescription";
            TextBoxDescription.ReadOnly = true;
            TextBoxDescription.Size = new Size(321, 154);
            TextBoxDescription.TabIndex = 23;
            TextBoxDescription.TabStop = false;
            TextBoxDescription.Text = resources.GetString("TextBoxDescription.Text");
            // 
            // OKButton
            // 
            OKButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            OKButton.DialogResult = DialogResult.Cancel;
            OKButton.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point, 0);
            OKButton.Location = new Point(399, 278);
            OKButton.Name = "OKButton";
            OKButton.Size = new Size(90, 30);
            OKButton.TabIndex = 24;
            OKButton.Text = "&OK";
            OKButton.UseVisualStyleBackColor = false;
            // 
            // frmAbout
            // 
            AcceptButton = OKButton;
            AutoScaleDimensions = new SizeF(96.0f, 96.0f);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;
            CancelButton = OKButton;
            ClientSize = new Size(540, 360);
            Controls.Add(TableLayoutPanel);
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmAbout";
            Padding = new Padding(24);
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "About";
            TableLayoutPanel.ResumeLayout(false);
            TableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)LogoPictureBox).EndInit();
            Load += new EventHandler(frmAbout_Load);
            ResumeLayout(false);

        }

    }
}
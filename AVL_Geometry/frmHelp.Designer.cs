using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace AERO_Console
{
    [Microsoft.VisualBasic.CompilerServices.DesignerGenerated()]
    public partial class frmHelp : Form
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
            components = new System.ComponentModel.Container();
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(frmHelp));
            txt1 = new ModernFastColoredTextBox();
            ((System.ComponentModel.ISupportInitialize)txt1).BeginInit();
            SuspendLayout();
            // 
            // txt1
            // 
            txt1.AutoCompleteBrackets = true;
            txt1.AutoCompleteBracketsList = new char[] { '(', ')', '{', '}', '[', ']', '"', '"', '\'', '\'' };
            txt1.AutoIndentCharsPatterns = @"^\s*[\w\.]+(\s\w+)?\s*(?<range>=)\s*(?<range>[^;=]+);" + '\n' + @"^\s*(case|default)\s*[^:]*(" + @"?<range>:)\s*(?<range>[^;]+);";
            txt1.AutoScrollMinSize = new Size(47, 18);
            txt1.BackBrush = null;
            txt1.BookmarkColor = Color.Red;
            txt1.CharHeight = 18;
            txt1.CharWidth = 9;
            txt1.CommentPrefix = "!|#";
            txt1.Cursor = Cursors.IBeam;
            // Me.txt1.DefaultMarkerSize = 8
            txt1.DisabledColor = Color.FromArgb(100, 180, 180, 180);
            txt1.Font = new Font("Consolas", 12.0f);
            txt1.HighlightingRangeType = FastColoredTextBoxNS.HighlightingRangeType.VisibleRange;
            txt1.IsReplaceMode = false;
            txt1.Location = new Point(26, 33);
            txt1.Name = "txt1";
            txt1.Paddings = new Padding(0);
            txt1.ReservedCountOfLineNumberChars = 3;
            txt1.SelectionColor = Color.FromArgb(60, 0, 0, 255);
            // Me.txt1.ServiceColors = CType(resources.GetObject("txt1.ServiceColors"), FastColoredTextBoxNS.ServiceColors)
            txt1.ShowFoldingLines = true;
            txt1.Size = new Size(136, 153);
            txt1.TabIndex = 3;
            txt1.Zoom = 100;
            // 
            // frmHelp
            // 
            AutoScaleDimensions = new SizeF(6.0f, 13.0f);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(623, 528);
            Controls.Add(txt1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "frmHelp";
            Text = "Help from AVL Documentation";
            ((System.ComponentModel.ISupportInitialize)txt1).EndInit();
            Load += new EventHandler(frmHelp_Load);
            ResumeLayout(false);

        }
        internal ModernFastColoredTextBox txt1;
    }
}
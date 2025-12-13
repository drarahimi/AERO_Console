<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmHelp
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmHelp))
        Me.txt1 = New ModernFastColoredTextBox
        CType(Me.txt1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'txt1
        '
        Me.txt1.AutoCompleteBrackets = True
        Me.txt1.AutoCompleteBracketsList = New Char() {Global.Microsoft.VisualBasic.ChrW(40), Global.Microsoft.VisualBasic.ChrW(41), Global.Microsoft.VisualBasic.ChrW(123), Global.Microsoft.VisualBasic.ChrW(125), Global.Microsoft.VisualBasic.ChrW(91), Global.Microsoft.VisualBasic.ChrW(93), Global.Microsoft.VisualBasic.ChrW(34), Global.Microsoft.VisualBasic.ChrW(34), Global.Microsoft.VisualBasic.ChrW(39), Global.Microsoft.VisualBasic.ChrW(39)}
        Me.txt1.AutoIndentCharsPatterns = "^\s*[\w\.]+(\s\w+)?\s*(?<range>=)\s*(?<range>[^;=]+);" & Global.Microsoft.VisualBasic.ChrW(10) & "^\s*(case|default)\s*[^:]*(" &
    "?<range>:)\s*(?<range>[^;]+);"
        Me.txt1.AutoScrollMinSize = New System.Drawing.Size(47, 18)
        Me.txt1.BackBrush = Nothing
        Me.txt1.BookmarkColor = System.Drawing.Color.Red
        Me.txt1.CharHeight = 18
        Me.txt1.CharWidth = 9
        Me.txt1.CommentPrefix = "!|#"
        Me.txt1.Cursor = System.Windows.Forms.Cursors.IBeam
        'Me.txt1.DefaultMarkerSize = 8
        Me.txt1.DisabledColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(180, Byte), Integer), CType(CType(180, Byte), Integer), CType(CType(180, Byte), Integer))
        Me.txt1.Font = New System.Drawing.Font("Consolas", 12.0!)
        Me.txt1.HighlightingRangeType = FastColoredTextBoxNS.HighlightingRangeType.VisibleRange
        Me.txt1.IsReplaceMode = False
        Me.txt1.Location = New System.Drawing.Point(26, 33)
        Me.txt1.Name = "txt1"
        Me.txt1.Paddings = New System.Windows.Forms.Padding(0)
        Me.txt1.ReservedCountOfLineNumberChars = 3
        Me.txt1.SelectionColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer))
        'Me.txt1.ServiceColors = CType(resources.GetObject("txt1.ServiceColors"), FastColoredTextBoxNS.ServiceColors)
        Me.txt1.ShowFoldingLines = True
        Me.txt1.Size = New System.Drawing.Size(136, 153)
        Me.txt1.TabIndex = 3
        Me.txt1.Zoom = 100
        '
        'frmHelp
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(623, 528)
        Me.Controls.Add(Me.txt1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "frmHelp"
        Me.Text = "Help from AVL Documentation"
        CType(Me.txt1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents txt1 As ModernFastColoredTextBox
End Class

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class frmMain
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmMain))
        Me.txtLog = New System.Windows.Forms.TextBox()
        Me.txtCommand = New System.Windows.Forms.TextBox()
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.FileToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.OpenCurrentDirectoryToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolsToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.AirplaneDesignToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.RestartConsoleToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.HelpToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.AboutToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.CheckForUpdatesToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStrip1 = New System.Windows.Forms.ToolStrip()
        Me.btnAVL = New System.Windows.Forms.ToolStripButton()
        Me.btnXFoil = New System.Windows.Forms.ToolStripButton()
        Me.btnDesigner = New System.Windows.Forms.ToolStripButton()
        Me.DisplayToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.FontToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.fd1 = New System.Windows.Forms.FontDialog()
        Me.MenuStrip1.SuspendLayout()
        Me.ToolStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'txtLog
        '
        Me.txtLog.BackColor = System.Drawing.Color.Black
        Me.txtLog.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtLog.ForeColor = System.Drawing.Color.White
        Me.txtLog.Location = New System.Drawing.Point(37, 66)
        Me.txtLog.Multiline = True
        Me.txtLog.Name = "txtLog"
        Me.txtLog.ReadOnly = True
        Me.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txtLog.Size = New System.Drawing.Size(764, 457)
        Me.txtLog.TabIndex = 1
        Me.txtLog.TabStop = False
        Me.txtLog.WordWrap = False
        '
        'txtCommand
        '
        Me.txtCommand.AcceptsReturn = True
        Me.txtCommand.BackColor = System.Drawing.Color.Black
        Me.txtCommand.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.txtCommand.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtCommand.ForeColor = System.Drawing.Color.White
        Me.txtCommand.Location = New System.Drawing.Point(0, 529)
        Me.txtCommand.Name = "txtCommand"
        Me.txtCommand.Size = New System.Drawing.Size(1026, 26)
        Me.txtCommand.TabIndex = 1
        '
        'MenuStrip1
        '
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.FileToolStripMenuItem, Me.ToolsToolStripMenuItem, Me.DisplayToolStripMenuItem, Me.HelpToolStripMenuItem})
        Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.Size = New System.Drawing.Size(1026, 24)
        Me.MenuStrip1.TabIndex = 2
        Me.MenuStrip1.Text = "MenuStrip1"
        '
        'FileToolStripMenuItem
        '
        Me.FileToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.OpenCurrentDirectoryToolStripMenuItem})
        Me.FileToolStripMenuItem.Name = "FileToolStripMenuItem"
        Me.FileToolStripMenuItem.Size = New System.Drawing.Size(37, 20)
        Me.FileToolStripMenuItem.Text = "File"
        '
        'OpenCurrentDirectoryToolStripMenuItem
        '
        Me.OpenCurrentDirectoryToolStripMenuItem.Name = "OpenCurrentDirectoryToolStripMenuItem"
        Me.OpenCurrentDirectoryToolStripMenuItem.Size = New System.Drawing.Size(197, 22)
        Me.OpenCurrentDirectoryToolStripMenuItem.Text = "Open Current Directory"
        '
        'ToolsToolStripMenuItem
        '
        Me.ToolsToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.AirplaneDesignToolStripMenuItem, Me.RestartConsoleToolStripMenuItem})
        Me.ToolsToolStripMenuItem.Name = "ToolsToolStripMenuItem"
        Me.ToolsToolStripMenuItem.Size = New System.Drawing.Size(46, 20)
        Me.ToolsToolStripMenuItem.Text = "Tools"
        '
        'AirplaneDesignToolStripMenuItem
        '
        Me.AirplaneDesignToolStripMenuItem.Name = "AirplaneDesignToolStripMenuItem"
        Me.AirplaneDesignToolStripMenuItem.Size = New System.Drawing.Size(175, 22)
        Me.AirplaneDesignToolStripMenuItem.Text = "Geometry Designer"
        '
        'RestartConsoleToolStripMenuItem
        '
        Me.RestartConsoleToolStripMenuItem.Name = "RestartConsoleToolStripMenuItem"
        Me.RestartConsoleToolStripMenuItem.Size = New System.Drawing.Size(175, 22)
        Me.RestartConsoleToolStripMenuItem.Text = "Restart Console"
        '
        'HelpToolStripMenuItem
        '
        Me.HelpToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.AboutToolStripMenuItem, Me.CheckForUpdatesToolStripMenuItem})
        Me.HelpToolStripMenuItem.Name = "HelpToolStripMenuItem"
        Me.HelpToolStripMenuItem.Size = New System.Drawing.Size(44, 20)
        Me.HelpToolStripMenuItem.Text = "Help"
        '
        'AboutToolStripMenuItem
        '
        Me.AboutToolStripMenuItem.Name = "AboutToolStripMenuItem"
        Me.AboutToolStripMenuItem.Size = New System.Drawing.Size(171, 22)
        Me.AboutToolStripMenuItem.Text = "About"
        '
        'CheckForUpdatesToolStripMenuItem
        '
        Me.CheckForUpdatesToolStripMenuItem.Name = "CheckForUpdatesToolStripMenuItem"
        Me.CheckForUpdatesToolStripMenuItem.Size = New System.Drawing.Size(171, 22)
        Me.CheckForUpdatesToolStripMenuItem.Text = "Check for Updates"
        '
        'ToolStrip1
        '
        Me.ToolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.btnAVL, Me.btnXFoil, Me.btnDesigner})
        Me.ToolStrip1.Location = New System.Drawing.Point(0, 24)
        Me.ToolStrip1.Name = "ToolStrip1"
        Me.ToolStrip1.Size = New System.Drawing.Size(1026, 25)
        Me.ToolStrip1.TabIndex = 3
        Me.ToolStrip1.Text = "ToolStrip1"
        '
        'btnAVL
        '
        Me.btnAVL.Checked = True
        Me.btnAVL.CheckOnClick = True
        Me.btnAVL.CheckState = System.Windows.Forms.CheckState.Checked
        Me.btnAVL.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnAVL.Image = CType(resources.GetObject("btnAVL.Image"), System.Drawing.Image)
        Me.btnAVL.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnAVL.Name = "btnAVL"
        Me.btnAVL.Size = New System.Drawing.Size(31, 22)
        Me.btnAVL.Text = "AVL"
        '
        'btnXFoil
        '
        Me.btnXFoil.CheckOnClick = True
        Me.btnXFoil.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnXFoil.Image = CType(resources.GetObject("btnXFoil.Image"), System.Drawing.Image)
        Me.btnXFoil.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnXFoil.Name = "btnXFoil"
        Me.btnXFoil.Size = New System.Drawing.Size(37, 22)
        Me.btnXFoil.Text = "XFoil"
        Me.btnXFoil.Visible = False
        '
        'btnDesigner
        '
        Me.btnDesigner.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right
        Me.btnDesigner.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnDesigner.Image = CType(resources.GetObject("btnDesigner.Image"), System.Drawing.Image)
        Me.btnDesigner.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnDesigner.Name = "btnDesigner"
        Me.btnDesigner.Size = New System.Drawing.Size(112, 22)
        Me.btnDesigner.Text = "Geometry Designer"
        '
        'DisplayToolStripMenuItem
        '
        Me.DisplayToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.FontToolStripMenuItem})
        Me.DisplayToolStripMenuItem.Name = "DisplayToolStripMenuItem"
        Me.DisplayToolStripMenuItem.Size = New System.Drawing.Size(57, 20)
        Me.DisplayToolStripMenuItem.Text = "Display"
        '
        'FontToolStripMenuItem
        '
        Me.FontToolStripMenuItem.Name = "FontToolStripMenuItem"
        Me.FontToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.FontToolStripMenuItem.Text = "Font"
        '
        'frmMain
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1026, 555)
        Me.Controls.Add(Me.txtLog)
        Me.Controls.Add(Me.ToolStrip1)
        Me.Controls.Add(Me.txtCommand)
        Me.Controls.Add(Me.MenuStrip1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MainMenuStrip = Me.MenuStrip1
        Me.Name = "frmMain"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "AVL - Console"
        Me.MenuStrip1.ResumeLayout(False)
        Me.MenuStrip1.PerformLayout()
        Me.ToolStrip1.ResumeLayout(False)
        Me.ToolStrip1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents txtLog As TextBox
    Friend WithEvents txtCommand As TextBox
    Friend WithEvents MenuStrip1 As MenuStrip
    Friend WithEvents FileToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolsToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents AirplaneDesignToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents HelpToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents AboutToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents OpenCurrentDirectoryToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents RestartConsoleToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents CheckForUpdatesToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolStrip1 As ToolStrip
    Friend WithEvents btnAVL As ToolStripButton
    Friend WithEvents btnXFoil As ToolStripButton
    Friend WithEvents btnDesigner As ToolStripButton
    Friend WithEvents DisplayToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents FontToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents fd1 As FontDialog
End Class

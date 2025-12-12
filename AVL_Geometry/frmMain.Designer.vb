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
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.FileToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.OpenCurrentDirectoryToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolsToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.AirplaneDesignToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.RestartConsoleToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.DisplayToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.FontToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.HelpToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.AVLHelpToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.AboutToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.CheckForUpdatesToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStrip1 = New System.Windows.Forms.ToolStrip()
        Me.ToolStripLabel1 = New System.Windows.Forms.ToolStripLabel()
        Me.txtName = New System.Windows.Forms.ToolStripComboBox()
        Me.fd1 = New System.Windows.Forms.FontDialog()
        Me.LayoutTable = New System.Windows.Forms.TableLayoutPanel()
        Me.txtLog = New System.Windows.Forms.TextBox()
        Me.txtCommand = New System.Windows.Forms.TextBox()
        Me.btnGeometry = New System.Windows.Forms.ToolStripButton()
        Me.btnMass = New System.Windows.Forms.ToolStripButton()
        Me.btnRun = New System.Windows.Forms.ToolStripButton()
        Me.btnDesigner = New System.Windows.Forms.ToolStripButton()
        Me.MenuStrip1.SuspendLayout()
        Me.ToolStrip1.SuspendLayout()
        Me.LayoutTable.SuspendLayout()
        Me.SuspendLayout()
        '
        'MenuStrip1
        '
        Me.MenuStrip1.BackColor = System.Drawing.Color.WhiteSmoke
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.FileToolStripMenuItem, Me.ToolsToolStripMenuItem, Me.DisplayToolStripMenuItem, Me.HelpToolStripMenuItem})
        Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System
        Me.MenuStrip1.Size = New System.Drawing.Size(1026, 24)
        Me.MenuStrip1.TabIndex = 2
        Me.MenuStrip1.Text = "MenuStrip1"
        '
        'FileToolStripMenuItem
        '
        Me.FileToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.OpenCurrentDirectoryToolStripMenuItem})
        Me.FileToolStripMenuItem.Name = "FileToolStripMenuItem"
        Me.FileToolStripMenuItem.Size = New System.Drawing.Size(37, 20)
        Me.FileToolStripMenuItem.Text = "&File"
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
        Me.ToolsToolStripMenuItem.Size = New System.Drawing.Size(47, 20)
        Me.ToolsToolStripMenuItem.Text = "&Tools"
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
        'DisplayToolStripMenuItem
        '
        Me.DisplayToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.FontToolStripMenuItem})
        Me.DisplayToolStripMenuItem.Name = "DisplayToolStripMenuItem"
        Me.DisplayToolStripMenuItem.Size = New System.Drawing.Size(57, 20)
        Me.DisplayToolStripMenuItem.Text = "&Display"
        '
        'FontToolStripMenuItem
        '
        Me.FontToolStripMenuItem.Name = "FontToolStripMenuItem"
        Me.FontToolStripMenuItem.Size = New System.Drawing.Size(98, 22)
        Me.FontToolStripMenuItem.Text = "Font"
        '
        'HelpToolStripMenuItem
        '
        Me.HelpToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.AVLHelpToolStripMenuItem, Me.AboutToolStripMenuItem, Me.CheckForUpdatesToolStripMenuItem})
        Me.HelpToolStripMenuItem.Name = "HelpToolStripMenuItem"
        Me.HelpToolStripMenuItem.Size = New System.Drawing.Size(44, 20)
        Me.HelpToolStripMenuItem.Text = "&Help"
        '
        'AVLHelpToolStripMenuItem
        '
        Me.AVLHelpToolStripMenuItem.Name = "AVLHelpToolStripMenuItem"
        Me.AVLHelpToolStripMenuItem.Size = New System.Drawing.Size(171, 22)
        Me.AVLHelpToolStripMenuItem.Text = "AVL Help"
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
        Me.ToolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripLabel1, Me.txtName, Me.btnGeometry, Me.btnMass, Me.btnRun, Me.btnDesigner})
        Me.ToolStrip1.Location = New System.Drawing.Point(0, 24)
        Me.ToolStrip1.Name = "ToolStrip1"
        Me.ToolStrip1.Padding = New System.Windows.Forms.Padding(5, 0, 1, 0)
        Me.ToolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional
        Me.ToolStrip1.Size = New System.Drawing.Size(1026, 25)
        Me.ToolStrip1.TabIndex = 3
        Me.ToolStrip1.Text = "ToolStrip1"
        '
        'ToolStripLabel1
        '
        Me.ToolStripLabel1.ForeColor = System.Drawing.Color.DimGray
        Me.ToolStripLabel1.Name = "ToolStripLabel1"
        Me.ToolStripLabel1.Size = New System.Drawing.Size(80, 22)
        Me.ToolStripLabel1.Text = "Project name:"
        '
        'txtName
        '
        Me.txtName.Name = "txtName"
        Me.txtName.Size = New System.Drawing.Size(250, 25)
        '
        'LayoutTable
        '
        Me.LayoutTable.ColumnCount = 1
        Me.LayoutTable.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.LayoutTable.Controls.Add(Me.txtLog, 0, 0)
        Me.LayoutTable.Controls.Add(Me.txtCommand, 0, 1)
        Me.LayoutTable.Dock = System.Windows.Forms.DockStyle.Fill
        Me.LayoutTable.Location = New System.Drawing.Point(0, 49)
        Me.LayoutTable.Name = "LayoutTable"
        Me.LayoutTable.Padding = New System.Windows.Forms.Padding(5)
        Me.LayoutTable.RowCount = 2
        Me.LayoutTable.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.LayoutTable.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35.0!))
        Me.LayoutTable.Size = New System.Drawing.Size(1026, 506)
        Me.LayoutTable.TabIndex = 4
        '
        'txtLog
        '
        Me.txtLog.BackColor = System.Drawing.Color.FromArgb(CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer), CType(CType(30, Byte), Integer))
        Me.txtLog.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.txtLog.Dock = System.Windows.Forms.DockStyle.Fill
        Me.txtLog.Font = New System.Drawing.Font("Consolas", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtLog.ForeColor = System.Drawing.Color.Gainsboro
        Me.txtLog.Location = New System.Drawing.Point(5, 5)
        Me.txtLog.Margin = New System.Windows.Forms.Padding(0, 0, 0, 5)
        Me.txtLog.Multiline = True
        Me.txtLog.Name = "txtLog"
        Me.txtLog.ReadOnly = True
        Me.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txtLog.Size = New System.Drawing.Size(1016, 456)
        Me.txtLog.TabIndex = 1
        Me.txtLog.TabStop = False
        Me.txtLog.WordWrap = False
        '
        'txtCommand
        '
        Me.txtCommand.AcceptsReturn = True
        Me.txtCommand.BackColor = System.Drawing.Color.FromArgb(CType(CType(45, Byte), Integer), CType(CType(45, Byte), Integer), CType(CType(48, Byte), Integer))
        Me.txtCommand.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtCommand.Dock = System.Windows.Forms.DockStyle.Fill
        Me.txtCommand.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtCommand.ForeColor = System.Drawing.Color.White
        Me.txtCommand.Location = New System.Drawing.Point(5, 466)
        Me.txtCommand.Margin = New System.Windows.Forms.Padding(0)
        Me.txtCommand.Name = "txtCommand"
        Me.txtCommand.Size = New System.Drawing.Size(1016, 26)
        Me.txtCommand.TabIndex = 0
        Me.txtCommand.Text = "Type your commands here..."
        '
        'btnGeometry
        '
        Me.btnGeometry.Image = Global.AERO_Console.My.Resources.Resources.drafting_compass
        Me.btnGeometry.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnGeometry.Margin = New System.Windows.Forms.Padding(5, 1, 0, 2)
        Me.btnGeometry.Name = "btnGeometry"
        Me.btnGeometry.Size = New System.Drawing.Size(108, 22)
        Me.btnGeometry.Text = "Load Geometry"
        '
        'btnMass
        '
        Me.btnMass.Image = Global.AERO_Console.My.Resources.Resources.gym
        Me.btnMass.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnMass.Name = "btnMass"
        Me.btnMass.Size = New System.Drawing.Size(83, 22)
        Me.btnMass.Text = "Load Mass"
        '
        'btnRun
        '
        Me.btnRun.Image = Global.AERO_Console.My.Resources.Resources.running
        Me.btnRun.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnRun.Name = "btnRun"
        Me.btnRun.Size = New System.Drawing.Size(77, 22)
        Me.btnRun.Text = "Load Run"
        '
        'btnDesigner
        '
        Me.btnDesigner.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right
        Me.btnDesigner.Image = Global.AERO_Console.My.Resources.Resources.ruler_triangle
        Me.btnDesigner.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnDesigner.Name = "btnDesigner"
        Me.btnDesigner.Size = New System.Drawing.Size(128, 22)
        Me.btnDesigner.Text = "Geometry Designer"
        '
        'frmMain
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.ClientSize = New System.Drawing.Size(1026, 555)
        Me.Controls.Add(Me.LayoutTable)
        Me.Controls.Add(Me.ToolStrip1)
        Me.Controls.Add(Me.MenuStrip1)
        Me.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MainMenuStrip = Me.MenuStrip1
        Me.Name = "frmMain"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "AVL - Console"
        Me.MenuStrip1.ResumeLayout(False)
        Me.MenuStrip1.PerformLayout()
        Me.ToolStrip1.ResumeLayout(False)
        Me.ToolStrip1.PerformLayout()
        Me.LayoutTable.ResumeLayout(False)
        Me.LayoutTable.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
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
    Friend WithEvents btnDesigner As ToolStripButton
    Friend WithEvents DisplayToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents FontToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents fd1 As FontDialog
    Friend WithEvents AVLHelpToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolStripLabel1 As ToolStripLabel
    Friend WithEvents txtName As ToolStripComboBox
    Friend WithEvents btnGeometry As ToolStripButton
    Friend WithEvents btnMass As ToolStripButton
    Friend WithEvents btnRun As ToolStripButton
    Friend WithEvents LayoutTable As TableLayoutPanel
    Friend WithEvents txtLog As TextBox
    Friend WithEvents txtCommand As TextBox
End Class
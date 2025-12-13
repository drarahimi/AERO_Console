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
        MenuStrip1 = New MenuStrip()
        FileToolStripMenuItem = New ToolStripMenuItem()
        OpenCurrentDirectoryToolStripMenuItem = New ToolStripMenuItem()
        ToolsToolStripMenuItem = New ToolStripMenuItem()
        AirplaneDesignToolStripMenuItem = New ToolStripMenuItem()
        RestartConsoleToolStripMenuItem = New ToolStripMenuItem()
        DisplayToolStripMenuItem = New ToolStripMenuItem()
        FontToolStripMenuItem = New ToolStripMenuItem()
        HelpToolStripMenuItem = New ToolStripMenuItem()
        AVLHelpToolStripMenuItem = New ToolStripMenuItem()
        AboutToolStripMenuItem = New ToolStripMenuItem()
        CheckForUpdatesToolStripMenuItem = New ToolStripMenuItem()
        ToolStrip1 = New ToolStrip()
        ToolStripLabel1 = New ToolStripLabel()
        txtName = New ToolStripComboBox()
        btnGeometry = New ToolStripButton()
        btnMass = New ToolStripButton()
        btnRun = New ToolStripButton()
        btnDesigner = New ToolStripButton()
        fd1 = New FontDialog()
        LayoutTable = New TableLayoutPanel()
        txtLog = New TextBox()
        txtCommand = New TextBox()
        MenuStrip1.SuspendLayout()
        ToolStrip1.SuspendLayout()
        LayoutTable.SuspendLayout()
        SuspendLayout()
        ' 
        ' MenuStrip1
        ' 
        MenuStrip1.BackColor = SystemColors.Menu
        MenuStrip1.Items.AddRange(New ToolStripItem() {FileToolStripMenuItem, ToolsToolStripMenuItem, DisplayToolStripMenuItem, HelpToolStripMenuItem})
        MenuStrip1.Location = New Point(0, 0)
        MenuStrip1.Name = "MenuStrip1"
        MenuStrip1.RenderMode = ToolStripRenderMode.Professional
        MenuStrip1.Size = New Size(1026, 24)
        MenuStrip1.TabIndex = 2
        MenuStrip1.Text = "MenuStrip1"
        ' 
        ' FileToolStripMenuItem
        ' 
        FileToolStripMenuItem.DropDownItems.AddRange(New ToolStripItem() {OpenCurrentDirectoryToolStripMenuItem})
        FileToolStripMenuItem.Name = "FileToolStripMenuItem"
        FileToolStripMenuItem.Size = New Size(37, 20)
        FileToolStripMenuItem.Text = "&File"
        ' 
        ' OpenCurrentDirectoryToolStripMenuItem
        ' 
        OpenCurrentDirectoryToolStripMenuItem.Name = "OpenCurrentDirectoryToolStripMenuItem"
        OpenCurrentDirectoryToolStripMenuItem.Size = New Size(197, 22)
        OpenCurrentDirectoryToolStripMenuItem.Text = "Open Current Directory"
        ' 
        ' ToolsToolStripMenuItem
        ' 
        ToolsToolStripMenuItem.DropDownItems.AddRange(New ToolStripItem() {AirplaneDesignToolStripMenuItem, RestartConsoleToolStripMenuItem})
        ToolsToolStripMenuItem.Name = "ToolsToolStripMenuItem"
        ToolsToolStripMenuItem.Size = New Size(47, 20)
        ToolsToolStripMenuItem.Text = "&Tools"
        ' 
        ' AirplaneDesignToolStripMenuItem
        ' 
        AirplaneDesignToolStripMenuItem.Name = "AirplaneDesignToolStripMenuItem"
        AirplaneDesignToolStripMenuItem.Size = New Size(175, 22)
        AirplaneDesignToolStripMenuItem.Text = "Geometry Designer"
        ' 
        ' RestartConsoleToolStripMenuItem
        ' 
        RestartConsoleToolStripMenuItem.Name = "RestartConsoleToolStripMenuItem"
        RestartConsoleToolStripMenuItem.Size = New Size(175, 22)
        RestartConsoleToolStripMenuItem.Text = "Restart Console"
        ' 
        ' DisplayToolStripMenuItem
        ' 
        DisplayToolStripMenuItem.DropDownItems.AddRange(New ToolStripItem() {FontToolStripMenuItem})
        DisplayToolStripMenuItem.Name = "DisplayToolStripMenuItem"
        DisplayToolStripMenuItem.Size = New Size(57, 20)
        DisplayToolStripMenuItem.Text = "&Display"
        ' 
        ' FontToolStripMenuItem
        ' 
        FontToolStripMenuItem.Name = "FontToolStripMenuItem"
        FontToolStripMenuItem.Size = New Size(98, 22)
        FontToolStripMenuItem.Text = "Font"
        ' 
        ' HelpToolStripMenuItem
        ' 
        HelpToolStripMenuItem.DropDownItems.AddRange(New ToolStripItem() {AVLHelpToolStripMenuItem, AboutToolStripMenuItem, CheckForUpdatesToolStripMenuItem})
        HelpToolStripMenuItem.Name = "HelpToolStripMenuItem"
        HelpToolStripMenuItem.Size = New Size(44, 20)
        HelpToolStripMenuItem.Text = "&Help"
        ' 
        ' AVLHelpToolStripMenuItem
        ' 
        AVLHelpToolStripMenuItem.Name = "AVLHelpToolStripMenuItem"
        AVLHelpToolStripMenuItem.Size = New Size(171, 22)
        AVLHelpToolStripMenuItem.Text = "AVL Help"
        ' 
        ' AboutToolStripMenuItem
        ' 
        AboutToolStripMenuItem.Name = "AboutToolStripMenuItem"
        AboutToolStripMenuItem.Size = New Size(171, 22)
        AboutToolStripMenuItem.Text = "About"
        ' 
        ' CheckForUpdatesToolStripMenuItem
        ' 
        CheckForUpdatesToolStripMenuItem.Name = "CheckForUpdatesToolStripMenuItem"
        CheckForUpdatesToolStripMenuItem.Size = New Size(171, 22)
        CheckForUpdatesToolStripMenuItem.Text = "Check for Updates"
        ' 
        ' ToolStrip1
        ' 
        ToolStrip1.Items.AddRange(New ToolStripItem() {ToolStripLabel1, txtName, btnGeometry, btnMass, btnRun, btnDesigner})
        ToolStrip1.Location = New Point(0, 24)
        ToolStrip1.Name = "ToolStrip1"
        ToolStrip1.Padding = New Padding(5, 0, 1, 0)
        ToolStrip1.RenderMode = ToolStripRenderMode.Professional
        ToolStrip1.Size = New Size(1026, 25)
        ToolStrip1.TabIndex = 3
        ToolStrip1.Text = "ToolStrip1"
        ' 
        ' ToolStripLabel1
        ' 
        ToolStripLabel1.ForeColor = Color.DimGray
        ToolStripLabel1.Name = "ToolStripLabel1"
        ToolStripLabel1.Size = New Size(80, 22)
        ToolStripLabel1.Text = "Project name:"
        ' 
        ' txtName
        ' 
        txtName.Name = "txtName"
        txtName.Size = New Size(250, 25)
        ' 
        ' btnGeometry
        ' 
        btnGeometry.Image = CType(resources.GetObject("btnGeometry.Image"), Image)
        btnGeometry.ImageTransparentColor = Color.Magenta
        btnGeometry.Margin = New Padding(5, 1, 0, 2)
        btnGeometry.Name = "btnGeometry"
        btnGeometry.Size = New Size(108, 22)
        btnGeometry.Text = "Load Geometry"
        ' 
        ' btnMass
        ' 
        btnMass.Image = CType(resources.GetObject("btnMass.Image"), Image)
        btnMass.ImageTransparentColor = Color.Magenta
        btnMass.Name = "btnMass"
        btnMass.Size = New Size(83, 22)
        btnMass.Text = "Load Mass"
        ' 
        ' btnRun
        ' 
        btnRun.Image = CType(resources.GetObject("btnRun.Image"), Image)
        btnRun.ImageTransparentColor = Color.Magenta
        btnRun.Name = "btnRun"
        btnRun.Size = New Size(77, 22)
        btnRun.Text = "Load Run"
        ' 
        ' btnDesigner
        ' 
        btnDesigner.Alignment = ToolStripItemAlignment.Right
        btnDesigner.Image = CType(resources.GetObject("btnDesigner.Image"), Image)
        btnDesigner.ImageTransparentColor = Color.Magenta
        btnDesigner.Name = "btnDesigner"
        btnDesigner.Size = New Size(128, 22)
        btnDesigner.Text = "Geometry Designer"
        ' 
        ' LayoutTable
        ' 
        LayoutTable.ColumnCount = 1
        LayoutTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100F))
        LayoutTable.Controls.Add(txtLog, 0, 0)
        LayoutTable.Controls.Add(txtCommand, 0, 1)
        LayoutTable.Dock = DockStyle.Fill
        LayoutTable.Location = New Point(0, 49)
        LayoutTable.Name = "LayoutTable"
        LayoutTable.Padding = New Padding(5)
        LayoutTable.RowCount = 2
        LayoutTable.RowStyles.Add(New RowStyle(SizeType.Percent, 100F))
        LayoutTable.RowStyles.Add(New RowStyle(SizeType.Absolute, 35F))
        LayoutTable.Size = New Size(1026, 506)
        LayoutTable.TabIndex = 4
        ' 
        ' txtLog
        ' 
        txtLog.BackColor = Color.FromArgb(CByte(30), CByte(30), CByte(30))
        txtLog.BorderStyle = BorderStyle.None
        txtLog.Dock = DockStyle.Fill
        txtLog.Font = New Font("Consolas", 11.25F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        txtLog.ForeColor = Color.Gainsboro
        txtLog.Location = New Point(5, 5)
        txtLog.Margin = New Padding(0, 0, 0, 5)
        txtLog.Multiline = True
        txtLog.Name = "txtLog"
        txtLog.ReadOnly = True
        txtLog.ScrollBars = ScrollBars.Vertical
        txtLog.Size = New Size(1016, 456)
        txtLog.TabIndex = 1
        txtLog.TabStop = False
        txtLog.WordWrap = False
        ' 
        ' txtCommand
        ' 
        txtCommand.AcceptsReturn = True
        txtCommand.BackColor = Color.FromArgb(CByte(45), CByte(45), CByte(48))
        txtCommand.BorderStyle = BorderStyle.FixedSingle
        txtCommand.Dock = DockStyle.Fill
        txtCommand.Font = New Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        txtCommand.ForeColor = Color.White
        txtCommand.Location = New Point(5, 466)
        txtCommand.Margin = New Padding(0)
        txtCommand.Name = "txtCommand"
        txtCommand.PlaceholderText = "Type your commands here..."
        txtCommand.Size = New Size(1016, 26)
        txtCommand.TabIndex = 0
        ' 
        ' frmMain
        ' 
        AutoScaleDimensions = New SizeF(96F, 96F)
        AutoScaleMode = AutoScaleMode.Dpi
        ClientSize = New Size(1026, 555)
        Controls.Add(LayoutTable)
        Controls.Add(ToolStrip1)
        Controls.Add(MenuStrip1)
        Font = New Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        MainMenuStrip = MenuStrip1
        Name = "frmMain"
        StartPosition = FormStartPosition.CenterScreen
        Text = "AVL - Console"
        MenuStrip1.ResumeLayout(False)
        MenuStrip1.PerformLayout()
        ToolStrip1.ResumeLayout(False)
        ToolStrip1.PerformLayout()
        LayoutTable.ResumeLayout(False)
        LayoutTable.PerformLayout()
        ResumeLayout(False)
        PerformLayout()

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
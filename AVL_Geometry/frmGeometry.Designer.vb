<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class frmGeometry
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
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmGeometry))
        Me.StatusStrip1 = New System.Windows.Forms.StatusStrip()
        Me.lblCursor = New System.Windows.Forms.ToolStripStatusLabel()
        Me.btnEditor = New System.Windows.Forms.ToolStripStatusLabel()
        Me.ToolStrip1 = New System.Windows.Forms.ToolStrip()
        Me.ToolStripLabel1 = New System.Windows.Forms.ToolStripLabel()
        Me.txtName = New System.Windows.Forms.ToolStripComboBox()
        Me.ToolStripDropDownButton1 = New System.Windows.Forms.ToolStripDropDownButton()
        Me.AVLTemplateToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.SurfaceToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.SectionToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ControlToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator1 = New System.Windows.Forms.ToolStripSeparator()
        Me.MassTemplateToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator2 = New System.Windows.Forms.ToolStripSeparator()
        Me.RunTemplateToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator5 = New System.Windows.Forms.ToolStripSeparator()
        Me.SeparatorToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.btnClear = New System.Windows.Forms.ToolStripButton()
        Me.ToolStripSeparator3 = New System.Windows.Forms.ToolStripSeparator()
        Me.ToolStripDropDownButton3 = New System.Windows.Forms.ToolStripDropDownButton()
        Me.btnTrefftz = New System.Windows.Forms.ToolStripMenuItem()
        Me.btnTest = New System.Windows.Forms.ToolStripMenuItem()
        Me.btnSaveView = New System.Windows.Forms.ToolStripMenuItem()
        Me.btnHelp = New System.Windows.Forms.ToolStripDropDownButton()
        Me.btnHelpFull = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator8 = New System.Windows.Forms.ToolStripSeparator()
        Me.btnHelpAVL = New System.Windows.Forms.ToolStripMenuItem()
        Me.btnHelpMass = New System.Windows.Forms.ToolStripMenuItem()
        Me.btnHelpRun = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator9 = New System.Windows.Forms.ToolStripSeparator()
        Me.btnHelpCommands = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator4 = New System.Windows.Forms.ToolStripSeparator()
        Me.btn3D = New System.Windows.Forms.ToolStripButton()
        Me.txt3 = New FastColoredTextBoxNS.FastColoredTextBox()
        Me.FileSystemWatcher1 = New System.IO.FileSystemWatcher()
        Me.bg1 = New System.ComponentModel.BackgroundWorker()
        Me.ToolStrip2 = New System.Windows.Forms.ToolStrip()
        Me.btnZoomin = New System.Windows.Forms.ToolStripButton()
        Me.btnZoomout = New System.Windows.Forms.ToolStripButton()
        Me.btnFitAll = New System.Windows.Forms.ToolStripButton()
        Me.ToolStripSeparator11 = New System.Windows.Forms.ToolStripSeparator()
        Me.btnBasefontplus = New System.Windows.Forms.ToolStripButton()
        Me.btnBasefontminus = New System.Windows.Forms.ToolStripButton()
        Me.ToolStripSeparator10 = New System.Windows.Forms.ToolStripSeparator()
        Me.btnDisplay = New System.Windows.Forms.ToolStripButton()
        Me.ToolStripSeparator12 = New System.Windows.Forms.ToolStripSeparator()
        Me.btnSpace = New System.Windows.Forms.ToolStripButton()
        Me.btnHover = New System.Windows.Forms.ToolStripButton()
        Me.ToolStripSeparator13 = New System.Windows.Forms.ToolStripSeparator()
        Me.btnSection = New System.Windows.Forms.ToolStripButton()
        Me.btnMass = New System.Windows.Forms.ToolStripButton()
        Me.btnControl = New System.Windows.Forms.ToolStripButton()
        Me.hp1 = New System.Windows.Forms.HelpProvider()
        Me.sc1 = New System.Windows.Forms.SplitContainer()
        Me.scup = New System.Windows.Forms.SplitContainer()
        Me.tc1 = New System.Windows.Forms.TabControl()
        Me.Geometry = New System.Windows.Forms.TabPage()
        Me.Mass = New System.Windows.Forms.TabPage()
        Me.Run = New System.Windows.Forms.TabPage()
        Me.ImageList1 = New System.Windows.Forms.ImageList(Me.components)
        Me.p3d = New System.Windows.Forms.PictureBox()
        Me.pxy = New System.Windows.Forms.PictureBox()
        Me.scdown = New System.Windows.Forms.SplitContainer()
        Me.pyz = New System.Windows.Forms.PictureBox()
        Me.pxz = New System.Windows.Forms.PictureBox()
        Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        Me.StatusStrip1.SuspendLayout()
        Me.ToolStrip1.SuspendLayout()
        CType(Me.txt3, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.FileSystemWatcher1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.ToolStrip2.SuspendLayout()
        CType(Me.sc1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.sc1.Panel1.SuspendLayout()
        Me.sc1.Panel2.SuspendLayout()
        Me.sc1.SuspendLayout()
        CType(Me.scup, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.scup.Panel1.SuspendLayout()
        Me.scup.Panel2.SuspendLayout()
        Me.scup.SuspendLayout()
        Me.tc1.SuspendLayout()
        Me.Geometry.SuspendLayout()
        CType(Me.p3d, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.pxy, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.scdown, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.scdown.Panel1.SuspendLayout()
        Me.scdown.Panel2.SuspendLayout()
        Me.scdown.SuspendLayout()
        CType(Me.pyz, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.pxz, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'StatusStrip1
        '
        Me.StatusStrip1.BackColor = System.Drawing.Color.WhiteSmoke
        Me.StatusStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.lblCursor, Me.btnEditor})
        Me.StatusStrip1.Location = New System.Drawing.Point(0, 594)
        Me.StatusStrip1.Name = "StatusStrip1"
        Me.StatusStrip1.Size = New System.Drawing.Size(979, 22)
        Me.StatusStrip1.TabIndex = 1
        Me.StatusStrip1.Text = "StatusStrip1"
        '
        'lblCursor
        '
        Me.lblCursor.Name = "lblCursor"
        Me.lblCursor.Size = New System.Drawing.Size(48, 17)
        Me.lblCursor.Text = "Cursor: "
        '
        'btnEditor
        '
        Me.btnEditor.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnEditor.Image = CType(resources.GetObject("btnEditor.Image"), System.Drawing.Image)
        Me.btnEditor.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnEditor.Name = "btnEditor"
        Me.btnEditor.Size = New System.Drawing.Size(916, 17)
        Me.btnEditor.Spring = True
        Me.btnEditor.Text = "Editor Help"
        Me.btnEditor.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.btnEditor.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage
        '
        'ToolStrip1
        '
        Me.ToolStrip1.BackColor = System.Drawing.Color.White
        Me.ToolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden
        Me.ToolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripLabel1, Me.txtName, Me.ToolStripDropDownButton1, Me.btnClear, Me.ToolStripSeparator3, Me.ToolStripDropDownButton3, Me.btnHelp, Me.ToolStripSeparator4, Me.btn3D})
        Me.ToolStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow
        Me.ToolStrip1.Location = New System.Drawing.Point(0, 0)
        Me.ToolStrip1.Name = "ToolStrip1"
        Me.ToolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional
        Me.ToolStrip1.Size = New System.Drawing.Size(979, 25)
        Me.ToolStrip1.TabIndex = 2
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
        Me.txtName.AutoToolTip = True
        Me.txtName.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.txtName.Name = "txtName"
        Me.txtName.Size = New System.Drawing.Size(250, 25)
        Me.txtName.ToolTipText = "Name of the project you are working with"
        '
        'ToolStripDropDownButton1
        '
        Me.ToolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.ToolStripDropDownButton1.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.AVLTemplateToolStripMenuItem, Me.SurfaceToolStripMenuItem, Me.SectionToolStripMenuItem, Me.ControlToolStripMenuItem, Me.ToolStripSeparator1, Me.MassTemplateToolStripMenuItem, Me.ToolStripSeparator2, Me.RunTemplateToolStripMenuItem, Me.ToolStripSeparator5, Me.SeparatorToolStripMenuItem})
        Me.ToolStripDropDownButton1.Image = CType(resources.GetObject("ToolStripDropDownButton1.Image"), System.Drawing.Image)
        Me.ToolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ToolStripDropDownButton1.Name = "ToolStripDropDownButton1"
        Me.ToolStripDropDownButton1.Size = New System.Drawing.Size(42, 22)
        Me.ToolStripDropDownButton1.Text = "Add"
        '
        'AVLTemplateToolStripMenuItem
        '
        Me.AVLTemplateToolStripMenuItem.Name = "AVLTemplateToolStripMenuItem"
        Me.AVLTemplateToolStripMenuItem.Size = New System.Drawing.Size(153, 22)
        Me.AVLTemplateToolStripMenuItem.Text = "AVL Template"
        '
        'SurfaceToolStripMenuItem
        '
        Me.SurfaceToolStripMenuItem.Name = "SurfaceToolStripMenuItem"
        Me.SurfaceToolStripMenuItem.Size = New System.Drawing.Size(153, 22)
        Me.SurfaceToolStripMenuItem.Text = "Surface"
        '
        'SectionToolStripMenuItem
        '
        Me.SectionToolStripMenuItem.Name = "SectionToolStripMenuItem"
        Me.SectionToolStripMenuItem.Size = New System.Drawing.Size(153, 22)
        Me.SectionToolStripMenuItem.Text = "Section"
        '
        'ControlToolStripMenuItem
        '
        Me.ControlToolStripMenuItem.Name = "ControlToolStripMenuItem"
        Me.ControlToolStripMenuItem.Size = New System.Drawing.Size(153, 22)
        Me.ControlToolStripMenuItem.Text = "Control"
        '
        'ToolStripSeparator1
        '
        Me.ToolStripSeparator1.Name = "ToolStripSeparator1"
        Me.ToolStripSeparator1.Size = New System.Drawing.Size(150, 6)
        '
        'MassTemplateToolStripMenuItem
        '
        Me.MassTemplateToolStripMenuItem.Name = "MassTemplateToolStripMenuItem"
        Me.MassTemplateToolStripMenuItem.Size = New System.Drawing.Size(153, 22)
        Me.MassTemplateToolStripMenuItem.Text = "Mass Template"
        '
        'ToolStripSeparator2
        '
        Me.ToolStripSeparator2.Name = "ToolStripSeparator2"
        Me.ToolStripSeparator2.Size = New System.Drawing.Size(150, 6)
        '
        'RunTemplateToolStripMenuItem
        '
        Me.RunTemplateToolStripMenuItem.Name = "RunTemplateToolStripMenuItem"
        Me.RunTemplateToolStripMenuItem.Size = New System.Drawing.Size(153, 22)
        Me.RunTemplateToolStripMenuItem.Text = "Run Template"
        '
        'ToolStripSeparator5
        '
        Me.ToolStripSeparator5.Name = "ToolStripSeparator5"
        Me.ToolStripSeparator5.Size = New System.Drawing.Size(150, 6)
        '
        'SeparatorToolStripMenuItem
        '
        Me.SeparatorToolStripMenuItem.Name = "SeparatorToolStripMenuItem"
        Me.SeparatorToolStripMenuItem.Size = New System.Drawing.Size(153, 22)
        Me.SeparatorToolStripMenuItem.Text = "Separator"
        '
        'btnClear
        '
        Me.btnClear.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnClear.Image = CType(resources.GetObject("btnClear.Image"), System.Drawing.Image)
        Me.btnClear.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnClear.Name = "btnClear"
        Me.btnClear.Size = New System.Drawing.Size(38, 22)
        Me.btnClear.Text = "Clear"
        '
        'ToolStripSeparator3
        '
        Me.ToolStripSeparator3.Name = "ToolStripSeparator3"
        Me.ToolStripSeparator3.Size = New System.Drawing.Size(6, 25)
        '
        'ToolStripDropDownButton3
        '
        Me.ToolStripDropDownButton3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.ToolStripDropDownButton3.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.btnTrefftz, Me.btnTest, Me.btnSaveView})
        Me.ToolStripDropDownButton3.Image = CType(resources.GetObject("ToolStripDropDownButton3.Image"), System.Drawing.Image)
        Me.ToolStripDropDownButton3.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ToolStripDropDownButton3.Name = "ToolStripDropDownButton3"
        Me.ToolStripDropDownButton3.Size = New System.Drawing.Size(45, 22)
        Me.ToolStripDropDownButton3.Text = "View"
        '
        'btnTrefftz
        '
        Me.btnTrefftz.Name = "btnTrefftz"
        Me.btnTrefftz.Size = New System.Drawing.Size(317, 22)
        Me.btnTrefftz.Text = "Trefftz Plane"
        Me.btnTrefftz.Visible = False
        '
        'btnTest
        '
        Me.btnTest.Name = "btnTest"
        Me.btnTest.Size = New System.Drawing.Size(317, 22)
        Me.btnTest.Text = "Geometry in AVL Window"
        '
        'btnSaveView
        '
        Me.btnSaveView.Name = "btnSaveView"
        Me.btnSaveView.Size = New System.Drawing.Size(317, 22)
        Me.btnSaveView.Text = "Save AVL then show Geometry in AVL Window"
        '
        'btnHelp
        '
        Me.btnHelp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnHelp.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.btnHelpFull, Me.ToolStripSeparator8, Me.btnHelpAVL, Me.btnHelpMass, Me.btnHelpRun, Me.ToolStripSeparator9, Me.btnHelpCommands})
        Me.btnHelp.Image = CType(resources.GetObject("btnHelp.Image"), System.Drawing.Image)
        Me.btnHelp.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnHelp.Name = "btnHelp"
        Me.btnHelp.Size = New System.Drawing.Size(25, 22)
        Me.btnHelp.Text = "?"
        '
        'btnHelpFull
        '
        Me.btnHelpFull.Name = "btnHelpFull"
        Me.btnHelpFull.Size = New System.Drawing.Size(185, 22)
        Me.btnHelpFull.Text = "Full Help Document"
        '
        'ToolStripSeparator8
        '
        Me.ToolStripSeparator8.Name = "ToolStripSeparator8"
        Me.ToolStripSeparator8.Size = New System.Drawing.Size(182, 6)
        '
        'btnHelpAVL
        '
        Me.btnHelpAVL.Name = "btnHelpAVL"
        Me.btnHelpAVL.Size = New System.Drawing.Size(185, 22)
        Me.btnHelpAVL.Text = "Geometry File (.avl)"
        '
        'btnHelpMass
        '
        Me.btnHelpMass.Name = "btnHelpMass"
        Me.btnHelpMass.Size = New System.Drawing.Size(185, 22)
        Me.btnHelpMass.Text = "Mass File (.mass)"
        '
        'btnHelpRun
        '
        Me.btnHelpRun.Name = "btnHelpRun"
        Me.btnHelpRun.Size = New System.Drawing.Size(185, 22)
        Me.btnHelpRun.Text = "Run File (.run)"
        '
        'ToolStripSeparator9
        '
        Me.ToolStripSeparator9.Name = "ToolStripSeparator9"
        Me.ToolStripSeparator9.Size = New System.Drawing.Size(182, 6)
        '
        'btnHelpCommands
        '
        Me.btnHelpCommands.Name = "btnHelpCommands"
        Me.btnHelpCommands.Size = New System.Drawing.Size(185, 22)
        Me.btnHelpCommands.Text = "Program Commands"
        '
        'ToolStripSeparator4
        '
        Me.ToolStripSeparator4.Name = "ToolStripSeparator4"
        Me.ToolStripSeparator4.Size = New System.Drawing.Size(6, 25)
        '
        'btn3D
        '
        Me.btn3D.BackColor = System.Drawing.Color.Lime
        Me.btn3D.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btn3D.Image = CType(resources.GetObject("btn3D.Image"), System.Drawing.Image)
        Me.btn3D.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btn3D.Name = "btn3D"
        Me.btn3D.Size = New System.Drawing.Size(80, 22)
        Me.btn3D.Text = "Show 3D: Off"
        Me.btn3D.ToolTipText = "Show 3D: Off"
        '
        'txt3
        '
        Me.txt3.AutoCompleteBrackets = True
        Me.txt3.AutoCompleteBracketsList = New Char() {Global.Microsoft.VisualBasic.ChrW(40), Global.Microsoft.VisualBasic.ChrW(41), Global.Microsoft.VisualBasic.ChrW(123), Global.Microsoft.VisualBasic.ChrW(125), Global.Microsoft.VisualBasic.ChrW(91), Global.Microsoft.VisualBasic.ChrW(93), Global.Microsoft.VisualBasic.ChrW(34), Global.Microsoft.VisualBasic.ChrW(34), Global.Microsoft.VisualBasic.ChrW(39), Global.Microsoft.VisualBasic.ChrW(39)}
        Me.txt3.AutoIndentCharsPatterns = "^\s*[\w\.]+(\s\w+)?\s*(?<range>=)\s*(?<range>[^;=]+);" & Global.Microsoft.VisualBasic.ChrW(10) & "^\s*(case|default)\s*[^:]*(" &
    "?<range>:)\s*(?<range>[^;]+);"
        Me.txt3.AutoScrollMinSize = New System.Drawing.Size(43, 17)
        Me.txt3.BackBrush = Nothing
        Me.txt3.BookmarkColor = System.Drawing.Color.Red
        Me.txt3.CharHeight = 17
        Me.txt3.CharWidth = 8
        Me.txt3.CommentPrefix = "!|#"
        Me.txt3.Cursor = System.Windows.Forms.Cursors.IBeam
        Me.txt3.DefaultMarkerSize = 8
        Me.txt3.DisabledColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(180, Byte), Integer), CType(CType(180, Byte), Integer), CType(CType(180, Byte), Integer))
        Me.txt3.Dock = System.Windows.Forms.DockStyle.Fill
        Me.txt3.Font = New System.Drawing.Font("Consolas", 11.25!)
        Me.txt3.HighlightingRangeType = FastColoredTextBoxNS.HighlightingRangeType.VisibleRange
        Me.txt3.IsReplaceMode = False
        Me.txt3.Location = New System.Drawing.Point(3, 3)
        Me.txt3.Name = "txt3"
        Me.txt3.Paddings = New System.Windows.Forms.Padding(0)
        Me.txt3.ReservedCountOfLineNumberChars = 3
        Me.txt3.SelectionColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.txt3.ServiceColors = CType(resources.GetObject("txt3.ServiceColors"), FastColoredTextBoxNS.ServiceColors)
        Me.txt3.ShowFoldingLines = True
        Me.txt3.Size = New System.Drawing.Size(406, 264)
        Me.txt3.TabIndex = 2
        Me.txt3.Zoom = 100
        '
        'FileSystemWatcher1
        '
        Me.FileSystemWatcher1.EnableRaisingEvents = True
        Me.FileSystemWatcher1.SynchronizingObject = Me
        '
        'bg1
        '
        Me.bg1.WorkerReportsProgress = True
        Me.bg1.WorkerSupportsCancellation = True
        '
        'ToolStrip2
        '
        Me.ToolStrip2.BackColor = System.Drawing.Color.White
        Me.ToolStrip2.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden
        Me.ToolStrip2.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.btnZoomin, Me.btnZoomout, Me.btnFitAll, Me.ToolStripSeparator11, Me.btnBasefontplus, Me.btnBasefontminus, Me.ToolStripSeparator10, Me.btnDisplay, Me.ToolStripSeparator12, Me.btnSpace, Me.btnHover, Me.ToolStripSeparator13, Me.btnSection, Me.btnMass, Me.btnControl})
        Me.ToolStrip2.Location = New System.Drawing.Point(0, 25)
        Me.ToolStrip2.Name = "ToolStrip2"
        Me.ToolStrip2.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional
        Me.ToolStrip2.Size = New System.Drawing.Size(979, 25)
        Me.ToolStrip2.TabIndex = 6
        Me.ToolStrip2.Text = "ToolStrip2"
        '
        'btnZoomin
        '
        Me.btnZoomin.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.btnZoomin.Image = Global.AERO_Console.My.Resources.Resources.zoom_in
        Me.btnZoomin.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnZoomin.Name = "btnZoomin"
        Me.btnZoomin.Size = New System.Drawing.Size(23, 22)
        Me.btnZoomin.Text = "Zoom in"
        '
        'btnZoomout
        '
        Me.btnZoomout.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.btnZoomout.Image = Global.AERO_Console.My.Resources.Resources.zoom_out
        Me.btnZoomout.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnZoomout.Name = "btnZoomout"
        Me.btnZoomout.Size = New System.Drawing.Size(23, 22)
        Me.btnZoomout.Text = "Zoom out"
        '
        'btnFitAll
        '
        Me.btnFitAll.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.btnFitAll.Image = Global.AERO_Console.My.Resources.Resources.search
        Me.btnFitAll.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnFitAll.Name = "btnFitAll"
        Me.btnFitAll.Size = New System.Drawing.Size(23, 22)
        Me.btnFitAll.Text = "Fit All"
        Me.btnFitAll.ToolTipText = "Fit all geometry and mass points into the views"
        '
        'ToolStripSeparator11
        '
        Me.ToolStripSeparator11.Name = "ToolStripSeparator11"
        Me.ToolStripSeparator11.Size = New System.Drawing.Size(6, 25)
        '
        'btnBasefontplus
        '
        Me.btnBasefontplus.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnBasefontplus.Name = "btnBasefontplus"
        Me.btnBasefontplus.Size = New System.Drawing.Size(43, 22)
        Me.btnBasefontplus.Text = "Font+"
        '
        'btnBasefontminus
        '
        Me.btnBasefontminus.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnBasefontminus.Image = CType(resources.GetObject("btnBasefontminus.Image"), System.Drawing.Image)
        Me.btnBasefontminus.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnBasefontminus.Name = "btnBasefontminus"
        Me.btnBasefontminus.Size = New System.Drawing.Size(40, 22)
        Me.btnBasefontminus.Text = "Font-"
        '
        'ToolStripSeparator10
        '
        Me.ToolStripSeparator10.Name = "ToolStripSeparator10"
        Me.ToolStripSeparator10.Size = New System.Drawing.Size(6, 25)
        '
        'btnDisplay
        '
        Me.btnDisplay.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnDisplay.Image = CType(resources.GetObject("btnDisplay.Image"), System.Drawing.Image)
        Me.btnDisplay.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnDisplay.Name = "btnDisplay"
        Me.btnDisplay.Size = New System.Drawing.Size(102, 22)
        Me.btnDisplay.Text = "Show Editor Only"
        Me.btnDisplay.Visible = False
        '
        'ToolStripSeparator12
        '
        Me.ToolStripSeparator12.Name = "ToolStripSeparator12"
        Me.ToolStripSeparator12.Size = New System.Drawing.Size(6, 25)
        Me.ToolStripSeparator12.Visible = False
        '
        'btnSpace
        '
        Me.btnSpace.BackColor = System.Drawing.Color.FromArgb(CType(CType(192, Byte), Integer), CType(CType(192, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.btnSpace.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnSpace.Image = CType(resources.GetObject("btnSpace.Image"), System.Drawing.Image)
        Me.btnSpace.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnSpace.Name = "btnSpace"
        Me.btnSpace.Size = New System.Drawing.Size(93, 22)
        Me.btnSpace.Text = "Auto Space: On"
        '
        'btnHover
        '
        Me.btnHover.BackColor = System.Drawing.Color.FromArgb(CType(CType(128, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.btnHover.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnHover.Image = CType(resources.GetObject("btnHover.Image"), System.Drawing.Image)
        Me.btnHover.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnHover.Name = "btnHover"
        Me.btnHover.Size = New System.Drawing.Size(118, 22)
        Me.btnHover.Text = "Highlight Hover: On"
        '
        'ToolStripSeparator13
        '
        Me.ToolStripSeparator13.Name = "ToolStripSeparator13"
        Me.ToolStripSeparator13.Size = New System.Drawing.Size(6, 25)
        '
        'btnSection
        '
        Me.btnSection.BackColor = System.Drawing.Color.Red
        Me.btnSection.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnSection.ForeColor = System.Drawing.Color.White
        Me.btnSection.Image = CType(resources.GetObject("btnSection.Image"), System.Drawing.Image)
        Me.btnSection.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnSection.Name = "btnSection"
        Me.btnSection.Size = New System.Drawing.Size(104, 22)
        Me.btnSection.Text = "Show Section: On"
        '
        'btnMass
        '
        Me.btnMass.BackColor = System.Drawing.Color.Blue
        Me.btnMass.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnMass.ForeColor = System.Drawing.Color.White
        Me.btnMass.Image = CType(resources.GetObject("btnMass.Image"), System.Drawing.Image)
        Me.btnMass.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnMass.Name = "btnMass"
        Me.btnMass.Size = New System.Drawing.Size(92, 22)
        Me.btnMass.Text = "Show Mass: On"
        Me.btnMass.ToolTipText = "Overlays the mass distributio. Note that the mass points are scaled based on thei" &
    "r mass values"
        '
        'btnControl
        '
        Me.btnControl.BackColor = System.Drawing.Color.Yellow
        Me.btnControl.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnControl.Image = CType(resources.GetObject("btnControl.Image"), System.Drawing.Image)
        Me.btnControl.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnControl.Name = "btnControl"
        Me.btnControl.Size = New System.Drawing.Size(105, 22)
        Me.btnControl.Text = "Show Control: On"
        '
        'sc1
        '
        Me.sc1.BackColor = System.Drawing.Color.WhiteSmoke
        Me.sc1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.sc1.Location = New System.Drawing.Point(0, 50)
        Me.sc1.Name = "sc1"
        Me.sc1.Orientation = System.Windows.Forms.Orientation.Horizontal
        '
        'sc1.Panel1
        '
        Me.sc1.Panel1.Controls.Add(Me.scup)
        Me.sc1.Panel1.Padding = New System.Windows.Forms.Padding(5)
        '
        'sc1.Panel2
        '
        Me.sc1.Panel2.Controls.Add(Me.scdown)
        Me.sc1.Panel2.Padding = New System.Windows.Forms.Padding(5)
        Me.sc1.Size = New System.Drawing.Size(979, 544)
        Me.sc1.SplitterDistance = 311
        Me.sc1.TabIndex = 7
        '
        'scup
        '
        Me.scup.BackColor = System.Drawing.Color.WhiteSmoke
        Me.scup.Dock = System.Windows.Forms.DockStyle.Fill
        Me.scup.Location = New System.Drawing.Point(5, 5)
        Me.scup.Name = "scup"
        '
        'scup.Panel1
        '
        Me.scup.Panel1.Controls.Add(Me.tc1)
        '
        'scup.Panel2
        '
        Me.scup.Panel2.Controls.Add(Me.p3d)
        Me.scup.Panel2.Controls.Add(Me.pxy)
        Me.scup.Size = New System.Drawing.Size(969, 301)
        Me.scup.SplitterDistance = 420
        Me.scup.TabIndex = 0
        '
        'tc1
        '
        Me.tc1.Appearance = System.Windows.Forms.TabAppearance.FlatButtons
        Me.tc1.Controls.Add(Me.Geometry)
        Me.tc1.Controls.Add(Me.Mass)
        Me.tc1.Controls.Add(Me.Run)
        Me.tc1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.tc1.ImageList = Me.ImageList1
        Me.tc1.Location = New System.Drawing.Point(0, 0)
        Me.tc1.Name = "tc1"
        Me.tc1.SelectedIndex = 0
        Me.tc1.Size = New System.Drawing.Size(420, 301)
        Me.tc1.TabIndex = 0
        '
        'Geometry
        '
        Me.Geometry.Controls.Add(Me.txt3)
        Me.Geometry.ImageIndex = 0
        Me.Geometry.Location = New System.Drawing.Point(4, 27)
        Me.Geometry.Name = "Geometry"
        Me.Geometry.Padding = New System.Windows.Forms.Padding(3)
        Me.Geometry.Size = New System.Drawing.Size(412, 270)
        Me.Geometry.TabIndex = 0
        Me.Geometry.Text = "Geometry"
        '
        'Mass
        '
        Me.Mass.ImageIndex = 1
        Me.Mass.Location = New System.Drawing.Point(4, 27)
        Me.Mass.Name = "Mass"
        Me.Mass.Padding = New System.Windows.Forms.Padding(3)
        Me.Mass.Size = New System.Drawing.Size(412, 270)
        Me.Mass.TabIndex = 1
        Me.Mass.Text = "Mass"
        '
        'Run
        '
        Me.Run.ImageIndex = 2
        Me.Run.Location = New System.Drawing.Point(4, 27)
        Me.Run.Name = "Run"
        Me.Run.Size = New System.Drawing.Size(412, 270)
        Me.Run.TabIndex = 2
        Me.Run.Text = "Run"
        '
        'ImageList1
        '
        Me.ImageList1.ImageStream = CType(resources.GetObject("ImageList1.ImageStream"), System.Windows.Forms.ImageListStreamer)
        Me.ImageList1.TransparentColor = System.Drawing.Color.Transparent
        Me.ImageList1.Images.SetKeyName(0, "drafting-compass.png")
        Me.ImageList1.Images.SetKeyName(1, "gym.png")
        Me.ImageList1.Images.SetKeyName(2, "running.png")
        '
        'p3d
        '
        Me.p3d.BackColor = System.Drawing.Color.White
        Me.p3d.Location = New System.Drawing.Point(194, 80)
        Me.p3d.Name = "p3d"
        Me.p3d.Size = New System.Drawing.Size(100, 50)
        Me.p3d.TabIndex = 1
        Me.p3d.TabStop = False
        Me.p3d.Visible = False
        '
        'pxy
        '
        Me.pxy.BackColor = System.Drawing.Color.White
        Me.pxy.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.pxy.Location = New System.Drawing.Point(57, 54)
        Me.pxy.Name = "pxy"
        Me.pxy.Size = New System.Drawing.Size(488, 247)
        Me.pxy.TabIndex = 0
        Me.pxy.TabStop = False
        '
        'scdown
        '
        Me.scdown.BackColor = System.Drawing.Color.WhiteSmoke
        Me.scdown.Dock = System.Windows.Forms.DockStyle.Fill
        Me.scdown.Location = New System.Drawing.Point(5, 5)
        Me.scdown.Name = "scdown"
        '
        'scdown.Panel1
        '
        Me.scdown.Panel1.Controls.Add(Me.pyz)
        '
        'scdown.Panel2
        '
        Me.scdown.Panel2.Controls.Add(Me.pxz)
        Me.scdown.Size = New System.Drawing.Size(969, 219)
        Me.scdown.SplitterDistance = 428
        Me.scdown.TabIndex = 0
        '
        'pyz
        '
        Me.pyz.BackColor = System.Drawing.Color.White
        Me.pyz.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.pyz.Dock = System.Windows.Forms.DockStyle.Fill
        Me.pyz.Location = New System.Drawing.Point(0, 0)
        Me.pyz.Name = "pyz"
        Me.pyz.Size = New System.Drawing.Size(428, 219)
        Me.pyz.TabIndex = 3
        Me.pyz.TabStop = False
        '
        'pxz
        '
        Me.pxz.BackColor = System.Drawing.Color.White
        Me.pxz.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.pxz.Dock = System.Windows.Forms.DockStyle.Fill
        Me.pxz.Location = New System.Drawing.Point(0, 0)
        Me.pxz.Name = "pxz"
        Me.pxz.Size = New System.Drawing.Size(537, 219)
        Me.pxz.TabIndex = 0
        Me.pxz.TabStop = False
        '
        'ToolTip1
        '
        Me.ToolTip1.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info
        Me.ToolTip1.ToolTipTitle = "What does this do"
        '
        'frmGeometry
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.BackColor = System.Drawing.Color.White
        Me.ClientSize = New System.Drawing.Size(979, 616)
        Me.Controls.Add(Me.sc1)
        Me.Controls.Add(Me.ToolStrip2)
        Me.Controls.Add(Me.ToolStrip1)
        Me.Controls.Add(Me.StatusStrip1)
        Me.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "frmGeometry"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "AVL - Designer"
        Me.StatusStrip1.ResumeLayout(False)
        Me.StatusStrip1.PerformLayout()
        Me.ToolStrip1.ResumeLayout(False)
        Me.ToolStrip1.PerformLayout()
        CType(Me.txt3, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.FileSystemWatcher1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ToolStrip2.ResumeLayout(False)
        Me.ToolStrip2.PerformLayout()
        Me.sc1.Panel1.ResumeLayout(False)
        Me.sc1.Panel2.ResumeLayout(False)
        CType(Me.sc1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.sc1.ResumeLayout(False)
        Me.scup.Panel1.ResumeLayout(False)
        Me.scup.Panel2.ResumeLayout(False)
        CType(Me.scup, System.ComponentModel.ISupportInitialize).EndInit()
        Me.scup.ResumeLayout(False)
        Me.tc1.ResumeLayout(False)
        Me.Geometry.ResumeLayout(False)
        CType(Me.p3d, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.pxy, System.ComponentModel.ISupportInitialize).EndInit()
        Me.scdown.Panel1.ResumeLayout(False)
        Me.scdown.Panel2.ResumeLayout(False)
        CType(Me.scdown, System.ComponentModel.ISupportInitialize).EndInit()
        Me.scdown.ResumeLayout(False)
        CType(Me.pyz, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.pxz, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents pxy As PictureBox
    Friend WithEvents StatusStrip1 As StatusStrip
    Friend WithEvents lblCursor As ToolStripStatusLabel
    Friend WithEvents ToolStrip1 As ToolStrip
    Friend WithEvents ToolStripDropDownButton1 As ToolStripDropDownButton
    Friend WithEvents AVLTemplateToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents SurfaceToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents SectionToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ControlToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator1 As ToolStripSeparator
    Friend WithEvents SeparatorToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents btnClear As ToolStripButton
    Friend WithEvents txt3 As FastColoredTextBoxNS.FastColoredTextBox
    Friend WithEvents MassTemplateToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator2 As ToolStripSeparator
    Friend WithEvents ToolStripSeparator3 As ToolStripSeparator
    Friend WithEvents RunTemplateToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator5 As ToolStripSeparator
    Friend WithEvents pxz As PictureBox
    Friend WithEvents btnEditor As ToolStripStatusLabel
    Friend WithEvents FileSystemWatcher1 As IO.FileSystemWatcher
    Friend WithEvents pyz As PictureBox
    Friend WithEvents bg1 As System.ComponentModel.BackgroundWorker
    Friend WithEvents ToolStrip2 As ToolStrip
    Friend WithEvents btnZoomin As ToolStripButton
    Friend WithEvents btnZoomout As ToolStripButton
    Friend WithEvents btnBasefontplus As ToolStripButton
    Friend WithEvents btnBasefontminus As ToolStripButton
    Friend WithEvents btnDisplay As ToolStripButton
    Friend WithEvents ToolStripDropDownButton3 As ToolStripDropDownButton
    Friend WithEvents btnTrefftz As ToolStripMenuItem
    Friend WithEvents btnTest As ToolStripMenuItem
    Friend WithEvents btnSaveView As ToolStripMenuItem
    Friend WithEvents ToolStripLabel1 As ToolStripLabel
    Friend WithEvents ToolStripSeparator4 As ToolStripSeparator
    Friend WithEvents hp1 As HelpProvider
    Friend WithEvents btnHelp As ToolStripDropDownButton
    Friend WithEvents btnHelpFull As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator8 As ToolStripSeparator
    Friend WithEvents btnHelpAVL As ToolStripMenuItem
    Friend WithEvents btnHelpMass As ToolStripMenuItem
    Friend WithEvents btnHelpRun As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator9 As ToolStripSeparator
    Friend WithEvents btnHelpCommands As ToolStripMenuItem
    Friend WithEvents txtName As ToolStripComboBox
    Friend WithEvents ToolStripSeparator11 As ToolStripSeparator
    Friend WithEvents ToolStripSeparator10 As ToolStripSeparator
    Friend WithEvents ToolStripSeparator12 As ToolStripSeparator
    Friend WithEvents btnSpace As ToolStripButton
    Friend WithEvents ToolStripSeparator13 As ToolStripSeparator
    Friend WithEvents btnMass As ToolStripButton
    Friend WithEvents btnControl As ToolStripButton
    Friend WithEvents btnFitAll As ToolStripButton
    Friend WithEvents btnSection As ToolStripButton
    Friend WithEvents btn3D As ToolStripButton
    Friend WithEvents sc1 As SplitContainer
    Friend WithEvents scup As SplitContainer
    Friend WithEvents tc1 As TabControl
    Friend WithEvents Geometry As TabPage
    Friend WithEvents Mass As TabPage
    Friend WithEvents scdown As SplitContainer
    Friend WithEvents Run As TabPage
    Friend WithEvents ToolTip1 As ToolTip
    Friend WithEvents btnHover As ToolStripButton
    Friend WithEvents ImageList1 As ImageList
    Friend WithEvents p3d As PictureBox
End Class
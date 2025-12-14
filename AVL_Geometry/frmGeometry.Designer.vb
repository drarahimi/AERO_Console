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
        components = New ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmGeometry))
        StatusStrip1 = New StatusStrip()
        lblCursor = New ToolStripStatusLabel()
        btnEditor = New ToolStripStatusLabel()
        ToolStrip1 = New ToolStrip()
        ToolStripLabel1 = New ToolStripLabel()
        txtName = New ToolStripComboBox()
        ToolStripDropDownButton1 = New ToolStripDropDownButton()
        AVLTemplateToolStripMenuItem = New ToolStripMenuItem()
        SurfaceToolStripMenuItem = New ToolStripMenuItem()
        SectionToolStripMenuItem = New ToolStripMenuItem()
        ControlToolStripMenuItem = New ToolStripMenuItem()
        ToolStripSeparator1 = New ToolStripSeparator()
        MassTemplateToolStripMenuItem = New ToolStripMenuItem()
        ToolStripSeparator2 = New ToolStripSeparator()
        RunTemplateToolStripMenuItem = New ToolStripMenuItem()
        ToolStripSeparator5 = New ToolStripSeparator()
        SeparatorToolStripMenuItem = New ToolStripMenuItem()
        btnClear = New ToolStripButton()
        ToolStripSeparator3 = New ToolStripSeparator()
        ToolStripDropDownButton3 = New ToolStripDropDownButton()
        btnTrefftz = New ToolStripMenuItem()
        btnTest = New ToolStripMenuItem()
        btnSaveView = New ToolStripMenuItem()
        btnHelp = New ToolStripDropDownButton()
        btnHelpFull = New ToolStripMenuItem()
        ToolStripSeparator8 = New ToolStripSeparator()
        btnHelpAVL = New ToolStripMenuItem()
        btnHelpMass = New ToolStripMenuItem()
        btnHelpRun = New ToolStripMenuItem()
        ToolStripSeparator9 = New ToolStripSeparator()
        btnHelpCommands = New ToolStripMenuItem()
        ToolStripSeparator4 = New ToolStripSeparator()
        btn3D = New ToolStripButton()
        FileSystemWatcher1 = New IO.FileSystemWatcher()
        bg1 = New ComponentModel.BackgroundWorker()
        ToolStrip2 = New ToolStrip()
        btnZoomin = New ToolStripButton()
        btnZoomout = New ToolStripButton()
        btnFitAll = New ToolStripButton()
        ToolStripSeparator11 = New ToolStripSeparator()
        btnBasefontplus = New ToolStripButton()
        btnBasefontminus = New ToolStripButton()
        ToolStripSeparator10 = New ToolStripSeparator()
        btnDisplay = New ToolStripButton()
        ToolStripSeparator12 = New ToolStripSeparator()
        btnSpace = New ToolStripButton()
        btnHover = New ToolStripButton()
        ToolStripSeparator13 = New ToolStripSeparator()
        btnSection = New ToolStripButton()
        btnMass = New ToolStripButton()
        btnControl = New ToolStripButton()
        sc1 = New SplitContainer()
        scup = New SplitContainer()
        tc1 = New TabControl()
        Geometry = New TabPage()
        Mass = New TabPage()
        Run = New TabPage()
        ImageList1 = New ImageList(components)
        p3d = New PictureBox()
        pxy = New PictureBox()
        scdown = New SplitContainer()
        pyz = New PictureBox()
        pxz = New PictureBox()
        StatusStrip1.SuspendLayout()
        ToolStrip1.SuspendLayout()
        CType(FileSystemWatcher1, ComponentModel.ISupportInitialize).BeginInit()
        ToolStrip2.SuspendLayout()
        CType(sc1, ComponentModel.ISupportInitialize).BeginInit()
        sc1.Panel1.SuspendLayout()
        sc1.Panel2.SuspendLayout()
        sc1.SuspendLayout()
        CType(scup, ComponentModel.ISupportInitialize).BeginInit()
        scup.Panel1.SuspendLayout()
        scup.Panel2.SuspendLayout()
        scup.SuspendLayout()
        tc1.SuspendLayout()
        CType(p3d, ComponentModel.ISupportInitialize).BeginInit()
        CType(pxy, ComponentModel.ISupportInitialize).BeginInit()
        CType(scdown, ComponentModel.ISupportInitialize).BeginInit()
        scdown.Panel1.SuspendLayout()
        scdown.Panel2.SuspendLayout()
        scdown.SuspendLayout()
        CType(pyz, ComponentModel.ISupportInitialize).BeginInit()
        CType(pxz, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' StatusStrip1
        ' 
        StatusStrip1.BackColor = Color.WhiteSmoke
        StatusStrip1.Items.AddRange(New ToolStripItem() {lblCursor, btnEditor})
        StatusStrip1.Location = New Point(0, 594)
        StatusStrip1.Name = "StatusStrip1"
        StatusStrip1.Size = New Size(979, 22)
        StatusStrip1.TabIndex = 1
        StatusStrip1.Text = "StatusStrip1"
        ' 
        ' lblCursor
        ' 
        lblCursor.Name = "lblCursor"
        lblCursor.Size = New Size(48, 17)
        lblCursor.Text = "Cursor: "
        ' 
        ' btnEditor
        ' 
        btnEditor.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnEditor.Image = CType(resources.GetObject("btnEditor.Image"), Image)
        btnEditor.ImageTransparentColor = Color.Magenta
        btnEditor.Name = "btnEditor"
        btnEditor.Size = New Size(916, 17)
        btnEditor.Spring = True
        btnEditor.Text = "Editor Help"
        btnEditor.TextAlign = ContentAlignment.MiddleRight
        btnEditor.TextImageRelation = TextImageRelation.TextBeforeImage
        ' 
        ' ToolStrip1
        ' 
        ToolStrip1.BackColor = Color.White
        ToolStrip1.GripStyle = ToolStripGripStyle.Hidden
        ToolStrip1.Items.AddRange(New ToolStripItem() {ToolStripLabel1, txtName, ToolStripDropDownButton1, btnClear, ToolStripSeparator3, ToolStripDropDownButton3, btnHelp, ToolStripSeparator4})
        ToolStrip1.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow
        ToolStrip1.Location = New Point(0, 0)
        ToolStrip1.Name = "ToolStrip1"
        ToolStrip1.RenderMode = ToolStripRenderMode.Professional
        ToolStrip1.Size = New Size(979, 25)
        ToolStrip1.TabIndex = 2
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
        txtName.AutoToolTip = True
        txtName.FlatStyle = FlatStyle.Flat
        txtName.Name = "txtName"
        txtName.Size = New Size(250, 25)
        txtName.ToolTipText = "Name of the project you are working with"
        ' 
        ' ToolStripDropDownButton1
        ' 
        ToolStripDropDownButton1.DisplayStyle = ToolStripItemDisplayStyle.Text
        ToolStripDropDownButton1.DropDownItems.AddRange(New ToolStripItem() {AVLTemplateToolStripMenuItem, SurfaceToolStripMenuItem, SectionToolStripMenuItem, ControlToolStripMenuItem, ToolStripSeparator1, MassTemplateToolStripMenuItem, ToolStripSeparator2, RunTemplateToolStripMenuItem, ToolStripSeparator5, SeparatorToolStripMenuItem})
        ToolStripDropDownButton1.Image = CType(resources.GetObject("ToolStripDropDownButton1.Image"), Image)
        ToolStripDropDownButton1.ImageTransparentColor = Color.Magenta
        ToolStripDropDownButton1.Name = "ToolStripDropDownButton1"
        ToolStripDropDownButton1.Size = New Size(42, 22)
        ToolStripDropDownButton1.Text = "Add"
        ' 
        ' AVLTemplateToolStripMenuItem
        ' 
        AVLTemplateToolStripMenuItem.Name = "AVLTemplateToolStripMenuItem"
        AVLTemplateToolStripMenuItem.Size = New Size(153, 22)
        AVLTemplateToolStripMenuItem.Text = "AVL Template"
        ' 
        ' SurfaceToolStripMenuItem
        ' 
        SurfaceToolStripMenuItem.Name = "SurfaceToolStripMenuItem"
        SurfaceToolStripMenuItem.Size = New Size(153, 22)
        SurfaceToolStripMenuItem.Text = "Surface"
        ' 
        ' SectionToolStripMenuItem
        ' 
        SectionToolStripMenuItem.Name = "SectionToolStripMenuItem"
        SectionToolStripMenuItem.Size = New Size(153, 22)
        SectionToolStripMenuItem.Text = "Section"
        ' 
        ' ControlToolStripMenuItem
        ' 
        ControlToolStripMenuItem.Name = "ControlToolStripMenuItem"
        ControlToolStripMenuItem.Size = New Size(153, 22)
        ControlToolStripMenuItem.Text = "Control"
        ' 
        ' ToolStripSeparator1
        ' 
        ToolStripSeparator1.Name = "ToolStripSeparator1"
        ToolStripSeparator1.Size = New Size(150, 6)
        ' 
        ' MassTemplateToolStripMenuItem
        ' 
        MassTemplateToolStripMenuItem.Name = "MassTemplateToolStripMenuItem"
        MassTemplateToolStripMenuItem.Size = New Size(153, 22)
        MassTemplateToolStripMenuItem.Text = "Mass Template"
        ' 
        ' ToolStripSeparator2
        ' 
        ToolStripSeparator2.Name = "ToolStripSeparator2"
        ToolStripSeparator2.Size = New Size(150, 6)
        ' 
        ' RunTemplateToolStripMenuItem
        ' 
        RunTemplateToolStripMenuItem.Name = "RunTemplateToolStripMenuItem"
        RunTemplateToolStripMenuItem.Size = New Size(153, 22)
        RunTemplateToolStripMenuItem.Text = "Run Template"
        ' 
        ' ToolStripSeparator5
        ' 
        ToolStripSeparator5.Name = "ToolStripSeparator5"
        ToolStripSeparator5.Size = New Size(150, 6)
        ' 
        ' SeparatorToolStripMenuItem
        ' 
        SeparatorToolStripMenuItem.Name = "SeparatorToolStripMenuItem"
        SeparatorToolStripMenuItem.Size = New Size(153, 22)
        SeparatorToolStripMenuItem.Text = "Separator"
        ' 
        ' btnClear
        ' 
        btnClear.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnClear.Image = CType(resources.GetObject("btnClear.Image"), Image)
        btnClear.ImageTransparentColor = Color.Magenta
        btnClear.Name = "btnClear"
        btnClear.Size = New Size(38, 22)
        btnClear.Text = "Clear"
        ' 
        ' ToolStripSeparator3
        ' 
        ToolStripSeparator3.Name = "ToolStripSeparator3"
        ToolStripSeparator3.Size = New Size(6, 25)
        ' 
        ' ToolStripDropDownButton3
        ' 
        ToolStripDropDownButton3.DisplayStyle = ToolStripItemDisplayStyle.Text
        ToolStripDropDownButton3.DropDownItems.AddRange(New ToolStripItem() {btnTrefftz, btnTest, btnSaveView})
        ToolStripDropDownButton3.Image = CType(resources.GetObject("ToolStripDropDownButton3.Image"), Image)
        ToolStripDropDownButton3.ImageTransparentColor = Color.Magenta
        ToolStripDropDownButton3.Name = "ToolStripDropDownButton3"
        ToolStripDropDownButton3.Size = New Size(45, 22)
        ToolStripDropDownButton3.Text = "View"
        ' 
        ' btnTrefftz
        ' 
        btnTrefftz.Name = "btnTrefftz"
        btnTrefftz.Size = New Size(317, 22)
        btnTrefftz.Text = "Trefftz Plane"
        btnTrefftz.Visible = False
        ' 
        ' btnTest
        ' 
        btnTest.Name = "btnTest"
        btnTest.Size = New Size(317, 22)
        btnTest.Text = "Geometry in AVL Window"
        ' 
        ' btnSaveView
        ' 
        btnSaveView.Name = "btnSaveView"
        btnSaveView.Size = New Size(317, 22)
        btnSaveView.Text = "Save AVL then show Geometry in AVL Window"
        ' 
        ' btnHelp
        ' 
        btnHelp.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnHelp.DropDownItems.AddRange(New ToolStripItem() {btnHelpFull, ToolStripSeparator8, btnHelpAVL, btnHelpMass, btnHelpRun, ToolStripSeparator9, btnHelpCommands})
        btnHelp.Image = CType(resources.GetObject("btnHelp.Image"), Image)
        btnHelp.ImageTransparentColor = Color.Magenta
        btnHelp.Name = "btnHelp"
        btnHelp.Size = New Size(25, 22)
        btnHelp.Text = "?"
        ' 
        ' btnHelpFull
        ' 
        btnHelpFull.Name = "btnHelpFull"
        btnHelpFull.Size = New Size(185, 22)
        btnHelpFull.Text = "Full Help Document"
        ' 
        ' ToolStripSeparator8
        ' 
        ToolStripSeparator8.Name = "ToolStripSeparator8"
        ToolStripSeparator8.Size = New Size(182, 6)
        ' 
        ' btnHelpAVL
        ' 
        btnHelpAVL.Name = "btnHelpAVL"
        btnHelpAVL.Size = New Size(185, 22)
        btnHelpAVL.Text = "Geometry File (.avl)"
        ' 
        ' btnHelpMass
        ' 
        btnHelpMass.Name = "btnHelpMass"
        btnHelpMass.Size = New Size(185, 22)
        btnHelpMass.Text = "Mass File (.mass)"
        ' 
        ' btnHelpRun
        ' 
        btnHelpRun.Name = "btnHelpRun"
        btnHelpRun.Size = New Size(185, 22)
        btnHelpRun.Text = "Run File (.run)"
        ' 
        ' ToolStripSeparator9
        ' 
        ToolStripSeparator9.Name = "ToolStripSeparator9"
        ToolStripSeparator9.Size = New Size(182, 6)
        ' 
        ' btnHelpCommands
        ' 
        btnHelpCommands.Name = "btnHelpCommands"
        btnHelpCommands.Size = New Size(185, 22)
        btnHelpCommands.Text = "Program Commands"
        ' 
        ' ToolStripSeparator4
        ' 
        ToolStripSeparator4.Name = "ToolStripSeparator4"
        ToolStripSeparator4.Size = New Size(6, 25)
        ' 
        ' btn3D
        ' 
        btn3D.Alignment = ToolStripItemAlignment.Right
        btn3D.BackColor = Color.Lime
        btn3D.DisplayStyle = ToolStripItemDisplayStyle.Text
        btn3D.Image = CType(resources.GetObject("btn3D.Image"), Image)
        btn3D.ImageTransparentColor = Color.Magenta
        btn3D.Name = "btn3D"
        btn3D.Size = New Size(80, 22)
        btn3D.Text = "Show 3D: Off"
        btn3D.ToolTipText = "Show 3D: Off"
        ' 
        ' FileSystemWatcher1
        ' 
        FileSystemWatcher1.EnableRaisingEvents = True
        FileSystemWatcher1.SynchronizingObject = Me
        ' 
        ' bg1
        ' 
        bg1.WorkerReportsProgress = True
        bg1.WorkerSupportsCancellation = True
        ' 
        ' ToolStrip2
        ' 
        ToolStrip2.BackColor = Color.White
        ToolStrip2.GripStyle = ToolStripGripStyle.Hidden
        ToolStrip2.Items.AddRange(New ToolStripItem() {btnZoomin, btnZoomout, btnFitAll, ToolStripSeparator11, btnBasefontplus, btnBasefontminus, ToolStripSeparator10, btnDisplay, ToolStripSeparator12, btnSpace, btnHover, ToolStripSeparator13, btnSection, btnMass, btnControl, btn3D})
        ToolStrip2.Location = New Point(0, 25)
        ToolStrip2.Name = "ToolStrip2"
        ToolStrip2.RenderMode = ToolStripRenderMode.Professional
        ToolStrip2.Size = New Size(979, 25)
        ToolStrip2.TabIndex = 6
        ToolStrip2.Text = "ToolStrip2"
        ' 
        ' btnZoomin
        ' 
        btnZoomin.Alignment = ToolStripItemAlignment.Right
        btnZoomin.DisplayStyle = ToolStripItemDisplayStyle.Image
        btnZoomin.Image = CType(resources.GetObject("btnZoomin.Image"), Image)
        btnZoomin.ImageTransparentColor = Color.Magenta
        btnZoomin.Name = "btnZoomin"
        btnZoomin.Size = New Size(23, 22)
        btnZoomin.Text = "Zoom in"
        ' 
        ' btnZoomout
        ' 
        btnZoomout.Alignment = ToolStripItemAlignment.Right
        btnZoomout.DisplayStyle = ToolStripItemDisplayStyle.Image
        btnZoomout.Image = CType(resources.GetObject("btnZoomout.Image"), Image)
        btnZoomout.ImageTransparentColor = Color.Magenta
        btnZoomout.Name = "btnZoomout"
        btnZoomout.Size = New Size(23, 22)
        btnZoomout.Text = "Zoom out"
        ' 
        ' btnFitAll
        ' 
        btnFitAll.Alignment = ToolStripItemAlignment.Right
        btnFitAll.DisplayStyle = ToolStripItemDisplayStyle.Image
        btnFitAll.Image = CType(resources.GetObject("btnFitAll.Image"), Image)
        btnFitAll.ImageTransparentColor = Color.Magenta
        btnFitAll.Name = "btnFitAll"
        btnFitAll.Size = New Size(23, 22)
        btnFitAll.Text = "Fit All"
        btnFitAll.ToolTipText = "Fit all geometry and mass points into the views"
        ' 
        ' ToolStripSeparator11
        ' 
        ToolStripSeparator11.Alignment = ToolStripItemAlignment.Right
        ToolStripSeparator11.Name = "ToolStripSeparator11"
        ToolStripSeparator11.Size = New Size(6, 25)
        ' 
        ' btnBasefontplus
        ' 
        btnBasefontplus.Alignment = ToolStripItemAlignment.Right
        btnBasefontplus.ImageTransparentColor = Color.Magenta
        btnBasefontplus.Name = "btnBasefontplus"
        btnBasefontplus.Size = New Size(43, 22)
        btnBasefontplus.Text = "Font+"
        ' 
        ' btnBasefontminus
        ' 
        btnBasefontminus.Alignment = ToolStripItemAlignment.Right
        btnBasefontminus.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnBasefontminus.Image = CType(resources.GetObject("btnBasefontminus.Image"), Image)
        btnBasefontminus.ImageTransparentColor = Color.Magenta
        btnBasefontminus.Name = "btnBasefontminus"
        btnBasefontminus.Size = New Size(40, 22)
        btnBasefontminus.Text = "Font-"
        ' 
        ' ToolStripSeparator10
        ' 
        ToolStripSeparator10.Alignment = ToolStripItemAlignment.Right
        ToolStripSeparator10.Name = "ToolStripSeparator10"
        ToolStripSeparator10.Size = New Size(6, 25)
        ' 
        ' btnDisplay
        ' 
        btnDisplay.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnDisplay.ImageTransparentColor = Color.Magenta
        btnDisplay.Name = "btnDisplay"
        btnDisplay.Size = New Size(102, 22)
        btnDisplay.Text = "Show Editor Only"
        btnDisplay.Visible = False
        ' 
        ' ToolStripSeparator12
        ' 
        ToolStripSeparator12.Name = "ToolStripSeparator12"
        ToolStripSeparator12.Size = New Size(6, 25)
        ToolStripSeparator12.Visible = False
        ' 
        ' btnSpace
        ' 
        btnSpace.BackColor = Color.FromArgb(CByte(192), CByte(192), CByte(255))
        btnSpace.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnSpace.Image = CType(resources.GetObject("btnSpace.Image"), Image)
        btnSpace.ImageTransparentColor = Color.Magenta
        btnSpace.Name = "btnSpace"
        btnSpace.Size = New Size(93, 22)
        btnSpace.Text = "Auto Space: On"
        ' 
        ' btnHover
        ' 
        btnHover.BackColor = Color.FromArgb(CByte(128), CByte(255), CByte(128))
        btnHover.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnHover.Image = CType(resources.GetObject("btnHover.Image"), Image)
        btnHover.ImageTransparentColor = Color.Magenta
        btnHover.Name = "btnHover"
        btnHover.Size = New Size(118, 22)
        btnHover.Text = "Highlight Hover: On"
        ' 
        ' ToolStripSeparator13
        ' 
        ToolStripSeparator13.Name = "ToolStripSeparator13"
        ToolStripSeparator13.Size = New Size(6, 25)
        ' 
        ' btnSection
        ' 
        btnSection.Alignment = ToolStripItemAlignment.Right
        btnSection.BackColor = Color.Red
        btnSection.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnSection.ForeColor = Color.White
        btnSection.Image = CType(resources.GetObject("btnSection.Image"), Image)
        btnSection.ImageTransparentColor = Color.Magenta
        btnSection.Name = "btnSection"
        btnSection.Size = New Size(104, 22)
        btnSection.Text = "Show Section: On"
        ' 
        ' btnMass
        ' 
        btnMass.Alignment = ToolStripItemAlignment.Right
        btnMass.BackColor = Color.Blue
        btnMass.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnMass.ForeColor = Color.White
        btnMass.Image = CType(resources.GetObject("btnMass.Image"), Image)
        btnMass.ImageTransparentColor = Color.Magenta
        btnMass.Name = "btnMass"
        btnMass.Size = New Size(92, 22)
        btnMass.Text = "Show Mass: On"
        btnMass.ToolTipText = "Overlays the mass distributio. Note that the mass points are scaled based on their mass values"
        ' 
        ' btnControl
        ' 
        btnControl.Alignment = ToolStripItemAlignment.Right
        btnControl.BackColor = Color.Yellow
        btnControl.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnControl.Image = CType(resources.GetObject("btnControl.Image"), Image)
        btnControl.ImageTransparentColor = Color.Magenta
        btnControl.Name = "btnControl"
        btnControl.Size = New Size(105, 22)
        btnControl.Text = "Show Control: On"
        ' 
        ' sc1
        ' 
        sc1.BackColor = Color.WhiteSmoke
        sc1.Dock = DockStyle.Fill
        sc1.Location = New Point(0, 50)
        sc1.Name = "sc1"
        sc1.Orientation = Orientation.Horizontal
        ' 
        ' sc1.Panel1
        ' 
        sc1.Panel1.Controls.Add(scup)
        sc1.Panel1.Padding = New Padding(5)
        ' 
        ' sc1.Panel2
        ' 
        sc1.Panel2.Controls.Add(scdown)
        sc1.Panel2.Padding = New Padding(5)
        sc1.Size = New Size(979, 544)
        sc1.SplitterDistance = 311
        sc1.TabIndex = 7
        ' 
        ' scup
        ' 
        scup.BackColor = Color.WhiteSmoke
        scup.Dock = DockStyle.Fill
        scup.Location = New Point(5, 5)
        scup.Name = "scup"
        ' 
        ' scup.Panel1
        ' 
        scup.Panel1.Controls.Add(tc1)
        ' 
        ' scup.Panel2
        ' 
        scup.Panel2.Controls.Add(p3d)
        scup.Panel2.Controls.Add(pxy)
        scup.Size = New Size(969, 301)
        scup.SplitterDistance = 420
        scup.TabIndex = 0
        ' 
        ' tc1
        ' 
        tc1.Appearance = TabAppearance.FlatButtons
        tc1.Controls.Add(Geometry)
        tc1.Controls.Add(Mass)
        tc1.Controls.Add(Run)
        tc1.Dock = DockStyle.Fill
        tc1.ImageList = ImageList1
        tc1.Location = New Point(0, 0)
        tc1.Name = "tc1"
        tc1.SelectedIndex = 0
        tc1.Size = New Size(420, 301)
        tc1.TabIndex = 0
        ' 
        ' Geometry
        ' 
        Geometry.ImageIndex = 0
        Geometry.Location = New Point(4, 27)
        Geometry.Name = "Geometry"
        Geometry.Padding = New Padding(3)
        Geometry.Size = New Size(412, 270)
        Geometry.TabIndex = 0
        Geometry.Text = "Geometry"
        ' 
        ' Mass
        ' 
        Mass.ImageIndex = 1
        Mass.Location = New Point(4, 27)
        Mass.Name = "Mass"
        Mass.Padding = New Padding(3)
        Mass.Size = New Size(412, 270)
        Mass.TabIndex = 1
        Mass.Text = "Mass"
        ' 
        ' Run
        ' 
        Run.ImageIndex = 2
        Run.Location = New Point(4, 27)
        Run.Name = "Run"
        Run.Size = New Size(412, 270)
        Run.TabIndex = 2
        Run.Text = "Run"
        ' 
        ' ImageList1
        ' 
        ImageList1.ColorDepth = ColorDepth.Depth8Bit
        ImageList1.ImageStream = CType(resources.GetObject("ImageList1.ImageStream"), ImageListStreamer)
        ImageList1.TransparentColor = Color.Transparent
        ImageList1.Images.SetKeyName(0, "drafting-compass.png")
        ImageList1.Images.SetKeyName(1, "gym.png")
        ImageList1.Images.SetKeyName(2, "running.png")
        ' 
        ' p3d
        ' 
        p3d.BackColor = Color.White
        p3d.Location = New Point(194, 80)
        p3d.Name = "p3d"
        p3d.Size = New Size(100, 50)
        p3d.TabIndex = 1
        p3d.TabStop = False
        p3d.Visible = False
        ' 
        ' pxy
        ' 
        pxy.BackColor = Color.White
        pxy.BorderStyle = BorderStyle.FixedSingle
        pxy.Dock = DockStyle.Fill
        pxy.Location = New Point(0, 0)
        pxy.Name = "pxy"
        pxy.Size = New Size(545, 301)
        pxy.TabIndex = 0
        pxy.TabStop = False
        ' 
        ' scdown
        ' 
        scdown.BackColor = Color.WhiteSmoke
        scdown.Dock = DockStyle.Fill
        scdown.Location = New Point(5, 5)
        scdown.Name = "scdown"
        ' 
        ' scdown.Panel1
        ' 
        scdown.Panel1.Controls.Add(pyz)
        ' 
        ' scdown.Panel2
        ' 
        scdown.Panel2.Controls.Add(pxz)
        scdown.Size = New Size(969, 219)
        scdown.SplitterDistance = 428
        scdown.TabIndex = 0
        ' 
        ' pyz
        ' 
        pyz.BackColor = Color.White
        pyz.BorderStyle = BorderStyle.FixedSingle
        pyz.Dock = DockStyle.Fill
        pyz.Location = New Point(0, 0)
        pyz.Name = "pyz"
        pyz.Size = New Size(428, 219)
        pyz.TabIndex = 3
        pyz.TabStop = False
        ' 
        ' pxz
        ' 
        pxz.BackColor = Color.White
        pxz.BorderStyle = BorderStyle.FixedSingle
        pxz.Dock = DockStyle.Fill
        pxz.Location = New Point(0, 0)
        pxz.Name = "pxz"
        pxz.Size = New Size(537, 219)
        pxz.TabIndex = 0
        pxz.TabStop = False
        ' 
        ' frmGeometry
        ' 
        AutoScaleDimensions = New SizeF(96F, 96F)
        AutoScaleMode = AutoScaleMode.Dpi
        BackColor = Color.White
        ClientSize = New Size(979, 616)
        Controls.Add(sc1)
        Controls.Add(ToolStrip2)
        Controls.Add(ToolStrip1)
        Controls.Add(StatusStrip1)
        Font = New Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Name = "frmGeometry"
        StartPosition = FormStartPosition.CenterScreen
        Text = "AVL - Designer"
        StatusStrip1.ResumeLayout(False)
        StatusStrip1.PerformLayout()
        ToolStrip1.ResumeLayout(False)
        ToolStrip1.PerformLayout()
        CType(FileSystemWatcher1, ComponentModel.ISupportInitialize).EndInit()
        ToolStrip2.ResumeLayout(False)
        ToolStrip2.PerformLayout()
        sc1.Panel1.ResumeLayout(False)
        sc1.Panel2.ResumeLayout(False)
        CType(sc1, ComponentModel.ISupportInitialize).EndInit()
        sc1.ResumeLayout(False)
        scup.Panel1.ResumeLayout(False)
        scup.Panel2.ResumeLayout(False)
        CType(scup, ComponentModel.ISupportInitialize).EndInit()
        scup.ResumeLayout(False)
        tc1.ResumeLayout(False)
        CType(p3d, ComponentModel.ISupportInitialize).EndInit()
        CType(pxy, ComponentModel.ISupportInitialize).EndInit()
        scdown.Panel1.ResumeLayout(False)
        scdown.Panel2.ResumeLayout(False)
        CType(scdown, ComponentModel.ISupportInitialize).EndInit()
        scdown.ResumeLayout(False)
        CType(pyz, ComponentModel.ISupportInitialize).EndInit()
        CType(pxz, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()

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
    'Friend WithEvents txt3 As FastColoredTextBoxNS.FastColoredTextBox
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
    Friend WithEvents btnHover As ToolStripButton
    Friend WithEvents ImageList1 As ImageList
    Friend WithEvents p3d As PictureBox
End Class
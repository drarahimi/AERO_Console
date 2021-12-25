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
        Me.pxy = New System.Windows.Forms.PictureBox()
        Me.StatusStrip1 = New System.Windows.Forms.StatusStrip()
        Me.lblCursor = New System.Windows.Forms.ToolStripStatusLabel()
        Me.btnEditor = New System.Windows.Forms.ToolStripStatusLabel()
        Me.ToolStrip1 = New System.Windows.Forms.ToolStrip()
        Me.btnHelp = New System.Windows.Forms.ToolStripButton()
        Me.btnClear = New System.Windows.Forms.ToolStripButton()
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
        Me.ToolStripDropDownButton3 = New System.Windows.Forms.ToolStripDropDownButton()
        Me.btnTrefftz = New System.Windows.Forms.ToolStripMenuItem()
        Me.btnTest = New System.Windows.Forms.ToolStripMenuItem()
        Me.btnSaveView = New System.Windows.Forms.ToolStripMenuItem()
        Me.btnHide = New System.Windows.Forms.ToolStripButton()
        Me.ToolStripSeparator3 = New System.Windows.Forms.ToolStripSeparator()
        Me.ToolStripDropDownButton2 = New System.Windows.Forms.ToolStripDropDownButton()
        Me.btnSaveG = New System.Windows.Forms.ToolStripMenuItem()
        Me.btnLoadG = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator6 = New System.Windows.Forms.ToolStripSeparator()
        Me.btnSaveM = New System.Windows.Forms.ToolStripMenuItem()
        Me.btnLoadM = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator7 = New System.Windows.Forms.ToolStripSeparator()
        Me.btnSaveR = New System.Windows.Forms.ToolStripMenuItem()
        Me.btnLoadR = New System.Windows.Forms.ToolStripMenuItem()
        Me.SplitContainer1 = New System.Windows.Forms.SplitContainer()
        Me.tc1 = New System.Windows.Forms.TabControl()
        Me.tab3D = New System.Windows.Forms.TabPage()
        Me.p3d = New System.Windows.Forms.PictureBox()
        Me.tabSide = New System.Windows.Forms.TabPage()
        Me.SplitContainer2 = New System.Windows.Forms.SplitContainer()
        Me.pxz = New System.Windows.Forms.PictureBox()
        Me.txt3 = New FastColoredTextBoxNS.FastColoredTextBox()
        Me.lblNote = New System.Windows.Forms.Label()
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.FileSystemWatcher1 = New System.IO.FileSystemWatcher()
        Me.tlp1 = New System.Windows.Forms.TableLayoutPanel()
        Me.pyz = New System.Windows.Forms.PictureBox()
        Me.bg1 = New System.ComponentModel.BackgroundWorker()
        Me.ToolStrip2 = New System.Windows.Forms.ToolStrip()
        Me.btnZoomin = New System.Windows.Forms.ToolStripButton()
        Me.btnZoomout = New System.Windows.Forms.ToolStripButton()
        Me.btnBasefontplus = New System.Windows.Forms.ToolStripButton()
        Me.btnBasefontminus = New System.Windows.Forms.ToolStripButton()
        Me.btnDisplay = New System.Windows.Forms.ToolStripButton()
        Me.ToolStripLabel1 = New System.Windows.Forms.ToolStripLabel()
        Me.txtName = New System.Windows.Forms.ToolStripTextBox()
        Me.ToolStripSeparator4 = New System.Windows.Forms.ToolStripSeparator()
        CType(Me.pxy, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.StatusStrip1.SuspendLayout()
        Me.ToolStrip1.SuspendLayout()
        CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SplitContainer1.Panel1.SuspendLayout()
        Me.SplitContainer1.SuspendLayout()
        Me.tc1.SuspendLayout()
        Me.tab3D.SuspendLayout()
        CType(Me.p3d, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.tabSide.SuspendLayout()
        CType(Me.SplitContainer2, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SplitContainer2.SuspendLayout()
        CType(Me.pxz, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.txt3, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.FileSystemWatcher1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.tlp1.SuspendLayout()
        CType(Me.pyz, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.ToolStrip2.SuspendLayout()
        Me.SuspendLayout()
        '
        'pxy
        '
        Me.pxy.BackColor = System.Drawing.Color.White
        Me.pxy.Location = New System.Drawing.Point(190, 3)
        Me.pxy.Name = "pxy"
        Me.pxy.Size = New System.Drawing.Size(100, 50)
        Me.pxy.TabIndex = 0
        Me.pxy.TabStop = False
        '
        'StatusStrip1
        '
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
        Me.ToolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripDropDownButton3, Me.ToolStripSeparator4, Me.ToolStripLabel1, Me.txtName, Me.ToolStripDropDownButton2, Me.ToolStripDropDownButton1, Me.btnClear, Me.ToolStripSeparator3, Me.btnHelp, Me.btnHide})
        Me.ToolStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow
        Me.ToolStrip1.Location = New System.Drawing.Point(0, 0)
        Me.ToolStrip1.Name = "ToolStrip1"
        Me.ToolStrip1.Size = New System.Drawing.Size(979, 25)
        Me.ToolStrip1.TabIndex = 2
        Me.ToolStrip1.Text = "ToolStrip1"
        '
        'btnHelp
        '
        Me.btnHelp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnHelp.Image = CType(resources.GetObject("btnHelp.Image"), System.Drawing.Image)
        Me.btnHelp.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnHelp.Name = "btnHelp"
        Me.btnHelp.Size = New System.Drawing.Size(23, 22)
        Me.btnHelp.Text = "?"
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
        Me.AVLTemplateToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.AVLTemplateToolStripMenuItem.Text = "AVL Template"
        '
        'SurfaceToolStripMenuItem
        '
        Me.SurfaceToolStripMenuItem.Name = "SurfaceToolStripMenuItem"
        Me.SurfaceToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.SurfaceToolStripMenuItem.Text = "Surface"
        '
        'SectionToolStripMenuItem
        '
        Me.SectionToolStripMenuItem.Name = "SectionToolStripMenuItem"
        Me.SectionToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.SectionToolStripMenuItem.Text = "Section"
        '
        'ControlToolStripMenuItem
        '
        Me.ControlToolStripMenuItem.Name = "ControlToolStripMenuItem"
        Me.ControlToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.ControlToolStripMenuItem.Text = "Control"
        '
        'ToolStripSeparator1
        '
        Me.ToolStripSeparator1.Name = "ToolStripSeparator1"
        Me.ToolStripSeparator1.Size = New System.Drawing.Size(177, 6)
        '
        'MassTemplateToolStripMenuItem
        '
        Me.MassTemplateToolStripMenuItem.Name = "MassTemplateToolStripMenuItem"
        Me.MassTemplateToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.MassTemplateToolStripMenuItem.Text = "Mass Template"
        '
        'ToolStripSeparator2
        '
        Me.ToolStripSeparator2.Name = "ToolStripSeparator2"
        Me.ToolStripSeparator2.Size = New System.Drawing.Size(177, 6)
        '
        'RunTemplateToolStripMenuItem
        '
        Me.RunTemplateToolStripMenuItem.Name = "RunTemplateToolStripMenuItem"
        Me.RunTemplateToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.RunTemplateToolStripMenuItem.Text = "Run Template"
        '
        'ToolStripSeparator5
        '
        Me.ToolStripSeparator5.Name = "ToolStripSeparator5"
        Me.ToolStripSeparator5.Size = New System.Drawing.Size(177, 6)
        '
        'SeparatorToolStripMenuItem
        '
        Me.SeparatorToolStripMenuItem.Name = "SeparatorToolStripMenuItem"
        Me.SeparatorToolStripMenuItem.Size = New System.Drawing.Size(180, 22)
        Me.SeparatorToolStripMenuItem.Text = "Separator"
        '
        'ToolStripDropDownButton3
        '
        Me.ToolStripDropDownButton3.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right
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
        Me.btnTrefftz.Size = New System.Drawing.Size(227, 22)
        Me.btnTrefftz.Text = "Trefftz Plane"
        '
        'btnTest
        '
        Me.btnTest.Name = "btnTest"
        Me.btnTest.Size = New System.Drawing.Size(227, 22)
        Me.btnTest.Text = "Geometry"
        '
        'btnSaveView
        '
        Me.btnSaveView.Name = "btnSaveView"
        Me.btnSaveView.Size = New System.Drawing.Size(227, 22)
        Me.btnSaveView.Text = "Save AVL the show Geometry"
        '
        'btnHide
        '
        Me.btnHide.CheckOnClick = True
        Me.btnHide.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnHide.Image = CType(resources.GetObject("btnHide.Image"), System.Drawing.Image)
        Me.btnHide.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnHide.Name = "btnHide"
        Me.btnHide.Size = New System.Drawing.Size(23, 22)
        Me.btnHide.Text = "!"
        Me.btnHide.ToolTipText = "Show/Hide the note"
        '
        'ToolStripSeparator3
        '
        Me.ToolStripSeparator3.Name = "ToolStripSeparator3"
        Me.ToolStripSeparator3.Size = New System.Drawing.Size(6, 25)
        '
        'ToolStripDropDownButton2
        '
        Me.ToolStripDropDownButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.ToolStripDropDownButton2.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.btnSaveG, Me.btnLoadG, Me.ToolStripSeparator6, Me.btnSaveM, Me.btnLoadM, Me.ToolStripSeparator7, Me.btnSaveR, Me.btnLoadR})
        Me.ToolStripDropDownButton2.Image = CType(resources.GetObject("ToolStripDropDownButton2.Image"), System.Drawing.Image)
        Me.ToolStripDropDownButton2.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ToolStripDropDownButton2.Name = "ToolStripDropDownButton2"
        Me.ToolStripDropDownButton2.Size = New System.Drawing.Size(84, 22)
        Me.ToolStripDropDownButton2.Text = "Component"
        '
        'btnSaveG
        '
        Me.btnSaveG.Name = "btnSaveG"
        Me.btnSaveG.Size = New System.Drawing.Size(180, 22)
        Me.btnSaveG.Text = "Save AVL"
        '
        'btnLoadG
        '
        Me.btnLoadG.Name = "btnLoadG"
        Me.btnLoadG.Size = New System.Drawing.Size(180, 22)
        Me.btnLoadG.Text = "Load AVL"
        '
        'ToolStripSeparator6
        '
        Me.ToolStripSeparator6.Name = "ToolStripSeparator6"
        Me.ToolStripSeparator6.Size = New System.Drawing.Size(177, 6)
        '
        'btnSaveM
        '
        Me.btnSaveM.Name = "btnSaveM"
        Me.btnSaveM.Size = New System.Drawing.Size(180, 22)
        Me.btnSaveM.Text = "Save Mass"
        '
        'btnLoadM
        '
        Me.btnLoadM.Name = "btnLoadM"
        Me.btnLoadM.Size = New System.Drawing.Size(180, 22)
        Me.btnLoadM.Text = "Load Mass"
        '
        'ToolStripSeparator7
        '
        Me.ToolStripSeparator7.Name = "ToolStripSeparator7"
        Me.ToolStripSeparator7.Size = New System.Drawing.Size(177, 6)
        '
        'btnSaveR
        '
        Me.btnSaveR.Name = "btnSaveR"
        Me.btnSaveR.Size = New System.Drawing.Size(180, 22)
        Me.btnSaveR.Text = "Save Run"
        '
        'btnLoadR
        '
        Me.btnLoadR.Name = "btnLoadR"
        Me.btnLoadR.Size = New System.Drawing.Size(180, 22)
        Me.btnLoadR.Text = "Load Run"
        '
        'SplitContainer1
        '
        Me.SplitContainer1.Location = New System.Drawing.Point(12, 205)
        Me.SplitContainer1.Name = "SplitContainer1"
        '
        'SplitContainer1.Panel1
        '
        Me.SplitContainer1.Panel1.Controls.Add(Me.tc1)
        Me.SplitContainer1.Size = New System.Drawing.Size(520, 373)
        Me.SplitContainer1.SplitterDistance = 239
        Me.SplitContainer1.TabIndex = 3
        Me.SplitContainer1.Visible = False
        '
        'tc1
        '
        Me.tc1.Controls.Add(Me.tab3D)
        Me.tc1.Controls.Add(Me.tabSide)
        Me.tc1.Location = New System.Drawing.Point(17, 20)
        Me.tc1.Name = "tc1"
        Me.tc1.SelectedIndex = 0
        Me.tc1.Size = New System.Drawing.Size(219, 338)
        Me.tc1.TabIndex = 2
        '
        'tab3D
        '
        Me.tab3D.Controls.Add(Me.p3d)
        Me.tab3D.Location = New System.Drawing.Point(4, 22)
        Me.tab3D.Name = "tab3D"
        Me.tab3D.Padding = New System.Windows.Forms.Padding(3)
        Me.tab3D.Size = New System.Drawing.Size(211, 312)
        Me.tab3D.TabIndex = 0
        Me.tab3D.Text = "3D"
        Me.tab3D.UseVisualStyleBackColor = True
        '
        'p3d
        '
        Me.p3d.Location = New System.Drawing.Point(70, 113)
        Me.p3d.Name = "p3d"
        Me.p3d.Size = New System.Drawing.Size(100, 50)
        Me.p3d.TabIndex = 0
        Me.p3d.TabStop = False
        '
        'tabSide
        '
        Me.tabSide.Controls.Add(Me.SplitContainer2)
        Me.tabSide.Location = New System.Drawing.Point(4, 22)
        Me.tabSide.Name = "tabSide"
        Me.tabSide.Padding = New System.Windows.Forms.Padding(3)
        Me.tabSide.Size = New System.Drawing.Size(211, 312)
        Me.tabSide.TabIndex = 1
        Me.tabSide.Text = "Top and Side"
        Me.tabSide.UseVisualStyleBackColor = True
        '
        'SplitContainer2
        '
        Me.SplitContainer2.Location = New System.Drawing.Point(6, 6)
        Me.SplitContainer2.Name = "SplitContainer2"
        Me.SplitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.SplitContainer2.Size = New System.Drawing.Size(205, 242)
        Me.SplitContainer2.SplitterDistance = 163
        Me.SplitContainer2.TabIndex = 1
        '
        'pxz
        '
        Me.pxz.BackColor = System.Drawing.Color.White
        Me.pxz.Location = New System.Drawing.Point(190, 193)
        Me.pxz.Name = "pxz"
        Me.pxz.Size = New System.Drawing.Size(100, 50)
        Me.pxz.TabIndex = 0
        Me.pxz.TabStop = False
        '
        'txt3
        '
        Me.txt3.AutoCompleteBrackets = True
        Me.txt3.AutoCompleteBracketsList = New Char() {Global.Microsoft.VisualBasic.ChrW(40), Global.Microsoft.VisualBasic.ChrW(41), Global.Microsoft.VisualBasic.ChrW(123), Global.Microsoft.VisualBasic.ChrW(125), Global.Microsoft.VisualBasic.ChrW(91), Global.Microsoft.VisualBasic.ChrW(93), Global.Microsoft.VisualBasic.ChrW(34), Global.Microsoft.VisualBasic.ChrW(34), Global.Microsoft.VisualBasic.ChrW(39), Global.Microsoft.VisualBasic.ChrW(39)}
        Me.txt3.AutoScrollMinSize = New System.Drawing.Size(29, 18)
        Me.txt3.BackBrush = Nothing
        Me.txt3.CharHeight = 18
        Me.txt3.CharWidth = 9
        Me.txt3.CommentPrefix = "!|#"
        Me.txt3.Cursor = System.Windows.Forms.Cursors.IBeam
        Me.txt3.DisabledColor = System.Drawing.Color.FromArgb(CType(CType(100, Byte), Integer), CType(CType(180, Byte), Integer), CType(CType(180, Byte), Integer), CType(CType(180, Byte), Integer))
        Me.txt3.Font = New System.Drawing.Font("Consolas", 12.0!)
        Me.txt3.IsReplaceMode = False
        Me.txt3.Location = New System.Drawing.Point(3, 3)
        Me.txt3.Name = "txt3"
        Me.txt3.Paddings = New System.Windows.Forms.Padding(0)
        Me.txt3.SelectionColor = System.Drawing.Color.FromArgb(CType(CType(60, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.txt3.ServiceColors = CType(resources.GetObject("txt3.ServiceColors"), FastColoredTextBoxNS.ServiceColors)
        Me.txt3.Size = New System.Drawing.Size(136, 153)
        Me.txt3.TabIndex = 2
        Me.txt3.Zoom = 100
        '
        'lblNote
        '
        Me.lblNote.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblNote.Font = New System.Drawing.Font("Consolas", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblNote.Location = New System.Drawing.Point(0, 50)
        Me.lblNote.Name = "lblNote"
        Me.lblNote.Size = New System.Drawing.Size(979, 131)
        Me.lblNote.TabIndex = 4
        Me.lblNote.Text = resources.GetString("lblNote.Text")
        Me.lblNote.Visible = False
        '
        'Timer1
        '
        Me.Timer1.Interval = 200
        '
        'FileSystemWatcher1
        '
        Me.FileSystemWatcher1.EnableRaisingEvents = True
        Me.FileSystemWatcher1.SynchronizingObject = Me
        '
        'tlp1
        '
        Me.tlp1.ColumnCount = 2
        Me.tlp1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.tlp1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.tlp1.Controls.Add(Me.txt3, 0, 0)
        Me.tlp1.Controls.Add(Me.pxz, 1, 1)
        Me.tlp1.Controls.Add(Me.pxy, 1, 0)
        Me.tlp1.Controls.Add(Me.pyz, 0, 1)
        Me.tlp1.Location = New System.Drawing.Point(569, 197)
        Me.tlp1.Name = "tlp1"
        Me.tlp1.RowCount = 2
        Me.tlp1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.tlp1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.tlp1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
        Me.tlp1.Size = New System.Drawing.Size(374, 381)
        Me.tlp1.TabIndex = 5
        '
        'pyz
        '
        Me.pyz.BackColor = System.Drawing.Color.White
        Me.pyz.Location = New System.Drawing.Point(3, 193)
        Me.pyz.Name = "pyz"
        Me.pyz.Size = New System.Drawing.Size(100, 50)
        Me.pyz.TabIndex = 3
        Me.pyz.TabStop = False
        '
        'bg1
        '
        Me.bg1.WorkerReportsProgress = True
        Me.bg1.WorkerSupportsCancellation = True
        '
        'ToolStrip2
        '
        Me.ToolStrip2.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.btnZoomin, Me.btnZoomout, Me.btnBasefontplus, Me.btnBasefontminus, Me.btnDisplay})
        Me.ToolStrip2.Location = New System.Drawing.Point(0, 25)
        Me.ToolStrip2.Name = "ToolStrip2"
        Me.ToolStrip2.Size = New System.Drawing.Size(979, 25)
        Me.ToolStrip2.TabIndex = 6
        Me.ToolStrip2.Text = "ToolStrip2"
        '
        'btnZoomin
        '
        Me.btnZoomin.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right
        Me.btnZoomin.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnZoomin.Image = CType(resources.GetObject("btnZoomin.Image"), System.Drawing.Image)
        Me.btnZoomin.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnZoomin.Name = "btnZoomin"
        Me.btnZoomin.Size = New System.Drawing.Size(56, 22)
        Me.btnZoomin.Text = "Zoom in"
        '
        'btnZoomout
        '
        Me.btnZoomout.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right
        Me.btnZoomout.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnZoomout.Image = CType(resources.GetObject("btnZoomout.Image"), System.Drawing.Image)
        Me.btnZoomout.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnZoomout.Name = "btnZoomout"
        Me.btnZoomout.Size = New System.Drawing.Size(64, 22)
        Me.btnZoomout.Text = "Zoom out"
        '
        'btnBasefontplus
        '
        Me.btnBasefontplus.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnBasefontplus.Image = CType(resources.GetObject("btnBasefontplus.Image"), System.Drawing.Image)
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
        'btnDisplay
        '
        Me.btnDisplay.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.btnDisplay.Image = CType(resources.GetObject("btnDisplay.Image"), System.Drawing.Image)
        Me.btnDisplay.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.btnDisplay.Name = "btnDisplay"
        Me.btnDisplay.Size = New System.Drawing.Size(102, 22)
        Me.btnDisplay.Text = "Show Editor Only"
        '
        'ToolStripLabel1
        '
        Me.ToolStripLabel1.Name = "ToolStripLabel1"
        Me.ToolStripLabel1.Size = New System.Drawing.Size(80, 22)
        Me.ToolStripLabel1.Text = "Project name:"
        '
        'txtName
        '
        Me.txtName.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.txtName.Name = "txtName"
        Me.txtName.Size = New System.Drawing.Size(100, 25)
        Me.txtName.Text = "test"
        '
        'ToolStripSeparator4
        '
        Me.ToolStripSeparator4.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right
        Me.ToolStripSeparator4.Name = "ToolStripSeparator4"
        Me.ToolStripSeparator4.Size = New System.Drawing.Size(6, 25)
        '
        'frmGeometry
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(979, 616)
        Me.Controls.Add(Me.lblNote)
        Me.Controls.Add(Me.tlp1)
        Me.Controls.Add(Me.ToolStrip2)
        Me.Controls.Add(Me.SplitContainer1)
        Me.Controls.Add(Me.ToolStrip1)
        Me.Controls.Add(Me.StatusStrip1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "frmGeometry"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "AVL - Designer"
        CType(Me.pxy, System.ComponentModel.ISupportInitialize).EndInit()
        Me.StatusStrip1.ResumeLayout(False)
        Me.StatusStrip1.PerformLayout()
        Me.ToolStrip1.ResumeLayout(False)
        Me.ToolStrip1.PerformLayout()
        Me.SplitContainer1.Panel1.ResumeLayout(False)
        CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.SplitContainer1.ResumeLayout(False)
        Me.tc1.ResumeLayout(False)
        Me.tab3D.ResumeLayout(False)
        CType(Me.p3d, System.ComponentModel.ISupportInitialize).EndInit()
        Me.tabSide.ResumeLayout(False)
        CType(Me.SplitContainer2, System.ComponentModel.ISupportInitialize).EndInit()
        Me.SplitContainer2.ResumeLayout(False)
        CType(Me.pxz, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.txt3, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.FileSystemWatcher1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.tlp1.ResumeLayout(False)
        CType(Me.pyz, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ToolStrip2.ResumeLayout(False)
        Me.ToolStrip2.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents pxy As PictureBox
    Friend WithEvents StatusStrip1 As StatusStrip
    Friend WithEvents lblCursor As ToolStripStatusLabel
    Friend WithEvents ToolStrip1 As ToolStrip
    Friend WithEvents SplitContainer1 As SplitContainer
    Friend WithEvents ToolStripDropDownButton1 As ToolStripDropDownButton
    Friend WithEvents AVLTemplateToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents SurfaceToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents SectionToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ControlToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator1 As ToolStripSeparator
    Friend WithEvents SeparatorToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents btnClear As ToolStripButton
    Friend WithEvents lblNote As Label
    Friend WithEvents btnHelp As ToolStripButton
    Friend WithEvents txt3 As FastColoredTextBoxNS.FastColoredTextBox
    Friend WithEvents btnHide As ToolStripButton
    Friend WithEvents MassTemplateToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator2 As ToolStripSeparator
    Friend WithEvents ToolStripSeparator3 As ToolStripSeparator
    Friend WithEvents RunTemplateToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator5 As ToolStripSeparator
    Friend WithEvents SplitContainer2 As SplitContainer
    Friend WithEvents pxz As PictureBox
    Friend WithEvents btnEditor As ToolStripStatusLabel
    Friend WithEvents tc1 As TabControl
    Friend WithEvents tab3D As TabPage
    Friend WithEvents tabSide As TabPage
    Friend WithEvents Timer1 As Timer
    Friend WithEvents p3d As PictureBox
    Friend WithEvents FileSystemWatcher1 As IO.FileSystemWatcher
    Friend WithEvents tlp1 As TableLayoutPanel
    Friend WithEvents pyz As PictureBox
    Friend WithEvents bg1 As System.ComponentModel.BackgroundWorker
    Friend WithEvents ToolStrip2 As ToolStrip
    Friend WithEvents btnZoomin As ToolStripButton
    Friend WithEvents btnZoomout As ToolStripButton
    Friend WithEvents btnBasefontplus As ToolStripButton
    Friend WithEvents btnBasefontminus As ToolStripButton
    Friend WithEvents btnDisplay As ToolStripButton
    Friend WithEvents ToolStripDropDownButton2 As ToolStripDropDownButton
    Friend WithEvents btnSaveG As ToolStripMenuItem
    Friend WithEvents btnLoadG As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator6 As ToolStripSeparator
    Friend WithEvents btnSaveM As ToolStripMenuItem
    Friend WithEvents btnLoadM As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator7 As ToolStripSeparator
    Friend WithEvents btnSaveR As ToolStripMenuItem
    Friend WithEvents btnLoadR As ToolStripMenuItem
    Friend WithEvents ToolStripDropDownButton3 As ToolStripDropDownButton
    Friend WithEvents btnTrefftz As ToolStripMenuItem
    Friend WithEvents btnTest As ToolStripMenuItem
    Friend WithEvents btnSaveView As ToolStripMenuItem
    Friend WithEvents ToolStripLabel1 As ToolStripLabel
    Friend WithEvents txtName As ToolStripTextBox
    Friend WithEvents ToolStripSeparator4 As ToolStripSeparator
End Class

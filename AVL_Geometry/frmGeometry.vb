Option Explicit On
Option Strict On

Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Drawing.Text
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Windows.Controls
Imports System.Windows.Forms.VisualStyles.VisualStyleElement
Imports System.Windows.Media.Media3D
Imports FastColoredTextBoxNS
Imports Microsoft.SqlServer

Public Class frmGeometry
    Private pxySvg As String = ""
    Private pxyPdf As String = ""
    Private pxzSvg As String = ""
    Private pxzPdf As String = ""
    Private pyzSvg As String = ""
    Private pyzPdf As String = ""
    Private p3dSvg As String = ""
    Private p3dPdf As String = ""
    Private trefftzSvg As String = ""
    Private trefftzPdf As String = ""
    Private Trefftz As TabPage
    Private pTrefftz As PictureBox
    Private btnRunTrefftz As New System.Windows.Forms.Button()
    Private _lastTrefftzSurfaces As List(Of TrefftzSurface) = Nothing
    Private _lastTrefftzCref As Double = 1.0
    Private _lastTrefftzLog As String = ""
    Private _lastTrefftzRawFile As String = ""
    Private _lastTrefftzTotals As TrefftzTotals = Nothing

    Private loadsSvg As String = ""
    Private loadsPdf As String = ""
    Private Loads As TabPage
    Private pLoads As PictureBox
    Private btnLoads As New System.Windows.Forms.Button()
    Private _lastVmSurfaces As List(Of VmSurface) = Nothing
    Private _lastVmLog As String = ""

    Private polarSvg As String = ""
    Private polarPdf As String = ""
    Private Polar As TabPage
    Private pPolar As PictureBox
    Private txtPolarMin As New System.Windows.Forms.TextBox()
    Private txtPolarMax As New System.Windows.Forms.TextBox()
    Private txtPolarStep As New System.Windows.Forms.TextBox()
    Private btnRunPolar As New System.Windows.Forms.Button()
    Private _lastPolarPoints As List(Of PolarPoint) = Nothing
    Private _lastPolarLog As String = ""

    Private Derivatives As TabPage
    Private txtDerivatives As New System.Windows.Forms.TextBox()
    Private btnRunDerivatives As New System.Windows.Forms.Button()
    Private btnExportDerivatives As New System.Windows.Forms.Button()
    Private _lastDerivativesText As String = ""

    Private feSvg As String = ""
    Private fePdf As String = ""
    Private FE As TabPage
    Private pFE As PictureBox
    Private btnRunFE As New System.Windows.Forms.Button()
    Private cmbFeStrip As New System.Windows.Forms.ComboBox()
    Private _lastFeStrips As List(Of FeStrip) = Nothing
    Private _lastFeLog As String = ""

    Private modesSvg As String = ""
    Private modesPdf As String = ""
    Private ModesTab As TabPage
    Private pModes As PictureBox
    Private btnRunModes As New System.Windows.Forms.Button()
    Private btnModeTips As New System.Windows.Forms.Button()
    Private _lastEigenvalues As List(Of EigenValue) = Nothing
    Private _lastModesLog As String = ""
    Private modesTip As New System.Windows.Forms.ToolTip()
    Private _lastModesHoverIndex As Integer = -1

    Private btnLoadTestProject As New ToolStripButton()

    ' Properties panel — click a Section/Control node in any view to edit its
    ' fields directly, without hand-editing the raw text (which remains the
    ' source of truth; this panel just reads/writes the same file+line).
    Private pnlProperties As System.Windows.Forms.Panel
    Private pnlSectionFields As System.Windows.Forms.Panel
    Private pnlControlFields As System.Windows.Forms.Panel
    Private pnlMassFields As System.Windows.Forms.Panel
    Private lblPropHeader As System.Windows.Forms.Label
    Private lblPropAirfoilKind As System.Windows.Forms.Label
    Private txtPropXle As New System.Windows.Forms.TextBox()
    Private txtPropYle As New System.Windows.Forms.TextBox()
    Private txtPropZle As New System.Windows.Forms.TextBox()
    Private txtPropChord As New System.Windows.Forms.TextBox()
    Private txtPropAinc As New System.Windows.Forms.TextBox()
    Private txtPropAirfoil As New System.Windows.Forms.TextBox()
    Private txtPropCname As New System.Windows.Forms.TextBox()
    Private txtPropCgain As New System.Windows.Forms.TextBox()
    Private txtPropXhinge As New System.Windows.Forms.TextBox()
    Private txtPropHx As New System.Windows.Forms.TextBox()
    Private txtPropHy As New System.Windows.Forms.TextBox()
    Private txtPropHz As New System.Windows.Forms.TextBox()
    Private txtPropSgnDup As New System.Windows.Forms.TextBox()
    Private txtPropMassVal As New System.Windows.Forms.TextBox()
    Private txtPropMassX As New System.Windows.Forms.TextBox()
    Private txtPropMassY As New System.Windows.Forms.TextBox()
    Private txtPropMassZ As New System.Windows.Forms.TextBox()
    Private txtPropIxx As New System.Windows.Forms.TextBox()
    Private txtPropIyy As New System.Windows.Forms.TextBox()
    Private txtPropIzz As New System.Windows.Forms.TextBox()
    Private btnPropApply As New System.Windows.Forms.Button()
    Private btnPropClose As New System.Windows.Forms.Button()
    Private propHelpTip As New System.Windows.Forms.ToolTip()
    Private _selectedNode As Node = Nothing
    Private mouseDownScreenPos As System.Drawing.Point = System.Drawing.Point.Empty

    ' Structure tree — Surface/Section/Control block navigator with drag-and-drop
    ' reordering/reassignment. Operates on whole text blocks (delimited by this
    ' app's own !beginX/!endX markers) rather than single lines.
    Private pnlStructureTree As System.Windows.Forms.Panel
    Private tvStructure As System.Windows.Forms.TreeView
    Private btnToggleStructureTree As New ToolStripButton()

    Dim xmax As Double = 10
    Dim xmin As Double = -10
    Dim ymax As Double = 10
    Dim ymin As Double = -10
    Dim zmax As Double = 10
    Dim zmin As Double = -10
    Dim gridnumber As Integer = 2
    Dim gridnumbermini As Integer = 4
    Dim gridstep As Integer
    Dim xoffset As Double = 0
    Dim yoffset As Double = 0
    Dim zoffset As Double = 0
    Dim xdown As Double
    Dim ydown As Double
    Dim zdown As Double
    Dim points As List(Of Node) = New List(Of Node)
    Dim curX As Double
    Dim curY As Double
    Dim curZ As Double
    Dim popupMenu As FastColoredTextBoxNS.AutocompleteMenu
    Dim blueStyle As TextStyle = New TextStyle(Brushes.Blue, Nothing, FontStyle.Regular)
    Dim greenStyle As TextStyle = New TextStyle(Brushes.Green, Nothing, FontStyle.Bold)
    Dim lightgreenStyle As TextStyle = New TextStyle(Brushes.Green, Nothing, FontStyle.Regular)
    Dim redStyle As TextStyle = New TextStyle(Brushes.Red, Nothing, FontStyle.Italic)
    Dim purpleStyle As TextStyle = New TextStyle(Brushes.Purple, Nothing, FontStyle.Underline)
    Dim ellipseStyle1 As EllipseStyle = New EllipseStyle(Color.Red)
    Dim ellipseStyle2 As EllipseStyle = New EllipseStyle(Color.Blue)
    Dim ellipseStyle3 As EllipseStyle = New EllipseStyle(Color.Cyan)
    Dim ellipseStyle4 As EllipseStyle = New EllipseStyle(Color.Magenta)
    Dim pAxis As Pen = New Pen(Color.Black)
    Dim pGrid As Pen = New Pen(Color.LightGray) With {
        .DashStyle = Drawing2D.DashStyle.Dash}
    Dim pDot As Pen = New Pen(Color.Red)
    Dim bPolySurface As Brush = Brushes.Aqua
    Dim bPolyAilern As Brush = Brushes.Yellow
    Dim bPolyFlap As Brush = Brushes.Gold
    Dim bPolyElevator As Brush = Brushes.LightGoldenrodYellow
    Dim bPolyRudder As Brush = Brushes.LightYellow
    Dim baseFontsize As Integer = 3
    Private _cachedAxisFont As Font = Nothing
    Private _cachedTickFont As Font = Nothing
    Private _cachedTickFontSize As Single = Single.NaN
    Private ReadOnly _fontCacheLock As New Object()
    Dim isHovered As Boolean = False
    Private btnAutosave As ToolStripButton = Nothing
    Private btnSave As ToolStripButton = Nothing
    Private lblDirtyWarning As ToolStripLabel = Nothing
    Private isDirty As Boolean = False
    Private lastActiveTabName As String = "Geometry"
    Private currentToolTipText As String = ""
    Dim rootPath As String = Application.StartupPath + "\appdata"
    Public projectName As String = "test"
    Dim updating As Boolean = False
    Public help As String = rootPath + "\avl_doc.txt"
    Dim autoSpace As Boolean = True
    Dim showMass As Boolean = True
    Dim showControl As Boolean = True
    Dim showSection As Boolean = True
    Dim showMesh As Boolean = False
    Dim show3D As Boolean = False
    Dim showHover As Boolean = True
    Dim parsedSurfaces As List(Of Surface) = New List(Of Surface)
    ' Add these to your variable declarations at the top of frmGeometry
    Dim viewAlpha As Single = 0 ' Yaw (Rotation around Y)
    Dim viewBeta As Single = 0   ' Pitch (Rotation around X)
    Dim viewGamma As Single = 0   ' Roll (Rotation around Z)
    Dim viewDist As Single = 200  ' Distance from camera (Zoom)
    Dim viewFOV As Single = 500   ' Field of View scale
    Dim lastMouseLoc As Point
    Dim txt3 As ModernFastColoredTextBox
    Dim firstFitAfterLoad As Boolean = False

    ' --- Drag-nodes mode state ---
    Dim isDragMode As Boolean = False
    Dim isDragging As Boolean = False
    Dim draggingNode As Node = Nothing
    Dim dragStartX As Double = 0
    Dim dragStartY As Double = 0
    Dim dragStartZ As Double = 0

    ' 1. CANCELLATION: Replaces WorkerSupportsCancellation
    Private _cts As CancellationTokenSource

    ' Render throttle: instead of firing a full async render on every event,
    ' callers just set _renderPending = True and the timer fires at most ~30fps.
    Private _renderPending As Boolean = False
    Private WithEvents _renderTimer As New System.Windows.Forms.Timer With {.Interval = 30}

    ' Set to True before calling drawAxes() when SVG/PDF export data is needed.
    ' Building the SVG/PDF StringBuilder on every frame is expensive; skip it during normal interaction.
    Private _captureVectors As Boolean = False

    Dim frames As Integer = 0
    Dim lastFrameTime As DateTime = Now

    ' Assuming you have a PictureBox named p3d, if not, add one to your designer

    Structure Section
        Dim Xle As Double
        Dim Yle As Double
        Dim Zle As Double
        Dim Chord As Double
        Dim Ainc As Double
        Dim Nspanwise As Double
        Dim Sspace As Double
        Dim lineNumber As Integer
        Dim controls As List(Of ControlSurface)
    End Structure
    Structure Surface
        Dim Name As String
        Dim yDuplicate As Boolean
        Dim yDuplicatevalue As Double
        Dim Nchordwise As Double
        Dim Cspace As Double
        Dim Nspanwise As Double
        Dim Sspace As Double
        Dim sections As List(Of Section)
    End Structure
    Structure ControlSurface
        Dim lineNumber As Integer
        Dim Type As String
        Dim Cgain As Double
        Dim Xhinge As Double
    End Structure
    Structure Blocks
        Dim GeometryBeginBefore As Integer
        Dim GeometryBeginAfter As Integer
        Dim GeometryEndBefore As Integer
        Dim GeometryEndAfter As Integer
        Dim SurfaceBeginBefore As Integer
        Dim SurfaceBeginAfter As Integer
        Dim SurfaceEndBefore As Integer
        Dim SurfaceEndAfter As Integer
        Dim SectionBeginBefore As Integer
        Dim SectionBeginAfter As Integer
        Dim SectionEndBefore As Integer
        Dim SectionEndAfter As Integer
        Dim ControlBeginBefore As Integer
        Dim ControlBeginAfter As Integer
        Dim ControlEndBefore As Integer
        Dim ControlEndAfter As Integer
    End Structure

    Private Sub frmGeometry_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        txt3 = New ModernFastColoredTextBox()
        txt3.Dock = DockStyle.Fill

        txt3.Font = New System.Drawing.Font("Consolas", 12)
        txt3.Cursor = System.Windows.Forms.Cursors.IBeam

        ' Highlights the line the cursor is currently on (Visual improvement)
        txt3.CurrentLineColor = System.Drawing.Color.FromArgb(50, 220, 220, 220)

        ' Selection color (kept yours, just cleaned up syntax)
        txt3.SelectionColor = System.Drawing.Color.FromArgb(60, 0, 0, 255)

        ' Better Line Number spacing
        txt3.ReservedCountOfLineNumberChars = 4 ' Allows up to 9999 lines without resizing
        txt3.LeftPadding = 5 ' Adds breathing room between border and text

        ' --- PERFORMANCE SETTINGS ---
        ' Only recalculate highlighting for what is visible on screen
        txt3.HighlightingRangeType = FastColoredTextBoxNS.HighlightingRangeType.VisibleRange

        ' Delays syntax check slightly to ensure typing stays buttery smooth
        txt3.DelayedEventsInterval = 200
        txt3.DelayedTextChangedInterval = 200

        ' If your lines are very long, turning this OFF improves performance significantly
        txt3.WordWrap = False

        ' --- BEHAVIOR ---
        txt3.ShowFoldingLines = True
        txt3.IsReplaceMode = False
        txt3.AutoScrollMinSize = New System.Drawing.Size(0, 0) ' Let it auto-calculate
        txt3.BackBrush = Nothing
        txt3.Paddings = New System.Windows.Forms.Padding(0)
        txt3.Zoom = 100
        txt3.TabIndex = 2

        ' --- AUTOMATION & SYNTAX ---
        txt3.AutoCompleteBrackets = True

        ' Cleaned up Char list using VB Literals
        txt3.AutoCompleteBracketsList = New Char() {
                                            "("c, ")"c,
                                            "{"c, "}"c,
                                            "["c, "]"c,
                                            """"c, """"c,
                                            "'"c, "'"c
                                        }

        txt3.AutoIndentCharsPatterns = ""
        txt3.AutoIndent = True

        ' Optional: Set language if known (e.g., CSharp, VB, JSON) for built-in speed
        ' txt3.Language = FastColoredTextBoxNS.Language.CSharp

        Geometry.Controls.Add(txt3)

        ' Add floating editor action buttons directly on the parent tab page (Geometry)
        ' so that they stay stationary and do not scroll with txt3 text content
        Geometry.Controls.Add(btnAdd)
        Geometry.Controls.Add(btnPrettify)
        Geometry.Controls.Add(btnUndo)
        Geometry.Controls.Add(btnRedo)
        Geometry.Controls.Add(btnClear)
        btnAdd.BringToFront()
        btnPrettify.BringToFront()
        btnUndo.BringToFront()
        btnRedo.BringToFront()
        btnClear.BringToFront()

        ' Bind tooltips to floating editor buttons
        Dim floatTooltip As New System.Windows.Forms.ToolTip()
        floatTooltip.SetToolTip(btnAdd, "Add Template or Element")
        floatTooltip.SetToolTip(btnPrettify, "Prettify: auto-indent and column-align the editor content")
        floatTooltip.SetToolTip(btnUndo, "Undo (Ctrl+Z)")
        floatTooltip.SetToolTip(btnRedo, "Redo (Ctrl+Y)")
        floatTooltip.SetToolTip(btnClear, "Clear Editor Content")

        AddHandler txt3.TextChangedDelayed, AddressOf txt3_TextChangedDelayed
        AddHandler txt3.ToolTipNeeded, AddressOf txt3_ToolTipNeeded
        AddHandler txt3.TextChanged, Sub(s, ev)
                                         UpdateUndoRedoState()
                                         ApplySyntaxHighlighting()
                                     End Sub
        AddHandler txt3.Leave, Sub(s, ev) FormatActiveText()
        AddHandler txt3.AutoIndentNeeded, AddressOf txt3_AutoIndentNeeded

        ' Customize ToolTip appearance
        txt3.ToolTip.OwnerDraw = True
        AddHandler txt3.ToolTip.Popup, AddressOf ToolTip_Popup
        AddHandler txt3.ToolTip.Draw, AddressOf ToolTip_Draw

        ' Inject dirty warning label
        lblDirtyWarning = New ToolStripLabel(" * Unsaved Changes * ")
        lblDirtyWarning.ForeColor = Color.Red
        lblDirtyWarning.Font = New Font(lblDirtyWarning.Font, FontStyle.Bold)
        lblDirtyWarning.Visible = False
        lblDirtyWarning.Name = "lblDirtyWarning"

        ' Inject Save button
        btnSave = New ToolStripButton("Save")
        btnSave.Name = "btnSave"
        btnSave.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnSave.BackColor = Color.FromArgb(220, 220, 220)
        btnSave.ToolTipText = "Save the active file to disk (Ctrl+S)"
        btnSave.Visible = Not My.Settings.autoSave
        AddHandler btnSave.Click, AddressOf btnSave_Click

        ' Inject Autosave toggle button
        btnAutosave = New ToolStripButton()
        btnAutosave.Name = "btnAutosave"
        btnAutosave.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnAutosave.Text = If(My.Settings.autoSave, "Autosave: On", "Autosave: Off")
        btnAutosave.BackColor = If(My.Settings.autoSave, Color.FromArgb(192, 255, 192), Color.FromArgb(255, 192, 192))
        btnAutosave.ToolTipText = "Toggle auto-save of AVL, mass, and run files"
        AddHandler btnAutosave.Click, AddressOf btnAutosave_Click

        Dim hoverIndex = ToolStrip1.Items.IndexOf(btnHover)
        If hoverIndex >= 0 Then
            ToolStrip1.Items.Insert(hoverIndex + 1, New ToolStripSeparator())
            ToolStrip1.Items.Insert(hoverIndex + 2, lblDirtyWarning)
            ToolStrip1.Items.Insert(hoverIndex + 3, btnSave)
            ToolStrip1.Items.Insert(hoverIndex + 4, btnAutosave)
        Else
            ToolStrip1.Items.Add(New ToolStripSeparator())
            ToolStrip1.Items.Add(lblDirtyWarning)
            ToolStrip1.Items.Add(btnSave)
            ToolStrip1.Items.Add(btnAutosave)
        End If

        ' Bind Ctrl+S shortcut to save active file
        AddHandler txt3.KeyDown, Sub(s, ev)
                                     If ev.Control AndAlso ev.KeyCode = Keys.S Then
                                         ev.SuppressKeyPress = True
                                         If btnSave.Visible Then
                                             btnSave_Click(Nothing, Nothing)
                                         End If
                                     End If
                                 End Sub


        'popupMenu = New FastColoredTextBoxNS.AutocompleteMenu(txt3)
        'popupMenu.MinFragmentLength = 2
        Dim keyWords As List(Of String) = New List(Of String)
        keyWords.AddRange("section|control|Mach|IYsym|IZsym|Zsym|Sref|Cref|Bref|Xref|Yref|Zref|Nchordwise|Cspace|Nspanwise|Sspace|Xle|Yle|Zle|Chord|Ainc|Nspanwise|Sspace|Cname|Cgain|Xhinge|HingeVec|SgnDup|YDUPLICATE|ANGLE".Split(CChar("|")))
        'popupMenu.Items.SetAutocompleteItems(keyWords)
        'tlp1.Dock = DockStyle.Fill
        sc1.Dock = DockStyle.Fill
        tc1.Dock = DockStyle.Fill

        pxy.Dock = DockStyle.Fill
        pxz.Dock = DockStyle.Fill
        pyz.Dock = DockStyle.Fill
        txt3.Dock = DockStyle.Fill
        frmMain.SetAllControlsFont(Me.Controls, frmMain.systemFont)
        drawAxes()


        ' Initialize warning label dynamically
        Dim lblWarning As New ToolStripLabel()
        lblWarning.Name = "lblWarning"
        lblWarning.ForeColor = Color.OrangeRed
        lblWarning.Font = New Font(ToolStrip1.Font, FontStyle.Bold)
        lblWarning.Text = "⚠️ Warning: Project name is empty. Saving, autosaving, and analysis are disabled!"
        ToolStrip1.Items.Add(lblWarning)

        AddHandler txtName.KeyDown, AddressOf txtName_KeyDown
        AddHandler txtName.Leave, AddressOf txtName_Leave

        AddHandler txtName.ComboBox.GotFocus, Sub(s, ev) ClearPlaceholder()
        AddHandler txtName.ComboBox.LostFocus, Sub(s, ev) SetPlaceholder()

        frmMain.findAVLs(Environment.CurrentDirectory)
        If (frmMain.txtName.Text <> "") Then
            txtName.Text = frmMain.txtName.Text
            If txtName.Text = "Enter AVL Project (e.g. glider)" OrElse txtName.Text = "Enter NACA (e.g. 2412) or dat file" Then
                txtName.ComboBox.ForeColor = Color.Gray
            Else
                txtName.ComboBox.ForeColor = Color.Black
            End If
        Else
            SetPlaceholder()
        End If

        UpdateGeometryTitle()
        UpdateProjectWarning()
        InitializeExportButtons()
        InitializeTrefftzTab()
        InitializeLoadsTab()
        InitializePolarTab()
        InitializeDerivativesTab()
        InitializeFETab()
        InitializeModesTab()
        InitializeTestProjectButton()
        InitializePropertiesPanel()
        InitializeStructureTreePanel()

        ' Initialize drag nodes button state
        btnDragMode.Text = "Drag Nodes: Off"
        btnDragMode.BackColor = Color.FromArgb(220, 220, 220)

        ' Enable double-buffering recursively on all controls to prevent hover/draw flicker
        EnableDoubleBuffering(Me)

        ' Set ComboBox FlatStyle and double-buffering directly
        Try
            Dim dbProp = GetType(System.Windows.Forms.Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic Or System.Reflection.BindingFlags.Instance)
            If dbProp IsNot Nothing Then
                If txtName IsNot Nothing AndAlso txtName.ComboBox IsNot Nothing Then
                    dbProp.SetValue(txtName.ComboBox, True, Nothing)
                    txtName.ComboBox.FlatStyle = FlatStyle.Flat
                End If
            End If
        Catch
        End Try

        _renderTimer.Start()
    End Sub

    'Public Sub findAVLs(path As String)
    '    Dim files() As String
    '    files = Directory.GetFiles(path, "*.avl", SearchOption.TopDirectoryOnly)
    '    txtName.Items.Clear()
    '    For Each FileName As String In files
    '        'Console.WriteLine(FileName)
    '        txtName.Items.Add(System.IO.Path.GetFileNameWithoutExtension(FileName))
    '    Next
    '    If (txtName.Items.Count > 0) Then
    '        txtName.SelectedIndex = 0
    '    End If

    'End Sub

    Public Sub loadTemplate()
        Dim value As String = File.ReadAllText(Path.Combine(rootPath, "template_avl.txt"))
        txt3.Text = value
    End Sub

    ' Marks the view as dirty. The render timer will pick it up within 30ms.
    ' Safe to call from any thread or event at any frequency.
    Public Sub drawAxes()
        _renderPending = True
        If Not _renderTimer.Enabled Then _renderTimer.Start()
    End Sub

    ' Fires at most every 30ms; only renders when something changed.
    Private Async Sub _renderTimer_Tick(sender As Object, e As EventArgs) Handles _renderTimer.Tick
        If Not _renderPending Then Return
        _renderPending = False

        If _cts IsNot Nothing Then Return   ' render already in flight; will re-check next tick
        _cts = New CancellationTokenSource()
        Dim token As CancellationToken = _cts.Token

        Try
            Await Task.Run(Sub() HeavyRender(Nothing, token))
        Catch ex As OperationCanceledException
        Catch ex As Exception
        Finally
            If _cts IsNot Nothing Then _cts.Dispose()
            _cts = Nothing
            ' If another redraw was requested while we were rendering, keep timer alive
            If Not _renderPending Then _renderTimer.Stop()
        End Try
    End Sub

    Public Function findname(ByVal l As List(Of Node), ByVal name As String) As Integer
        Dim result As Integer = -1
        For i = 0 To l.Count - 1
            If l(i).Surface = name Then
                result = i
                Exit For
            End If
        Next
        Return result
    End Function

    ' Returns the N+1 normalised break positions (0..1) for N panels using AVL spacing codes.
    ' Sspace > 0 → cosine (clustered at both ends)
    ' Sspace < 0 → sine-half (clustered at root / first end only)
    ' Sspace = 0 → uniform
    Private Function GetPanelBreaks(N As Integer, Sspace As Double) As Double()
        Dim n1 As Integer = CInt(N)
        If n1 < 1 Then n1 = 1
        Dim breaks(n1) As Double
        For k = 0 To n1
            Dim t As Double = CDbl(k) / n1
            If Math.Abs(Sspace) > 0.1 Then
                ' cosine / sine clustering
                breaks(k) = 0.5 * (1.0 - Math.Cos(Math.PI * t))
            Else
                breaks(k) = t
            End If
        Next
        Return breaks
    End Function

    ' Draws the AVL vortex-lattice panel mesh for one surface onto G.
    ' proj selects which 2 world coordinates map to screen X/Y.
    '   "XY" → world X→screenX, world Y→screenY
    '   "XZ" → world X→screenX, world Z→screenY
    '   "YZ" → world Y→screenX, world Z→screenY
    Private Sub DrawMeshForSurface(G As SvgGraphics, su As Surface, proj As String,
                                    W As Integer, H As Integer,
                                    hMin As Double, hMax As Double,
                                    vMin As Double, vMax As Double,
                                    hOff As Double, vOff As Double,
                                    meshPen As Pen, isDuplicate As Boolean)
        Dim sects = su.sections
        If sects Is Nothing OrElse sects.Count < 2 Then Return
        Dim Nc As Integer = Math.Max(1, CInt(If(su.Nchordwise > 0, su.Nchordwise, 8)))
        Dim cBreaks = GetPanelBreaks(Nc, su.Cspace)
        Dim Ns As Integer = Math.Max(1, CInt(If(su.Nspanwise > 0, su.Nspanwise, 12)))
        Dim sBreaks = GetPanelBreaks(Ns, su.Sspace)
        Dim ySign As Double = If(isDuplicate, -1.0, 1.0)
        Dim yDupVal As Double = If(su.yDuplicate, su.yDuplicatevalue, 0.0)

        ' Iterate over each spanwise panel segment (between consecutive sections)
        For seg = 0 To sects.Count - 2
            Dim s0 As Section = sects(seg)
            Dim s1 As Section = sects(seg + 1)

            ' Effective Y including yDuplicate offset
            Dim y0Eff As Double = (s0.Yle + yDupVal) * ySign
            Dim y1Eff As Double = (s1.Yle + yDupVal) * ySign

            ' Determine the number of spanwise panels for this segment
            ' (Use per-section Nspanwise if set, else fall back to surface level)
            Dim segNs As Integer = If(s0.Nspanwise > 0, CInt(s0.Nspanwise), Ns)
            Dim segSp As Double = If(s0.Nspanwise > 0, s0.Sspace, su.Sspace)
            Dim segSBreaks = GetPanelBreaks(segNs, segSp)

            ' Spanwise boundary lines (constant t along span, varying chord fraction)
            For ki = 0 To segSBreaks.Length - 1
                Dim t As Double = segSBreaks(ki)
                ' Interpolate section geometry at this spanwise station
                Dim xle As Double = s0.Xle + t * (s1.Xle - s0.Xle)
                Dim yle As Double = y0Eff + t * (y1Eff - y0Eff)
                Dim zle As Double = s0.Zle + t * (s1.Zle - s0.Zle)
                Dim chord As Double = s0.Chord + t * (s1.Chord - s0.Chord)
                Dim xte As Double = xle + chord

                Dim isLeVisible As Boolean = True
                Dim isTeVisible As Boolean = True
                If proj = "3D" Then
                    Dim pLe As New Node(xle, yle, zle, "", False, 0, Node.NodeType.Geometry)
                    Dim pTe As New Node(xte, yle, zle, "", False, 0, Node.NodeType.Geometry)
                    isLeVisible = pLe.RotateY(viewAlpha).RotateX(viewBeta).RotateZ(viewGamma).Project(W, H, viewDist, viewFOV).Visible
                    isTeVisible = pTe.RotateY(viewAlpha).RotateX(viewBeta).RotateZ(viewGamma).Project(W, H, viewDist, viewFOV).Visible
                End If

                If isLeVisible AndAlso isTeVisible Then
                    ' Convert world → screen for LE and TE of this spanwise station
                    Dim leS = WorldToScreen(xle, yle, zle, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff)
                    Dim teS = WorldToScreen(xte, yle, zle, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff)

                    ' Draw the chordwise line (LE to TE) at this spanwise boundary
                    G.DrawLine(meshPen, leS, teS)
                End If
            Next

            ' Chordwise panel lines: draw Nc-1 lines at fixed chord fractions across the full span segment
            For ki = 0 To cBreaks.Length - 1
                Dim tc As Double = cBreaks(ki)
                ' Sample a few spanwise stations to draw a smooth spanwise line at this chord fraction
                Dim pts As New List(Of PointF)
                Dim isSpanVisible As Boolean = True
                For si = 0 To segSBreaks.Length - 1
                    Dim ts As Double = segSBreaks(si)
                    Dim xle As Double = s0.Xle + ts * (s1.Xle - s0.Xle)
                    Dim yle As Double = y0Eff + ts * (y1Eff - y0Eff)
                    Dim zle As Double = s0.Zle + ts * (s1.Zle - s0.Zle)
                    Dim chord As Double = s0.Chord + ts * (s1.Chord - s0.Chord)
                    Dim wx As Double = xle + tc * chord

                    If proj = "3D" Then
                        Dim pPt As New Node(wx, yle, zle, "", False, 0, Node.NodeType.Geometry)
                        If Not pPt.RotateY(viewAlpha).RotateX(viewBeta).RotateZ(viewGamma).Project(W, H, viewDist, viewFOV).Visible Then
                            isSpanVisible = False
                        End If
                    End If
                    pts.Add(WorldToScreen(wx, yle, zle, proj, W, H, hMin, hMax, vMin, vMax, hOff, vOff))
                Next
                If pts.Count >= 2 AndAlso isSpanVisible Then
                    G.DrawLines(meshPen, pts.ToArray())
                End If
            Next
        Next
    End Sub

    Private Function WorldToScreen(wx As Double, wy As Double, wz As Double, proj As String,
                                    W As Integer, H As Integer,
                                    hMin As Double, hMax As Double,
                                    vMin As Double, vMax As Double,
                                    hOff As Double, vOff As Double) As PointF
        Dim sh As Double, sv As Double
        Select Case proj
            Case "XZ"
                sh = wx * W / (hMax - hMin) + W / 2 + hOff
                sv = -wz * H / (vMax - vMin) + H / 2 + vOff
            Case "YZ"
                sh = -wy * W / (hMax - hMin) + W / 2 + hOff
                sv = -wz * H / (vMax - vMin) + H / 2 + vOff
            Case "3D"
                Dim pNode As New Node(wx, wy, wz, "", False, 0, Node.NodeType.Geometry)
                Dim projNode = pNode.RotateY(viewAlpha).RotateX(viewBeta).RotateZ(viewGamma).Project(W, H, viewDist, viewFOV)
                sh = projNode.X
                sv = projNode.Y
            Case Else ' XY
                sh = wx * W / (hMax - hMin) + W / 2 + hOff
                sv = -wy * H / (vMax - vMin) + H / 2 + vOff
        End Select
        Return New PointF(CSng(sh), CSng(sv))
    End Function


    Public Function anyHovered() As Boolean
        'Dim result As Boolean = False

        For Each p As Node In points
            If p.Hovered Then Return True
        Next

        Return False
    End Function

    Private Sub p1_MouseMove(sender As Object, e As MouseEventArgs) Handles pxy.MouseMove
        Dim e1 As Double = (xmax - xmin) / pxy.Width * (e.X - (pxy.Width / 2) - xoffset)
        Dim e2 As Double = (ymax - ymin) / pxy.Height * (e.Y - (pxy.Height / 2) - yoffset)
        curX = e1
        curY = -e2
        lblCursor.Text = "Cursor: [X: " + String.Format("{0,5:###.0}", Math.Round(e1, 1)) + ", Y: " + String.Format("{0,5:###.0}", Math.Round(-e2, 1)) + "]"

        If isDragMode Then
            Dim hitEps = 7.0 * (xmax - xmin) / pxy.Width
            Dim nearDraggable = points.Any(Function(n) n.IsDraggable AndAlso Math.Abs(n.X - e1) < hitEps AndAlso Math.Abs(n.Y - (-e2)) < hitEps)
            pxy.Cursor = If(nearDraggable OrElse isDragging, Cursors.SizeAll, Cursors.Hand)
            If isDragging AndAlso draggingNode IsNot Nothing AndAlso e.Button = MouseButtons.Left Then
                draggingNode.X = CSng(e1)
                draggingNode.Y = CSng(-e2)
                draggingNode.Point = New Point3D(e1, -e2, draggingNode.Z)
                drawAxes()
            End If
            Return
        End If

        ' --- Normal hover / pan behaviour ---
        ' Hit tolerance is scaled to the current view bounds (constant ~7px on
        ' screen) rather than a fixed world-unit eps, so nodes stay easy to
        ' select regardless of how zoomed out a large-dimension aircraft is.
        Dim hitEpsX = 7.0 * (xmax - xmin) / pxy.Width
        Dim hitEpsY = 7.0 * (ymax - ymin) / pxy.Height
        Dim prevHovered As Node = points.FirstOrDefault(Function(p) p.Hovered)
        Dim newHovered As Node = Nothing
        For Each p As Node In points
            If (p.X > e1 - hitEpsX) And (p.X < e1 + hitEpsX) And (p.Y > -e2 - hitEpsY) And (p.Y < -e2 + hitEpsY) Then
                If (p.type = Node.NodeType.Geometry And showSection And showHover) Then
                    newHovered = p : Exit For
                End If
                If (p.type = Node.NodeType.Mass And showMass And showHover) Then
                    newHovered = p
                End If
            End If
        Next

        Dim hoverChanged = (newHovered IsNot prevHovered)
        For Each p As Node In points
            p.Hovered = (p Is newHovered)
        Next
        If newHovered IsNot Nothing Then
            isHovered = True
            tc1.SelectedIndex = If(newHovered.type = Node.NodeType.Mass, 1, 0)
            selectText(newHovered.lineNumber)
        Else
            isHovered = False
        End If

        Dim panning = e.Button = MouseButtons.Left AndAlso Not isHovered
        If panning Then
            xoffset = e.X - xdown
            yoffset = e.Y - ydown
        End If

        If hoverChanged OrElse panning OrElse showHover Then drawAxes()
    End Sub

    Private Sub selectText(lineNumber As Integer)
        With txt3
            '.SelectAll()
            Dim line = .GetLine(lineNumber)
            .SelectionStart = txt3.Text.IndexOf(.GetLineText(lineNumber))
            .SelectionLength = .GetLineLength(lineNumber)
            .Invalidate()
            .DoCaretVisible()
            'SendKeys.Send("{HOME}+{END}")
        End With
        'Me.Text = lineNumber.ToString + "," + sstart.ToString + "," + send.ToString + " | " + txt3.GetLineText(lineNumber)

    End Sub

    Private Sub frmGeometry_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        drawAxes()
    End Sub

    Private Sub p1_MouseDown(sender As Object, e As MouseEventArgs) Handles pxy.MouseDown
        If e.Button = MouseButtons.Left Then
            mouseDownScreenPos = e.Location
            If isDragMode Then
                Dim e1 = (xmax - xmin) / pxy.Width * (e.X - (pxy.Width / 2) - xoffset)
                Dim e2 = (ymax - ymin) / pxy.Height * (e.Y - (pxy.Height / 2) - yoffset)
                Dim hitEps = 7.0 * (xmax - xmin) / pxy.Width
                For Each n As Node In points
                    If n.IsDraggable AndAlso Math.Abs(n.X - e1) < hitEps AndAlso Math.Abs(n.Y - (-e2)) < hitEps Then
                        draggingNode = n
                        isDragging = True
                        dragStartX = n.X
                        dragStartY = n.Y
                        dragStartZ = n.Z
                        pxy.Cursor = Cursors.SizeAll
                        Exit For
                    End If
                Next
                Return
            End If
            pxy.Cursor = Cursors.SizeAll
            xdown = e.X - xoffset
            ydown = e.Y - yoffset
            isHovered = anyHovered()
        End If
    End Sub

    Private Sub p1_MouseWheel(sender As Object, e As MouseEventArgs) Handles pxy.MouseWheel
        'Me.Text = e.Delta
        If e.Delta < 0 Then
            gridnumber += 1
            drawAxes()
        Else
            If gridnumber > 1 Then
                gridnumber -= 1
                drawAxes()
            End If
        End If
    End Sub

    Private Sub p1_MouseUp(sender As Object, e As MouseEventArgs) Handles pxy.MouseUp
        If isDragMode AndAlso isDragging AndAlso draggingNode IsNot Nothing Then
            isDragging = False
            Dim e1 = (xmax - xmin) / pxy.Width * (e.X - (pxy.Width / 2) - xoffset)
            Dim e2 = (ymax - ymin) / pxy.Height * (e.Y - (pxy.Height / 2) - yoffset)
            CommitNodeDrag(draggingNode, e1, -e2, dragStartZ)  ' Z unchanged in XY view
            draggingNode = Nothing
            pxy.Cursor = Cursors.Hand
            Return
        End If
        pxy.Cursor = Cursors.Default
        If e.Button = MouseButtons.Left Then drawAxes()
        HandlePotentialNodeClick(e)
    End Sub

    Private Sub btnReset_Click(sender As Object, e As EventArgs)
        points.Clear()
        drawAxes()
    End Sub

    ' This menu is shown regardless of which of the Geometry/Mass/Run tabs is
    ' active, but txt3 (the shared editor) only ever holds the currently
    ' selected tab's content - without this guard, clicking "AVL Template"
    ' while on the Mass or Run tab would silently blow away that tab's content
    ' with the AVL template text instead.
    Private Sub AVLTemplateToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AVLTemplateToolStripMenuItem.Click
        If tc1.SelectedTab IsNot Nothing AndAlso tc1.SelectedTab.Name <> "Geometry" Then
            MessageBox.Show("Switch to the Geometry tab first - this would replace the " & tc1.SelectedTab.Name & " tab's content otherwise.",
                             "Wrong Tab", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim f = Path.Combine(rootPath, "template_avl.txt")
        Dim val = File.ReadAllText(f)

        ' Replace entire text via selection to preserve undo history
        txt3.Selection.Start = New Place(0, 0)
        txt3.Selection.End = New Place(txt3.Lines(txt3.LinesCount - 1).Length, txt3.LinesCount - 1)
        txt3.InsertText(val)

        txt3.SelectionStart = txt3.Text.Length
        txt3.DoCaretVisible()
        FormatActiveText()
    End Sub

    Private Sub SurfaceToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SurfaceToolStripMenuItem.Click

        Dim before = CountBlocks()
        Dim after = CountBlocks(False)

        ' 1. Calculate the current state based on what is BEFORE the cursor
        ' If (Starts - Ends) > 0, we are currently inside that block.
        Dim insideGeometry As Boolean = (before("!begingeometry") - before("!endgeometry")) > 0
        Dim insideSurface As Boolean = (before("!beginsurface") - before("!endsurface")) > 0

        ' 2. Check existence of the main wrapper tags
        Dim hasGeometryTags As Boolean = (before("!begingeometry") + after("!begingeometry") > 0) AndAlso
                                 (before("!endgeometry") + after("!endgeometry") > 0)

        ' --- VALIDATION CHECKS ---

        ' CHECK 1: Global Structure
        If Not hasGeometryTags Then
            MessageBox.Show("Your AVL file is missing !begingeometry or !endgeometry tags." & Environment.NewLine & Environment.NewLine &
                    "Please add an AVL template first.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' CHECK 2: Must be inside Geometry
        If Not insideGeometry Then
            MessageBox.Show("You must insert a Surface block inside the Geometry tags." & Environment.NewLine & Environment.NewLine &
                    "Move your cursor between !begingeometry and !endgeometry.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' CHECK 3: Cannot be inside another Surface (No Nested Surfaces)
        If insideSurface Then
            MessageBox.Show("You cannot add a Surface block inside another Surface block." & Environment.NewLine & Environment.NewLine &
                    "Move your cursor outside of existing !beginsurface and !endsurface tags.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        ' --- INSERTION LOGIC ---

        Try
            ' Construct safe path
            Dim templatePath As String = System.IO.Path.Combine(rootPath, "template_surface.txt")

            If System.IO.File.Exists(templatePath) Then
                Dim contentToInsert As String = Environment.NewLine & System.IO.File.ReadAllText(templatePath)
                txt3.InsertText(contentToInsert)
                txt3.Focus()
            Else
                MessageBox.Show("Template file not found: " & templatePath, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Catch ex As Exception
            MessageBox.Show("Error inserting surface template: " & ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

    End Sub

    Private Sub SectionToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SectionToolStripMenuItem.Click

        Dim before = CountBlocks()
        Dim after = CountBlocks(False)

        ' 1. Calculate the current state based on what is BEFORE the cursor
        ' If (Starts - Ends) > 0, we are currently inside that block.
        Dim insideGeometry As Boolean = (before("!begingeometry") - before("!endgeometry")) > 0
        Dim insideSurface As Boolean = (before("!beginsurface") - before("!endsurface")) > 0
        Dim insideSection As Boolean = (before("!beginsection") - before("!endsection")) > 0
        Dim insideControl As Boolean = (before("!begincontrol") - before("!endcontrol")) > 0

        ' 2. Check existence of the main wrapper tags
        Dim hasGeometryTags As Boolean = (before("!begingeometry") + after("!begingeometry") > 0) AndAlso (before("!endgeometry") + after("!endgeometry") > 0)
        Dim hasSurfaceTags As Boolean = (before("!beginsurface") + after("!beginsurface") > 0) AndAlso (before("!endsurface") + after("!endsurface") > 0)

        ' --- VALIDATION CHECKS ---

        ' CHECK 1: Global Structure (Geometry)
        If Not hasGeometryTags Then
            MessageBox.Show("Your AVL file is missing !begingeometry or !endgeometry tags." & Environment.NewLine & Environment.NewLine &
                    "Please add an AVL template first.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' CHECK 2: Global Structure (Surface)
        If Not hasSurfaceTags Then
            MessageBox.Show("Your AVL file is missing !beginsurface or !endsurface tags." & Environment.NewLine & Environment.NewLine &
                    "Please add a Surface template first.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' CHECK 3: Must be inside a Surface
        If Not insideSurface Then
            MessageBox.Show("You must insert a Section block inside a Surface block." & Environment.NewLine & Environment.NewLine &
                    "Move your cursor between !beginsurface and !endsurface.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' CHECK 4: Cannot be inside another Section (No Nested Sections)
        If insideSection Then
            MessageBox.Show("You cannot add a Section block inside another Section block." & Environment.NewLine & Environment.NewLine &
                    "Move your cursor outside of the existing !beginsection and !endsection tags.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        ' CHECK 5: Cannot be inside a Control (Section cannot be inside Control)
        If insideControl Then
            MessageBox.Show("You cannot add a Section block inside a Control block." & Environment.NewLine & Environment.NewLine &
                    "Move your cursor outside of the existing !begincontrol and !endcontrol tags.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If


        ' --- INSERTION LOGIC ---

        Try
            ' Construct safe path
            Dim templatePath As String = System.IO.Path.Combine(rootPath, "template_section.txt")

            If System.IO.File.Exists(templatePath) Then
                Dim contentToInsert As String = Environment.NewLine & System.IO.File.ReadAllText(templatePath)
                txt3.InsertText(contentToInsert)
                txt3.Focus()
            Else
                MessageBox.Show("Template file not found: " & templatePath, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Catch ex As Exception
            MessageBox.Show("Error inserting section template: " & ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

    End Sub

    Private Sub ControlToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ControlToolStripMenuItem.Click

        Dim before = CountBlocks()
        Dim after = CountBlocks(False)

        ' 1. Calculate the current "depth" based on what is BEFORE the cursor.
        ' If Depth is > 0, we are currently inside that block.
        Dim insideGeometry As Boolean = (before("!begingeometry") - before("!endgeometry")) > 0
        Dim insideSurface As Boolean = (before("!beginsurface") - before("!endsurface")) > 0
        Dim insideSection As Boolean = (before("!beginsection") - before("!endsection")) > 0
        Dim insideControl As Boolean = (before("!begincontrol") - before("!endcontrol")) > 0

        ' 2. Check existence (File must actually contain the tags)
        Dim hasGeometryTags As Boolean = (before("!begingeometry") + after("!begingeometry") > 0) AndAlso (before("!endgeometry") + after("!endgeometry") > 0)
        Dim hasSurfaceTags As Boolean = (before("!beginsurface") + after("!beginsurface") > 0) AndAlso (before("!endsurface") + after("!endsurface") > 0)
        Dim hasSectionTags As Boolean = (before("!beginsection") + after("!beginsection") > 0) AndAlso (before("!endsection") + after("!endsection") > 0)

        ' --- VALIDATION CHECKS ---

        ' CHECK 1: Geometry Context
        If Not hasGeometryTags Then
            MessageBox.Show("Your AVL file is missing !begingeometry or !endgeometry tags." & Environment.NewLine & Environment.NewLine &
                    "Please add an AVL template first.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If Not insideGeometry Then
            MessageBox.Show("You must insert this component inside the Geometry block." & Environment.NewLine & Environment.NewLine &
                    "Move your cursor between !begingeometry and !endgeometry.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' CHECK 2: Surface Context
        If Not hasSurfaceTags Then
            MessageBox.Show("Your AVL file is missing !beginsurface or !endsurface tags.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If Not insideSurface Then
            MessageBox.Show("You must insert this component inside a Surface block." & Environment.NewLine & Environment.NewLine &
                    "Move your cursor between !beginsurface and !endsurface.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' CHECK 3: Section Context (Assuming Control must be inside a Section)
        ' If controls CAN exist outside sections in your specific usage, remove this block.
        If Not insideSection Then
            MessageBox.Show("You must insert the Control block inside a Section." & Environment.NewLine & Environment.NewLine &
                    "Move your cursor between !beginsection and !endsection.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' CHECK 4: Nested Controls (Prevent Control inside Control)
        If insideControl Then
            MessageBox.Show("You cannot place a Control block inside another Control block." & Environment.NewLine & Environment.NewLine &
                    "Move your cursor outside of existing !begincontrol and !endcontrol tags.",
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        ' --- INSERTION LOGIC ---

        Try
            ' Use Path.Combine for safety
            Dim templatePath As String = System.IO.Path.Combine(rootPath, "template_control.txt")

            If System.IO.File.Exists(templatePath) Then
                Dim contentToInsert As String = Environment.NewLine & System.IO.File.ReadAllText(templatePath)
                txt3.InsertText(contentToInsert)
                txt3.Focus()
            Else
                MessageBox.Show("Template file not found: " & templatePath, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Catch ex As Exception
            MessageBox.Show("Error inserting template: " & ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

    End Sub

    Private Sub SeparatorToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SeparatorToolStripMenuItem.Click
        Dim val = "#==========================================="
        Dim sel = txt3.SelectionStart
        txt3.Text = txt3.Text.Insert(sel, Environment.NewLine + val)
        txt3.SelectionStart = sel + (Environment.NewLine + val).Length
        txt3.DoCaretVisible()


    End Sub

    Private Sub btnDragMode_Click(sender As Object, e As EventArgs) Handles btnDragMode.Click
        isDragMode = Not isDragMode
        ' Cancel any in-progress drag when the mode is toggled off
        If Not isDragMode Then
            isDragging = False
            draggingNode = Nothing
        End If

        btnDragMode.Text = If(isDragMode, "Drag Nodes: On", "Drag Nodes: Off")
        btnDragMode.BackColor = If(isDragMode, Color.FromArgb(192, 255, 192), Color.FromArgb(220, 220, 220))

        Dim c As Cursor = If(isDragMode, Cursors.Hand, Cursors.Default)
        pxy.Cursor = c
        pxz.Cursor = c
        pyz.Cursor = c
    End Sub

    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        ctxAddMenu.Show(btnAdd, New Point(0, btnAdd.Height))
    End Sub

    Private Sub btnPrettify_Click(sender As Object, e As EventArgs) Handles btnPrettify.Click
        If txt3 Is Nothing OrElse txt3.LinesCount = 0 Then Return
        FormatActiveText()
        UpdateUndoRedoState()
    End Sub

    Private Sub btnUndo_Click(sender As Object, e As EventArgs) Handles btnUndo.Click
        If txt3 IsNot Nothing AndAlso txt3.UndoEnabled Then
            txt3.Undo()
        End If
    End Sub

    Private Sub btnRedo_Click(sender As Object, e As EventArgs) Handles btnRedo.Click
        If txt3 IsNot Nothing AndAlso txt3.RedoEnabled Then
            txt3.Redo()
        End If
    End Sub

    Private Sub UpdateUndoRedoState()
        If txt3 Is Nothing Then Return
        btnUndo.Enabled = txt3.UndoEnabled
        btnRedo.Enabled = txt3.RedoEnabled
    End Sub

    ' Writes the new world coordinates for a dragged node back to the AVL or mass file,
    ' then refreshes the editor text (if the matching tab is active) and redraws.
    '
    ' Axis semantics per view:
    '   pxy  → newX/newY used; newZ = dragStartZ (unchanged)
    '   pxz  → newX/newZ used; newY = dragStartY (unchanged)
    '   pyz  → newY/newZ used; newX = dragStartX (unchanged)
    '
    ' NodeSubType controls what column(s) get updated:
    '   LeadingEdge   → Xle, Yle, Zle (cols 0-2 of the section data line)
    '   TrailingEdge  → Chord = newX - Xle (col 3; Y/Z ignored since chord is axial)
    '   ControlHinge  → Xhinge = (newX - parentXle) / parentChord (col 2 of the control line)
    Private Sub CommitNodeDrag(node As Node, newX As Double, newY As Double, newZ As Double)
        If String.IsNullOrEmpty(projectName) Then Return
        If node.type = Node.NodeType.Geometry Then
            Dim f = Path.Combine(Application.StartupPath, $"{projectName}.avl")
            If Not File.Exists(f) Then Return

            Dim raw = TrimAll(File.ReadAllText(f))
            Dim lines() As String = raw.Replace(vbLf, "").Split(CChar(vbCrLf))
            Dim changedLine As Integer = -1

            Select Case node.SubType

                Case Node.NodeSubType.LeadingEdge
                    Dim ln = node.lineNumber
                    If ln < 0 OrElse ln >= lines.Length Then Return
                    Dim parts = lines(ln).Split(" "c)
                    If parts.Length < 3 Then Return
                    parts(0) = newX.ToString("G6")
                    parts(1) = newY.ToString("G6")
                    parts(2) = newZ.ToString("G6")
                    Dim originalLineText = If(ln < txt3.LinesCount, txt3.Lines(ln), "")
                    Dim leadingWS = GetLeadingWhitespace(originalLineText)
                    lines(ln) = leadingWS & FormatLineText(String.Join(" ", parts)).TrimStart()
                    changedLine = ln

                Case Node.NodeSubType.TrailingEdge
                    ' Only X is meaningful — chord is always axial so Y/Z can't shift independently
                    Dim ln = node.lineNumber
                    If ln < 0 OrElse ln >= lines.Length Then Return
                    Dim parts = lines(ln).Split(" "c)
                    If parts.Length < 4 Then Return
                    Dim xle = CDbl(parts(0))
                    Dim newChord = newX - xle
                    If newChord < 0.001 Then Return   ' reject zero/negative chord
                    parts(3) = newChord.ToString("G6")
                    Dim originalLineText = If(ln < txt3.LinesCount, txt3.Lines(ln), "")
                    Dim leadingWS = GetLeadingWhitespace(originalLineText)
                    lines(ln) = leadingWS & FormatLineText(String.Join(" ", parts)).TrimStart()
                    changedLine = ln

                Case Node.NodeSubType.ControlHinge
                    ' Only X is meaningful — Xhinge is a chord fraction
                    If node.parentChord <= 0 Then Return
                    Dim fraction = (newX - node.parentXle) / node.parentChord
                    fraction = Math.Max(0.001, Math.Min(0.999, fraction))
                    ' Preserve the original sign convention (negative Xhinge = measured from TE)
                    Dim newXhinge As Double = If(node.originalXhinge >= 0, fraction, -fraction)
                    Dim ctrlLn = node.controlLineNumber
                    If ctrlLn < 0 OrElse ctrlLn >= lines.Length Then Return
                    Dim ctrlParts = lines(ctrlLn).Split(" "c)
                    If ctrlParts.Length < 3 Then Return
                    ctrlParts(2) = newXhinge.ToString("G6")
                    Dim originalLineText = If(ctrlLn < txt3.LinesCount, txt3.Lines(ctrlLn), "")
                    Dim leadingWS = GetLeadingWhitespace(originalLineText)
                    lines(ctrlLn) = leadingWS & FormatLineText(String.Join(" ", ctrlParts)).TrimStart()
                    changedLine = ctrlLn

                Case Else
                    Return
            End Select

            Dim newContent As String = String.Join(vbCrLf, lines)
            File.WriteAllText(f, newContent)

            If tc1.SelectedTab.Name = "Geometry" AndAlso changedLine <> -1 Then
                updating = True
                ReplaceEditorLine(changedLine, lines(changedLine))
                updating = False
            End If

        ElseIf node.type = Node.NodeType.Mass Then
            Dim f = Path.Combine(Application.StartupPath, $"{projectName}.mass")
            If Not File.Exists(f) Then Return

            Dim lines() As String = File.ReadAllLines(f)
            Dim ln = node.lineNumber
            If ln < 0 OrElse ln >= lines.Length Then Return

            Dim parts = lines(ln).Split(" "c)
            If parts.Length < 4 Then Return

            parts(1) = newX.ToString("G6")
            parts(2) = newY.ToString("G6")
            parts(3) = newZ.ToString("G6")
            Dim originalLineText = If(ln < txt3.LinesCount, txt3.Lines(ln), "")
            Dim leadingWS = GetLeadingWhitespace(originalLineText)
            lines(ln) = leadingWS & FormatLineText(String.Join(" ", parts)).TrimStart()

            Dim newContent As String = String.Join(vbCrLf, lines)
            File.WriteAllText(f, newContent)

            If tc1.SelectedTab.Name = "Mass" Then
                updating = True
                ReplaceEditorLine(ln, lines(ln))
                updating = False
            End If
        End If

        findPoints()
        drawAxes()
    End Sub

    ' Distinguishes a genuine click (open the properties panel) from a pan/drag
    ' (which already has its own handling above and returns before reaching here).
    ' Only fires outside drag mode - in drag mode, repositioning is the click.
    Private Sub HandlePotentialNodeClick(e As MouseEventArgs)
        If isDragMode Then Return
        If e.Button <> MouseButtons.Left Then Return
        Dim dx = e.X - mouseDownScreenPos.X
        Dim dy = e.Y - mouseDownScreenPos.Y
        If (dx * dx + dy * dy) > 16 Then Return   ' moved more than ~4px -> treat as a pan, not a click

        ' Mirrored (YDUPLICATE) nodes are included here too - they share the
        ' same underlying line as their primary, so clicking either side opens
        ' the same properties. Mass nodes are always independently clickable
        ' (mass points aren't mirrored/subtyped the way geometry nodes are).
        Dim hovered As Node = Nothing
        For Each p As Node In points
            If p.Hovered AndAlso
               (p.type = Node.NodeType.Mass OrElse
                (p.type = Node.NodeType.Geometry AndAlso
                 (p.SubType = Node.NodeSubType.LeadingEdge OrElse p.SubType = Node.NodeSubType.TrailingEdge OrElse p.SubType = Node.NodeSubType.ControlHinge))) Then
                hovered = p
                Exit For
            End If
        Next
        If hovered IsNot Nothing Then ShowNodeProperties(hovered)
    End Sub

    Private Sub InitializePropertiesPanel()
        If pnlProperties IsNot Nothing Then Return

        pnlProperties = New System.Windows.Forms.Panel()
        pnlProperties.Dock = DockStyle.Right
        pnlProperties.Width = 270
        pnlProperties.BackColor = Color.WhiteSmoke
        pnlProperties.BorderStyle = BorderStyle.FixedSingle
        pnlProperties.Visible = False
        Me.Controls.Add(pnlProperties)

        lblPropHeader = New System.Windows.Forms.Label() With {
            .Text = "Properties",
            .AutoSize = False,
            .Location = New Point(10, 10),
            .Size = New Size(200, 20),
            .Font = New Font(Me.Font, FontStyle.Bold)
        }
        pnlProperties.Controls.Add(lblPropHeader)

        btnPropClose.Text = "x"
        btnPropClose.Location = New Point(236, 6)
        btnPropClose.Size = New Size(24, 24)
        btnPropClose.FlatStyle = FlatStyle.Flat
        btnPropClose.BackColor = Color.White
        btnPropClose.Cursor = Cursors.Hand
        AddHandler btnPropClose.Click, Sub(s, ev) HidePropertiesPanel()
        pnlProperties.Controls.Add(btnPropClose)

        ' Section fields (Leading/Trailing edge node clicked)
        pnlSectionFields = New System.Windows.Forms.Panel() With {.Location = New Point(0, 40), .Size = New Size(268, 180)}
        AddPropRow(pnlSectionFields, 4, "Xle:", txtPropXle, "Airfoil's leading edge location, X. (AVL SECTION keyword)")
        AddPropRow(pnlSectionFields, 32, "Yle:", txtPropYle, "Airfoil's leading edge location, Y. (AVL SECTION keyword)")
        AddPropRow(pnlSectionFields, 60, "Zle:", txtPropZle, "Airfoil's leading edge location, Z. (AVL SECTION keyword)")
        AddPropRow(pnlSectionFields, 88, "Chord:", txtPropChord, "The airfoil's chord. Trailing edge is located at (Xle+Chord, Yle, Zle). (AVL SECTION keyword)")
        AddPropRow(pnlSectionFields, 116, "Ainc (twist):", txtPropAinc, "Incidence angle (deg), a rotation about the surface's spanwise axis projected onto the Y-Z plane. Only affects the flow-tangency boundary condition - does not rotate the drawn geometry. (AVL SECTION keyword)")
        lblPropAirfoilKind = New System.Windows.Forms.Label() With {.Text = "Airfoil:", .AutoSize = True, .Location = New Point(10, 147)}
        txtPropAirfoil.Location = New Point(120, 144)
        txtPropAirfoil.Width = 96
        Dim helpAirfoil As New System.Windows.Forms.Label() With {
            .Text = "?", .AutoSize = False, .Size = New Size(18, 18), .Location = New Point(222, 146),
            .TextAlign = ContentAlignment.MiddleCenter, .BackColor = Color.Gainsboro, .Cursor = Cursors.Help,
            .Font = New Font(Me.Font.FontFamily, 7.5F, FontStyle.Bold)
        }
        propHelpTip.SetToolTip(helpAirfoil, "Camber-line shape for this section: a NACA 4-digit code (NACA keyword) or an airfoil coordinate/.dat filename (AIRFOIL/AFILE keyword). Chord and incidence are linearly interpolated between defining sections.")
        pnlSectionFields.Controls.Add(lblPropAirfoilKind)
        pnlSectionFields.Controls.Add(txtPropAirfoil)
        pnlSectionFields.Controls.Add(helpAirfoil)
        pnlProperties.Controls.Add(pnlSectionFields)

        ' Control fields (hinge node clicked)
        pnlControlFields = New System.Windows.Forms.Panel() With {.Location = New Point(0, 40), .Size = New Size(268, 210), .Visible = False}
        AddPropRow(pnlControlFields, 4, "Cname:", txtPropCname, "Name of the control variable. Reuse the same name across multiple sections/surfaces to link their deflections together (e.g. flap-to-elevator mixing). (AVL CONTROL keyword)")
        AddPropRow(pnlControlFields, 32, "Cgain:", txtPropCgain, "Control deflection gain: degrees of surface deflection per unit of the control variable.")
        AddPropRow(pnlControlFields, 60, "Xhinge:", txtPropXhinge, "x/c location of the hinge line. Positive: control surface spans Xhinge..1 (trailing-edge surface). Negative: spans 0..-Xhinge (leading-edge surface).")
        AddPropRow(pnlControlFields, 88, "HingeVec X:", txtPropHx, "X component of the vector giving the hinge axis the surface rotates about. Positive deflection is a positive (right-hand rule) rotation about this vector. Setting X,Y,Z all to 0 aligns the hinge vector along the hinge line itself.")
        AddPropRow(pnlControlFields, 116, "HingeVec Y:", txtPropHy, "Y component of the hinge-axis vector. See HingeVec X for details.")
        AddPropRow(pnlControlFields, 144, "HingeVec Z:", txtPropHz, "Z component of the hinge-axis vector. See HingeVec X for details.")
        AddPropRow(pnlControlFields, 172, "SgnDup:", txtPropSgnDup, "Sign of the deflection on the mirrored (YDUPLICATE) surface. Symmetric controls (e.g. elevator) use +1; anti-symmetric ones (e.g. aileron) use -1. A magnitude other than 1 also scales the mirrored deflection.")
        pnlProperties.Controls.Add(pnlControlFields)

        ' Mass fields (mass point clicked)
        pnlMassFields = New System.Windows.Forms.Panel() With {.Location = New Point(0, 40), .Size = New Size(268, 210), .Visible = False}
        AddPropRow(pnlMassFields, 4, "Mass:", txtPropMassVal, "Mass of this item.")
        AddPropRow(pnlMassFields, 32, "X:", txtPropMassX, "Location of this item's own center of gravity, X. Must use the same coordinate system (origin, orientation, units) as the .avl geometry file.")
        AddPropRow(pnlMassFields, 60, "Y:", txtPropMassY, "Location of this item's own center of gravity, Y. Must use the same coordinate system as the .avl geometry file.")
        AddPropRow(pnlMassFields, 88, "Z:", txtPropMassZ, "Location of this item's own center of gravity, Z. Must use the same coordinate system as the .avl geometry file.")
        AddPropRow(pnlMassFields, 116, "Ixx:", txtPropIxx, "Moment of inertia about the item's own CG: Ixx = integral of (y²+z²) dm.")
        AddPropRow(pnlMassFields, 144, "Iyy:", txtPropIyy, "Moment of inertia about the item's own CG: Iyy = integral of (x²+z²) dm.")
        AddPropRow(pnlMassFields, 172, "Izz:", txtPropIzz, "Moment of inertia about the item's own CG: Izz = integral of (x²+y²) dm.")
        pnlProperties.Controls.Add(pnlMassFields)

        btnPropApply.Text = "Apply"
        btnPropApply.Location = New Point(10, 258)
        btnPropApply.Size = New Size(100, 28)
        btnPropApply.FlatStyle = FlatStyle.Flat
        btnPropApply.BackColor = Color.White
        btnPropApply.Cursor = Cursors.Hand
        AddHandler btnPropApply.Click, AddressOf btnPropApply_Click
        pnlProperties.Controls.Add(btnPropApply)

        Dim lblPropHint As New System.Windows.Forms.Label() With {
            .Text = "Click a section, control, or mass node in any view to edit it here.",
            .AutoSize = False,
            .Size = New Size(250, 45),
            .Location = New Point(10, 300),
            .ForeColor = Color.Gray
        }
        pnlProperties.Controls.Add(lblPropHint)
    End Sub

    ' The trailing "?" is a small help indicator - hover it for a description of
    ' the field sourced from AVL's own documentation (avl_doc.txt).
    Private Sub AddPropRow(parent As System.Windows.Forms.Panel, y As Integer, labelText As String, tb As System.Windows.Forms.TextBox, helpText As String)
        Dim lbl As New System.Windows.Forms.Label() With {.Text = labelText, .AutoSize = True, .Location = New Point(10, y + 3)}
        tb.Location = New Point(120, y)
        tb.Width = 96
        Dim helpLbl As New System.Windows.Forms.Label() With {
            .Text = "?",
            .AutoSize = False,
            .Size = New Size(18, 18),
            .Location = New Point(222, y + 2),
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = Color.Gainsboro,
            .Cursor = Cursors.Help,
            .Font = New Font(Me.Font.FontFamily, 7.5F, FontStyle.Bold)
        }
        propHelpTip.SetToolTip(helpLbl, helpText)
        parent.Controls.Add(lbl)
        parent.Controls.Add(tb)
        parent.Controls.Add(helpLbl)
    End Sub

    Private Sub HidePropertiesPanel()
        pnlProperties.Visible = False
        _selectedNode = Nothing
    End Sub

    ' Scans forward from a section's data line for its NACA/AIRFOIL block.
    ' Airfoil identity isn't captured by the Section struct/findPoints parser
    ' at all, so this reads directly off the raw lines instead.
    Private Function FindAirfoilForSection(lines() As String, sectionLineNumber As Integer) As Tuple(Of String, String, Integer)
        Dim limit = Math.Min(lines.Length - 1, sectionLineNumber + 15)
        For i = sectionLineNumber + 1 To limit - 1
            Dim t = lines(i).Trim().ToLowerInvariant()
            If t = "naca" Then Return Tuple.Create("NACA", lines(i + 1).Trim(), i + 1)
            If t = "airfoil" Then Return Tuple.Create("AIRFOIL", lines(i + 1).Trim(), i + 1)
            If t = "section" OrElse t = "surface" OrElse t = "control" OrElse t.StartsWith("!end") Then Exit For
        Next
        Return Nothing
    End Function

    Private Sub ShowNodeProperties(node As Node)
        If String.IsNullOrEmpty(projectName) Then Return

        If node.type = Node.NodeType.Mass Then
            Dim mf = Path.Combine(Application.StartupPath, $"{projectName}.mass")
            If Not File.Exists(mf) Then Return
            Dim mlines() As String = File.ReadAllLines(mf)
            Dim mln = node.lineNumber
            If mln < 0 OrElse mln >= mlines.Length Then Return

            Dim mnumeric As New List(Of String)
            For Each v In mlines(mln).Split(CChar(" "))
                If IsNumeric(v) Then mnumeric.Add(v)
            Next
            If mnumeric.Count = 0 Then Return

            _selectedNode = node
            Dim header = "Mass Point"
            Dim cIdx = mlines(mln).IndexOf("!"c)
            If cIdx >= 0 Then
                Dim c = mlines(mln).Substring(cIdx + 1).Trim()
                If c <> "" Then header &= " — " & c
            End If
            lblPropHeader.Text = header
            pnlSectionFields.Visible = False
            pnlControlFields.Visible = False
            pnlMassFields.Visible = True
            pnlProperties.Visible = True

            txtPropMassVal.Text = If(mnumeric.Count > 0, mnumeric(0), "0")
            txtPropMassX.Text = If(mnumeric.Count > 1, mnumeric(1), "0")
            txtPropMassY.Text = If(mnumeric.Count > 2, mnumeric(2), "0")
            txtPropMassZ.Text = If(mnumeric.Count > 3, mnumeric(3), "0")
            txtPropIxx.Text = If(mnumeric.Count > 4, mnumeric(4), "0")
            txtPropIyy.Text = If(mnumeric.Count > 5, mnumeric(5), "0")
            txtPropIzz.Text = If(mnumeric.Count > 6, mnumeric(6), "0")
            Return
        End If

        Dim f = Path.Combine(Application.StartupPath, $"{projectName}.avl")
        If Not File.Exists(f) Then Return
        Dim lines() As String = TrimAll(File.ReadAllText(f)).Replace(vbLf, "").Split(CChar(vbCrLf))

        If node.SubType = Node.NodeSubType.LeadingEdge OrElse node.SubType = Node.NodeSubType.TrailingEdge Then
            Dim ln = node.lineNumber
            If ln < 0 OrElse ln >= lines.Length Then Return

            Dim numeric As New List(Of String)
            For Each v In lines(ln).Split(CChar(" "))
                If IsNumeric(v) Then numeric.Add(v)
            Next

            _selectedNode = node
            Dim surfaceLabel = node.Surface
            If surfaceLabel.EndsWith("_dup") Then surfaceLabel = surfaceLabel.Substring(0, surfaceLabel.Length - 4)
            lblPropHeader.Text = "Section — " & surfaceLabel & If(node.IsDuplicate, " (mirrored)", "")
            pnlSectionFields.Visible = True
            pnlControlFields.Visible = False
            pnlMassFields.Visible = False
            pnlProperties.Visible = True

            txtPropXle.Text = If(numeric.Count > 0, numeric(0), "0")
            txtPropYle.Text = If(numeric.Count > 1, numeric(1), "0")
            txtPropZle.Text = If(numeric.Count > 2, numeric(2), "0")
            txtPropChord.Text = If(numeric.Count > 3, numeric(3), "0")
            txtPropAinc.Text = If(numeric.Count > 4, numeric(4), "0")

            Dim af = FindAirfoilForSection(lines, ln)
            If af IsNot Nothing Then
                lblPropAirfoilKind.Text = af.Item1 & ":"
                txtPropAirfoil.Text = af.Item2
                txtPropAirfoil.Enabled = True
            Else
                lblPropAirfoilKind.Text = "Airfoil:"
                txtPropAirfoil.Text = "(none found)"
                txtPropAirfoil.Enabled = False
            End If

        ElseIf node.SubType = Node.NodeSubType.ControlHinge Then
            Dim ln = node.controlLineNumber
            If ln < 0 OrElse ln >= lines.Length Then Return

            Dim tokens As New List(Of String)
            For Each v In lines(ln).Split(CChar(" "))
                If v.Trim().Length > 0 Then tokens.Add(v.Trim())
            Next
            If tokens.Count = 0 Then Return

            _selectedNode = node
            lblPropHeader.Text = "Control — " & tokens(0) & If(node.IsDuplicate, " (mirrored)", "")
            pnlSectionFields.Visible = False
            pnlControlFields.Visible = True
            pnlMassFields.Visible = False
            pnlProperties.Visible = True

            txtPropCname.Text = tokens(0)
            txtPropCgain.Text = If(tokens.Count > 1, tokens(1), "1.0")
            txtPropXhinge.Text = If(tokens.Count > 2, tokens(2), "0.5")
            txtPropHx.Text = If(tokens.Count > 3, tokens(3), "0")
            txtPropHy.Text = If(tokens.Count > 4, tokens(4), "0")
            txtPropHz.Text = If(tokens.Count > 5, tokens(5), "0")
            txtPropSgnDup.Text = If(tokens.Count > 6, tokens(6), "1.0")
        End If
    End Sub

    Private Sub btnPropApply_Click(sender As Object, e As EventArgs)
        If _selectedNode Is Nothing OrElse String.IsNullOrEmpty(projectName) Then Return
        Dim node = _selectedNode

        If node.type = Node.NodeType.Mass Then
            ApplyMassProperties(node)
            Return
        End If

        Dim f = Path.Combine(Application.StartupPath, $"{projectName}.avl")
        If Not File.Exists(f) Then Return

        Dim lines() As String = TrimAll(File.ReadAllText(f)).Replace(vbLf, "").Split(CChar(vbCrLf))
        Dim changedLines As New List(Of Integer)

        Try
            If node.SubType = Node.NodeSubType.LeadingEdge OrElse node.SubType = Node.NodeSubType.TrailingEdge Then
                Dim ln = node.lineNumber
                If ln < 0 OrElse ln >= lines.Length Then Return

                Dim xle = CDbl(txtPropXle.Text)
                Dim yle = CDbl(txtPropYle.Text)
                Dim zle = CDbl(txtPropZle.Text)
                Dim chord = CDbl(txtPropChord.Text)
                Dim ainc = CDbl(txtPropAinc.Text)
                If chord < 0.001 Then
                    MessageBox.Show("Chord must be positive.", "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If

                ' Preserve any existing Nspanwise/Sspace override columns (6th/7th) instead of dropping them
                Dim origNumeric As New List(Of String)
                For Each v In lines(ln).Split(CChar(" "))
                    If IsNumeric(v) Then origNumeric.Add(v)
                Next
                Dim nspanwise = If(origNumeric.Count > 5, origNumeric(5), "0")
                Dim sspace = If(origNumeric.Count > 6, origNumeric(6), "0")

                Dim newLine = String.Join(" ", New String() {xle.ToString("G6"), yle.ToString("G6"), zle.ToString("G6"), chord.ToString("G6"), ainc.ToString("G6"), nspanwise, sspace})
                Dim leadingWS = GetLeadingWhitespace(If(ln < txt3.LinesCount, txt3.Lines(ln), lines(ln)))
                lines(ln) = leadingWS & FormatLineText(newLine).TrimStart()
                changedLines.Add(ln)

                If txtPropAirfoil.Enabled Then
                    Dim af = FindAirfoilForSection(lines, ln)
                    If af IsNot Nothing Then
                        Dim afLn = af.Item3
                        Dim afLeadingWS = GetLeadingWhitespace(If(afLn < txt3.LinesCount, txt3.Lines(afLn), lines(afLn)))
                        lines(afLn) = afLeadingWS & txtPropAirfoil.Text.Trim()
                        changedLines.Add(afLn)
                    End If
                End If

            ElseIf node.SubType = Node.NodeSubType.ControlHinge Then
                Dim ln = node.controlLineNumber
                If ln < 0 OrElse ln >= lines.Length Then Return

                Dim cname = txtPropCname.Text.Trim()
                If cname = "" Then
                    MessageBox.Show("Control name cannot be empty.", "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
                Dim cgain = CDbl(txtPropCgain.Text)
                Dim xhinge = CDbl(txtPropXhinge.Text)
                Dim hx = CDbl(txtPropHx.Text)
                Dim hy = CDbl(txtPropHy.Text)
                Dim hz = CDbl(txtPropHz.Text)
                Dim sgndup = CDbl(txtPropSgnDup.Text)

                Dim newLine = String.Join(" ", New String() {cname, cgain.ToString("G6"), xhinge.ToString("G6"), hx.ToString("G6"), hy.ToString("G6"), hz.ToString("G6"), sgndup.ToString("G6")})
                Dim leadingWS = GetLeadingWhitespace(If(ln < txt3.LinesCount, txt3.Lines(ln), lines(ln)))
                lines(ln) = leadingWS & FormatLineText(newLine).TrimStart()
                changedLines.Add(ln)
            Else
                Return
            End If
        Catch ex As Exception
            MessageBox.Show("Please enter valid numeric values for all fields." & Environment.NewLine & ex.Message, "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End Try

        Dim newContent As String = String.Join(vbCrLf, lines)
        File.WriteAllText(f, newContent)

        If tc1.SelectedTab IsNot Nothing AndAlso tc1.SelectedTab.Name = "Geometry" Then
            updating = True
            For Each cl In changedLines
                ReplaceEditorLine(cl, lines(cl))
            Next
            updating = False
            FormatActiveText()
        End If

        findPoints()
        drawAxes()

        ' Re-select the refreshed node instance (same line/SubType/mirror-side) so
        ' the panel keeps reflecting what's now actually on disk.
        Dim reselect As Node = Nothing
        For Each p As Node In points
            If p.type = Node.NodeType.Geometry AndAlso p.IsDuplicate = node.IsDuplicate AndAlso p.SubType = node.SubType Then
                If node.SubType = Node.NodeSubType.ControlHinge Then
                    If p.controlLineNumber = node.controlLineNumber Then reselect = p : Exit For
                Else
                    If p.lineNumber = node.lineNumber Then reselect = p : Exit For
                End If
            End If
        Next
        If reselect IsNot Nothing Then ShowNodeProperties(reselect)
    End Sub

    Private Sub ApplyMassProperties(node As Node)
        Dim f = Path.Combine(Application.StartupPath, $"{projectName}.mass")
        If Not File.Exists(f) Then Return

        Dim lines() As String = File.ReadAllLines(f)
        Dim ln = node.lineNumber
        If ln < 0 OrElse ln >= lines.Length Then Return

        Try
            Dim massVal = CDbl(txtPropMassVal.Text)
            Dim mx = CDbl(txtPropMassX.Text)
            Dim my = CDbl(txtPropMassY.Text)
            Dim mz = CDbl(txtPropMassZ.Text)
            Dim ixx = CDbl(txtPropIxx.Text)
            Dim iyy = CDbl(txtPropIyy.Text)
            Dim izz = CDbl(txtPropIzz.Text)
            If massVal <= 0 Then
                MessageBox.Show("Mass must be positive.", "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Preserve any trailing "!comment" text (e.g. "!wing structure") instead of dropping it
            Dim origLine = lines(ln)
            Dim commentIdx = origLine.IndexOf("!"c)
            Dim comment = If(commentIdx >= 0, "   " & origLine.Substring(commentIdx), "")

            Dim newLine = String.Join(" ", New String() {massVal.ToString("G6"), mx.ToString("G6"), my.ToString("G6"), mz.ToString("G6"), ixx.ToString("G6"), iyy.ToString("G6"), izz.ToString("G6")})
            Dim leadingWS = GetLeadingWhitespace(If(ln < txt3.LinesCount, txt3.Lines(ln), origLine))
            lines(ln) = leadingWS & FormatLineText(newLine).TrimStart() & comment
        Catch ex As Exception
            MessageBox.Show("Please enter valid numeric values for all fields." & Environment.NewLine & ex.Message, "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End Try

        Dim newContent As String = String.Join(vbCrLf, lines)
        File.WriteAllText(f, newContent)

        If tc1.SelectedTab IsNot Nothing AndAlso tc1.SelectedTab.Name = "Mass" Then
            updating = True
            ReplaceEditorLine(ln, lines(ln))
            updating = False
            FormatActiveText()
        End If

        findPoints()
        drawAxes()

        Dim reselect As Node = Nothing
        For Each p As Node In points
            If p.type = Node.NodeType.Mass AndAlso p.lineNumber = node.lineNumber Then
                reselect = p
                Exit For
            End If
        Next
        If reselect IsNot Nothing Then ShowNodeProperties(reselect)
    End Sub

    ' ===================== Structure tree =====================

    Private Sub InitializeStructureTreePanel()
        If pnlStructureTree IsNot Nothing Then Return

        pnlStructureTree = New System.Windows.Forms.Panel()
        pnlStructureTree.Dock = DockStyle.Left
        pnlStructureTree.Width = 240
        pnlStructureTree.BackColor = Color.White
        pnlStructureTree.BorderStyle = BorderStyle.FixedSingle
        pnlStructureTree.Visible = False
        Me.Controls.Add(pnlStructureTree)

        ' A single 2-ROW TableLayoutPanel (toolbar row, tree row) instead of
        ' two Dock=Top/Dock=Fill SIBLINGS. Sibling docking depends on z-order
        ' resolution between the two controls, which is where the earlier
        ' overlap kept coming from; a table's row heights are computed
        ' directly with no ordering ambiguity, so this can't overlap.
        Dim outer As New System.Windows.Forms.TableLayoutPanel()
        outer.Dock = DockStyle.Fill
        outer.ColumnCount = 1
        outer.RowCount = 2
        outer.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(SizeType.Percent, 100.0F))
        outer.RowStyles.Add(New System.Windows.Forms.RowStyle(SizeType.Absolute, 76.0F))
        outer.RowStyles.Add(New System.Windows.Forms.RowStyle(SizeType.Percent, 100.0F))
        pnlStructureTree.Controls.Add(outer)

        ' 2x2 grid of Add/Delete buttons, filling row 0 of `outer`.
        Dim toolbar As New System.Windows.Forms.TableLayoutPanel()
        toolbar.Dock = DockStyle.Fill
        toolbar.BackColor = Color.WhiteSmoke
        toolbar.ColumnCount = 2
        toolbar.RowCount = 2
        toolbar.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(SizeType.Percent, 50.0F))
        toolbar.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(SizeType.Percent, 50.0F))
        toolbar.RowStyles.Add(New System.Windows.Forms.RowStyle(SizeType.Percent, 50.0F))
        toolbar.RowStyles.Add(New System.Windows.Forms.RowStyle(SizeType.Percent, 50.0F))

        Dim structTip As New System.Windows.Forms.ToolTip()

        Dim btnAddSurface As New System.Windows.Forms.Button() With {.Text = "+ Surface", .Dock = DockStyle.Fill, .Margin = New Padding(4), .FlatStyle = FlatStyle.Flat, .BackColor = Color.White, .Cursor = Cursors.Hand}
        Dim btnAddSection As New System.Windows.Forms.Button() With {.Text = "+ Section", .Dock = DockStyle.Fill, .Margin = New Padding(4), .FlatStyle = FlatStyle.Flat, .BackColor = Color.White, .Cursor = Cursors.Hand}
        Dim btnAddControl As New System.Windows.Forms.Button() With {.Text = "+ Control", .Dock = DockStyle.Fill, .Margin = New Padding(4), .FlatStyle = FlatStyle.Flat, .BackColor = Color.White, .Cursor = Cursors.Hand}
        Dim btnDeleteBlock As New System.Windows.Forms.Button() With {.Text = "Delete", .Dock = DockStyle.Fill, .Margin = New Padding(4), .FlatStyle = FlatStyle.Flat, .BackColor = Color.White, .Cursor = Cursors.Hand}
        structTip.SetToolTip(btnAddSurface, "Add a Surface after the selected one (or at the end)")
        structTip.SetToolTip(btnAddSection, "Add a Section after the selected one (or as the surface's last section)")
        structTip.SetToolTip(btnAddControl, "Add a Control after the selected one (or as the section's last control)")
        structTip.SetToolTip(btnDeleteBlock, "Delete the selected block")

        AddHandler btnAddSurface.Click, AddressOf AddSurface_Click
        AddHandler btnAddSection.Click, AddressOf AddSection_Click
        AddHandler btnAddControl.Click, AddressOf AddControl_Click
        AddHandler btnDeleteBlock.Click, AddressOf DeleteBlock_Click

        toolbar.Controls.Add(btnAddSurface, 0, 0)
        toolbar.Controls.Add(btnAddSection, 1, 0)
        toolbar.Controls.Add(btnAddControl, 0, 1)
        toolbar.Controls.Add(btnDeleteBlock, 1, 1)
        outer.Controls.Add(toolbar, 0, 0)

        tvStructure = New System.Windows.Forms.TreeView()
        tvStructure.Dock = DockStyle.Fill
        tvStructure.AllowDrop = True
        tvStructure.HideSelection = False
        AddHandler tvStructure.AfterSelect, AddressOf tvStructure_AfterSelect
        AddHandler tvStructure.NodeMouseDoubleClick, AddressOf tvStructure_NodeMouseDoubleClick
        AddHandler tvStructure.ItemDrag, AddressOf tvStructure_ItemDrag
        AddHandler tvStructure.DragEnter, AddressOf tvStructure_DragEnter
        AddHandler tvStructure.DragOver, AddressOf tvStructure_DragOver
        AddHandler tvStructure.DragDrop, AddressOf tvStructure_DragDrop
        outer.Controls.Add(tvStructure, 0, 1)

        btnToggleStructureTree.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnToggleStructureTree.Text = "Structure Tree"
        AddHandler btnToggleStructureTree.Click, AddressOf ToggleStructureTree_Click
        Dim insertAt = ToolStrip2.Items.IndexOf(btnHover)
        If insertAt >= 0 Then
            ToolStrip2.Items.Insert(insertAt + 1, btnToggleStructureTree)
        Else
            ToolStrip2.Items.Add(btnToggleStructureTree)
        End If
    End Sub

    Private Sub ToggleStructureTree_Click(sender As Object, e As EventArgs)
        pnlStructureTree.Visible = Not pnlStructureTree.Visible
        If pnlStructureTree.Visible Then
            ' The panel (and its docked toolbar/tree children) was built while
            ' Visible=False; WinForms can skip laying out an invisible subtree,
            ' so force a fresh layout pass now that it's actually on screen -
            ' otherwise the toolbar's reserved height can be stale/zero and the
            ' buttons appear to sit on top of the tree.
            pnlStructureTree.PerformLayout()
            RefreshStructureTree()
        End If
    End Sub

    ' Reads the current node under a tree click and jumps the editor to it,
    ' highlighting its whole line range directly by index. Deliberately does
    ' NOT use the existing selectText() helper here - that one finds a line by
    ' searching the document text for a match, which breaks for Control blocks
    ' since every Control's separator/keyword lines are textually identical
    ' (IndexOf always jumps to the first one in the file).
    Private Sub tvStructure_AfterSelect(sender As Object, e As TreeViewEventArgs)
        Dim blk = TryCast(e.Node.Tag, GeomBlock)
        If blk Is Nothing OrElse String.IsNullOrEmpty(projectName) Then Return
        tc1.SelectedIndex = 0   ' Geometry tab
        SelectEditorLineRange(blk.StartLine, blk.EndLine)
    End Sub

    Private Sub SelectEditorLineRange(startLine As Integer, endLine As Integer)
        If txt3 Is Nothing OrElse txt3.LinesCount = 0 Then Return
        If startLine < 0 OrElse startLine >= txt3.LinesCount Then Return
        Dim clampedEnd = Math.Min(Math.Max(endLine, startLine), txt3.LinesCount - 1)
        txt3.Selection.Start = New Place(0, startLine)
        txt3.Selection.End = New Place(txt3.Lines(clampedEnd).Length, clampedEnd)
        txt3.Invalidate()
        ' DoCaretVisible() only guarantees the caret (selection end) is on
        ' screen, which can leave a multi-line block's start scrolled out of
        ' view. DoRangeVisible scrolls to fit as much of the whole range as
        ' the viewport allows, starting from its top.
        txt3.DoRangeVisible(txt3.Selection, True)
    End Sub

    ' Double-click a Section or Control node to open it in the properties
    ' panel (same panel a canvas click opens), in addition to the single-click
    ' navigation above. Matches the tree block to its corresponding Node in
    ' `points` via DataLine, since that's what the properties panel operates on.
    Private Sub tvStructure_NodeMouseDoubleClick(sender As Object, e As TreeNodeMouseClickEventArgs)
        Dim blk = TryCast(e.Node.Tag, GeomBlock)
        If blk Is Nothing OrElse blk.DataLine < 0 Then Return
        If blk.Kind <> "Section" AndAlso blk.Kind <> "Control" Then Return
        If String.IsNullOrEmpty(projectName) Then Return

        tc1.SelectedIndex = 0   ' Geometry tab
        SelectEditorLineRange(blk.StartLine, blk.EndLine)
        findPoints()   ' ensure `points` reflects the now-active Geometry tab's live text

        Dim matched As Node = Nothing
        For Each p As Node In points
            If p.type <> Node.NodeType.Geometry OrElse p.IsDuplicate Then Continue For
            If blk.Kind = "Section" Then
                If p.SubType = Node.NodeSubType.LeadingEdge AndAlso p.lineNumber = blk.DataLine Then
                    matched = p
                    Exit For
                End If
            Else
                If p.SubType = Node.NodeSubType.ControlHinge AndAlso p.controlLineNumber = blk.DataLine Then
                    matched = p
                    Exit For
                End If
            End If
        Next
        If matched IsNot Nothing Then ShowNodeProperties(matched)
    End Sub

    Private Sub tvStructure_ItemDrag(sender As Object, e As ItemDragEventArgs)
        tvStructure.DoDragDrop(e.Item, DragDropEffects.Move)
    End Sub

    Private Sub tvStructure_DragEnter(sender As Object, e As DragEventArgs)
        e.Effect = DragDropEffects.Move
    End Sub

    Private Sub tvStructure_DragOver(sender As Object, e As DragEventArgs)
        Dim pt = tvStructure.PointToClient(New Point(e.X, e.Y))
        Dim targetNode = tvStructure.GetNodeAt(pt)
        Dim draggedNode = TryCast(e.Data.GetData(GetType(TreeNode)), TreeNode)
        If targetNode IsNot Nothing Then tvStructure.SelectedNode = targetNode
        e.Effect = If(targetNode IsNot Nothing AndAlso draggedNode IsNot Nothing AndAlso draggedNode IsNot targetNode AndAlso IsValidDropTarget(draggedNode, targetNode),
                      DragDropEffects.Move, DragDropEffects.None)
    End Sub

    Private Sub tvStructure_DragDrop(sender As Object, e As DragEventArgs)
        Dim pt = tvStructure.PointToClient(New Point(e.X, e.Y))
        Dim targetNode = tvStructure.GetNodeAt(pt)
        Dim draggedNode = TryCast(e.Data.GetData(GetType(TreeNode)), TreeNode)
        If draggedNode Is Nothing OrElse targetNode Is Nothing OrElse draggedNode Is targetNode Then Return
        If Not IsValidDropTarget(draggedNode, targetNode) Then Return
        MoveBlock(draggedNode, targetNode)
    End Sub

    ' Same-kind drops reorder as siblings (dropped node goes right after the
    ' target). Cross-kind drops append into a container (Section -> Surface,
    ' Control -> Section). Surface can only reorder against Surface.
    Private Function IsValidDropTarget(dragged As TreeNode, target As TreeNode) As Boolean
        Dim dblk = TryCast(dragged.Tag, GeomBlock)
        Dim tblk = TryCast(target.Tag, GeomBlock)
        If dblk Is Nothing OrElse tblk Is Nothing Then Return False
        If IsAncestor(dragged, target) Then Return False
        Select Case dblk.Kind
            Case "Surface"
                Return tblk.Kind = "Surface"
            Case "Section"
                Return tblk.Kind = "Section" OrElse tblk.Kind = "Surface"
            Case "Control"
                Return tblk.Kind = "Control" OrElse tblk.Kind = "Section"
        End Select
        Return False
    End Function

    Private Function IsAncestor(possibleAncestor As TreeNode, node As TreeNode) As Boolean
        Dim p = node.Parent
        While p IsNot Nothing
            If p Is possibleAncestor Then Return True
            p = p.Parent
        End While
        Return False
    End Function

    ' Cuts the dragged block's exact text range out of the file and reinserts
    ' it next to the drop target, adjusting for the index shift caused by the
    ' removal when the target sits later in the file than the dragged block.
    Private Sub MoveBlock(draggedNode As TreeNode, targetNode As TreeNode)
        If String.IsNullOrEmpty(projectName) Then Return
        Dim f = Path.Combine(Application.StartupPath, $"{projectName}.avl")
        If Not File.Exists(f) Then Return
        Dim lines As New List(Of String)(TrimAll(File.ReadAllText(f)).Replace(vbLf, "").Split(CChar(vbCrLf)))

        Dim dblk = DirectCast(draggedNode.Tag, GeomBlock)
        Dim tblk = DirectCast(targetNode.Tag, GeomBlock)

        Dim blockLines = lines.GetRange(dblk.StartLine, dblk.EndLine - dblk.StartLine + 1)
        lines.RemoveRange(dblk.StartLine, dblk.EndLine - dblk.StartLine + 1)

        Dim insertIndex As Integer
        If dblk.Kind = tblk.Kind Then
            ' Sibling reorder: right after the target block's own closing line.
            insertIndex = tblk.EndLine + 1
        ElseIf dblk.Kind = "Section" AndAlso tblk.Kind = "Surface" Then
            ' Sections are nested inside !beginsurface/!endsurface - insert before !endsurface.
            insertIndex = tblk.EndLine
        ElseIf dblk.Kind = "Control" AndAlso tblk.Kind = "Section" Then
            ' Controls trail !endsection as siblings, not nested inside it - append
            ' after the section's last existing control (or after the section itself).
            If tblk.Children.Count > 0 Then
                insertIndex = tblk.Children(tblk.Children.Count - 1).EndLine + 1
            Else
                insertIndex = tblk.EndLine + 1
            End If
        Else
            Return
        End If

        If dblk.StartLine < insertIndex Then insertIndex -= blockLines.Count
        lines.InsertRange(insertIndex, blockLines)

        SaveAndRefreshGeometry(lines)
    End Sub

    Private Function GetSelectedBlock() As GeomBlock
        Dim n = tvStructure.SelectedNode
        If n Is Nothing Then Return Nothing
        Return TryCast(n.Tag, GeomBlock)
    End Function

    ' Inserted right after the selected Surface if one is selected; otherwise
    ' appended at the end of the geometry (before !endgeometry).
    Private Sub AddSurface_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(projectName) Then Return
        Dim f = Path.Combine(Application.StartupPath, $"{projectName}.avl")
        If Not File.Exists(f) Then Return
        Dim lines As New List(Of String)(TrimAll(File.ReadAllText(f)).Replace(vbLf, "").Split(CChar(vbCrLf)))

        Dim selBlk = GetSelectedBlock()
        Dim insertIndex As Integer
        If selBlk IsNot Nothing AndAlso selBlk.Kind = "Surface" Then
            insertIndex = selBlk.EndLine + 1
        Else
            Dim endGeomIdx = -1
            For k = 0 To lines.Count - 1
                If lines(k).Trim().ToLowerInvariant() = "!endgeometry" Then endGeomIdx = k : Exit For
            Next
            If endGeomIdx = -1 Then
                MessageBox.Show("Could not find the '!endgeometry' marker in this file - cannot add a new surface.", "Add Surface", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            insertIndex = endGeomIdx
        End If

        Dim template As String() = {
            "#====================================================================",
            "SURFACE",
            "[New Surface]",
            "!beginsurface",
            "#Nchordwise  Cspace   Nspanwise   Sspace",
            "8            1.0      8           1.0",
            "#",
            "ANGLE",
            "0.0",
            "#-------------------------------------------------------------",
            "SECTION",
            "!beginsection",
            "#Xle    Yle    Zle     Chord   Ainc  Nspanwise  Sspace",
            "0.0     0.0    0.0     1.0     0.0   0          0",
            "NACA",
            "0012",
            "!endsection",
            "!endsurface"
        }
        lines.InsertRange(insertIndex, template)
        SaveAndRefreshGeometry(lines)
    End Sub

    ' Inserted right after the selected Section if one is selected; otherwise
    ' appended as the last section of the selected/ancestor Surface.
    Private Sub AddSection_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(projectName) Then Return
        Dim surfBlk = GetSelectedSurfaceBlock()
        If surfBlk Is Nothing Then
            MessageBox.Show("Select a surface (or one of its sections/controls) first.", "Add Section", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Dim f = Path.Combine(Application.StartupPath, $"{projectName}.avl")
        If Not File.Exists(f) Then Return
        Dim lines As New List(Of String)(TrimAll(File.ReadAllText(f)).Replace(vbLf, "").Split(CChar(vbCrLf)))

        Dim selBlk = GetSelectedBlock()
        Dim insertIndex As Integer
        If selBlk IsNot Nothing AndAlso selBlk.Kind = "Section" Then
            insertIndex = selBlk.EndLine + 1
        Else
            insertIndex = surfBlk.EndLine   ' before !endsurface = last section
        End If

        Dim template As String() = {
            "#-------------------------------------------------------------",
            "SECTION",
            "!beginsection",
            "#Xle    Yle    Zle     Chord   Ainc  Nspanwise  Sspace",
            "0.0     0.0    0.0     1.0     0.0   0          0",
            "NACA",
            "0012",
            "!endsection"
        }
        lines.InsertRange(insertIndex, template)
        SaveAndRefreshGeometry(lines)
    End Sub

    ' Inserted right after the selected Control if one is selected; otherwise
    ' appended as the last control of the selected/ancestor Section.
    Private Sub AddControl_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(projectName) Then Return
        Dim secBlk = GetSelectedSectionBlock()
        If secBlk Is Nothing Then
            MessageBox.Show("Select a section (or one of its controls) first.", "Add Control", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Dim f = Path.Combine(Application.StartupPath, $"{projectName}.avl")
        If Not File.Exists(f) Then Return
        Dim lines As New List(Of String)(TrimAll(File.ReadAllText(f)).Replace(vbLf, "").Split(CChar(vbCrLf)))

        Dim selBlk = GetSelectedBlock()
        Dim insertIndex As Integer
        If selBlk IsNot Nothing AndAlso selBlk.Kind = "Control" Then
            insertIndex = selBlk.EndLine + 1
        ElseIf secBlk.Children.Count > 0 Then
            insertIndex = secBlk.Children(secBlk.Children.Count - 1).EndLine + 1
        Else
            insertIndex = secBlk.EndLine + 1
        End If

        Dim template As String() = {
            "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++",
            "CONTROL",
            "!begincontrol",
            "#Cname   Cgain  Xhinge  HingeVec     SgnDup",
            "control1 1.0    0.75    0.0 0.0 0.0  1.0",
            "!endcontrol"
        }
        lines.InsertRange(insertIndex, template)
        SaveAndRefreshGeometry(lines)
    End Sub

    Private Sub DeleteBlock_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(projectName) Then Return
        Dim n = tvStructure.SelectedNode
        If n Is Nothing Then Return
        Dim blk = TryCast(n.Tag, GeomBlock)
        If blk Is Nothing Then Return

        Dim res = MessageBox.Show($"Delete this {blk.Kind.ToLowerInvariant()} ({blk.Label})? This removes its entire text block from the file.",
                                   "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
        If res = DialogResult.No Then Return

        Dim f = Path.Combine(Application.StartupPath, $"{projectName}.avl")
        If Not File.Exists(f) Then Return
        Dim lines As New List(Of String)(TrimAll(File.ReadAllText(f)).Replace(vbLf, "").Split(CChar(vbCrLf)))
        lines.RemoveRange(blk.StartLine, blk.EndLine - blk.StartLine + 1)
        SaveAndRefreshGeometry(lines)
    End Sub

    Private Function GetSelectedSurfaceBlock() As GeomBlock
        Dim n = tvStructure.SelectedNode
        If n Is Nothing Then Return Nothing
        Dim blk = TryCast(n.Tag, GeomBlock)
        While blk IsNot Nothing AndAlso blk.Kind <> "Surface"
            n = n.Parent
            blk = If(n IsNot Nothing, TryCast(n.Tag, GeomBlock), Nothing)
        End While
        Return blk
    End Function

    Private Function GetSelectedSectionBlock() As GeomBlock
        Dim n = tvStructure.SelectedNode
        If n Is Nothing Then Return Nothing
        Dim blk = TryCast(n.Tag, GeomBlock)
        If blk IsNot Nothing AndAlso blk.Kind = "Control" Then
            n = n.Parent
            blk = If(n IsNot Nothing, TryCast(n.Tag, GeomBlock), Nothing)
        End If
        If blk IsNot Nothing AndAlso blk.Kind = "Section" Then Return blk
        Return Nothing
    End Function

    ' Shared write+refresh tail for every structure-tree mutation (move/add/delete).
    ' Replaces txt3's content via a full-selection InsertText (not a raw .Text set)
    ' so Ctrl+Z in the editor still undoes it when the Geometry tab is active.
    Private Sub SaveAndRefreshGeometry(lines As List(Of String))
        Dim f = Path.Combine(Application.StartupPath, $"{projectName}.avl")
        Dim newContent = String.Join(vbCrLf, lines)
        File.WriteAllText(f, newContent)

        If tc1.SelectedTab IsNot Nothing AndAlso tc1.SelectedTab.Name = "Geometry" Then
            updating = True
            txt3.Selection.Start = New Place(0, 0)
            txt3.Selection.End = New Place(txt3.Lines(txt3.LinesCount - 1).Length, txt3.LinesCount - 1)
            txt3.InsertText(newContent)
            updating = False
            ApplySyntaxHighlighting()
            FormatActiveText()
        End If

        findPoints()
        drawAxes()
    End Sub

    ' Rebuilds the tree from the current .avl file. Called from findPoints() so
    ' it stays live across typing, drag-edits, properties-panel saves, etc. -
    ' the same central refresh hook everything else already relies on.
    Private Sub RefreshStructureTree()
        If pnlStructureTree Is Nothing OrElse Not pnlStructureTree.Visible Then Return
        If String.IsNullOrEmpty(projectName) Then Return

        ' Mirror findPoints()'s own convention: read the LIVE editor buffer
        ' when the Geometry tab is active (so typed-but-not-yet-saved edits
        ' show up immediately), falling back to disk otherwise. Reading from
        ' disk unconditionally was why the tree lagged behind the editor.
        Dim avlText As String
        If tc1.SelectedTab IsNot Nothing AndAlso tc1.SelectedTab.Name = "Geometry" AndAlso txt3 IsNot Nothing Then
            avlText = txt3.Text
        Else
            Dim f = Path.Combine(Application.StartupPath, $"{projectName}.avl")
            If Not File.Exists(f) Then Return
            avlText = File.ReadAllText(f)
        End If

        Dim lines() As String = TrimAll(avlText).Replace(vbLf, "").Split(CChar(vbCrLf))
        Dim surfaces = ScanGeometryBlocks(lines)

        Dim expandedPaths As New HashSet(Of String)
        CollectExpandedPaths(tvStructure.Nodes, expandedPaths)

        tvStructure.BeginUpdate()
        tvStructure.Nodes.Clear()
        For Each surf In surfaces
            Dim surfNode = New TreeNode(surf.Label) With {.Tag = surf}
            For Each sec In surf.Children
                Dim secNode = New TreeNode(sec.Label) With {.Tag = sec}
                For Each ctrl In sec.Children
                    secNode.Nodes.Add(New TreeNode(ctrl.Label) With {.Tag = ctrl})
                Next
                surfNode.Nodes.Add(secNode)
            Next
            tvStructure.Nodes.Add(surfNode)
        Next

        If expandedPaths.Count = 0 Then
            tvStructure.ExpandAll()
        Else
            RestoreExpandedPaths(tvStructure.Nodes, expandedPaths)
        End If
        tvStructure.EndUpdate()
    End Sub

    Private Sub CollectExpandedPaths(nodes As TreeNodeCollection, into As HashSet(Of String))
        For Each n As TreeNode In nodes
            If n.IsExpanded Then into.Add(n.FullPath)
            CollectExpandedPaths(n.Nodes, into)
        Next
    End Sub

    Private Sub RestoreExpandedPaths(nodes As TreeNodeCollection, paths As HashSet(Of String))
        For Each n As TreeNode In nodes
            If paths.Contains(n.FullPath) Then n.Expand()
            RestoreExpandedPaths(n.Nodes, paths)
        Next
    End Sub

    ' Parses the file into a Surface > Section > Control block tree using this
    ' app's own !beginX/!endX markers, which unambiguously delimit each block's
    ' exact line range - unlike findPoints' keyword-adjacency heuristic, these
    ' are reliable enough to cut/paste/delete against directly.
    Private Function ScanGeometryBlocks(lines() As String) As List(Of GeomBlock)
        Dim surfaces As New List(Of GeomBlock)
        Dim i = 0
        While i < lines.Length
            If lines(i).Trim().ToLowerInvariant() = "surface" Then
                Dim surfStart = ExtendBackForSeparator(lines, i)
                Dim name = If(i + 1 < lines.Length, lines(i + 1).Trim().Trim("["c, "]"c), "Surface")
                If name = "" Then name = "Surface"
                Dim surfEnd = FindMarker(lines, i, "!endsurface")
                If surfEnd = -1 Then surfEnd = lines.Length - 1
                Dim surf As New GeomBlock() With {.Kind = "Surface", .Label = name, .StartLine = surfStart, .EndLine = surfEnd}

                Dim j = i + 1
                Dim lastSection As GeomBlock = Nothing
                While j <= surfEnd
                    Dim t = lines(j).Trim().ToLowerInvariant()
                    If t = "section" Then
                        Dim secStart = ExtendBackForSeparator(lines, j)
                        Dim secEnd = FindMarker(lines, j, "!endsection")
                        If secEnd = -1 OrElse secEnd > surfEnd Then secEnd = surfEnd
                        Dim secDataLine = FindFirstDataLine(lines, j)
                        Dim sec As New GeomBlock() With {.Kind = "Section", .Label = DescribeSection(lines, secDataLine), .StartLine = secStart, .EndLine = secEnd, .DataLine = secDataLine}
                        surf.Children.Add(sec)
                        lastSection = sec
                        j = secEnd + 1
                        Continue While
                    End If
                    If t = "control" Then
                        Dim ctrlStart = ExtendBackForSeparator(lines, j)
                        Dim ctrlEnd = FindMarker(lines, j, "!endcontrol")
                        If ctrlEnd = -1 OrElse ctrlEnd > surfEnd Then ctrlEnd = surfEnd
                        Dim ctrlDataLine = FindFirstDataLine(lines, j)
                        Dim ctrl As New GeomBlock() With {.Kind = "Control", .Label = DescribeControl(lines, ctrlDataLine), .StartLine = ctrlStart, .EndLine = ctrlEnd, .DataLine = ctrlDataLine}
                        If lastSection IsNot Nothing Then
                            lastSection.Children.Add(ctrl)
                        Else
                            surf.Children.Add(ctrl)
                        End If
                        j = ctrlEnd + 1
                        Continue While
                    End If
                    j += 1
                End While

                surfaces.Add(surf)
                i = surfEnd + 1
                Continue While
            End If
            i += 1
        End While
        Return surfaces
    End Function

    Private Function FindMarker(lines() As String, fromLine As Integer, marker As String) As Integer
        For k = fromLine To lines.Length - 1
            If lines(k).Trim().ToLowerInvariant() = marker Then Return k
        Next
        Return -1
    End Function

    ' Rolls a decorative separator comment line (e.g. "#----", "#====") that
    ' immediately precedes a block's keyword into the block itself, so
    ' moving/deleting a block doesn't leave an orphaned separator behind.
    Private Function ExtendBackForSeparator(lines() As String, keywordLine As Integer) As Integer
        Dim k = keywordLine - 1
        If k >= 0 Then
            Dim t = lines(k).Trim()
            If t.StartsWith("#") Then
                Dim body = t.TrimStart("#"c)
                If body.Length > 0 AndAlso body.Trim(body(0)) = "" Then Return k
            End If
        End If
        Return keywordLine
    End Function

    ' First non-blank, non-comment, non-marker line after a block's keyword -
    ' i.e. the actual data row. Shared by the describers below AND by the
    ' double-click handler, which needs this exact line number to match a
    ' block against the corresponding Node in `points` (Node.lineNumber /
    ' Node.controlLineNumber point at this same row).
    Private Function FindFirstDataLine(lines() As String, keywordLine As Integer) As Integer
        For k = keywordLine + 1 To Math.Min(lines.Length - 1, keywordLine + 5)
            Dim t = lines(k).Trim()
            If t = "" OrElse t.StartsWith("#") OrElse t.StartsWith("!") Then Continue For
            Return k
        Next
        Return -1
    End Function

    Private Function DescribeSection(lines() As String, dataLine As Integer) As String
        If dataLine = -1 Then Return "Section"
        Dim nums As New List(Of String)
        For Each v In lines(dataLine).Split(CChar(" "))
            If IsNumeric(v) Then nums.Add(v)
        Next
        If nums.Count >= 4 Then Return $"Section (Xle={nums(0)}, Chord={nums(3)})"
        Return "Section"
    End Function

    Private Function DescribeControl(lines() As String, dataLine As Integer) As String
        If dataLine = -1 Then Return "Control"
        Dim toks As New List(Of String)
        For Each v In lines(dataLine).Split(CChar(" "))
            If v.Trim().Length > 0 Then toks.Add(v.Trim())
        Next
        If toks.Count > 0 Then Return toks(0)
        Return "Control"
    End Function

    Private Sub btnClear_Click(sender As Object, e As EventArgs) Handles btnClear.Click
        If MessageBox.Show("Are you sure you want to clear the current file? This cannot be undone.",
                           "Clear File",
                           MessageBoxButtons.YesNo,
                           MessageBoxIcon.Warning,
                           MessageBoxDefaultButton.Button2) = DialogResult.Yes Then
            txt3.Text = ""
        End If
    End Sub


    Public Sub CaptureApplication(ByVal procWindowTitle As String, ByVal caption As String)
        Dim proc As Process = Nothing
        While IsNothing(proc)
            Application.DoEvents()
            For Each p As Process In Process.GetProcesses()
                If (p.MainWindowTitle = procWindowTitle) Then
                    proc = p
                    Exit For
                End If
                'Debug.WriteLine(p.MainWindowTitle)
            Next
        End While
        Threading.Thread.Sleep(200) 'wait for window to fully open
        'Dim proc = Process.GetProcessesByName(procName)(0)
        Dim rectw = New User32.Rect()
        Dim rect = New User32.Rect()
        Dim cpoint = New User32.POINTAPI()
        User32.GetWindowRect(proc.MainWindowHandle, rectw)
        User32.GetClientRect(proc.MainWindowHandle, rect)
        User32.ClientToScreen(proc.MainWindowHandle, cpoint)
        User32.SetWindowPos(proc.MainWindowHandle, User32.HWND_TOPMOST, rectw.left, rectw.top, rectw.right - rectw.left, rectw.bottom - rectw.top, User32.SWP_SHOWWINDOW)
        Dim width As Integer = rect.right - rect.left
        Dim height As Integer = rect.bottom - rect.top
        'Dim bmp = New Bitmap(width, height, PixelFormat.Format32bppArgb)
        Dim bmp = New Bitmap(rect.right, rect.bottom, PixelFormat.Format32bppArgb)

        Using graphics As Graphics = Graphics.FromImage(bmp)
            'graphics.CopyFromScreen(rect.left, rect.top, 0, 0, New Size(width, height), CopyPixelOperation.SourceCopy)
            graphics.CopyFromScreen(cpoint.X, cpoint.Y, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy)
        End Using

        'bmp.Save("c:\tmp\test.png", ImageFormat.Png)

        Dim frm As New Form
        Dim p1 As New PictureBox
        p1.Image = bmp
        p1.SizeMode = PictureBoxSizeMode.AutoSize
        frm.ClientSize = p1.Size
        p1.Dock = DockStyle.Fill
        frm.Controls.Add(p1)
        frm.Text = caption
        frm.Icon = frmMain.Icon
        frm.StartPosition = FormStartPosition.CenterScreen
        frm.Show()
        'frmMain.p.StandardInput.WriteLine("quit")
        frmMain.RestartConsoleToolStripMenuItem.PerformClick()
    End Sub
    Private Class User32
        <StructLayout(LayoutKind.Sequential)>
        Public Structure Rect
            Public left As Integer
            Public top As Integer
            Public right As Integer
            Public bottom As Integer
        End Structure
        Public Structure POINTAPI
            Public X As Integer
            Public Y As Integer
        End Structure
        <DllImport("user32.dll")>
        Public Shared Function GetWindowRect(ByVal hWnd As IntPtr, ByRef rect As Rect) As IntPtr
        End Function
        <DllImport("user32.dll")>
        Public Shared Function GetClientRect(ByVal hWnd As IntPtr, ByRef rect As Rect) As Int32
        End Function
        <DllImport("user32.dll")>
        Public Shared Function ClientToScreen(ByVal hWnd As IntPtr, ByRef lpPoint As POINTAPI) As Boolean
        End Function
        <DllImport("user32.dll")>
        Public Shared Function SetWindowPos(ByVal hWnd As IntPtr, ByVal hWndInsertAfter As IntPtr, ByVal X As Integer, ByVal Y As Integer, ByVal cx As Integer, ByVal cy As Integer, ByVal uFlags As UInteger) As Boolean
        End Function
        Public Shared ReadOnly HWND_TOPMOST As IntPtr = New IntPtr(-1)
        Public Const SWP_NOSIZE As UInt32 = &H1
        Public Const SWP_NOMOVE As UInt32 = &H2
        Public Const SWP_SHOWWINDOW As UInt32 = &H40


    End Class

    Private Sub findPoints()
        'Debug.WriteLine($"==================================================================")


        Dim geometryfilename = Path.Combine(Application.StartupPath, $"{projectName}.avl")
        Dim avlText As String = ""
        Dim isGeometryTab As Boolean = (tc1.SelectedTab IsNot Nothing AndAlso tc1.SelectedTab.Name = "Geometry" AndAlso txt3 IsNot Nothing)

        If isGeometryTab Then
            avlText = txt3.Text
        ElseIf File.Exists(geometryfilename) Then
            avlText = File.ReadAllText(geometryfilename)
        Else
            Return
        End If

        Dim lines() As String = TrimAll(avlText.Replace(vbLf, "")).Split(CChar(vbCrLf))


        Dim c As Integer = 0
        Dim vals As String()
        'Dim section As Section
        'Dim surface As Surface
        'Dim sections As List(Of Section)
        Dim surfaces As List(Of Surface) = New List(Of Surface)
        Dim i As Integer = 0

        Do While (i < lines.Count - 2)
            'Debug.WriteLine($"Line number: {i} | {lines(i).ToLower.Trim = "surface"} | {lines(i)}")
            If lines(i).ToLower.Trim = "surface" Then
                'MsgBox("found a surface")
                'Debug.WriteLine($"Found surface at line {i + 1}")
                Dim surface = New Surface
                surface.sections = New List(Of Section)
                Dim controls = New List(Of Control)
                surface.Name = lines(i + 1).Trim.Replace(Environment.NewLine, "")
                'MsgBox(lines(i + 1))
                i += 2
                Do While (lines(i + 1).ToLower.Trim <> "surface" And (i < lines.Count - 2))
                    If lines(i).ToLower.Trim = "yduplicate" Then
                        surface.yDuplicate = True
                        surface.yDuplicatevalue = CDbl(lines(i + 1))
                    End If

                    If lines(i).ToLower.Trim.StartsWith("#nchordwise") Then
                        vals = lines(i + 1).Split(CChar(" "))
                        Dim nc As Integer = 0
                        For l = 0 To UBound(vals)
                            If IsNumeric(vals(l)) Then
                                nc += 1
                                Select Case nc
                                    Case 1 : surface.Nchordwise = CDbl(vals(l))
                                    Case 2 : surface.Cspace = CDbl(vals(l))
                                    Case 3 : surface.Nspanwise = CDbl(vals(l))
                                    Case 4 : surface.Sspace = CDbl(vals(l))
                                End Select
                            End If
                        Next
                    End If

                    'check for sections
                    If lines(i).ToLower.Trim = "section" Then
                        'Debug.WriteLine($"Found    section at line {i + 1}")
                        'MsgBox("found a section")
                        i += 1
                    End If
                    If lines(i).ToLower.Trim.StartsWith("#xle") Then
                        vals = lines(i + 1).Split(CChar(" "))
                        Dim counter As Integer = 0
                        Dim section = New Section
                        section.controls = New List(Of ControlSurface)
                        section.lineNumber = i + 1
                        For l = 0 To UBound(vals)
                            If IsNumeric(vals(l)) Then
                                counter += 1
                                Select Case counter
                                    Case 1
                                        section.Xle = CDbl(vals(l))
                                    Case 2
                                        section.Yle = CDbl(vals(l))
                                    Case 3
                                        section.Zle = CDbl(vals(l))
                                    Case 4
                                        section.Chord = CDbl(vals(l))
                                    Case 5
                                        section.Ainc = CDbl(vals(l))
                                    Case 6
                                        section.Nspanwise = CDbl(vals(l))
                                    Case 7
                                        section.Sspace = CDbl(vals(l))
                                End Select
                            End If
                        Next

                        Do While (lines(i + 2).ToLower.Trim <> "surface" And lines(i + 2).ToLower.Trim <> "section" And i < lines.Count - 3)

                            'check for controls
                            If lines(i).ToLower.Trim = "control" Then
                                'Debug.WriteLine($"Found       control at line {i + 1}")
                                i += 1
                            End If

                            If lines(i).ToLower.Trim.StartsWith("#cname") Then
                                vals = lines(i + 1).Split(CChar(" "))
                                Dim control = New ControlSurface
                                control.lineNumber = i + 1
                                control.Type = vals(0)
                                control.Cgain = CDbl(vals(1))
                                control.Xhinge = CDbl(vals(2))
                                section.controls.Add(control)
                            End If

                            i += 1

                        Loop


                        surface.sections.Add(section)
                    End If

                    i += 1

                Loop
                surfaces.Add(surface)
            End If
            i += 1
        Loop

        'Me.Text = $"Surface Count: {surfaces.Count.ToString}"

        parsedSurfaces = surfaces
        points.Clear()

        If surfaces.Count > 0 Then
            For Each su As Surface In surfaces
                For Each se As Section In su.sections
                    With se
                        ' Leading edge — draggable, updates Xle/Yle/Zle
                        Dim p1 As Point3D = New Point3D(.Xle, .Yle + If(su.yDuplicate, su.yDuplicatevalue, 0), .Zle)
                        Dim n1 = New Node(p1, su.Name, False, se.lineNumber, Node.NodeType.Geometry)
                        n1.SubType = Node.NodeSubType.LeadingEdge : n1.IsDuplicate = False
                        points.Add(n1)
                        ' Trailing edge — draggable, updates Chord only
                        Dim p2 As Point3D = New Point3D(.Xle + .Chord, .Yle + If(su.yDuplicate, su.yDuplicatevalue, 0), .Zle)
                        Dim n2 = New Node(p2, su.Name, False, se.lineNumber, Node.NodeType.Geometry)
                        n2.SubType = Node.NodeSubType.TrailingEdge : n2.IsDuplicate = False
                        n2.parentXle = CSng(.Xle) : n2.parentChord = CSng(.Chord)
                        points.Add(n2)
                        If (se.controls.Count > 0) Then
                            For Each cs As ControlSurface In se.controls
                                ' Control hinge (pc1) — draggable, updates Xhinge only
                                Dim pc1 As Point3D = New Point3D(.Xle + If(cs.Xhinge > 0, cs.Xhinge, 1 - cs.Xhinge) * .Chord, .Yle + If(su.yDuplicate, su.yDuplicatevalue, 0), .Zle)
                                Dim nc1 = New Node(pc1, "control" + cs.Type.Replace(vbLf, ""), False, se.lineNumber, Node.NodeType.Geometry)
                                nc1.SubType = Node.NodeSubType.ControlHinge : nc1.IsDuplicate = False
                                nc1.parentXle = CSng(.Xle) : nc1.parentChord = CSng(.Chord)
                                nc1.controlLineNumber = cs.lineNumber : nc1.originalXhinge = CSng(cs.Xhinge)
                                points.Add(nc1)
                                ' Control trailing edge (pc2) — not independently draggable (same as n2)
                                Dim pc2 As Point3D = New Point3D(.Xle + .Chord, .Yle + If(su.yDuplicate, su.yDuplicatevalue, 0), .Zle)
                                points.Add(New Node(pc2, "control" + cs.Type.Replace(vbLf, ""), False, se.lineNumber, Node.NodeType.Geometry))
                            Next
                        End If
                        If su.yDuplicate Then
                            ' Mirrored duplicates — not independently draggable (IsDraggable excludes
                            ' them), but they point at the SAME underlying line as the primary node,
                            ' so they're still clickable to open the properties panel for that line.
                            Dim p3 As Point3D = New Point3D(.Xle, -(.Yle + If(su.yDuplicate, su.yDuplicatevalue, 0)), .Zle)
                            Dim n3 = New Node(p3, su.Name + "_dup", False, se.lineNumber, Node.NodeType.Geometry)
                            n3.SubType = Node.NodeSubType.LeadingEdge : n3.IsDuplicate = True
                            points.Add(n3)
                            Dim p4 As Point3D = New Point3D(.Xle + .Chord, -(.Yle + If(su.yDuplicate, su.yDuplicatevalue, 0)), .Zle)
                            Dim n4 = New Node(p4, su.Name + "_dup", False, se.lineNumber, Node.NodeType.Geometry)
                            n4.SubType = Node.NodeSubType.TrailingEdge : n4.IsDuplicate = True
                            points.Add(n4)
                            If (se.controls.Count > 0) Then
                                For Each cs As ControlSurface In se.controls
                                    Dim pc3 As Point3D = New Point3D(.Xle + If(cs.Xhinge > 0, cs.Xhinge, 1 - cs.Xhinge) * .Chord, -(.Yle + If(su.yDuplicate, su.yDuplicatevalue, 0)), .Zle)
                                    Dim nc3 = New Node(pc3, "control" + cs.Type.Replace(vbLf, "") + "_dup", False, se.lineNumber, Node.NodeType.Geometry)
                                    nc3.SubType = Node.NodeSubType.ControlHinge : nc3.IsDuplicate = True
                                    nc3.parentXle = CSng(.Xle) : nc3.parentChord = CSng(.Chord)
                                    nc3.controlLineNumber = cs.lineNumber : nc3.originalXhinge = CSng(cs.Xhinge)
                                    points.Add(nc3)
                                    Dim pc4 As Point3D = New Point3D(.Xle + .Chord, -(.Yle + If(su.yDuplicate, su.yDuplicatevalue, 0)), .Zle)
                                    points.Add(New Node(pc4, "control" + cs.Type.Replace(vbLf, "") + "_dup", False, se.lineNumber, Node.NodeType.Geometry))
                                Next
                            End If
                        End If
                    End With
                Next
            Next
        End If


        Dim massText As String = ""
        Dim isMassTab As Boolean = (tc1.SelectedTab IsNot Nothing AndAlso tc1.SelectedTab.Name = "Mass" AndAlso txt3 IsNot Nothing)
        Dim hasMassData As Boolean = False
        Dim massfilename = Path.Combine(Application.StartupPath, $"{projectName}.mass")

        If isMassTab Then
            massText = txt3.Text
            hasMassData = True
        ElseIf File.Exists(massfilename) Then
            massText = File.ReadAllText(massfilename)
            hasMassData = True
        End If

        If hasMassData Then
            Dim massLines() As String = massText.Split(New String() {vbCrLf, vbLf, vbCr}, StringSplitOptions.None)
            Dim lnum = -1
            For Each line As String In massLines
                lnum += 1
                ' Trim double spaces to support split by space
                Dim cleanLine = line.Trim()
                While cleanLine.Contains("  ")
                    cleanLine = cleanLine.Replace("  ", " ")
                End While
                Dim pars = cleanLine.Split(CChar(" "))
                If pars.Length > 0 AndAlso pars(0) <> "" Then
                    Dim val As Double = 0
                    If (Double.TryParse(pars(0), val)) Then
                        Dim xval As Double = 0
                        Dim yval As Double = 0
                        Dim zval As Double = 0
                        If pars.Length > 1 Then Double.TryParse(pars(1), xval)
                        If pars.Length > 2 Then Double.TryParse(pars(2), yval)
                        If pars.Length > 3 Then Double.TryParse(pars(3), zval)
                        points.Add(New Node(xval, yval, zval, "Mass", False, lnum, Node.NodeType.Mass, CSng(val)))
                    End If
                End If
            Next
        End If

        updating = False

        RefreshStructureTree()

    End Sub

    Private Sub txt3_TextChangedDelayed(sender As Object, e As Object)
        If (updating = True) Then Return

        If My.Settings.autoSave Then
            ForceSaveActiveFile(True)
        Else
            isDirty = True
            UpdateDirtyWarning()
        End If

        findPoints()
        drawAxes()
    End Sub

    Private Function CountBlocks(Optional countforbefore As Boolean = True) As Dictionary(Of String, Integer)
        Dim result As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)
        Dim loc = txt3.SelectionStart
        Dim input = txt3.Text.Substring(0, loc)
        If (countforbefore = False) Then
            input = txt3.Text.Substring(loc, txt3.Text.Length - loc)
        End If
        Dim phrase = "!begingeometry"
        Dim Occurrences = Regex.Matches(input, $"{phrase}").Count
        result(phrase) = Occurrences
        phrase = "!beginsurface"
        Occurrences = Regex.Matches(input, $"{phrase}").Count
        result(phrase) = Occurrences
        phrase = "!beginsection"
        Occurrences = Regex.Matches(input, $"{phrase}").Count
        result(phrase) = Occurrences
        phrase = "!begincontrol"
        Occurrences = Regex.Matches(input, $"{phrase}").Count
        result(phrase) = Occurrences

        phrase = "!endgeometry"
        Occurrences = Regex.Matches(input, $"{phrase}").Count
        result(phrase) = Occurrences
        phrase = "!endsurface"
        Occurrences = Regex.Matches(input, $"{phrase}").Count
        result(phrase) = Occurrences
        phrase = "!endsection"
        Occurrences = Regex.Matches(input, $"{phrase}").Count
        result(phrase) = Occurrences
        phrase = "!endcontrol"
        Occurrences = Regex.Matches(input, $"{phrase}").Count
        result(phrase) = Occurrences

        'Dim before = CountBlocks()
        'Dim after = CountBlocks(False)

        If (countforbefore = True) Then
            For Each p As KeyValuePair(Of String, Integer) In result
                Console.WriteLine($"BEFORE: {p.Key} has {p.Value} items")
            Next
        Else
            For Each p As KeyValuePair(Of String, Integer) In result
                Console.WriteLine($"AFTER: {p.Key} has {p.Value} items")

            Next
        End If


        Return result
    End Function



    Private Sub txt3_ToolTipNeeded(sender As Object, e As ToolTipNeededEventArgs)

        If Not String.IsNullOrEmpty(e.HoveredWord) Then
            e.ToolTipIcon = ToolTipIcon.Info
            Select Case e.HoveredWord.ToLower
                Case "header", "mach", "iysym", "izsym", "zsym", "sref", "cref", "bref", "xref", "yref", "zref"
                    e.ToolTipTitle = "Header data"
                    e.ToolTipText = readLines(help, 243, 293)
                Case "cspace", "bspace", "sspace"
                    e.ToolTipTitle = "Vortex Lattice Spacing Distributions"
                    e.ToolTipText = readLines(help, 999, 1049)
                Case "surface", "nchordwise", "nspanwise"
                    e.ToolTipTitle = "Surface-definition keywords and data formats"
                    e.ToolTipText = readLines(help, 378, 400)
                Case "yduplicate"
                    e.ToolTipTitle = "Surface-definition keywords and data formats"
                    e.ToolTipText = readLines(help, 423, 450)
                Case "scale"
                    e.ToolTipTitle = "Surface-definition keywords and data formats"
                    e.ToolTipText = readLines(help, 452, 461)
                Case "translate"
                    e.ToolTipTitle = "Surface-definition keywords and data formats"
                    e.ToolTipText = readLines(help, 464, 474)
                Case "nowake"
                    e.ToolTipTitle = "Surface-definition keywords and data formats"
                    e.ToolTipText = readLines(help, 488, 495)
                Case "noalbe"
                    e.ToolTipTitle = "Surface-definition keywords and data formats"
                    e.ToolTipText = readLines(help, 497, 508)
                Case "noload"
                    e.ToolTipTitle = "Surface-definition keywords and data formats"
                    e.ToolTipText = readLines(help, 510, 538)
                Case "angle"
                    e.ToolTipTitle = "Surface-definition keywords and data formats"
                    e.ToolTipText = readLines(help, 476, 486)
                Case "cdcl"
                    e.ToolTipTitle = "Surface-definition keywords and data formats"
                    e.ToolTipText = readLines(help, 540, 573)
                Case "section", "xle", "yle", "zle", "chord", "ainc"
                    e.ToolTipTitle = "Section-definition keywords and data formats"
                    e.ToolTipText = readLines(help, 576, 625)
                Case "naca"
                    e.ToolTipTitle = "Section-definition keywords and data formats"
                    e.ToolTipText = readLines(help, 628, 642)
                Case "airfoil"
                    e.ToolTipTitle = "Section-definition keywords and data formats"
                    e.ToolTipText = readLines(help, 645, 671)
                Case "afile"
                    e.ToolTipTitle = "Section-definition keywords and data formats"
                    e.ToolTipText = readLines(help, 674, 694)
                Case "design"
                    e.ToolTipTitle = "Section-definition keywords and data formats"
                    e.ToolTipText = readLines(help, 697, 727)
                Case "control", "cname", "cgain", "xhinge", "hingevec", "sgnDup"
                    e.ToolTipTitle = "Control-definition keywords and data formats"
                    e.ToolTipText = readLines(help, 729, 755)
                Case "claf"
                    e.ToolTipTitle = "Control-definition keywords and data formats"
                    e.ToolTipText = readLines(help, 875, 898)
                Case "lunit", "munit", "tunit", "g", "rho"
                    e.ToolTipTitle = "Header data"
                    e.ToolTipText = readLines(help, 1129, 1180) + Environment.NewLine + readLines(help, 1197, 1230)
                Case "mass", "ixx", "iyy", "izz", "yref", "zref"
                    e.ToolTipTitle = "Header data"
                    e.ToolTipText = readLines(help, 1233, 1300)

                Case Else
                    e.ToolTipTitle = e.HoveredWord
                    e.ToolTipText = "No information available for '" + e.HoveredWord & "'" + Environment.NewLine + "Use the help button (?) at in the menu to look for more information in the AVL documentation"
            End Select
            If e.ToolTipText IsNot Nothing Then
                e.ToolTipText = e.ToolTipText.Trim()
            End If
            currentToolTipText = If(e.ToolTipText, "")
        End If
    End Sub


    Public Function readLines(ByVal path As String, ByVal startline As Integer, Optional endline As Integer = 0) As String

        If endline = 0 Then
            endline = startline
        End If

        Dim all() As String = File.ReadAllLines(path)

        Dim lines As New List(Of String)

        For i = startline To endline
            lines.Add(all(i).Trim())
        Next

        ' Remove leading empty lines
        While lines.Count > 0 AndAlso String.IsNullOrWhiteSpace(lines(0))
            lines.RemoveAt(0)
        End While

        ' Remove trailing empty lines
        While lines.Count > 0 AndAlso String.IsNullOrWhiteSpace(lines(lines.Count - 1))
            lines.RemoveAt(lines.Count - 1)
        End While

        Return String.Join(vbCrLf, lines)
    End Function




    Private Sub txt3_AutoIndentNeeded(sender As Object, e As AutoIndentEventArgs)
        Dim lineTextTrimmed = e.LineText.Trim().ToLower()

        ' If the line starts with any !begin tag, shift next lines to the right
        If lineTextTrimmed.StartsWith("!begingeometry") OrElse
           lineTextTrimmed.StartsWith("!beginsurface") OrElse
           lineTextTrimmed.StartsWith("!beginsection") OrElse
           lineTextTrimmed.StartsWith("!begincontrol") Then
            e.ShiftNextLines = e.TabLength
            Return
        End If

        ' If the line starts with any !end tag, shift current line and next lines to the left
        If lineTextTrimmed.StartsWith("!endgeometry") OrElse
           lineTextTrimmed.StartsWith("!endsurface") OrElse
           lineTextTrimmed.StartsWith("!endsection") OrElse
           lineTextTrimmed.StartsWith("!endcontrol") Then
            e.Shift = -e.TabLength
            e.ShiftNextLines = -e.TabLength
            Return
        End If
    End Sub

    Private Function TrimAll(Text As String, Optional filename As String = "") As String

        If Text.Length = 0 Then Return "" 'zero len string



        Dim result = Text

        While (result.IndexOf("  ") > -1)
            result = result.Replace("  ", " ")
        End While

        'Const toRemove As String = " " & vbTab & vbCr & vbLf 'what to remove

        Dim lines = result.Split(CChar(Environment.NewLine))
        Dim output = ""

        'For Each Str As String In lines
        '    Dim s As Long : s = 1
        '    Dim e As Long : e = Len(Str)
        '    Dim c As String

        '    Do 'how many chars to skip on the left side
        '        c = Mid(Str, s, 1)
        '        If c = "" Or InStr(1, toRemove, c) = 0 Then Exit Do
        '        s = s + 1
        '    Loop
        '    result += IIf(result.Length = 0, "", Environment.NewLine) + Mid(Str, s, (e - s) + 1) 'return remaining text
        'Next

        For Each Str As String In lines
            Dim newline = Str.Trim
            'Debug.WriteLine(newline.Length & "|" & Str.Length & ": " & Str & newline)
            If newline.Length > 0 Then
                If (newline.ToLower.StartsWith("run case")) Then
                    newline = newline.Replace("Run case ", "Run case  ").Replace("Run Case ", "Run case  ").Replace("run case ", "Run case  ")
                    output += newline + Environment.NewLine
                Else
                    output += newline + Environment.NewLine
                End If
            End If
        Next
        'File.WriteAllText($"{Application.StartupPath}\temp.txt", output)

        Return output

    End Function

    ' See the guard comment on AVLTemplateToolStripMenuItem_Click - same hazard.
    Private Sub MassTemplateToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles MassTemplateToolStripMenuItem.Click
        If tc1.SelectedTab IsNot Nothing AndAlso tc1.SelectedTab.Name <> "Mass" Then
            MessageBox.Show("Switch to the Mass tab first - this would replace the " & tc1.SelectedTab.Name & " tab's content otherwise.",
                             "Wrong Tab", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim f = Path.Combine(rootPath, "template_mass.txt")
        Dim val = File.ReadAllText(f)

        ' Replace entire text via selection to preserve undo history
        txt3.Selection.Start = New Place(0, 0)
        txt3.Selection.End = New Place(txt3.Lines(txt3.LinesCount - 1).Length, txt3.LinesCount - 1)
        txt3.InsertText(val)

        txt3.SelectionStart = txt3.Text.Length
        txt3.DoCaretVisible()
        FormatActiveText()
    End Sub





    ' See the guard comment on AVLTemplateToolStripMenuItem_Click - same hazard.
    Private Sub RunTemplateToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RunTemplateToolStripMenuItem.Click
        If tc1.SelectedTab IsNot Nothing AndAlso tc1.SelectedTab.Name <> "Run" Then
            MessageBox.Show("Switch to the Run tab first - this would replace the " & tc1.SelectedTab.Name & " tab's content otherwise.",
                             "Wrong Tab", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim f = Path.Combine(rootPath, "template_run.txt")
        Dim val = File.ReadAllText(f)

        ' Replace entire text via selection to preserve undo history
        txt3.Selection.Start = New Place(0, 0)
        txt3.Selection.End = New Place(txt3.Lines(txt3.LinesCount - 1).Length, txt3.LinesCount - 1)
        txt3.InsertText(val)

        txt3.SelectionStart = txt3.Text.Length
        txt3.DoCaretVisible()
        FormatActiveText()
    End Sub


    Private Sub pxz_MouseDown(sender As Object, e As MouseEventArgs) Handles pxz.MouseDown
        If e.Button = MouseButtons.Left Then
            mouseDownScreenPos = e.Location
            If isDragMode Then
                Dim e1 = (xmax - xmin) / pxy.Width * (e.X - (pxz.Width / 2) - xoffset)
                Dim e2 = (zmax - zmin) / pxz.Height * (e.Y - (pxz.Height / 2) - zoffset)
                Dim hitEpsX = 7.0 * (xmax - xmin) / pxz.Width
                Dim hitEpsZ = 7.0 * (zmax - zmin) / pxz.Height
                For Each n As Node In points
                    If n.IsDraggable AndAlso Math.Abs(n.X - e1) < hitEpsX AndAlso Math.Abs(n.Z - (-e2)) < hitEpsZ Then
                        draggingNode = n
                        isDragging = True
                        dragStartX = n.X
                        dragStartY = n.Y
                        dragStartZ = n.Z
                        pxz.Cursor = Cursors.SizeAll
                        Exit For
                    End If
                Next
                Return
            End If
            pxz.Cursor = Cursors.SizeAll
            xdown = e.X - xoffset
            zdown = e.Y - zoffset
        End If
    End Sub

    Private Sub pxz_MouseWheel(sender As Object, e As MouseEventArgs) Handles pxz.MouseWheel
        If e.Delta < 0 Then
            gridnumber += 1
            drawAxes()
        Else
            If gridnumber > 1 Then
                gridnumber -= 1
                drawAxes()
            End If
        End If

    End Sub

    Private Sub pxz_MouseMove(sender As Object, e As MouseEventArgs) Handles pxz.MouseMove
        Dim e1 As Double = (xmax - xmin) / pxy.Width * (e.X - (pxz.Width / 2) - xoffset)
        Dim e2 As Double = (zmax - zmin) / pxz.Height * (e.Y - (pxz.Height / 2) - zoffset)
        curX = e1
        curZ = -e2
        lblCursor.Text = "Cursor: [X: " + String.Format("{0,5:###.0}", Math.Round(e1, 1)) + ", Z: " + String.Format("{0,5:###.0}", Math.Round(-e2, 1)) + "]"

        If isDragMode Then
            Dim hitEpsX = 7.0 * (xmax - xmin) / pxz.Width
            Dim hitEpsZ = 7.0 * (zmax - zmin) / pxz.Height
            Dim nearDraggable = points.Any(Function(n) n.IsDraggable AndAlso Math.Abs(n.X - e1) < hitEpsX AndAlso Math.Abs(n.Z - (-e2)) < hitEpsZ)
            pxz.Cursor = If(nearDraggable OrElse isDragging, Cursors.SizeAll, Cursors.Hand)
            If isDragging AndAlso draggingNode IsNot Nothing AndAlso e.Button = MouseButtons.Left Then
                draggingNode.X = CSng(e1)
                draggingNode.Z = CSng(-e2)
                draggingNode.Point = New Point3D(e1, draggingNode.Y, -e2)
                drawAxes()
            End If
            Return
        End If

        Dim hoverEpsX = 7.0 * (xmax - xmin) / pxz.Width
        Dim hoverEpsZ = 7.0 * (zmax - zmin) / pxz.Height
        Dim prevHovered As Node = points.FirstOrDefault(Function(p) p.Hovered)
        Dim newHovered As Node = Nothing
        For Each p As Node In points
            If (p.X > e1 - hoverEpsX) And (p.X < e1 + hoverEpsX) And (p.Z > -e2 - hoverEpsZ) And (p.Z < -e2 + hoverEpsZ) Then
                If (p.type = Node.NodeType.Geometry And showSection And showHover) Then
                    newHovered = p : Exit For
                End If
                If (p.type = Node.NodeType.Mass And showMass And showHover) Then
                    newHovered = p
                End If
            End If
        Next

        Dim hoverChanged = (newHovered IsNot prevHovered)
        For Each p As Node In points
            p.Hovered = (p Is newHovered)
        Next
        If newHovered IsNot Nothing Then
            isHovered = True
            tc1.SelectedIndex = If(newHovered.type = Node.NodeType.Mass, 1, 0)
            selectText(newHovered.lineNumber)
        Else
            isHovered = False
        End If

        Dim panning = e.Button = MouseButtons.Left
        If panning Then
            xoffset = e.X - xdown
            zoffset = e.Y - zdown
        End If

        If hoverChanged OrElse panning OrElse showHover Then drawAxes()
    End Sub

    Private Sub pxz_MouseUp(sender As Object, e As MouseEventArgs) Handles pxz.MouseUp
        If isDragMode AndAlso isDragging AndAlso draggingNode IsNot Nothing Then
            isDragging = False
            Dim e1 = (xmax - xmin) / pxy.Width * (e.X - (pxz.Width / 2) - xoffset)
            Dim e2 = (zmax - zmin) / pxz.Height * (e.Y - (pxz.Height / 2) - zoffset)
            CommitNodeDrag(draggingNode, e1, dragStartY, -e2)  ' Y unchanged in XZ view
            draggingNode = Nothing
            pxz.Cursor = Cursors.Hand
            Return
        End If
        pxz.Cursor = Cursors.Default
        If e.Button = MouseButtons.Left Then drawAxes()
        HandlePotentialNodeClick(e)
    End Sub

    Private Sub btnEditor_Click(sender As Object, e As EventArgs) Handles btnEditor.Click
        MsgBox("Shortcut keys you can use for the editor:

Left, Right, Up, Down, Home, End, PageUp, PageDown - moves caret
Shift+(Left, Right, Up, Down, Home, End, PageUp, PageDown) - moves caret with selection
Ctrl+F, Ctrl+H - shows Find and Replace dialogs
F3 - find next
Ctrl+G - shows GoTo dialog
Ctrl+(C, V, X) - standard clipboard operations
Ctrl+A - selects all text
Ctrl+Z, Alt+Backspace, Ctrl+R - Undo/Redo opertions
Tab, Shift+Tab - increase/decrease left indent of selected range
Ctrl+Home, Ctrl+End - go to first/last char of the text
Shift+Ctrl+Home, Shift+Ctrl+End - go to first/last char of the text with selection
Ctrl+Left, Ctrl+Right - go word left/right
Shift+Ctrl+Left, Shift+Ctrl+Right - go word left/right with selection
Ctrl+-, Shift+Ctrl+- - backward/forward navigation
Ctrl+U, Shift+Ctrl+U - converts selected text to upper/lower case
Ctrl+Shift+C - inserts/removes comment prefix in selected lines
Ins - switches between Insert Mode and Overwrite Mode
Ctrl+Backspace, Ctrl+Del - remove word left/right
Alt+Mouse, Alt+Shift+(Up, Down, Right, Left) - enables column selection mode
Alt+Up, Alt+Down - moves selected lines up/down
Shift+Del - removes current line
Ctrl+B, Ctrl+Shift-B, Ctrl+N, Ctrl+Shift+N - add, removes and navigates to bookmark
Esc - closes all opened tooltips, menus and hints
Ctrl+Wheel - zooming
Ctrl+M, Ctrl+E - start/stop macro recording, executing of macro
Alt+F [char] - finds nearest [char]
Ctrl+(Up, Down) - scrolls Up/Down
Ctrl+(NumpadPlus, NumpadMinus, 0) - zoom in, zoom out, no zoom
Ctrl+I - forced AutoIndentChars of current line", vbOKOnly, "Editor Shortcuts")
    End Sub

    Private Sub pyz_Click(sender As Object, e As EventArgs) Handles pyz.Click

    End Sub

    Private Sub pyz_MouseDown(sender As Object, e As MouseEventArgs) Handles pyz.MouseDown
        If e.Button = MouseButtons.Left Then
            mouseDownScreenPos = e.Location
            If isDragMode Then
                Dim e1 = (ymax - ymin) / pyz.Width * (e.X - (pyz.Width / 2) - yoffset)
                Dim e2 = (zmax - zmin) / pyz.Height * (e.Y - (pyz.Height / 2) - zoffset)
                Dim hitEpsY = 7.0 * (ymax - ymin) / pyz.Width
                Dim hitEpsZ = 7.0 * (zmax - zmin) / pyz.Height
                For Each n As Node In points
                    If n.IsDraggable AndAlso Math.Abs(n.Y - e1) < hitEpsY AndAlso Math.Abs(n.Z - (-e2)) < hitEpsZ Then
                        draggingNode = n
                        isDragging = True
                        dragStartX = n.X
                        dragStartY = n.Y
                        dragStartZ = n.Z
                        pyz.Cursor = Cursors.SizeAll
                        Exit For
                    End If
                Next
                Return
            End If
            pyz.Cursor = Cursors.SizeAll
            ydown = e.X - yoffset
            zdown = e.Y - zoffset
        End If
    End Sub

    Private Sub pyz_MouseMove(sender As Object, e As MouseEventArgs) Handles pyz.MouseMove
        Dim e1 As Double = (ymax - ymin) / pyz.Width * (e.X - (pyz.Width / 2) - yoffset)
        Dim e2 As Double = (zmax - zmin) / pyz.Height * (e.Y - (pyz.Height / 2) - zoffset)
        curY = e1
        curZ = -e2
        lblCursor.Text = "Cursor: [Y: " + String.Format("{0,5:###.0}", Math.Round(e1, 1)) + ", Z: " + String.Format("{0,5:###.0}", Math.Round(-e2, 1)) + "]"

        If isDragMode Then
            Dim hitEpsY = 7.0 * (ymax - ymin) / pyz.Width
            Dim hitEpsZ = 7.0 * (zmax - zmin) / pyz.Height
            Dim nearDraggable = points.Any(Function(n) n.IsDraggable AndAlso Math.Abs(n.Y - e1) < hitEpsY AndAlso Math.Abs(n.Z - (-e2)) < hitEpsZ)
            pyz.Cursor = If(nearDraggable OrElse isDragging, Cursors.SizeAll, Cursors.Hand)
            If isDragging AndAlso draggingNode IsNot Nothing AndAlso e.Button = MouseButtons.Left Then
                draggingNode.Y = CSng(e1)
                draggingNode.Z = CSng(-e2)
                draggingNode.Point = New Point3D(draggingNode.X, e1, -e2)
                drawAxes()
            End If
            Return
        End If

        Dim hoverEpsY = 7.0 * (ymax - ymin) / pyz.Width
        Dim hoverEpsZ = 7.0 * (zmax - zmin) / pyz.Height
        Dim prevHovered As Node = points.FirstOrDefault(Function(p) p.Hovered)
        Dim newHovered As Node = Nothing
        For Each p As Node In points
            If (p.Y > e1 - hoverEpsY) And (p.Y < e1 + hoverEpsY) And (p.Z > -e2 - hoverEpsZ) And (p.Z < -e2 + hoverEpsZ) Then
                If (p.type = Node.NodeType.Geometry And showSection And showHover) Then
                    newHovered = p : Exit For
                End If
                If (p.type = Node.NodeType.Mass And showMass And showHover) Then
                    newHovered = p
                End If
            End If
        Next

        Dim hoverChanged = (newHovered IsNot prevHovered)
        For Each p As Node In points
            p.Hovered = (p Is newHovered)
        Next
        If newHovered IsNot Nothing Then
            isHovered = True
            tc1.SelectedIndex = If(newHovered.type = Node.NodeType.Mass, 1, 0)
            selectText(newHovered.lineNumber)
        Else
            isHovered = False
        End If

        Dim panning = e.Button = MouseButtons.Left
        If panning Then
            yoffset = e.X - ydown
            zoffset = e.Y - zdown
        End If

        If hoverChanged OrElse panning OrElse showHover Then drawAxes()
    End Sub

    Private Sub pyz_MouseUp(sender As Object, e As MouseEventArgs) Handles pyz.MouseUp
        If isDragMode AndAlso isDragging AndAlso draggingNode IsNot Nothing Then
            isDragging = False
            Dim e1 = (ymax - ymin) / pyz.Width * (e.X - (pyz.Width / 2) - yoffset)
            Dim e2 = (zmax - zmin) / pyz.Height * (e.Y - (pyz.Height / 2) - zoffset)
            CommitNodeDrag(draggingNode, dragStartX, e1, -e2)  ' X unchanged in YZ view
            draggingNode = Nothing
            pyz.Cursor = Cursors.Hand
            Return
        End If
        pyz.Cursor = Cursors.Default
        If e.Button = MouseButtons.Left Then drawAxes()
        HandlePotentialNodeClick(e)
    End Sub

    Private Sub pyz_MouseWheel(sender As Object, e As MouseEventArgs) Handles pyz.MouseWheel
        If e.Delta < 0 Then
            gridnumber += 1
            drawAxes()
        Else
            If gridnumber > 1 Then
                gridnumber -= 1
                drawAxes()
            End If
        End If
    End Sub

    'https://www.youtube.com/watch?v=ih20l3pJoeU
    Private Function MultiplyMatrixVector(ByVal x As Single, ByVal y As Single, ByVal z As Single, ByVal m(,) As Single) As Point3D
        Dim i As Point3D = New Point3D(x, y, z)
        Dim o As Point3D

        o.X = i.X * m(0, 0) + i.Y * m(1, 0) + i.Z * m(2, 0) + m(3, 0)
        o.Y = i.X * m(0, 1) + i.Y * m(1, 1) + i.Z * m(2, 1) + m(3, 1)
        o.Z = i.X * m(0, 2) + i.Y * m(1, 2) + i.Z * m(2, 2) + m(3, 2)
        Dim w = i.X * m(0, 3) + i.Y * m(1, 3) + i.Z * m(2, 3) + m(3, 3)

        If (w <> 0) Then
            o.X = o.X / w
            o.Y = o.Y / w
            o.Z = o.Z / w
        End If

    End Function

    Private Function getProjectedPoint(ByVal x As Single, ByVal y As Single, ByVal z As Single, ByVal width As Single, ByVal height As Single,
                                       ByVal xmin As Single, ByVal xmax As Single, ByVal offsetx As Single, ByVal offsety As Single,
                                       ByVal offsetz As Single, ByVal m(,) As Single) As Point3D
        Dim i As Point3D = New Point3D((x + offsetx) / (xmax - xmin), (y - offsety) / (xmax - xmin), (z - offsetz) / (xmax - xmin))

        Dim r = MultiplyMatrixVector(CSng(i.X), CSng(i.Y), CSng(i.Z), m)

        Dim o As Point3D = New Point3D(r.X * (xmax - xmin) + offsetx, r.Y * (xmax - xmin) + offsety, r.Z * (xmax - xmin) + offsetz)

        Return o

    End Function

    Function PerspectiveProjection(ByVal point3D As Point3D, ByVal distanceFromViewer As Double, ByVal screenWidth As Double, ByVal screenHeight As Double) As Point
        ' Calculate the projected point using perspective rules
        Dim projectedPoint As New Point
        Dim scaleFactor As Double = distanceFromViewer / point3D.Z
        projectedPoint.X = CInt(point3D.X * scaleFactor + screenWidth / 2)
        projectedPoint.Y = CInt(-point3D.Y * scaleFactor + screenHeight / 2)
        ' Return the projected point
        Return projectedPoint
    End Function

    ' Tick/axis fonts are reused across renders instead of being allocated (and leaked) on every frame.
    ' HeavyRender runs via Task.Run, so cache access is locked in case two renders ever overlap.
    Private Function GetTickFont(size As Single) As Font
        SyncLock _fontCacheLock
            If _cachedTickFont Is Nothing OrElse _cachedTickFontSize <> size Then
                _cachedTickFont?.Dispose()
                _cachedTickFont = New Font(FontFamily.GenericMonospace.Name, size, FontStyle.Regular)
                _cachedTickFontSize = size
            End If
            Return _cachedTickFont
        End SyncLock
    End Function

    Private Function GetAxisFont() As Font
        SyncLock _fontCacheLock
            If _cachedAxisFont Is Nothing Then
                _cachedAxisFont = New Font(FontFamily.GenericSerif.Name, 12, FontStyle.Regular)
            End If
            Return _cachedAxisFont
        End SyncLock
    End Function

    ' Renders the Trefftz Plane plot to match AVL's own native "T" graphics
    ' window (confirmed against a real AVL 3.37 screenshot): black background,
    ' a config/case name + coefficient-grid text block, a legend, and two
    ' stacked regions sharing the Y (spanwise) axis - Cl-perp/Cl/Cl*C/Cref on
    ' a left axis on top, alpha_i alone on a right axis below. Mirrors the
    ' SvgGraphics + capture-vectors pattern used by the geometry views so it
    ' gets the same PNG/SVG/PDF export via ExportView/WriteVectorPdf.
    Private Sub RenderTrefftzPlot(Optional captureVectors As Boolean = False)
        If pTrefftz Is Nothing OrElse pTrefftz.Width <= 0 OrElse pTrefftz.Height <= 0 Then Return

        Dim w As Integer = pTrefftz.Width
        Dim h As Integer = pTrefftz.Height
        Dim BMP As New Bitmap(w, h)
        Dim tickFont As Font = GetTickFont(9)
        Dim svgOut As String = ""
        Dim pdfOut As String = ""

        Using G As New SvgGraphics(w, h, Graphics.FromImage(BMP), captureVectors)
            G.SmoothingMode = SmoothingMode.AntiAlias
            G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit
            G.Clear(Color.Black)

            If _lastTrefftzSurfaces Is Nothing OrElse _lastTrefftzSurfaces.Count = 0 Then
                G.DrawString("Run ""Trefftz Plot"" to compute and plot spanwise loading.", tickFont, Brushes.Gray, New PointF(10, 10))
            Else
                Dim headerBottom = DrawTrefftzHeaderText(G, tickFont, w, h)
                DrawTrefftzCombinedPlot(G, tickFont, w, h, headerBottom)
            End If

            If captureVectors Then
                svgOut = G.GetSvgContent()
                pdfOut = G.GetPdfContentStream()
            End If
        End Using

        Dim oldImage = pTrefftz.Image
        pTrefftz.Image = BMP
        oldImage?.Dispose()

        If captureVectors Then
            trefftzSvg = svgOut
            trefftzPdf = pdfOut
        End If
    End Sub

    ' Draws the config/run-case name, the alpha/beta/M/CL/CY/CD/CDi/CDo/Cl'/Cm/Cn'/e
    ' grid, the "AVL / Trefftz Plane" label, and the curve legend - matching the
    ' text block AVL's own Trefftz Plane window shows. Returns the Y position
    ' where the plot area below it should start.
    Private Function DrawTrefftzHeaderText(G As SvgGraphics, font As Font, w As Integer, h As Integer) As Single
        Dim t = _lastTrefftzTotals
        Dim white = Brushes.White
        Dim lineH As Single = font.Height + 4
        Dim x0 As Single = 8
        Dim y As Single = 6

        Dim titleFont = New Font(font.FontFamily, font.Size + 2, FontStyle.Bold)
        Dim cfgName = If(t IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(t.ConfigName), t.ConfigName, projectName)
        G.DrawString(cfgName, titleFont, white, New PointF(x0, y))
        y += titleFont.Height + 2
        If t IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(t.RunCaseName) Then
            G.DrawString(t.RunCaseName, font, white, New PointF(x0, y))
        End If
        y += lineH + 6

        Dim col1 As Single = x0
        Dim col2 As Single = x0 + 90
        Dim col3 As Single = x0 + 210
        Dim col4 As Single = x0 + 320

        If t IsNot Nothing AndAlso t.Valid Then
            G.DrawString($"α = {t.Alpha:0.0000}", font, white, New PointF(col1, y))
            G.DrawString("pb/2V = 0.0000", font, white, New PointF(col2, y))
            G.DrawString($"CL  = {t.CLtot:0.0000}", font, white, New PointF(col3, y))
            G.DrawString($"Cl' = {t.ClPrimeTot:0.0000}", font, white, New PointF(col4, y))
            y += lineH

            G.DrawString($"β = {t.Beta:0.0000}", font, white, New PointF(col1, y))
            G.DrawString("qc/2V = 0.0000", font, white, New PointF(col2, y))
            G.DrawString($"CY  = {t.CYff:0.0000}", font, white, New PointF(col3, y))
            G.DrawString($"Cm  = {t.Cmtot:0.0000}", font, white, New PointF(col4, y))
            y += lineH

            G.DrawString($"M = {t.Mach:0.000}", font, white, New PointF(col1, y))
            G.DrawString("rb/2V = 0.0000", font, white, New PointF(col2, y))
            G.DrawString($"CD  = {t.CDtot:0.00000}", font, white, New PointF(col3, y))
            G.DrawString($"Cn' = {t.CnPrimeTot:0.0000}", font, white, New PointF(col4, y))
            y += lineH

            G.DrawString($"CDi = {t.CDff:0.00000}", font, white, New PointF(col3, y))
            G.DrawString($"e   = {t.SpanEff:0.0000}", font, white, New PointF(col4, y))
            y += lineH

            G.DrawString($"CDo = {t.CDvis:0.00000}", font, white, New PointF(col3, y))
            y += lineH
        Else
            G.DrawString("(Total-forces coefficients unavailable - see the AVL transcript)", font, Brushes.Gray, New PointF(x0, y))
            y += lineH
        End If

        ' Top-right "AVL / Trefftz Plane" label
        Dim label1 = "AVL"
        Dim label2 = "Trefftz Plane"
        Dim s1 = G.MeasureString(label1, font)
        Dim s2 = G.MeasureString(label2, font)
        G.DrawString(label1, font, white, New PointF(w - s1.Width - 8, 6))
        G.DrawString(label2, font, white, New PointF(w - s2.Width - 8, 6 + lineH))

        ' Legend, matching AVL's curve colors/styles
        Dim legendX1 As Single = w - 130
        Dim legendX2 As Single = w - 100
        Dim sampleW As Single = 22
        Dim legendY As Single = 6 + lineH * 2 + 10

        Dim pClPerp As New Pen(Color.LightGray, 1) With {.DashStyle = DashStyle.Dash}
        Dim pCl As New Pen(Color.OrangeRed, 1) With {.DashStyle = DashStyle.Dash}
        Dim pLoad As New Pen(Color.LimeGreen, 1.5F)
        Dim pAlphaI As New Pen(Color.DeepSkyBlue, 1) With {.DashStyle = DashStyle.Dot}

        G.DrawLine(pClPerp, legendX1, legendY, legendX1 + sampleW, legendY)
        G.DrawString("Cl" & ChrW(&H22A5), font, Brushes.LightGray, New PointF(legendX2, CSng(legendY - font.Height / 2)))
        legendY += lineH

        G.DrawLine(pCl, legendX1, legendY, legendX1 + sampleW, legendY)
        G.DrawString("Cl", font, Brushes.OrangeRed, New PointF(legendX2, CSng(legendY - font.Height / 2)))
        legendY += lineH

        G.DrawLine(pLoad, legendX1, legendY, legendX1 + sampleW, legendY)
        G.DrawString("Cl C/Cref", font, Brushes.LimeGreen, New PointF(legendX2, CSng(legendY - font.Height / 2)))
        legendY += lineH

        G.DrawLine(pAlphaI, legendX1, legendY, legendX1 + sampleW, legendY)
        G.DrawString(ChrW(&H3B1) & "i", font, Brushes.DeepSkyBlue, New PointF(legendX2, CSng(legendY - font.Height / 2)))
        legendY += lineH

        Return Math.Max(y, legendY) + 6
    End Function

    ' Rounds a raw axis step up to a "nice" 1/2/5 x 10^n value so gridlines get
    ' clean round-number labels, matching AVL's own axis tick spacing.
    Private Function NiceStep(range As Double, targetTicks As Integer) As Double
        If range <= 0 Then Return 1.0
        Dim rawStep = range / Math.Max(1, targetTicks)
        Dim mag = Math.Pow(10, Math.Floor(Math.Log10(rawStep)))
        Dim norm = rawStep / mag
        Dim niceNorm As Double
        If norm < 1.5 Then
            niceNorm = 1
        ElseIf norm < 3 Then
            niceNorm = 2
        ElseIf norm < 7 Then
            niceNorm = 5
        Else
            niceNorm = 10
        End If
        Return niceNorm * mag
    End Function

    Private Sub NiceAxisRange(dataMin As Double, dataMax As Double, targetTicks As Integer,
                               ByRef axisMin As Double, ByRef axisMax As Double, ByRef tickStep As Double)
        If dataMin = dataMax Then
            dataMin -= 1 : dataMax += 1
        End If
        tickStep = NiceStep(dataMax - dataMin, targetTicks)
        axisMin = Math.Floor(dataMin / tickStep) * tickStep
        axisMax = Math.Ceiling(dataMax / tickStep) * tickStep
    End Sub

    ' Draws one axis's gridlines + tick labels for the geometry views (pxy/
    ' pxz/pyz) using a "nice" round-number step (1/2/5 x 10^n) instead of
    ' always spacing by exactly 1 world-unit. The old approach pegged tick
    ' spacing directly to gridnumber (the zoom-level control), which is fine
    ' for small models but breaks down for large ones: fitting a 40-unit-wide
    ' aircraft needs gridnumber ~40, which packed 40+ overlapping labels into
    ' the same pixel width AND (via the old baseFontsize/(gridnumber/10)
    ' formula) shrank the font toward sub-1pt sizes - illegible, and the tiny
    ' fractional font size is also why the text looked "unsmooth"/jagged
    ' rather than a ClearType problem (ClearTypeGridFit was already set).
    ' This only changes what gets drawn - gridstep/xmin/xmax/etc (the actual
    ' zoom/world-extent state that node-dragging and mesh overlay rely on)
    ' are untouched.
    '
    ' pixelsPerUnitSigned: +gridstep for horizontal axes (positive = right),
    ' -gridstep for vertical axes (positive = up, since screen Y grows down) -
    ' this sign convention already matches every existing view's min/max
    ' computation (horizontal axes subtract the offset term, vertical axes
    ' add it), so callers just pass through whichever gridstep sign applies.
    Private Sub DrawNiceAxisGrid(G As SvgGraphics, tickFont As Font, isVertical As Boolean,
                                  origin0 As Double, labelOtherAxisPos As Single,
                                  pixelsPerUnitSigned As Double, worldMin As Double, worldMax As Double,
                                  viewW As Integer, viewH As Integer)
        Dim lo = Math.Min(worldMin, worldMax)
        Dim hi = Math.Max(worldMin, worldMax)
        If hi - lo <= 0 Then Return
        Dim tickStep = NiceStep(hi - lo, 8)
        Dim v = Math.Ceiling(lo / tickStep) * tickStep
        Do While v <= hi + tickStep * 0.001
            If Math.Abs(v) > tickStep * 0.001 Then
                Dim px As Single = CSng(origin0 + v * pixelsPerUnitSigned)
                Dim lbl = v.ToString("0.####")
                If isVertical Then
                    G.DrawLine(pGrid, px, 0, px, viewH)
                    G.DrawString(lbl, tickFont, Brushes.Black, New PointF(px, labelOtherAxisPos))
                Else
                    G.DrawLine(pGrid, 0, px, viewW, px)
                    G.DrawString(lbl, tickFont, Brushes.Black, New PointF(labelOtherAxisPos, px))
                End If
            End If
            v += tickStep
        Loop
    End Sub

    ' Draws the two stacked plot regions (Cl-family on a left axis, alpha_i on
    ' a right axis) that share the spanwise Y x-axis, matching AVL's own
    ' Trefftz Plane window layout.
    Private Sub DrawTrefftzCombinedPlot(G As SvgGraphics, tickFont As Font, w As Integer, h As Integer, top As Single)
        Dim margin As Single = 45
        Dim plotX As Single = margin
        Dim plotY As Single = top + 10
        Dim plotW As Single = w - margin - 60
        Dim plotH As Single = h - plotY - 30
        If plotW <= 10 OrElse plotH <= 10 Then Return

        Dim yMin As Double = Double.MaxValue, yMax As Double = Double.MinValue
        Dim leftMin As Double = 0, leftMax As Double = Double.MinValue
        Dim rightMin As Double = 0, rightMax As Double = 0
        Dim any As Boolean = False
        For Each surf As TrefftzSurface In _lastTrefftzSurfaces
            For Each s As TrefftzStrip In surf.Strips
                any = True
                yMin = Math.Min(yMin, s.Yle)
                yMax = Math.Max(yMax, s.Yle)
                leftMax = Math.Max(leftMax, Math.Max(s.ClNorm, Math.Max(s.Cl, s.CCl / _lastTrefftzCref)))
                leftMin = Math.Min(leftMin, Math.Min(s.ClNorm, Math.Min(s.Cl, s.CCl / _lastTrefftzCref)))
                rightMin = Math.Min(rightMin, s.Ai)
                rightMax = Math.Max(rightMax, s.Ai)
            Next
        Next
        If Not any Then Return

        Dim xAxisMin As Double, xAxisMax As Double, xStep As Double
        NiceAxisRange(yMin, yMax, 6, xAxisMin, xAxisMax, xStep)
        Dim leftAxisMin As Double, leftAxisMax As Double, leftStep As Double
        NiceAxisRange(leftMin, leftMax, 5, leftAxisMin, leftAxisMax, leftStep)
        Dim rightAxisMin As Double, rightAxisMax As Double, rightStep As Double
        NiceAxisRange(rightMin, rightMax, 4, rightAxisMin, rightAxisMax, rightStep)

        Dim splitY As Single = plotY + plotH * 0.65F
        Dim upperH As Single = splitY - plotY
        Dim lowerH As Single = (plotY + plotH) - splitY

        Dim whitePen As New Pen(Color.White, 1)
        Dim gridPen As New Pen(Color.FromArgb(90, 90, 90), 1) With {.DashStyle = DashStyle.Dash}

        G.DrawRectangle(whitePen, plotX, plotY, plotW, plotH)

        ' Vertical gridlines (shared Y/spanwise axis), tick labels near the split boundary
        Dim xt = xAxisMin
        Do While xt <= xAxisMax + xStep * 0.001
            Dim px As Single = CSng(plotX + (xt - xAxisMin) / (xAxisMax - xAxisMin) * plotW)
            G.DrawLine(gridPen, px, plotY, px, plotY + plotH)
            Dim lbl = xt.ToString("0.0")
            Dim lblSize = G.MeasureString(lbl, tickFont)
            G.DrawString(lbl, tickFont, Brushes.White, New PointF(px - lblSize.Width / 2, splitY + 2))
            xt += xStep
        Loop
        Dim yLbl = "Y"
        Dim yLblSize = G.MeasureString(yLbl, tickFont)
        G.DrawString(yLbl, tickFont, Brushes.White, New PointF(plotX + plotW - yLblSize.Width, splitY + tickFont.Height + 4))

        ' Horizontal gridlines, upper region (left axis: Cl-perp / Cl / Cl*C/Cref)
        Dim lt = leftAxisMin
        Do While lt <= leftAxisMax + leftStep * 0.001
            Dim py As Single = CSng(splitY - (lt - leftAxisMin) / (leftAxisMax - leftAxisMin) * upperH)
            G.DrawLine(gridPen, plotX, py, plotX + plotW, py)
            Dim lbl = lt.ToString("0.0")
            Dim lblSize = G.MeasureString(lbl, tickFont)
            G.DrawString(lbl, tickFont, Brushes.White, New PointF(plotX - lblSize.Width - 4, py - lblSize.Height / 2))
            lt += leftStep
        Loop

        ' Horizontal gridlines, lower region (right axis: alpha_i)
        Dim rt = rightAxisMin
        Do While rt <= rightAxisMax + rightStep * 0.001
            Dim py As Single = CSng((plotY + plotH) - (rt - rightAxisMin) / (rightAxisMax - rightAxisMin) * lowerH)
            G.DrawLine(gridPen, plotX, py, plotX + plotW, py)
            Dim lbl = rt.ToString("0.00")
            G.DrawString(lbl, tickFont, Brushes.DeepSkyBlue, New PointF(plotX + plotW + 4, CSng(py - tickFont.Height / 2)))
            rt += rightStep
        Loop

        Dim pClPerp As New Pen(Color.LightGray, 1) With {.DashStyle = DashStyle.Dash}
        Dim pCl As New Pen(Color.OrangeRed, 1) With {.DashStyle = DashStyle.Dash}
        Dim pLoad As New Pen(Color.LimeGreen, 1.5F)
        Dim pAlphaI As New Pen(Color.DeepSkyBlue, 1) With {.DashStyle = DashStyle.Dot}

        For Each surf As TrefftzSurface In _lastTrefftzSurfaces
            If surf.Strips.Count = 0 Then Continue For
            Dim ordered As New List(Of TrefftzStrip)(surf.Strips)
            ordered.Sort(Function(a, b) a.Yle.CompareTo(b.Yle))

            Dim ptsClPerp As New List(Of PointF)
            Dim ptsCl As New List(Of PointF)
            Dim ptsLoad As New List(Of PointF)
            Dim ptsAi As New List(Of PointF)
            For Each s As TrefftzStrip In ordered
                Dim px As Single = CSng(plotX + (s.Yle - xAxisMin) / (xAxisMax - xAxisMin) * plotW)
                ptsClPerp.Add(New PointF(px, CSng(splitY - (s.ClNorm - leftAxisMin) / (leftAxisMax - leftAxisMin) * upperH)))
                ptsCl.Add(New PointF(px, CSng(splitY - (s.Cl - leftAxisMin) / (leftAxisMax - leftAxisMin) * upperH)))
                ptsLoad.Add(New PointF(px, CSng(splitY - (s.CCl / _lastTrefftzCref - leftAxisMin) / (leftAxisMax - leftAxisMin) * upperH)))
                ptsAi.Add(New PointF(px, CSng((plotY + plotH) - (s.Ai - rightAxisMin) / (rightAxisMax - rightAxisMin) * lowerH)))
            Next
            If ptsClPerp.Count >= 2 Then
                G.DrawLines(pClPerp, ptsClPerp.ToArray())
                G.DrawLines(pCl, ptsCl.ToArray())
                G.DrawLines(pLoad, ptsLoad.ToArray())
                G.DrawLines(pAlphaI, ptsAi.ToArray())
            End If
        Next
    End Sub

    Private Sub HeavyRender(progress As IProgress(Of Integer), token As CancellationToken)
        ' Fixed, non-shrinking size (still respects the user's baseFontsize +/-
        ' control) - previously this divided by gridnumber, so zooming out far
        ' enough to fit a large aircraft (gridnumber ~40-50) shrank the font to
        ' sub-1pt, which is both illegible and renders jagged regardless of
        ' ClearType settings.
        Dim tickFont As Font = GetTickFont(baseFontsize + 8)
        Dim axisFont As Font = GetAxisFont()
        gridstep = CInt(pxy.Width / gridnumber)
        Dim x0 As Integer = CInt((pxy.Width / 2) + xoffset)
        Dim y0 As Integer = CInt((pxy.Height / 2) + yoffset)
        Dim xcount = 0
        Dim origin = 0
        Dim ycount = 0
        Dim curxx As Single
        Dim curyx As Single
        Dim epsx As Single
        Dim pointsx As List(Of Node) = New List(Of Node)
        Dim radius As Integer = 3
        Dim pointsx2 As List(Of Node)
        'XY plane========================================================================
        Dim BMP As Bitmap = New Bitmap(pxy.Width, pxy.Height)
        Using G As New SvgGraphics(pxy.Width, pxy.Height, Graphics.FromImage(BMP), _captureVectors)
            G.SmoothingMode = SmoothingMode.AntiAlias
            G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit


            'Draw grids
            xcount = CInt(pxy.Width / gridstep)
            xmin = (-xcount / 2) - (xoffset / gridstep)
            xmax = xmin + xcount
            DrawNiceAxisGrid(G, tickFont, True, x0, y0, gridstep, xmin, xmax, pxy.Width, pxy.Height)
            G.DrawLine(pAxis, x0, 0, x0, pxy.Height)
            G.DrawString(0.ToString, tickFont, Brushes.Black, New PointF(x0, y0))


            gridstep = CInt(pxy.Height / gridnumber)
            ycount = CInt(pxy.Height / gridstep)
            ymin = (-ycount / 2) + (yoffset / gridstep)
            ymax = ymin + ycount
            DrawNiceAxisGrid(G, tickFont, False, y0, x0, -gridstep, ymin, ymax, pxy.Width, pxy.Height)
            G.DrawLine(pAxis, 0, y0, pxy.Width, y0)



            origin = CInt(pxy.Width / 20)
            G.DrawLine(pAxis, origin, origin + G.MeasureString("X", axisFont).Height / 2, origin * 2, origin + G.MeasureString("X", axisFont).Height / 2)
            G.DrawString("X", axisFont, Brushes.Black, New PointF(origin * 2, origin))
            G.DrawLine(pAxis, origin, origin + G.MeasureString("X", axisFont).Height / 2, origin, Single.Parse(CStr(origin * 0.1)) + G.MeasureString("X", axisFont).Height / 2)
            G.DrawString("Y", axisFont, Brushes.Black, New PointF(origin - G.MeasureString("Y", axisFont).Width, Single.Parse(CStr(origin * 0.1))))

            'Me.Text = "[" + xmin.ToString("0.00") + "," + xmax.ToString("0.00") + "] , [" + ymin.ToString("0.00") + "," + ymax.ToString("0.00") + "]"

            pointsx = New List(Of Node)
            For Each p As Node In points
                Dim xscale = p.Point.X * (pxy.Width) / (xmax - xmin) + (pxy.Width / 2) + xoffset
                Dim yscale = -p.Point.Y * (pxy.Height) / (ymax - ymin) + (pxy.Height / 2) + yoffset
                If (p.type = Node.NodeType.Geometry) Then
                    pointsx.Add(New Node(xscale, yscale, 0, p.Surface, p.Hovered, p.lineNumber, p.type))
                End If
            Next

            curxx = CSng(curX * (pxy.Width) / (xmax - xmin) + (pxy.Width / 2) + xoffset)
            curyx = CSng(-curY * (pxy.Height) / (ymax - ymin) + (pxy.Height / 2) + yoffset)
            ' Fixed screen-pixel radius (matches the hover hit-test tolerance) rather than
            ' a world-unit eps scaled to pixels, which used to shrink to invisible on large aircraft.
            epsx = 7.0F

            pointsx2 = New List(Of Node)(pointsx)
            If pointsx.Count > 2 Then
                While pointsx.Count > 0
                    Dim name As String = pointsx(0).Surface
                    Dim ps As List(Of PointF) = New List(Of PointF)
                    'Debug.WriteLine($"Points count: {pointsx.Count}, Surface: {pointsx(0).Surface}")
                    Do Until (findname(pointsx, name) = -1)
                        ps.Add(New PointF(CSng(pointsx(findname(pointsx, name)).Point.X), CSng(pointsx(findname(pointsx, name)).Point.Y)))
                        pointsx.RemoveAt(findname(pointsx, name))
                    Loop
                    Dim ps2 As List(Of PointF) = New List(Of PointF)
                    For i = 0 To ps.Count - 1 Step 2
                        ps2.Add(ps(i))
                    Next
                    For i = ps.Count - 1 To 1 Step -2
                        ps2.Add(ps(i))
                    Next
                    'lblNote.Text += Environment.NewLine + str
                    Using myPath As GraphicsPath = New GraphicsPath()
                        myPath.AddLines(ps2.ToArray())

                        If (name.ToLower.Contains("controlflap") And showControl) Then
                            G.DrawPath(pAxis, myPath)
                        ElseIf (name.ToLower.Contains("controlaileron") And showControl) Then
                            G.DrawPath(pAxis, myPath)
                        ElseIf (name.ToLower.Contains("controlrudder") And showControl) Then
                            G.DrawPath(pAxis, myPath)
                        ElseIf (name.ToLower.Contains("controlelevator") And showControl) Then
                            G.DrawPath(pAxis, myPath)
                        ElseIf (Not name.ToLower.Contains("controlflap") And Not name.ToLower.Contains("controlaileron") And Not name.ToLower.Contains("controlrudder") And Not name.ToLower.Contains("controlelevator")) Then
                            G.DrawPath(pAxis, myPath)
                        End If

                        If (name.ToLower.Contains("controlflap") And showControl) Then
                            G.FillPath(bPolyFlap, myPath)
                        ElseIf (name.ToLower.Contains("controlaileron") And showControl) Then
                            G.FillPath(bPolyAilern, myPath)
                        ElseIf (name.ToLower.Contains("controlrudder") And showControl) Then
                            G.FillPath(bPolyRudder, myPath)
                        ElseIf (name.ToLower.Contains("controlelevator") And showControl) Then
                            G.FillPath(bPolyElevator, myPath)
                        Else
                            G.FillPath(bPolySurface, myPath)
                        End If
                    End Using
                End While
            End If

            If (showSection = True) Then
                For Each p As Node In pointsx2
                    Dim name As String = p.Surface
                    If (showControl Or (Not name.ToLower.Contains("controlflap") And Not name.ToLower.Contains("controlaileron") And Not name.ToLower.Contains("controlrudder") And Not name.ToLower.Contains("controlelevator"))) Then
                        If Not p.Hovered Then
                            G.FillEllipse(Brushes.Red, New RectangleF(p.X - radius, p.Y - radius, radius * 2, radius * 2))
                        Else
                            G.FillEllipse(Brushes.Green, New RectangleF(p.X - radius, p.Y - radius, radius * 2, radius * 2))
                        End If
                    End If
                Next
            End If

            If (showMass = True And File.Exists(Application.StartupPath + $"\{projectName}.mass")) Then
                Dim mtotal As Double = 0
                Dim mcount = 0
                For Each p As Node In points
                    If (p.type = Node.NodeType.Mass) Then
                        mtotal += p.mass
                        mcount += 1
                    End If
                Next
                Dim mavg = mtotal / mcount

                For Each p As Node In points
                    If (p.type = Node.NodeType.Mass) Then
                        'Debug.WriteLine($"Found mass at {p.X}, {p.Y}, {p.Z}")
                        Dim xmass As Double = p.X * (pxy.Width) / (xmax - xmin) + (pxy.Width / 2) + xoffset
                        Dim ymass As Double = -p.Y * (pxy.Height) / (ymax - ymin) + (pxy.Height / 2) + yoffset
                        Dim rmass = (p.mass / (mavg * 2)) * radius + radius
                        'Debug.WriteLine($"Found mass {p.mass} at {p.X}, {p.Y}, {p.Z} -> {rmass} with mavg: {mavg}={mtotal}/{mcount}")
                        If Not p.Hovered Then
                            G.FillEllipse(Brushes.Blue, New RectangleF(CSng(xmass - rmass), CSng(ymass - rmass), CSng(rmass * 2), CSng(rmass * 2)))
                        Else
                            G.FillEllipse(Brushes.Green, New RectangleF(CSng(xmass - rmass), CSng(ymass - rmass), CSng(rmass * 2), CSng(rmass * 2)))
                        End If
                    End If
                Next

            End If

            'Me.Text = curY.ToString + ", " + curyx.ToString + " | " + ymin.ToString + "," + ymax.ToString + " | " + yoffset.ToString
            'draw selection region
            If showMesh AndAlso parsedSurfaces IsNot Nothing Then
                Using meshPen As New Pen(Color.FromArgb(140, Color.DarkSlateGray)) With {.DashStyle = Drawing2D.DashStyle.Dot}
                    For Each su As Surface In parsedSurfaces
                        DrawMeshForSurface(G, su, "XY", pxy.Width, pxy.Height, xmin, xmax, ymin, ymax, xoffset, yoffset, meshPen, False)
                        If su.yDuplicate Then
                            DrawMeshForSurface(G, su, "XY", pxy.Width, pxy.Height, xmin, xmax, ymin, ymax, xoffset, yoffset, meshPen, True)
                        End If
                    Next
                End Using
            End If
            If (showHover) Then
                G.DrawRectangle(Pens.Red, curxx - epsx, curyx - epsx, epsx * 2, epsx * 2)
            End If
            pxySvg = G.GetSvgContent()
            pxyPdf = G.GetPdfContentStream()
        End Using

        Dim old As System.Drawing.Image = Nothing
        Dim bmpXY As Bitmap = BMP
        If pxy.InvokeRequired Then
            pxy.Invoke(Sub()
                           old = pxy.Image
                           pxy.Image = bmpXY
                       End Sub)
        Else
            old = pxy.Image
            pxy.Image = bmpXY
        End If

        ' Check for cancellation request
        ' This throws OperationCanceledException if Cancel() was called
        token.ThrowIfCancellationRequested()
        ' Report progress back to the UI
        If progress IsNot Nothing Then
            progress.Report(10)
        End If

        If old IsNot Nothing Then old.Dispose()
        'XZ plane========================================================================
        Dim z0 As Integer = CInt((pxz.Height / 2) + zoffset)
        Dim zcount As Double = 0
        BMP = New Bitmap(pxz.Width, pxz.Height)
        Using G As New SvgGraphics(pxz.Width, pxz.Height, Graphics.FromImage(BMP), _captureVectors)
            G.SmoothingMode = SmoothingMode.AntiAlias
            G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit
            gridstep = CInt(pxz.Width / gridnumber)
            x0 = CInt((pxz.Width / 2) + xoffset)


            xcount = CInt(pxz.Width / gridstep)
            xmin = (-xcount / 2) - (xoffset / gridstep)
            xmax = xmin + xcount
            DrawNiceAxisGrid(G, tickFont, True, x0, z0, gridstep, xmin, xmax, pxz.Width, pxz.Height)
            G.DrawLine(pAxis, x0, 0, x0, pxz.Height)
            G.DrawString(0.ToString, tickFont, Brushes.Black, New PointF(x0, z0))


            gridstep = CInt(pxz.Height / gridnumber)
            zcount = pxz.Height / gridstep
            zmin = (-zcount / 2) + (zoffset / gridstep)
            zmax = zmin + zcount
            DrawNiceAxisGrid(G, tickFont, False, z0, x0, -gridstep, zmin, zmax, pxz.Width, pxz.Height)
            G.DrawLine(pAxis, 0, z0, pxz.Width, z0)

            origin = CInt(pxz.Width / 20)
            G.DrawLine(pAxis, origin, origin + G.MeasureString("X", axisFont).Height / 2, origin * 2, origin + G.MeasureString("X", axisFont).Height / 2)
            G.DrawString("X", axisFont, Brushes.Black, New PointF(origin * 2, origin))
            G.DrawLine(pAxis, origin, origin + G.MeasureString("X", axisFont).Height / 2, origin, Single.Parse(CStr(origin * 0.1)) + G.MeasureString("X", axisFont).Height / 2)
            G.DrawString("Z", axisFont, Brushes.Black, New PointF(origin - G.MeasureString("Z", axisFont).Width, Single.Parse(CStr(origin * 0.1))))


            'Me.Text = "[" + xmin.ToString("0.00") + "," + xmax.ToString("0.00") + "] , [" + ymin.ToString("0.00") + "," + ymax.ToString("0.00") + "]"

            'curyx = CSng(-curY * (pxz.Height) / (ymax - ymin) + (pxz.Height / 2) + yoffset)
            'Dim curzx = CSng(curZ * (pxz.Width) / (zmax - zmin) + (pxz.Width / 2) + zoffset)
            curxx = CSng(curX * (pxz.Width) / (xmax - xmin) + (pxz.Width / 2) + xoffset)
            Dim curzx = CSng(-curZ * (pxz.Height) / (zmax - zmin) + (pxz.Height / 2) + zoffset)
            'Dim epsy = CSng(eps * (pxz.Width) / (ymax - ymin))


            'radius = 5
            pointsx = New List(Of Node)
            For Each p As Node In points
                Dim xscale = p.Point.X * (pxz.Width) / (xmax - xmin) + (pxz.Width / 2) + xoffset
                Dim zscale = -p.Point.Z * (pxz.Height) / (zmax - zmin) + (pxz.Height / 2) + zoffset
                If (p.type = Node.NodeType.Geometry) Then
                    pointsx.Add(New Node(xscale, zscale, 0, p.Surface, p.Hovered, p.lineNumber, p.type))
                End If
            Next
            pointsx2 = New List(Of Node)(pointsx)
            If pointsx.Count > 2 Then
                While pointsx.Count > 0
                    Dim name As String = pointsx(0).Surface
                    Dim ps As List(Of PointF) = New List(Of PointF)
                    Do Until (findname(pointsx, name) = -1)
                        ps.Add(New PointF(CSng(pointsx(findname(pointsx, name)).Point.X), CSng(pointsx(findname(pointsx, name)).Point.Y)))
                        pointsx.RemoveAt(findname(pointsx, name))
                    Loop
                    'MsgBox(ps.Count)
                    'Dim str As String = ""
                    'For Each p As PointF In ps
                    '    str += " | " + "(" + p.X.ToString + "," + p.Y.ToString + ")"
                    'Next
                    Dim ps2 As List(Of PointF) = New List(Of PointF)
                    For i = 0 To ps.Count - 1 Step 2
                        ps2.Add(ps(i))
                    Next
                    For i = ps.Count - 1 To 1 Step -2
                        ps2.Add(ps(i))
                    Next
                    'lblNote.Text += Environment.NewLine + str
                    Using myPath As GraphicsPath = New GraphicsPath()
                        myPath.AddLines(ps2.ToArray())
                        If (name.ToLower.Contains("controlflap") And showControl) Then
                            G.DrawPath(pAxis, myPath)
                        ElseIf (name.ToLower.Contains("controlaileron") And showControl) Then
                            G.DrawPath(pAxis, myPath)
                        ElseIf (name.ToLower.Contains("controlrudder") And showControl) Then
                            G.DrawPath(pAxis, myPath)
                        ElseIf (name.ToLower.Contains("controlelevator") And showControl) Then
                            G.DrawPath(pAxis, myPath)
                        ElseIf (Not name.ToLower.Contains("controlflap") And Not name.ToLower.Contains("controlaileron") And Not name.ToLower.Contains("controlrudder") And Not name.ToLower.Contains("controlelevator")) Then
                            G.DrawPath(pAxis, myPath)
                        End If

                        If (name.ToLower.Contains("controlflap") And showControl) Then
                            G.FillPath(bPolyFlap, myPath)
                        ElseIf (name.ToLower.Contains("controlaileron") And showControl) Then
                            G.FillPath(bPolyAilern, myPath)
                        ElseIf (name.ToLower.Contains("controlrudder") And showControl) Then
                            G.FillPath(bPolyRudder, myPath)
                        ElseIf (name.ToLower.Contains("controlelevator") And showControl) Then
                            G.FillPath(bPolyElevator, myPath)
                        Else
                            G.FillPath(bPolySurface, myPath)
                        End If
                    End Using
                End While
            End If

            If (showSection = True) Then
                For Each p As Node In pointsx2
                    Dim name As String = p.Surface
                    If (showControl Or (Not name.ToLower.Contains("controlflap") And Not name.ToLower.Contains("controlaileron") And Not name.ToLower.Contains("controlrudder") And Not name.ToLower.Contains("controlelevator"))) Then

                        If Not p.Hovered Then
                            G.FillEllipse(Brushes.Red, New RectangleF(p.X - radius, p.Y - radius, radius * 2, radius * 2))
                        Else
                            G.FillEllipse(Brushes.Green, New RectangleF(p.X - radius, p.Y - radius, radius * 2, radius * 2))
                        End If
                    End If
                Next
            End If


            If (showMass = True And File.Exists(Application.StartupPath + $"\{projectName}.mass")) Then
                Dim mtotal As Single = 0
                Dim mcount As Integer = 0
                For Each p As Node In points
                    If (p.type = Node.NodeType.Mass) Then
                        mtotal += p.mass
                        mcount += 1
                    End If
                Next
                Dim mavg = mtotal / mcount

                For Each p As Node In points
                    If (p.type = Node.NodeType.Mass) Then
                        Dim xmass As Double = p.X * (pxz.Width) / (xmax - xmin) + (pxz.Width / 2) + xoffset
                        Dim zmass As Double = -p.Z * (pxz.Height) / (zmax - zmin) + (pxz.Height / 2) + zoffset
                        Dim rmass = (p.mass / (mavg * 2)) * radius + radius
                        G.FillEllipse(Brushes.Blue, New RectangleF(CSng(xmass - rmass), CSng(zmass - rmass), rmass * 2, rmass * 2))
                    End If
                Next

            End If

            'draw selection region
            If showMesh AndAlso parsedSurfaces IsNot Nothing Then
                Using meshPen As New Pen(Color.FromArgb(140, Color.DarkSlateGray)) With {.DashStyle = Drawing2D.DashStyle.Dot}
                    For Each su As Surface In parsedSurfaces
                        DrawMeshForSurface(G, su, "XZ", pxz.Width, pxz.Height, xmin, xmax, zmin, zmax, xoffset, zoffset, meshPen, False)
                        If su.yDuplicate Then
                            DrawMeshForSurface(G, su, "XZ", pxz.Width, pxz.Height, xmin, xmax, zmin, zmax, xoffset, zoffset, meshPen, True)
                        End If
                    Next
                End Using
            End If
            If (showHover) Then
                G.DrawRectangle(Pens.Red, curxx - epsx, curzx - epsx, epsx * 2, epsx * 2)
            End If
            pxzSvg = G.GetSvgContent()
            pxzPdf = G.GetPdfContentStream()
        End Using

        Dim bmpXZ As Bitmap = BMP
        If pxz.InvokeRequired Then
            pxz.Invoke(Sub()
                           old = pxz.Image
                           pxz.Image = bmpXZ
                       End Sub)
        Else
            old = pxz.Image
            pxz.Image = bmpXZ
        End If
        If old IsNot Nothing Then old.Dispose()
        'YZ plane========================================================================
        BMP = New Bitmap(pyz.Width, pyz.Height)
        Using G As New SvgGraphics(pyz.Width, pyz.Height, Graphics.FromImage(BMP), _captureVectors)
            G.SmoothingMode = SmoothingMode.AntiAlias
            G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit
            gridstep = CInt(pyz.Width / gridnumber)
            y0 = CInt((pyz.Width / 2) + yoffset)
            z0 = CInt((pyz.Height / 2) + zoffset)


            ycount = CInt(pyz.Width / gridstep)
            ymin = (-ycount / 2) - (yoffset / gridstep)
            ymax = ymin + ycount
            DrawNiceAxisGrid(G, tickFont, True, y0, z0, gridstep, ymin, ymax, pyz.Width, pyz.Height)
            G.DrawLine(pAxis, y0, 0, y0, pyz.Height)
            G.DrawString(0.ToString, tickFont, Brushes.Black, New PointF(y0, z0))


            gridstep = CInt(pyz.Height / gridnumber)
            zcount = pyz.Height / gridstep
            zmin = (-zcount / 2) + (zoffset / gridstep)
            zmax = zmin + zcount
            DrawNiceAxisGrid(G, tickFont, False, z0, y0, -gridstep, zmin, zmax, pyz.Width, pyz.Height)
            G.DrawLine(pAxis, 0, z0, pyz.Width, z0)

            origin = CInt(pyz.Width / 20)
            G.DrawLine(pAxis, origin * 2, origin + G.MeasureString("Z", axisFont).Height / 2, origin, origin + G.MeasureString("Z", axisFont).Height / 2)
            G.DrawString("Y", axisFont, Brushes.Black, New PointF(origin - G.MeasureString("Y", axisFont).Width, origin))
            G.DrawLine(pAxis, origin * 2, origin + G.MeasureString("Z", axisFont).Height / 2, origin * 2, Single.Parse(CStr(origin * 0.1)) + G.MeasureString("Z", axisFont).Height / 2)
            G.DrawString("Z", axisFont, Brushes.Black, New PointF(origin * 2, Single.Parse(CStr(origin * 0.1))))


            'Me.Text = "[" + xmin.ToString("0.00") + "," + xmax.ToString("0.00") + "] , [" + ymin.ToString("0.00") + "," + ymax.ToString("0.00") + "]"
            curyx = CSng(curY * (pyz.Width) / (ymax - ymin) + (pyz.Width / 2) + yoffset)
            Dim curzx = CSng(-curZ * (pyz.Height) / (zmax - zmin) + (pyz.Height / 2) + zoffset)
            'Dim epsy = CSng(eps * (pxz.Width) / (ymax - ymin))

            'radius = 5
            pointsx = New List(Of Node)
            For Each p As Node In points
                Dim yscale = -p.Point.Y * (pyz.Width) / (ymax - ymin) + (pyz.Width / 2) + yoffset
                Dim zscale = -p.Point.Z * (pyz.Height) / (zmax - zmin) + (pyz.Height / 2) + zoffset
                If (p.type = Node.NodeType.Geometry) Then
                    pointsx.Add(New Node(yscale, zscale, 0, p.Surface, p.Hovered, p.lineNumber, p.type))
                End If
            Next
            pointsx2 = New List(Of Node)(pointsx)
            If pointsx.Count > 2 Then
                While pointsx.Count > 0
                    Dim name As String = pointsx(0).Surface
                    Dim ps As List(Of PointF) = New List(Of PointF)
                    Do Until (findname(pointsx, name) = -1)
                        ps.Add(New PointF(CSng(pointsx(findname(pointsx, name)).Point.X), CSng(pointsx(findname(pointsx, name)).Point.Y)))
                        pointsx.RemoveAt(findname(pointsx, name))
                    Loop
                    'MsgBox(ps.Count)
                    'Dim str As String = ""
                    'For Each p As PointF In ps
                    '    str += " | " + "(" + p.X.ToString + "," + p.Y.ToString + ")"
                    'Next
                    Dim ps2 As List(Of PointF) = New List(Of PointF)
                    For i = 0 To ps.Count - 1 Step 2
                        ps2.Add(ps(i))
                    Next
                    For i = ps.Count - 1 To 1 Step -2
                        ps2.Add(ps(i))
                    Next
                    'lblNote.Text += Environment.NewLine + str
                    Using myPath As GraphicsPath = New GraphicsPath()
                        myPath.AddLines(ps2.ToArray())
                        If (name.ToLower.Contains("controlflap") And showControl) Then
                            G.DrawPath(pAxis, myPath)
                        ElseIf (name.ToLower.Contains("controlaileron") And showControl) Then
                            G.DrawPath(pAxis, myPath)
                        ElseIf (name.ToLower.Contains("controlrudder") And showControl) Then
                            G.DrawPath(pAxis, myPath)
                        ElseIf (name.ToLower.Contains("controlelevator") And showControl) Then
                            G.DrawPath(pAxis, myPath)
                        ElseIf (Not name.ToLower.Contains("controlflap") And Not name.ToLower.Contains("controlaileron") And Not name.ToLower.Contains("controlrudder") And Not name.ToLower.Contains("controlelevator")) Then
                            G.DrawPath(pAxis, myPath)
                        End If

                        If (name.ToLower.Contains("controlflap") And showControl) Then
                            G.FillPath(bPolyFlap, myPath)
                        ElseIf (name.ToLower.Contains("controlaileron") And showControl) Then
                            G.FillPath(bPolyAilern, myPath)
                        ElseIf (name.ToLower.Contains("controlrudder") And showControl) Then
                            G.FillPath(bPolyRudder, myPath)
                        ElseIf (name.ToLower.Contains("controlelevator") And showControl) Then
                            G.FillPath(bPolyElevator, myPath)
                        Else
                            G.FillPath(bPolySurface, myPath)
                        End If
                    End Using
                End While
            End If

            If (showSection = True) Then

                For Each p As Node In pointsx2
                    Dim name As String = p.Surface
                    If (showControl Or (Not name.ToLower.Contains("controlflap") And Not name.ToLower.Contains("controlaileron") And Not name.ToLower.Contains("controlrudder") And Not name.ToLower.Contains("controlelevator"))) Then

                        If Not p.Hovered Then
                            G.FillEllipse(Brushes.Red, New RectangleF(p.X - radius, p.Y - radius, radius * 2, radius * 2))
                        Else
                            G.FillEllipse(Brushes.Green, New RectangleF(p.X - radius, p.Y - radius, radius * 2, radius * 2))
                        End If
                    End If
                Next
            End If


            If (showMass = True And File.Exists(Application.StartupPath + $"\{projectName}.mass")) Then
                Dim mtotal As Single = 0
                Dim mcount As Integer = 0
                For Each p As Node In points
                    If (p.type = Node.NodeType.Mass) Then
                        mtotal += p.mass
                        mcount += 1
                    End If
                Next
                Dim mavg = mtotal / mcount

                For Each p As Node In points
                    If (p.type = Node.NodeType.Mass) Then
                        Dim ymass As Double = -p.Y * (pyz.Width) / (ymax - ymin) + (pyz.Width / 2) + yoffset
                        Dim zmass As Double = -p.Z * (pyz.Height) / (zmax - zmin) + (pyz.Height / 2) + zoffset
                        Dim rmass = (p.mass / (mavg * 2)) * radius + radius
                        G.FillEllipse(Brushes.Blue, New RectangleF(CSng(ymass - rmass), CSng(zmass - rmass), rmass * 2, rmass * 2))
                    End If
                Next

            End If

            'draw selection region
            If showMesh AndAlso parsedSurfaces IsNot Nothing Then
                Using meshPen As New Pen(Color.FromArgb(140, Color.DarkSlateGray)) With {.DashStyle = Drawing2D.DashStyle.Dot}
                    For Each su As Surface In parsedSurfaces
                        DrawMeshForSurface(G, su, "YZ", pyz.Width, pyz.Height, ymin, ymax, zmin, zmax, yoffset, zoffset, meshPen, False)
                        If su.yDuplicate Then
                            DrawMeshForSurface(G, su, "YZ", pyz.Width, pyz.Height, ymin, ymax, zmin, zmax, yoffset, zoffset, meshPen, True)
                        End If
                    Next
                End Using
            End If
            If (showHover) Then
                G.DrawRectangle(Pens.Red, curyx - epsx, curzx - epsx, epsx * 2, epsx * 2)
            End If
            pyzSvg = G.GetSvgContent()
            pyzPdf = G.GetPdfContentStream()
        End Using

        Dim bmpYZ As Bitmap = BMP
        If pyz.InvokeRequired Then
            pyz.Invoke(Sub()
                           old = pyz.Image
                           pyz.Image = bmpYZ
                       End Sub)
        Else
            old = pyz.Image
            pyz.Image = bmpYZ
        End If
        If old IsNot Nothing Then old.Dispose()

        '3D Plane =======================================================================
        If show3D Then
            ' 1. Setup Graphics
            BMP = New Bitmap(p3d.Width, p3d.Height)
            Using G As New SvgGraphics(p3d.Width, p3d.Height, Graphics.FromImage(BMP), _captureVectors)
                G.SmoothingMode = SmoothingMode.AntiAlias
                G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

                ' 2. Calculate Grid Bounds based on Geometry
                Dim gMinX As Single = -10, gMaxX As Single = 10
                Dim gMinY As Single = -10, gMaxY As Single = 10

                Dim geoNodes = points.Where(Function(n) n.type = Node.NodeType.Geometry).ToList()
                If geoNodes.Count > 0 Then
                    gMinX = geoNodes.Min(Function(n) n.X)
                    gMaxX = geoNodes.Max(Function(n) n.X)
                    gMinY = geoNodes.Min(Function(n) n.Y)
                    gMaxY = geoNodes.Max(Function(n) n.Y)
                End If

                ' --- NEW STEP CALCULATION LOGIC ---
                ' Calculate the total span of the geometry
                Dim span As Single = Math.Max(gMaxX - gMinX, gMaxY - gMinY)
                If span = 0 Then span = 10

                ' Calculate total divisions needed (Major grids * Mini grids) to match 2D view logic
                Dim totalDivisions As Integer = gridnumber
                If gridnumbermini > 1 Then totalDivisions = gridnumber * gridnumbermini
                If totalDivisions < 1 Then totalDivisions = 1

                Dim rawStep As Single = span / totalDivisions

                ' Snap rawStep to specific "nice" increments including 0.25
                Dim step3D As Single

                If rawStep <= 0.1 Then
                    step3D = 0.1F
                ElseIf rawStep <= 0.25 Then
                    step3D = 0.25F
                ElseIf rawStep <= 0.5 Then
                    step3D = 0.5F
                ElseIf rawStep <= 1.0 Then
                    step3D = 1.0F
                ElseIf rawStep <= 2.0 Then
                    step3D = 2.0F
                ElseIf rawStep <= 2.5 Then
                    step3D = 2.5F
                ElseIf rawStep <= 5.0 Then
                    step3D = 5.0F
                Else
                    ' For larger steps, round to nearest 5 or 10
                    step3D = CSng(Math.Ceiling(rawStep / 5) * 5)
                End If
                ' ----------------------------------

                ' Expand bounds slightly to align with step
                gMinX -= step3D
                gMaxX += step3D
                gMinY -= step3D
                gMaxY += step3D

                ' Use Decimal for loop to prevent floating point errors (like 0.75000001)
                Dim startX As Decimal = CDec(Math.Floor(gMinX / step3D) * step3D)
                Dim endX As Decimal = CDec(Math.Ceiling(gMaxX / step3D) * step3D)
                Dim startY As Decimal = CDec(Math.Floor(gMinY / step3D) * step3D)
                Dim endY As Decimal = CDec(Math.Ceiling(gMaxY / step3D) * step3D)
                Dim stepDec As Decimal = CDec(step3D)

                ' 3. Draw Grid Lines & Ticks (Z=0 Plane)
                Dim gridPen As Pen = New Pen(Color.LightGray) With {.DashStyle = DashStyle.Dash}

                ' --- Draw Constant X lines ---
                For xVal As Decimal = startX To endX Step stepDec
                    Dim xSng As Single = CSng(xVal)

                    Dim pStart As New Node(xSng, CSng(startY), 0, "", False, 0, Node.NodeType.Geometry)
                    Dim pEnd As New Node(xSng, CSng(endY), 0, "", False, 0, Node.NodeType.Geometry)

                    Dim v1 = pStart.RotateY(viewAlpha).RotateX(viewBeta).RotateZ(viewGamma).Project(p3d.Width, p3d.Height, viewDist, viewFOV)
                    Dim v2 = pEnd.RotateY(viewAlpha).RotateX(viewBeta).RotateZ(viewGamma).Project(p3d.Width, p3d.Height, viewDist, viewFOV)

                    If v1.Visible And v2.Visible Then
                        ' Draw darker line for Zero, lighter for others
                        G.DrawLine(If(Math.Abs(xSng) < 0.001, Pens.Black, gridPen), v1.X, v1.Y, v2.X, v2.Y)

                        ' Text Label
                        Dim pTick As New Node(xSng, 0, 0, "", False, 0, Node.NodeType.Geometry)
                        Dim tTick = pTick.RotateY(viewAlpha).RotateX(viewBeta).RotateZ(viewGamma).Project(p3d.Width, p3d.Height, viewDist, viewFOV)

                        If tTick.Visible Then
                            G.DrawString(xSng.ToString("0.##"), tickFont, Brushes.Black, tTick.X + 2, tTick.Y + 2)
                        End If
                    End If
                Next

                ' --- Draw Constant Y lines ---
                For yVal As Decimal = startY To endY Step stepDec
                    Dim ySng As Single = CSng(yVal)

                    Dim pStart As New Node(CSng(startX), ySng, 0, "", False, 0, Node.NodeType.Geometry)
                    Dim pEnd As New Node(CSng(endX), ySng, 0, "", False, 0, Node.NodeType.Geometry)

                    Dim v1 = pStart.RotateY(viewAlpha).RotateX(viewBeta).RotateZ(viewGamma).Project(p3d.Width, p3d.Height, viewDist, viewFOV)
                    Dim v2 = pEnd.RotateY(viewAlpha).RotateX(viewBeta).RotateZ(viewGamma).Project(p3d.Width, p3d.Height, viewDist, viewFOV)

                    If v1.Visible And v2.Visible Then
                        G.DrawLine(If(Math.Abs(ySng) < 0.001, Pens.Black, gridPen), v1.X, v1.Y, v2.X, v2.Y)

                        ' Text Label
                        Dim pTick As New Node(0, ySng, 0, "", False, 0, Node.NodeType.Geometry)
                        Dim tTick = pTick.RotateY(viewAlpha).RotateX(viewBeta).RotateZ(viewGamma).Project(p3d.Width, p3d.Height, viewDist, viewFOV)

                        If tTick.Visible Then
                            G.DrawString(ySng.ToString("0.##"), tickFont, Brushes.Black, tTick.X + 2, tTick.Y + 2)
                        End If
                    End If
                Next

                ' 4. Draw 3D Axes (Origin Gizmo)
                Dim originp As New Node(0, 0, 0, "", False, 0, Node.NodeType.Geometry)
                Dim axisLen As Single = CSng(step3D * 1.5) ' Scale axis to match grid
                Dim axX As New Node(axisLen, 0, 0, "", False, 0, Node.NodeType.Geometry)
                Dim axY As New Node(0, axisLen, 0, "", False, 0, Node.NodeType.Geometry)
                Dim axZ As New Node(0, 0, axisLen, "", False, 0, Node.NodeType.Geometry)

                Dim tOr = originp.RotateY(viewAlpha).RotateX(viewBeta).RotateZ(viewGamma).Project(p3d.Width, p3d.Height, viewDist, viewFOV)
                Dim tX = axX.RotateY(viewAlpha).RotateX(viewBeta).RotateZ(viewGamma).Project(p3d.Width, p3d.Height, viewDist, viewFOV)
                Dim tY = axY.RotateY(viewAlpha).RotateX(viewBeta).RotateZ(viewGamma).Project(p3d.Width, p3d.Height, viewDist, viewFOV)
                Dim tZ = axZ.RotateY(viewAlpha).RotateX(viewBeta).RotateZ(viewGamma).Project(p3d.Width, p3d.Height, viewDist, viewFOV)

                G.DrawLine(New Pen(Color.Red, 2), tOr.X, tOr.Y, tX.X, tX.Y)
                G.DrawString("X", axisFont, Brushes.Red, tX.X, tX.Y)
                G.DrawLine(New Pen(Color.Green, 2), tOr.X, tOr.Y, tY.X, tY.Y)
                G.DrawString("Y", axisFont, Brushes.Green, tY.X, tY.Y)
                G.DrawLine(New Pen(Color.Blue, 2), tOr.X, tOr.Y, tZ.X, tZ.Y)
                G.DrawString("Z", axisFont, Brushes.Blue, tZ.X, tZ.Y)

                ' 5. Draw Geometry
                Dim points3D As List(Of Node) = New List(Of Node)

                For Each p As Node In points
                    Dim rNode = p.RotateY(viewAlpha).RotateX(viewBeta).RotateZ(viewGamma)
                    points3D.Add(rNode.Project(p3d.Width, p3d.Height, viewDist, viewFOV))
                Next

                Dim points3DGeo As List(Of Node) = points3D.Where(Function(n) n.type = Node.NodeType.Geometry).ToList()

                If points3DGeo.Count > 2 Then
                    While points3DGeo.Count > 0
                        Dim name As String = points3DGeo(0).Surface
                        Dim ps As List(Of PointF) = New List(Of PointF)
                        Dim isPolygonVisible As Boolean = True

                        Do Until (findname(points3DGeo, name) = -1)
                            Dim idx = findname(points3DGeo, name)
                            Dim currentNode = points3DGeo(idx)
                            ps.Add(New PointF(currentNode.X, currentNode.Y))
                            If currentNode.Visible = False Then isPolygonVisible = False
                            points3DGeo.RemoveAt(idx)
                        Loop

                        Dim ps2 As List(Of PointF) = New List(Of PointF)
                        For k = 0 To ps.Count - 1 Step 2
                            ps2.Add(ps(k))
                        Next
                        For k = ps.Count - 1 To 1 Step -2
                            ps2.Add(ps(k))
                        Next

                        If ps2.Count > 1 And isPolygonVisible Then
                            Using myPath As GraphicsPath = New GraphicsPath()
                                myPath.AddLines(ps2.ToArray())

                                Dim fillBrush As Brush = bPolySurface
                                If (name.ToLower.Contains("controlflap")) Then fillBrush = bPolyFlap
                                If (name.ToLower.Contains("controlaileron")) Then fillBrush = bPolyAilern
                                If (name.ToLower.Contains("controlrudder")) Then fillBrush = bPolyRudder
                                If (name.ToLower.Contains("controlelevator")) Then fillBrush = bPolyElevator

                                If showControl Or Not name.ToLower.Contains("control") Then
                                    G.FillPath(fillBrush, myPath)
                                    G.DrawPath(pAxis, myPath)
                                End If
                            End Using
                        End If
                    End While
                End If

                ' Draw mesh in 3D
                If showMesh AndAlso parsedSurfaces IsNot Nothing Then
                    Using meshPen As New Pen(Color.FromArgb(140, Color.DarkSlateGray)) With {.DashStyle = Drawing2D.DashStyle.Dot}
                        For Each su As Surface In parsedSurfaces
                            DrawMeshForSurface(G, su, "3D", p3d.Width, p3d.Height, 0, 0, 0, 0, 0, 0, meshPen, False)
                            If su.yDuplicate Then
                                DrawMeshForSurface(G, su, "3D", p3d.Width, p3d.Height, 0, 0, 0, 0, 0, 0, meshPen, True)
                            End If
                        Next
                    End Using
                End If

                ' 6. Draw Mass Points in 3D
                If (showMass = True And File.Exists(Application.StartupPath + $"\{projectName}.mass")) Then
                    Dim mtotal As Single = 0
                    Dim mcount As Integer = 0

                    ' Calculate Average Mass
                    For Each p As Node In points
                        If (p.type = Node.NodeType.Mass) Then
                            mtotal += p.mass
                            mcount += 1
                        End If
                    Next

                    If mcount > 0 Then
                        Dim mavg = mtotal / mcount

                        For Each p As Node In points
                            If (p.type = Node.NodeType.Mass) Then
                                ' 1. Rotate
                                Dim rNode = p.RotateY(viewAlpha).RotateX(viewBeta).RotateZ(viewGamma)

                                ' 2. Project to Screen Coordinates
                                Dim screenNode = rNode.Project(p3d.Width, p3d.Height, viewDist, viewFOV)

                                ' 3. Draw if visible (not behind camera)
                                If screenNode.Visible Then
                                    ' Calculate Radius size relative to mass average
                                    ' Note: 'radius' variable comes from your variable definitions at start of sub
                                    Dim rmass As Single = CSng((p.mass / (mavg * 2)) * radius + radius)

                                    ' Draw centered on the projected point
                                    G.FillEllipse(Brushes.Blue, New RectangleF(screenNode.X - rmass, screenNode.Y - rmass, rmass * 2, rmass * 2))

                                    ' Optional: Draw a subtle border to make it pop against the aircraft
                                    G.DrawEllipse(Pens.White, New RectangleF(screenNode.X - rmass, screenNode.Y - rmass, rmass * 2, rmass * 2))
                                End If
                            End If
                        Next
                    End If
                End If
                p3dSvg = G.GetSvgContent()
                p3dPdf = G.GetPdfContentStream()
            End Using

            Dim bmp3D As Bitmap = BMP
            If p3d.InvokeRequired Then
                p3d.Invoke(Sub()
                               old = p3d.Image
                               p3d.Image = bmp3D
                           End Sub)
            Else
                old = p3d.Image
                p3d.Image = bmp3D
            End If
            If old IsNot Nothing Then old.Dispose()
        End If

    End Sub

    Private Sub btnZoomin_Click(sender As Object, e As EventArgs) Handles btnZoomin.Click
        If gridnumber > 1 Then
            gridnumber -= 1
            drawAxes()
        End If
    End Sub

    Private Sub btnZoomout_Click(sender As Object, e As EventArgs) Handles btnZoomout.Click
        gridnumber += 1
        drawAxes()
    End Sub

    Private Sub btnBasefontminus_Click(sender As Object, e As EventArgs) Handles btnBasefontminus.Click
        If baseFontsize > 1 Then
            baseFontsize -= 1
            drawAxes()
        End If
    End Sub

    Private Sub btnBasefontplus_Click(sender As Object, e As EventArgs) Handles btnBasefontplus.Click
        If baseFontsize < 20 Then
            baseFontsize += 1
            drawAxes()
        End If
    End Sub

    Private Sub frmGeometry_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        drawAxes()
        Me.Focus()
    End Sub
    ' txt3 is shared across the Geometry/Mass/Run tabs and only ever holds
    ' whichever one is currently selected (tc1_SelectedIndexChanged swaps its
    ' content on tab change) - several analysis-tab buttons call SaveAVL()
    ' unconditionally before running AVL, so without this guard, clicking e.g.
    ' "Run Trefftz Plot" while the Mass or Run tab happens to be active would
    ' silently overwrite the .avl file with Mass/Run text. When the wrong tab
    ' is active there's nothing to save anyway (txt3 isn't holding unsaved
    ' Geometry edits), so this is a safe no-op, not a suppressed error.
    Private Sub SaveAVL()
        If tc1.SelectedTab IsNot Nothing AndAlso tc1.SelectedTab.Name <> "Geometry" Then Return
        Try
            Dim f = Path.Combine(Application.StartupPath, $"{projectName}.avl")
            File.WriteAllText(f, TrimAll(txt3.Text))
        Catch er As Exception
            MsgBox("Error: " + er.Message)
            Try
                frmMain.p.Kill()
                frmMain.loadConsole()
            Catch
            End Try
        End Try
    End Sub

    Private Sub LoadAVL()
        Try
            Dim f = Path.Combine(Application.StartupPath, $"{projectName}.avl")
            If (File.Exists(f)) Then
                txt3.Text = File.ReadAllText(f)
                FormatActiveText()
            Else
                txt3.Text = String.Empty
            End If
            isDirty = False
            UpdateDirtyWarning()
        Catch ex As Exception
            MsgBox("Error: " + ex.Message)
        End Try

    End Sub

    ' See the guard comment on SaveAVL() - same shared-txt3 hazard applies here.
    Private Sub SaveMass()
        If tc1.SelectedTab IsNot Nothing AndAlso tc1.SelectedTab.Name <> "Mass" Then Return
        Try
            Dim f = Path.Combine(Application.StartupPath, $"{projectName}.mass")
            File.WriteAllText(f, TrimAll(txt3.Text))
        Catch er As Exception
            MsgBox("Error: " + er.Message)
            Try
                frmMain.p.Kill()
                frmMain.loadConsole()
            Catch
            End Try
        End Try
    End Sub

    Private Sub LoadMass()
        Try
            Dim f = Path.Combine(Application.StartupPath, $"{projectName}.mass")
            If (File.Exists(f)) Then
                txt3.Text = File.ReadAllText(f)
                FormatActiveText()
            Else
                txt3.Text = String.Empty
            End If
            isDirty = False
            UpdateDirtyWarning()
        Catch ex As Exception
            MsgBox("Error: " + ex.Message)
        End Try

    End Sub

    ' See the guard comment on SaveAVL() - same shared-txt3 hazard applies here.
    Private Sub SaveRun()
        If tc1.SelectedTab IsNot Nothing AndAlso tc1.SelectedTab.Name <> "Run" Then Return
        Dim f = Path.Combine(Application.StartupPath, $"{projectName}.run")
        File.WriteAllText(f, TrimAll(txt3.Text, f))
    End Sub

    Private Sub LoadRun()
        Try
            Dim f = Path.Combine(Application.StartupPath, $"{projectName}.run")
            If (File.Exists(f)) Then
                txt3.Text = File.ReadAllText(f)
                FormatActiveText()
            Else
                txt3.Text = String.Empty
            End If
            isDirty = False
            UpdateDirtyWarning()
        Catch ex As Exception
            MsgBox("Error: " + ex.Message)
        End Try
    End Sub

    ' Instead of sending AVL's "t" Trefftz command (which opens AVL's own native
    ' graphics window and captures nothing), this runs the case and asks AVL to
    ' dump strip forces ("fs") to a text file, then parses and plots that data
    ' in-house so it gets the same PNG/SVG/PDF export as the geometry views.
    Private Async Sub TrefftzPlaneToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles btnTrefftz.Click
        If String.IsNullOrEmpty(projectName) Then
            MessageBox.Show("Please enter a project name in the text box at the top before running analysis.", "Missing Project Name", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtName.Focus()
            Return
        End If
        If frmMain.p Is Nothing OrElse frmMain.p.HasExited Then Return

        btnRunTrefftz.Enabled = False
        Try
            Dim surfaces = Await RunTrefftzAnalysisAsync()
            _lastTrefftzSurfaces = surfaces
            _lastTrefftzCref = GetCrefFromAvlText(txt3.Text)
            _lastTrefftzTotals = ParseTotalForces(_lastTrefftzLog)
            RenderTrefftzPlot()

            Dim totalStrips As Integer = 0
            If surfaces IsNot Nothing Then
                For Each s In surfaces
                    totalStrips += s.Strips.Count
                Next
            End If
            If surfaces Is Nothing OrElse totalStrips = 0 Then
                Dim debugPath = Path.Combine(Application.StartupPath, $"{projectName}_trefftz_debug.txt")
                Try
                    Dim content = "=== AVL console transcript ===" & vbCrLf & If(_lastTrefftzLog, "") &
                                  vbCrLf & vbCrLf & "=== Raw FS output file AVL wrote (as parsed) ===" & vbCrLf & If(_lastTrefftzRawFile, "(no file was produced)")
                    File.WriteAllText(debugPath, content)
                    Process.Start(New ProcessStartInfo("notepad.exe", $"""{debugPath}""") With {.UseShellExecute = True})
                Catch
                End Try
                Dim reason = If(surfaces Is Nothing OrElse surfaces.Count = 0,
                                 "AVL never reported any ""Surface #"" section - the load/oper/x/fs commands likely failed.",
                                 $"AVL reported {surfaces.Count} surface(s) but no data rows were recognized under any of them - the strip-forces table format didn't match what this app expects.")
                MessageBox.Show("AVL did not return any strip-force data." & vbCrLf & vbCrLf & reason & vbCrLf & vbCrLf &
                                 "Full details written to:" & vbCrLf & debugPath,
                                 "Trefftz Plot", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Else
                tc1.SelectedTab = Trefftz
            End If
        Finally
            btnRunTrefftz.Enabled = True
        End Try
    End Sub

    ' Runs the current case and asks AVL to write strip forces to a temp file
    ' (OPER -> X -> FS -> <file>), then waits for that file to appear and finish
    ' writing before parsing it. There's no stdout parsing anywhere in this app,
    ' so file polling is how we know AVL finished the write.
    Private Async Function RunTrefftzAnalysisAsync() As Task(Of List(Of TrefftzSurface))
        Dim f = Path.Combine(Application.StartupPath, $"{projectName}.avl")
        ' AVL (legacy Fortran, userio.f) silently truncates filenames longer than
        ' its fixed internal buffer. Application.StartupPath (e.g.
        ' ...\bin\Debug\net10.0-windows\, or deeper for a published build) plus a
        ' project-name-based filename can exceed that limit, so the truncated
        ' path collides with an existing file/directory and AVL reports "File
        ' exists" and writes nothing - confirmed against a real AVL 3.37 build.
        ' The OS temp folder + a random short name keeps this comfortably short
        ' and unique, so the "File exists" prompt can never trigger at all.
        Dim outFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

        ' Make sure AVL's "load" sees the current editor content. Autosave is a
        ' user toggle (btnAutosave) and debounced by 200ms even when on, so the
        ' .avl file on disk can be stale or (for a brand-new project) not exist
        ' yet - without this, "load" fails silently in AVL, every command after
        ' it gets rejected, and "fs" never runs, which looks like "no data".
        SaveAVL()
        _lastTrefftzLog = ""
        If Not File.Exists(f) Then
            _lastTrefftzLog = "(Could not save the geometry to " & f & " - check that the project name/path is valid and writable.)"
            Return New List(Of TrefftzSurface)
        End If

        Dim logLengthBefore = frmMain.txtLog.Text.Length

        With frmMain
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine("load " + f)
            .p.StandardInput.WriteLine("oper")
            .p.StandardInput.WriteLine("x")
            .p.StandardInput.WriteLine("fs")
            .p.StandardInput.WriteLine(outFile)
            .p.StandardInput.WriteLine()
            .p.StandardInput.Flush()
        End With

        Dim deadline = DateTime.Now.AddSeconds(10)
        Dim lastLen As Long = -1
        Do
            Await Task.Delay(100)
            Try
                If File.Exists(outFile) Then
                    Dim len = New FileInfo(outFile).Length
                    If len > 0 AndAlso len = lastLen Then Exit Do
                    lastLen = len
                End If
            Catch
                ' file may still be locked by AVL mid-write; keep polling
            End Try
        Loop While DateTime.Now < deadline

        ' Let frmMain's 75ms batched log-flush timer catch up before reading it.
        Await Task.Delay(150)
        Try
            Dim logText = frmMain.txtLog.Text
            If logLengthBefore <= logText.Length Then
                _lastTrefftzLog = logText.Substring(logLengthBefore)
            End If
        Catch
        End Try

        If Not File.Exists(outFile) Then Return New List(Of TrefftzSurface)

        Try
            _lastTrefftzRawFile = File.ReadAllText(outFile)
        Catch
        End Try

        Try
            Return ParseTrefftzStripFile(outFile)
        Catch
            Return New List(Of TrefftzSurface)
        Finally
            Try
                File.Delete(outFile)
            Catch
            End Try
        End Try
    End Function

    ' Parses AVL's "FS" (strip forces) output. Format confirmed against a real
    ' AVL 3.37 run:
    '   Surface #  1     Wing
    '      j      Yle    Chord     Area     c cl      ai      cl_norm  cl       cd       cdv    cm_c/4    cm_LE  C.P.x/c
    '       1   0.0214   1.0000   0.0852   0.4802   0.0193   0.4816   0.4816   0.0046   0.0000   0.0006  -0.1194    0.249
    ' Data rows are detected by shape (leading integer strip index + 12 or 13
    ' numeric columns) rather than by parsing the header, since "c cl" is a
    ' two-word column name that would otherwise misalign a token-count-based
    ' header parse. C.P.x/c (the 13th column) is cm/cl, which AVL omits
    ' entirely - not zero, not NaN, just absent - for a zero-lift strip
    ' (cl = 0, a 0/0 division), so a 12-token row is equally valid and just
    ' means C.P.x/c is undefined for that strip. Confirmed against a real
    ' zero-lift AVL run where every row was missing that trailing column.
    Private Function ParseTrefftzStripFile(path As String) As List(Of TrefftzSurface)
        Dim surfaces As New List(Of TrefftzSurface)
        Dim current As TrefftzSurface = Nothing

        For Each raw As String In File.ReadAllLines(path)
            Dim line = raw.Trim()
            If line.StartsWith("Surface #") Then
                current = New TrefftzSurface With {.Name = line}
                surfaces.Add(current)
                Continue For
            End If
            If current Is Nothing Then Continue For

            Dim tokens = line.Split({" "c, Chr(9)}, StringSplitOptions.RemoveEmptyEntries)
            If tokens.Length <> 12 AndAlso tokens.Length <> 13 Then Continue For

            Dim idx As Integer
            If Not Integer.TryParse(tokens(0), idx) Then Continue For

            Dim colCount = tokens.Length - 1
            Dim v(colCount - 1) As Double
            Dim ok As Boolean = True
            For i = 1 To colCount
                If Not Double.TryParse(tokens(i), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, v(i - 1)) Then
                    ok = False
                    Exit For
                End If
            Next
            If Not ok Then Continue For

            current.Strips.Add(New TrefftzStrip With {
                .Yle = v(0), .Chord = v(1), .Area = v(2), .CCl = v(3), .Ai = v(4),
                .ClNorm = v(5), .Cl = v(6), .Cd = v(7), .Cdv = v(8),
                .CmC4 = v(9), .CmLE = v(10), .Cpxc = If(colCount >= 12, v(11), 0.0)
            })
        Next

        Return surfaces
    End Function

    ' Reads Cref from the .avl geometry text. AVL's file format fixes the order
    ' of the first few non-comment lines: name, Mach, IYsym/IZsym/Zsym,
    ' Sref/Cref/Bref, Xref/Yref/Zref - so Cref is always the 2nd token of the
    ' 4th data line.
    Private Function GetCrefFromAvlText(avlText As String) As Double
        Dim dataLines As New List(Of String)
        For Each raw In avlText.Replace(vbCr, "").Split(vbLf)
            Dim t = raw.Trim()
            If t.Length = 0 Then Continue For
            If t.StartsWith("#") OrElse t.StartsWith("!") Then Continue For
            dataLines.Add(t)
            If dataLines.Count = 4 Then Exit For
        Next

        If dataLines.Count < 4 Then Return 1.0

        Dim toks = dataLines(3).Split({" "c, Chr(9)}, StringSplitOptions.RemoveEmptyEntries)
        Dim cref As Double = 1.0
        If toks.Length >= 2 Then
            Double.TryParse(toks(1), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, cref)
        End If
        Return If(cref <> 0, cref, 1.0)
    End Function

    ' Parses the "Vortex Lattice Output -- Total Forces" block that AVL prints
    ' to the console right after "X" (execute) - this is the same numeric block
    ' AVL's own native Trefftz Plane window shows as text (CL, CD, CDi, CDo, e,
    ' etc.), confirmed against a real AVL 3.37 Trefftz Plane screenshot:
    '   CLtot =   0.42112            -> "CL"
    '   CDtot =   0.00588            -> "CD"
    '   CDvis =   0.00000            -> "CDo"
    '   CLff  =   0.42163  CDff = 0.0058967 | Trefftz  -> CDff is "CDi"
    '   CYff  =   0.00000     e =    0.9596 | Plane
    '   Cl'tot, Cmtot, Cn'tot                -> "Cl'", "Cm", "Cn'"
    Private Function ParseTotalForces(logText As String) As TrefftzTotals
        Dim t As New TrefftzTotals()
        If String.IsNullOrEmpty(logText) Then Return t

        Dim numPattern = "(-?\d+\.?\d*(?:[eE][-+]?\d+)?)"
        Dim ExtractNum = Function(pattern As String) As Double?
                             Dim m = Regex.Match(logText, pattern)
                             If m.Success Then
                                 Dim v As Double
                                 If Double.TryParse(m.Groups(1).Value, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, v) Then
                                     Return v
                                 End If
                             End If
                             Return Nothing
                         End Function

        Dim cfgMatch = Regex.Match(logText, "Configuration:\s*(.+)")
        If cfgMatch.Success Then t.ConfigName = cfgMatch.Groups(1).Value.Trim()

        Dim caseMatch = Regex.Match(logText, "Run case:\s*(.+)")
        If caseMatch.Success Then t.RunCaseName = caseMatch.Groups(1).Value.Trim()

        Dim cltot = ExtractNum("CLtot\s*=\s*" & numPattern)
        If Not cltot.HasValue Then Return t

        t.CLtot = cltot.Value
        t.Alpha = If(ExtractNum("Alpha\s*=\s*" & numPattern), 0.0)
        t.Beta = If(ExtractNum("Beta\s*=\s*" & numPattern), 0.0)
        t.Mach = If(ExtractNum("Mach\s*=\s*" & numPattern), 0.0)
        t.CDtot = If(ExtractNum("CDtot\s*=\s*" & numPattern), 0.0)
        t.CDvis = If(ExtractNum("CDvis\s*=\s*" & numPattern), 0.0)
        t.CDff = If(ExtractNum("CDff\s*=\s*" & numPattern), 0.0)
        t.CYff = If(ExtractNum("CYff\s*=\s*" & numPattern), 0.0)
        t.SpanEff = If(ExtractNum("\be\s*=\s*" & numPattern), 0.0)
        t.ClPrimeTot = If(ExtractNum("Cl'tot\s*=\s*" & numPattern), 0.0)
        t.Cmtot = If(ExtractNum("Cmtot\s*=\s*" & numPattern), 0.0)
        t.CnPrimeTot = If(ExtractNum("Cn'tot\s*=\s*" & numPattern), 0.0)
        t.Valid = True

        Return t
    End Function

    ' Runs the current case and asks AVL to write spanwise shear/bending-moment
    ' data to a temp file (OPER -> X -> VM -> <file>). Same pattern as the
    ' Trefftz FS flow: save geometry first, unique short temp path (AVL
    ' truncates long paths - see RunTrefftzAnalysisAsync), poll for the file.
    Private Async Sub RunLoadsAnalysis_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(projectName) Then
            MessageBox.Show("Please enter a project name in the text box at the top before running analysis.", "Missing Project Name", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtName.Focus()
            Return
        End If
        If frmMain.p Is Nothing OrElse frmMain.p.HasExited Then Return

        btnLoads.Enabled = False
        Try
            Dim surfaces = Await RunLoadsAnalysisAsync()
            _lastVmSurfaces = surfaces
            RenderLoadsPlot()

            Dim totalPoints As Integer = 0
            If surfaces IsNot Nothing Then
                For Each s In surfaces
                    totalPoints += s.Points.Count
                Next
            End If

            If surfaces Is Nothing OrElse totalPoints = 0 Then
                Dim debugPath = Path.Combine(Application.StartupPath, $"{projectName}_loads_debug.txt")
                Try
                    File.WriteAllText(debugPath, If(_lastVmLog, ""))
                    Process.Start(New ProcessStartInfo("notepad.exe", $"""{debugPath}""") With {.UseShellExecute = True})
                Catch
                End Try
                MessageBox.Show("AVL did not return any shear/moment data." & vbCrLf & vbCrLf &
                                 "Full details written to:" & vbCrLf & debugPath,
                                 "Spanwise Loads", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Else
                tc1.SelectedTab = Loads
            End If
        Finally
            btnLoads.Enabled = True
        End Try
    End Sub

    Private Async Function RunLoadsAnalysisAsync() As Task(Of List(Of VmSurface))
        Dim f = Path.Combine(Application.StartupPath, $"{projectName}.avl")
        Dim outFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

        SaveAVL()
        _lastVmLog = ""
        If Not File.Exists(f) Then
            _lastVmLog = "(Could not save the geometry to " & f & " - check that the project name/path is valid and writable.)"
            Return New List(Of VmSurface)
        End If

        Dim logLengthBefore = frmMain.txtLog.Text.Length

        With frmMain
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine("load " + f)
            .p.StandardInput.WriteLine("oper")
            .p.StandardInput.WriteLine("x")
            .p.StandardInput.WriteLine("vm")
            .p.StandardInput.WriteLine(outFile)
            .p.StandardInput.WriteLine()
            .p.StandardInput.Flush()
        End With

        Dim deadline = DateTime.Now.AddSeconds(10)
        Dim lastLen As Long = -1
        Do
            Await Task.Delay(100)
            Try
                If File.Exists(outFile) Then
                    Dim len = New FileInfo(outFile).Length
                    If len > 0 AndAlso len = lastLen Then Exit Do
                    lastLen = len
                End If
            Catch
            End Try
        Loop While DateTime.Now < deadline

        Await Task.Delay(150)
        Try
            Dim logText = frmMain.txtLog.Text
            If logLengthBefore <= logText.Length Then
                _lastVmLog = logText.Substring(logLengthBefore)
            End If
        Catch
        End Try

        If Not File.Exists(outFile) Then Return New List(Of VmSurface)

        Try
            Return ParseVmFile(outFile)
        Catch
            Return New List(Of VmSurface)
        Finally
            Try
                File.Delete(outFile)
            Catch
            End Try
        End Try
    End Function

    ' Parses AVL's "VM" (strip shear/moment) output. Format confirmed against a
    ' real AVL 3.37 run:
    '   Surface:   1
    '   [Wing]
    '      2Ymin/Bref =    0.00000000
    '      2Ymax/Bref =    1.00000000
    '    2Y/Bref      Vz/(q*Sref)      Mx/(q*Bref*Sref)
    '     0.0000  0.210609         0.478813E-01
    ' Unlike FS, rows here have no leading integer index (2Y/Bref is a float,
    ' possibly negative) and values can be in Fortran "E-01" scientific form,
    ' which Double.TryParse with NumberStyles.Float already handles natively.
    Private Function ParseVmFile(path As String) As List(Of VmSurface)
        Dim surfaces As New List(Of VmSurface)
        Dim current As VmSurface = Nothing
        Dim awaitingName As Boolean = False
        Dim inDataSection As Boolean = False

        For Each raw As String In File.ReadAllLines(path)
            Dim line = raw.Trim()
            If line.Length = 0 Then Continue For

            If line.StartsWith("Surface:") Then
                current = New VmSurface()
                surfaces.Add(current)
                awaitingName = True
                inDataSection = False
                Continue For
            End If

            If awaitingName Then
                current.Name = line
                awaitingName = False
                Continue For
            End If

            If line.StartsWith("2Y/Bref") Then
                inDataSection = True
                Continue For
            End If

            If Not inDataSection OrElse current Is Nothing Then Continue For

            Dim tokens = line.Split({" "c, Chr(9)}, StringSplitOptions.RemoveEmptyEntries)
            If tokens.Length <> 3 Then Continue For
            Dim v(2) As Double
            Dim ok As Boolean = True
            For i = 0 To 2
                If Not Double.TryParse(tokens(i), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, v(i)) Then
                    ok = False
                    Exit For
                End If
            Next
            If ok Then current.Points.Add(New VmStrip With {.Y2Bref = v(0), .Vz = v(1), .Mx = v(2)})
        Next

        Return surfaces
    End Function

    ' Renders spanwise shear (Vz) and bending moment (Mx) vs 2Y/Bref as two
    ' stacked plots, styled consistently with the Trefftz plot (black bg).
    Private Sub RenderLoadsPlot(Optional captureVectors As Boolean = False)
        If pLoads Is Nothing OrElse pLoads.Width <= 0 OrElse pLoads.Height <= 0 Then Return

        Dim w As Integer = pLoads.Width
        Dim h As Integer = pLoads.Height
        Dim BMP As New Bitmap(w, h)
        Dim tickFont As Font = GetTickFont(9)
        Dim axisFont As Font = GetAxisFont()
        Dim svgOut As String = ""
        Dim pdfOut As String = ""

        Using G As New SvgGraphics(w, h, Graphics.FromImage(BMP), captureVectors)
            G.SmoothingMode = SmoothingMode.AntiAlias
            G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit
            G.Clear(Color.Black)

            If _lastVmSurfaces Is Nothing OrElse _lastVmSurfaces.Count = 0 Then
                G.DrawString("Run ""Shear & Bending Moment"" to compute and plot spanwise loads.", tickFont, Brushes.Gray, New PointF(10, 10))
            Else
                Dim margin As Single = 50
                Dim plotH As Single = (h - margin * 3) / 2
                Dim plotW As Single = w - margin * 2
                DrawVmSubplot(G, tickFont, axisFont, margin, margin, plotW, plotH,
                              "Vz / (q*Sref)  (spanwise shear)", Color.OrangeRed,
                              Function(p As VmStrip) p.Vz)
                DrawVmSubplot(G, tickFont, axisFont, margin, margin * 2 + plotH, plotW, plotH,
                              "Mx / (q*Bref*Sref)  (bending moment)", Color.LimeGreen,
                              Function(p As VmStrip) p.Mx)
            End If

            If captureVectors Then
                svgOut = G.GetSvgContent()
                pdfOut = G.GetPdfContentStream()
            End If
        End Using

        Dim oldImage = pLoads.Image
        pLoads.Image = BMP
        oldImage?.Dispose()

        If captureVectors Then
            loadsSvg = svgOut
            loadsPdf = pdfOut
        End If
    End Sub

    ' Draws one 2Y/Bref-vs-value line plot (one polyline per AVL surface) into the given sub-rectangle.
    Private Sub DrawVmSubplot(G As SvgGraphics, tickFont As Font, axisFont As Font,
                               x As Single, y As Single, w As Single, h As Single,
                               title As String, curveColor As Color, valueOf As Func(Of VmStrip, Double))
        Dim xMin As Double = Double.MaxValue, xMax As Double = Double.MinValue
        Dim vMin As Double = Double.MaxValue, vMax As Double = Double.MinValue
        Dim any As Boolean = False
        For Each surf As VmSurface In _lastVmSurfaces
            For Each p As VmStrip In surf.Points
                any = True
                xMin = Math.Min(xMin, p.Y2Bref)
                xMax = Math.Max(xMax, p.Y2Bref)
                Dim v = valueOf(p)
                vMin = Math.Min(vMin, v)
                vMax = Math.Max(vMax, v)
            Next
        Next
        If Not any Then Return
        vMin = Math.Min(0, vMin)
        vMax = Math.Max(0, vMax)
        If xMin = xMax Then xMin -= 1 : xMax += 1
        If vMin = vMax Then vMin -= 1 : vMax += 1
        Dim vPad As Double = (vMax - vMin) * 0.1
        vMin -= vPad
        vMax += vPad

        Dim whitePen As New Pen(Color.White, 1)
        Dim gridPen As New Pen(Color.FromArgb(90, 90, 90), 1) With {.DashStyle = DashStyle.Dash}

        G.DrawRectangle(whitePen, x, y, w, h)
        G.DrawString(title, axisFont, Brushes.White, New PointF(x, y - axisFont.Height - 2))

        If vMin < 0 AndAlso vMax > 0 Then
            Dim zeroY As Single = CSng(y + h - (0 - vMin) / (vMax - vMin) * h)
            G.DrawLine(gridPen, x, zeroY, x + w, zeroY)
        End If

        Dim pen As New Pen(curveColor, 1.5F)
        For Each surf As VmSurface In _lastVmSurfaces
            If surf.Points.Count = 0 Then Continue For
            Dim ordered As New List(Of VmStrip)(surf.Points)
            ordered.Sort(Function(a, b) a.Y2Bref.CompareTo(b.Y2Bref))
            Dim pts As New List(Of PointF)
            For Each p As VmStrip In ordered
                Dim px As Single = CSng(x + (p.Y2Bref - xMin) / (xMax - xMin) * w)
                Dim py As Single = CSng(y + h - (valueOf(p) - vMin) / (vMax - vMin) * h)
                pts.Add(New PointF(px, py))
            Next
            If pts.Count >= 2 Then G.DrawLines(pen, pts.ToArray())
        Next

        G.DrawString(xMin.ToString("0.00"), tickFont, Brushes.White, New PointF(x, y + h + 2))
        Dim maxLabel = xMax.ToString("0.00")
        Dim maxLabelSize = G.MeasureString(maxLabel, tickFont)
        G.DrawString(maxLabel, tickFont, Brushes.White, New PointF(x + w - maxLabelSize.Width, y + h + 2))

        Dim vMaxLabel = vMax.ToString("0.0000")
        Dim vMaxLabelSize = G.MeasureString(vMaxLabel, tickFont)
        G.DrawString(vMaxLabel, tickFont, Brushes.White, New PointF(x - vMaxLabelSize.Width - 2, y))
        Dim vMinLabel = vMin.ToString("0.0000")
        Dim vMinLabelSize = G.MeasureString(vMinLabel, tickFont)
        G.DrawString(vMinLabel, tickFont, Brushes.White, New PointF(x - vMinLabelSize.Width - 2, y + h - vMinLabelSize.Height))
    End Sub

    Private Async Sub RunPolarSweep_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(projectName) Then
            MessageBox.Show("Please enter a project name in the text box at the top before running analysis.", "Missing Project Name", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtName.Focus()
            Return
        End If
        If frmMain.p Is Nothing OrElse frmMain.p.HasExited Then Return

        Dim aMin As Double, aMax As Double, aStep As Double
        If Not Double.TryParse(txtPolarMin.Text, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, aMin) OrElse
           Not Double.TryParse(txtPolarMax.Text, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, aMax) OrElse
           Not Double.TryParse(txtPolarStep.Text, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, aStep) OrElse
           aStep <= 0 OrElse aMax < aMin Then
            MessageBox.Show("Enter valid numeric alpha min/max/step (step must be positive, max >= min).", "Drag Polar", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim pointCount = CInt(Math.Floor((aMax - aMin) / aStep)) + 1
        If pointCount > 60 Then
            MessageBox.Show("That's more than 60 points - narrow the range or increase the step size.", "Drag Polar", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        btnRunPolar.Enabled = False
        Try
            Dim points = Await RunPolarSweepAsync(aMin, aMax, aStep)
            _lastPolarPoints = points
            RenderPolarPlot()

            If points Is Nothing OrElse points.Count = 0 Then
                Dim debugPath = Path.Combine(Application.StartupPath, $"{projectName}_polar_debug.txt")
                Try
                    File.WriteAllText(debugPath, If(_lastPolarLog, ""))
                    Process.Start(New ProcessStartInfo("notepad.exe", $"""{debugPath}""") With {.UseShellExecute = True})
                Catch
                End Try
                MessageBox.Show("AVL did not return any polar data." & vbCrLf & vbCrLf &
                                 "Full details written to:" & vbCrLf & debugPath,
                                 "Drag Polar", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        Finally
            btnRunPolar.Enabled = True
        End Try
    End Sub

    ' Sweeps alpha from aMin to aMax and collects CL/CD/Cm at each point. AVL has
    ' no built-in sweep command, so this scripts one manually: from the OPER top
    ' level, "a" alone enters a constraint-select submenu, and "a <value>" inside
    ' that submenu sets alpha and returns to OPER (confirmed against real AVL -
    ' sending "a <value>" directly at the OPER top level is NOT recognized).
    ' Each "x" prints a full "Total Forces" block to the console; rather than
    ' polling a file per point, this reads them all out of the console
    ' transcript afterward using the existing ParseTotalForces regex parser.
    Private Async Function RunPolarSweepAsync(aMin As Double, aMax As Double, aStep As Double) As Task(Of List(Of PolarPoint))
        Dim f = Path.Combine(Application.StartupPath, $"{projectName}.avl")
        Dim points As New List(Of PolarPoint)

        SaveAVL()
        _lastPolarLog = ""
        If Not File.Exists(f) Then
            _lastPolarLog = "(Could not save the geometry to " & f & " - check that the project name/path is valid and writable.)"
            Return points
        End If

        Dim logLengthBefore = frmMain.txtLog.Text.Length
        Dim pointCount = CInt(Math.Floor((aMax - aMin) / aStep)) + 1

        With frmMain
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine("load " + f)
            .p.StandardInput.WriteLine("oper")

            Dim alpha = aMin
            Do While alpha <= aMax + aStep * 0.001
                .p.StandardInput.WriteLine("a")
                .p.StandardInput.WriteLine("a " & alpha.ToString(Globalization.CultureInfo.InvariantCulture))
                .p.StandardInput.WriteLine("x")
                alpha += aStep
            Loop

            .p.StandardInput.WriteLine()
            .p.StandardInput.Flush()
        End With

        Await Task.Delay(Math.Min(8000, 200 * pointCount + 300))

        Try
            Dim logText = frmMain.txtLog.Text
            If logLengthBefore <= logText.Length Then
                _lastPolarLog = logText.Substring(logLengthBefore)
            End If
        Catch
        End Try

        If String.IsNullOrEmpty(_lastPolarLog) Then Return points

        ' Split the transcript into one chunk per "Total Forces" block (one per sweep point).
        Dim blocks = Regex.Split(_lastPolarLog, "(?=Vortex Lattice Output -- Total Forces)")
        For Each block In blocks
            If Not block.Contains("Vortex Lattice Output -- Total Forces") Then Continue For
            Dim t = ParseTotalForces(block)
            If t.Valid Then
                points.Add(New PolarPoint With {.Alpha = t.Alpha, .CL = t.CLtot, .CD = t.CDtot, .Cm = t.Cmtot})
            End If
        Next

        Return points
    End Function

    ' Renders the 2x2 drag-polar grid (CL-alpha, CD-alpha, Cm-alpha, CL-CD),
    ' styled consistently with the other in-house plots (black bg, PNG/SVG/PDF
    ' export via ExportView).
    Private Sub RenderPolarPlot(Optional captureVectors As Boolean = False)
        If pPolar Is Nothing OrElse pPolar.Width <= 0 OrElse pPolar.Height <= 0 Then Return

        Dim w As Integer = pPolar.Width
        Dim h As Integer = pPolar.Height
        Dim BMP As New Bitmap(w, h)
        Dim tickFont As Font = GetTickFont(9)
        Dim axisFont As Font = GetAxisFont()
        Dim svgOut As String = ""
        Dim pdfOut As String = ""

        Using G As New SvgGraphics(w, h, Graphics.FromImage(BMP), captureVectors)
            G.SmoothingMode = SmoothingMode.AntiAlias
            G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit
            G.Clear(Color.Black)

            If _lastPolarPoints Is Nothing OrElse _lastPolarPoints.Count = 0 Then
                G.DrawString("Set an alpha range and click ""Run Polar Sweep"" to compute the drag polar.", tickFont, Brushes.Gray, New PointF(10, 10))
            Else
                Dim ordered As New List(Of PolarPoint)(_lastPolarPoints)
                ordered.Sort(Function(a, b) a.Alpha.CompareTo(b.Alpha))

                Dim marginX As Single = 55
                Dim marginY As Single = 35
                Dim cellW As Single = (w - marginX * 3) / 2
                Dim cellH As Single = (h - marginY * 3) / 2

                DrawPolarSubplot(G, tickFont, axisFont, marginX, marginY, cellW, cellH,
                                  "CL vs alpha", Color.OrangeRed,
                                  ordered, Function(p) p.Alpha, Function(p) p.CL)
                DrawPolarSubplot(G, tickFont, axisFont, marginX * 2 + cellW, marginY, cellW, cellH,
                                  "CD vs alpha", Color.DeepSkyBlue,
                                  ordered, Function(p) p.Alpha, Function(p) p.CD)
                DrawPolarSubplot(G, tickFont, axisFont, marginX, marginY * 2 + cellH, cellW, cellH,
                                  "Cm vs alpha", Color.LimeGreen,
                                  ordered, Function(p) p.Alpha, Function(p) p.Cm)
                DrawPolarSubplot(G, tickFont, axisFont, marginX * 2 + cellW, marginY * 2 + cellH, cellW, cellH,
                                  "CL vs CD  (drag polar)", Color.Gold,
                                  ordered, Function(p) p.CD, Function(p) p.CL)
            End If

            If captureVectors Then
                svgOut = G.GetSvgContent()
                pdfOut = G.GetPdfContentStream()
            End If
        End Using

        Dim oldImage = pPolar.Image
        pPolar.Image = BMP
        oldImage?.Dispose()

        If captureVectors Then
            polarSvg = svgOut
            polarPdf = pdfOut
        End If
    End Sub

    Private Sub DrawPolarSubplot(G As SvgGraphics, tickFont As Font, axisFont As Font,
                                  x As Single, y As Single, w As Single, h As Single,
                                  title As String, curveColor As Color,
                                  points As List(Of PolarPoint),
                                  xOf As Func(Of PolarPoint, Double), yOf As Func(Of PolarPoint, Double))
        If points.Count = 0 Then Return

        Dim xMin As Double = Double.MaxValue, xMax As Double = Double.MinValue
        Dim yMin As Double = Double.MaxValue, yMax As Double = Double.MinValue
        For Each p In points
            Dim xv = xOf(p) : Dim yv = yOf(p)
            xMin = Math.Min(xMin, xv) : xMax = Math.Max(xMax, xv)
            yMin = Math.Min(yMin, yv) : yMax = Math.Max(yMax, yv)
        Next
        If xMin = xMax Then xMin -= 1 : xMax += 1
        If yMin = yMax Then yMin -= 1 : yMax += 1
        Dim yPad = (yMax - yMin) * 0.1
        yMin -= yPad
        yMax += yPad

        Dim whitePen As New Pen(Color.White, 1)
        Dim gridPen As New Pen(Color.FromArgb(90, 90, 90), 1) With {.DashStyle = DashStyle.Dash}

        G.DrawRectangle(whitePen, x, y, w, h)
        G.DrawString(title, axisFont, Brushes.White, New PointF(x, y - axisFont.Height - 2))

        If yMin < 0 AndAlso yMax > 0 Then
            Dim zeroY As Single = CSng(y + h - (0 - yMin) / (yMax - yMin) * h)
            G.DrawLine(gridPen, x, zeroY, x + w, zeroY)
        End If

        Dim pen As New Pen(curveColor, 1.5F)
        Dim markerBrush As New SolidBrush(curveColor)
        Dim pts As New List(Of PointF)
        For Each p In points
            Dim px As Single = CSng(x + (xOf(p) - xMin) / (xMax - xMin) * w)
            Dim py As Single = CSng(y + h - (yOf(p) - yMin) / (yMax - yMin) * h)
            pts.Add(New PointF(px, py))
        Next
        If pts.Count >= 2 Then G.DrawLines(pen, pts.ToArray())
        For Each pt In pts
            G.FillEllipse(markerBrush, pt.X - 2, pt.Y - 2, 4, 4)
        Next

        G.DrawString(xMin.ToString("0.00"), tickFont, Brushes.White, New PointF(x, y + h + 2))
        Dim maxLabel = xMax.ToString("0.00")
        Dim maxLabelSize = G.MeasureString(maxLabel, tickFont)
        G.DrawString(maxLabel, tickFont, Brushes.White, New PointF(x + w - maxLabelSize.Width, y + h + 2))

        Dim yMaxLabel = yMax.ToString("0.0000")
        Dim yMaxLabelSize = G.MeasureString(yMaxLabel, tickFont)
        G.DrawString(yMaxLabel, tickFont, Brushes.White, New PointF(x - yMaxLabelSize.Width - 2, y))
        Dim yMinLabel = yMin.ToString("0.0000")
        Dim yMinLabelSize = G.MeasureString(yMinLabel, tickFont)
        G.DrawString(yMinLabel, tickFont, Brushes.White, New PointF(x - yMinLabelSize.Width - 2, y + h - yMinLabelSize.Height))
    End Sub

    Private Async Sub RunDerivatives_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(projectName) Then
            MessageBox.Show("Please enter a project name in the text box at the top before running analysis.", "Missing Project Name", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtName.Focus()
            Return
        End If
        If frmMain.p Is Nothing OrElse frmMain.p.HasExited Then Return

        btnRunDerivatives.Enabled = False
        Try
            Dim text = Await RunDerivativesAnalysisAsync()
            _lastDerivativesText = text
            txtDerivatives.Text = If(String.IsNullOrWhiteSpace(text), "AVL did not return any data. Check the geometry and try again.", text)
            tc1.SelectedTab = Derivatives
        Finally
            btnRunDerivatives.Enabled = True
        End Try
    End Sub

    ' Runs ST, SB, FN, FB, HM back-to-back in one OPER session (chained without
    ' a blank line between them, since AVL auto-returns to the OPER menu after
    ' each data-write command - a blank line there pops back out to the top
    ' level and breaks the chain, confirmed while testing this sequence) and
    ' combines all five outputs into one formatted text block. FB/HM are
    ' legitimately empty for geometries with no bodies/control surfaces.
    Private Async Function RunDerivativesAnalysisAsync() As Task(Of String)
        Dim f = Path.Combine(Application.StartupPath, $"{projectName}.avl")

        SaveAVL()
        If Not File.Exists(f) Then
            Return "(Could not save the geometry to " & f & " - check that the project name/path is valid and writable.)"
        End If

        Dim stFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        Dim sbFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        Dim fnFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        Dim fbFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        Dim hmFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

        With frmMain
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine("load " + f)
            .p.StandardInput.WriteLine("oper")
            .p.StandardInput.WriteLine("x")
            .p.StandardInput.WriteLine("st")
            .p.StandardInput.WriteLine(stFile)
            .p.StandardInput.WriteLine("sb")
            .p.StandardInput.WriteLine(sbFile)
            .p.StandardInput.WriteLine("fn")
            .p.StandardInput.WriteLine(fnFile)
            .p.StandardInput.WriteLine("fb")
            .p.StandardInput.WriteLine(fbFile)
            .p.StandardInput.WriteLine("hm")
            .p.StandardInput.WriteLine(hmFile)
            .p.StandardInput.WriteLine()
            .p.StandardInput.Flush()
        End With

        ' Poll the last file in the chain (hm) - by the time it's stable, the
        ' earlier ones are already fully written.
        Dim deadline = DateTime.Now.AddSeconds(12)
        Dim lastLen As Long = -1
        Do
            Await Task.Delay(150)
            Try
                If File.Exists(hmFile) Then
                    Dim len = New FileInfo(hmFile).Length
                    If len > 0 AndAlso len = lastLen Then Exit Do
                    lastLen = len
                End If
            Catch
            End Try
        Loop While DateTime.Now < deadline
        Await Task.Delay(150)

        Dim sb As New Text.StringBuilder()
        AppendDerivativesSection(sb, "STABILITY DERIVATIVES (stability axis)", stFile)
        AppendDerivativesSection(sb, "BODY-AXIS DERIVATIVES", sbFile)
        AppendDerivativesSection(sb, "SURFACE FORCES", fnFile)
        AppendDerivativesSection(sb, "BODY FORCES", fbFile)
        AppendDerivativesSection(sb, "CONTROL HINGE MOMENTS", hmFile)

        For Each fp In New String() {stFile, sbFile, fnFile, fbFile, hmFile}
            Try
                File.Delete(fp)
            Catch
            End Try
        Next

        Return sb.ToString()
    End Function

    Private Sub AppendDerivativesSection(sb As Text.StringBuilder, title As String, path As String)
        sb.AppendLine("======================================================================")
        sb.AppendLine(title)
        sb.AppendLine("======================================================================")
        If File.Exists(path) Then
            Dim content = File.ReadAllText(path).Trim()
            If String.IsNullOrWhiteSpace(content) Then
                sb.AppendLine("(no data - this geometry may not have bodies/control surfaces defined)")
            Else
                sb.AppendLine(content)
            End If
        Else
            sb.AppendLine("(AVL did not produce output for this command)")
        End If
        sb.AppendLine()
    End Sub

    Private Sub ExportDerivatives_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(_lastDerivativesText) Then
            MessageBox.Show("Run the analysis first.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim sfd As New SaveFileDialog()
        sfd.Filter = "Text File (*.txt)|*.txt"
        sfd.FileName = $"{projectName}_derivatives.txt"
        If sfd.ShowDialog() = DialogResult.OK Then
            Try
                File.WriteAllText(sfd.FileName, _lastDerivativesText)
                MessageBox.Show("Exported successfully to:" & vbCrLf & sfd.FileName, "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show("Error exporting file: " & ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End If
    End Sub

    Private Async Sub RunFEAnalysis_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(projectName) Then
            MessageBox.Show("Please enter a project name in the text box at the top before running analysis.", "Missing Project Name", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtName.Focus()
            Return
        End If
        If frmMain.p Is Nothing OrElse frmMain.p.HasExited Then Return

        btnRunFE.Enabled = False
        Try
            Dim strips = Await RunFEAnalysisAsync()
            _lastFeStrips = strips

            cmbFeStrip.Items.Clear()
            If strips IsNot Nothing Then
                For Each s In strips
                    cmbFeStrip.Items.Add($"{s.SurfaceName}  Y={s.Yle:0.000}  (strip {s.StripIndex})")
                Next
            End If

            If strips Is Nothing OrElse strips.Count = 0 Then
                cmbFeStrip.Enabled = False
                RenderFEPlot()
                Dim debugPath = Path.Combine(Application.StartupPath, $"{projectName}_pressure_debug.txt")
                Try
                    File.WriteAllText(debugPath, If(_lastFeLog, ""))
                    Process.Start(New ProcessStartInfo("notepad.exe", $"""{debugPath}""") With {.UseShellExecute = True})
                Catch
                End Try
                MessageBox.Show("AVL did not return any element-force data." & vbCrLf & vbCrLf &
                                 "Full details written to:" & vbCrLf & debugPath,
                                 "Pressure Distribution", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Else
                cmbFeStrip.Enabled = True
                cmbFeStrip.SelectedIndex = 0 ' triggers RenderFEPlot via SelectedIndexChanged
                tc1.SelectedTab = FE
            End If
        Finally
            btnRunFE.Enabled = True
        End Try
    End Sub

    ' Runs the current case and asks AVL to write element (chordwise panel)
    ' forces to a temp file (OPER -> X -> FE -> <file>). Same save/short-temp-
    ' path/poll pattern as the other analyses.
    Private Async Function RunFEAnalysisAsync() As Task(Of List(Of FeStrip))
        Dim f = Path.Combine(Application.StartupPath, $"{projectName}.avl")
        Dim outFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

        SaveAVL()
        _lastFeLog = ""
        If Not File.Exists(f) Then
            _lastFeLog = "(Could not save the geometry to " & f & " - check that the project name/path is valid and writable.)"
            Return New List(Of FeStrip)
        End If

        Dim logLengthBefore = frmMain.txtLog.Text.Length

        With frmMain
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine("load " + f)
            .p.StandardInput.WriteLine("oper")
            .p.StandardInput.WriteLine("x")
            .p.StandardInput.WriteLine("fe")
            .p.StandardInput.WriteLine(outFile)
            .p.StandardInput.WriteLine()
            .p.StandardInput.Flush()
        End With

        Dim deadline = DateTime.Now.AddSeconds(15)
        Dim lastLen As Long = -1
        Do
            Await Task.Delay(150)
            Try
                If File.Exists(outFile) Then
                    Dim len = New FileInfo(outFile).Length
                    If len > 0 AndAlso len = lastLen Then Exit Do
                    lastLen = len
                End If
            Catch
            End Try
        Loop While DateTime.Now < deadline

        Await Task.Delay(150)
        Try
            Dim logText = frmMain.txtLog.Text
            If logLengthBefore <= logText.Length Then
                _lastFeLog = logText.Substring(logLengthBefore)
            End If
        Catch
        End Try

        If Not File.Exists(outFile) Then Return New List(Of FeStrip)

        Try
            Return ParseFeFile(outFile)
        Catch
            Return New List(Of FeStrip)
        Finally
            Try
                File.Delete(outFile)
            Catch
            End Try
        End Try
    End Function

    ' Parses AVL's "FE" (element forces) output. Format confirmed against a
    ' real AVL 3.37 run - one block per spanwise strip:
    '   Strip #  1     # Chordwise =  8   First Vortex =   1
    '      Xle =   0.00000 ...
    '      Yle =   0.02139    Strip Width  =   0.08519 ...
    '      cl  =   0.48163       cd  =   0.00462      cdv =   0.00000
    '      I        X           Y           Z           DX        Slope        dCp
    '      1     0.00851     0.04259     0.00000     0.05242     0.00000     2.14797
    ' dCp is the local chordwise pressure-coefficient jump across each panel.
    Private Function ParseFeFile(path As String) As List(Of FeStrip)
        Dim strips As New List(Of FeStrip)
        Dim currentSurfaceName As String = ""
        Dim current As FeStrip = Nothing
        Dim inTable As Boolean = False

        For Each raw As String In File.ReadAllLines(path)
            Dim line = raw.Trim()

            Dim surfMatch = Regex.Match(line, "^Surface #\s*\d+\s+(.+)$")
            If surfMatch.Success Then
                currentSurfaceName = surfMatch.Groups(1).Value.Trim()
                Continue For
            End If

            Dim stripMatch = Regex.Match(line, "^Strip #\s*(\d+)")
            If stripMatch.Success Then
                current = New FeStrip With {.SurfaceName = currentSurfaceName, .StripIndex = Integer.Parse(stripMatch.Groups(1).Value)}
                strips.Add(current)
                inTable = False
                Continue For
            End If

            If current Is Nothing Then Continue For

            If line.StartsWith("Yle") Then
                Dim m = Regex.Match(line, "Yle\s*=\s*(-?[\d.]+)")
                If m.Success Then Double.TryParse(m.Groups(1).Value, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, current.Yle)
                Continue For
            End If

            If line.StartsWith("cl") Then
                Dim mCl = Regex.Match(line, "cl\s*=\s*(-?[\d.]+)")
                Dim mCd = Regex.Match(line, "cd\s*=\s*(-?[\d.]+)")
                If mCl.Success Then Double.TryParse(mCl.Groups(1).Value, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, current.Cl)
                If mCd.Success Then Double.TryParse(mCd.Groups(1).Value, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, current.Cd)
                Continue For
            End If

            If line.StartsWith("I ") AndAlso line.Contains("dCp") Then
                inTable = True
                Continue For
            End If

            If Not inTable Then Continue For

            Dim tokens = line.Split({" "c, Chr(9)}, StringSplitOptions.RemoveEmptyEntries)
            If tokens.Length <> 7 Then
                inTable = False
                Continue For
            End If
            Dim idx As Integer
            If Not Integer.TryParse(tokens(0), idx) Then
                inTable = False
                Continue For
            End If
            Dim x As Double, dcp As Double
            If Double.TryParse(tokens(1), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, x) AndAlso
               Double.TryParse(tokens(6), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, dcp) Then
                current.Panels.Add(New FePanel With {.X = x, .DCp = dcp})
            End If
        Next

        Return strips
    End Function

    ' Renders the chordwise dCp distribution for the currently selected span
    ' station (cmbFeStrip), styled consistently with the other in-house plots.
    Private Sub RenderFEPlot(Optional captureVectors As Boolean = False)
        If pFE Is Nothing OrElse pFE.Width <= 0 OrElse pFE.Height <= 0 Then Return

        Dim w As Integer = pFE.Width
        Dim h As Integer = pFE.Height
        Dim BMP As New Bitmap(w, h)
        Dim tickFont As Font = GetTickFont(9)
        Dim axisFont As Font = GetAxisFont()
        Dim svgOut As String = ""
        Dim pdfOut As String = ""

        Using G As New SvgGraphics(w, h, Graphics.FromImage(BMP), captureVectors)
            G.SmoothingMode = SmoothingMode.AntiAlias
            G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit
            G.Clear(Color.Black)

            Dim strip As FeStrip = Nothing
            If _lastFeStrips IsNot Nothing AndAlso cmbFeStrip.SelectedIndex >= 0 AndAlso cmbFeStrip.SelectedIndex < _lastFeStrips.Count Then
                strip = _lastFeStrips(cmbFeStrip.SelectedIndex)
            End If

            If strip Is Nothing OrElse strip.Panels.Count = 0 Then
                G.DrawString("Run ""Pressure Analysis"" and pick a span station to see its chordwise dCp distribution.", tickFont, Brushes.Gray, New PointF(10, 10))
            Else
                Dim margin As Single = 55
                Dim plotW As Single = w - margin * 2
                Dim plotH As Single = h - margin * 2

                Dim title = $"dCp vs X/c  -  {strip.SurfaceName}  Y={strip.Yle:0.000}  (cl={strip.Cl:0.0000}, cd={strip.Cd:0.0000})"
                DrawFeSubplot(G, tickFont, axisFont, margin, margin, plotW, plotH, title, strip)
            End If

            If captureVectors Then
                svgOut = G.GetSvgContent()
                pdfOut = G.GetPdfContentStream()
            End If
        End Using

        Dim oldImage = pFE.Image
        pFE.Image = BMP
        oldImage?.Dispose()

        If captureVectors Then
            feSvg = svgOut
            fePdf = pdfOut
        End If
    End Sub

    Private Sub DrawFeSubplot(G As SvgGraphics, tickFont As Font, axisFont As Font,
                               x As Single, y As Single, w As Single, h As Single,
                               title As String, strip As FeStrip)
        Dim xMin As Double = Double.MaxValue, xMax As Double = Double.MinValue
        Dim vMin As Double = Double.MaxValue, vMax As Double = Double.MinValue
        For Each p In strip.Panels
            xMin = Math.Min(xMin, p.X)
            xMax = Math.Max(xMax, p.X)
            vMin = Math.Min(vMin, p.DCp)
            vMax = Math.Max(vMax, p.DCp)
        Next
        vMin = Math.Min(0, vMin)
        If xMin = xMax Then xMin -= 1 : xMax += 1
        If vMin = vMax Then vMin -= 1 : vMax += 1
        Dim vPad As Double = (vMax - vMin) * 0.1
        vMin -= vPad
        vMax += vPad

        Dim whitePen As New Pen(Color.White, 1)
        Dim gridPen As New Pen(Color.FromArgb(90, 90, 90), 1) With {.DashStyle = DashStyle.Dash}

        G.DrawRectangle(whitePen, x, y, w, h)
        G.DrawString(title, axisFont, Brushes.White, New PointF(x, y - axisFont.Height - 2))

        If vMin < 0 AndAlso vMax > 0 Then
            Dim zeroY As Single = CSng(y + h - (0 - vMin) / (vMax - vMin) * h)
            G.DrawLine(gridPen, x, zeroY, x + w, zeroY)
        End If

        Dim ordered As New List(Of FePanel)(strip.Panels)
        ordered.Sort(Function(a, b) a.X.CompareTo(b.X))
        Dim pen As New Pen(Color.Gold, 1.5F)
        Dim markerBrush As New SolidBrush(Color.Gold)
        Dim pts As New List(Of PointF)
        For Each p In ordered
            Dim px As Single = CSng(x + (p.X - xMin) / (xMax - xMin) * w)
            Dim py As Single = CSng(y + h - (p.DCp - vMin) / (vMax - vMin) * h)
            pts.Add(New PointF(px, py))
        Next
        If pts.Count >= 2 Then G.DrawLines(pen, pts.ToArray())
        For Each pt In pts
            G.FillEllipse(markerBrush, pt.X - 2.5F, pt.Y - 2.5F, 5, 5)
        Next

        G.DrawString(xMin.ToString("0.00"), tickFont, Brushes.White, New PointF(x, y + h + 2))
        Dim maxLabel = xMax.ToString("0.00")
        Dim maxLabelSize = G.MeasureString(maxLabel, tickFont)
        G.DrawString(maxLabel, tickFont, Brushes.White, New PointF(x + w - maxLabelSize.Width, y + h + 2))
        Dim xAxisLabelSize = G.MeasureString("X/c", tickFont)
        G.DrawString("X/c", tickFont, Brushes.White, New PointF(x + w / 2 - xAxisLabelSize.Width / 2, y + h + 2))

        Dim vMaxLabel = vMax.ToString("0.00")
        Dim vMaxLabelSize = G.MeasureString(vMaxLabel, tickFont)
        G.DrawString(vMaxLabel, tickFont, Brushes.White, New PointF(x - vMaxLabelSize.Width - 2, y))
        Dim vMinLabel = vMin.ToString("0.00")
        Dim vMinLabelSize = G.MeasureString(vMinLabel, tickFont)
        G.DrawString(vMinLabel, tickFont, Brushes.White, New PointF(x - vMinLabelSize.Width - 2, y + h - vMinLabelSize.Height))
    End Sub

    Private Async Sub RunModesAnalysis_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(projectName) Then
            MessageBox.Show("Please enter a project name in the text box at the top before running analysis.", "Missing Project Name", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtName.Focus()
            Return
        End If
        If frmMain.p Is Nothing OrElse frmMain.p.HasExited Then Return

        Dim massPath = Path.Combine(Application.StartupPath, $"{projectName}.mass")
        Dim runPath = Path.Combine(Application.StartupPath, $"{projectName}.run")
        If Not File.Exists(massPath) OrElse Not File.Exists(runPath) Then
            Dim res = MessageBox.Show(
                "No saved Mass and/or Run file found for this project." & vbCrLf & vbCrLf &
                "Eigenmode analysis still needs mass/inertia and a trimmed, non-zero velocity to mean anything - " &
                "without them AVL uses placeholder values (mass=1kg, Ixx=Iyy=Izz=1) and the result won't reflect your aircraft." & vbCrLf & vbCrLf &
                "Continue anyway?",
                "Missing Mass/Run Data", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            If res = DialogResult.No Then Return
        End If

        btnRunModes.Enabled = False
        _lastModesHoverIndex = -1
        Try
            Dim eigs = Await RunModesAnalysisAsync(massPath, runPath)

            ' Match each eigenvalue (from the "W" file) to its eigenvector
            ' (from the console transcript, only source that reports it) by
            ' position - both come from the same "N" computation in the same
            ' order, since this app only ever runs a single selected case.
            If eigs IsNot Nothing AndAlso eigs.Count > 0 Then
                Dim vectors = ParseModeEigenvectors(_lastModesLog)
                If vectors.Count = eigs.Count Then
                    For i = 0 To eigs.Count - 1
                        eigs(i).MagU = vectors(i).MagU
                        eigs(i).MagV = vectors(i).MagV
                        eigs(i).MagW = vectors(i).MagW
                        eigs(i).MagP = vectors(i).MagP
                        eigs(i).MagQ = vectors(i).MagQ
                        eigs(i).MagR = vectors(i).MagR
                        eigs(i).MagPhi = vectors(i).MagPhi
                        eigs(i).MagTheta = vectors(i).MagTheta
                        eigs(i).MagPsi = vectors(i).MagPsi
                    Next
                    ClassifyModes(eigs)
                End If
            End If

            _lastEigenvalues = eigs
            RenderModesPlot()

            If eigs Is Nothing OrElse eigs.Count = 0 Then
                Dim debugPath = Path.Combine(Application.StartupPath, $"{projectName}_modes_debug.txt")
                Try
                    File.WriteAllText(debugPath, If(_lastModesLog, ""))
                    Process.Start(New ProcessStartInfo("notepad.exe", $"""{debugPath}""") With {.UseShellExecute = True})
                Catch
                End Try
                MessageBox.Show("AVL did not return any eigenvalues." & vbCrLf & vbCrLf &
                                 "This usually means the trim/mass data isn't valid (e.g. zero velocity)." & vbCrLf & vbCrLf &
                                 "Full details written to:" & vbCrLf & debugPath,
                                 "Eigenvalue Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Else
                tc1.SelectedTab = ModesTab
            End If
        Finally
            btnRunModes.Enabled = True
        End Try
    End Sub

    ' Runs AVL's top-level ".MODE" menu (NOT under OPER) to compute dynamic-
    ' stability eigenvalues: load -> [mass/mset] -> [case] -> oper -> x ->
    ' <blank to exit OPER> -> mode -> n (compute) -> w -> <file> -> <blank to
    ' exit MODE>. Confirmed against real AVL - "mode" is only valid from the
    ' top-level menu, so OPER must be explicitly exited with a blank line first
    ' (chaining straight from "x" the way other OPER commands chain does NOT
    ' work here). Does NOT force-save the Mass/Run tabs (see RunModesAnalysis_
    ' Click) since txt3 may currently hold a different tab's content - only
    ' uses massPath/runPath if they already exist on disk.
    Private Async Function RunModesAnalysisAsync(massPath As String, runPath As String) As Task(Of List(Of EigenValue))
        Dim f = Path.Combine(Application.StartupPath, $"{projectName}.avl")
        Dim outFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

        SaveAVL()
        _lastModesLog = ""
        If Not File.Exists(f) Then
            _lastModesLog = "(Could not save the geometry to " & f & " - check that the project name/path is valid and writable.)"
            Return New List(Of EigenValue)
        End If

        Dim logLengthBefore = frmMain.txtLog.Text.Length

        With frmMain
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            ' The "N" (new eigenmode calculation) command inherently pops open
            ' AVL's own native root-locus graphics window as a side effect (per
            ' AVL's docs, unlike the FS/VM/ST/SB/FN/FB/HM/FE commands used
            ' elsewhere in this app, which are pure text/file output with no
            ' graphics side effect) - with no window-close command in this
            ' scripted flow, that window would be left open indefinitely.
            ' Disabling AVL's graphics-enable flag first (PLOP -> G) prevents
            ' it from ever opening at all; confirmed against real AVL that the
            ' eigenvalue computation and file write still work identically.
            .p.StandardInput.WriteLine("plop")
            .p.StandardInput.WriteLine("g")
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine("load " + f)
            If File.Exists(massPath) Then
                .p.StandardInput.WriteLine("mass " + massPath)
                .p.StandardInput.WriteLine("mset 1")
            End If
            If File.Exists(runPath) Then
                .p.StandardInput.WriteLine("case " + runPath)
            End If
            .p.StandardInput.WriteLine("oper")
            .p.StandardInput.WriteLine("x")
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine("mode")
            .p.StandardInput.WriteLine("n")
            .p.StandardInput.WriteLine("w")
            .p.StandardInput.WriteLine(outFile)
            .p.StandardInput.WriteLine()
            .p.StandardInput.Flush()
        End With

        Dim deadline = DateTime.Now.AddSeconds(15)
        Dim lastLen As Long = -1
        Do
            Await Task.Delay(150)
            Try
                If File.Exists(outFile) Then
                    Dim len = New FileInfo(outFile).Length
                    If len > 0 AndAlso len = lastLen Then Exit Do
                    lastLen = len
                End If
            Catch
            End Try
        Loop While DateTime.Now < deadline

        Await Task.Delay(150)
        Try
            Dim logText = frmMain.txtLog.Text
            If logLengthBefore <= logText.Length Then
                _lastModesLog = logText.Substring(logLengthBefore)
            End If
        Catch
        End Try

        If Not File.Exists(outFile) Then Return New List(Of EigenValue)

        Try
            Return ParseEigFile(outFile)
        Catch
            Return New List(Of EigenValue)
        Finally
            Try
                File.Delete(outFile)
            Catch
            End Try
        End Try
    End Function

    ' Parses AVL's ".MODE" -> "W" eigenvalue output. Format confirmed against
    ' a real AVL 3.37 run:
    '   # [AR Plane]
    '   #
    '   #   Run case     Eigenvalue
    '          1    -15.427003         33.093903
    Private Function ParseEigFile(path As String) As List(Of EigenValue)
        Dim results As New List(Of EigenValue)
        For Each raw As String In File.ReadAllLines(path)
            Dim line = raw.Trim()
            If line.Length = 0 OrElse line.StartsWith("#") Then Continue For
            Dim tokens = line.Split({" "c, Chr(9)}, StringSplitOptions.RemoveEmptyEntries)
            If tokens.Length <> 3 Then Continue For
            Dim rc As Integer
            Dim re As Double, im As Double
            If Integer.TryParse(tokens(0), rc) AndAlso
               Double.TryParse(tokens(1), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, re) AndAlso
               Double.TryParse(tokens(2), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, im) Then
                results.Add(New EigenValue With {.RunCase = rc, .Real = re, .Imag = im})
            End If
        Next
        Return results
    End Function

    ' Parses the eigenvector blocks AVL's "N" (new eigenmode calculation)
    ' command prints to the console for each mode - confirmed against a real
    ' AVL 3.37 run:
    '   mode 1:  -15.4270       33.0939
    '  u  :     0.0000    -0.0000      v  :    -0.1575    -0.1720      x  : -0.3561E-09 -0.5899E-10
    '  w  :     0.0000     0.0000      p  :    -0.0082    -0.0579      y  : -0.1560E-02 -0.9599E-03
    '  ...
    ' Scans each per-mode chunk for every "name : re im" triple regardless of
    ' which line it's wrapped onto, rather than assuming a fixed column
    ' layout. This is separate from the eigenvalues themselves (which come
    ' from the "W" file via ParseEigFile) - the console transcript is the only
    ' place AVL reports the eigenvectors needed to classify each mode.
    Private Function ParseModeEigenvectors(logText As String) As List(Of EigenValue)
        Dim results As New List(Of EigenValue)
        If String.IsNullOrEmpty(logText) Then Return results

        Dim numPat = "(-?\d+\.?\d*(?:[eE][-+]?\d+)?)"
        Dim modeHeaderPat = "mode\s+\d+:\s*" & numPat & "\s+" & numPat
        Dim headers = Regex.Matches(logText, modeHeaderPat)
        For i = 0 To headers.Count - 1
            Dim h = headers(i)
            Dim blockStart = h.Index + h.Length
            Dim blockEnd = If(i + 1 < headers.Count, headers(i + 1).Index, logText.Length)
            Dim block = logText.Substring(blockStart, blockEnd - blockStart)

            Dim ev As New EigenValue()
            ev.Real = Double.Parse(h.Groups(1).Value, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture)
            ev.Imag = Double.Parse(h.Groups(2).Value, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture)

            Dim varPat = "(\w+)\s*:\s*" & numPat & "\s+" & numPat
            For Each m As Match In Regex.Matches(block, varPat)
                Dim name = m.Groups(1).Value.ToLowerInvariant()
                Dim re As Double
                Dim im As Double
                If Not Double.TryParse(m.Groups(2).Value, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, re) Then Continue For
                If Not Double.TryParse(m.Groups(3).Value, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, im) Then Continue For
                Dim mag = Math.Sqrt(re * re + im * im)
                Select Case name
                    Case "u" : ev.MagU = mag
                    Case "v" : ev.MagV = mag
                    Case "w" : ev.MagW = mag
                    Case "p" : ev.MagP = mag
                    Case "q" : ev.MagQ = mag
                    Case "r" : ev.MagR = mag
                    Case "phi" : ev.MagPhi = mag
                    Case "the" : ev.MagTheta = mag
                    Case "psi" : ev.MagPsi = mag
                End Select
            Next
            results.Add(ev)
        Next
        Return results
    End Function

    ' Classifies each mode as a named flight-dynamics mode using standard
    ' heuristics: which state group dominates the eigenvector (u/w/q/theta =
    ' longitudinal, v/p/r/phi = lateral-directional), whether the root is
    ' complex (oscillatory) or real, and - since phugoid vs short-period and
    ' spiral vs roll-subsidence are only distinguishable by RELATIVE frequency/
    ' speed, not an absolute threshold - ranking same-category roots against
    ' each other (matching how this is actually done in flight-dynamics
    ' analysis). This is a best-effort label, not a guaranteed-correct
    ' classification for unusual configurations.
    Private Sub ClassifyModes(modes As List(Of EigenValue))
        Dim longEnergy = Function(m As EigenValue) m.MagU * m.MagU + m.MagW * m.MagW + m.MagQ * m.MagQ + m.MagTheta * m.MagTheta
        Dim latEnergy = Function(m As EigenValue) m.MagV * m.MagV + m.MagP * m.MagP + m.MagR * m.MagR + m.MagPhi * m.MagPhi
        Dim freq = Function(m As EigenValue) Math.Sqrt(m.Real * m.Real + m.Imag * m.Imag)

        Dim longModes As New List(Of EigenValue)
        Dim latModes As New List(Of EigenValue)
        For Each m In modes
            If longEnergy(m) >= latEnergy(m) Then
                longModes.Add(m)
            Else
                latModes.Add(m)
            End If
        Next

        Dim longOsc = longModes.FindAll(Function(m) Math.Abs(m.Imag) > 0.001)
        Dim longReal = longModes.FindAll(Function(m) Math.Abs(m.Imag) <= 0.001)
        longOsc.Sort(Function(a, b) freq(a).CompareTo(freq(b)))
        For Each m In longOsc
            m.ModeLabel = If(freq(m) < 2.0, "Phugoid", "Short Period")
        Next
        For Each m In longReal
            m.ModeLabel = "Longitudinal"
        Next

        Dim latOsc = latModes.FindAll(Function(m) Math.Abs(m.Imag) > 0.001)
        Dim latReal = latModes.FindAll(Function(m) Math.Abs(m.Imag) <= 0.001)
        latReal.Sort(Function(a, b) Math.Abs(b.Real).CompareTo(Math.Abs(a.Real)))
        For Each m In latOsc
            m.ModeLabel = "Dutch Roll"
        Next
        For i2 = 0 To latReal.Count - 1
            If i2 = 0 AndAlso latReal.Count > 1 Then
                latReal(i2).ModeLabel = "Roll Subsidence"
            ElseIf Math.Abs(latReal(i2).Real) < 1.0 Then
                latReal(i2).ModeLabel = "Spiral"
            Else
                latReal(i2).ModeLabel = "Roll Subsidence"
            End If
        Next
    End Sub

    ' Shows a popup with mode-specific suggestions for turning an unstable
    ' root stable via geometry or mass-distribution changes. Standard,
    ' textbook flight-dynamics guidance (Nelson, Etkin) rather than anything
    ' computed from this specific aircraft's derivatives - a starting point to
    ' try, not a guarantee, and re-running the analysis after a change is the
    ' only way to confirm it actually helped (fixes for one mode can worsen
    ' another - e.g. fin size trades off Dutch roll against spiral stability).
    Private Sub ShowModeStabilityTips_Click(sender As Object, e As EventArgs)
        If _lastEigenvalues Is Nothing OrElse _lastEigenvalues.Count = 0 Then
            MessageBox.Show("Run the eigenvalue analysis first.", "Stability Tips", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim unstable = _lastEigenvalues.FindAll(Function(m) m.Real >= 0)
        If unstable.Count = 0 Then
            MessageBox.Show(
                "All computed modes are stable (every root has a negative real part) - no changes needed." & vbCrLf & vbCrLf &
                "This only reflects the mass/inertia and trim condition used for this run - re-check after any significant geometry or loading change.",
                "Stability Tips", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim seenLabels As New List(Of String)
        Dim sb As New Text.StringBuilder()
        sb.AppendLine($"{unstable.Count} unstable mode(s) found (positive real part = a growing oscillation or divergence):")
        sb.AppendLine()
        For Each m In unstable
            Dim label = If(String.IsNullOrEmpty(m.ModeLabel), "Unclassified", m.ModeLabel)
            If seenLabels.Contains(label) Then Continue For ' a complex-conjugate pair shares one tip
            seenLabels.Add(label)
            sb.AppendLine(GetStabilityTip(label))
            sb.AppendLine()
        Next
        sb.Append("These are general aerodynamic guidelines, not computed for this specific configuration. " &
                   "After making a change, re-run the analysis to confirm it actually helped - a fix for one mode can weaken another.")

        MessageBox.Show(sb.ToString(), "Stability Improvement Tips", MessageBoxButtons.OK, MessageBoxIcon.Warning)
    End Sub

    Private Function GetStabilityTip(modeLabel As String) As String
        Select Case modeLabel
            Case "Short Period"
                Return "SHORT PERIOD (longitudinal, fast pitch oscillation)" & vbCrLf &
                       "Usually caused by insufficient static margin - the CG sitting too close to, or behind, the neutral point." & vbCrLf &
                       "Try: moving the CG forward (shift mass forward - battery/payload/engine placement), or increasing the horizontal " &
                       "tail's stabilizing effect (larger tail area, or a longer tail moment arm) to move the neutral point aft. " &
                       "Either one increases the static margin, which directly increases short-period stability."
            Case "Phugoid"
                Return "PHUGOID (longitudinal, slow speed/altitude exchange)" & vbCrLf &
                       "Less commonly unstable than short period. Usually linked to weak pitch damping, or the short-period and phugoid " &
                       "modes interacting badly because the static margin is very small." & vbCrLf &
                       "Try: increasing pitch damping (a larger or longer-arm horizontal tail increases Cmq), and confirming there's a " &
                       "healthy static margin. Since the phugoid is fundamentally a slow trade between speed and altitude (kinetic vs " &
                       "potential energy), reducing excess drag at the trim condition can also help."
            Case "Dutch Roll"
                Return "DUTCH ROLL (lateral-directional, yaw/roll oscillation)" & vbCrLf &
                       "Usually caused by too little directional stability or yaw damping relative to the dihedral effect." & vbCrLf &
                       "Try: increasing vertical tail (fin) area, or moving it further aft for a longer moment arm - this increases both " &
                       "directional stability and yaw damping. Reducing excessive wing dihedral (or sweep, which acts like added " &
                       "dihedral) can also help. Trade-off: a bigger fin helps Dutch roll but can hurt spiral stability (see below) - " &
                       "aircraft design usually balances the two rather than maximizing either."
            Case "Spiral"
                Return "SPIRAL (lateral-directional, slow roll/heading divergence)" & vbCrLf &
                       "Usually caused by too much directional stability (a big fin) relative to dihedral effect - the aircraft " &
                       """weathervanes"" into a bank faster than the dihedral rolls it back level." & vbCrLf &
                       "Try: increasing wing dihedral angle (or sweep), or reducing vertical tail size/moment arm. Spiral instability is " &
                       "usually the mildest and slowest of the classic instabilities and is easy for a pilot (or autopilot) to correct, " &
                       "so a small amount is common even in certified aircraft - only worth chasing if it's fast."
            Case "Roll Subsidence"
                Return "ROLL SUBSIDENCE (lateral, roll-rate damping)" & vbCrLf &
                       "This mode is almost always stable for a conventional aircraft - roll damping is inherently stabilizing for any " &
                       "wing that's generating lift. If it's showing unstable here, double-check the mass/inertia data (Ixx) and control " &
                       "surface definitions first - that usually points to a setup issue rather than a real aerodynamic problem."
            Case "Longitudinal"
                Return "LONGITUDINAL (non-oscillatory)" & vbCrLf &
                       "A real (non-oscillatory) unstable longitudinal root, dominated by speed/pitch states rather than a classic " &
                       "phugoid or short-period oscillation." & vbCrLf &
                       "Try the same fixes as short period: move the CG forward, or increase horizontal tail size/moment arm to add " &
                       "static margin."
            Case Else
                Return "UNCLASSIFIED MODE" & vbCrLf &
                       "This root's eigenvector wasn't clearly dominated by either the longitudinal or lateral-directional state group, " &
                       "so a specific recommendation isn't available. Check the geometry for anything unusual (e.g. a canard, an " &
                       "unconventional tail, or strongly coupled control surfaces) that could mix the two axes together."
        End Select
    End Function

    ' Renders the eigenvalues in the complex plane (real vs imaginary part) -
    ' a standard root-locus plot. Points are colored by stability (stable/
    ' left-half-plane = green, unstable/right-half-plane = red), with a
    ' vertical line marking the Real=0 stability boundary.
    Private Sub RenderModesPlot(Optional captureVectors As Boolean = False)
        If pModes Is Nothing OrElse pModes.Width <= 0 OrElse pModes.Height <= 0 Then Return

        Dim w As Integer = pModes.Width
        Dim h As Integer = pModes.Height
        Dim BMP As New Bitmap(w, h)
        Dim tickFont As Font = GetTickFont(9)
        Dim axisFont As Font = GetAxisFont()
        Dim svgOut As String = ""
        Dim pdfOut As String = ""

        Using G As New SvgGraphics(w, h, Graphics.FromImage(BMP), captureVectors)
            G.SmoothingMode = SmoothingMode.AntiAlias
            G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit
            G.Clear(Color.Black)

            If _lastEigenvalues Is Nothing OrElse _lastEigenvalues.Count = 0 Then
                G.DrawString("Run ""Run Eigenvalue Analysis"" to compute and plot the root locus.", tickFont, Brushes.Gray, New PointF(10, 10))
            Else
                Dim margin As Single = 55
                Dim plotW As Single = w - margin * 2
                Dim plotH As Single = h - margin * 2
                DrawModesSubplot(G, tickFont, axisFont, margin, margin, plotW, plotH)
            End If

            If captureVectors Then
                svgOut = G.GetSvgContent()
                pdfOut = G.GetPdfContentStream()
            End If
        End Using

        Dim oldImage = pModes.Image
        pModes.Image = BMP
        oldImage?.Dispose()

        If captureVectors Then
            modesSvg = svgOut
            modesPdf = pdfOut
        End If
    End Sub

    Private Sub DrawModesSubplot(G As SvgGraphics, tickFont As Font, axisFont As Font,
                                  x As Single, y As Single, w As Single, h As Single)
        Dim xMin As Double = Double.MaxValue, xMax As Double = Double.MinValue
        Dim yMin As Double = Double.MaxValue, yMax As Double = Double.MinValue
        For Each ev In _lastEigenvalues
            xMin = Math.Min(xMin, ev.Real)
            xMax = Math.Max(xMax, ev.Real)
            yMin = Math.Min(yMin, ev.Imag)
            yMax = Math.Max(yMax, ev.Imag)
        Next
        xMin = Math.Min(0, xMin)
        xMax = Math.Max(0, xMax)
        yMin = Math.Min(0, yMin)
        yMax = Math.Max(0, yMax)
        If xMin = xMax Then xMin -= 1 : xMax += 1
        If yMin = yMax Then yMin -= 1 : yMax += 1
        Dim xPad As Double = (xMax - xMin) * 0.15
        Dim yPad As Double = (yMax - yMin) * 0.15
        xMin -= xPad : xMax += xPad
        yMin -= yPad : yMax += yPad

        Dim whitePen As New Pen(Color.White, 1)
        Dim gridPen As New Pen(Color.FromArgb(90, 90, 90), 1) With {.DashStyle = DashStyle.Dash}
        Dim stabilityPen As New Pen(Color.Gold, 1.5F) With {.DashStyle = DashStyle.Dash}

        G.DrawRectangle(whitePen, x, y, w, h)
        G.DrawString("Root Locus  (Real vs Imaginary part of each eigenvalue)", axisFont, Brushes.White, New PointF(x, y - axisFont.Height - 2))

        ' Stability boundary (Real = 0) and Imag = 0 axis
        If xMin < 0 AndAlso xMax > 0 Then
            Dim zeroX As Single = CSng(x + (0 - xMin) / (xMax - xMin) * w)
            G.DrawLine(stabilityPen, zeroX, y, zeroX, y + h)
        End If
        If yMin < 0 AndAlso yMax > 0 Then
            Dim zeroY As Single = CSng(y + h - (0 - yMin) / (yMax - yMin) * h)
            G.DrawLine(gridPen, x, zeroY, x + w, zeroY)
        End If

        Dim stableBrush As New SolidBrush(Color.LimeGreen)
        Dim unstableBrush As New SolidBrush(Color.OrangeRed)
        For Each ev In _lastEigenvalues
            Dim px As Single = CSng(x + (ev.Real - xMin) / (xMax - xMin) * w)
            Dim py As Single = CSng(y + h - (ev.Imag - yMin) / (yMax - yMin) * h)
            ev.ScreenX = px
            ev.ScreenY = py
            Dim brush = If(ev.Real < 0, stableBrush, unstableBrush)
            G.FillEllipse(brush, px - 4, py - 4, 8, 8)
            If Not String.IsNullOrEmpty(ev.ModeLabel) Then
                G.DrawString(ev.ModeLabel, tickFont, Brushes.LightGray, New PointF(px + 6, py - tickFont.Height - 2))
            End If
        Next

        G.DrawString(xMin.ToString("0.0"), tickFont, Brushes.White, New PointF(x, y + h + 2))
        Dim maxLabel = xMax.ToString("0.0")
        Dim maxLabelSize = G.MeasureString(maxLabel, tickFont)
        G.DrawString(maxLabel, tickFont, Brushes.White, New PointF(x + w - maxLabelSize.Width, y + h + 2))
        Dim xAxisLabelSize = G.MeasureString("Real", tickFont)
        G.DrawString("Real", tickFont, Brushes.White, New PointF(x + w / 2 - xAxisLabelSize.Width / 2, y + h + 2))

        Dim yMaxLabel = yMax.ToString("0.0")
        Dim yMaxLabelSize = G.MeasureString(yMaxLabel, tickFont)
        G.DrawString(yMaxLabel, tickFont, Brushes.White, New PointF(x - yMaxLabelSize.Width - 2, y))
        Dim yMinLabel = yMin.ToString("0.0")
        Dim yMinLabelSize = G.MeasureString(yMinLabel, tickFont)
        G.DrawString(yMinLabel, tickFont, Brushes.White, New PointF(x - yMinLabelSize.Width - 2, y + h - yMinLabelSize.Height))

        ' Legend
        Dim legendX As Single = x + w - 110
        Dim legendY As Single = y + 8
        G.FillEllipse(stableBrush, legendX, legendY, 8, 8)
        G.DrawString("stable (Re<0)", tickFont, Brushes.LimeGreen, New PointF(legendX + 12, legendY - 2))
        G.FillEllipse(unstableBrush, legendX, legendY + 16, 8, 8)
        G.DrawString("unstable (Re" & ChrW(&H2265) & "0)", tickFont, Brushes.OrangeRed, New PointF(legendX + 12, legendY + 14))
    End Sub

    Private Sub GeometryToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles btnTest.Click
        If String.IsNullOrEmpty(projectName) Then
            MessageBox.Show("Please enter a project name in the text box at the top before running analysis.", "Missing Project Name", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtName.Focus()
            Return
        End If
        If frmMain.p Is Nothing OrElse frmMain.p.HasExited Then Return
        Dim f = Path.Combine(Application.StartupPath, $"{projectName}.avl")
        With frmMain
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine()
            .p.StandardInput.WriteLine("load " + f)
            .p.StandardInput.WriteLine("oper")
            .p.StandardInput.WriteLine("g")
            .p.StandardInput.WriteLine("k")
            .p.StandardInput.Flush()
        End With
        frmInfo.Show()
    End Sub



    Private Sub txtName_Click(sender As Object, e As EventArgs)

    End Sub

    Private Function GetPlaceholderText() As String
        If frmMain.curApp.ToLower() = "avl" Then
            Return "Enter AVL Project (e.g. glider)"
        Else
            Return "Enter NACA (e.g. 2412) or dat file"
        End If
    End Function

    Private Sub SetPlaceholder()
        If String.IsNullOrEmpty(txtName.Text) OrElse txtName.Text = "Enter AVL Project (e.g. glider)" OrElse txtName.Text = "Enter NACA (e.g. 2412) or dat file" Then
            RemoveHandler txtName.TextChanged, AddressOf txtName_TextChanged
            txtName.Text = GetPlaceholderText()
            txtName.ComboBox.ForeColor = Color.Gray
            AddHandler txtName.TextChanged, AddressOf txtName_TextChanged
        End If
    End Sub

    Private Sub ClearPlaceholder()
        If txtName.Text = "Enter AVL Project (e.g. glider)" OrElse txtName.Text = "Enter NACA (e.g. 2412) or dat file" Then
            RemoveHandler txtName.TextChanged, AddressOf txtName_TextChanged
            txtName.Text = ""
            txtName.ComboBox.ForeColor = Color.Black
            AddHandler txtName.TextChanged, AddressOf txtName_TextChanged
        End If
    End Sub

    Private Sub txtName_TextChanged(sender As Object, e As EventArgs) Handles txtName.TextChanged
        If frmMain.IsSyncingProject Then Return

        Dim txt = txtName.Text
        If txt = "Enter AVL Project (e.g. glider)" OrElse txt = "Enter NACA (e.g. 2412) or dat file" Then
            projectName = ""
        Else
            projectName = txt
        End If

        UpdateGeometryTitle()
        UpdateProjectWarning()

        ' Sync with frmMain
        frmMain.IsSyncingProject = True
        Try
            If frmMain.txtName.Text <> txtName.Text Then
                frmMain.txtName.Text = txtName.Text
            End If
            If txtName.Text = "Enter AVL Project (e.g. glider)" OrElse txtName.Text = "Enter NACA (e.g. 2412) or dat file" Then
                frmMain.txtName.ComboBox.ForeColor = Color.Gray
            Else
                frmMain.txtName.ComboBox.ForeColor = Color.Black
            End If
        Finally
            frmMain.IsSyncingProject = False
        End Try
    End Sub

    Private Sub txtName_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Return Then
            e.SuppressKeyPress = True
            LoadActiveProject()
        End If
    End Sub

    Private Sub txtName_Leave(sender As Object, e As EventArgs)
        LoadActiveProject()
    End Sub

    Private Sub LoadActiveProject()
        If String.IsNullOrEmpty(projectName) Then Return
        If Not My.Settings.autoSave AndAlso isDirty Then
            Dim res = MessageBox.Show($"You have unsaved changes in the {lastActiveTabName} tab. Would you like to save them before switching projects?",
                                      "Unsaved Changes",
                                      MessageBoxButtons.YesNoCancel,
                                      MessageBoxIcon.Warning)
            If res = DialogResult.Yes Then
                Select Case lastActiveTabName
                    Case "Geometry" : SaveAVL()
                    Case "Mass" : SaveMass()
                    Case "Run" : SaveRun()
                End Select
                isDirty = False
                UpdateDirtyWarning()
            ElseIf res = DialogResult.Cancel Then
                txtName.Text = projectName
                Return
            Else
                isDirty = False
                UpdateDirtyWarning()
            End If
        End If

        projectName = txtName.Text

        ' Reload files based on selected tab
        Select Case tc1.SelectedTab.Name
            Case "Geometry"
                LoadAVL()
                btnFitAll_Click(Nothing, Nothing)
            Case "Mass"
                LoadMass()
            Case "Run"
                LoadRun()
        End Select
    End Sub

    Public Sub UpdateGeometryTitle()
        Dim activeFile As String = ""
        If tc1.SelectedTab IsNot Nothing Then
            Select Case tc1.SelectedTab.Name
                Case "Geometry" : activeFile = $"{projectName}.avl"
                Case "Mass" : activeFile = $"{projectName}.mass"
                Case "Run" : activeFile = $"{projectName}.run"
            End Select
        End If

        Dim versionStr As String = My.Application.Info.Version.ToString
        Me.Text = $"Geometry Designer (v{versionStr}) - Active File: {activeFile} (Auto-saved)"
    End Sub

    Public Sub UpdateProjectWarning()
        Dim lbl = TryCast(ToolStrip1.Items("lblWarning"), ToolStripLabel)
        If lbl IsNot Nothing Then
            If String.IsNullOrEmpty(projectName) Then
                lbl.Text = "⚠️ Warning: Project name is empty. Saving, autosaving, and analysis are disabled!"
                lbl.Visible = True
            ElseIf projectName.ToLower() = "test" Then
                lbl.Text = "⚠️ Warning: Using default 'test' project. Changes will be overwritten!"
                lbl.Visible = True
            Else
                lbl.Visible = False
            End If
        End If
    End Sub

    Private Sub ToolTip_Popup(sender As Object, e As PopupEventArgs)
        Dim fontTitle As New Font("Consolas", 11, FontStyle.Bold)
        Dim fontBody As New Font("Consolas", 10, FontStyle.Regular)
        Dim tt = DirectCast(sender, System.Windows.Forms.ToolTip)

        Dim totalSize As Size
        If Not String.IsNullOrEmpty(tt.ToolTipTitle) Then
            Dim titleSize = TextRenderer.MeasureText(tt.ToolTipTitle, fontTitle)
            Dim bodySize = TextRenderer.MeasureText(currentToolTipText, fontBody)
            totalSize = New Size(Math.Max(titleSize.Width, bodySize.Width) + 16, titleSize.Height + bodySize.Height + 20)
        Else
            Dim bodySize = TextRenderer.MeasureText(currentToolTipText, fontBody)
            totalSize = New Size(bodySize.Width + 16, bodySize.Height + 16)
        End If

        e.ToolTipSize = totalSize
    End Sub

    Private Sub ToolTip_Draw(sender As Object, e As DrawToolTipEventArgs)
        Dim g = e.Graphics
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit
        Dim bounds = e.Bounds

        ' Draw background (white card background)
        Using bgBrush As New SolidBrush(Color.White)
            g.FillRectangle(bgBrush, bounds)
        End Using

        ' Draw border (thin dark border)
        Using borderPen As New Pen(Color.FromArgb(60, 60, 60), 1)
            g.DrawRectangle(borderPen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1)
        End Using

        Dim tt = DirectCast(sender, System.Windows.Forms.ToolTip)
        Dim fontTitle As New Font("Consolas", 11, FontStyle.Bold)
        Dim fontBody As New Font("Consolas", 10, FontStyle.Regular)

        Dim textY = bounds.Y + 8
        If Not String.IsNullOrEmpty(tt.ToolTipTitle) Then
            g.DrawString(tt.ToolTipTitle, fontTitle, Brushes.Navy, bounds.X + 8, textY)
            textY += 18
            ' Draw thin line separating title and body
            Using sepPen As New Pen(Color.FromArgb(220, 220, 220), 1)
                g.DrawLine(sepPen, bounds.X + 8, textY, bounds.Right - 8, textY)
            End Using
            textY += 6
        End If

        g.DrawString(e.ToolTipText, fontBody, Brushes.Black, bounds.X + 8, textY)
    End Sub

    Private Sub btnAutosave_Click(sender As Object, e As EventArgs)
        My.Settings.autoSave = Not My.Settings.autoSave
        My.Settings.Save()

        btnAutosave.Text = If(My.Settings.autoSave, "Autosave: On", "Autosave: Off")
        btnAutosave.BackColor = If(My.Settings.autoSave, Color.FromArgb(192, 255, 192), Color.FromArgb(255, 192, 192))

        btnSave.Visible = Not My.Settings.autoSave

        If My.Settings.autoSave Then
            ForceSaveActiveFile(True)
            If Not String.IsNullOrEmpty(projectName) Then
                isDirty = False
                UpdateDirtyWarning()
            End If
        End If
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs)
        ForceSaveActiveFile(False)
        If Not String.IsNullOrEmpty(projectName) Then
            isDirty = False
            UpdateDirtyWarning()
        End If
    End Sub

    Private Sub ForceSaveActiveFile(Optional isAutoSave As Boolean = False)
        If String.IsNullOrEmpty(projectName) Then
            If Not isAutoSave Then
                MessageBox.Show("Please enter a project name in the text box at the top before saving.", "Missing Project Name", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtName.Focus()
            End If
            Return
        End If
        Select Case tc1.SelectedTab.Name
            Case "Geometry" : SaveAVL()
            Case "Mass" : SaveMass()
            Case "Run" : SaveRun()
        End Select
    End Sub

    Private Sub UpdateDirtyWarning()
        If lblDirtyWarning IsNot Nothing Then
            lblDirtyWarning.Visible = (Not My.Settings.autoSave) AndAlso isDirty
        End If
    End Sub

    Private Sub frmGeometry_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        _renderTimer.Stop()
        If _cts IsNot Nothing Then _cts.Cancel()
        If Not My.Settings.autoSave AndAlso isDirty Then
            Dim res = MessageBox.Show("You have unsaved changes. Would you like to save them before closing?",
                                      "Unsaved Changes",
                                      MessageBoxButtons.YesNoCancel,
                                      MessageBoxIcon.Warning)
            If res = DialogResult.Yes Then
                ForceSaveActiveFile()
            ElseIf res = DialogResult.Cancel Then
                e.Cancel = True
            End If
        End If
    End Sub

    Public Sub ApplySyntaxHighlighting()
        If txt3 Is Nothing Then Return

        ' Clear previous highlighting
        With txt3.Range
            .ClearStyle()
            .ClearFoldingMarkers()

            ' 1. AVL (GEOMETRY) KEYWORDS
            Dim avlRegex As String = "(?<![!#].*)(?i)\b(" &
        "Mach|IYsym|IZsym|Zsym|Sref|Cref|Bref|Xref|Yref|Zref|" &
        "Nchordwise|Cspace|Nspanwise|Sspace|" &
        "Xle|Yle|Zle|Chord|Ainc|ANGLE|YDUPLICATE|SCALE|TRANSLATE|" &
        "Cname|Cgain|Xhinge|HingeVec|SgnDup" &
        ")\b"

            ' 2. MASS FILE KEYWORDS
            Dim massRegex As String = "(?<![!#].*)(?i)\b(" &
        "mass|Lunit|Munit|Tunit|g|rho|" &
        "x|y|z|X_cg|Y_cg|Z_cg|" &
        "Ixx|Iyy|Izz|Ixy|Iyz|Izx" &
        ")\b"

            ' 3. RUN CASE KEYWORDS (Standard)
            Dim runStandardRegex As String = "(?<![!#].*)(?i)\b(" &
        "alpha|beta|pb/2V|qc/2V|rb/2V|" &
        "aileron|flap|elevator|rudder|" &
        "CL|CDo|visc|" &
        "bank|elevation|heading|velocity|density|" &
        "CL_a|CL_u|CM_a|CM_u|" &
        "Cl|roll|mom|Cm|pitch|Cn|yaw|" &
        "deg|m/s|m/s^2|kg/m^3|kg-m^2|kg|m" &
        ")\b"

            ' 4. RUN CASE KEYWORDS (Special Dot-Enders)
            Dim runDotRegex As String = "(?<![!#].*)(?i)\b(" &
        "grav\.acc\.|turn_rad\.|load_fac\." &
        ")(?=\s|$)"

            ' Apply Styles
            .SetStyle(blueStyle, avlRegex, RegexOptions.ExplicitCapture)
            .SetStyle(blueStyle, massRegex, RegexOptions.ExplicitCapture)
            .SetStyle(blueStyle, runStandardRegex, RegexOptions.ExplicitCapture)
            .SetStyle(blueStyle, runDotRegex, RegexOptions.ExplicitCapture)

            ' Apply Comment Styles
            .SetStyle(greenStyle, "(?i:\bsurface\b|\bsection\b|\bcontrol\b)", RegexOptions.ExplicitCapture)
            .SetStyle(lightgreenStyle, "(?i:#.*)")
            .SetStyle(lightgreenStyle, "!.*$", RegexOptions.Multiline)

            ' Folding and Blocks
            .SetStyle(ellipseStyle1, "(?i:!beginsurface|!endsurface)")
            .SetStyle(ellipseStyle2, "(?i:!beginsection|!endsection)")
            .SetStyle(ellipseStyle3, "(?i:!begincontrol|!endcontrol)")
            .SetStyle(ellipseStyle4, "(?i:!begingeometry|!endgeometry)")
            .SetStyle(redStyle, "\[[^\]]*\]")

            .SetFoldingMarkers("{", "}")
            .SetFoldingMarkers("!beginsurface\b", "!endsurface\b", RegexOptions.IgnoreCase)
            .SetFoldingMarkers("!beginsection\b", "!endsection\b", RegexOptions.IgnoreCase)
            .SetFoldingMarkers("!begincontrol\b", "!endcontrol\b", RegexOptions.IgnoreCase)
            .SetFoldingMarkers("!begingeometry\b", "!endgeometry\b", RegexOptions.IgnoreCase)
        End With
        txt3.AdjustFolding()
    End Sub

    Private Function GetLeadingWhitespace(text As String) As String
        If String.IsNullOrEmpty(text) Then Return ""
        Dim ws = ""
        For Each c In text
            If Char.IsWhiteSpace(c) Then
                ws &= c
            Else
                Exit For
            End If
        Next
        Return ws
    End Function

    Private Function FormatLineText(lineText As String) As String
        If String.IsNullOrEmpty(lineText) Then Return ""
        Dim spacelen = If(autoSpace, 12, 2)
        Dim foundexclam = False
        Dim pars() As String = lineText.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
        Dim str = ""
        For j = 0 To pars.Count - 1
            If pars(j).Contains("!") Or
                pars(j).Contains("[") Or
                lineText.StartsWith("Run Case", StringComparison.CurrentCultureIgnoreCase) Then
                foundexclam = True
            End If
            If j <> pars.Count - 1 Then
                If foundexclam = False Then
                    If pars(j).Contains("hingevec", StringComparison.CurrentCultureIgnoreCase) Then
                        str += String.Format("{0,-" & (spacelen * 3).ToString & "}", pars(j).Replace(" ", "")) + " "
                    Else
                        If Not pars(j).Contains("visc", StringComparison.CurrentCultureIgnoreCase) Then
                            If j < pars.Count - 2 Then
                                If Not (pars(j + 1).Contains("roll", StringComparison.CurrentCultureIgnoreCase) Or
                                        pars(j + 1).Contains("pitch", StringComparison.CurrentCultureIgnoreCase) Or
                                        pars(j + 1).Contains("yaw", StringComparison.CurrentCultureIgnoreCase) Or
                                        pars(j + 1).Contains("mom", StringComparison.CurrentCultureIgnoreCase)) Then
                                    str += String.Format("{0,-" & (spacelen).ToString & "}", pars(j).Replace(" ", "")) + " "
                                Else
                                    str += pars(j).Replace(" ", "") + " "
                                End If
                            Else
                                str += String.Format("{0,-" & (spacelen).ToString & "}", pars(j).Replace(" ", "")) + " "
                            End If
                        Else
                            str += pars(j).Replace(" ", "") + " "
                        End If
                    End If
                Else
                    str += pars(j).Replace(" ", "") + " "
                End If
            Else
                str += pars(j).Replace(" ", "")
            End If
        Next
        Return str
    End Function

    Public Sub FormatActiveText()
        If (updating = True) OrElse txt3 Is Nothing OrElse txt3.LinesCount = 0 Then Return

        Dim seli = txt3.SelectionStart
        Dim vsv As Integer = txt3.VerticalScroll.Value
        Dim hsv As Integer = txt3.HorizontalScroll.Value
        Dim spacelen = If(autoSpace, 12, 2)

        updating = True
        Dim text = ""
        For i = 0 To txt3.LinesCount - 1
            Dim leadingWS = GetLeadingWhitespace(txt3.Lines(i))
            Dim foundexclam = False
            Dim pars() As String = txt3.Lines(i).Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
            Dim str = ""
            For j = 0 To pars.Count - 1
                If pars(j).Contains("!") Or
                    pars(j).Contains("[") Or
                    txt3.Lines(i).StartsWith("Run Case", StringComparison.CurrentCultureIgnoreCase) Then
                    foundexclam = True
                End If
                If j <> pars.Count - 1 Then
                    If foundexclam = False Then
                        If pars(j).Contains("hingevec", StringComparison.CurrentCultureIgnoreCase) Then
                            str += String.Format("{0,-" & (spacelen * 3).ToString & "}", pars(j).Replace(" ", "")) + " "
                        Else
                            If Not pars(j).Contains("visc", StringComparison.CurrentCultureIgnoreCase) Then
                                If j < pars.Count - 2 Then
                                    If Not (pars(j + 1).Contains("roll", StringComparison.CurrentCultureIgnoreCase) Or
                                            pars(j + 1).Contains("pitch", StringComparison.CurrentCultureIgnoreCase) Or
                                            pars(j + 1).Contains("yaw", StringComparison.CurrentCultureIgnoreCase) Or
                                            pars(j + 1).Contains("mom", StringComparison.CurrentCultureIgnoreCase)) Then
                                        str += String.Format("{0,-" & (spacelen).ToString & "}", pars(j).Replace(" ", "")) + " "
                                    Else
                                        str += pars(j).Replace(" ", "") + " "
                                    End If
                                Else
                                    str += String.Format("{0,-" & (spacelen).ToString & "}", pars(j).Replace(" ", "")) + " "
                                End If
                            Else
                                str += pars(j).Replace(" ", "") + " "
                            End If
                        End If
                    Else
                        str += pars(j).Replace(" ", "") + " "
                    End If
                Else
                    str += pars(j).Replace(" ", "")
                End If
            Next
            text += leadingWS & str & Environment.NewLine
        Next

        ' Always strip the trailing newline the loop appends after the last line.
        ' Without this, a document that already ends with \r\n would accumulate
        ' an extra blank line on every prettify call.
        If text.EndsWith(Environment.NewLine) Then
            text = text.Substring(0, text.Length - Environment.NewLine.Length)
        End If

        If txt3.Text <> text Then
            Dim savedCaret As Place = txt3.Selection.Start

            ' Replace all text via selection to preserve undo history
            txt3.Selection.Start = New Place(0, 0)
            txt3.Selection.End = New Place(txt3.Lines(txt3.LinesCount - 1).Length, txt3.LinesCount - 1)

            Dim savedAutoIndent = txt3.AutoIndent
            txt3.AutoIndent = False
            txt3.InsertText(text)
            txt3.AutoIndent = savedAutoIndent

            ' Recalculate block-level auto-indentation on the selection
            txt3.Selection.Start = New Place(0, 0)
            txt3.Selection.End = New Place(txt3.Lines(txt3.LinesCount - 1).Length, txt3.LinesCount - 1)
            txt3.DoAutoIndent()

            ' Clamp caret position safely
            Dim newCaret = savedCaret
            If newCaret.iLine >= txt3.LinesCount Then
                newCaret.iLine = txt3.LinesCount - 1
            End If
            If newCaret.iLine >= 0 Then
                If newCaret.iChar > txt3.Lines(newCaret.iLine).Length Then
                    newCaret.iChar = txt3.Lines(newCaret.iLine).Length
                End If
            Else
                newCaret = New Place(0, 0)
            End If
            txt3.Selection.Start = newCaret

            txt3.VerticalScroll.Value = Math.Min(vsv, txt3.VerticalScroll.Maximum)
            txt3.HorizontalScroll.Value = Math.Min(hsv, txt3.HorizontalScroll.Maximum)
        End If

        ApplySyntaxHighlighting()
        updating = False
    End Sub

    Private Sub ReplaceEditorLine(lineIndex As Integer, newText As String)
        If txt3 Is Nothing OrElse lineIndex < 0 OrElse lineIndex >= txt3.LinesCount Then Return
        Dim savedCaret = txt3.Selection.Start
        Dim vsv = txt3.VerticalScroll.Value
        Dim hsv = txt3.HorizontalScroll.Value

        txt3.Selection.Start = New Place(0, lineIndex)
        txt3.Selection.End = New Place(txt3.Lines(lineIndex).Length, lineIndex)

        Dim savedAutoIndent = txt3.AutoIndent
        txt3.AutoIndent = False
        txt3.InsertText(newText)
        txt3.AutoIndent = savedAutoIndent

        ' Recalculate block-level auto-indentation on the line selection
        txt3.Selection.Start = New Place(0, lineIndex)
        txt3.Selection.End = New Place(txt3.Lines(lineIndex).Length, lineIndex)
        txt3.DoAutoIndent()

        txt3.Selection.Start = savedCaret
        txt3.VerticalScroll.Value = vsv
        txt3.HorizontalScroll.Value = hsv
    End Sub

    Private Sub btnHelp_Click_1(sender As Object, e As EventArgs) Handles btnHelp.Click

    End Sub

    Private Sub btnHelp_DropDownItemClicked(sender As Object, e As ToolStripItemClickedEventArgs) Handles btnHelp.DropDownItemClicked

        Select Case e.ClickedItem.Text
            Case "Full Help Document"
                'Process.Start(rootPath + "\avl_doc.txt")
                frmHelp.Show()
                frmHelp.txt1.Text = readLines(help, 1, 2388)
            Case "Geometry File (.avl)"
                frmHelp.Show()
                frmHelp.txt1.Text = readLines(help, 215, 1124)
            Case "Mass File (.mass)"
                frmHelp.Show()
                frmHelp.txt1.Text = readLines(help, 1126, 1300)
            Case "Run File (.run)"
                frmHelp.Show()
                frmHelp.txt1.Text = readLines(help, 1302, 1310) + Environment.NewLine + readLines(help, 1993, 2101)
            Case "Program Commands"
                frmHelp.Show()
                frmHelp.txt1.Text = readLines(help, 1312, 2388)

        End Select
    End Sub

    Private Sub btnHelpMass_Click(sender As Object, e As EventArgs) Handles btnHelpMass.Click

    End Sub

    Private Sub btnSpace_Click(sender As Object, e As EventArgs) Handles btnSpace.Click
        If (btnSpace.Text.Contains("On")) Then
            autoSpace = False
            btnSpace.Text = "Auto Space: Off"
            txt3_TextChangedDelayed(sender, New FastColoredTextBoxNS.TextChangedEventArgs(txt3.Range))
        Else
            autoSpace = True
            btnSpace.Text = "Auto Space: On"
            txt3_TextChangedDelayed(sender, New FastColoredTextBoxNS.TextChangedEventArgs(txt3.Range))
        End If
    End Sub

    Private Sub btnHelpAVL_Click(sender As Object, e As EventArgs) Handles btnHelpAVL.Click

    End Sub

    Private Sub btnHelpFull_Click(sender As Object, e As EventArgs) Handles btnHelpFull.Click

    End Sub

    Private Sub txtName_Click_1(sender As Object, e As EventArgs) Handles txtName.Click

    End Sub

    Private Sub btnMass_Click(sender As Object, e As EventArgs) Handles btnMass.Click
        If (btnMass.Text.Contains("On")) Then
            showMass = False
            btnMass.Text = "Mass: Off"
        Else
            showMass = True
            btnMass.Text = "Mass: On"
        End If
        drawAxes()

    End Sub

    Private Sub btnControl_Click(sender As Object, e As EventArgs) Handles btnControl.Click
        If (btnControl.Text.Contains("On")) Then
            showControl = False
            btnControl.Text = "Control: Off"
        Else
            showControl = True
            btnControl.Text = "Control: On"
        End If
        drawAxes()

    End Sub

    Private Sub btnSection_Click(sender As Object, e As EventArgs) Handles btnSection.Click
        If (btnSection.Text.Contains("On")) Then
            showSection = False
            btnSection.Text = "Section: Off"
        Else
            showSection = True
            btnSection.Text = "Section: On"
        End If
        drawAxes()
    End Sub

    Private Sub btnMesh_Click(sender As Object, e As EventArgs) Handles btnMesh.Click
        If (btnMesh.Text.Contains("On")) Then
            showMesh = False
            btnMesh.Text = "Mesh: Off"
            btnMesh.BackColor = Color.FromArgb(220, 220, 220)
        Else
            showMesh = True
            btnMesh.Text = "Mesh: On"
            btnMesh.BackColor = Color.FromArgb(100, 180, 255)
        End If
        drawAxes()
    End Sub

    Private Sub lblNote_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub btn3D_Click(sender As Object, e As EventArgs) Handles btn3D.Click
        If (btn3D.Text.Contains("On")) Then
            show3D = False
            btn3D.Text = "3D: Off"
            Debug.WriteLine("Show 3D is false")
            p3d.Visible = False
            pxy.Visible = True
        Else
            show3D = True
            btn3D.Text = "3D: On"
            Debug.WriteLine("Show 3D is true")
            p3d.Visible = True
            p3d.Dock = DockStyle.Fill
            pxy.Visible = False
            Fit3D()
        End If
        drawAxes()
    End Sub

    Private Sub btnFitAll_Click(sender As Object, e As EventArgs) Handles btnFitAll.Click
        'XY plane========================================================================
        Dim x0 As Integer
        Dim y0 As Integer
        Dim xcount As Integer
        Dim ycount As Integer
        Dim Xs As List(Of Double) = New List(Of Double)
        Dim Ys As List(Of Double) = New List(Of Double)
        Dim dx = 100
        Dim dy = 100
        xoffset = 0
        yoffset = 0

        Dim iter = 0

        Do Until ((dx = 0 And dy = 0) Or iter > 200)

            iter += 1

            gridstep = CInt(pxy.Width / gridnumber)
            x0 = CInt((pxy.Width / 2) + xoffset)
            y0 = CInt((pxy.Height / 2) + yoffset)

            'Draw grids 
            xcount = CInt(pxy.Width / gridstep)
            xmin = (-xcount / 2) - (xoffset / gridstep)
            xmax = xmin + xcount

            gridstep = CInt(pxy.Height / gridnumber)
            ycount = CInt(pxy.Height / gridstep)
            ymin = (-ycount / 2) + (yoffset / gridstep)
            ymax = ymin + ycount

            Xs = New List(Of Double)
            Ys = New List(Of Double)
            For Each p As Node In points
                Dim xscale = p.Point.X * (pxy.Width) / (xmax - xmin) + (pxy.Width / 2) + xoffset
                Dim yscale = -p.Point.Y * (pxy.Height) / (ymax - ymin) + (pxy.Height / 2) + yoffset
                Xs.Add(xscale)
                Ys.Add(yscale)
            Next

            If (Xs.Count = 0) Then Return
            dx = CInt((pxy.Width / 2) - Xs.Average())
            dy = CInt((pxy.Height / 2) - Ys.Average())

            xoffset += dx
            yoffset += dy

            Debug.WriteLine($"iter: {iter} | dx,dy = {dx},{dy} | xoffset,yoffset = {xoffset},{yoffset}")

            For i = 1 To 50
                gridnumber = i
                gridstep = CInt(pxy.Width / gridnumber)
                x0 = CInt((pxy.Width / 2) + xoffset)
                y0 = CInt((pxy.Height / 2) + yoffset)

                'Draw grids 
                xcount = CInt(pxy.Width / gridstep)
                xmin = (-xcount / 2) - (xoffset / gridstep)
                xmax = xmin + xcount

                gridstep = CInt(pxy.Height / gridnumber)
                ycount = CInt(pxy.Height / gridstep)
                ymin = (-ycount / 2) + (yoffset / gridstep)
                ymax = ymin + ycount

                Xs = New List(Of Double)
                Ys = New List(Of Double)
                For Each p As Node In points
                    Dim xscale = p.Point.X * (pxy.Width) / (xmax - xmin) + (pxy.Width / 2) + xoffset
                    Dim yscale = -p.Point.Y * (pxy.Height) / (ymax - ymin) + (pxy.Height / 2) + yoffset
                    Xs.Add(xscale)
                    Ys.Add(yscale)
                Next

                If (Xs.Count = 0) Then
                    Return
                End If

                Dim margin = Math.Min(30, CInt(Math.Min(pxy.Width, pxy.Height) * 0.1))
                If (Xs.Max() <= pxy.Width - margin AndAlso Xs.Min() >= margin AndAlso Ys.Max() <= pxy.Height - margin AndAlso Ys.Min() >= margin) Then
                    Exit For
                End If

            Next

            Debug.WriteLine($"Exited loop for FitAll at scale {gridnumber}")
            Debug.WriteLine($"{Xs.Max()} <= {pxy.Width} And {Xs.Min()} >= 0 And {Ys.Max()} <= {pxy.Height} And {Ys.Min()} >= 0")



        Loop

        drawAxes()


    End Sub

    Private Async Sub tc1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles tc1.SelectedIndexChanged
        If Not My.Settings.autoSave AndAlso isDirty Then
            Dim res = MessageBox.Show($"You have unsaved changes in the {lastActiveTabName} tab. Would you like to save them before switching?",
                                      "Unsaved Changes",
                                      MessageBoxButtons.YesNoCancel,
                                      MessageBoxIcon.Warning)
            If res = DialogResult.Yes Then
                Select Case lastActiveTabName
                    Case "Geometry" : SaveAVL()
                    Case "Mass" : SaveMass()
                    Case "Run" : SaveRun()
                End Select
                isDirty = False
                UpdateDirtyWarning()
            ElseIf res = DialogResult.Cancel Then
                RemoveHandler tc1.SelectedIndexChanged, AddressOf tc1_SelectedIndexChanged
                tc1.SelectedTab = tc1.TabPages(lastActiveTabName)
                AddHandler tc1.SelectedIndexChanged, AddressOf tc1_SelectedIndexChanged
                Return
            Else
                isDirty = False
                UpdateDirtyWarning()
            End If
        End If

        lastActiveTabName = tc1.SelectedTab.Name

        ' Every analysis tab (Trefftz/Loads/Polar/Derivatives/FE/Modes) is built
        ' at Load time while it's NOT the selected tab, i.e. while WinForms
        ' treats it as an invisible control - and an invisible control's docked
        ' children (control-strip Dock=Top, plot PictureBox Dock=Fill) can end
        ' up with stale/zero bounds because layout gets skipped for invisible
        ' subtrees. This is the same bug the structure tree panel had. Force a
        ' fresh layout of whichever tab just became visible so its control
        ' strip and plot never end up overlapping.
        tc1.SelectedTab.PerformLayout()

        ' txt3 (and its floating Add/Prettify/Undo/Redo/Clear action buttons,
        ' which all operate on it) only belongs on the three text-editor tabs -
        ' the analysis tabs (Trefftz, Loads, Polar, etc.) have their own
        ' content and must not have the editor grafted onto them too.
        If tc1.SelectedTab.Name = "Geometry" OrElse tc1.SelectedTab.Name = "Mass" OrElse tc1.SelectedTab.Name = "Run" Then
            If Not tc1.SelectedTab.Controls.Contains(txt3) Then
                tc1.SelectedTab.Controls.Add(txt3)
            End If

            For Each editorBtn As System.Windows.Forms.Control In New System.Windows.Forms.Control() {btnAdd, btnPrettify, btnUndo, btnRedo, btnClear}
                If Not tc1.SelectedTab.Controls.Contains(editorBtn) Then
                    tc1.SelectedTab.Controls.Add(editorBtn)
                End If
                editorBtn.BringToFront()
            Next
        End If

        Select Case tc1.SelectedTab.Name
            Case "Geometry"
                LoadAVL()
                'Debug.WriteLine("fitting all")
                'btnFitAll_Click(Nothing, Nothing)
                If (firstFitAfterLoad) Then
                    Await Task.Delay(txt3.DelayedTextChangedInterval + 100)
                    'MessageBox.Show("Fitting all")
                    btnFitAll_Click(Nothing, Nothing)
                    firstFitAfterLoad = False
                End If
            Case "Mass"
                LoadMass()
            Case "Run"
                LoadRun()
        End Select

        UpdateGeometryTitle()

    End Sub

    Private Sub txtName_SelectedIndexChanged(sender As Object, e As EventArgs) Handles txtName.SelectedIndexChanged
        firstFitAfterLoad = True
        tc1_SelectedIndexChanged(sender, e)
    End Sub

    Private Sub btnHover_Click(sender As Object, e As EventArgs) Handles btnHover.Click
        If (btnHover.Text.Contains("On")) Then
            showHover = False
            btnHover.Text = "Highlight Hover: Off"
        Else
            showHover = True
            btnHover.Text = "Highlight Hover: On"
        End If
        drawAxes()
    End Sub

    Private Sub p3d_MouseDown(sender As Object, e As MouseEventArgs) Handles p3d.MouseDown
        If e.Button = MouseButtons.Left Or e.Button = MouseButtons.Right Or e.Button = MouseButtons.Middle Then
            lastMouseLoc = e.Location
            p3d.Cursor = Cursors.SizeAll
        End If
    End Sub

    Private Sub p3d_MouseMove(sender As Object, e As MouseEventArgs) Handles p3d.MouseMove
        If e.Button = MouseButtons.Left Then
            ' Rotate Alpha/Beta (Existing)
            Dim dx As Single = e.X - lastMouseLoc.X
            Dim dy As Single = e.Y - lastMouseLoc.Y
            viewAlpha += dx * 0.5F
            viewBeta -= dy * 0.5F
            lastMouseLoc = e.Location
            drawAxes()

            'Rotate around X
        ElseIf e.Button = MouseButtons.Right Then
            Dim dx As Single = e.X - lastMouseLoc.X
            viewBeta += dx * 0.5F
            lastMouseLoc = e.Location
            drawAxes()

            'Rotate around Y
        ElseIf e.Button = MouseButtons.Middle Then
            Dim dx As Single = e.X - lastMouseLoc.X
            viewGamma += dx * 0.5F
            lastMouseLoc = e.Location
            drawAxes()
        End If
    End Sub

    Private Sub p3d_MouseUp(sender As Object, e As MouseEventArgs) Handles p3d.MouseUp
        p3d.Cursor = Cursors.Default
    End Sub

    Private Sub p3d_MouseEnter(sender As Object, e As EventArgs) Handles p3d.MouseEnter
        p3d.Focus()
    End Sub

    Private Sub p3d_MouseWheel(sender As Object, e As MouseEventArgs) Handles p3d.MouseWheel
        ' Control Distance (Zoom)
        ' Divide by 120 because e.Delta usually returns +/- 120 per "click"
        Dim scrollAmount As Integer = CInt(e.Delta / 120)

        ' Adjust Zoom speed by multiplying (e.g., * 5)
        viewDist -= scrollAmount

        ' Prevent inverting the view (going behind the camera)
        If viewDist < 2 Then viewDist = 2
        If viewDist > 5 Then viewDist = 5

        drawAxes()
    End Sub
    Private Sub Fit3D()
        ' Find geometric bounds
        Dim geoNodes = points.Where(Function(n) n.type = Node.NodeType.Geometry).ToList()
        If geoNodes.Count = 0 Then Return

        ' Get absolute maximum extent from origin (0,0,0) to ensure rotation fits
        Dim maxExtent As Single = 0
        For Each p As Node In geoNodes
            maxExtent = Math.Max(maxExtent, Math.Abs(p.X))
            maxExtent = Math.Max(maxExtent, Math.Abs(p.Y))
            maxExtent = Math.Max(maxExtent, Math.Abs(p.Z))
        Next

        ' Multiply by 2 because we need to fit the whole diameter (-max to +max)
        Dim objectSize As Single = maxExtent * 2.5F

        ' Screen dimension
        Dim minScreenDim As Single = Math.Min(p3d.Width, p3d.Height)

        ' Calculate distance needed to fit this object size within FOV
        ' viewDist = (ObjectSize * FOV) / ScreenSize
        If minScreenDim > 0 Then
            viewDist = (objectSize * viewFOV) / minScreenDim
        End If

        ' Clamp min distance
        If viewDist < 2 Then viewDist = 2

        ' Reset Angles for a nice ISO view
        viewAlpha = 0 ' Yaw (Rotation around Y)
        viewBeta = 120 ' Pitch (Rotation around X)
        viewGamma = 0 'Roll (Rotation around Z)
        drawAxes()
    End Sub

    Private Sub btnAddProject_Click(sender As Object, e As EventArgs) Handles btnAddProject.Click
        Dim pname = InputBox("Enter new project name:", "New Project", "test")
        If (pname <> "") Then
            projectName = pname
            txtName.Text = projectName
            txtName_SelectedIndexChanged(Nothing, Nothing)
            tc1.SelectedIndex = 0
        End If
    End Sub

    Private Sub InitializeExportButtons()
        AddExportButtonTo(pxy, "XY_Plane")
        AddExportButtonTo(pxz, "XZ_Plane")
        AddExportButtonTo(pyz, "YZ_Plane")
        AddExportButtonTo(p3d, "3D_View")
        AddViewPresetButtonTo(p3d)
    End Sub

    ' Builds the in-house Trefftz/loading-plot tab (spanwise loading, induced drag,
    ' induced angle) so it gets PNG/SVG/PDF export via the same machinery as the
    ' geometry views, instead of relying on AVL's native Trefftz graphics window.
    Private Sub InitializeTrefftzTab()
        If Trefftz IsNot Nothing Then Return

        Trefftz = New TabPage("Trefftz")
        tc1.Controls.Add(Trefftz)

        ' A single 2-row TableLayoutPanel (control strip, plot) instead of a
        ' Dock=Top strip + Dock=Fill plot as SIBLINGS of the TabPage. This tab
        ' is built while it's not the selected tab, and WinForms can leave a
        ' still-invisible control's docked-sibling children with stale/zero
        ' bounds - a table's row layout has no such ordering ambiguity.
        Dim outer As New System.Windows.Forms.TableLayoutPanel()
        outer.Dock = DockStyle.Fill
        outer.ColumnCount = 1
        outer.RowCount = 2
        outer.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(SizeType.Percent, 100.0F))
        outer.RowStyles.Add(New System.Windows.Forms.RowStyle(SizeType.Absolute, 36.0F))
        outer.RowStyles.Add(New System.Windows.Forms.RowStyle(SizeType.Percent, 100.0F))
        Trefftz.Controls.Add(outer)

        Dim controlPanel As New System.Windows.Forms.Panel()
        controlPanel.Dock = DockStyle.Fill
        controlPanel.BackColor = Color.WhiteSmoke

        btnRunTrefftz.Text = "Run Trefftz Plot"
        btnRunTrefftz.Location = New Point(8, 6)
        btnRunTrefftz.Size = New Size(130, 25)
        btnRunTrefftz.FlatStyle = FlatStyle.Flat
        btnRunTrefftz.BackColor = Color.White
        btnRunTrefftz.Cursor = Cursors.Hand
        AddHandler btnRunTrefftz.Click, AddressOf TrefftzPlaneToolStripMenuItem_Click

        controlPanel.Controls.Add(btnRunTrefftz)
        outer.Controls.Add(controlPanel, 0, 0)

        pTrefftz = New PictureBox()
        pTrefftz.BackColor = Color.White
        pTrefftz.BorderStyle = BorderStyle.FixedSingle
        pTrefftz.Dock = DockStyle.Fill
        pTrefftz.Name = "pTrefftz"
        pTrefftz.TabStop = False
        outer.Controls.Add(pTrefftz, 0, 1)

        AddExportButtonToPanel(controlPanel, pTrefftz, "Trefftz_Loading")
        AddHandler pTrefftz.Resize, Sub(s, ev) RenderTrefftzPlot()

        ' The pre-existing toolbar entry for this feature is redundant now that
        ' the tab has its own Run button - strip it from the View dropdown
        ' rather than leave two ways to trigger the same thing.
        ToolStripDropDownButton3.DropDownItems.Remove(btnTrefftz)

        RenderTrefftzPlot()
    End Sub

    ' Builds the spanwise shear/bending-moment tab, driven by AVL's "VM" command.
    Private Sub InitializeLoadsTab()
        If Loads IsNot Nothing Then Return

        Loads = New TabPage("Loads")
        tc1.Controls.Add(Loads)

        Dim outer As New System.Windows.Forms.TableLayoutPanel()
        outer.Dock = DockStyle.Fill
        outer.ColumnCount = 1
        outer.RowCount = 2
        outer.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(SizeType.Percent, 100.0F))
        outer.RowStyles.Add(New System.Windows.Forms.RowStyle(SizeType.Absolute, 36.0F))
        outer.RowStyles.Add(New System.Windows.Forms.RowStyle(SizeType.Percent, 100.0F))
        Loads.Controls.Add(outer)

        Dim controlPanel As New System.Windows.Forms.Panel()
        controlPanel.Dock = DockStyle.Fill
        controlPanel.BackColor = Color.WhiteSmoke

        btnLoads.Text = "Run Shear && Bending Moment"
        btnLoads.Location = New Point(8, 6)
        btnLoads.Size = New Size(190, 25)
        btnLoads.FlatStyle = FlatStyle.Flat
        btnLoads.BackColor = Color.White
        btnLoads.Cursor = Cursors.Hand
        AddHandler btnLoads.Click, AddressOf RunLoadsAnalysis_Click

        controlPanel.Controls.Add(btnLoads)
        outer.Controls.Add(controlPanel, 0, 0)

        pLoads = New PictureBox()
        pLoads.BackColor = Color.White
        pLoads.BorderStyle = BorderStyle.FixedSingle
        pLoads.Dock = DockStyle.Fill
        pLoads.Name = "pLoads"
        pLoads.TabStop = False
        outer.Controls.Add(pLoads, 0, 1)

        AddExportButtonToPanel(controlPanel, pLoads, "Spanwise_Loads")
        AddHandler pLoads.Resize, Sub(s, ev) RenderLoadsPlot()

        RenderLoadsPlot()
    End Sub

    ' Builds the drag-polar tab: a small alpha-min/max/step control strip above
    ' a 4-panel CL-alpha / CD-alpha / Cm-alpha / CL-CD plot, driven by a loop of
    ' "a" -> "a <value>" -> "x" AVL commands (verified sequence - AVL's OPER-level
    ' "a" enters a constraint-select submenu; "a <value>" inside that submenu
    ' sets alpha and returns to OPER) reusing ParseTotalForces per point.
    Private Sub InitializePolarTab()
        If Polar IsNot Nothing Then Return

        Polar = New TabPage("Polar")
        tc1.Controls.Add(Polar)

        Dim outer As New System.Windows.Forms.TableLayoutPanel()
        outer.Dock = DockStyle.Fill
        outer.ColumnCount = 1
        outer.RowCount = 2
        outer.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(SizeType.Percent, 100.0F))
        outer.RowStyles.Add(New System.Windows.Forms.RowStyle(SizeType.Absolute, 36.0F))
        outer.RowStyles.Add(New System.Windows.Forms.RowStyle(SizeType.Percent, 100.0F))
        Polar.Controls.Add(outer)

        Dim controlPanel As New System.Windows.Forms.Panel()
        controlPanel.Dock = DockStyle.Fill
        controlPanel.BackColor = Color.WhiteSmoke

        Dim lblMin As New System.Windows.Forms.Label() With {.Text = "Alpha min:", .AutoSize = True, .Location = New Point(8, 12)}
        txtPolarMin.Text = "-4"
        txtPolarMin.Location = New Point(72, 8)
        txtPolarMin.Width = 40

        Dim lblMax As New System.Windows.Forms.Label() With {.Text = "max:", .AutoSize = True, .Location = New Point(118, 12)}
        txtPolarMax.Text = "10"
        txtPolarMax.Location = New Point(152, 8)
        txtPolarMax.Width = 40

        Dim lblStep As New System.Windows.Forms.Label() With {.Text = "step:", .AutoSize = True, .Location = New Point(198, 12)}
        txtPolarStep.Text = "2"
        txtPolarStep.Location = New Point(234, 8)
        txtPolarStep.Width = 35

        btnRunPolar.Text = "Run Polar Sweep"
        btnRunPolar.Location = New Point(285, 6)
        btnRunPolar.Size = New Size(130, 25)
        btnRunPolar.FlatStyle = FlatStyle.Flat
        btnRunPolar.BackColor = Color.White
        btnRunPolar.Cursor = Cursors.Hand
        AddHandler btnRunPolar.Click, AddressOf RunPolarSweep_Click

        controlPanel.Controls.Add(lblMin)
        controlPanel.Controls.Add(txtPolarMin)
        controlPanel.Controls.Add(lblMax)
        controlPanel.Controls.Add(txtPolarMax)
        controlPanel.Controls.Add(lblStep)
        controlPanel.Controls.Add(txtPolarStep)
        controlPanel.Controls.Add(btnRunPolar)
        outer.Controls.Add(controlPanel, 0, 0)

        pPolar = New PictureBox()
        pPolar.BackColor = Color.White
        pPolar.BorderStyle = BorderStyle.FixedSingle
        pPolar.Dock = DockStyle.Fill
        pPolar.Name = "pPolar"
        pPolar.TabStop = False
        outer.Controls.Add(pPolar, 0, 1)

        AddExportButtonToPanel(controlPanel, pPolar, "Drag_Polar")
        AddHandler pPolar.Resize, Sub(s, ev) RenderPolarPlot()

        RenderPolarPlot()
    End Sub

    ' Builds the stability-derivatives/forces tab: unlike the other tabs this is
    ' tabular/key-value data (ST/SB/FN/FB/HM), not an XY curve, so it's shown as
    ' AVL's own formatted monospace text rather than a custom chart.
    Private Sub InitializeDerivativesTab()
        If Derivatives IsNot Nothing Then Return

        Derivatives = New TabPage("Derivatives")
        tc1.Controls.Add(Derivatives)

        Dim outer As New System.Windows.Forms.TableLayoutPanel()
        outer.Dock = DockStyle.Fill
        outer.ColumnCount = 1
        outer.RowCount = 2
        outer.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(SizeType.Percent, 100.0F))
        outer.RowStyles.Add(New System.Windows.Forms.RowStyle(SizeType.Absolute, 36.0F))
        outer.RowStyles.Add(New System.Windows.Forms.RowStyle(SizeType.Percent, 100.0F))
        Derivatives.Controls.Add(outer)

        Dim controlPanel As New System.Windows.Forms.Panel()
        controlPanel.Dock = DockStyle.Fill
        controlPanel.BackColor = Color.WhiteSmoke

        btnRunDerivatives.Text = "Run Stability && Forces Analysis"
        btnRunDerivatives.Location = New Point(8, 6)
        btnRunDerivatives.Size = New Size(210, 25)
        btnRunDerivatives.FlatStyle = FlatStyle.Flat
        btnRunDerivatives.BackColor = Color.White
        btnRunDerivatives.Cursor = Cursors.Hand
        AddHandler btnRunDerivatives.Click, AddressOf RunDerivatives_Click

        btnExportDerivatives.Text = "Export as Text..."
        btnExportDerivatives.Location = New Point(226, 6)
        btnExportDerivatives.Size = New Size(120, 25)
        btnExportDerivatives.FlatStyle = FlatStyle.Flat
        btnExportDerivatives.BackColor = Color.White
        btnExportDerivatives.Cursor = Cursors.Hand
        AddHandler btnExportDerivatives.Click, AddressOf ExportDerivatives_Click

        controlPanel.Controls.Add(btnRunDerivatives)
        controlPanel.Controls.Add(btnExportDerivatives)
        outer.Controls.Add(controlPanel, 0, 0)

        txtDerivatives.Multiline = True
        txtDerivatives.ReadOnly = True
        txtDerivatives.ScrollBars = System.Windows.Forms.ScrollBars.Both
        txtDerivatives.WordWrap = False
        txtDerivatives.Dock = DockStyle.Fill
        txtDerivatives.Font = New Font(FontFamily.GenericMonospace, 9.5F)
        txtDerivatives.BackColor = Color.Black
        txtDerivatives.ForeColor = Color.White
        txtDerivatives.BorderStyle = BorderStyle.None
        txtDerivatives.Text = "Click ""Run Stability & Forces Analysis"" to compute ST/SB/FN/FB/HM data."
        outer.Controls.Add(txtDerivatives, 0, 1)

    End Sub

    ' Builds the chordwise pressure-distribution tab, driven by AVL's "FE"
    ' (element forces) command. FE gives dCp at every chordwise panel of every
    ' spanwise strip - too much to usefully overlay at once, so this shows one
    ' strip's chordwise dCp distribution at a time via a span-station picker.
    Private Sub InitializeFETab()
        If FE IsNot Nothing Then Return

        FE = New TabPage("Pressure")
        tc1.Controls.Add(FE)

        Dim outer As New System.Windows.Forms.TableLayoutPanel()
        outer.Dock = DockStyle.Fill
        outer.ColumnCount = 1
        outer.RowCount = 2
        outer.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(SizeType.Percent, 100.0F))
        outer.RowStyles.Add(New System.Windows.Forms.RowStyle(SizeType.Absolute, 36.0F))
        outer.RowStyles.Add(New System.Windows.Forms.RowStyle(SizeType.Percent, 100.0F))
        FE.Controls.Add(outer)

        Dim controlPanel As New System.Windows.Forms.Panel()
        controlPanel.Dock = DockStyle.Fill
        controlPanel.BackColor = Color.WhiteSmoke

        btnRunFE.Text = "Run Pressure Analysis"
        btnRunFE.Location = New Point(8, 6)
        btnRunFE.Size = New Size(150, 25)
        btnRunFE.FlatStyle = FlatStyle.Flat
        btnRunFE.BackColor = Color.White
        btnRunFE.Cursor = Cursors.Hand
        AddHandler btnRunFE.Click, AddressOf RunFEAnalysis_Click

        Dim lblStation As New System.Windows.Forms.Label() With {.Text = "Span station:", .AutoSize = True, .Location = New Point(168, 11)}
        cmbFeStrip.Location = New Point(255, 7)
        cmbFeStrip.Width = 220
        cmbFeStrip.DropDownStyle = ComboBoxStyle.DropDownList
        cmbFeStrip.Enabled = False
        AddHandler cmbFeStrip.SelectedIndexChanged, Sub(s, ev) RenderFEPlot()

        controlPanel.Controls.Add(btnRunFE)
        controlPanel.Controls.Add(lblStation)
        controlPanel.Controls.Add(cmbFeStrip)
        outer.Controls.Add(controlPanel, 0, 0)

        pFE = New PictureBox()
        pFE.BackColor = Color.White
        pFE.BorderStyle = BorderStyle.FixedSingle
        pFE.Dock = DockStyle.Fill
        pFE.Name = "pFE"
        pFE.TabStop = False
        outer.Controls.Add(pFE, 0, 1)

        AddExportButtonToPanel(controlPanel, pFE, "Pressure_Distribution")
        AddHandler pFE.Resize, Sub(s, ev) RenderFEPlot()

        RenderFEPlot()
    End Sub

    ' Builds the eigenvalue/root-locus tab, driven by AVL's top-level ".MODE"
    ' menu (outside OPER - dynamic-stability eigenmode analysis). Meaningful
    ' results require mass/inertia data (uses {project}.mass if present) and a
    ' trimmed run case with a real velocity (uses {project}.run if present) -
    ' without those AVL still computes something using placeholder mass=1kg,
    ' Ixx=Iyy=Izz=1 (confirmed against real AVL), so a warning is shown instead
    ' of blocking the run.
    Private Sub InitializeModesTab()
        If ModesTab IsNot Nothing Then Return

        ModesTab = New TabPage("Dynamics")
        tc1.Controls.Add(ModesTab)

        Dim outer As New System.Windows.Forms.TableLayoutPanel()
        outer.Dock = DockStyle.Fill
        outer.ColumnCount = 1
        outer.RowCount = 2
        outer.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(SizeType.Percent, 100.0F))
        outer.RowStyles.Add(New System.Windows.Forms.RowStyle(SizeType.Absolute, 36.0F))
        outer.RowStyles.Add(New System.Windows.Forms.RowStyle(SizeType.Percent, 100.0F))
        ModesTab.Controls.Add(outer)

        Dim controlPanel As New System.Windows.Forms.Panel()
        controlPanel.Dock = DockStyle.Fill
        controlPanel.BackColor = Color.WhiteSmoke

        btnRunModes.Text = "Run Eigenvalue Analysis"
        btnRunModes.Location = New Point(8, 6)
        btnRunModes.Size = New Size(170, 25)
        btnRunModes.FlatStyle = FlatStyle.Flat
        btnRunModes.BackColor = Color.White
        btnRunModes.Cursor = Cursors.Hand
        AddHandler btnRunModes.Click, AddressOf RunModesAnalysis_Click

        btnModeTips.Text = "Tips to Improve Stability"
        btnModeTips.Location = New Point(186, 6)
        btnModeTips.Size = New Size(170, 25)
        btnModeTips.FlatStyle = FlatStyle.Flat
        btnModeTips.BackColor = Color.White
        btnModeTips.Cursor = Cursors.Hand
        AddHandler btnModeTips.Click, AddressOf ShowModeStabilityTips_Click

        Dim lblHint As New System.Windows.Forms.Label() With {
            .Text = "Needs a saved Mass tab and a trimmed Run case (with velocity) for meaningful results.",
            .AutoSize = True,
            .ForeColor = Color.DimGray,
            .Location = New Point(364, 11)
        }

        controlPanel.Controls.Add(btnRunModes)
        controlPanel.Controls.Add(btnModeTips)
        controlPanel.Controls.Add(lblHint)
        outer.Controls.Add(controlPanel, 0, 0)

        pModes = New PictureBox()
        pModes.BackColor = Color.White
        pModes.BorderStyle = BorderStyle.FixedSingle
        pModes.Dock = DockStyle.Fill
        pModes.Name = "pModes"
        pModes.TabStop = False
        outer.Controls.Add(pModes, 0, 1)

        AddExportButtonToPanel(controlPanel, pModes, "Root_Locus")
        AddHandler pModes.Resize, Sub(s, ev) RenderModesPlot()
        AddHandler pModes.MouseMove, AddressOf pModes_MouseMove
        AddHandler pModes.MouseLeave, Sub(s, ev) modesTip.Hide(pModes)

        RenderModesPlot()
    End Sub

    ' Shows mode details (name, eigenvalue, natural frequency, damping ratio,
    ' period) when hovering near a plotted root - ScreenX/ScreenY are stamped
    ' onto each EigenValue by DrawModesSubplot every render.
    Private Sub pModes_MouseMove(sender As Object, e As MouseEventArgs)
        If _lastEigenvalues Is Nothing OrElse _lastEigenvalues.Count = 0 Then Return

        Dim nearest As EigenValue = Nothing
        Dim nearestDist As Double = Double.MaxValue
        Dim nearestIndex As Integer = -1
        For i = 0 To _lastEigenvalues.Count - 1
            Dim ev = _lastEigenvalues(i)
            Dim dx = e.X - ev.ScreenX
            Dim dy = e.Y - ev.ScreenY
            Dim dist = Math.Sqrt(dx * dx + dy * dy)
            If dist < nearestDist Then
                nearestDist = dist
                nearest = ev
                nearestIndex = i
            End If
        Next

        If nearest Is Nothing OrElse nearestDist > 8 Then
            If _lastModesHoverIndex <> -1 Then
                modesTip.Hide(pModes)
                _lastModesHoverIndex = -1
            End If
            Return
        End If

        If nearestIndex = _lastModesHoverIndex Then Return
        _lastModesHoverIndex = nearestIndex

        Dim omega = Math.Sqrt(nearest.Real * nearest.Real + nearest.Imag * nearest.Imag)
        Dim zeta = If(omega > 0, -nearest.Real / omega, 0.0)
        Dim lines As New List(Of String)
        lines.Add(If(String.IsNullOrEmpty(nearest.ModeLabel), "Unclassified mode", nearest.ModeLabel))
        lines.Add($"Eigenvalue: {nearest.Real:0.###} {If(nearest.Imag >= 0, "+", "-")} {Math.Abs(nearest.Imag):0.###}i")
        lines.Add($"Natural freq (ωn): {omega:0.###} rad/s")
        lines.Add($"Damping ratio (ζ): {zeta:0.###}")
        If Math.Abs(nearest.Imag) > 0.001 Then
            lines.Add($"Period: {2 * Math.PI / Math.Abs(nearest.Imag):0.###} s")
        End If
        lines.Add(If(nearest.Real < 0, "Stable", "Unstable"))

        modesTip.Show(String.Join(vbCrLf, lines), pModes, e.X + 12, e.Y + 12, 6000)
    End Sub

    ' Adds a "Load Test Project" toolbar button next to "New Project" that
    ' writes a ready-made .avl/.mass/.run trio exercising every analysis tab
    ' (Trefftz, Loads, Polar, Derivatives, Pressure, Dynamics): a 3-surface
    ' aircraft (tapered/swept/dihedral cambered wing with aileron, tail with
    ' elevator, fin with rudder), a mass breakdown, and a trimmed run case.
    ' Verified against real AVL 3.37 - every command (FS/VM/ST/SB/FN/FB/HM/
    ' FE/MODE) returns real non-empty data for this geometry.
    Private Sub InitializeTestProjectButton()
        btnLoadTestProject.DisplayStyle = ToolStripItemDisplayStyle.Text
        btnLoadTestProject.Text = "Load Test Project"
        AddHandler btnLoadTestProject.Click, AddressOf LoadTestProject_Click

        Dim insertAt = ToolStrip1.Items.IndexOf(btnAddProject)
        If insertAt >= 0 Then
            ToolStrip1.Items.Insert(insertAt + 1, btnLoadTestProject)
        Else
            ToolStrip1.Items.Add(btnLoadTestProject)
        End If
    End Sub

    Private Sub LoadTestProject_Click(sender As Object, e As EventArgs)
        Const pname As String = "TestAircraft"
        Dim avlPath = Path.Combine(Application.StartupPath, $"{pname}.avl")
        Dim massPath = Path.Combine(Application.StartupPath, $"{pname}.mass")
        Dim runPath = Path.Combine(Application.StartupPath, $"{pname}.run")

        If File.Exists(avlPath) OrElse File.Exists(massPath) OrElse File.Exists(runPath) Then
            Dim res = MessageBox.Show(
                $"A project named ""{pname}"" already exists and will be overwritten with the test project." & vbCrLf & vbCrLf & "Continue?",
                "Load Test Project", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            If res = DialogResult.No Then Return
        End If

        Try
            File.WriteAllText(avlPath, TestProjectAvlText())
            File.WriteAllText(massPath, TestProjectMassText())
            File.WriteAllText(runPath, TestProjectRunText())
        Catch ex As Exception
            MessageBox.Show("Error creating test project files: " & ex.Message, "Load Test Project", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End Try

        projectName = pname
        txtName.Text = projectName
        txtName_SelectedIndexChanged(Nothing, Nothing)
        tc1.SelectedIndex = 0

        MessageBox.Show(
            "Test project ""TestAircraft"" created: a 3-surface aircraft (wing+aileron, tail+elevator, fin+rudder) " &
            "with a mass breakdown and a trimmed run case." & vbCrLf & vbCrLf &
            "It's set up to exercise every analysis tab - Trefftz, Loads, Polar, Derivatives, Pressure, and Dynamics.",
            "Test Project Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Function TestProjectAvlText() As String
        Return String.Join(vbCrLf, New String() {
            "[Test Aircraft]",
            "#Mach",
            "0.0",
            "#IYsym   IZsym   Zsym",
            "0        0       0.0",
            "#Sref    Cref    Bref",
            "7.2      0.9     8.0",
            "#Xref    Yref    Zref",
            "0.87     0.0     0.0",
            "!begingeometry",
            "#====================================================================",
            "SURFACE",
            "[Wing]",
            "!beginsurface",
            "#Nchordwise  Cspace   Nspanwise   Sspace",
            "10           1.0      16          1.0",
            "#",
            "YDUPLICATE",
            "0.0",
            "#",
            "ANGLE",
            "0.0",
            "#-------------------------------------------------------------",
            "SECTION",
            "!beginsection",
            "#Xle    Yle    Zle     Chord   Ainc  Nspanwise  Sspace",
            "0.0     0.0    0.0     1.2     0.0   0          0",
            "NACA",
            "4412",
            "!endsection",
            "#-------------------------------------------------------------",
            "SECTION",
            "!beginsection",
            "#Xle    Yle    Zle     Chord   Ainc  Nspanwise  Sspace",
            "0.21    2.8    0.245   0.78    -2.1  0          0",
            "NACA",
            "4412",
            "!endsection",
            "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++",
            "CONTROL",
            "!begincontrol",
            "#Cname   Cgain  Xhinge  HingeVec     SgnDup",
            "aileron  1.0    0.75    0.0 0.0 0.0  -1.0",
            "!endcontrol",
            "#-------------------------------------------------------------",
            "SECTION",
            "!beginsection",
            "#Xle    Yle    Zle     Chord   Ainc  Nspanwise  Sspace",
            "0.30    4.0    0.35    0.6     -3.0  0          0",
            "NACA",
            "4412",
            "!endsection",
            "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++",
            "CONTROL",
            "!begincontrol",
            "#Cname   Cgain  Xhinge  HingeVec     SgnDup",
            "aileron  1.0    0.75    0.0 0.0 0.0  -1.0",
            "!endcontrol",
            "!endsurface",
            "#====================================================================",
            "SURFACE",
            "[Htail]",
            "!beginsurface",
            "#Nchordwise  Cspace   Nspanwise   Sspace",
            "8            1.0      8           1.0",
            "#",
            "YDUPLICATE",
            "0.0",
            "#",
            "ANGLE",
            "0.0",
            "#-------------------------------------------------------------",
            "SECTION",
            "!beginsection",
            "#Xle    Yle    Zle     Chord   Ainc  Nspanwise  Sspace",
            "3.5     0.0    0.1     0.5     0.0   0          0",
            "NACA",
            "0012",
            "!endsection",
            "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++",
            "CONTROL",
            "!begincontrol",
            "#Cname    Cgain  Xhinge  HingeVec     SgnDup",
            "elevator  1.0    0.6     0.0 0.0 0.0  1.0",
            "!endcontrol",
            "#-------------------------------------------------------------",
            "SECTION",
            "!beginsection",
            "#Xle    Yle    Zle     Chord   Ainc  Nspanwise  Sspace",
            "3.5     1.4    0.1     0.5     0.0   0          0",
            "NACA",
            "0012",
            "!endsection",
            "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++",
            "CONTROL",
            "!begincontrol",
            "#Cname    Cgain  Xhinge  HingeVec     SgnDup",
            "elevator  1.0    0.6     0.0 0.0 0.0  1.0",
            "!endcontrol",
            "!endsurface",
            "#====================================================================",
            "SURFACE",
            "[Vtail]",
            "!beginsurface",
            "#Nchordwise  Cspace   Nspanwise   Sspace",
            "8            1.0      8           1.0",
            "#",
            "ANGLE",
            "0.0",
            "#-------------------------------------------------------------",
            "SECTION",
            "!beginsection",
            "#Xle    Yle    Zle     Chord   Ainc  Nspanwise  Sspace",
            "3.5     0.0    0.1     0.5     0.0   0          0",
            "NACA",
            "0012",
            "!endsection",
            "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++",
            "CONTROL",
            "!begincontrol",
            "#Cname   Cgain  Xhinge  HingeVec     SgnDup",
            "rudder   1.0    0.6     0.0 0.0 0.0  1.0",
            "!endcontrol",
            "#-------------------------------------------------------------",
            "SECTION",
            "!beginsection",
            "#Xle    Yle    Zle     Chord   Ainc  Nspanwise  Sspace",
            "3.7     0.0    1.3     0.3     0.0   0          0",
            "NACA",
            "0012",
            "!endsection",
            "#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++",
            "CONTROL",
            "!begincontrol",
            "#Cname   Cgain  Xhinge  HingeVec     SgnDup",
            "rudder   1.0    0.6     0.0 0.0 0.0  1.0",
            "!endcontrol",
            "!endsurface",
            "!endgeometry",
            ""
        })
    End Function

    Private Function TestProjectMassText() As String
        Return String.Join(vbCrLf, New String() {
            "#[Test Aircraft]",
            "!x,y,z cordinate system matches AVL default",
            "Lunit = 1.0 m",
            "Munit = 1.0 kg",
            "Tunit = 1.0 s",
            "#-------------------------",
            "g = 9.81",
            "rho = 1.225",
            "#-------------------------",
            "#mass   x       y      z      Ixx    Iyy    Izz",
            "5.0     0.3     0.0    0.0    2.0    0.6    2.5   !wing structure",
            "1.5     3.5     0.0    0.1    0.05   0.15   0.2   !tail structure",
            "3.0     0.5     0.0    -0.05  0.02   0.3    0.32  !fuselage + payload",
            ""
        })
    End Function

    Private Function TestProjectRunText() As String
        Return String.Join(vbCrLf, New String() {
            " ---------------------------------------------",
            " Run case  1:  Cruise",
            "",
            " alpha        ->  alpha       =   3.00000",
            " beta         ->  beta        =   0.00000",
            " pb/2V        ->  pb/2V       =   0.00000",
            " qc/2V        ->  qc/2V       =   0.00000",
            " rb/2V        ->  rb/2V       =   0.00000",
            " aileron      ->  Cl roll mom =   0.00000",
            " elevator     ->  Cm pitchmom =   0.00000",
            " rudder       ->  Cn yaw  mom =   0.00000",
            "",
            " alpha     =   3.00000     deg",
            " beta      =   0.00000     deg",
            " pb/2V     =   0.00000",
            " qc/2V     =   0.00000",
            " rb/2V     =   0.00000",
            " CL        =   0.00000",
            " CDo       =   0.02000",
            " bank      =   0.00000     deg",
            " elevation =   0.00000     deg",
            " heading   =   0.00000     deg",
            " Mach      =   0.00000",
            " velocity  =  18.00000     m/s",
            " density   =   1.22500     kg/m^3",
            " grav.acc. =   9.81000     m/s^2",
            " turn_rad. =   0.00000     m",
            " load_fac. =   0.00000",
            " X_cg      =   0.87000     m",
            " Y_cg      =   0.00000     m",
            " Z_cg      =   0.05000     m",
            " mass      =   9.50000     kg",
            " Ixx       =   2.50000     kg-m^2",
            " Iyy       =   3.20000     kg-m^2",
            " Izz       =   5.10000     kg-m^2",
            " Ixy       =   0.00000     kg-m^2",
            " Iyz       =   0.00000     kg-m^2",
            " Izx       =   0.00000     kg-m^2",
            " visc CL_a =   0.00000",
            " visc CL_u =   0.00000",
            " visc CM_a =   0.00000",
            " visc CM_u =   0.00000",
            ""
        })
    End Function

    Private Sub AddExportButtonTo(pb As PictureBox, viewName As String)
        Dim btnExport As New System.Windows.Forms.Button()
        btnExport.Text = "Export ▾"
        btnExport.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular)
        btnExport.BackColor = Color.White
        btnExport.ForeColor = Color.Black
        btnExport.FlatStyle = FlatStyle.Flat
        btnExport.FlatAppearance.BorderSize = 1
        btnExport.FlatAppearance.BorderColor = Color.LightGray
        btnExport.Size = New Size(75, 25)
        btnExport.Location = New Point(Math.Max(pb.Width, 160) - 85, 10)
        btnExport.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnExport.Cursor = Cursors.Hand

        Dim menu As New ContextMenuStrip()

        Dim pngItem = New ToolStripMenuItem("Export as PNG...", Nothing, Sub(s, ev) ExportView(pb, "PNG", viewName))
        Dim svgItem = New ToolStripMenuItem("Export as SVG...", Nothing, Sub(s, ev) ExportView(pb, "SVG", viewName))
        Dim pdfItem = New ToolStripMenuItem("Export as PDF...", Nothing, Sub(s, ev) ExportView(pb, "PDF", viewName))

        menu.Items.Add(pngItem)
        menu.Items.Add(svgItem)
        menu.Items.Add(pdfItem)

        AddHandler btnExport.Click, Sub(s, ev)
                                        menu.Show(btnExport, New Point(0, btnExport.Height))
                                    End Sub

        pb.Controls.Add(btnExport)
        btnExport.BringToFront()
    End Sub

    ' Same export menu as AddExportButtonTo, but placed as a normal docked
    ' child of the tab's top control panel instead of floating on top of the
    ' PictureBox. Anchored to the panel's right edge and actively repositioned
    ' on every panel resize (rather than a fixed X guessed at creation time),
    ' so it can't end up hidden behind other controls or off-panel regardless
    ' of layout timing or how wide any auto-sized labels in the panel turn out
    ' to be (e.g. the Dynamics tab's hint text).
    Private Sub AddExportButtonToPanel(panel As System.Windows.Forms.Panel, pb As PictureBox, viewName As String)
        Dim btnExport As New System.Windows.Forms.Button()
        btnExport.Text = "Export ▾"
        btnExport.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular)
        btnExport.BackColor = Color.White
        btnExport.ForeColor = Color.Black
        btnExport.FlatStyle = FlatStyle.Flat
        btnExport.FlatAppearance.BorderSize = 1
        btnExport.FlatAppearance.BorderColor = Color.LightGray
        btnExport.Size = New Size(75, 25)
        btnExport.Top = 6
        btnExport.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnExport.Cursor = Cursors.Hand

        Dim menu As New ContextMenuStrip()

        Dim pngItem = New ToolStripMenuItem("Export as PNG...", Nothing, Sub(s, ev) ExportView(pb, "PNG", viewName))
        Dim svgItem = New ToolStripMenuItem("Export as SVG...", Nothing, Sub(s, ev) ExportView(pb, "SVG", viewName))
        Dim pdfItem = New ToolStripMenuItem("Export as PDF...", Nothing, Sub(s, ev) ExportView(pb, "PDF", viewName))

        menu.Items.Add(pngItem)
        menu.Items.Add(svgItem)
        menu.Items.Add(pdfItem)

        AddHandler btnExport.Click, Sub(s, ev)
                                        menu.Show(btnExport, New Point(0, btnExport.Height))
                                    End Sub

        Dim reposition = Sub()
                              btnExport.Left = Math.Max(0, panel.ClientSize.Width - btnExport.Width - 10)
                          End Sub
        AddHandler panel.Resize, Sub(s, ev) reposition()

        panel.Controls.Add(btnExport)
        btnExport.BringToFront()
        reposition()
    End Sub

    Private Sub AddViewPresetButtonTo(pb As PictureBox)
        Dim btnView As New System.Windows.Forms.Button()
        btnView.Text = "View ▾"
        btnView.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular)
        btnView.BackColor = Color.White
        btnView.ForeColor = Color.Black
        btnView.FlatStyle = FlatStyle.Flat
        btnView.FlatAppearance.BorderSize = 1
        btnView.FlatAppearance.BorderColor = Color.LightGray
        btnView.Size = New Size(65, 25)
        btnView.Location = New Point(pb.Width - 155, 10)
        btnView.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnView.Cursor = Cursors.Hand

        Dim menu As New ContextMenuStrip()

        Dim isoItem = New ToolStripMenuItem("Isometric", Nothing, Sub(s, ev) Set3DView(35, 120, -20))
        Dim defaultItem = New ToolStripMenuItem("Default", Nothing, Sub(s, ev) Set3DView(0, 120, 0))
        Dim sep = New ToolStripSeparator()
        Dim frontItem = New ToolStripMenuItem("Front", Nothing, Sub(s, ev) Set3DView(90, 0, -90))
        Dim backItem = New ToolStripMenuItem("Back", Nothing, Sub(s, ev) Set3DView(-90, 0, 90))
        Dim leftItem = New ToolStripMenuItem("Left", Nothing, Sub(s, ev) Set3DView(180, -90, 0))
        Dim rightItem = New ToolStripMenuItem("Right", Nothing, Sub(s, ev) Set3DView(0, 90, 0))
        Dim topItem = New ToolStripMenuItem("Top", Nothing, Sub(s, ev) Set3DView(0, 180, 0))
        Dim bottomItem = New ToolStripMenuItem("Bottom", Nothing, Sub(s, ev) Set3DView(0, 0, 0))

        menu.Items.Add(isoItem)
        menu.Items.Add(defaultItem)
        menu.Items.Add(sep)
        menu.Items.Add(frontItem)
        menu.Items.Add(backItem)
        menu.Items.Add(leftItem)
        menu.Items.Add(rightItem)
        menu.Items.Add(topItem)
        menu.Items.Add(bottomItem)

        AddHandler btnView.Click, Sub(s, ev)
                                      menu.Show(btnView, New Point(0, btnView.Height))
                                  End Sub

        pb.Controls.Add(btnView)
    End Sub

    Private Sub Set3DView(alpha As Single, beta As Single, gamma As Single)
        viewAlpha = alpha
        viewBeta = beta
        viewGamma = gamma
        drawAxes()
    End Sub

    ' Triggers a render that captures SVG/PDF vector data for export.
    ' Waits for it to complete before returning.
    Private Async Function drawAxesCapture() As System.Threading.Tasks.Task
        _captureVectors = True
        _renderPending = False
        If _cts IsNot Nothing Then _cts.Cancel()
        Try
            Await Task.Delay(50)  ' let any in-flight render finish
        Catch
        End Try
        _cts = New CancellationTokenSource()
        Dim token = _cts.Token
        Try
            Await Task.Run(Sub() HeavyRender(Nothing, token))
        Catch
        Finally
            If _cts IsNot Nothing Then _cts.Dispose()
            _cts = Nothing
            _captureVectors = False
        End Try
    End Function

    Private Async Sub ExportView(pb As PictureBox, format As String, defaultName As String)
        Dim svgContent As String = ""
        Dim pdfContent As String = ""

        If pb Is pTrefftz Then
            If format <> "PNG" Then RenderTrefftzPlot(True)
            svgContent = trefftzSvg
            pdfContent = trefftzPdf
        ElseIf pb Is pLoads Then
            If format <> "PNG" Then RenderLoadsPlot(True)
            svgContent = loadsSvg
            pdfContent = loadsPdf
        ElseIf pb Is pPolar Then
            If format <> "PNG" Then RenderPolarPlot(True)
            svgContent = polarSvg
            pdfContent = polarPdf
        ElseIf pb Is pFE Then
            If format <> "PNG" Then RenderFEPlot(True)
            svgContent = feSvg
            pdfContent = fePdf
        ElseIf pb Is pModes Then
            If format <> "PNG" Then RenderModesPlot(True)
            svgContent = modesSvg
            pdfContent = modesPdf
        Else
            If format <> "PNG" Then
                Await drawAxesCapture()
            End If

            If pb Is pxy Then
                svgContent = pxySvg
                pdfContent = pxyPdf
            ElseIf pb Is pxz Then
                svgContent = pxzSvg
                pdfContent = pxzPdf
            ElseIf pb Is pyz Then
                svgContent = pyzSvg
                pdfContent = pyzPdf
            ElseIf pb Is p3d Then
                svgContent = p3dSvg
                pdfContent = p3dPdf
            End If
        End If

        If format = "PNG" Then
            If pb.Image Is Nothing Then
                MessageBox.Show("There is no image to export.", "Export View", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
        Else
            If String.IsNullOrEmpty(svgContent) Then
                MessageBox.Show("The view has not finished rendering. Please wait a moment and try again.", "Export View", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
        End If

        Dim sfd As New SaveFileDialog()
        sfd.Title = $"Export {defaultName} as {format}"

        Dim projName As String = txtName.Text.Trim()
        If String.IsNullOrEmpty(projName) OrElse projName.Contains("Enter AVL Project") OrElse projName.Contains("Enter NACA") Then
            sfd.FileName = defaultName
        Else
            For Each c In Path.GetInvalidFileNameChars()
                projName = projName.Replace(c, "_"c)
            Next
            sfd.FileName = $"{projName}_{defaultName}"
        End If

        Select Case format
            Case "PNG"
                sfd.Filter = "PNG Image (*.png)|*.png"
                sfd.DefaultExt = "png"
            Case "SVG"
                sfd.Filter = "SVG Image (*.svg)|*.svg"
                sfd.DefaultExt = "svg"
            Case "PDF"
                sfd.Filter = "PDF Document (*.pdf)|*.pdf"
                sfd.DefaultExt = "pdf"
        End Select

        If sfd.ShowDialog() = DialogResult.OK Then
            Try
                Select Case format
                    Case "PNG"
                        Dim imgCopy As System.Drawing.Image = Nothing
                        SyncLock pb
                            If pb.Image IsNot Nothing Then
                                imgCopy = CType(pb.Image.Clone(), System.Drawing.Image)
                            End If
                        End SyncLock
                        If imgCopy IsNot Nothing Then
                            imgCopy.Save(sfd.FileName, ImageFormat.Png)
                            imgCopy.Dispose()
                        Else
                            Throw New Exception("Failed to clone picture box image.")
                        End If
                    Case "SVG"
                        File.WriteAllText(sfd.FileName, svgContent, Encoding.UTF8)
                    Case "PDF"
                        WriteVectorPdf(pdfContent, pb.Width, pb.Height, sfd.FileName)
                End Select

                MessageBox.Show($"{format} exported successfully to:" & vbCrLf & sfd.FileName, "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show("Error exporting file: " & ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End If
    End Sub

    Private Sub WriteVectorPdf(pdfContentStream As String, width As Double, height As Double, outputPath As String)
        Dim pdfWidth As Double = width * 72.0 / 96.0
        Dim pdfHeight As Double = height * 72.0 / 96.0

        Using fs As New FileStream(outputPath, FileMode.Create, FileAccess.Write)
            Using writer As New StreamWriter(fs, Encoding.ASCII)
                Dim offsets As New List(Of Long)()

                writer.Write("%PDF-1.4" & vbLf)

                writer.Flush()
                offsets.Add(fs.Position)
                writer.Write("1 0 obj" & vbLf & "<< /Type /Catalog /Pages 2 0 R >>" & vbLf & "endobj" & vbLf)

                writer.Flush()
                offsets.Add(fs.Position)
                writer.Write("2 0 obj" & vbLf & "<< /Type /Pages /Kids [ 3 0 R ] /Count 1 >>" & vbLf & "endobj" & vbLf)

                writer.Flush()
                offsets.Add(fs.Position)
                Dim wStr As String = pdfWidth.ToString(System.Globalization.CultureInfo.InvariantCulture)
                Dim hStr As String = pdfHeight.ToString(System.Globalization.CultureInfo.InvariantCulture)
                writer.Write("3 0 obj" & vbLf & $"<< /Type /Page /Parent 2 0 R /MediaBox [ 0 0 {wStr} {hStr} ] /Resources << /Font << /F1 5 0 R /F2 6 0 R /F3 7 0 R /F4 8 0 R >> >> /Contents 4 0 R >>" & vbLf & "endobj" & vbLf)

                Dim contentBytes As Byte() = Encoding.ASCII.GetBytes(pdfContentStream)
                writer.Flush()
                offsets.Add(fs.Position)
                writer.Write("4 0 obj" & vbLf & $"<< /Length {contentBytes.Length} >>" & vbLf & "stream" & vbLf)
                writer.Flush()
                fs.Write(contentBytes, 0, contentBytes.Length)
                writer.Write(vbLf & "endstream" & vbLf & "endobj" & vbLf)

                writer.Flush()
                offsets.Add(fs.Position)
                writer.Write("5 0 obj" & vbLf & "<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>" & vbLf & "endobj" & vbLf)

                writer.Flush()
                offsets.Add(fs.Position)
                writer.Write("6 0 obj" & vbLf & "<< /Type /Font /Subtype /Type1 /BaseFont /Courier-Bold >>" & vbLf & "endobj" & vbLf)

                writer.Flush()
                offsets.Add(fs.Position)
                writer.Write("7 0 obj" & vbLf & "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>" & vbLf & "endobj" & vbLf)

                writer.Flush()
                offsets.Add(fs.Position)
                writer.Write("8 0 obj" & vbLf & "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>" & vbLf & "endobj" & vbLf)

                writer.Flush()
                Dim startXref As Long = fs.Position
                writer.Write("xref" & vbLf & "0 9" & vbLf & "0000000000 65535 f " & vbLf)
                For Each offset In offsets
                    writer.Write(offset.ToString("D10") & " 00000 n " & vbLf)
                Next
                writer.Write("trailer" & vbLf & "<< /Size 9 /Root 1 0 R >>" & vbLf & "startxref" & vbLf & startXref & vbLf & "%%EOF" & vbLf)
            End Using
        End Using
    End Sub

    ' Enable double-buffering on Form controls recursively (using fully qualified System.Windows.Forms.Control)
    Private Sub EnableDoubleBuffering(ctrl As System.Windows.Forms.Control)
        Try
            Dim dbProp = GetType(System.Windows.Forms.Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic Or System.Reflection.BindingFlags.Instance)
            If dbProp IsNot Nothing Then
                dbProp.SetValue(ctrl, True, Nothing)
            End If
        Catch
        End Try

        For Each child As System.Windows.Forms.Control In ctrl.Controls
            EnableDoubleBuffering(child)
        Next
    End Sub
End Class

Public Class SvgGraphics
    Implements IDisposable

    Public Width As Integer
    Public Height As Integer
    Public g As System.Drawing.Graphics

    ' When False, all SVG/PDF string building is skipped (normal interactive renders).
    ' Set to True only when exporting.
    Public CaptureVectors As Boolean = False

    Private sbSvg As StringBuilder = Nothing
    Private sbPdf As StringBuilder = Nothing

    Public Sub New(w As Integer, h As Integer, realGraphics As System.Drawing.Graphics, Optional captureVectors As Boolean = False)
        Width = w
        Height = h
        g = realGraphics
        CaptureVectors = captureVectors

        If CaptureVectors Then
            sbSvg = New StringBuilder()
            sbPdf = New StringBuilder()
            sbSvg.AppendLine($"<svg xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" width=""{w}"" height=""{h}"" viewBox=""0 0 {w} {h}"">")
            sbSvg.AppendLine($"  <rect width=""{w}"" height=""{h}"" fill=""white"" />")
            sbPdf.AppendLine("q")
            sbPdf.AppendLine($"1 0 0 -1 0 {h.ToString(System.Globalization.CultureInfo.InvariantCulture)} cm")
            sbPdf.AppendLine("1 w")
            sbPdf.AppendLine("0 0 0 RG")
            sbPdf.AppendLine("0 0 0 rg")
        End If
    End Sub

    Public Sub Clear(c As Color)
        If g IsNot Nothing Then g.Clear(c)
        If Not CaptureVectors Then Return
        Dim cHex = ColorToHex(c)
        sbSvg.AppendLine($"  <rect width=""{Width}"" height=""{Height}"" fill=""{cHex}"" />")
        sbPdf.AppendLine($"{ColorToPdfColor(c)} rg")
        sbPdf.AppendLine($"0 0 {Width} {Height} re")
        sbPdf.AppendLine("f")
    End Sub

    Public Sub DrawLine(pen As Pen, x1 As Single, y1 As Single, x2 As Single, y2 As Single)
        If g IsNot Nothing Then g.DrawLine(pen, x1, y1, x2, y2)
        If Not CaptureVectors Then Return
        Dim cHex = ColorToHex(pen.Color)
        Dim dash = GetSvgDashArray(pen)
        sbSvg.AppendLine($"  <line x1=""{x1.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" y1=""{y1.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" x2=""{x2.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" y2=""{y2.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" stroke=""{cHex}"" stroke-width=""{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" {(If(String.IsNullOrEmpty(dash), "", $"stroke-dasharray=""{dash}"""))} />")
        sbPdf.AppendLine("q")
        sbPdf.AppendLine($"{ColorToPdfColor(pen.Color)} RG")
        sbPdf.AppendLine($"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)} w")
        Dim pdfDash = GetPdfDashArray(pen)
        If Not String.IsNullOrEmpty(pdfDash) Then sbPdf.AppendLine(pdfDash)
        sbPdf.AppendLine($"{x1.ToString(System.Globalization.CultureInfo.InvariantCulture)} {y1.ToString(System.Globalization.CultureInfo.InvariantCulture)} m")
        sbPdf.AppendLine($"{x2.ToString(System.Globalization.CultureInfo.InvariantCulture)} {y2.ToString(System.Globalization.CultureInfo.InvariantCulture)} l")
        sbPdf.AppendLine("S")
        sbPdf.AppendLine("Q")
    End Sub

    Public Sub DrawLine(pen As Pen, p1 As PointF, p2 As PointF)
        DrawLine(pen, p1.X, p1.Y, p2.X, p2.Y)
    End Sub

    Public Sub DrawEllipse(pen As Pen, rect As RectangleF)
        DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height)
    End Sub

    Public Sub DrawEllipse(pen As Pen, rect As Rectangle)
        DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height)
    End Sub

    Public Sub FillEllipse(brush As Brush, rect As RectangleF)
        FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height)
    End Sub

    Public Sub FillEllipse(brush As Brush, rect As Rectangle)
        FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height)
    End Sub

    Public Sub DrawRectangle(pen As Pen, rect As Rectangle)
        DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height)
    End Sub

    Public Sub DrawRectangle(pen As Pen, rect As RectangleF)
        DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height)
    End Sub

    Public Sub FillRectangle(brush As Brush, rect As RectangleF)
        FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height)
    End Sub

    Public Sub DrawLines(pen As Pen, points As PointF())
        If g IsNot Nothing Then g.DrawLines(pen, points)
        If Not CaptureVectors Then Return
        If points.Length < 2 Then Return

        Dim ptsStr = PointsToString(points)
        Dim cHex = ColorToHex(pen.Color)
        Dim dash = GetSvgDashArray(pen)
        sbSvg.AppendLine($"  <polyline points=""{ptsStr}"" fill=""none"" stroke=""{cHex}"" stroke-width=""{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" {(If(String.IsNullOrEmpty(dash), "", $"stroke-dasharray=""{dash}"""))} />")

        sbPdf.AppendLine("q")
        sbPdf.AppendLine($"{ColorToPdfColor(pen.Color)} RG")
        sbPdf.AppendLine($"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)} w")
        Dim pdfDash = GetPdfDashArray(pen)
        If Not String.IsNullOrEmpty(pdfDash) Then sbPdf.AppendLine(pdfDash)

        sbPdf.AppendLine($"{points(0).X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points(0).Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} m")
        For i = 1 To points.Length - 1
            sbPdf.AppendLine($"{points(i).X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points(i).Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} l")
        Next
        sbPdf.AppendLine("S")
        sbPdf.AppendLine("Q")
    End Sub

    Public Sub DrawPolygon(pen As Pen, points As PointF())
        If g IsNot Nothing Then g.DrawPolygon(pen, points)
        If Not CaptureVectors Then Return
        If points.Length < 2 Then Return

        Dim ptsStr = PointsToString(points)
        Dim cHex = ColorToHex(pen.Color)
        Dim dash = GetSvgDashArray(pen)
        sbSvg.AppendLine($"  <polygon points=""{ptsStr}"" fill=""none"" stroke=""{cHex}"" stroke-width=""{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" {(If(String.IsNullOrEmpty(dash), "", $"stroke-dasharray=""{dash}"""))} />")

        sbPdf.AppendLine("q")
        sbPdf.AppendLine($"{ColorToPdfColor(pen.Color)} RG")
        sbPdf.AppendLine($"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)} w")
        Dim pdfDash = GetPdfDashArray(pen)
        If Not String.IsNullOrEmpty(pdfDash) Then sbPdf.AppendLine(pdfDash)

        sbPdf.AppendLine($"{points(0).X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points(0).Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} m")
        For i = 1 To points.Length - 1
            sbPdf.AppendLine($"{points(i).X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points(i).Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} l")
        Next
        sbPdf.AppendLine("h S")
        sbPdf.AppendLine("Q")
    End Sub

    Public Sub FillPolygon(brush As Brush, points As PointF())
        If g IsNot Nothing Then g.FillPolygon(brush, points)
        If Not CaptureVectors Then Return
        If points.Length < 2 Then Return

        Dim ptsStr = PointsToString(points)
        Dim color = GetBrushColor(brush)
        Dim cHex = ColorToHex(color)
        Dim opacity = color.A / 255.0
        sbSvg.AppendLine($"  <polygon points=""{ptsStr}"" fill=""{cHex}"" opacity=""{opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" />")

        sbPdf.AppendLine("q")
        sbPdf.AppendLine($"{ColorToPdfColor(color)} rg")
        sbPdf.AppendLine($"{points(0).X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points(0).Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} m")
        For i = 1 To points.Length - 1
            sbPdf.AppendLine($"{points(i).X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points(i).Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} l")
        Next
        sbPdf.AppendLine("f")
        sbPdf.AppendLine("Q")
    End Sub

    Public Sub DrawEllipse(pen As Pen, x As Single, y As Single, w As Single, h As Single)
        If g IsNot Nothing Then g.DrawEllipse(pen, x, y, w, h)
        If Not CaptureVectors Then Return
        Dim cx = x + w / 2
        Dim cy = y + h / 2
        Dim rx = w / 2
        Dim ry = h / 2
        Dim cHex = ColorToHex(pen.Color)
        sbSvg.AppendLine($"  <ellipse cx=""{cx.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" cy=""{cy.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" rx=""{rx.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" ry=""{ry.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" stroke=""{cHex}"" stroke-width=""{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" fill=""none"" />")

        sbPdf.AppendLine("q")
        sbPdf.AppendLine($"{ColorToPdfColor(pen.Color)} RG")
        sbPdf.AppendLine($"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)} w")
        AppendPdfEllipse(x, y, w, h, "S")
        sbPdf.AppendLine("Q")
    End Sub

    Public Sub FillEllipse(brush As Brush, x As Single, y As Single, w As Single, h As Single)
        If g IsNot Nothing Then g.FillEllipse(brush, x, y, w, h)
        If Not CaptureVectors Then Return
        Dim cx = x + w / 2
        Dim cy = y + h / 2
        Dim rx = w / 2
        Dim ry = h / 2
        Dim color = GetBrushColor(brush)
        Dim cHex = ColorToHex(color)
        Dim opacity = color.A / 255.0
        sbSvg.AppendLine($"  <ellipse cx=""{cx.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" cy=""{cy.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" rx=""{rx.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" ry=""{ry.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" fill=""{cHex}"" opacity=""{opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" />")

        sbPdf.AppendLine("q")
        sbPdf.AppendLine($"{ColorToPdfColor(color)} rg")
        AppendPdfEllipse(x, y, w, h, "f")
        sbPdf.AppendLine("Q")
    End Sub

    Public Sub DrawRectangle(pen As Pen, x As Single, y As Single, w As Single, h As Single)
        If g IsNot Nothing Then g.DrawRectangle(pen, x, y, w, h)
        If Not CaptureVectors Then Return
        Dim cHex = ColorToHex(pen.Color)
        sbSvg.AppendLine($"  <rect x=""{x.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" y=""{y.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" width=""{w.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" height=""{h.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" stroke=""{cHex}"" stroke-width=""{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" fill=""none"" />")

        sbPdf.AppendLine("q")
        sbPdf.AppendLine($"{ColorToPdfColor(pen.Color)} RG")
        sbPdf.AppendLine($"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)} w")
        sbPdf.AppendLine($"{x.ToString(System.Globalization.CultureInfo.InvariantCulture)} {y.ToString(System.Globalization.CultureInfo.InvariantCulture)} {w.ToString(System.Globalization.CultureInfo.InvariantCulture)} {h.ToString(System.Globalization.CultureInfo.InvariantCulture)} re")
        sbPdf.AppendLine("S")
        sbPdf.AppendLine("Q")
    End Sub

    Public Sub FillRectangle(brush As Brush, x As Single, y As Single, w As Single, h As Single)
        If g IsNot Nothing Then g.FillRectangle(brush, x, y, w, h)
        If Not CaptureVectors Then Return
        Dim color = GetBrushColor(brush)
        Dim cHex = ColorToHex(color)
        Dim opacity = color.A / 255.0
        sbSvg.AppendLine($"  <rect x=""{x.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" y=""{y.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" width=""{w.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" height=""{h.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" fill=""{cHex}"" opacity=""{opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" />")

        sbPdf.AppendLine("q")
        sbPdf.AppendLine($"{ColorToPdfColor(color)} rg")
        sbPdf.AppendLine($"{x.ToString(System.Globalization.CultureInfo.InvariantCulture)} {y.ToString(System.Globalization.CultureInfo.InvariantCulture)} {w.ToString(System.Globalization.CultureInfo.InvariantCulture)} {h.ToString(System.Globalization.CultureInfo.InvariantCulture)} re")
        sbPdf.AppendLine("f")
        sbPdf.AppendLine("Q")
    End Sub

    Public Sub FillRectangle(brush As Brush, rect As Rectangle)
        FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height)
    End Sub

    Public Sub DrawPath(pen As Pen, path As Drawing2D.GraphicsPath)
        If g IsNot Nothing Then g.DrawPath(pen, path)
        If Not CaptureVectors Then Return
        Dim d = PathDataToSvgD(path.PathData)
        Dim cHex = ColorToHex(pen.Color)
        sbSvg.AppendLine($"  <path d=""{d}"" stroke=""{cHex}"" stroke-width=""{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" fill=""none"" {(If(String.IsNullOrEmpty(GetSvgDashArray(pen)), "", $"stroke-dasharray=""{GetSvgDashArray(pen)}"""))} />")

        sbPdf.AppendLine("q")
        sbPdf.AppendLine($"{ColorToPdfColor(pen.Color)} RG")
        sbPdf.AppendLine($"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)} w")
        Dim pdfDash = GetPdfDashArray(pen)
        If Not String.IsNullOrEmpty(pdfDash) Then sbPdf.AppendLine(pdfDash)
        AppendPdfPath(path.PathData)
        sbPdf.AppendLine("S")
        sbPdf.AppendLine("Q")
    End Sub

    Public Sub FillPath(brush As Brush, path As Drawing2D.GraphicsPath)
        If g IsNot Nothing Then g.FillPath(brush, path)
        If Not CaptureVectors Then Return
        Dim d = PathDataToSvgD(path.PathData)
        Dim color = GetBrushColor(brush)
        Dim cHex = ColorToHex(color)
        Dim opacity = color.A / 255.0
        sbSvg.AppendLine($"  <path d=""{d}"" fill=""{cHex}"" opacity=""{opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" />")

        sbPdf.AppendLine("q")
        sbPdf.AppendLine($"{ColorToPdfColor(color)} rg")
        AppendPdfPath(path.PathData)
        sbPdf.AppendLine("f")
        sbPdf.AppendLine("Q")
    End Sub

    Public Sub DrawString(s As String, font As Font, brush As Brush, x As Single, y As Single)
        If g IsNot Nothing Then g.DrawString(s, font, brush, x, y)
        If Not CaptureVectors Then Return
        Dim color = GetBrushColor(brush)
        Dim cHex = ColorToHex(color)
        Dim opacity = color.A / 255.0
        Dim escaped = EscapeXml(s)

        Dim isBold = font.Bold
        Dim isItalic = font.Italic
        Dim fontName = font.Name

        sbSvg.AppendLine($"  <text x=""{x.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" y=""{y.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" font-family=""{fontName}"" font-size=""{font.Size.ToString(System.Globalization.CultureInfo.InvariantCulture)}px"" {(If(isBold, "font-weight=""bold""", ""))} {(If(isItalic, "font-style=""italic""", ""))} fill=""{cHex}"" opacity=""{opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" dominant-baseline=""hanging"">{escaped}</text>")

        Dim pdfFontRef = "F1"
        If fontName.Contains("Consolas") OrElse fontName.Contains("Monospace") OrElse fontName.Contains("Courier") Then
            pdfFontRef = If(isBold, "F2", "F1")
        Else
            pdfFontRef = If(isBold, "F4", "F3")
        End If

        Dim escapedPdf = EscapePdfString(s)
        sbPdf.AppendLine("BT")
        sbPdf.AppendLine($"/{pdfFontRef} {font.Size.ToString(System.Globalization.CultureInfo.InvariantCulture)} Tf")
        sbPdf.AppendLine($"{ColorToPdfColor(color)} rg")
        Dim baselineY = y + font.Size * 0.8F
        sbPdf.AppendLine($"1 0 0 -1 {x.ToString(System.Globalization.CultureInfo.InvariantCulture)} {baselineY.ToString(System.Globalization.CultureInfo.InvariantCulture)} Tm")
        sbPdf.AppendLine($"({escapedPdf}) Tj")
        sbPdf.AppendLine("ET")
    End Sub

    Public Sub DrawString(s As String, font As Font, brush As Brush, pt As PointF)
        DrawString(s, font, brush, pt.X, pt.Y)
    End Sub

    Public Function MeasureString(text As String, font As Font) As SizeF
        If g IsNot Nothing Then
            Return g.MeasureString(text, font)
        Else
            Using tempBmp As New Bitmap(1, 1)
                Using tempG = Graphics.FromImage(tempBmp)
                    Return tempG.MeasureString(text, font)
                End Using
            End Using
        End If
    End Function

    Public Property SmoothingMode As Drawing2D.SmoothingMode
        Get
            If g IsNot Nothing Then Return g.SmoothingMode
            Return Drawing2D.SmoothingMode.Default
        End Get
        Set(value As Drawing2D.SmoothingMode)
            If g IsNot Nothing Then g.SmoothingMode = value
        End Set
    End Property

    Public Property TextRenderingHint As System.Drawing.Text.TextRenderingHint
        Get
            If g IsNot Nothing Then Return g.TextRenderingHint
            Return System.Drawing.Text.TextRenderingHint.SystemDefault
        End Get
        Set(value As System.Drawing.Text.TextRenderingHint)
            If g IsNot Nothing Then g.TextRenderingHint = value
        End Set
    End Property

    Public Property PixelOffsetMode As Drawing2D.PixelOffsetMode
        Get
            If g IsNot Nothing Then Return g.PixelOffsetMode
            Return Drawing2D.PixelOffsetMode.Default
        End Get
        Set(value As Drawing2D.PixelOffsetMode)
            If g IsNot Nothing Then g.PixelOffsetMode = value
        End Set
    End Property

    Public ReadOnly Property DpiX As Single
        Get
            If g IsNot Nothing Then Return g.DpiX
            Return 96.0F
        End Get
    End Property

    Public Function GetSvgContent() As String
        If sbSvg Is Nothing Then Return ""
        Return sbSvg.ToString() & "</svg>"
    End Function

    Public Function GetPdfContentStream() As String
        If sbPdf Is Nothing Then Return ""
        Return sbPdf.ToString() & "Q" & vbLf
    End Function

    Private Function ColorToHex(c As Color) As String
        Return $"#{c.R:X2}{c.G:X2}{c.B:X2}"
    End Function

    Private Function ColorToPdfColor(c As Color) As String
        Dim r = c.R / 255.0
        Dim g = c.G / 255.0
        Dim b = c.B / 255.0
        Return $"{r.ToString(System.Globalization.CultureInfo.InvariantCulture)} {g.ToString(System.Globalization.CultureInfo.InvariantCulture)} {b.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
    End Function

    Private Function GetBrushColor(brush As Brush) As Color
        If TypeOf brush Is SolidBrush Then
            Return CType(brush, SolidBrush).Color
        End If
        Return Color.Black
    End Function

    Private Function GetSvgDashArray(pen As Pen) As String
        If pen.DashStyle = DashStyle.Dash Then
            Return "4,4"
        ElseIf pen.DashStyle = DashStyle.Dot Then
            Return "1,3"
        ElseIf pen.DashStyle = DashStyle.DashDot Then
            Return "4,3,1,3"
        End If
        Return ""
    End Function

    Private Function GetPdfDashArray(pen As Pen) As String
        If pen.DashStyle = DashStyle.Dash Then
            Return "[4 4] 0 d"
        ElseIf pen.DashStyle = DashStyle.Dot Then
            Return "[1 3] 0 d"
        ElseIf pen.DashStyle = DashStyle.DashDot Then
            Return "[4 3 1 3] 0 d"
        End If
        Return ""
    End Function

    Private Function PointsToString(points As PointF()) As String
        Dim sbPoints As New StringBuilder()
        For Each pt In points
            sbPoints.Append($"{pt.X.ToString(System.Globalization.CultureInfo.InvariantCulture)},{pt.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} ")
        Next
        Return sbPoints.ToString().Trim()
    End Function

    Private Function PathDataToSvgD(pathData As Drawing2D.PathData) As String
        Dim sbD As New StringBuilder()
        Dim pts = pathData.Points
        Dim types = pathData.Types

        Dim i = 0
        While i < pts.Length
            Dim type = types(i)
            Dim pt = pts(i)
            Dim xStr = pt.X.ToString(System.Globalization.CultureInfo.InvariantCulture)
            Dim yStr = pt.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)

            If (type And &H7) = 0 Then
                sbD.Append($"M {xStr} {yStr} ")
                i += 1
            ElseIf (type And &H7) = 1 Then
                sbD.Append($"L {xStr} {yStr} ")
                If (type And &H80) = &H80 Then sbD.Append("Z ")
                i += 1
            ElseIf (type And &H7) = 3 Then
                If i + 2 < pts.Length Then
                    Dim cp1 = pts(i)
                    Dim cp2 = pts(i + 1)
                    Dim ep = pts(i + 2)
                    Dim cp1x = cp1.X.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    Dim cp1y = cp1.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    Dim cp2x = cp2.X.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    Dim cp2y = cp2.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    Dim epx = ep.X.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    Dim epy = ep.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)

                    sbD.Append($"C {cp1x} {cp1y}, {cp2x} {cp2y}, {epx} {epy} ")
                    Dim epType = types(i + 2)
                    If (epType And &H80) = &H80 Then sbD.Append("Z ")
                    i += 3
                Else
                    i += 1
                End If
            Else
                i += 1
            End If
        End While
        Return sbD.ToString().Trim()
    End Function

    Private Sub AppendPdfPath(pathData As Drawing2D.PathData)
        Dim pts = pathData.Points
        Dim types = pathData.Types

        Dim i = 0
        While i < pts.Length
            Dim type = types(i)
            Dim pt = pts(i)
            Dim xStr = pt.X.ToString(System.Globalization.CultureInfo.InvariantCulture)
            Dim yStr = pt.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)

            If (type And &H7) = 0 Then
                sbPdf.AppendLine($"{xStr} {yStr} m")
                i += 1
            ElseIf (type And &H7) = 1 Then
                sbPdf.AppendLine($"{xStr} {yStr} l")
                If (type And &H80) = &H80 Then sbPdf.AppendLine("h")
                i += 1
            ElseIf (type And &H7) = 3 Then
                If i + 2 < pts.Length Then
                    Dim cp1 = pts(i)
                    Dim cp2 = pts(i + 1)
                    Dim ep = pts(i + 2)
                    Dim cp1x = cp1.X.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    Dim cp1y = cp1.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    Dim cp2x = cp2.X.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    Dim cp2y = cp2.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    Dim epx = ep.X.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    Dim epy = ep.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)

                    sbPdf.AppendLine($"{cp1x} {cp1y} {cp2x} {cp2y} {epx} {epy} c")
                    Dim epType = types(i + 2)
                    If (epType And &H80) = &H80 Then sbPdf.AppendLine("h")
                    i += 3
                Else
                    i += 1
                End If
            Else
                i += 1
            End If
        End While
    End Sub

    Private Sub AppendPdfEllipse(x As Single, y As Single, w As Single, h As Single, op As String)
        Dim cx = x + w / 2
        Dim cy = y + h / 2
        Dim rx = w / 2
        Dim ry = h / 2

        Dim kappa As Double = 0.55228474983079345
        Dim ox = rx * kappa
        Dim oy = ry * kappa

        Dim cxStr = cx.ToString(System.Globalization.CultureInfo.InvariantCulture)
        Dim cyStr = cy.ToString(System.Globalization.CultureInfo.InvariantCulture)
        Dim rxStr = rx.ToString(System.Globalization.CultureInfo.InvariantCulture)
        Dim ryStr = ry.ToString(System.Globalization.CultureInfo.InvariantCulture)

        Dim xM = (cx - rx).ToString(System.Globalization.CultureInfo.InvariantCulture)
        Dim xP = (cx + rx).ToString(System.Globalization.CultureInfo.InvariantCulture)
        Dim yM = (cy - ry).ToString(System.Globalization.CultureInfo.InvariantCulture)
        Dim yP = (cy + ry).ToString(System.Globalization.CultureInfo.InvariantCulture)

        Dim cpXM_ox = (cx - rx + ox).ToString(System.Globalization.CultureInfo.InvariantCulture)
        Dim cpXP_ox = (cx + rx - ox).ToString(System.Globalization.CultureInfo.InvariantCulture)
        Dim cpYM_oy = (cy - ry + oy).ToString(System.Globalization.CultureInfo.InvariantCulture)
        Dim cpYP_oy = (cy + ry - oy).ToString(System.Globalization.CultureInfo.InvariantCulture)

        Dim cpCX_ox = (cx - ox).ToString(System.Globalization.CultureInfo.InvariantCulture)
        Dim cpCX_pox = (cx + ox).ToString(System.Globalization.CultureInfo.InvariantCulture)
        Dim cpCY_oy = (cy - oy).ToString(System.Globalization.CultureInfo.InvariantCulture)
        Dim cpCY_poy = (cy + oy).ToString(System.Globalization.CultureInfo.InvariantCulture)

        sbPdf.AppendLine($"{xM} {cyStr} m")
        sbPdf.AppendLine($"{xM} {cpCY_oy} {cpCX_ox} {yM} {cxStr} {yM} c")
        sbPdf.AppendLine($"{cpCX_pox} {yM} {xP} {cpCY_oy} {xP} {cyStr} c")
        sbPdf.AppendLine($"{xP} {cpCY_poy} {cpCX_pox} {yP} {cxStr} {yP} c")
        sbPdf.AppendLine($"{cpCX_ox} {yP} {xM} {cpCY_poy} {xM} {cyStr} c")
        sbPdf.AppendLine($"h {op}")
    End Sub

    Private Function EscapeXml(s As String) As String
        Return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("""", "&quot;").Replace("'", "&apos;")
    End Function

    Private Function EscapePdfString(s As String) As String
        Return s.Replace("\", "\\").Replace("(", "\(").Replace(")", "\)")
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        If g IsNot Nothing Then
            g.Dispose()
        End If
    End Sub
End Class


' A Surface/Section/Control text block in the .avl file, delimited by this
' app's own !beginX/!endX markers. Used by the structure tree to navigate,
' reorder (cut/paste line ranges), and add/delete blocks.
Public Class GeomBlock
    Public Kind As String   ' "Surface" / "Section" / "Control"
    Public Label As String
    Public StartLine As Integer   ' inclusive
    Public EndLine As Integer     ' inclusive - the !endX line
    Public DataLine As Integer = -1   ' Section/Control's own data row - matches Node.lineNumber/controlLineNumber
    Public Children As New List(Of GeomBlock)
End Class

Public Class Node
    Public Point As Point3D
    Public Surface As String
    Public X As Single
    Public Y As Single
    Public Z As Single
    Public Hovered As Boolean
    Public lineNumber As Integer
    Public type As NodeType
    Public mass As Single = 0
    Public Visible As Boolean = True
    Public IsDuplicate As Boolean = False
    ' SubType encodes what this node controls in the file
    Public SubType As NodeSubType = NodeSubType.None
    ' Extra metadata used by CommitNodeDrag for TrailingEdge / ControlHinge nodes
    Public parentXle As Single = 0
    Public parentChord As Single = 0
    Public controlLineNumber As Integer = -1   ' line index of the CONTROL data row
    Public originalXhinge As Single = 0        ' sign-convention reference for Xhinge

    Enum NodeSubType
        None = 0
        LeadingEdge = 1
        TrailingEdge = 2
        ControlHinge = 3
    End Enum

    Public ReadOnly Property IsDraggable As Boolean
        Get
            If type = NodeType.Mass Then Return True
            If type = NodeType.Geometry AndAlso Not IsDuplicate Then
                Return SubType = NodeSubType.LeadingEdge OrElse
                       SubType = NodeSubType.TrailingEdge OrElse
                       SubType = NodeSubType.ControlHinge
            End If
            Return False
        End Get
    End Property

    Enum NodeType
        Geometry = 0
        Mass = 1
    End Enum

    Sub New()
        Me.Hovered = False
        Me.lineNumber = 0
    End Sub

    Sub New(ByVal p As Point3D, ByVal surfacename As String, ByVal hovered As Boolean, ByVal linenumber As Integer, ByVal nodetype As NodeType, Optional mass As Single = 0)
        Me.Point = p
        Me.X = CSng(p.X)
        Me.Y = CSng(p.Y)
        Me.Z = CSng(p.Z)
        Me.Surface = surfacename
        Me.Hovered = hovered
        Me.lineNumber = linenumber
        Me.type = nodetype
        Me.mass = mass
    End Sub

    Sub New(ByVal x As Double, ByVal y As Double, ByVal z As Double, ByVal surfacename As String, ByVal hovered As Boolean, ByVal linenumber As Integer, ByVal nodetype As NodeType, Optional mass As Single = 0)
        Me.Point = New Point3D(x, y, z)
        Me.X = CSng(x)
        Me.Y = CSng(y)
        Me.Z = CSng(z)
        Me.Surface = surfacename
        Me.Hovered = hovered
        Me.lineNumber = linenumber
        Me.type = nodetype
        Me.mass = mass
    End Sub

    Public Function RotateZ(angle As Single) As Node
        Dim rad As Single = CSng(angle * Math.PI / 180)
        Dim cosa As Single = CSng(Math.Cos(rad))
        Dim sina As Single = CSng(Math.Sin(rad))
        Dim Xn As Single = Me.X * cosa - Me.Y * sina
        Dim Yn As Single = Me.X * sina + Me.Y * cosa
        Return New Node(New Point3D(Xn, Yn, Me.Z), Me.Surface, Me.Hovered, Me.lineNumber, Me.type, Me.mass)
    End Function

    Public Function RotateX(angle As Single) As Node
        Dim rad As Single = CSng(angle * Math.PI / 180)
        Dim cosa As Single = CSng(Math.Cos(rad))
        Dim sina As Single = CSng(Math.Sin(rad))
        Dim yn As Single = Me.Y * cosa - Me.Z * sina
        Dim zn As Single = Me.Y * sina + Me.Z * cosa
        Return New Node(New Point3D(Me.X, yn, zn), Me.Surface, Me.Hovered, Me.lineNumber, Me.type, Me.mass)
    End Function

    Public Function RotateY(angle As Single) As Node
        Dim rad As Single = CSng(angle * Math.PI / 180)
        Dim cosa As Single = CSng(Math.Cos(rad))
        Dim sina As Single = CSng(Math.Sin(rad))
        Dim Zn As Single = Me.Z * cosa - Me.X * sina
        Dim Xn As Single = Me.Z * sina + Me.X * cosa
        Return New Node(New Point3D(Xn, Me.Y, Zn), Me.Surface, Me.Hovered, Me.lineNumber, Me.type, Me.mass)
    End Function

    Public Function Project(viewWidth As Single, viewHeight As Single, viewDistance As Single, Optional fov As Single = 500) As Node
        ' 1. Check if point is behind or too close to camera (Near Plane Clipping)
        ' We use 1.0 as a safety buffer.
        If (viewDistance + Me.Z) < 0 Then
            ' It is behind the camera. Mark as invisible.
            Dim n As New Node(New Point3D(0, 0, Me.Z), Me.Surface, Me.Hovered, Me.lineNumber, Me.type, Me.mass)
            n.Visible = False
            Return n
        End If

        ' 2. Safe Projection
        Dim scale As Single = fov / (viewDistance + Me.Z)
        Dim px As Single = Me.X * scale + viewWidth / 2
        Dim py As Single = Me.Y * scale + viewHeight / 2

        Return New Node(New Point3D(px, py, Me.Z), Me.Surface, Me.Hovered, Me.lineNumber, Me.type, Me.mass)
    End Function
End Class

' One row of AVL's "FS" (strip forces) output table.
Public Class TrefftzStrip
    Public Yle As Double
    Public Chord As Double
    Public Area As Double
    Public CCl As Double       ' "c cl" = chord * local cl -> spanwise loading when divided by Cref
    Public Ai As Double        ' induced angle of attack, radians
    Public ClNorm As Double
    Public Cl As Double
    Public Cd As Double        ' local induced-drag coefficient
    Public Cdv As Double
    Public CmC4 As Double
    Public CmLE As Double
    Public Cpxc As Double
End Class

' One "Surface # n ..." block of AVL's FS output, with its strips in span order.
Public Class TrefftzSurface
    Public Name As String
    Public Strips As New List(Of TrefftzStrip)
End Class

' Totals block AVL prints after "X" (execute) - "Vortex Lattice Output -- Total
' Forces" - matching what AVL's own native Trefftz Plane window displays as text.
Public Class TrefftzTotals
    Public Valid As Boolean = False
    Public ConfigName As String = ""
    Public RunCaseName As String = ""
    Public Alpha As Double
    Public Beta As Double
    Public Mach As Double
    Public CLtot As Double
    Public CDtot As Double
    Public CDvis As Double
    Public CDff As Double
    Public CYff As Double
    Public SpanEff As Double
    Public ClPrimeTot As Double
    Public Cmtot As Double
    Public CnPrimeTot As Double
End Class

' One row of AVL's "VM" (strip shear/moment) output table.
Public Class VmStrip
    Public Y2Bref As Double    ' 2Y/Bref, normalized span position (-1 to 1)
    Public Vz As Double        ' Vz/(q*Sref), spanwise shear
    Public Mx As Double        ' Mx/(q*Bref*Sref), spanwise bending moment
End Class

Public Class VmSurface
    Public Name As String
    Public Points As New List(Of VmStrip)
End Class

' One point of a drag-polar alpha sweep (reuses ParseTotalForces per point).
Public Class PolarPoint
    Public Alpha As Double
    Public CL As Double
    Public CD As Double
    Public Cm As Double
End Class

' One chordwise vortex-panel row of AVL's "FE" (element forces) output.
Public Class FePanel
    Public X As Double
    Public DCp As Double
End Class

' One spanwise strip's chordwise dCp distribution from "FE".
Public Class FeStrip
    Public SurfaceName As String
    Public StripIndex As Integer
    Public Yle As Double
    Public Cl As Double
    Public Cd As Double
    Public Panels As New List(Of FePanel)
End Class

' One eigenvalue (a complex root) from AVL's ".MODE" eigenmode analysis, plus
' the eigenvector magnitudes (from the console transcript) used to classify it
' as a named flight-dynamics mode (phugoid, dutch roll, etc).
Public Class EigenValue
    Public RunCase As Integer
    Public Real As Double
    Public Imag As Double
    Public MagU As Double
    Public MagV As Double
    Public MagW As Double
    Public MagP As Double
    Public MagQ As Double
    Public MagR As Double
    Public MagPhi As Double
    Public MagTheta As Double
    Public MagPsi As Double
    Public ModeLabel As String = ""
    ' Set by DrawModesSubplot for the currently rendered plot, used for hover
    ' hit-testing in pModes_MouseMove - not persisted/exported.
    Public ScreenX As Single
    Public ScreenY As Single
End Class

Friend Class EllipseStyle
    Inherits Style
    Dim lineColor As Color = Color.Red
    Dim linewidth As Integer = 1
    Sub New()
    End Sub
    Sub New(ByVal color As Color)
        lineColor = color
    End Sub
    Sub New(ByVal color As Color, ByVal width As Integer)
        lineColor = color
        linewidth = width
    End Sub
    Sub New(ByVal width As Integer)
        linewidth = width
    End Sub
    Public Overrides Sub Draw(gr As Graphics, position As Point, range As Range)
        Dim size As Size = Style.GetSizeOfRange(range)
        Dim rect As Rectangle = New Rectangle(position, size)
        rect.Inflate(1, 0)
        Dim path As GraphicsPath = Style.GetRoundedRectangle(rect, 7)
        gr.DrawPath(New Pen(lineColor, linewidth), path)
    End Sub
End Class


Public Class ModernFastColoredTextBox
    Inherits FastColoredTextBox

    ' DPI scale factor
    Private _dpiScale As Single = 1.0F

    Public Sub New()
        MyBase.New()

        ' Calculate DPI scale
        Using g As Graphics = Me.CreateGraphics()
            _dpiScale = g.DpiX / 96.0F
        End Using

        ' Modern font scaled by DPI
        Me.Font = New Font("Consolas", 10 * _dpiScale)
        Me.ForeColor = Color.Black
        Me.BackColor = Color.White
        Me.ShowLineNumbers = True

        ' Smooth rendering
        Me.DoubleBuffered = True

    End Sub

    ' Paint override for ClearType + anti-alias
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        e.Graphics.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality
        MyBase.OnPaint(e)
    End Sub

End Class



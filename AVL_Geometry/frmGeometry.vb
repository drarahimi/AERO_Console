Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports System.Windows.Media.Media3D
Imports FastColoredTextBoxNS

Public Class frmGeometry
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
    Dim eps As Single = 0.05
    Dim isHovered As Boolean = False
    Dim rootPath As String = Application.StartupPath + "\appdata"
    Dim projectName As String = "test"
    Dim updating As Boolean = False
    Public help As String = rootPath + "\avl_doc.txt"
    Dim autoSpace As Boolean = True
    Dim showMass As Boolean = True
    Dim showControl As Boolean = True
    Dim showSection As Boolean = True
    Dim show3D As Boolean = True

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
        popupMenu = New FastColoredTextBoxNS.AutocompleteMenu(txt3)
        popupMenu.MinFragmentLength = 2
        Dim keyWords As List(Of String) = New List(Of String)
        keyWords.AddRange("section|control|Mach|IYsym|IZsym|Zsym|Sref|Cref|Bref|Xref|Yref|Zref|Nchordwise|Cspace|Nspanwise|Sspace|Xle|Yle|Zle|Chord|Ainc|Nspanwise|Sspace|Cname|Cgain|Xhinge|HingeVec|SgnDup|YDUPLICATE|ANGLE".Split("|"))
        popupMenu.Items.SetAutocompleteItems(keyWords)
        tlp1.Dock = DockStyle.Fill

        pxy.Dock = DockStyle.Fill
        pxz.Dock = DockStyle.Fill
        pyz.Dock = DockStyle.Fill
        txt3.Dock = DockStyle.Fill
        frmMain.SetAllControlsFont(Me.Controls, frmMain.systemFont)
        drawAxes()


        txtName_TextChanged(sender, e)
        finAVLs(Environment.CurrentDirectory)

        'loadTemplate()
        btnLoadG.PerformClick()


    End Sub

    Public Sub finAVLs(path As String)
        Dim files() As String
        files = Directory.GetFiles(path, "*.avl", SearchOption.TopDirectoryOnly)
        txtName.Items.Clear()
        For Each FileName As String In files
            'Console.WriteLine(FileName)
            txtName.Items.Add(FileName.Split("\").Last.Replace(".avl", ""))
        Next
        If (txtName.Items.Count > 0) Then
            txtName.SelectedIndex = 0
        End If

    End Sub

    Public Sub loadTemplate()
        Dim value As String = File.ReadAllText(rootPath + "\template_avl.txt")
        txt3.Text = value
        'txt3.Colorize()
    End Sub

    Public Sub drawAxes()
        If bg1.IsBusy Then
            bg1.CancelAsync()
        Else
            bg1.RunWorkerAsync()
        End If
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


    Public Function anyHovered() As Boolean
        'Dim result As Boolean = False

        For Each p As Node In points
            If p.Hovered Then Return True
        Next

        Return False
    End Function

    Private Sub p1_MouseMove(sender As Object, e As MouseEventArgs) Handles pxy.MouseMove
        Dim s As String = ""
        Dim e1 As Double = (xmax - xmin) / pxy.Width * (e.X - (pxy.Width / 2) - xoffset)
        Dim t1 As String = "[X: " + String.Format("{0,5:###.0}", Math.Round(e1, 1)) '.ToString("{0,5:###.0}")
        Dim e2 As Double = (ymax - ymin) / pxy.Height * (e.Y - (pxy.Height / 2) - yoffset)
        Dim t2 As String = ", Y: " + String.Format("{0,5:###.0}", Math.Round(-e2, 1)) + "]" '.ToString("0.0") + "]"
        s = t1 + t2
        curX = e1
        curY = -e2
        lblCursor.Text = "Cursor: " + s

        For Each p As Node In points
            If (p.X > e1 - eps) And (p.X < e1 + eps) And (p.Y > -e2 - eps) And (p.Y < -e2 + eps) Then
                p.Hovered = True
                selectText(p.lineNumber)
                isHovered = True
            Else
                p.Hovered = False
            End If
        Next

        If e.Button = MouseButtons.Left And (isHovered = False) Then
            xoffset = e.X - xdown
            yoffset = e.Y - ydown
        End If


        drawAxes()


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
        pxy.Cursor = Cursors.Default
        If e.Button = MouseButtons.Left Then
            'points.Add(New Node(curX, curY, 0))
            drawAxes()
        End If
    End Sub

    Private Sub btnReset_Click(sender As Object, e As EventArgs)
        points.Clear()
        drawAxes()
    End Sub

    Private Sub AVLTemplateToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AVLTemplateToolStripMenuItem.Click
        Dim f = rootPath + "\template_avl.txt"
        Dim val = File.ReadAllText(f)
        txt3.Text = val
        txt3.SelectionStart = txt3.Text.Length + vbNewLine.Length
        txt3.DoCaretVisible()

    End Sub

    Private Sub SurfaceToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SurfaceToolStripMenuItem.Click

        Dim before = CountBlocks()
        Dim after = CountBlocks(False)

        If ((before("!begingeometry") + after("!begingeometry") < 1) Or (before("!endgeometry") + after("!endgeometry") < 1)) Then
            MsgBox("You must have a !begingeometry and !endgeometry tags in your AVL file and include all other blocks inside these tags.\n\n If you did not add an AVL template first in your AVL file, make sure to add an AVL template first and then other components inside.\n\n Fix this and try again!".Replace("\n", vbNewLine), MsgBoxStyle.OkOnly, Application.ProductName)
            Return
        End If

        If (before("!beginsurface") <> before("!endsurface")) Then
            MsgBox("You cannot add a surface block inside another surface block.\n\n If your curser is between a !beginsurface and !endsurface, move your cursur to outside the two tags before inserting a section block.\n\n Fix this and try again!".Replace("\n", vbNewLine), MsgBoxStyle.OkOnly, Application.ProductName)
            Return
        End If

        If (before("!begingeometry") <> after("!endgeometry")) Then
            MsgBox("You must have all geometry tags including surface, section, and control inside a !begingeometry and !endgeometry tag block.\n\n If your curser is not between the !begingeometry and !endgeometry, move your cursur to between the two tags before inserting a surface block.\n\n Fix this and try again!".Replace("\n", vbNewLine), MsgBoxStyle.OkOnly, Application.ProductName)
            Return
        End If


        Dim f = rootPath + "\template_surface.txt"
        Dim val = File.ReadAllText(f)
        Dim sel = txt3.SelectionStart
        txt3.Text = txt3.Text.Insert(sel, vbNewLine + val)
        txt3.SelectionStart = sel + (vbNewLine + val).Length
        txt3.DoCaretVisible()

    End Sub

    Private Sub SectionToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SectionToolStripMenuItem.Click

        Dim before = CountBlocks()
        Dim after = CountBlocks(False)

        If ((before("!begingeometry") + after("!begingeometry") < 1) Or (before("!endgeometry") + after("!endgeometry") < 1)) Then
            MsgBox("You must have a !begingeometry and !endgeometry tags in your AVL file and include all other blocks inside these tags.\n\n If you did not add an AVL template first in your AVL file, make sure to add an AVL template first and then other components inside.\n\n Fix this and try again!".Replace("\n", vbNewLine), MsgBoxStyle.OkOnly, Application.ProductName)
            Return
        End If
        If ((before("!beginsurface") + after("!beginsurface") < 1) Or (before("!endsurface") + after("!endsurface") < 1)) Then
            MsgBox("You must have a !beginsurface and !endsurface tags in your AVL file and include all other section and control blocks inside these tags.\n\n If you did not add a surface template first in your AVL file, make sure to add a surface template first and then section and control components inside.\n\n Fix this and try again!".Replace("\n", vbNewLine), MsgBoxStyle.OkOnly, Application.ProductName)
            Return
        End If
        If before("!beginsurface") - before("!endsurface") <> after("!endsurface") - after("!beginsurface") Or before("!beginsurface") < 1 Or (before("!beginsurface") - before("!endsurface") <> after("!endsurface")) Then
            MsgBox("You must have all section and control tags inside a !beginsurface and !endsurface tag block.\n\n If your curser is not between the !beginsurface and !endsurface, move your cursur to between the two tags before inserting a section block.\n\n Fix this and try again!".Replace("\n", vbNewLine), MsgBoxStyle.OkOnly, Application.ProductName)
            Return
        End If
        If (before("!beginsection") <> before("!endsection")) Then
            MsgBox("You cannot add a section block inside another section block.\n\n If your curser is between a !beginsection and !endsection, move your cursur to outside the two tags before inserting a section block.\n\n Fix this and try again!".Replace("\n", vbNewLine), MsgBoxStyle.OkOnly, Application.ProductName)
            Return
        End If
        If (before("!begincontrol") <> before("!endcontrol")) Then
            MsgBox("You cannot add a section block inside another control block.\n\n If your curser is between a !begincontrol and !endcontrol, move your cursur to outside the two tags before inserting a section block.\n\n Fix this and try again!".Replace("\n", vbNewLine), MsgBoxStyle.OkOnly, Application.ProductName)
            Return
        End If

        Dim f = rootPath + "\template_section.txt"
        Dim val = File.ReadAllText(f)
        Dim sel = txt3.SelectionStart
        txt3.Text = txt3.Text.Insert(sel, vbNewLine + val)
        txt3.SelectionStart = sel + (vbNewLine + val).Length
        txt3.DoCaretVisible()

    End Sub

    Private Sub ControlToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ControlToolStripMenuItem.Click

        Dim before = CountBlocks()
        Dim after = CountBlocks(False)

        If ((before("!begingeometry") + after("!begingeometry") < 1) Or (before("!endgeometry") + after("!endgeometry") < 1)) Then
            MsgBox("You must have a !begingeometry and !endgeometry tags in your AVL file and include all other blocks inside these tags.\n\n If you did not add an AVL template first in your AVL file, make sure to add an AVL template first and then other components inside.\n\n Fix this and try again!".Replace("\n", vbNewLine), MsgBoxStyle.OkOnly, Application.ProductName)
            Return
        End If
        If (before("!begingeometry") <> after("!endgeometry")) Then
            MsgBox("You must have all geometry tags including surface, section, and control inside a !begingeometry and !endgeometry tag block.\n\n If your curser is not between the !begingeometry and !endgeometry, move your cursur to between the two tags before inserting a surface block.\n\n Fix this and try again!".Replace("\n", vbNewLine), MsgBoxStyle.OkOnly, Application.ProductName)
            Return
        End If

        If ((before("!beginsurface") + after("!beginsurface") < 1) Or (before("!endsurface") + after("!endsurface") < 1)) Then
            MsgBox("You must have a !beginsurface and !endsurface tags in your AVL file and include all other section and control blocks inside these tags.\n\n If you did not add a surface template first in your AVL file, make sure to add a surface template first and then section and control components inside.\n\n Fix this and try again!".Replace("\n", vbNewLine), MsgBoxStyle.OkOnly, Application.ProductName)
            Return
        End If
        If (before("!beginsurface") - before("!endsurface") <> after("!endsurface") - after("!beginsurface") Or before("!beginsurface") < 1 Or (before("!beginsurface") - before("!endsurface") <> after("!endsurface"))) Then
            MsgBox("You must have all section and control tags inside a !beginsurface and !endsurface tag block.\n\n If your curser is not between the !beginsurface and !endsurface, move your cursur to between the two tags before inserting a section block.\n\n Fix this and try again!".Replace("\n", vbNewLine), MsgBoxStyle.OkOnly, Application.ProductName)
            Return
        End If
        'If (before("!beginsection") <> before("!endsection")) Then
        '    MsgBox("You cannot add a control block inside another section block.\n\n If your curser is between a !beginsection and !endsection, move your cursur to outside the two tags before inserting a section block.\n\n Fix this and try again!".Replace("\n", vbNewLine), MsgBoxStyle.OkOnly, Application.ProductName)
        '    Return
        'End If
        If (before("!beginsection") - before("!endsection") <> after("!endsection") - after("!beginsection")) Or (before("!beginsection") - before("!endsection") <> after("!endsection")) Then
            MsgBox("You must have all control tags inside a !beginsection and !endsection tag block.\n\n If your curser is not between the !beginsection and !endsection, move your cursur to between the two tags before inserting a section block.\n\n Fix this and try again!".Replace("\n", vbNewLine), MsgBoxStyle.OkOnly, Application.ProductName)
            Return
        End If
        If (before("!begincontrol") <> before("!endcontrol")) Then
            MsgBox("You cannot add a control block inside another control block.\n\n If your curser is between a !begincontrol and !endcontrol, move your cursur to outside the two tags before inserting a section block.\n\n Fix this and try again!".Replace("\n", vbNewLine), MsgBoxStyle.OkOnly, Application.ProductName)
            Return
        End If


        Dim f = rootPath + "\template_control.txt"
        Dim val = File.ReadAllText(f)
        Dim sel = txt3.SelectionStart
        txt3.Text = txt3.Text.Insert(sel, vbNewLine + val)
        txt3.SelectionStart = sel + (vbNewLine + val).Length
        txt3.DoCaretVisible()

    End Sub

    Private Sub SeparatorToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SeparatorToolStripMenuItem.Click
        Dim val = "#==========================================="
        Dim sel = txt3.SelectionStart
        txt3.Text = txt3.Text.Insert(sel, vbNewLine + val)
        txt3.SelectionStart = sel + (vbNewLine + val).Length
        txt3.DoCaretVisible()


    End Sub

    Private Sub btnClear_Click(sender As Object, e As EventArgs) Handles btnClear.Click
        txt3.Text = ""
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


    Private Sub btnHelp_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub txt3_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txt3.TextChanged
        findPoints()
    End Sub

    Private Sub findPoints()
        Debug.WriteLine($"==================================================================")
        Dim lines() As String = TrimAll(txt3.Text.Replace(vbLf, "")).Split(vbCrLf)

        Dim c As Integer = 0
        Dim vals
        'Dim section As Section
        'Dim surface As Surface
        'Dim sections As List(Of Section)
        Dim surfaces As List(Of Surface) = New List(Of Surface)
        Dim i As Integer = 0

        Do While (i < lines.Count - 2)
            'Debug.WriteLine($"Line number: {i} | {lines(i).ToLower.Trim = "surface"} | {lines(i)}")
            If lines(i).ToLower.Trim = "surface" Then
                'MsgBox("found a surface")
                Debug.WriteLine($"Found surface at line {i + 1}")
                Dim surface = New Surface
                surface.sections = New List(Of Section)
                Dim controls = New List(Of Control)
                surface.Name = lines(i + 1).Trim.Replace(vbNewLine, "")
                'MsgBox(lines(i + 1))
                i += 2
                Do While (lines(i + 1).ToLower.Trim <> "surface" And (i < lines.Count - 2))
                    If lines(i).ToLower.Trim = "yduplicate" Then
                        surface.yDuplicate = True
                        surface.yDuplicatevalue = CDbl(lines(i + 1))
                    End If

                    'check for sections
                    If lines(i).ToLower.Trim = "section" Then
                        Debug.WriteLine($"Found    section at line {i + 1}")
                        'MsgBox("found a section")
                        i += 1
                    End If
                    If lines(i).ToLower.Trim.StartsWith("#xle") Then
                        vals = lines(i + 1).Split(" ")
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
                                Debug.WriteLine($"Found       control at line {i + 1}")
                                i += 1
                            End If

                            If lines(i).ToLower.Trim.StartsWith("#cname") Then
                                vals = lines(i + 1).Split(" ")
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

        points.Clear()

        If surfaces.Count > 0 Then
            For Each su As Surface In surfaces
                For Each se As Section In su.sections
                    With se
                        Dim p1 As Point3D = New Point3D(.Xle, .Yle + (IIf(su.yDuplicate, su.yDuplicatevalue, 0)), .Zle)
                        points.Add(New Node(p1, su.Name, False, se.lineNumber))
                        Dim p2 As Point3D = New Point3D(.Xle + .Chord, .Yle + (IIf(su.yDuplicate, su.yDuplicatevalue, 0)), .Zle)
                        points.Add(New Node(p2, su.Name, False, se.lineNumber))
                        If (se.controls.Count > 0) Then
                            For Each cs As ControlSurface In se.controls
                                Dim pc1 As Point3D = New Point3D(.Xle + IIf(cs.Xhinge > 0, cs.Xhinge, 1 - cs.Xhinge) * .Chord, .Yle + (IIf(su.yDuplicate, su.yDuplicatevalue, 0)), .Zle)
                                points.Add(New Node(pc1, "control" + cs.Type.Replace(vbLf, ""), False, se.lineNumber))
                                Dim pc2 As Point3D = New Point3D(.Xle + .Chord, .Yle + (IIf(su.yDuplicate, su.yDuplicatevalue, 0)), .Zle)
                                points.Add(New Node(pc2, "control" + cs.Type.Replace(vbLf, ""), False, se.lineNumber))
                            Next
                        End If
                        If su.yDuplicate Then
                            Dim p3 As Point3D = New Point3D(.Xle, -(.Yle + (IIf(su.yDuplicate, su.yDuplicatevalue, 0))), .Zle)
                            points.Add(New Node(p3, su.Name + "_dup", False, se.lineNumber))
                            Dim p4 As Point3D = New Point3D(.Xle + .Chord, -(.Yle + (IIf(su.yDuplicate, su.yDuplicatevalue, 0))), .Zle)
                            points.Add(New Node(p4, su.Name + "_dup", False, se.lineNumber))
                            If (se.controls.Count > 0) Then
                                For Each cs As ControlSurface In se.controls
                                    Dim pc3 As Point3D = New Point3D(.Xle + IIf(cs.Xhinge > 0, cs.Xhinge, 1 - cs.Xhinge) * .Chord, -(.Yle + (IIf(su.yDuplicate, su.yDuplicatevalue, 0))), .Zle)
                                    points.Add(New Node(pc3, "control" + cs.Type.Replace(vbLf, "") + "_dup", False, se.lineNumber))
                                    Dim pc4 As Point3D = New Point3D(.Xle + .Chord, -(.Yle + (IIf(su.yDuplicate, su.yDuplicatevalue, 0))), .Zle)
                                    points.Add(New Node(pc4, "control" + cs.Type.Replace(vbLf, "") + "_dup", False, se.lineNumber))
                                Next
                            End If
                        End If
                    End With
                Next
            Next
        End If

        drawAxes()

    End Sub

    Private Sub txt3_TextChangedDelayed(sender As Object, e As TextChangedEventArgs) Handles txt3.TextChangedDelayed
        'Try
        If (updating = False) Then
            Dim seli = txt3.SelectionStart
            Dim vsv As Integer = txt3.VerticalScroll.Value
            Dim hsv As Integer = txt3.HorizontalScroll.Value
            Dim spacelen = IIf(autoSpace, 12, 2)
            Debug.WriteLine($"vsv: {vsv}, hsv: {hsv}")

            updating = True
            'Debug.WriteLine(txt3.Selection.Start)
            'If autoSpace Then
            Dim text = ""
            For i = 0 To txt3.LinesCount - 1
                Dim foundexclam = False
                Dim pars() As String = txt3.Lines(i).Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                Dim str = ""
                For j = 0 To pars.Count - 1
                    If (pars(j).Contains("!") Or pars(j).Contains("[")) Then
                        foundexclam = True
                    End If
                    If j <> pars.Count - 1 Then
                        If foundexclam = False Then
                            If (Not pars(j).ToLower.Contains("hingevec")) Then
                                str += String.Format("{0,-" & spacelen.ToString & "}", pars(j).Replace(" ", "")) + " "
                            Else
                                str += String.Format("{0,-" & (spacelen * 3).ToString & "}", pars(j).Replace(" ", "")) + " "
                            End If

                        Else
                            str += pars(j).Replace(" ", "") + " "
                        End If

                    Else
                        str += pars(j).Replace(" ", "")
                    End If
                Next
                text += str + Environment.NewLine
            Next
            txt3.Text = text
            'End If
            'clear previous highlighting
            With txt3.Range
                .ClearStyle()
                .ClearFoldingMarkers()
                .SetStyle(blueStyle, "(?i:Mach|IYsym|IZsym|Zsym|Sref|Cref|Bref|Xref|Yref|Zref|Nchordwise|Cspace|Nspanwise|Sspace|Xle|Yle|Zle|Chord|Ainc|Nspanwise|Sspace|Cname|Cgain|Xhinge|HingeVec|SgnDup|YDUPLICATE|ANGLE|mass|\bx\b|\by\b|\bz\b|Ixx|Iyy|Izz|alpha|CL|beta|pb/2V|qc/2V|rb/2V|aileron|\bflap\b|Cl roll mom|elevator|Cm pitchmom|rudder|Cn yaw  mom|beta|CL|CDo|\bbank\b|elevation|heading|velocity|density|grav.acc.|turn_rad.|load_fac.|X_cg|Y_cg|Z_cg|Ixy|Iyz|Izx|\bvisc\b|\bCL_a\b|\bCL_u\b|\bCM_a\b|\bCM_u\b)", RegexOptions.ExplicitCapture)
                .SetStyle(greenStyle, "(?i:\bsurface\b|\bsection\b|\bcontrol\b)", RegexOptions.ExplicitCapture)
                .SetStyle(lightgreenStyle, "(?i:#.*)")
                .SetStyle(lightgreenStyle, "!.*$", RegexOptions.Multiline)
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

            txt3.SelectAll()
            txt3.DoAutoIndent()
            txt3.SelectionStart = seli
            txt3.SelectionLength = 0
            Debug.WriteLine($"vsv: {vsv}, hsv: {hsv}")
            txt3.VerticalScroll.Value = vsv
            txt3.HorizontalScroll.Value = hsv
            txt3.UpdateScrollbars()
            'txt3.AdjustFolding()

            Debug.WriteLine($"vsv: {txt3.VerticalScroll.Value}/{txt3.VerticalScroll.Maximum}, hsv: {txt3.HorizontalScroll.Value}/{txt3.VerticalScroll.Maximum}")



            updating = False




        End If
        'Try

        'Catch
        'End Try
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



    Private Sub txt3_ToolTipNeeded(sender As Object, e As ToolTipNeededEventArgs) Handles txt3.ToolTipNeeded

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
                    e.ToolTipText = readLines(help, 1129, 1180) + vbNewLine + vbNewLine + readLines(help, 1197, 1230)
                Case "mass", "ixx", "iyy", "izz", "yref", "zref"
                    e.ToolTipTitle = "Header data"
                    e.ToolTipText = readLines(help, 1233, 1300)

                Case Else
                    e.ToolTipTitle = e.HoveredWord
                    e.ToolTipText = "No information available for '" + e.HoveredWord & "'" + vbNewLine + vbNewLine + "Use the help button (?) at in the menu to look for more information in the AVL documentation"
            End Select
        End If
    End Sub


    Public Function readLines(ByVal path As String, ByVal startline As Integer, Optional endline As Integer = 0) As String
        Dim result As String = ""
        If endline = 0 Then
            endline = startline
        End If
        Dim all() As String = File.ReadAllLines(path)
        For i = startline To endline
            result += vbNewLine + all.ElementAt(i).ToString
        Next
        Return result

    End Function



    'Private Sub txt3_AutoIndentNeeded(sender As Object, e As AutoIndentEventArgs) Handles txt3.AutoIndentNeeded
    '' if current line is "begin" then next
    '' line shift to right
    ''MsgBox("in indent")
    'If e.LineText.ToLower.Trim() = "!begin" Then
    '    e.ShiftNextLines = e.TabLength
    '    Return
    'End If
    '' if current line is "end" then current
    '' and next line shift to left
    'If e.LineText.ToLower.Trim() = "!end" Then
    '    e.Shift = -e.TabLength
    '    e.ShiftNextLines = -e.TabLength
    '    Return
    'End If
    'End Sub

    Private Function TrimAll(Text As String, Optional filename As String = "") As String

        If Text.Length = 0 Then Return "" 'zero len string



        Dim result = Text

        While (result.IndexOf("  ") > -1)
            result = result.Replace("  ", " ")
        End While

        'Const toRemove As String = " " & vbTab & vbCr & vbLf 'what to remove

        Dim lines = result.Split(Environment.NewLine)
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
        '    result += IIf(result.Length = 0, "", vbNewLine) + Mid(Str, s, (e - s) + 1) 'return remaining text
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



    Private Sub btnHide_CheckStateChanged(sender As Object, e As EventArgs) Handles btnHide.CheckStateChanged
        lblNote.Visible = btnHide.Checked
    End Sub





    Private Sub MassTemplateToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles MassTemplateToolStripMenuItem.Click
        Dim f = rootPath + "\template_mass.txt"
        Dim val = File.ReadAllText(f)
        txt3.Text = val
        txt3.SelectionStart = txt3.Text.Length + vbNewLine.Length
        txt3.DoCaretVisible()

    End Sub





    Private Sub RunTemplateToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RunTemplateToolStripMenuItem.Click
        Dim f = rootPath + "\template_run.txt"
        Dim val = File.ReadAllText(f)
        txt3.Text = val
        txt3.SelectionStart = txt3.Text.Length + vbNewLine.Length
        txt3.DoCaretVisible()

    End Sub


    Private Sub p1_SizeChanged(sender As Object, e As EventArgs) Handles pxy.SizeChanged
        drawAxes()
    End Sub


    Private Sub pxz_MouseDown(sender As Object, e As MouseEventArgs) Handles pxz.MouseDown
        If e.Button = MouseButtons.Left Then
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
        If e.Button = MouseButtons.Left Then
            xoffset = e.X - xdown
            zoffset = e.Y - zdown
            drawAxes()
        End If
        Dim s As String = ""
        Dim e1 As Double = (xmax - xmin) / pxy.Width * (e.X - (pxz.Width / 2) - xoffset)
        Dim t1 As String = "[X: " + String.Format("{0,5:###.0}", Math.Round(e1, 1)) '.ToString("{0,5:###.0}")
        Dim e2 As Double = (zmax - zmin) / pxz.Height * (e.Y - (pxz.Height / 2) - zoffset)
        Dim t2 As String = ", Z: " + String.Format("{0,5:###.0}", Math.Round(-e2, 1)) + "]" '.ToString("0.0") + "]"
        s = t1 + t2
        curX = e1
        curZ = -e2
        lblCursor.Text = "Cursor: " + s '+ e.X.ToString + ", " + e.Y.ToString

    End Sub

    Private Sub pxz_MouseUp(sender As Object, e As MouseEventArgs) Handles pxz.MouseUp
        pxz.Cursor = Cursors.Default
        If e.Button = MouseButtons.Left Then
            'points.Add(New Node(curX, curY, 0))
            drawAxes()
        End If
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

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Me.Select()
        Timer1.Enabled = False
        btnLoadG.PerformClick()
        'tc1.SelectTab("tabSide")
    End Sub

    Private Sub pxz_SizeChanged(sender As Object, e As EventArgs) Handles pxz.SizeChanged
        drawAxes()
    End Sub

    Private Sub p3d_SizeChanged(sender As Object, e As EventArgs)
        drawAxes()
    End Sub

    Private Sub pyz_Click(sender As Object, e As EventArgs) Handles pyz.Click

    End Sub

    Private Sub pyz_MouseDown(sender As Object, e As MouseEventArgs) Handles pyz.MouseDown
        If e.Button = MouseButtons.Left Then
            pyz.Cursor = Cursors.SizeAll
            ydown = e.X - yoffset
            zdown = e.Y - zoffset
        End If
    End Sub

    Private Sub pyz_MouseMove(sender As Object, e As MouseEventArgs) Handles pyz.MouseMove
        If e.Button = MouseButtons.Left Then
            yoffset = e.X - ydown
            zoffset = e.Y - zdown
            drawAxes()
        End If
        Dim s As String = ""
        Dim e1 As Double = (ymax - ymin) / pyz.Width * (e.X - (pyz.Width / 2) - yoffset)
        Dim t1 As String = "[Y: " + String.Format("{0,5:###.0}", Math.Round(e1, 1)) '.ToString("{0,5:###.0}")
        Dim e2 As Double = (zmax - zmin) / pyz.Height * (e.Y - (pyz.Height / 2) - zoffset)
        Dim t2 As String = ", Z: " + String.Format("{0,5:###.0}", Math.Round(-e2, 1)) + "]" '.ToString("0.0") + "]"
        s = t1 + t2
        curY = e1
        curZ = -e2
        lblCursor.Text = "Cursor: " + s '+ e.X.ToString + ", " + e.Y.ToString

    End Sub

    Private Sub pyz_MouseUp(sender As Object, e As MouseEventArgs) Handles pyz.MouseUp
        pyz.Cursor = Cursors.Default
        If e.Button = MouseButtons.Left Then
            'points.Add(New Node(curX, curY, 0))
            drawAxes()
        End If
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

    Private Sub pyz_SizeChanged(sender As Object, e As EventArgs) Handles pyz.SizeChanged
        drawAxes()
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

        Dim r = MultiplyMatrixVector(i.X, i.Y, i.Z, m)

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

    Private Sub bg1_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles bg1.DoWork
        'If (tc1.SelectedTab.Name = "tabSide") Then
        On Error GoTo errHandler
        'Try
        'XY plane========================================================================
        Dim BMP As Bitmap = New Bitmap(pxy.Width, pxy.Height)
        Dim G As Graphics = Graphics.FromImage(BMP)
        G.SmoothingMode = SmoothingMode.AntiAlias
        G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias
        Dim tickFont As Font = New Font(FontFamily.GenericMonospace.Name, Single.Parse(baseFontsize / (gridnumber / 10)), FontStyle.Regular)
        Dim axisFont As Font = New Font(FontFamily.GenericSerif.Name, Single.Parse(12), FontStyle.Regular)
        gridstep = pxy.Width / gridnumber
        Dim x0 As Integer = (pxy.Width / 2) + xoffset
        Dim y0 As Integer = (pxy.Height / 2) + yoffset
        Dim ci = 0
        Dim xcount = 0
        Dim maxstep = 0
        Dim origin = 0
        Dim ycount = 0
        Dim curxx
        Dim curyx
        Dim epsx
        Dim pointsx As List(Of Node) = New List(Of Node)
        Dim radius As Integer = 3
        Dim pointsx2 As List(Of Node)


        Dim fNear = 0
        Dim fFar = 100
        Dim fFOV = 90
        Dim fAspectRatio = pxy.Width / pxy.Height
        Dim fFOVRad = 1 / Math.Tan(fFOV * 0.5 / 180 * Math.PI)
        Dim mat4x4(,) As Single = {{0, 0, 0, 0}, {0, 0, 0, 0}, {0, 0, 0, 0}, {0, 0, 0, 0}}
        mat4x4(0, 0) = fAspectRatio * fFOVRad
        mat4x4(1, 1) = fFOVRad
        mat4x4(2, 2) = fFar / (fFar - fNear)
        mat4x4(3, 2) = (-fFar * fNear) / (fFar - fNear)
        mat4x4(2, 3) = 1
        mat4x4(3, 3) = 0


        'Draw grids 
        ci = -gridstep
        xcount = pxy.Width / gridstep
        xmin = (-xcount / 2) - (xoffset / gridstep)
        xmax = xmin + xcount
        maxstep = Math.Max(Math.Abs(xmax), Math.Abs(xmin))
        For i = 0 To maxstep
            ci += gridstep
            If (Not show3D) Then
                G.DrawLine(pGrid, x0 + ci, 0, x0 + ci, pxy.Height)
                G.DrawString(i.ToString, tickFont, Brushes.Black, New PointF(x0 + ci, y0))
                G.DrawLine(pGrid, x0 - ci, 0, x0 - ci, pxy.Height)
                G.DrawString(-i.ToString, tickFont, Brushes.Black, New PointF(x0 - ci, y0))
            Else
                'Dim p1 = PerspectiveProjection(New Point3D(x0 + ci, 0, 0), fFar, pxy.Width, pxy.Height) 'MultiplyMatrixVector(New Point3D(x0 + ci, 0, 0), mat4x4)
                'Dim p2 = PerspectiveProjection(New Point3D(x0 + ci, pxy.Height, 0), fFar, pxy.Width, pxy.Height) 'MultiplyMatrixVector(New Point3D(x0 + ci, pxy.Height, 0), mat4x4)
                'Dim p3 = PerspectiveProjection(New Point3D(x0 + ci, y0, 0), fFar, pxy.Width, pxy.Height) 'MultiplyMatrixVector(New Point3D(x0 + ci, y0, 0), mat4x4)
                Dim p1 = getProjectedPoint(i, 0, 0, pxy.Width, pxy.Height, xmin, xmax, xoffset, yoffset, zoffset, mat4x4) 'MultiplyMatrixVector(New Point3D(x0 + ci, 0, 0), mat4x4)
                Dim p2 = getProjectedPoint(i, 100, 0, pxy.Width, pxy.Height, xmin, xmax, xoffset, yoffset, zoffset, mat4x4) 'MultiplyMatrixVector(New Point3D(x0 + ci, pxy.Height, 0), mat4x4)
                Dim p3 = getProjectedPoint(i, 0, 0, pxy.Width, pxy.Height, xmin, xmax, xoffset, yoffset, zoffset, mat4x4) 'MultiplyMatrixVector(New Point3D(x0 + ci, y0, 0), mat4x4)
                G.DrawLine(pGrid, CSng(p1.X), CSng(p1.Y), CSng(p2.X), CSng(p2.Y))
                G.DrawString(i.ToString, tickFont, Brushes.Black, New PointF(p3.X, p3.Y))
                'p1 = PerspectiveProjection(New Point3D(x0 - ci, 0, 0), fFar, pxy.Width, pxy.Height) 'MultiplyMatrixVector(New Point3D(x0 - ci, 0, 0), mat4x4)
                'p2 = PerspectiveProjection(New Point3D(x0 - ci, pxy.Height, 0), fFar, pxy.Width, pxy.Height) 'MultiplyMatrixVector(New Point3D(x0 - ci, pxy.Height, 0), mat4x4)
                'p3 = PerspectiveProjection(New Point3D(x0 - ci, y0, 0), fFar, pxy.Width, pxy.Height) 'MultiplyMatrixVector(New Point3D(x0 - ci, y0, 0), mat4x4)
                p1 = getProjectedPoint(-i, 0, 0, pxy.Width, pxy.Height, xmin, xmax, xoffset, yoffset, zoffset, mat4x4) 'MultiplyMatrixVector(New Point3D(x0 - ci, 0, 0), mat4x4)
                p2 = getProjectedPoint(-i, -100, 0, pxy.Width, pxy.Height, xmin, xmax, xoffset, yoffset, zoffset, mat4x4) 'MultiplyMatrixVector(New Point3D(x0 - ci, pxy.Height, 0), mat4x4)
                p3 = getProjectedPoint(-i, 0, 0, pxy.Width, pxy.Height, xmin, xmax, xoffset, yoffset, zoffset, mat4x4) 'MultiplyMatrixVector(New Point3D(x0 - ci, y0, 0), mat4x4)
                G.DrawLine(pGrid, CSng(p1.X), CSng(p1.Y), CSng(p2.X), CSng(p2.Y))
                G.DrawString(-i.ToString, tickFont, Brushes.Black, New PointF(p3.X, p3.Y))
            End If
            For j = 1 To gridnumbermini - 1
                Dim cj As Integer = ci + (gridstep / gridnumbermini) * j
                G.DrawLine(pGrid, x0 + cj, 0, x0 + cj, pxy.Height)
                G.DrawString((i + (j / gridnumbermini)).ToString, tickFont, Brushes.Black, New PointF(x0 + cj, y0))
                G.DrawLine(pGrid, x0 - cj, 0, x0 - cj, pxy.Height)
                G.DrawString(-(i + (j / gridnumbermini)).ToString, tickFont, Brushes.Black, New PointF(x0 - cj, y0))
            Next
        Next
        G.DrawLine(pAxis, x0, 0, x0, pxy.Height)
        G.DrawString(0.ToString, tickFont, Brushes.Black, New PointF(x0, y0))


        gridstep = pxy.Height / gridnumber
        ci = -gridstep
        ycount = pxy.Height / gridstep
        ymin = (-ycount / 2) + (yoffset / gridstep)
        ymax = ymin + ycount
        maxstep = Math.Max(Math.Abs(ymax), Math.Abs(ymin))
        For i = 0 To maxstep
            ci += gridstep
            G.DrawLine(pGrid, 0, y0 + ci, pxy.Width, y0 + ci)
            G.DrawString(i.ToString, tickFont, Brushes.Black, New PointF(x0, y0 - ci))
            G.DrawLine(pGrid, 0, y0 - ci, pxy.Width, y0 - ci)
            G.DrawString(-i.ToString, tickFont, Brushes.Black, New PointF(x0, y0 + ci))
            For j = 1 To gridnumbermini - 1
                Dim cj As Integer = ci + (gridstep / gridnumbermini) * j
                G.DrawLine(pGrid, 0, y0 + cj, pxy.Width, y0 + cj)
                G.DrawString((i + (j / gridnumbermini)).ToString, tickFont, Brushes.Black, New PointF(x0, y0 - cj))
                G.DrawLine(pGrid, 0, y0 - cj, pxy.Width, y0 - cj)
                G.DrawString(-(i + (j / gridnumbermini)).ToString, tickFont, Brushes.Black, New PointF(x0, y0 + cj))
            Next
        Next
        G.DrawLine(pAxis, 0, y0, pxy.Width, y0)



        origin = pxy.Width / 20
        G.DrawLine(pAxis, origin, origin + G.MeasureString("X", axisFont).Height / 2, origin * 2, origin + G.MeasureString("X", axisFont).Height / 2)
        G.DrawString("X", axisFont, Brushes.Black, New PointF(origin * 2, origin))
        G.DrawLine(pAxis, origin, origin + G.MeasureString("X", axisFont).Height / 2, origin, Single.Parse(origin * 0.1) + G.MeasureString("X", axisFont).Height / 2)
        G.DrawString("Y", axisFont, Brushes.Black, New PointF(origin - G.MeasureString("Y", axisFont).Width, Single.Parse(origin * 0.1)))

        'Me.Text = "[" + xmin.ToString("0.00") + "," + xmax.ToString("0.00") + "] , [" + ymin.ToString("0.00") + "," + ymax.ToString("0.00") + "]"

        pointsx = New List(Of Node)
        For Each p As Node In points
            Dim xscale = p.Point.X * (pxy.Width) / (xmax - xmin) + (pxy.Width / 2) + xoffset
            Dim yscale = -p.Point.Y * (pxy.Height) / (ymax - ymin) + (pxy.Height / 2) + yoffset
            pointsx.Add(New Node(xscale, yscale, 0, p.Surface, p.Hovered, p.lineNumber))
        Next

        curxx = Single.Parse(curX * (pxy.Width) / (xmax - xmin) + (pxy.Width / 2) + xoffset)
        curyx = Single.Parse(-curY * (pxy.Height) / (ymax - ymin) + (pxy.Height / 2) + yoffset)
        epsx = Single.Parse(eps * (pxy.Width) / (xmax - xmin))

        pointsx2 = New List(Of Node)(pointsx)
        If pointsx.Count > 2 Then
            While pointsx.Count > 0
                Dim name As String = pointsx(0).Surface
                Dim ps As List(Of PointF) = New List(Of PointF)
                Do Until (findname(pointsx, name) = -1)
                    ps.Add(New PointF(pointsx(findname(pointsx, name)).Point.X, pointsx(findname(pointsx, name)).Point.Y))
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
                'lblNote.Text += vbNewLine + str
                Dim myPath As GraphicsPath = New GraphicsPath()
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
            Dim fmass = Application.StartupPath + $"\{projectName}.mass"
            Dim lines = File.ReadAllLines(fmass)
            For Each line As String In lines
                Dim pars = line.Split(" ")
                Dim val As Double = 0
                If (Double.TryParse(pars(0), val)) Then
                    Dim xmass As Double = Double.Parse(pars(1)) * (pxy.Width) / (xmax - xmin) + (pxy.Width / 2) + xoffset
                    Dim ymass As Double = Double.Parse(pars(2)) * (pxy.Height) / (ymax - ymin) + (pxy.Height / 2) + yoffset
                    G.FillEllipse(Brushes.Blue, New RectangleF(xmass - radius, ymass - radius, radius * 2, radius * 2))
                End If
            Next
        End If





        'Me.Text = curY.ToString + ", " + curyx.ToString + " | " + ymin.ToString + "," + ymax.ToString + " | " + yoffset.ToString
        'G.DrawRectangle(Pens.Red, curxx - epsx, curyx - epsx, epsx * 2, epsx * 2) 'draw selection region
        pxy.Image = BMP

        'XZ plane========================================================================
        BMP = New Bitmap(pxz.Width, pxz.Height)
        G = Graphics.FromImage(BMP)
        G.SmoothingMode = SmoothingMode.AntiAlias
        G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias
        gridstep = pxz.Width / gridnumber
        x0 = (pxz.Width / 2) + xoffset
        Dim z0 As Integer = (pxz.Height / 2) + zoffset


        ci = -gridstep
        xcount = pxz.Width / gridstep
        xmin = (-xcount / 2) - (xoffset / gridstep)
        xmax = xmin + xcount
        maxstep = Math.Max(Math.Abs(xmax), Math.Abs(xmin))
        For i = 0 To maxstep
            ci += gridstep
            G.DrawLine(pGrid, x0 + ci, 0, x0 + ci, pxz.Height)
            G.DrawString(i.ToString, tickFont, Brushes.Black, New PointF(x0 + ci, z0))
            G.DrawLine(pGrid, x0 - ci, 0, x0 - ci, pxz.Height)
            G.DrawString(-i.ToString, tickFont, Brushes.Black, New PointF(x0 - ci, z0))
            For j = 1 To gridnumbermini - 1
                Dim cj As Integer = ci + (gridstep / gridnumbermini) * j
                G.DrawLine(pGrid, x0 + cj, 0, x0 + cj, pxz.Height)
                G.DrawString((i + (j / gridnumbermini)).ToString, tickFont, Brushes.Black, New PointF(x0 + cj, z0))
                G.DrawLine(pGrid, x0 - cj, 0, x0 - cj, pxz.Height)
                G.DrawString(-(i + (j / gridnumbermini)).ToString, tickFont, Brushes.Black, New PointF(x0 - cj, z0))
            Next
        Next
        G.DrawLine(pAxis, x0, 0, x0, pxz.Height)
        G.DrawString(0.ToString, tickFont, Brushes.Black, New PointF(x0, z0))


        gridstep = pxz.Height / gridnumber
        ci = -gridstep
        Dim zcount = pxz.Height / gridstep
        zmin = (-zcount / 2) + (zoffset / gridstep)
        zmax = zmin + zcount
        maxstep = Math.Max(Math.Abs(ymax), Math.Abs(ymin))
        For i = 0 To maxstep
            ci += gridstep
            G.DrawLine(pGrid, 0, z0 + ci, pxz.Width, z0 + ci)
            G.DrawString(i.ToString, tickFont, Brushes.Black, New PointF(x0, z0 - ci))
            G.DrawLine(pGrid, 0, z0 - ci, pxz.Width, z0 - ci)
            G.DrawString(-i.ToString, tickFont, Brushes.Black, New PointF(x0, z0 + ci))
            For j = 1 To gridnumbermini - 1
                Dim cj As Integer = ci + (gridstep / gridnumbermini) * j
                G.DrawLine(pGrid, 0, z0 + cj, pxz.Width, z0 + cj)
                G.DrawString((i + (j / gridnumbermini)).ToString, tickFont, Brushes.Black, New PointF(x0, z0 - cj))
                G.DrawLine(pGrid, 0, z0 - cj, pxz.Width, z0 - cj)
                G.DrawString(-(i + (j / gridnumbermini)).ToString, tickFont, Brushes.Black, New PointF(x0, z0 + cj))
            Next
        Next
        G.DrawLine(pAxis, 0, z0, pxz.Width, z0)

        origin = pxz.Width / 20
        G.DrawLine(pAxis, origin, origin + G.MeasureString("X", axisFont).Height / 2, origin * 2, origin + G.MeasureString("X", axisFont).Height / 2)
        G.DrawString("X", axisFont, Brushes.Black, New PointF(origin * 2, origin))
        G.DrawLine(pAxis, origin, origin + G.MeasureString("X", axisFont).Height / 2, origin, Single.Parse(origin * 0.1) + G.MeasureString("X", axisFont).Height / 2)
        G.DrawString("Z", axisFont, Brushes.Black, New PointF(origin - G.MeasureString("Z", axisFont).Width, Single.Parse(origin * 0.1)))


        'Me.Text = "[" + xmin.ToString("0.00") + "," + xmax.ToString("0.00") + "] , [" + ymin.ToString("0.00") + "," + ymax.ToString("0.00") + "]"

        curyx = Single.Parse(-curY * (pxz.Height) / (ymax - ymin) + (pxz.Height / 2) + yoffset)
        Dim curzx = Single.Parse(curZ * (pxz.Width) / (zmax - zmin) + (pxz.Width / 2) + zoffset)
        'epsy = Single.Parse(eps * (pxz.Width) / (ymax - ymin))


        'radius = 5
        pointsx = New List(Of Node)
        For Each p As Node In points
            Dim xscale = p.Point.X * (pxz.Width) / (xmax - xmin) + (pxz.Width / 2) + xoffset
            Dim zscale = -p.Point.Z * (pxz.Height) / (zmax - zmin) + (pxz.Height / 2) + zoffset
            pointsx.Add(New Node(xscale, zscale, 0, p.Surface, p.Hovered, p.lineNumber))
        Next
        pointsx2 = New List(Of Node)(pointsx)
        If pointsx.Count > 2 Then
            While pointsx.Count > 0
                Dim name As String = pointsx(0).Surface
                Dim ps As List(Of PointF) = New List(Of PointF)
                Do Until (findname(pointsx, name) = -1)
                    ps.Add(New PointF(pointsx(findname(pointsx, name)).Point.X, pointsx(findname(pointsx, name)).Point.Y))
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
                'lblNote.Text += vbNewLine + str
                Dim myPath As GraphicsPath = New GraphicsPath()
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
        'G.DrawRectangle(Pens.Red, curyx - epsy, curzx - epsy, epsy * 2, epsy * 2) 'draw selection region

        If (showMass = True And File.Exists(Application.StartupPath + $"\{projectName}.mass")) Then
            Dim fmass = Application.StartupPath + $"\{projectName}.mass"
            Dim lines = File.ReadAllLines(fmass)
            For Each line As String In lines
                Dim pars = line.Split(" ")
                Dim val As Double = 0
                If (Double.TryParse(pars(0), val)) Then
                    Dim xmass As Double = Double.Parse(pars(1)) * (pxz.Width) / (xmax - xmin) + (pxz.Width / 2) + xoffset
                    Dim ymass As Double = Double.Parse(pars(3)) * (pxz.Height) / (zmax - zmin) + (pxz.Height / 2) + zoffset
                    G.FillEllipse(Brushes.Blue, New RectangleF(xmass - radius, ymass - radius, radius * 2, radius * 2))
                End If
            Next
        End If


        pxz.Image = BMP

        'YZ plane========================================================================
        BMP = New Bitmap(pyz.Width, pyz.Height)
        G = Graphics.FromImage(BMP)
        G.SmoothingMode = SmoothingMode.AntiAlias
        G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias
        gridstep = pyz.Width / gridnumber
        y0 = (pxz.Width / 2) + yoffset
        z0 = (pxz.Height / 2) + zoffset


        ci = -gridstep
        ycount = pyz.Width / gridstep
        ymin = (-ycount / 2) - (yoffset / gridstep)
        ymax = ymin + ycount
        maxstep = Math.Max(Math.Abs(ymax), Math.Abs(ymin))
        For i = 0 To maxstep
            ci += gridstep
            G.DrawLine(pGrid, y0 + ci, 0, y0 + ci, pyz.Height)
            G.DrawString(i.ToString, tickFont, Brushes.Black, New PointF(y0 + ci, z0))
            G.DrawLine(pGrid, y0 - ci, 0, y0 - ci, pyz.Height)
            G.DrawString(-i.ToString, tickFont, Brushes.Black, New PointF(y0 - ci, z0))
            For j = 1 To gridnumbermini - 1
                Dim cj As Integer = ci + (gridstep / gridnumbermini) * j
                G.DrawLine(pGrid, y0 + cj, 0, y0 + cj, pyz.Height)
                G.DrawString((i + (j / gridnumbermini)).ToString, tickFont, Brushes.Black, New PointF(y0 + cj, z0))
                G.DrawLine(pGrid, y0 - cj, 0, y0 - cj, pyz.Height)
                G.DrawString(-(i + (j / gridnumbermini)).ToString, tickFont, Brushes.Black, New PointF(y0 - cj, z0))
            Next
        Next
        G.DrawLine(pAxis, y0, 0, y0, pyz.Height)
        G.DrawString(0.ToString, tickFont, Brushes.Black, New PointF(y0, z0))


        gridstep = pyz.Height / gridnumber
        ci = -gridstep
        zcount = pyz.Height / gridstep
        zmin = (-zcount / 2) + (zoffset / gridstep)
        zmax = zmin + zcount
        maxstep = Math.Max(Math.Abs(zmax), Math.Abs(zmin))
        For i = 0 To maxstep
            ci += gridstep
            G.DrawLine(pGrid, 0, z0 + ci, pyz.Width, z0 + ci)
            G.DrawString(i.ToString, tickFont, Brushes.Black, New PointF(y0, z0 - ci))
            G.DrawLine(pGrid, 0, z0 - ci, pyz.Width, z0 - ci)
            G.DrawString(-i.ToString, tickFont, Brushes.Black, New PointF(y0, z0 + ci))
            For j = 1 To gridnumbermini - 1
                Dim cj As Integer = ci + (gridstep / gridnumbermini) * j
                G.DrawLine(pGrid, 0, z0 + cj, pyz.Width, z0 + cj)
                G.DrawString((i + (j / gridnumbermini)).ToString, tickFont, Brushes.Black, New PointF(y0, z0 - cj))
                G.DrawLine(pGrid, 0, z0 - cj, pyz.Width, z0 - cj)
                G.DrawString(-(i + (j / gridnumbermini)).ToString, tickFont, Brushes.Black, New PointF(y0, z0 + cj))
            Next
        Next
        G.DrawLine(pAxis, 0, z0, pyz.Width, z0)

        origin = pyz.Width / 20
        G.DrawLine(pAxis, origin, origin + G.MeasureString("Z", axisFont).Height / 2, origin * 2, origin + G.MeasureString("Z", axisFont).Height / 2)
        G.DrawString("Y", axisFont, Brushes.Black, New PointF(origin * 2, origin))
        G.DrawLine(pAxis, origin, origin + G.MeasureString("Z", axisFont).Height / 2, origin, Single.Parse(origin * 0.1) + G.MeasureString("Z", axisFont).Height / 2)
        G.DrawString("Z", axisFont, Brushes.Black, New PointF(origin - G.MeasureString("Z", axisFont).Width, Single.Parse(origin * 0.1)))


        'Me.Text = "[" + xmin.ToString("0.00") + "," + xmax.ToString("0.00") + "] , [" + ymin.ToString("0.00") + "," + ymax.ToString("0.00") + "]"

        'radius = 5
        pointsx = New List(Of Node)
        For Each p As Node In points
            Dim yscale = p.Point.Y * (pyz.Width) / (ymax - ymin) + (pyz.Width / 2) + yoffset
            Dim zscale = -p.Point.Z * (pyz.Height) / (zmax - zmin) + (pyz.Height / 2) + zoffset
            pointsx.Add(New Node(yscale, zscale, 0, p.Surface, p.Hovered, p.lineNumber))
        Next
        pointsx2 = New List(Of Node)(pointsx)
        If pointsx.Count > 2 Then
            While pointsx.Count > 0
                Dim name As String = pointsx(0).Surface
                Dim ps As List(Of PointF) = New List(Of PointF)
                Do Until (findname(pointsx, name) = -1)
                    ps.Add(New PointF(pointsx(findname(pointsx, name)).Point.X, pointsx(findname(pointsx, name)).Point.Y))
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
                'lblNote.Text += vbNewLine + str
                Dim myPath As GraphicsPath = New GraphicsPath()
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
            Dim fmass = Application.StartupPath + $"\{projectName}.mass"
            Dim lines = File.ReadAllLines(fmass)
            For Each line As String In lines
                Dim pars = line.Split(" ")
                Dim val As Double = 0
                If (Double.TryParse(pars(0), val)) Then
                    Dim xmass As Double = Double.Parse(pars(2)) * (pyz.Width) / (ymax - ymin) + (pyz.Width / 2) + yoffset
                    Dim ymass As Double = Double.Parse(pars(3)) * (pyz.Height) / (zmax - zmin) + (pyz.Height / 2) + zoffset
                    G.FillEllipse(Brushes.Blue, New RectangleF(xmass - radius, ymass - radius, radius * 2, radius * 2))
                End If
            Next
        End If


        pyz.Image = BMP

        'Else

        '    '3D plane========================================================================
        '    Dim BMP As Bitmap = New Bitmap(p3d.Width, p3d.Height)
        '    Dim G As Graphics = Graphics.FromImage(BMP)
        '    G.SmoothingMode = SmoothingMode.AntiAlias
        '    Dim tickFont As Font = New Font(FontFamily.GenericMonospace.Name, Single.Parse(baseFontsize / (gridnumber / 10)), FontStyle.Regular)
        '    Dim axisFont As Font = New Font(FontFamily.GenericSerif.Name, Single.Parse(12), FontStyle.Regular)
        '    gridstep = p3d.Width / gridnumber
        '    Dim x0 As Integer = (p3d.Width / 2) + xoffset
        '    Dim y0 As Integer = (p3d.Height / 2) + yoffset
        '    Dim z0 As Integer = (p3d.Height / 2) + zoffset
        '    Dim angle = -45

        '    Dim ci As Integer = -gridstep
        '    Dim xcount = p3d.Width / gridstep
        '    xmin = (-xcount / 2) - (xoffset / gridstep)
        '    xmax = xmin + xcount
        '    Dim maxstep = Math.Max(Math.Abs(xmax), Math.Abs(xmin))
        '    Dim x1, x2 As Node
        '    Dim distance As Integer = 100
        '    For i = 0 To maxstep
        '        ci += gridstep
        '        x1 = New Node(x0 - ci, 0, 0, "", False, 0).RotateZ(angle).Project(p3d.Width, p3d.Height, distance)
        '        x2 = New Node(x0, 0, 0, "", False, 0).RotateZ(angle).Project(p3d.Width, p3d.Height, distance)
        '        G.DrawLine(pGrid, x1.X, x1.Y, x2.X, x2.Y)
        '        G.DrawString(i.ToString, tickFont, Brushes.Black, New PointF(x1.X, x1.Y))
        '        x1 = New Node(x0 + ci, 0, 0, "", False, 0).RotateZ(angle).Project(p3d.Width, p3d.Height, distance)
        '        x2 = New Node(x0, 0, 0, "", False, 0).RotateZ(angle).Project(p3d.Width, p3d.Height, distance)
        '        G.DrawLine(pGrid, x1.X, x1.Y, x2.X, x2.Y)
        '        G.DrawString(-i.ToString, tickFont, Brushes.Black, New PointF(x1.X, x1.Y))
        '        'For j = 1 To gridnumbermini - 1
        '        '    Dim cj As Integer = ci + (gridstep / gridnumbermini) * j
        '        '    x1 = New Node(x0 + cj, 0, 0, "", False, 0).RotateZ(angle).Project(p3d.Width, p3d.Height, distance)
        '        '    x2 = New Node(x0 + cj, 0, 0, "", False, 0).RotateZ(angle).Project(p3d.Width, p3d.Height, distance)
        '        '    G.DrawLine(pGrid, x1.X, x1.Y, x2.X, x2.Y)
        '        '    G.DrawString((i + (j / gridnumbermini)).ToString, tickFont, Brushes.Black, New PointF(x1.X, x1.Y))
        '        '    x1 = New Node(x0 - cj, 0, 0, "", False, 0).RotateZ(angle).Project(p3d.Width, p3d.Height, distance)
        '        '    x2 = New Node(x0 - cj, 0, 0, "", False, 0).RotateZ(angle).Project(p3d.Width, p3d.Height, distance)
        '        '    G.DrawLine(pGrid, x1.X, x1.Y, x2.X, x2.Y)
        '        '    G.DrawString(-(i + (j / gridnumbermini)).ToString, tickFont, Brushes.Black, New PointF(x1.X, x1.Y))
        '        'Next
        '    Next
        '    x1 = New Node(-p3d.Height, 0, 0, "", False, 0).RotateZ(angle).Project(p3d.Width, p3d.Height, distance)
        '    'x1.Project(x1.X, x1.Y, x1.Z, p3d.Width, p3d.Height, 100)
        '    x2 = New Node(p3d.Height, 0, 0, "", False, 0).RotateZ(angle).Project(p3d.Width, p3d.Height, distance)
        '    'x2.Project(x2.X, x2.Y, x2.Z, p3d.Width, p3d.Height, 100)
        '    G.DrawLine(pAxis, x1.X, x1.Y, x2.X, x2.Y)
        '    G.DrawString(0.ToString, tickFont, Brushes.Black, New PointF(x1.X, x1.Y))

        '    'ci = -gridstep
        '    'Dim ycount = p3d.Height / gridstep
        '    'ymin = (-ycount / 2) + (yoffset / gridstep)
        '    'ymax = ymin + ycount
        '    'maxstep = Math.Max(Math.Abs(ymax), Math.Abs(ymin))
        '    'For i = 0 To maxstep
        '    '    ci += gridstep
        '    '    G.DrawLine(pGrid, 0, y0 + ci, p3d.Width, y0 + ci)
        '    '    G.DrawString(i.ToString, tickFont, Brushes.Black, New PointF(x0, y0 - ci))
        '    '    G.DrawLine(pGrid, 0, y0 - ci, p3d.Width, y0 - ci)
        '    '    G.DrawString(-i.ToString, tickFont, Brushes.Black, New PointF(x0, y0 + ci))
        '    '    For j = 1 To gridnumbermini - 1
        '    '        Dim cj As Integer = ci + (gridstep / gridnumbermini) * j
        '    '        G.DrawLine(pGrid, 0, y0 + cj, p3d.Width, y0 + cj)
        '    '        G.DrawString((i + (j / gridnumbermini)).ToString, tickFont, Brushes.Black, New PointF(x0, y0 - cj))
        '    '        G.DrawLine(pGrid, 0, y0 - cj, p3d.Width, y0 - cj)
        '    '        G.DrawString(-(i + (j / gridnumbermini)).ToString, tickFont, Brushes.Black, New PointF(x0, y0 + cj))
        '    '    Next
        '    'Next
        '    'G.DrawLine(pAxis, 0, y0, p3d.Width, y0)

        '    'Dim origin As Single = p3d.Width / 20
        '    'G.DrawLine(pAxis, origin, origin + G.MeasureString("X", axisFont).Height / 2, origin * 2, origin + G.MeasureString("X", axisFont).Height / 2)
        '    'G.DrawString("X", axisFont, Brushes.Black, New PointF(origin * 2, origin))
        '    'G.DrawLine(pAxis, origin, origin + G.MeasureString("X", axisFont).Height / 2, origin, Single.Parse(origin * 0.1) + G.MeasureString("X", axisFont).Height / 2)
        '    'G.DrawString("Y", axisFont, Brushes.Black, New PointF(origin - G.MeasureString("Y", axisFont).Width, Single.Parse(origin * 0.1)))

        '    ''Me.Text = "[" + xmin.ToString("0.00") + "," + xmax.ToString("0.00") + "] , [" + ymin.ToString("0.00") + "," + ymax.ToString("0.00") + "]"

        '    'Dim radius As Integer = 3
        '    'Dim pointsx As List(Of Node) = New List(Of Node)
        '    'For Each p As Node In points
        '    '    Dim xscale = p.Point.X * (p3d.Width) / (xmax - xmin) + (p3d.Width / 2) + xoffset
        '    '    Dim yscale = -p.Point.Y * (p3d.Height) / (ymax - ymin) + (p3d.Height / 2) + yoffset
        '    '    pointsx.Add(New Node(xscale, yscale, 0, p.Surface, p.Hovered, p.lineNumber))
        '    'Next


        '    p3d.Image = BMP
        '    Exit Sub
        'End If
        'Catch e As Exception
        'MsgBox("DrawInPicture Error: " + e.GetBaseException.Message)
        'End Try
errHandler:
        bg1.CancelAsync()
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
        baseFontsize -= 1
        drawAxes()
    End Sub

    Private Sub btnBasefontplus_Click(sender As Object, e As EventArgs) Handles btnBasefontplus.Click
        baseFontsize += 1
        drawAxes()

    End Sub

    Private Sub frmGeometry_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        drawAxes()
    End Sub

    Private Sub btnDisplay_Click(sender As Object, e As EventArgs) Handles btnDisplay.Click
        Select Case btnDisplay.Text.ToLower()
            Case "Show Editor Only".ToLower()
                btnDisplay.Text = "Show Editor and XY Plane Only"
                tlp1.Controls.Remove(pxy)
                tlp1.Controls.Remove(pyz)
                tlp1.Controls.Remove(pxz)
                tlp1.SetRowSpan(txt3, 2)
                tlp1.SetColumnSpan(txt3, 2)
            Case "Show Editor and XY Plane Only".ToLower()
                btnDisplay.Text = "Show Editor and All Planes"
                tlp1.SetRowSpan(txt3, 2)
                tlp1.SetColumnSpan(txt3, 1)
                tlp1.SetRowSpan(pxy, 2)
                tlp1.SetColumnSpan(pxy, 1)
                tlp1.Controls.Add(pxy, 1, 0)
                pxy.Dock = DockStyle.Fill
            Case "Show Editor and All Planes".ToLower()
                btnDisplay.Text = "Show Editor Only"
                tlp1.SetRowSpan(txt3, 1)
                tlp1.SetColumnSpan(txt3, 1)
                tlp1.SetRowSpan(pxy, 1)
                tlp1.SetColumnSpan(pxy, 1)
                tlp1.Controls.Add(pyz, 0, 1)
                tlp1.Controls.Add(pxz, 1, 1)
                pxz.Dock = DockStyle.Fill
                pyz.Dock = DockStyle.Fill

        End Select
    End Sub

    Private Sub SaveAVLToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles btnSaveG.Click
        Try
            Dim f = Application.StartupPath + $"\{projectName}.avl"
            'MsgBox(TrimAll(txt3.Text))
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

    Private Sub LoadAVLToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles btnLoadG.Click
        Try
            Dim f = Application.StartupPath + $"\{projectName}.avl"
            txt3.Text = File.ReadAllText(f)
        Catch ex As Exception
            MsgBox("Error: " + ex.Message)
        End Try

    End Sub

    Private Sub SaveMassToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles btnSaveM.Click
        Try
            Dim f = Application.StartupPath + $"\{projectName}.mass"
            'MsgBox(TrimAll(txt3.Text))
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

    Private Sub LoadMassToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles btnLoadM.Click
        Try
            Dim f = Application.StartupPath + $"\{projectName}.mass"
            txt3.Text = File.ReadAllText(f)
        Catch ex As Exception
            MsgBox("Error: " + ex.Message)
        End Try

    End Sub

    Private Sub SaveRunToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles btnSaveR.Click
        'Try
        Dim f = Application.StartupPath + $"\{projectName}.run"
        'MsgBox(TrimAll(txt3.Text))
        File.WriteAllText(f, TrimAll(txt3.Text, f))
        'Catch er As Exception
        'MsgBox("Error: " + er.Message)
        '    Try
        '        frmMain.p.Kill()
        '        frmMain.loadConsole()
        '    Catch
        '    End Try
        'End Try
    End Sub

    Private Sub LoadRunToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles btnLoadR.Click
        Try
            Dim f = Application.StartupPath + $"\{projectName}.run"
            txt3.Text = File.ReadAllText(f)
        Catch ex As Exception
            MsgBox("Error: " + ex.Message)
        End Try
    End Sub

    Private Sub TrefftzPlaneToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles btnTrefftz.Click
        Dim f = Application.StartupPath + $"\{projectName}.avl"
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
            .p.StandardInput.WriteLine("t")
            '.p.StandardInput.WriteLine("h")
            '.p.StandardInput.WriteLine()
            '.p.StandardInput.WriteLine()
        End With
        'Dim plot = Application.StartupPath + "\plot.ps"

        'CaptureApplication("PltLib", "Trefftz plane")
    End Sub

    Private Sub GeometryToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles btnTest.Click
        'btnSave_Click(sender, e)
        Dim f = Application.StartupPath + $"\{projectName}.avl"
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
            '.p.StandardInput.WriteLine()
            '.p.StandardInput.WriteLine()
            '.p.StandardInput.WriteLine()
            '.p.StandardInput.WriteLine("quit")

            '.p.StandardInput.WriteLine("k")
            'Dim psi = New PostScript_Interp()
            'Dim bmp = psi.Load(Path.Combine(Application.StartupPath, "plot.ps"))

            'Dim frm As New Form()
            'Dim p1 = New PictureBox()
            'p1.Image = bmp
            'frm.Controls.Add(p1)
            'p1.Dock = DockStyle.Fill
            'frm.Show()
            'SendKeys.Send("LLL")
        End With
        frmInfo.Show()
        'CaptureApplication("PltLib", "3D Geometry View")
    End Sub

    Private Sub SaveAVLTheShowGeometryToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles btnSaveView.Click
        btnSaveG.PerformClick()
        btnTest.PerformClick()
    End Sub

    Private Sub txtName_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub txtName_TextChanged(sender As Object, e As EventArgs) Handles txtName.TextChanged
        projectName = txtName.Text
        Me.Text = $"AVL - Designer - working on <{projectName}>"
        Me.btnSaveG.Text = $"Save Geometry ({projectName}.avl)"
        Me.btnLoadG.Text = $"Load Geometry ({projectName}.avl)"
        Me.btnSaveM.Text = $"Save Mass ({projectName}.mass)"
        Me.btnLoadM.Text = $"Load Mass ({projectName}.mass)"
        Me.btnSaveR.Text = $"Save Run ({projectName}.run)"
        Me.btnLoadR.Text = $"Load Run ({projectName}.run)"
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

    Private Sub txt3_Load(sender As Object, e As EventArgs) Handles txt3.Load

    End Sub

    Private Sub btnHelpMass_Click(sender As Object, e As EventArgs) Handles btnHelpMass.Click

    End Sub

    Private Sub btnSpace_Click(sender As Object, e As EventArgs) Handles btnSpace.Click
        If (btnSpace.Text.Contains("On")) Then
            autoSpace = False
            btnSpace.Text = "Auto Space: Off"
            txt3_TextChangedDelayed(sender, New TextChangedEventArgs(txt3.Range))
        Else
            autoSpace = True
            btnSpace.Text = "Auto Space: On"
            txt3_TextChangedDelayed(sender, New TextChangedEventArgs(txt3.Range))
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
            btnMass.Text = "Show Mass: Off"
        Else
            showMass = True
            btnMass.Text = "Show Mass: On"
        End If
        drawAxes()

    End Sub

    Private Sub btnControl_Click(sender As Object, e As EventArgs) Handles btnControl.Click
        If (btnControl.Text.Contains("On")) Then
            showControl = False
            btnControl.Text = "Show Control: Off"
        Else
            showControl = True
            btnControl.Text = "Show Control: On"
        End If
        drawAxes()

    End Sub

    Private Sub btnSection_Click(sender As Object, e As EventArgs) Handles btnSection.Click
        If (btnSection.Text.Contains("On")) Then
            showSection = False
            btnSection.Text = "Show Section: Off"
        Else
            showSection = True
            btnSection.Text = "Show Section: On"
        End If
        drawAxes()
    End Sub

    Private Sub lblNote_Click(sender As Object, e As EventArgs) Handles lblNote.Click

    End Sub

    Private Sub btn3D_Click(sender As Object, e As EventArgs) Handles btn3D.Click
        If (btn3D.Text.Contains("On")) Then
            show3D = False
            btn3D.Text = "Show 3D: Off"
        Else
            show3D = True
            btn3D.Text = "Show 3D: On"
        End If
        drawAxes()
    End Sub
End Class
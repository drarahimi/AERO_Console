Option Explicit On
Option Strict On

Imports System.Diagnostics
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading

''' <summary>
''' Standalone XFOIL analysis window: owns its own dedicated xfoil.exe child process
''' (independent of frmMain's engine selector / process), runs alpha-sweep polars and
''' single-point Cp/boundary-layer snapshots, and renders the results as native .NET
''' plots the same way frmGeometry's AVL "Polar" tab does (GDI+ via SvgGraphics, with
''' PNG/SVG/PDF export).
''' </summary>
Public Class frmXfoilAnalysis
    Inherits System.Windows.Forms.Form

#Region "Process management"

    Private p As Process
    Private bt As Thread
    Private Leaving As Boolean = False
    Private ReadOnly _logBuffer As New StringBuilder()
    Private ReadOnly _logBufferLock As New Object()
    Private WithEvents _logFlushTimer As New System.Windows.Forms.Timer With {.Interval = 75}

    Private ReadOnly _tempDir As String = Path.Combine(Path.GetTempPath(), "aeroconsole_xfoil")
    Private ReadOnly _polFile As String
    Private ReadOnly _cpFile As String
    Private ReadOnly _blFile As String

    Private Sub ReadThread()
        Try
            Dim buffer(4096) As Char
            Do Until Leaving OrElse p Is Nothing OrElse p.HasExited
                Dim bytesRead As Integer = p.StandardOutput.Read(buffer, 0, buffer.Length)
                If bytesRead <= 0 Then Exit Do
                Dim textChunk As String = New String(buffer, 0, bytesRead)
                SyncLock _logBufferLock
                    _logBuffer.Append(textChunk)
                End SyncLock
            Loop
        Catch
            ' Process exited or stream closed
        End Try
    End Sub

    Private Sub _logFlushTimer_Tick(sender As Object, e As EventArgs) Handles _logFlushTimer.Tick
        Dim pending As String = Nothing
        SyncLock _logBufferLock
            If _logBuffer.Length > 0 Then
                pending = _logBuffer.ToString()
                _logBuffer.Clear()
            End If
        End SyncLock
        If pending Is Nothing Then Return
        txtLog.AppendText(pending)
        txtLog.SelectionStart = txtLog.TextLength
        txtLog.ScrollToCaret()
    End Sub

    Private Sub StartProcess(appPath As String)
        If p IsNot Nothing Then
            Try
                If Not p.HasExited Then p.Kill()
                p.Dispose()
            Catch
            End Try
            p = Nothing
        End If

        p = New Process()
        Dim startinfo As New ProcessStartInfo()
        With startinfo
            .FileName = appPath
            .Arguments = ""
            .WorkingDirectory = Application.StartupPath
            .RedirectStandardError = False
            .RedirectStandardOutput = True
            .RedirectStandardInput = True
            .UseShellExecute = False
            .CreateNoWindow = True
            .EnvironmentVariables("GFORTRAN_UNBUFFERED_ALL") = "y"
            .EnvironmentVariables("GFORTRAN_UNBUFFERED_PRECONNECTED") = "y"
        End With
        p.StartInfo = startinfo
        p.EnableRaisingEvents = True
        p.Start()

        bt = New Thread(AddressOf ReadThread)
        bt.IsBackground = True
        bt.Start()

        txtLog.Clear()
        lblStatus.Text = "Status: XFOIL running"
    End Sub

    ''' <summary>
    ''' Starts (or restarts) the dedicated xfoil.exe process if needed, downloading it
    ''' first via frmMain's shared downloader if it isn't present in appdata\.
    ''' </summary>
    Private Async Function EnsureXfoilReadyAsync() As Task(Of Boolean)
        Dim appPath = Path.Combine(Application.StartupPath, "appdata", "xfoil.exe")
        If Not File.Exists(appPath) Then
            Dim downloaded = Await frmMain.DownloadXfoilAsync()
            If Not downloaded OrElse Not File.Exists(appPath) Then
                AppMessageBox.Show("XFOIL executable was not found and could not be downloaded.", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End If
        End If

        If p Is Nothing OrElse p.HasExited Then
            StartProcess(appPath)
            If p.HasExited Then
                AppMessageBox.Show("XFOIL process exited immediately! Exit code: " & p.ExitCode, "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return False
            End If
        End If
        Return True
    End Function

    Private Sub WriteCmd(text As String)
        p.StandardInput.WriteLine(text)
    End Sub

    Private Sub FlushCmd()
        p.StandardInput.Flush()
    End Sub

    ''' <summary>
    ''' Polls a file XFOIL is writing until its size stops growing (or times out),
    ''' which is far more reliable than scraping XFOIL's console echo the way
    ''' frmGeometry's AVL polar sweep diffs txtLog - XFOIL's pacc/cpwr/dump outputs
    ''' are plain fixed-format files, so waiting for the write to finish and then
    ''' parsing the file is simpler and less brittle.
    ''' </summary>
    Private Async Function WaitForFileStableAsync(path As String, timeoutMs As Integer) As Task(Of Boolean)
        Dim start = Environment.TickCount64
        Dim lastLen As Long = -1
        Dim stableTicks As Integer = 0
        Do
            Await Task.Delay(250)
            If File.Exists(path) Then
                Dim len As Long = -2
                Try
                    len = New FileInfo(path).Length
                Catch
                    ' still being written/locked - treat as not yet stable
                End Try
                If len = lastLen AndAlso len > 0 Then
                    stableTicks += 1
                    If stableTicks >= 2 Then Return True
                Else
                    stableTicks = 0
                End If
                lastLen = len
            End If
        Loop While Environment.TickCount64 - start < timeoutMs
        Try
            Return File.Exists(path) AndAlso New FileInfo(path).Length > 0
        Catch
            Return False
        End Try
    End Function

#End Region

#Region "UI fields"

    Private txtAirfoil As TextBox
    Private btnBrowseAirfoil As Button
    Private txtRe As TextBox
    Private txtMach As TextBox
    Private txtNcrit As TextBox
    Private txtAlphaMin As TextBox
    Private txtAlphaMax As TextBox
    Private txtAlphaStep As TextBox
    Private txtSingleAlpha As TextBox
    Private btnRunPolar As Button
    Private btnRunPoint As Button
    Private lblStatus As Label
    Private txtLog As TextBox
    Private tc As TabControl
    Private pPolar As PictureBox
    Private pCp As PictureBox
    Private pBl As PictureBox

#End Region

#Region "Data"

    Private _polarPoints As New List(Of XfoilPolarPoint)
    Private _cpPoints As New List(Of XfoilCpPoint)
    Private _blTop As New List(Of XfoilBLPoint)
    Private _blBottom As New List(Of XfoilBLPoint)

    Private _polarSvg As String = "" : Private _polarPdf As String = ""
    Private _cpSvg As String = "" : Private _cpPdf As String = ""
    Private _blSvg As String = "" : Private _blPdf As String = ""

#End Region

    Public Sub New()
        Directory.CreateDirectory(_tempDir)
        _polFile = Path.Combine(_tempDir, "polar.pol")
        _cpFile = Path.Combine(_tempDir, "cp.txt")
        _blFile = Path.Combine(_tempDir, "bl.txt")
        InitializeUi()
    End Sub

#Region "UI layout"

    Private Sub InitializeUi()
        Me.Text = "XFOIL Analysis"
        Me.Width = 1040
        Me.Height = 760
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Font = New Font("Segoe UI", 9.0F)

        Dim outer As New TableLayoutPanel()
        outer.Dock = DockStyle.Fill
        outer.ColumnCount = 1
        outer.RowCount = 3
        outer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        outer.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        outer.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        outer.RowStyles.Add(New RowStyle(SizeType.Absolute, 90.0F))
        Me.Controls.Add(outer)

        outer.Controls.Add(BuildParamsPanel(), 0, 0)

        tc = New TabControl()
        tc.Dock = DockStyle.Fill

        Dim tabPolar = New TabPage("Polar")
        Dim tabCp = New TabPage("Cp Distribution")
        Dim tabBl = New TabPage("Boundary Layer")
        tc.Controls.Add(tabPolar)
        tc.Controls.Add(tabCp)
        tc.Controls.Add(tabBl)
        outer.Controls.Add(tc, 0, 1)

        pPolar = BuildPlotTab(tabPolar, "Polar")
        pCp = BuildPlotTab(tabCp, "Cp")
        pBl = BuildPlotTab(tabBl, "BL")

        AddHandler pPolar.Resize, Sub(s, e) RenderPolarPlot()
        AddHandler pCp.Resize, Sub(s, e) RenderCpPlot()
        AddHandler pBl.Resize, Sub(s, e) RenderBlPlot()

        ' Render once up front (placeholder text) rather than waiting for the first Resize/run,
        ' so all three tabs show a properly filled box immediately instead of blank until then.
        RenderPolarPlot()
        RenderCpPlot()
        RenderBlPlot()

        txtLog = New TextBox()
        txtLog.Dock = DockStyle.Fill
        txtLog.Multiline = True
        txtLog.ReadOnly = True
        txtLog.ScrollBars = ScrollBars.Vertical
        txtLog.Font = New Font("Consolas", 8.5F)
        outer.Controls.Add(txtLog, 0, 2)
    End Sub

    Private Function BuildParamsPanel() As Panel
        Dim panel As New Panel()
        panel.Dock = DockStyle.Top
        panel.AutoSize = True
        panel.BackColor = Color.WhiteSmoke
        panel.Padding = New Padding(8, 6, 8, 6)

        Dim rows As New TableLayoutPanel()
        rows.AutoSize = True
        rows.AutoSizeMode = AutoSizeMode.GrowAndShrink
        rows.ColumnCount = 1
        rows.RowCount = 3
        panel.Controls.Add(rows)

        ' Row 1: airfoil source
        Dim row1 As New FlowLayoutPanel()
        row1.AutoSize = True
        row1.WrapContents = False
        row1.Controls.Add(NewLabel("Airfoil (NACA e.g. 0012, or .dat path):"))
        txtAirfoil = New TextBox() With {.Width = 320, .Text = "0012", .Margin = New Padding(4, 3, 4, 3)}
        row1.Controls.Add(txtAirfoil)
        btnBrowseAirfoil = NewButton("Browse...", 80)
        AddHandler btnBrowseAirfoil.Click, AddressOf btnBrowseAirfoil_Click
        row1.Controls.Add(btnBrowseAirfoil)
        rows.Controls.Add(row1, 0, 0)

        ' Row 2: flow conditions
        Dim row2 As New FlowLayoutPanel()
        row2.AutoSize = True
        row2.WrapContents = False
        row2.Controls.Add(NewLabel("Re:"))
        txtRe = New TextBox() With {.Width = 70, .Text = "1e6", .Margin = New Padding(4, 3, 12, 3)}
        row2.Controls.Add(txtRe)
        row2.Controls.Add(NewLabel("Mach:"))
        txtMach = New TextBox() With {.Width = 50, .Text = "0", .Margin = New Padding(4, 3, 12, 3)}
        row2.Controls.Add(txtMach)
        row2.Controls.Add(NewLabel("Ncrit:"))
        txtNcrit = New TextBox() With {.Width = 40, .Text = "9", .Margin = New Padding(4, 3, 12, 3)}
        row2.Controls.Add(txtNcrit)
        row2.Controls.Add(NewLabel("(leave Re = 0 for an inviscid run)"))
        rows.Controls.Add(row2, 0, 1)

        ' Row 3: alpha sweep + single-point + run buttons
        Dim row3 As New FlowLayoutPanel()
        row3.AutoSize = True
        row3.WrapContents = False
        row3.Controls.Add(NewLabel("Alpha min:"))
        txtAlphaMin = New TextBox() With {.Width = 40, .Text = "-4", .Margin = New Padding(4, 3, 8, 3)}
        row3.Controls.Add(txtAlphaMin)
        row3.Controls.Add(NewLabel("max:"))
        txtAlphaMax = New TextBox() With {.Width = 40, .Text = "12", .Margin = New Padding(4, 3, 8, 3)}
        row3.Controls.Add(txtAlphaMax)
        row3.Controls.Add(NewLabel("step:"))
        txtAlphaStep = New TextBox() With {.Width = 35, .Text = "1", .Margin = New Padding(4, 3, 12, 3)}
        row3.Controls.Add(txtAlphaStep)
        btnRunPolar = NewButton("Run Polar Sweep", 130)
        AddHandler btnRunPolar.Click, AddressOf btnRunPolar_Click
        row3.Controls.Add(btnRunPolar)

        row3.Controls.Add(NewLabel("      Single alpha (Cp/BL):"))
        txtSingleAlpha = New TextBox() With {.Width = 40, .Text = "5", .Margin = New Padding(4, 3, 12, 3)}
        row3.Controls.Add(txtSingleAlpha)
        btnRunPoint = NewButton("Run Point Analysis", 140)
        AddHandler btnRunPoint.Click, AddressOf btnRunPoint_Click
        row3.Controls.Add(btnRunPoint)
        rows.Controls.Add(row3, 0, 2)

        lblStatus = New Label() With {.AutoSize = True, .Text = "Status: idle", .Margin = New Padding(4, 6, 4, 0)}
        rows.Controls.Add(lblStatus, 0, 3)
        rows.RowCount = 4

        Return panel
    End Function

    Private Function NewLabel(text As String) As Label
        Return New Label() With {.Text = text, .AutoSize = True, .Anchor = AnchorStyles.Left, .Margin = New Padding(0, 6, 4, 3)}
    End Function

    Private Function NewButton(text As String, w As Integer) As Button
        Dim b As New Button()
        b.Text = text
        b.Size = New Size(w, 25)
        b.Margin = New Padding(4, 2, 4, 3)
        b.FlatStyle = FlatStyle.Flat
        b.BackColor = Color.White
        b.Cursor = Cursors.Hand
        Return b
    End Function

    ''' <summary>
    ''' Builds a tab's content: a thin top strip (hosts the export button) plus a
    ''' fill PictureBox, mirroring frmGeometry's AddExportButtonToPanel pattern so
    ''' each plot gets the same PNG/SVG/PDF export UX as the AVL polar tab.
    ''' </summary>
    Private Function BuildPlotTab(tab As TabPage, viewName As String) As PictureBox
        Dim outer As New TableLayoutPanel()
        outer.Dock = DockStyle.Fill
        outer.ColumnCount = 1
        outer.RowCount = 2
        outer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        outer.RowStyles.Add(New RowStyle(SizeType.Absolute, 36.0F))
        outer.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        tab.Controls.Add(outer)

        Dim controlPanel As New Panel()
        controlPanel.Dock = DockStyle.Fill
        controlPanel.BackColor = Color.WhiteSmoke
        outer.Controls.Add(controlPanel, 0, 0)

        Dim pb As New PictureBox()
        pb.Dock = DockStyle.Fill
        pb.BackColor = Color.White
        ' StretchImage (not Normal) so the plot always visually fills the box even if a
        ' render lands a beat before/after a layout pass changes the control's size (tab
        ' switches, DPI, window resize) - the Resize handler below still re-renders at the
        ' new exact size right after, so this is just a safety net, not the main scaler.
        pb.SizeMode = PictureBoxSizeMode.StretchImage
        outer.Controls.Add(pb, 0, 1)

        AddExportButtonToPanel(controlPanel, pb, viewName)
        Return pb
    End Function

    Private Sub AddExportButtonToPanel(panel As Panel, pb As PictureBox, viewName As String)
        Dim btnExport As New Button()
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
        menu.Items.Add(New ToolStripMenuItem("Export as PNG...", Nothing, Sub(s, ev) ExportView(pb, "PNG", viewName)))
        menu.Items.Add(New ToolStripMenuItem("Export as SVG...", Nothing, Sub(s, ev) ExportView(pb, "SVG", viewName)))
        menu.Items.Add(New ToolStripMenuItem("Export as PDF...", Nothing, Sub(s, ev) ExportView(pb, "PDF", viewName)))

        AddHandler btnExport.Click, Sub(s, ev) menu.Show(btnExport, New Point(0, btnExport.Height))

        Dim reposition = Sub() btnExport.Left = Math.Max(0, panel.ClientSize.Width - btnExport.Width - 10)
        AddHandler panel.Resize, Sub(s, ev) reposition()

        panel.Controls.Add(btnExport)
        btnExport.BringToFront()
        reposition()
    End Sub

    Private Sub ExportView(pb As PictureBox, format As String, viewName As String)
        Dim svgContent As String = ""
        Dim pdfContent As String = ""

        If pb Is pPolar Then
            If format <> "PNG" Then RenderPolarPlot(True)
            svgContent = _polarSvg : pdfContent = _polarPdf
        ElseIf pb Is pCp Then
            If format <> "PNG" Then RenderCpPlot(True)
            svgContent = _cpSvg : pdfContent = _cpPdf
        ElseIf pb Is pBl Then
            If format <> "PNG" Then RenderBlPlot(True)
            svgContent = _blSvg : pdfContent = _blPdf
        End If

        If format = "PNG" Then
            If pb.Image Is Nothing Then
                AppMessageBox.Show("There is no image to export.", "Export View", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
        Else
            If String.IsNullOrEmpty(svgContent) Then
                AppMessageBox.Show("The view has not finished rendering. Please wait a moment and try again.", "Export View", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
        End If

        Dim sfd As New SaveFileDialog()
        sfd.Title = $"Export {viewName} as {format}"
        Dim baseName = "xfoil_" & viewName.ToLower().Replace(" ", "_")
        sfd.FileName = baseName

        Select Case format
            Case "PNG" : sfd.Filter = "PNG Image (*.png)|*.png" : sfd.DefaultExt = "png"
            Case "SVG" : sfd.Filter = "SVG Image (*.svg)|*.svg" : sfd.DefaultExt = "svg"
            Case "PDF" : sfd.Filter = "PDF Document (*.pdf)|*.pdf" : sfd.DefaultExt = "pdf"
        End Select

        If sfd.ShowDialog() = DialogResult.OK Then
            Try
                Select Case format
                    Case "PNG"
                        Using imgCopy As Image = CType(pb.Image.Clone(), Image)
                            imgCopy.Save(sfd.FileName, ImageFormat.Png)
                        End Using
                    Case "SVG"
                        File.WriteAllText(sfd.FileName, svgContent, Encoding.UTF8)
                    Case "PDF"
                        frmGeometry.WriteVectorPdf(pdfContent, pb.Width, pb.Height, sfd.FileName)
                End Select
                AppToast.Show($"{format} exported to " & Path.GetFileName(sfd.FileName))
            Catch ex As Exception
                AppMessageBox.Show("Error exporting file: " & ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End If
    End Sub

    Private Sub btnBrowseAirfoil_Click(sender As Object, e As EventArgs)
        Dim ofd As New OpenFileDialog()
        ofd.Title = "Select airfoil coordinate file"
        ofd.Filter = "Airfoil files (*.dat)|*.dat|All files (*.*)|*.*"
        If ofd.ShowDialog() = DialogResult.OK Then
            txtAirfoil.Text = ofd.FileName
        End If
    End Sub

#End Region

#Region "Run polar sweep"

    Private Async Sub btnRunPolar_Click(sender As Object, e As EventArgs)
        Dim re As Double, mach As Double, ncrit As Double, aMin As Double, aMax As Double, aStep As Double
        If Not Double.TryParse(txtRe.Text, NumberStyles.Float, CultureInfo.InvariantCulture, re) Then re = 0
        If Not Double.TryParse(txtMach.Text, NumberStyles.Float, CultureInfo.InvariantCulture, mach) Then mach = 0
        If Not Double.TryParse(txtNcrit.Text, NumberStyles.Float, CultureInfo.InvariantCulture, ncrit) Then ncrit = 9
        If Not Double.TryParse(txtAlphaMin.Text, NumberStyles.Float, CultureInfo.InvariantCulture, aMin) OrElse
           Not Double.TryParse(txtAlphaMax.Text, NumberStyles.Float, CultureInfo.InvariantCulture, aMax) OrElse
           Not Double.TryParse(txtAlphaStep.Text, NumberStyles.Float, CultureInfo.InvariantCulture, aStep) OrElse aStep = 0 Then
            AppMessageBox.Show("Enter valid numeric alpha min/max/step values.", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If String.IsNullOrWhiteSpace(txtAirfoil.Text) Then
            AppMessageBox.Show("Enter a NACA code (e.g. 0012) or browse to an airfoil .dat file.", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        btnRunPolar.Enabled = False
        btnRunPoint.Enabled = False
        Try
            If Not Await EnsureXfoilReadyAsync() Then Return

            Try
                If File.Exists(_polFile) Then File.Delete(_polFile)
            Catch
            End Try

            lblStatus.Text = "Status: running polar sweep..."

            LoadAirfoilCommands()
            WriteCmd("oper")
            If re > 0 Then
                WriteCmd("visc " & re.ToString("F0", CultureInfo.InvariantCulture))
                WriteCmd("iter 200")
                WriteCmd("vpar")
                WriteCmd("n " & ncrit.ToString("0.###", CultureInfo.InvariantCulture))
                WriteCmd("")
            End If
            WriteCmd("mach " & mach.ToString("0.###", CultureInfo.InvariantCulture))
            WriteCmd("pacc")
            WriteCmd(_polFile)
            WriteCmd("")
            WriteCmd($"aseq {aMin.ToString("0.###", CultureInfo.InvariantCulture)} {aMax.ToString("0.###", CultureInfo.InvariantCulture)} {aStep.ToString("0.###", CultureInfo.InvariantCulture)}")
            WriteCmd("pacc")
            WriteCmd("")
            FlushCmd()

            Dim numPoints = Math.Max(1, CInt(Math.Abs((aMax - aMin) / aStep)) + 1)
            Dim timeoutMs = Math.Max(6000, numPoints * 1200)
            Dim ok = Await WaitForFileStableAsync(_polFile, timeoutMs)
            If Not ok Then
                lblStatus.Text = "Status: polar sweep did not produce output (check log below)"
                AppMessageBox.Show("XFOIL did not produce a polar file in time. Check the log for errors (e.g. an airfoil that failed to load, or a non-converging viscous run).", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            _polarPoints = ParsePolarFile(_polFile)
            RenderPolarPlot()
            lblStatus.Text = $"Status: polar sweep complete - {_polarPoints.Count} point(s)"
            tc.SelectedIndex = 0
        Finally
            btnRunPolar.Enabled = True
            btnRunPoint.Enabled = True
        End Try
    End Sub

    Private Sub LoadAirfoilCommands()
        Dim text = txtAirfoil.Text.Trim()
        If Regex.IsMatch(text, "^\d{4,5}$") Then
            WriteCmd($"naca {text}")
        Else
            WriteCmd($"load {text}")
        End If
    End Sub

#End Region

#Region "Run point analysis (Cp + boundary layer)"

    Private Async Sub btnRunPoint_Click(sender As Object, e As EventArgs)
        Dim re As Double, mach As Double, alpha As Double
        If Not Double.TryParse(txtRe.Text, NumberStyles.Float, CultureInfo.InvariantCulture, re) Then re = 0
        If Not Double.TryParse(txtMach.Text, NumberStyles.Float, CultureInfo.InvariantCulture, mach) Then mach = 0
        If Not Double.TryParse(txtSingleAlpha.Text, NumberStyles.Float, CultureInfo.InvariantCulture, alpha) Then
            AppMessageBox.Show("Enter a valid numeric single alpha value.", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If String.IsNullOrWhiteSpace(txtAirfoil.Text) Then
            AppMessageBox.Show("Enter a NACA code (e.g. 0012) or browse to an airfoil .dat file.", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        btnRunPolar.Enabled = False
        btnRunPoint.Enabled = False
        Try
            If Not Await EnsureXfoilReadyAsync() Then Return

            Try
                If File.Exists(_cpFile) Then File.Delete(_cpFile)
                If File.Exists(_blFile) Then File.Delete(_blFile)
            Catch
            End Try

            lblStatus.Text = "Status: running point analysis..."

            LoadAirfoilCommands()
            WriteCmd("oper")
            If re > 0 Then
                WriteCmd("visc " & re.ToString("F0", CultureInfo.InvariantCulture))
                WriteCmd("iter 200")
            End If
            WriteCmd("mach " & mach.ToString("0.###", CultureInfo.InvariantCulture))
            WriteCmd($"alfa {alpha.ToString("0.###", CultureInfo.InvariantCulture)}")
            WriteCmd($"cpwr {_cpFile}")
            If re > 0 Then WriteCmd($"dump {_blFile}")
            WriteCmd("")
            FlushCmd()

            Dim ok = Await WaitForFileStableAsync(_cpFile, 8000)
            If Not ok Then
                lblStatus.Text = "Status: point analysis did not produce Cp output (check log below)"
                AppMessageBox.Show("XFOIL did not produce a Cp output file in time. Check the log for errors.", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            _cpPoints = ParseCpFile(_cpFile)
            RenderCpPlot()

            If re > 0 Then
                Dim okBl = Await WaitForFileStableAsync(_blFile, 8000)
                If okBl Then
                    ParseBlFile(_blFile, _blTop, _blBottom)
                Else
                    _blTop.Clear() : _blBottom.Clear()
                End If
            Else
                _blTop.Clear() : _blBottom.Clear()
            End If
            RenderBlPlot()

            lblStatus.Text = $"Status: point analysis complete at alpha = {alpha}"
            tc.SelectedIndex = 1
        Finally
            btnRunPolar.Enabled = True
            btnRunPoint.Enabled = True
        End Try
    End Sub

#End Region

#Region "Parsing"

    ''' <summary>
    ''' XFOIL's PACC polar file: a fixed header block ending in a line naming the
    ''' columns ("alpha  CL  CD  CDp  CM  Top_Xtr  Bot_Xtr") followed by a dashed
    ''' separator, then one fixed-width numeric row per alpha.
    ''' </summary>
    Private Function ParsePolarFile(path As String) As List(Of XfoilPolarPoint)
        Dim result As New List(Of XfoilPolarPoint)
        Dim lines = File.ReadAllLines(path)
        Dim headerIdx = -1
        For i = 0 To lines.Length - 1
            If lines(i).IndexOf("alpha", StringComparison.OrdinalIgnoreCase) >= 0 AndAlso
               lines(i).IndexOf("CL", StringComparison.OrdinalIgnoreCase) >= 0 Then
                headerIdx = i
                Exit For
            End If
        Next
        If headerIdx < 0 Then Return result

        For i = headerIdx + 2 To lines.Length - 1
            Dim line = lines(i)
            If String.IsNullOrWhiteSpace(line) Then Continue For
            Dim toks = line.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
            If toks.Length < 7 Then Continue For

            Dim vals(6) As Double
            Dim allOk = True
            For c = 0 To 6
                If Not Double.TryParse(toks(c), NumberStyles.Float, CultureInfo.InvariantCulture, vals(c)) Then
                    allOk = False
                    Exit For
                End If
            Next
            If Not allOk Then Continue For

            result.Add(New XfoilPolarPoint With {
                .Alpha = vals(0), .CL = vals(1), .CD = vals(2),
                .CDp = vals(3), .CM = vals(4), .TopXtr = vals(5), .BotXtr = vals(6)
            })
        Next
        Return result
    End Function

    ''' <summary>
    ''' XFOIL's CPWR output has a couple of header/comment lines followed by numeric
    ''' rows of either "x  Cp" or "x  y  Cp" depending on XFOIL version/flags. Rather
    ''' than assume one exact layout, take the first token as x and the last as Cp on
    ''' any line with 2+ numeric tokens, preserving file order (which traces the
    ''' airfoil surface as a loop: TE -> upper -> LE -> lower -> TE).
    ''' </summary>
    Private Function ParseCpFile(path As String) As List(Of XfoilCpPoint)
        Dim result As New List(Of XfoilCpPoint)
        For Each line In File.ReadAllLines(path)
            If String.IsNullOrWhiteSpace(line) Then Continue For
            Dim toks = line.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
            If toks.Length < 2 Then Continue For
            Dim x As Double, cp As Double
            If Double.TryParse(toks(0), NumberStyles.Float, CultureInfo.InvariantCulture, x) AndAlso
               Double.TryParse(toks(toks.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, cp) Then
                result.Add(New XfoilCpPoint With {.X = x, .Cp = cp})
            End If
        Next
        Return result
    End Function

    ''' <summary>
    ''' XFOIL's boundary-layer "dump" file lists s, x, y, Ue/Vinf, Dstar, Theta, Cf, H
    ''' (column order/count varies slightly by version). Rows are split into upper vs
    ''' lower surface by the sign of y - a safe heuristic since airfoil coordinates are
    ''' referenced to the chord line (y >= 0 is the upper surface) - rather than relying
    ''' on the exact blank-line block boundaries XFOIL uses between the two sides.
    ''' </summary>
    Private Sub ParseBlFile(path As String, top As List(Of XfoilBLPoint), bottom As List(Of XfoilBLPoint))
        top.Clear()
        bottom.Clear()
        For Each line In File.ReadAllLines(path)
            If String.IsNullOrWhiteSpace(line) Then Continue For
            Dim toks = line.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
            If toks.Length < 8 Then Continue For

            Dim vals(7) As Double
            Dim allOk = True
            For c = 0 To 7
                If Not Double.TryParse(toks(c), NumberStyles.Float, CultureInfo.InvariantCulture, vals(c)) Then
                    allOk = False
                    Exit For
                End If
            Next
            If Not allOk Then Continue For

            Dim pt As New XfoilBLPoint With {.X = vals(1), .Cf = vals(6), .H = vals(7), .Theta = vals(5)}
            If vals(2) >= 0 Then
                top.Add(pt)
            Else
                bottom.Add(pt)
            End If
        Next
    End Sub

#End Region

#Region "Rendering"

    Private Function MakeTickFont() As Font
        Return New Font("Segoe UI", 8.0F)
    End Function

    Private Function MakeAxisFont() As Font
        Return New Font("Segoe UI", 9.0F, FontStyle.Bold)
    End Function

    Private Sub RenderPolarPlot(Optional captureVectors As Boolean = False)
        If pPolar Is Nothing OrElse pPolar.Width <= 0 OrElse pPolar.Height <= 0 Then Return
        Dim w = pPolar.Width : Dim h = pPolar.Height
        Dim bmp As New Bitmap(w, h)
        Dim tickFont = MakeTickFont() : Dim axisFont = MakeAxisFont()
        Dim svgOut As String = "" : Dim pdfOut As String = ""

        Using g As New SvgGraphics(w, h, Graphics.FromImage(bmp), captureVectors)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.Clear(Color.White)

            If _polarPoints.Count = 0 Then
                g.DrawString("Set airfoil + alpha range and click ""Run Polar Sweep"".", tickFont, Brushes.Gray, New PointF(10, 10))
            Else
                Dim ordered As New List(Of XfoilPolarPoint)(_polarPoints)
                ordered.Sort(Function(a, b) a.Alpha.CompareTo(b.Alpha))

                Dim marginX As Single = 55
                Dim marginY As Single = 35
                Dim cellW As Single = (w - marginX * 3) / 2
                Dim cellH As Single = (h - marginY * 3) / 2

                DrawSubplot(g, tickFont, axisFont, marginX, marginY, cellW, cellH,
                            "CL vs alpha", Color.OrangeRed, ordered, Function(pt) pt.Alpha, Function(pt) pt.CL)
                DrawSubplot(g, tickFont, axisFont, marginX * 2 + cellW, marginY, cellW, cellH,
                            "CD vs alpha", Color.DeepSkyBlue, ordered, Function(pt) pt.Alpha, Function(pt) pt.CD)
                DrawSubplot(g, tickFont, axisFont, marginX, marginY * 2 + cellH, cellW, cellH,
                            "Cm vs alpha", Color.LimeGreen, ordered, Function(pt) pt.Alpha, Function(pt) pt.CM)
                DrawSubplot(g, tickFont, axisFont, marginX * 2 + cellW, marginY * 2 + cellH, cellW, cellH,
                            "CL vs CD (drag polar)", Color.Goldenrod, ordered, Function(pt) pt.CD, Function(pt) pt.CL)
            End If

            If captureVectors Then
                svgOut = g.GetSvgContent()
                pdfOut = g.GetPdfContentStream()
            End If
        End Using

        Dim old = pPolar.Image
        pPolar.Image = bmp
        old?.Dispose()
        If captureVectors Then _polarSvg = svgOut : _polarPdf = pdfOut
    End Sub

    Private Sub DrawSubplot(Of T)(g As SvgGraphics, tickFont As Font, axisFont As Font,
                                   x As Single, y As Single, w As Single, h As Single,
                                   title As String, curveColor As Color,
                                   points As List(Of T),
                                   xOf As Func(Of T, Double), yOf As Func(Of T, Double))
        If points.Count = 0 Then Return

        Dim xMin As Double = Double.MaxValue, xMax As Double = Double.MinValue
        Dim yMin As Double = Double.MaxValue, yMax As Double = Double.MinValue
        For Each pt In points
            Dim xv = xOf(pt) : Dim yv = yOf(pt)
            xMin = Math.Min(xMin, xv) : xMax = Math.Max(xMax, xv)
            yMin = Math.Min(yMin, yv) : yMax = Math.Max(yMax, yv)
        Next
        If xMin = xMax Then xMin -= 1 : xMax += 1
        If yMin = yMax Then yMin -= 1 : yMax += 1
        Dim yPad = (yMax - yMin) * 0.1
        yMin -= yPad
        yMax += yPad

        Dim axisPen As New Pen(Color.Black, 1)
        Dim gridPen As New Pen(Color.LightGray, 1) With {.DashStyle = DashStyle.Dash}

        g.DrawRectangle(axisPen, x, y, w, h)
        g.DrawString(title, axisFont, New SolidBrush(Color.Black), New PointF(x, y - axisFont.Height - 2))

        If yMin < 0 AndAlso yMax > 0 Then
            Dim zeroY As Single = CSng(y + h - (0 - yMin) / (yMax - yMin) * h)
            g.DrawLine(gridPen, x, zeroY, x + w, zeroY)
        End If

        Dim pen As New Pen(curveColor, 1.5F)
        Dim markerBrush As New SolidBrush(curveColor)
        Dim pts As New List(Of PointF)
        For Each pt In points
            Dim px As Single = CSng(x + (xOf(pt) - xMin) / (xMax - xMin) * w)
            Dim py As Single = CSng(y + h - (yOf(pt) - yMin) / (yMax - yMin) * h)
            pts.Add(New PointF(px, py))
        Next
        If pts.Count >= 2 Then g.DrawLines(pen, pts.ToArray())
        For Each pt In pts
            g.FillEllipse(markerBrush, pt.X - 2, pt.Y - 2, 4, 4)
        Next

        g.DrawString(xMin.ToString("0.00"), tickFont, New SolidBrush(Color.Black), New PointF(x, y + h + 2))
        Dim maxLabel = xMax.ToString("0.00")
        Dim maxLabelSize = g.MeasureString(maxLabel, tickFont)
        g.DrawString(maxLabel, tickFont, New SolidBrush(Color.Black), New PointF(x + w - maxLabelSize.Width, y + h + 2))

        Dim yMaxLabel = yMax.ToString("0.0000")
        Dim yMaxLabelSize = g.MeasureString(yMaxLabel, tickFont)
        g.DrawString(yMaxLabel, tickFont, New SolidBrush(Color.Black), New PointF(x - yMaxLabelSize.Width - 2, y))
        Dim yMinLabel = yMin.ToString("0.0000")
        Dim yMinLabelSize = g.MeasureString(yMinLabel, tickFont)
        g.DrawString(yMinLabel, tickFont, New SolidBrush(Color.Black), New PointF(x - yMinLabelSize.Width - 2, y + h - yMinLabelSize.Height))
    End Sub

    ''' <summary>
    ''' Cp plotted with the y-axis inverted (more negative/suction Cp drawn upward),
    ''' the standard aerodynamics convention, connecting points in the file's native
    ''' panel-node order so the curve traces the airfoil surface as a loop rather
    ''' than a jagged sort-by-x line.
    ''' </summary>
    Private Sub RenderCpPlot(Optional captureVectors As Boolean = False)
        If pCp Is Nothing OrElse pCp.Width <= 0 OrElse pCp.Height <= 0 Then Return
        Dim w = pCp.Width : Dim h = pCp.Height
        Dim bmp As New Bitmap(w, h)
        Dim tickFont = MakeTickFont() : Dim axisFont = MakeAxisFont()
        Dim svgOut As String = "" : Dim pdfOut As String = ""

        Using g As New SvgGraphics(w, h, Graphics.FromImage(bmp), captureVectors)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.Clear(Color.White)

            If _cpPoints.Count = 0 Then
                g.DrawString("Set a single alpha and click ""Run Point Analysis"" to compute the Cp distribution.", tickFont, Brushes.Gray, New PointF(10, 10))
            Else
                Dim marginX As Single = 55
                Dim marginY As Single = 35
                Dim plotW As Single = w - marginX * 2
                Dim plotH As Single = h - marginY * 2

                Dim xMin As Double = Double.MaxValue, xMax As Double = Double.MinValue
                Dim cpMin As Double = Double.MaxValue, cpMax As Double = Double.MinValue
                For Each pt In _cpPoints
                    xMin = Math.Min(xMin, pt.X) : xMax = Math.Max(xMax, pt.X)
                    cpMin = Math.Min(cpMin, pt.Cp) : cpMax = Math.Max(cpMax, pt.Cp)
                Next
                If xMin = xMax Then xMin -= 1 : xMax += 1
                If cpMin = cpMax Then cpMin -= 1 : cpMax += 1
                Dim pad = (cpMax - cpMin) * 0.1
                cpMin -= pad : cpMax += pad

                Dim axisPen As New Pen(Color.Black, 1)
                g.DrawRectangle(axisPen, marginX, marginY, plotW, plotH)
                g.DrawString("Cp vs x/c (axis inverted - suction peaks point up)", axisFont, New SolidBrush(Color.Black), New PointF(marginX, marginY - axisFont.Height - 2))

                ' Inverted: cpMax (most positive) maps to the BOTTOM of the plot rect.
                Dim pen As New Pen(Color.OrangeRed, 1.5F)
                Dim pts As New List(Of PointF)
                For Each pt In _cpPoints
                    Dim px As Single = CSng(marginX + (pt.X - xMin) / (xMax - xMin) * plotW)
                    Dim py As Single = CSng(marginY + (pt.Cp - cpMin) / (cpMax - cpMin) * plotH)
                    pts.Add(New PointF(px, py))
                Next
                If pts.Count >= 2 Then g.DrawLines(pen, pts.ToArray())

                g.DrawString(xMin.ToString("0.00"), tickFont, New SolidBrush(Color.Black), New PointF(marginX, marginY + plotH + 2))
                Dim maxLabel = xMax.ToString("0.00")
                Dim maxLabelSize = g.MeasureString(maxLabel, tickFont)
                g.DrawString(maxLabel, tickFont, New SolidBrush(Color.Black), New PointF(marginX + plotW - maxLabelSize.Width, marginY + plotH + 2))

                Dim cpTopLabel = cpMax.ToString("0.00")
                g.DrawString(cpTopLabel, tickFont, New SolidBrush(Color.Black), New PointF(marginX - g.MeasureString(cpTopLabel, tickFont).Width - 2, marginY + plotH - tickFont.Height))
                Dim cpBotLabel = cpMin.ToString("0.00")
                g.DrawString(cpBotLabel, tickFont, New SolidBrush(Color.Black), New PointF(marginX - g.MeasureString(cpBotLabel, tickFont).Width - 2, marginY))
            End If

            If captureVectors Then
                svgOut = g.GetSvgContent()
                pdfOut = g.GetPdfContentStream()
            End If
        End Using

        Dim old = pCp.Image
        pCp.Image = bmp
        old?.Dispose()
        If captureVectors Then _cpSvg = svgOut : _cpPdf = pdfOut
    End Sub

    Private Sub RenderBlPlot(Optional captureVectors As Boolean = False)
        If pBl Is Nothing OrElse pBl.Width <= 0 OrElse pBl.Height <= 0 Then Return
        Dim w = pBl.Width : Dim h = pBl.Height
        Dim bmp As New Bitmap(w, h)
        Dim tickFont = MakeTickFont() : Dim axisFont = MakeAxisFont()
        Dim svgOut As String = "" : Dim pdfOut As String = ""

        Using g As New SvgGraphics(w, h, Graphics.FromImage(bmp), captureVectors)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.Clear(Color.White)

            If _blTop.Count = 0 AndAlso _blBottom.Count = 0 Then
                g.DrawString("Run a viscous (Re > 0) point analysis to compute boundary-layer data.", tickFont, Brushes.Gray, New PointF(10, 10))
            Else
                Dim marginX As Single = 55
                Dim marginY As Single = 35
                Dim cellW As Single = w - marginX * 2
                Dim cellH As Single = (h - marginY * 3) / 2

                DrawBlPane(g, tickFont, axisFont, marginX, marginY, cellW, cellH,
                           "Skin friction Cf vs x/c", Function(pt As XfoilBLPoint) pt.Cf)
                DrawBlPane(g, tickFont, axisFont, marginX, marginY * 2 + cellH, cellW, cellH,
                           "Shape factor H vs x/c", Function(pt As XfoilBLPoint) pt.H)
            End If

            If captureVectors Then
                svgOut = g.GetSvgContent()
                pdfOut = g.GetPdfContentStream()
            End If
        End Using

        Dim old = pBl.Image
        pBl.Image = bmp
        old?.Dispose()
        If captureVectors Then _blSvg = svgOut : _blPdf = pdfOut
    End Sub

    Private Sub DrawBlPane(g As SvgGraphics, tickFont As Font, axisFont As Font,
                            x As Single, y As Single, w As Single, h As Single,
                            title As String, yOf As Func(Of XfoilBLPoint, Double))
        Dim allPts As New List(Of XfoilBLPoint)
        allPts.AddRange(_blTop)
        allPts.AddRange(_blBottom)
        If allPts.Count = 0 Then Return

        Dim xMin As Double = Double.MaxValue, xMax As Double = Double.MinValue
        Dim yMin As Double = Double.MaxValue, yMax As Double = Double.MinValue
        For Each pt In allPts
            xMin = Math.Min(xMin, pt.X) : xMax = Math.Max(xMax, pt.X)
            Dim yv = yOf(pt)
            yMin = Math.Min(yMin, yv) : yMax = Math.Max(yMax, yv)
        Next
        If xMin = xMax Then xMin -= 1 : xMax += 1
        If yMin = yMax Then yMin -= 1 : yMax += 1
        Dim pad = (yMax - yMin) * 0.1
        yMin -= pad : yMax += pad

        Dim axisPen As New Pen(Color.Black, 1)
        g.DrawRectangle(axisPen, x, y, w, h)
        g.DrawString(title, axisFont, New SolidBrush(Color.Black), New PointF(x, y - axisFont.Height - 2))
        g.DrawString("upper", MakeTickFont(), New SolidBrush(Color.OrangeRed), New PointF(x + w - 90, y + 2))
        g.DrawString("lower", MakeTickFont(), New SolidBrush(Color.DeepSkyBlue), New PointF(x + w - 45, y + 2))

        DrawBlSeries(g, x, y, w, h, xMin, xMax, yMin, yMax, _blTop, yOf, Color.OrangeRed)
        DrawBlSeries(g, x, y, w, h, xMin, xMax, yMin, yMax, _blBottom, yOf, Color.DeepSkyBlue)

        g.DrawString(xMin.ToString("0.00"), tickFont, New SolidBrush(Color.Black), New PointF(x, y + h + 2))
        Dim maxLabel = xMax.ToString("0.00")
        Dim maxLabelSize = g.MeasureString(maxLabel, tickFont)
        g.DrawString(maxLabel, tickFont, New SolidBrush(Color.Black), New PointF(x + w - maxLabelSize.Width, y + h + 2))
        Dim yMaxLabel = yMax.ToString("0.000")
        g.DrawString(yMaxLabel, tickFont, New SolidBrush(Color.Black), New PointF(x - g.MeasureString(yMaxLabel, tickFont).Width - 2, y))
        Dim yMinLabel = yMin.ToString("0.000")
        g.DrawString(yMinLabel, tickFont, New SolidBrush(Color.Black), New PointF(x - g.MeasureString(yMinLabel, tickFont).Width - 2, y + h - tickFont.Height))
    End Sub

    Private Sub DrawBlSeries(g As SvgGraphics, x As Single, y As Single, w As Single, h As Single,
                              xMin As Double, xMax As Double, yMin As Double, yMax As Double,
                              points As List(Of XfoilBLPoint), yOf As Func(Of XfoilBLPoint, Double), color As Color)
        If points.Count < 2 Then Return
        Dim ordered As New List(Of XfoilBLPoint)(points)
        ordered.Sort(Function(a, b) a.X.CompareTo(b.X))

        Dim pen As New Pen(color, 1.5F)
        Dim pts As New List(Of PointF)
        For Each pt In ordered
            Dim px As Single = CSng(x + (pt.X - xMin) / (xMax - xMin) * w)
            Dim py As Single = CSng(y + h - (yOf(pt) - yMin) / (yMax - yMin) * h)
            pts.Add(New PointF(px, py))
        Next
        g.DrawLines(pen, pts.ToArray())
    End Sub

#End Region

    Private Sub frmXfoilAnalysis_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        Leaving = True
        _logFlushTimer.Stop()
        Try
            If p IsNot Nothing AndAlso Not p.HasExited Then p.Kill()
            p?.Dispose()
        Catch
        End Try
    End Sub

End Class

Public Class XfoilPolarPoint
    Public Alpha, CL, CD, CDp, CM, TopXtr, BotXtr As Double
End Class

Public Class XfoilCpPoint
    Public X, Cp As Double
End Class

Public Class XfoilBLPoint
    Public X, Cf, H, Theta As Double
End Class

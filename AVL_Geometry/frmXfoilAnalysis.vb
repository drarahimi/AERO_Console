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

    ' Debounce timers for the plot Resize handlers below: a TableLayoutPanel/TabControl
    ' layout pass can fire a control's Resize event more than once with different
    ' intermediate sizes before settling (a known WinForms quirk), and re-rendering
    ' synchronously on every one of those intermediate events was causing the plot to
    ' visibly thrash between a stale small size and the correct final size. Collapsing
    ' a burst of Resize events into a single render of whatever size is current when
    ' the burst goes quiet avoids that.
    Private WithEvents _polarResizeTimer As New System.Windows.Forms.Timer With {.Interval = 120}
    Private WithEvents _cpResizeTimer As New System.Windows.Forms.Timer With {.Interval = 120}
    Private WithEvents _blResizeTimer As New System.Windows.Forms.Timer With {.Interval = 120}
    Private WithEvents _geomResizeTimer As New System.Windows.Forms.Timer With {.Interval = 120}

    Private ReadOnly _tempDir As String = Path.Combine(Path.GetTempPath(), "aeroconsole_xfoil")
    ' Regenerated fresh (unique filename) at the start of every run rather than reused -
    ' if a just-exited prior run's process hadn't fully released its file handle yet,
    ' XFOIL would hit its own "Output file exists. Overwrite? Y" prompt on the SAME
    ' filename and sit there waiting for a keystroke we never send, silently hanging
    ' with no error. A fresh name every time makes that class of hang impossible.
    Private _polFile As String
    Private _cpFile As String
    Private _blFile As String
    Private _geomFile As String

    Private Function NewTempFile(baseName As String) As String
        Return Path.Combine(_tempDir, baseName & "_" & Guid.NewGuid().ToString("N").Substring(0, 8) & ".txt")
    End Function

    ''' <summary>
    ''' Reads a specific process's stdout, passed in explicitly rather than read back off
    ''' the shared "p" field. "p" gets reassigned to a brand-new Process on every run (see
    ''' StartProcess - a fresh process is started per analysis run), and a background
    ''' thread whose loop condition instead re-reads that shared field would silently
    ''' retarget itself onto whatever process "p" currently points to the next time its
    ''' loop condition is evaluated - i.e. an old run's leftover thread could end up
    ''' racing the new run's thread to read the NEW process's stdout, each stealing chunks
    ''' of the other's output. Capturing the exact Process instance as a parameter at
    ''' thread-start time makes each thread bound to the run it was created for.
    ''' </summary>
    Private Sub ReadThread(proc As Process)
        SyncLock _logBufferLock
            _logBuffer.Append($"*** Reader thread started for pid {proc.Id} (blocking on first read...)" & Environment.NewLine)
        End SyncLock
        Try
            Dim buffer(4096) As Char
            Do Until Leaving OrElse proc.HasExited
                Dim bytesRead As Integer = proc.StandardOutput.Read(buffer, 0, buffer.Length)
                SyncLock _logBufferLock
                    _logBuffer.Append($"*** [read returned {bytesRead}]" & Environment.NewLine)
                End SyncLock
                If bytesRead <= 0 Then Exit Do
                Dim textChunk As String = New String(buffer, 0, bytesRead)
                SyncLock _logBufferLock
                    _logBuffer.Append(textChunk)
                End SyncLock
            Loop
        Catch ex As Exception
            ' Surface this instead of swallowing it silently - a read failure here (broken
            ' pipe, access denied, etc.) previously looked identical in the log to "XFOIL
            ' just isn't saying anything yet", making a real failure indistinguishable from
            ' XFOIL still being busy.
            SyncLock _logBufferLock
                _logBuffer.Append(Environment.NewLine & "*** stdout read failed: " & ex.Message & Environment.NewLine)
            End SyncLock
        End Try
    End Sub

    ''' <summary>Appends a line to the log box directly - only call this from the UI thread.</summary>
    Private Sub LogLine(text As String)
        If txtLog Is Nothing Then Return
        txtLog.AppendText(text & Environment.NewLine)
        txtLog.SelectionStart = txtLog.TextLength
        txtLog.ScrollToCaret()
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

    Private Sub _polarResizeTimer_Tick(sender As Object, e As EventArgs) Handles _polarResizeTimer.Tick
        _polarResizeTimer.Stop()
        RenderPolarPlot()
    End Sub

    Private Sub _cpResizeTimer_Tick(sender As Object, e As EventArgs) Handles _cpResizeTimer.Tick
        _cpResizeTimer.Stop()
        RenderCpPlot()
    End Sub

    Private Sub _blResizeTimer_Tick(sender As Object, e As EventArgs) Handles _blResizeTimer.Tick
        _blResizeTimer.Stop()
        RenderBlPlot()
    End Sub

    Private Sub _geomResizeTimer_Tick(sender As Object, e As EventArgs) Handles _geomResizeTimer.Tick
        _geomResizeTimer.Stop()
        RenderGeometryPlot()
    End Sub

    Private Sub StartProcess(appPath As String)
        If p IsNot Nothing Then
            Try
                If Not p.HasExited Then
                    p.Kill()
                    ' Wait for the OS to fully tear down the killed process (release its
                    ' stdio pipe handles etc.) before starting a new one - starting the
                    ' next process immediately after Kill() (which only requests
                    ' termination, asynchronously) was observed to make the *next* run's
                    ' xfoil.exe produce no output at all, even though the same sequence
                    ' works fine as the first run in a session.
                    p.WaitForExit(2000)
                End If
                p.Dispose()
            Catch
            End Try
            p = Nothing
        End If

        ' Defensive: StartProcess can in principle run before the constructor's
        ' Directory.CreateDirectory(_tempDir) has taken effect, or the folder could have
        ' been deleted externally - a WorkingDirectory that doesn't exist makes p.Start()
        ' throw, which used to be swallowed with no visible sign anything went wrong.
        Directory.CreateDirectory(_tempDir)

        p = New Process()
        Dim startinfo As New ProcessStartInfo()
        With startinfo
            .FileName = appPath
            .Arguments = ""
            ' XFOIL's own SAVE/PACC/CPWR/DUMP file-name prompts are read into a fixed-length
            ' Fortran CHARACTER buffer - confirmed against the real xfoil.exe that a long
            ' absolute path (e.g. under %TEMP%\aeroconsole_xfoil\...) silently fails to open
            ' the file at all (no error, just nothing written), while the same command with
            ' a short path works fine. Running with _tempDir as the working directory lets
            ' every *short, relative* filename we pass for those commands resolve correctly
            ' regardless of how deep the user's actual %TEMP% happens to be nested.
            .WorkingDirectory = _tempDir
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

        Try
            p.Start()
            p.StandardInput.AutoFlush = True
        Catch ex As Exception
            LogLine("*** Failed to start xfoil.exe: " & ex.Message)
            Throw
        End Try

        txtLog.Clear()
        LogLine($"*** Started xfoil.exe (pid {p.Id}), working dir: {_tempDir}")

        Dim startedProcess = p
        bt = New Thread(Sub() ReadThread(startedProcess))
        bt.IsBackground = True
        bt.Start()

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

        ' Always start a fresh XFOIL process for each analysis run so every run starts cleanly
        ' at the top-level XFOIL prompt rather than inheriting a leftover OPER/VPAR submenu state.
        Try
            StartProcess(appPath)
        Catch ex As Exception
            AppMessageBox.Show("Could not start xfoil.exe: " & ex.Message, "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
        If p Is Nothing OrElse p.HasExited Then
            AppMessageBox.Show("XFOIL process exited immediately! Exit code: " & p.ExitCode, "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End If
        Return True
    End Function

    ''' <summary>
    ''' Sends a command to XFOIL and echoes it into the log box (prefixed "&gt;&gt;&gt;",
    ''' or "(blank)" for an empty line - blank lines are meaningful XFOIL input, they pop
    ''' back up a menu level) so the log shows the full conversation, not just XFOIL's
    ''' side of it. Makes it possible to tell exactly which command a stalled run is stuck
    ''' after: the last ">>>" line with no XFOIL response beneath it.
    ''' </summary>
    Private Sub WriteCmd(text As String)
        LogLine(">>> " & If(text = "", "(blank)", text))
        Try
            p.StandardInput.WriteLine(text)
        Catch ex As Exception
            ' If the pipe is already broken (process died, handle closed, etc.) this would
            ' otherwise fail completely silently from the caller's point of view - the run
            ' just sits there forever waiting for a response that was never actually sent.
            ' Deliberately not re-thrown: the caller's subsequent WaitForFileAsync will just
            ' time out and show its own "check the log" message, which will now actually
            ' explain why, instead of risking an unhandled-exception crash on the UI thread.
            LogLine("*** Failed to send command: " & ex.Message)
        End Try
    End Sub

    Private Sub FlushCmd()
        Try
            p.StandardInput.Flush()
        Catch ex As Exception
            LogLine("*** Failed to flush input: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Closes XFOIL's stdin, which forces the Fortran runtime to hit end-of-file on its
    ''' next read and terminate - regardless of which menu level ("OPER", "OPERv", etc.)
    ''' it's currently sitting at. Confirmed against the real xfoil.exe: a literal "quit"
    ''' typed while inside the OPER menu is rejected ("QUIT command not recognized"),
    ''' which left the process hung waiting at that prompt forever in real interactive use
    ''' (the earlier "quit"-based approach only ever appeared to work because each
    ''' WaitForXFileAsync falls back to a plain File.Exists check once its timeout expires -
    ''' every run was silently burning its full timeout). Closing the pipe is exit-path-
    ''' agnostic and actually lets the completion checks that look at p.HasExited fire
    ''' promptly instead of always waiting out the clock.
    ''' </summary>
    Private Sub CloseXfoilInput()
        Try
            p.StandardInput.Close()
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Polls a file XFOIL is writing until it parses to non-empty data (optionally also
    ''' requiring the process to have exited, for commands issued as the last step of a
    ''' run), or times out.
    ''' </summary>
    Private Async Function WaitForFileAsync(path As String, timeoutMs As Integer, hasData As Func(Of String, Boolean), Optional requireProcessExit As Boolean = True) As Task(Of Boolean)
        Dim start = Environment.TickCount64
        Do
            Await Task.Delay(150)
            If File.Exists(path) Then
                Dim ok = False
                Try
                    ok = hasData(path)
                Catch
                End Try
                If ok AndAlso (Not requireProcessExit OrElse p Is Nothing OrElse p.HasExited) Then Return True
            End If
        Loop While Environment.TickCount64 - start < timeoutMs

        If File.Exists(path) Then
            Try
                Return hasData(path)
            Catch
                Return False
            End Try
        End If
        Return False
    End Function

#End Region

#Region "UI fields"

    Private txtAirfoil As TextBox
    Private btnBrowseAirfoil As Button
    Private btnLoadAirfoil As Button
    Private cmbTheme As ComboBox
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
    Private tabPolar As TabPage
    Private tabCp As TabPage
    Private tabBl As TabPage
    Private tabGeom As TabPage
    Private pPolar As PictureBox
    Private pCp As PictureBox
    Private pBl As PictureBox
    Private pGeom As PictureBox
    Private lstPolarRuns As CheckedListBox
    Private btnClearRuns As Button
    Private cmbBlQuantity As ComboBox

#End Region

#Region "Plot zoom/pan"

    ''' <summary>
    ''' Per-view zoom/pan state, in "native" (unzoomed, Scale=1) pixel coordinates:
    ''' PanX/PanY is the native-space point shown at the PictureBox's top-left corner.
    ''' Rather than stretching a fixed-resolution bitmap (which blurs when zoomed in),
    ''' every zoom/pan change re-renders the plot with this transform applied directly to
    ''' the drawing Graphics - so lines and text are always drawn crisp at the exact
    ''' effective resolution, the same way e.g. a PDF viewer stays sharp at any zoom.
    ''' </summary>
    Private Class PlotZoomState
        Public Scale As Double = 1.0
        Public PanX As Double = 0.0
        Public PanY As Double = 0.0
    End Class

    Private ReadOnly _plotTip As New ToolTip()
    ' Keyed by PictureBox rather than stored as named fields (pPolar/pCp/pBl/pGeom) so the
    ' shared Paint/mouse/render-dispatch handlers below can stay generic across all four
    ' plot views.
    Private ReadOnly _plotBitmaps As New Dictionary(Of PictureBox, Bitmap)()
    Private ReadOnly _plotZooms As New Dictionary(Of PictureBox, PlotZoomState)()
    Private ReadOnly _plotRenderers As New Dictionary(Of PictureBox, Action)()
    Private _dragPb As PictureBox = Nothing
    Private _dragStart As Point
    Private _dragPanStart As PointF

    ''' <summary>
    ''' Replaces a view's rendered bitmap and repaints it. The bitmap is always exactly
    ''' PictureBox-sized (never scaled up/down for zoom - see PlotZoomState) so painting
    ''' it is a plain 1:1 blit, never a stretch.
    ''' </summary>
    Private Sub SetPlotBitmap(pb As PictureBox, bmp As Bitmap)
        Dim old As Bitmap = Nothing
        _plotBitmaps.TryGetValue(pb, old)
        _plotBitmaps(pb) = bmp
        old?.Dispose()
        pb.Invalidate()
    End Sub

    ''' <summary>Re-renders whichever plot owns this PictureBox, picking up its current
    ''' zoom/pan state - the only way zoom/pan changes actually become visible.</summary>
    Private Sub RedrawPlot(pb As PictureBox)
        Dim renderer As Action = Nothing
        If _plotRenderers.TryGetValue(pb, renderer) Then renderer()
    End Sub

    ''' <summary>
    ''' Applies the current zoom/pan as a direct transform on the real drawing surface
    ''' (g.g - the SvgGraphics wrapper's underlying System.Drawing.Graphics), so every
    ''' subsequent DrawLine/DrawString/etc. in the caller draws already at the correct
    ''' zoomed screen position and resolution - true vector re-rendering, not a bitmap
    ''' stretch, which is what keeps lines and text crisp at any zoom level. No-op for
    ''' PNG/SVG/PDF export (captureVectors = True), which always renders the full
    ''' un-zoomed view regardless of the on-screen zoom state.
    ''' </summary>
    Private Sub ApplyPlotZoomTransform(g As SvgGraphics, pb As PictureBox, captureVectors As Boolean)
        If captureVectors Then Return
        Dim zoom As PlotZoomState = Nothing
        If Not _plotZooms.TryGetValue(pb, zoom) Then Return
        If zoom.Scale = 1.0 AndAlso zoom.PanX = 0.0 AndAlso zoom.PanY = 0.0 Then Return
        g.g.Transform = New Matrix(CSng(zoom.Scale), 0, 0, CSng(zoom.Scale),
                                    CSng(-zoom.PanX * zoom.Scale), CSng(-zoom.PanY * zoom.Scale))
    End Sub

    ''' <summary>
    ''' Clamps pan so the visible native-space window can never extend past the plot's own
    ''' [0, native size] extent - at Scale = 1 the window exactly equals that extent, so
    ''' this always pins pan to (0,0) until the user has zoomed in.
    ''' </summary>
    Private Sub ClampPan(pb As PictureBox, zoom As PlotZoomState)
        Dim viewW = pb.Width / zoom.Scale
        Dim viewH = pb.Height / zoom.Scale
        zoom.PanX = If(viewW >= pb.Width, 0.0, Math.Max(0.0, Math.Min(zoom.PanX, pb.Width - viewW)))
        zoom.PanY = If(viewH >= pb.Height, 0.0, Math.Max(0.0, Math.Min(zoom.PanY, pb.Height - viewH)))
    End Sub

    Private Sub PlotPictureBox_Paint(sender As Object, e As PaintEventArgs)
        Dim pb = CType(sender, PictureBox)
        Dim bmp As Bitmap = Nothing
        If Not _plotBitmaps.TryGetValue(pb, bmp) OrElse bmp Is Nothing Then Return
        e.Graphics.DrawImageUnscaled(bmp, 0, 0)
    End Sub

    ''' <summary>Mouse-wheel zoom, keeping the native-space point under the cursor fixed
    ''' on screen, then immediately re-renders at the new effective resolution.</summary>
    Private Sub PlotPictureBox_MouseWheel(sender As Object, e As MouseEventArgs)
        Dim pb = CType(sender, PictureBox)
        Dim zoom As PlotZoomState = Nothing
        If Not _plotZooms.TryGetValue(pb, zoom) Then Return
        If pb.Width <= 0 OrElse pb.Height <= 0 Then Return

        Dim oldScale = zoom.Scale
        Dim factor = If(e.Delta > 0, 1.15, 1.0 / 1.15)
        Dim newScale = Math.Max(1.0, Math.Min(20.0, oldScale * factor))
        If newScale = oldScale Then Return

        Dim nativeX = zoom.PanX + e.X / oldScale
        Dim nativeY = zoom.PanY + e.Y / oldScale
        zoom.Scale = newScale
        zoom.PanX = nativeX - e.X / newScale
        zoom.PanY = nativeY - e.Y / newScale
        ClampPan(pb, zoom)
        RedrawPlot(pb)
    End Sub

    Private Sub PlotPictureBox_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button <> MouseButtons.Left Then Return
        Dim pb = CType(sender, PictureBox)
        Dim zoom As PlotZoomState = Nothing
        If Not _plotZooms.TryGetValue(pb, zoom) Then Return
        _dragPb = pb
        _dragStart = e.Location
        _dragPanStart = New PointF(CSng(zoom.PanX), CSng(zoom.PanY))
        pb.Cursor = Cursors.SizeAll
    End Sub

    Private Sub PlotPictureBox_MouseMove(sender As Object, e As MouseEventArgs)
        Dim pb = CType(sender, PictureBox)
        If _dragPb IsNot pb Then Return
        Dim zoom = _plotZooms(pb)
        zoom.PanX = _dragPanStart.X - (e.X - _dragStart.X) / zoom.Scale
        zoom.PanY = _dragPanStart.Y - (e.Y - _dragStart.Y) / zoom.Scale
        ClampPan(pb, zoom)
        RedrawPlot(pb)
    End Sub

    Private Sub PlotPictureBox_MouseUp(sender As Object, e As MouseEventArgs)
        If e.Button <> MouseButtons.Left Then Return
        Dim pb = CType(sender, PictureBox)
        If _dragPb Is pb Then
            _dragPb = Nothing
            pb.Cursor = Cursors.Default
        End If
    End Sub

    Private Sub PlotPictureBox_DoubleClick(sender As Object, e As EventArgs)
        ResetPlotZoom(CType(sender, PictureBox))
    End Sub

    Private Sub ResetPlotZoom(pb As PictureBox)
        Dim zoom As PlotZoomState = Nothing
        If Not _plotZooms.TryGetValue(pb, zoom) Then Return
        zoom.Scale = 1.0
        zoom.PanX = 0.0
        zoom.PanY = 0.0
        RedrawPlot(pb)
    End Sub

#End Region

#Region "Data"

    Private Shared ReadOnly _runColorPalette As Color() = {
        Color.DodgerBlue, Color.Crimson, Color.ForestGreen, Color.DarkOrange,
        Color.Purple, Color.Teal, Color.SaddleBrown, Color.DeepPink
    }

    ' Boundary-layer quantities available on the BL tab, shown one at a time - matching
    ' XFOIL's own VPLO submenu names/order as closely as the data this app actually has
    ' access to allows. VPLO also offers CD (dissipation coefficient), N (amplification
    ' ratio), and CT (max shear coefficient), but those aren't in the "dump" file XFOIL
    ' writes (confirmed against a real run: dump is fixed at s/x/y/Ue/Dstar/Theta/Cf/H,
    ' and unlike the interactive on-screen VPLO plots, XFOIL has no command to write
    ' those three to a file this app can parse) - RT/RTL are still derivable, though,
    ' since Re_theta = Re_chord * (Ue/Vinf) * (Theta/c), all of which ARE in the dump.
    '
    ' Indices 0/1/2/5/6 are single-quantity "one line per surface" plots, rendered via
    ' BlQuantityValue + the normal top/bottom DrawBlSeries path. Indices 3/4 (DT/DB) are
    ' XFOIL's own combined Dstar+Theta-vs-x plot for ONE surface only (not one quantity
    ' across both surfaces) - RenderBlPlot special-cases those two instead of going
    ' through BlQuantityValue. Keep BlQuantityDualIndex in sync with their position here.
    Private Shared ReadOnly _blQuantityNames As String() = {
        "H (shape parameter)", "UE (edge velocity)", "CF (skin friction)",
        "DT (top: Dstar & Theta)", "DB (bottom: Dstar & Theta)",
        "RT (Re_theta)", "RTL (log Re_theta)"
    }
    Private Const BlQuantityIndexDT As Integer = 3
    Private Const BlQuantityIndexDB As Integer = 4
    ' Axis label for each single-quantity entry, as (main character, superscript/
    ' subscript) - matches XFOIL's own naming (e.g. the shape parameter is "H" with
    ' subscript "k", read "Hk"). Unused (blank) for the DT/DB dual-quantity entries,
    ' which draw their own small two-color legend instead - see RenderBlPlot.
    Private Shared ReadOnly _blQuantityAxisMain As String() = {"H", "U", "C", "", "", "Re", "logRe"}
    Private Shared ReadOnly _blQuantityAxisSub As String() = {"k", "e", "f", "", "", "θ", "θ"}

    ' Dark ("XFOIL", black background/white content, matches the real xfoil.exe's own
    ' plot windows) is the default; Light swaps background/foreground only - the
    ' upper/lower surface accent colors stay the same in both so they're never the same
    ' color as whatever the background happens to be.
    Private _isDarkTheme As Boolean = True
    Private ReadOnly Property ThemeBackColor As Color
        Get
            Return If(_isDarkTheme, Color.Black, Color.White)
        End Get
    End Property
    Private ReadOnly Property ThemeForeColor As Color
        Get
            Return If(_isDarkTheme, Color.White, Color.Black)
        End Get
    End Property
    Private ReadOnly Property ThemeGridColor As Color
        Get
            Return If(_isDarkTheme, Color.FromArgb(90, 90, 90), Color.LightGray)
        End Get
    End Property
    Private ReadOnly Property ThemeUpperColor As Color
        Get
            Return Color.OrangeRed
        End Get
    End Property
    Private ReadOnly Property ThemeLowerColor As Color
        Get
            Return Color.DeepSkyBlue
        End Get
    End Property

    Private _polarRuns As New List(Of XfoilPolarRun)
    Private _cpPoints As New List(Of XfoilCpPoint)
    Private _blTop As New List(Of XfoilBLPoint)
    Private _blBottom As New List(Of XfoilBLPoint)
    Private _airfoilCoords As New List(Of XfoilGeomPoint)

    ' Transition location for the single-point (Cp/BL) run, read from XFOIL's console
    ' echo ("Side 1/2 free transition at x/c = ..."). Not available from the .pol polar
    ' sweep dump itself for this per-point view (that data - Top_Xtr/Bot_Xtr - lives in
    ' XfoilPolarPoint instead, one pair per alpha).
    Private _lastTopXtr As Double? = Nothing
    Private _lastBotXtr As Double? = Nothing

    ' Converged CL/CM/CD for the single-point run, read from XFOIL's console echo
    ' ("a =  5.000   CL =  0.5571" / "Cm =  0.0019  CD = 0.00848 => CDf = ... CDp = ..."),
    ' for the parameter block on the Cp/BL plots (matches XFOIL's own CPX view). Re and
    ' Ncrit aren't parsed from the log - they're just the run's own input parameters,
    ' captured directly in btnRunPoint_Click.
    Private _lastPointCL As Double? = Nothing
    Private _lastPointCM As Double? = Nothing
    Private _lastPointCD As Double? = Nothing
    Private _lastPointAlpha As Double? = Nothing
    Private _lastPointRe As Double? = Nothing
    Private _lastPointNcrit As Double? = Nothing
    Private _lastPointMach As Double? = Nothing

    Private _polarSvg As String = "" : Private _polarPdf As String = ""
    Private _cpSvg As String = "" : Private _cpPdf As String = ""
    Private _blSvg As String = "" : Private _blPdf As String = ""
    Private _geomSvg As String = "" : Private _geomPdf As String = ""

#End Region

    Public Sub New()
        Directory.CreateDirectory(_tempDir)
        Me.Icon = frmMain.Icon
        InitializeUi()
        ' _logFlushTimer is only declared/wired (New Timer With {...}, Handles ... .Tick) above -
        ' a WinForms Timer does NOT start ticking just from being constructed, Enabled defaults
        ' to False. Without this, _logBuffer accumulates every byte XFOIL ever prints (and any
        ' Reader-thread/status lines appended to it) forever, but none of it ever reaches the
        ' visible txtLog box - looks exactly like "XFOIL isn't saying anything" even when the
        ' underlying process communication is working fine.
        _logFlushTimer.Start()
    End Sub

#Region "UI layout"

    Private Sub InitializeUi()
        Me.Text = "XFOIL Analysis"
        Me.Size = frmMain.Size
        Me.WindowState = frmMain.WindowState
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Font = frmMain.systemFont

        Dim outer As New TableLayoutPanel()
        outer.Dock = DockStyle.Fill
        outer.ColumnCount = 1
        outer.RowCount = 3
        outer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        outer.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        outer.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        outer.RowStyles.Add(New RowStyle(SizeType.Absolute, 200.0F))
        Me.Controls.Add(outer)

        outer.Controls.Add(BuildParamsPanel(), 0, 0)

        tc = New TabControl()
        tc.Dock = DockStyle.Fill

        tabPolar = New TabPage("Polar")
        tabCp = New TabPage("Cp Distribution")
        tabBl = New TabPage("Boundary Layer")
        tabGeom = New TabPage("Geometry")
        tc.Controls.Add(tabPolar)
        tc.Controls.Add(tabCp)
        tc.Controls.Add(tabBl)
        tc.Controls.Add(tabGeom)
        outer.Controls.Add(tc, 0, 1)

        pPolar = BuildPlotTab(tabPolar, "Polar")
        pCp = BuildPlotTab(tabCp, "Cp")

        Dim lblBlQty As New Label() With {
            .Text = "Quantity:",
            .AutoSize = True,
            .Font = frmMain.systemFont,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        cmbBlQuantity = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = frmMain.systemFont,
            .Width = 230,
            .Height = 23
        }
        cmbBlQuantity.Items.AddRange(_blQuantityNames)
        cmbBlQuantity.SelectedIndex = 0
        AddHandler cmbBlQuantity.SelectedIndexChanged, Sub(s, e) RenderBlPlot()
        _plotTip.SetToolTip(cmbBlQuantity, "Which boundary-layer quantity to plot (from the last viscous point-analysis run)")
        pBl = BuildPlotTab(tabBl, "BL", {CType(lblBlQty, Control), cmbBlQuantity})

        pGeom = BuildPlotTab(tabGeom, "Geometry")

        _plotRenderers(pPolar) = AddressOf RenderPolarPlot
        _plotRenderers(pCp) = AddressOf RenderCpPlot
        _plotRenderers(pBl) = AddressOf RenderBlPlot
        _plotRenderers(pGeom) = AddressOf RenderGeometryPlot

        BuildPolarRunsPanel(tabPolar)

        ' Resizing invalidates any active zoom/pan (its offsets are in the old size's pixel
        ' space), so reset to fit rather than leaving the view stale or oddly cropped.
        AddHandler pPolar.Resize, Sub(s, e)
                                      ResetPlotZoom(pPolar)
                                      _polarResizeTimer.Stop()
                                      _polarResizeTimer.Start()
                                  End Sub
        AddHandler pCp.Resize, Sub(s, e)
                                   ResetPlotZoom(pCp)
                                   _cpResizeTimer.Stop()
                                   _cpResizeTimer.Start()
                               End Sub
        AddHandler pBl.Resize, Sub(s, e)
                                   ResetPlotZoom(pBl)
                                   _blResizeTimer.Stop()
                                   _blResizeTimer.Start()
                               End Sub
        AddHandler pGeom.Resize, Sub(s, e)
                                     ResetPlotZoom(pGeom)
                                     _geomResizeTimer.Stop()
                                     _geomResizeTimer.Start()
                                 End Sub

        ' Re-render the newly active tab when the user switches tabs: WinForms only finishes
        ' laying out a TabPage's Dock=Fill children a moment after that page actually becomes
        ' selected, so a tab that was never selected before can still report a stale/undersized
        ' PictureBox if read synchronously in this same handler. BeginInvoke defers the render
        ' to the next message-loop tick, by which point the resize/layout cascade has settled.
        AddHandler tc.SelectedIndexChanged, Sub(s, e)
                                                Me.BeginInvoke(Sub()
                                                                   Select Case tc.SelectedIndex
                                                                       Case 0 : RenderPolarPlot()
                                                                       Case 1 : RenderCpPlot()
                                                                       Case 2 : RenderBlPlot()
                                                                       Case 3 : RenderGeometryPlot()
                                                                   End Select
                                                               End Sub)
                                            End Sub

        ' Re-render the initially-selected tab (Polar) once more after the window is actually
        ' shown, deferred a tick via BeginInvoke so it runs after WinForms finishes the resize/
        ' layout cascade to the form's final size - the construction-time render below can run
        ' before that cascade is done and land on a stale, too-small size. Deliberately only
        ' the Polar tab here, not all four: rendering every tab together before any of them has
        ' ever been the selected tab causes them to fight over the shared TabControl's layout
        ' pass and land on stale sizes. The others render correctly on their own the moment the
        ' user actually selects them, via the SelectedIndexChanged handler above.
        AddHandler Me.Shown, Sub(s, e)
                                 Me.BeginInvoke(Sub() RenderPolarPlot())
                             End Sub

        RenderPolarPlot()
        RenderCpPlot()
        RenderBlPlot()
        RenderGeometryPlot()

        outer.Controls.Add(BuildLogPanel(), 0, 2)
    End Sub

    ''' <summary>
    ''' Raw XFOIL console log, so a hung/misbehaving run can actually be diagnosed:
    ''' every command WE send is echoed here (prefixed "&gt;&gt;&gt;") interleaved with
    ''' whatever XFOIL prints back, in the order it happens. If a run stalls, the last
    ''' few lines show exactly which command it stalled after.
    ''' </summary>
    Private Function BuildLogPanel() As Panel
        Dim outer As New TableLayoutPanel()
        outer.Dock = DockStyle.Fill
        outer.ColumnCount = 1
        outer.RowCount = 2
        outer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        outer.RowStyles.Add(New RowStyle(SizeType.Absolute, 34.0F))
        outer.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

        Dim header As New Panel()
        header.Dock = DockStyle.Fill
        header.BackColor = Color.WhiteSmoke
        header.Controls.Add(New Label() With {
            .Text = "XFOIL Console Log",
            .Dock = DockStyle.Left,
            .Width = 200,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(4, 0, 0, 0),
            .Font = New Font(frmMain.systemFont, FontStyle.Bold)
        })
        Dim btnCopyLog As New Button() With {
            .Text = "Copy",
            .Dock = DockStyle.Right,
            .Width = 70,
            .Height = 26,
            .Margin = New Padding(4),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.White,
            .Cursor = Cursors.Hand
        }
        AddHandler btnCopyLog.Click, Sub(s, e)
                                         If txtLog.TextLength > 0 Then
                                             Clipboard.SetText(txtLog.Text)
                                             AppToast.Show("Log copied to clipboard")
                                         End If
                                     End Sub
        Dim btnClearLog As New Button() With {
            .Text = "Clear",
            .Dock = DockStyle.Right,
            .Width = 70,
            .Height = 26,
            .Margin = New Padding(4),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.White,
            .Cursor = Cursors.Hand
        }
        AddHandler btnClearLog.Click, Sub(s, e) txtLog.Clear()
        Dim btnCheckStale As New Button() With {
            .Text = "Check XFOIL Processes",
            .Dock = DockStyle.Right,
            .Width = 150,
            .Height = 26,
            .Margin = New Padding(4),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.White,
            .Cursor = Cursors.Hand
        }
        AddHandler btnCheckStale.Click, AddressOf btnCheckStale_Click
        _plotTip.SetToolTip(btnCheckStale, "List every xfoil.exe process on this machine and offer to kill any left over from a crashed session")
        header.Controls.Add(btnClearLog)
        header.Controls.Add(btnCopyLog)
        header.Controls.Add(btnCheckStale)
        outer.Controls.Add(header, 0, 0)

        txtLog = New TextBox()
        txtLog.Dock = DockStyle.Fill
        txtLog.Multiline = True
        txtLog.ReadOnly = True
        txtLog.ScrollBars = ScrollBars.Vertical
        txtLog.Font = frmMain.systemFont
        txtLog.BackColor = Color.Black
        txtLog.ForeColor = Color.White
        txtLog.BorderStyle = BorderStyle.FixedSingle
        outer.Controls.Add(txtLog, 0, 1)

        Return outer
    End Function

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
        btnLoadAirfoil = NewButton("Load Airfoil", 100)
        AddHandler btnLoadAirfoil.Click, AddressOf btnLoadAirfoil_Click
        row1.Controls.Add(btnLoadAirfoil)
        row1.Controls.Add(NewLabel("      Plot theme:"))
        cmbTheme = New ComboBox() With {.Width = 110, .DropDownStyle = ComboBoxStyle.DropDownList, .Margin = New Padding(4, 3, 4, 3)}
        cmbTheme.Items.Add("Dark (XFOIL)")
        cmbTheme.Items.Add("Light")
        cmbTheme.SelectedIndex = 0
        AddHandler cmbTheme.SelectedIndexChanged, AddressOf cmbTheme_SelectedIndexChanged
        row1.Controls.Add(cmbTheme)
        rows.Controls.Add(row1, 0, 0)

        ' Row 2: flow conditions
        Dim row2 As New FlowLayoutPanel()
        row2.AutoSize = True
        row2.WrapContents = False
        row2.Controls.Add(NewLabel("Re:"))
        txtRe = New TextBox() With {.Width = 70, .Text = "1e6", .Margin = New Padding(4, 3, 12, 3)}
        _plotTip.SetToolTip(txtRe, "Reynolds number. Accepts scientific notation (e.g. 1e6). 0 runs an inviscid (no boundary-layer) analysis.")
        row2.Controls.Add(txtRe)
        row2.Controls.Add(NewLabel("Mach:"))
        txtMach = New TextBox() With {.Width = 50, .Text = "0", .Margin = New Padding(4, 3, 12, 3)}
        row2.Controls.Add(txtMach)
        row2.Controls.Add(NewLabel("Ncrit:"))
        txtNcrit = New TextBox() With {.Width = 40, .Text = "9", .Margin = New Padding(4, 3, 12, 3)}
        _plotTip.SetToolTip(txtNcrit,
            "Ncrit: how 'clean' (low-turbulence) the airflow is, which controls how early the boundary" & vbCrLf &
            "layer transitions from smooth (laminar) to turbulent flow." & vbCrLf &
            vbCrLf &
            "XFOIL predicts transition with the e^N method: tiny disturbances in the laminar boundary" & vbCrLf &
            "layer grow exponentially with distance; transition is assumed to occur once that growth" & vbCrLf &
            "reaches a factor of e^Ncrit. A lower Ncrit means transition (and more drag) happens sooner." & vbCrLf &
            vbCrLf &
            "Typical values: ~4-5 for a noisy/turbulent wind tunnel or a dirty/bumpy wing surface," & vbCrLf &
            "9 for a smooth low-turbulence wind tunnel (XFOIL's default, used here), 11-14 for very" & vbCrLf &
            "clean free-flight/sailplane conditions." & vbCrLf &
            vbCrLf &
            "Only affects viscous runs (Re > 0) - ignored for an inviscid run (Re = 0).")
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
    Private Function BuildPlotTab(tab As TabPage, viewName As String, Optional leftControls As IEnumerable(Of Control) = Nothing) As PictureBox
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
        pb.BackColor = ThemeBackColor
        ' Never set pb.Image directly - the rendered bitmap is kept in _plotBitmaps and
        ' drawn manually in PlotPictureBox_Paint (with the zoom/pan transform applied),
        ' so PictureBox's own built-in Image drawing would just double-paint underneath it.
        pb.SizeMode = PictureBoxSizeMode.Normal
        _plotZooms(pb) = New PlotZoomState()
        AddHandler pb.Paint, AddressOf PlotPictureBox_Paint
        AddHandler pb.MouseWheel, AddressOf PlotPictureBox_MouseWheel
        AddHandler pb.MouseDown, AddressOf PlotPictureBox_MouseDown
        AddHandler pb.MouseMove, AddressOf PlotPictureBox_MouseMove
        AddHandler pb.MouseUp, AddressOf PlotPictureBox_MouseUp
        AddHandler pb.DoubleClick, AddressOf PlotPictureBox_DoubleClick
        _plotTip.SetToolTip(pb, "Scroll to zoom, drag to pan, double-click to reset")
        outer.Controls.Add(pb, 0, 1)

        If leftControls IsNot Nothing Then
            Dim leftX As Integer = 8
            For Each ctrl In leftControls
                ctrl.Location = New Point(leftX, (36 - ctrl.Height) \ 2)
                ctrl.Anchor = AnchorStyles.Top Or AnchorStyles.Left
                controlPanel.Controls.Add(ctrl)
                leftX += ctrl.Width + 8
            Next
        End If

        AddExportButtonToPanel(controlPanel, pb, viewName)
        Return pb
    End Function

    ''' <summary>
    ''' Adds a right-docked side panel to the Polar tab listing every polar sweep run
    ''' so far, with a checkbox to toggle its visibility in the plot (multi-run overlay/
    ''' comparison) and a button to clear the run history. Added after BuildPlotTab has
    ''' already added the Fill-docked plot area to tabPolar.Controls - a Right-docked
    ''' sibling still gets its own space and the Fill sibling takes whatever remains,
    ''' regardless of add order (DockStyle.Fill is always resolved last).
    ''' </summary>
    Private Sub BuildPolarRunsPanel(tab As TabPage)
        Dim panel As New Panel()
        panel.Dock = DockStyle.Right
        panel.Width = 190
        panel.BackColor = Color.WhiteSmoke

        Dim header As New Label() With {
            .Text = "Polar Runs",
            .Dock = DockStyle.Top,
            .Height = 22,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(6, 0, 0, 0),
            .Font = New Font(frmMain.systemFont, FontStyle.Bold)
        }
        panel.Controls.Add(header)

        btnClearRuns = New Button() With {
            .Text = "Clear Runs",
            .Dock = DockStyle.Bottom,
            .Height = 26,
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.White,
            .Cursor = Cursors.Hand
        }
        AddHandler btnClearRuns.Click, AddressOf btnClearRuns_Click
        panel.Controls.Add(btnClearRuns)

        lstPolarRuns = New CheckedListBox()
        lstPolarRuns.Dock = DockStyle.Fill
        lstPolarRuns.CheckOnClick = True
        lstPolarRuns.BorderStyle = BorderStyle.None
        _plotTip.SetToolTip(lstPolarRuns, "Every completed polar sweep. Check/uncheck a run to show/hide it on the plot.")
        AddHandler lstPolarRuns.ItemCheck, AddressOf lstPolarRuns_ItemCheck
        panel.Controls.Add(lstPolarRuns)

        tab.Controls.Add(panel)
    End Sub

    Private Sub AddExportButtonToPanel(panel As Panel, pb As PictureBox, viewName As String)
        Dim btnExport As New Button()
        btnExport.Text = "Export ▾"
        btnExport.Font = frmMain.systemFont
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
        ElseIf pb Is pGeom Then
            If format <> "PNG" Then RenderGeometryPlot(True)
            svgContent = _geomSvg : pdfContent = _geomPdf
        End If

        Dim plotBmp As Bitmap = Nothing
        If format = "PNG" Then
            _plotBitmaps.TryGetValue(pb, plotBmp)
            If plotBmp Is Nothing Then
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
                        Using imgCopy As Image = CType(plotBmp.Clone(), Image)
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

    Private Async Sub btnLoadAirfoil_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtAirfoil.Text) Then
            AppMessageBox.Show("Enter a NACA code (e.g. 0012) or browse to an airfoil .dat file.", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        btnLoadAirfoil.Enabled = False
        btnRunPolar.Enabled = False
        btnRunPoint.Enabled = False
        Try
            lblStatus.Text = "Status: loading airfoil..."
            Dim ok = Await EnsureAirfoilLoadedAsync()
            lblStatus.Text = If(ok, "Status: airfoil loaded", "Status: failed to load airfoil (check log below)")
            If ok Then tc.SelectedIndex = 3
        Finally
            btnLoadAirfoil.Enabled = True
            btnRunPolar.Enabled = True
            btnRunPoint.Enabled = True
        End Try
    End Sub

    Private Sub cmbTheme_SelectedIndexChanged(sender As Object, e As EventArgs)
        _isDarkTheme = (cmbTheme.SelectedIndex = 0)
        pPolar.BackColor = ThemeBackColor
        pCp.BackColor = ThemeBackColor
        pBl.BackColor = ThemeBackColor
        pGeom.BackColor = ThemeBackColor
        RenderPolarPlot()
        RenderCpPlot()
        RenderBlPlot()
        RenderGeometryPlot()
    End Sub

    Private Sub lstPolarRuns_ItemCheck(sender As Object, e As ItemCheckEventArgs)
        If e.Index < 0 OrElse e.Index >= _polarRuns.Count Then Return
        Dim run = _polarRuns(e.Index)
        ' ItemCheck fires before the check state is applied, so use e.NewValue.
        run.Visible = (e.NewValue = CheckState.Checked)
        Me.BeginInvoke(Sub() RenderPolarPlot())
    End Sub

    Private Sub btnClearRuns_Click(sender As Object, e As EventArgs)
        _polarRuns.Clear()
        lstPolarRuns.Items.Clear()
        RenderPolarPlot()
    End Sub

    ''' <summary>
    ''' Lists every "xfoil" process currently running on the machine (not just ones this
    ''' form started) - a stale one left over from a crashed/killed earlier session can
    ''' hold a lock on appdata\xfoil.exe or the temp working folder, or otherwise interfere
    ''' with a fresh run in ways that look identical to "XFOIL just isn't responding".
    ''' Offers to kill any that aren't the one this form is actively using right now.
    ''' </summary>
    Private Sub btnCheckStale_Click(sender As Object, e As EventArgs)
        Dim all As Process() = Process.GetProcessesByName("xfoil")
        Try
            If all.Length = 0 Then
                LogLine("*** No xfoil.exe processes found running on this machine.")
                Return
            End If

            Dim ownPid As Integer = -1
            If p IsNot Nothing Then
                Try
                    If Not p.HasExited Then ownPid = p.Id
                Catch
                End Try
            End If

            LogLine($"*** Found {all.Length} xfoil.exe process(es) running:")
            Dim stale As New List(Of Process)
            For Each proc In all
                Dim isOwn = (proc.Id = ownPid)
                Dim startInfo = ""
                Try
                    startInfo = $", started {proc.StartTime:HH:mm:ss}, {proc.TotalProcessorTime.TotalSeconds:0.0}s CPU time"
                Catch
                    ' Access to some process info can be denied depending on how it was launched.
                End Try
                LogLine($"    PID {proc.Id}{startInfo}{If(isOwn, "  <- this window's current process", "")}")
                If Not isOwn Then stale.Add(proc)
            Next

            If stale.Count = 0 Then
                LogLine("*** All of them are the one this window is currently using - nothing stale.")
                Return
            End If

            Dim msg = $"Found {stale.Count} xfoil.exe process(es) not associated with this window (possibly left over from a crashed or force-closed session). Kill them?"
            If AppMessageBox.Show(msg, "Check XFOIL Processes", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                For Each proc In stale
                    Try
                        proc.Kill()
                        LogLine($"*** Killed stale xfoil.exe (PID {proc.Id}).")
                    Catch ex As Exception
                        LogLine($"*** Failed to kill PID {proc.Id}: {ex.Message}")
                    End Try
                Next
            End If
        Finally
            For Each proc In all
                proc.Dispose()
            Next
        End Try
    End Sub

#End Region

#Region "Loading the airfoil + geometry"

    Private Sub LoadAirfoilCommands()
        Dim text = txtAirfoil.Text.Trim()
        If Regex.IsMatch(text, "^\d{4,5}$") Then
            WriteCmd($"naca {text}")
        Else
            WriteCmd($"load {text}")
        End If
    End Sub

    ''' <summary>
    ''' Loads the airfoil and captures its buffer coordinates via XFOIL's top-level SAVE
    ''' command (verified against the real xfoil.exe: a title line followed by plain
    ''' "x y" rows, TE -> upper -> LE -> lower -> TE) into _airfoilCoords, which backs
    ''' both the Geometry tab and the airfoil-shape overlay on the Cp plot.
    '''
    ''' ALWAYS closes stdin (forcing the process to exit) before checking for the file.
    ''' Confirmed directly against xfoil.exe: the file SAVE writes is not flushed to disk
    ''' until the process itself terminates - it stays sitting in an unflushed buffer for
    ''' as long as the process is kept alive to receive more commands (reproduced outside
    ''' this app entirely, with a bare .NET Process and no reader thread at all, so it's a
    ''' genuine behavior of this XFOIL build's Fortran I/O, not a bug in how this app reads
    ''' output). That means this step can never share a process with whatever analysis
    ''' commands come after it - the caller must start a fresh process afterward.
    ''' </summary>
    Private Async Function EnsureAirfoilLoadedAsync() As Task(Of Boolean)
        If Not Await EnsureXfoilReadyAsync() Then Return False

        _geomFile = NewTempFile("geom")

        LoadAirfoilCommands()
        WriteCmd($"save {Path.GetFileName(_geomFile)}")
        FlushCmd()
        CloseXfoilInput()

        Dim ok = Await WaitForFileAsync(_geomFile, 15000, Function(path) ParseGeomFile(path).Count > 0, requireProcessExit:=True)
        If ok Then
            _airfoilCoords = ParseGeomFile(_geomFile)
            RenderGeometryPlot()
            RenderCpPlot()
        End If
        Return ok
    End Function

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
        btnLoadAirfoil.Enabled = False
        Try
            lblStatus.Text = "Status: loading airfoil..."
            If Not Await EnsureAirfoilLoadedAsync() Then Return

            ' EnsureAirfoilLoadedAsync always closes its own process once the geometry file
            ' is safely flushed to disk (see its comment) - start a fresh one here for the
            ' actual analysis run, re-sending the airfoil load since it's a brand new session.
            If Not Await EnsureXfoilReadyAsync() Then Return
            LoadAirfoilCommands()

            _polFile = NewTempFile("polar")

            lblStatus.Text = "Status: running polar sweep..."

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
            WriteCmd(Path.GetFileName(_polFile))
            WriteCmd("")
            WriteCmd($"aseq {aMin.ToString("0.###", CultureInfo.InvariantCulture)} {aMax.ToString("0.###", CultureInfo.InvariantCulture)} {aStep.ToString("0.###", CultureInfo.InvariantCulture)}")
            WriteCmd("pacc")
            FlushCmd()
            CloseXfoilInput()

            Dim numPoints = Math.Max(1, CInt(Math.Abs((aMax - aMin) / aStep)) + 1)
            Dim timeoutMs = Math.Max(10000, numPoints * 2000)
            Dim ok = Await WaitForFileAsync(_polFile, timeoutMs, Function(path) ParsePolarFile(path).Count > 0)
            If Not ok Then
                lblStatus.Text = "Status: polar sweep did not produce output (check log below)"
                AppMessageBox.Show("XFOIL did not produce a polar file in time. Check the log for errors (e.g. an airfoil that failed to load, or a non-converging viscous run).", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim points = ParsePolarFile(_polFile)
            Dim run As New XfoilPolarRun With {
                .Label = $"{txtAirfoil.Text.Trim()}  Re={FormatRe(re)}  M={mach:0.00}",
                .Color = _runColorPalette(_polarRuns.Count Mod _runColorPalette.Length),
                .Points = points,
                .Visible = True
            }
            _polarRuns.Add(run)
            lstPolarRuns.Items.Add(run.Label, True)

            RenderPolarPlot()
            lblStatus.Text = $"Status: polar sweep complete - {points.Count} point(s)"
            tc.SelectedIndex = 0
        Finally
            btnRunPolar.Enabled = True
            btnRunPoint.Enabled = True
            btnLoadAirfoil.Enabled = True
        End Try
    End Sub

    Private Function FormatRe(re As Double) As String
        If re <= 0 Then Return "inviscid"
        Return (re / 1000000.0).ToString("0.0#", CultureInfo.InvariantCulture) & "e6"
    End Function

#End Region

#Region "Run point analysis (Cp + boundary layer)"

    Private Async Sub btnRunPoint_Click(sender As Object, e As EventArgs)
        Dim re As Double, mach As Double, alpha As Double, ncrit As Double
        If Not Double.TryParse(txtRe.Text, NumberStyles.Float, CultureInfo.InvariantCulture, re) Then re = 0
        If Not Double.TryParse(txtMach.Text, NumberStyles.Float, CultureInfo.InvariantCulture, mach) Then mach = 0
        If Not Double.TryParse(txtNcrit.Text, NumberStyles.Float, CultureInfo.InvariantCulture, ncrit) Then ncrit = 9
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
        btnLoadAirfoil.Enabled = False
        Try
            lblStatus.Text = "Status: loading airfoil..."
            If Not Await EnsureAirfoilLoadedAsync() Then Return

            ' EnsureAirfoilLoadedAsync always closes its own process once the geometry file
            ' is safely flushed to disk (see its comment) - start a fresh one here for the
            ' actual analysis run, re-sending the airfoil load since it's a brand new session.
            If Not Await EnsureXfoilReadyAsync() Then Return
            LoadAirfoilCommands()

            _cpFile = NewTempFile("cp")
            _blFile = NewTempFile("bl")

            lblStatus.Text = "Status: running point analysis..."

            WriteCmd("oper")
            If re > 0 Then
                WriteCmd("visc " & re.ToString("F0", CultureInfo.InvariantCulture))
                WriteCmd("iter 200")
                WriteCmd("vpar")
                WriteCmd("n " & ncrit.ToString("0.###", CultureInfo.InvariantCulture))
                WriteCmd("")
            End If
            WriteCmd("mach " & mach.ToString("0.###", CultureInfo.InvariantCulture))
            WriteCmd($"alfa {alpha.ToString("0.###", CultureInfo.InvariantCulture)}")
            WriteCmd($"cpwr {Path.GetFileName(_cpFile)}")
            If re > 0 Then WriteCmd($"dump {Path.GetFileName(_blFile)}")
            FlushCmd()
            CloseXfoilInput()

            Dim ok = Await WaitForFileAsync(_cpFile, 10000, Function(path) ParseCpFile(path).Count > 0)
            If Not ok Then
                lblStatus.Text = "Status: point analysis did not produce Cp output (check log below)"
                AppMessageBox.Show("XFOIL did not produce a Cp output file in time. Check the log for errors.", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            _cpPoints = ParseCpFile(_cpFile)

            If re > 0 Then
                Dim okBl = Await WaitForFileAsync(_blFile, 10000, Function(path)
                                                                      Dim top As New List(Of XfoilBLPoint)
                                                                      Dim bot As New List(Of XfoilBLPoint)
                                                                      ParseBlFile(path, top, bot)
                                                                      Return top.Count > 0 OrElse bot.Count > 0
                                                                  End Function)
                If okBl Then
                    ParseBlFile(_blFile, _blTop, _blBottom)
                Else
                    _blTop.Clear() : _blBottom.Clear()
                End If
            Else
                _blTop.Clear() : _blBottom.Clear()
            End If

            ParseTransitionFromLog()
            _lastPointAlpha = alpha
            _lastPointMach = mach
            If re > 0 Then
                _lastPointRe = re
                _lastPointNcrit = ncrit
            Else
                _lastPointRe = Nothing
                _lastPointNcrit = Nothing
            End If

            lblStatus.Text = $"Status: point analysis complete at alpha = {alpha}"
            tc.SelectedIndex = 1
            RenderCpPlot()
            RenderBlPlot()
        Finally
            btnRunPolar.Enabled = True
            btnRunPoint.Enabled = True
            btnLoadAirfoil.Enabled = True
        End Try
    End Sub

    ''' <summary>
    ''' XFOIL's console echo for a converged point prints "Side 1 free transition at
    ''' x/c = ..." / "Side 2 free transition at x/c = ..." once per Newton iteration
    ''' (Side 1 = upper/top surface, Side 2 = lower/bottom - confirmed against a real
    ''' run), and separately "a =  5.000      CL =  0.5571" / "Cm =  0.0019     CD =
    ''' 0.00848   =>   CDf =  0.00543    CDp =  0.00304" for the converged forces. Take
    ''' the LAST match of each from the accumulated log text (the final converged value)
    ''' to mark transition and show the parameter block on the Cp plot, matching XFOIL's
    ''' own CPX view.
    ''' </summary>
    Private Sub ParseTransitionFromLog()
        _lastTopXtr = Nothing
        _lastBotXtr = Nothing
        _lastPointCL = Nothing
        _lastPointCM = Nothing
        _lastPointCD = Nothing
        Dim text = txtLog.Text
        If String.IsNullOrEmpty(text) Then Return

        _lastTopXtr = LastRegexDouble(text, "Side\s+1\s+\S+\s+transition at x/c\s*=\s*([\d.]+)")
        _lastBotXtr = LastRegexDouble(text, "Side\s+2\s+\S+\s+transition at x/c\s*=\s*([\d.]+)")
        _lastPointCL = LastRegexDouble(text, "CL\s*=\s*(-?[\d.]+)")
        _lastPointCM = LastRegexDouble(text, "Cm\s*=\s*(-?[\d.]+)")
        ' "CD" only (not "CDp"/"CDf") - the trailing letter in those means \s*= never
        ' matches right after "CD", so this is safely just the total drag coefficient.
        _lastPointCD = LastRegexDouble(text, "CD\s*=\s*(-?[\d.]+)")
    End Sub

    Private Function LastRegexDouble(text As String, pattern As String) As Double?
        Dim matches = Regex.Matches(text, pattern)
        If matches.Count = 0 Then Return Nothing
        Dim v As Double
        If Double.TryParse(matches(matches.Count - 1).Groups(1).Value, NumberStyles.Float, CultureInfo.InvariantCulture, v) Then
            Return v
        End If
        Return Nothing
    End Function

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
    ''' XFOIL's CPWR output (verified against the real xfoil.exe): a title line, an
    ''' "Alfa = ... Re = ..." line, a "# x y Cp" column header, then rows of
    ''' "x  y  Cp" (3 columns). Take the first token as x and the last as Cp so this
    ''' still degrades gracefully on older/inviscid-only XFOIL builds that write just
    ''' "x  Cp" (2 columns). Preserve file order (traces the airfoil surface as a
    ''' loop: TE -> upper -> LE -> lower -> TE) rather than sorting by x.
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
    ''' XFOIL's SAVE command (verified against the real xfoil.exe): a title line (the
    ''' airfoil name) followed by plain "x y" coordinate rows tracing the airfoil
    ''' surface as a loop (TE -> upper -> LE -> lower -> TE). Same lenient "any line
    ''' with 2+ numeric tokens" parse as ParseCpFile, preserving file order.
    ''' </summary>
    Private Function ParseGeomFile(path As String) As List(Of XfoilGeomPoint)
        Dim result As New List(Of XfoilGeomPoint)
        For Each line In File.ReadAllLines(path)
            If String.IsNullOrWhiteSpace(line) Then Continue For
            Dim toks = line.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
            If toks.Length < 2 Then Continue For
            Dim x As Double, y As Double
            If Double.TryParse(toks(0), NumberStyles.Float, CultureInfo.InvariantCulture, x) AndAlso
               Double.TryParse(toks(1), NumberStyles.Float, CultureInfo.InvariantCulture, y) Then
                result.Add(New XfoilGeomPoint With {.X = x, .Y = y})
            End If
        Next
        Return result
    End Function

    ''' <summary>
    ''' XFOIL's boundary-layer "dump" file (verified against the real xfoil.exe): a
    ''' "#" header row followed by exactly 8 columns per row - s, x, y, Ue/Vinf, Dstar,
    ''' Theta, Cf, H (no amplification/Ctau column in this XFOIL 6.99 build; some other
    ''' versions reportedly add a 9th/10th column, hence still bounds-checking rather
    ''' than assuming exactly 8). Rows are split into upper vs lower surface by the sign
    ''' of y - a safe heuristic since airfoil coordinates are referenced to the chord
    ''' line (y >= 0 is the upper surface) - rather than relying on the exact blank-line
    ''' block boundaries XFOIL uses between the two sides.
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

            Dim pt As New XfoilBLPoint With {
                .X = vals(1), .Y = vals(2), .Ue = vals(3), .Dstar = vals(4), .Theta = vals(5), .Cf = vals(6), .H = vals(7)
            }
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

    ''' <summary>
    ''' Draws the airfoil-name + Re/alpha/CL/CM/CD/L-over-D/Ncrit parameter block shared
    ''' by the Cp and BL plots, matching XFOIL's own CPX view exactly (including the
    ''' "×10⁶" Re exponent and the C_L/C_M/C_D/N_cr subscripts). Values come from the
    ''' last completed single-point run (_lastPoint... fields); any not yet available
    ''' show "--".
    ''' </summary>
    Private Sub DrawParamBlock(g As SvgGraphics, titleFont As Font, paramFont As Font, subFont As Font,
                                brush As SolidBrush, paramX As Single, paramY As Single)
        Dim airfoilName = txtAirfoil.Text.Trim()
        If Regex.IsMatch(airfoilName, "^\d{4,5}$") Then airfoilName = "NACA " & airfoilName
        g.DrawString(airfoilName.ToUpperInvariant(), titleFont, brush, New PointF(paramX, paramY))

        Dim lineY = paramY + 26
        Dim DrawValue = Sub(value As String)
                             g.DrawString("=", paramFont, brush, New PointF(paramX + 50, lineY))
                             g.DrawString(value, paramFont, brush, New PointF(paramX + 70, lineY))
                             lineY += 19
                         End Sub
        Dim DrawSub = Sub(mainText As String, subText As String)
                          g.DrawString(mainText, paramFont, brush, New PointF(paramX, lineY))
                          Dim tw = g.MeasureString(mainText, paramFont).Width
                          g.DrawString(subText, subFont, brush, New PointF(paramX + tw - 1, lineY + 6))
                      End Sub

        ' Re, with a superscript "×10⁶" exponent.
        g.DrawString("Re", paramFont, brush, New PointF(paramX, lineY))
        If _lastPointRe.HasValue AndAlso _lastPointRe.Value > 0 Then
            g.DrawString("=", paramFont, brush, New PointF(paramX + 50, lineY))
            Dim reMain = (_lastPointRe.Value / 1000000.0).ToString("0.000", CultureInfo.InvariantCulture) & "×10"
            g.DrawString(reMain, paramFont, brush, New PointF(paramX + 70, lineY))
            Dim reW = g.MeasureString(reMain, paramFont).Width
            g.DrawString("6", subFont, brush, New PointF(paramX + 70 + reW - 1, lineY - 3))
            lineY += 19
        Else
            DrawValue("inviscid")
        End If

        ' alpha
        g.DrawString("α", paramFont, brush, New PointF(paramX, lineY))
        DrawValue(If(_lastPointAlpha.HasValue, _lastPointAlpha.Value.ToString("0.0000", CultureInfo.InvariantCulture) & "°", "--"))

        ' CL
        DrawSub("C", "L")
        DrawValue(If(_lastPointCL.HasValue, _lastPointCL.Value.ToString("0.0000", CultureInfo.InvariantCulture), "--"))

        ' CM
        DrawSub("C", "M")
        DrawValue(If(_lastPointCM.HasValue, _lastPointCM.Value.ToString("0.0000", CultureInfo.InvariantCulture), "--"))

        ' CD
        DrawSub("C", "D")
        DrawValue(If(_lastPointCD.HasValue, _lastPointCD.Value.ToString("0.00000", CultureInfo.InvariantCulture), "--"))

        ' L/D
        g.DrawString("L/D", paramFont, brush, New PointF(paramX, lineY))
        Dim ldText = "--"
        If _lastPointCL.HasValue AndAlso _lastPointCD.HasValue AndAlso _lastPointCD.Value <> 0 Then
            ldText = (_lastPointCL.Value / _lastPointCD.Value).ToString("0.00", CultureInfo.InvariantCulture)
        End If
        DrawValue(ldText)

        ' Ncrit (only meaningful for a viscous run)
        DrawSub("N", "cr")
        DrawValue(If(_lastPointNcrit.HasValue, _lastPointNcrit.Value.ToString("0.00", CultureInfo.InvariantCulture), "--"))
    End Sub

    ''' <summary>
    ''' Index of the point with the smallest X (the leading edge) in a list that traces
    ''' the airfoil surface as a loop (TE -> upper -> LE -> lower -> TE) - splitting a
    ''' polyline at this index separates the upper-surface half from the lower-surface
    ''' half so each can be colored differently.
    ''' </summary>
    Private Function IndexOfMinX(points As List(Of XfoilCpPoint)) As Integer
        Dim idx = 0
        Dim minX As Double = Double.MaxValue
        For i = 0 To points.Count - 1
            If points(i).X < minX Then minX = points(i).X : idx = i
        Next
        Return idx
    End Function

    Private Function IndexOfMinX(points As List(Of XfoilGeomPoint)) As Integer
        Dim idx = 0
        Dim minX As Double = Double.MaxValue
        For i = 0 To points.Count - 1
            If points(i).X < minX Then minX = points(i).X : idx = i
        Next
        Return idx
    End Function

    Private Sub RenderPolarPlot(Optional captureVectors As Boolean = False)
        If pPolar Is Nothing OrElse pPolar.Width <= 0 OrElse pPolar.Height <= 0 Then Return
        Dim w = pPolar.Width : Dim h = pPolar.Height
        Dim bmp As New Bitmap(w, h)
        Dim tickFont = MakeTickFont() : Dim axisFont = MakeAxisFont()
        Dim svgOut As String = "" : Dim pdfOut As String = ""

        Dim hasVisibleData = False
        For Each r In _polarRuns
            If r.Visible AndAlso r.Points.Count > 0 Then hasVisibleData = True : Exit For
        Next

        Using g As New SvgGraphics(w, h, Graphics.FromImage(bmp), captureVectors)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.Clear(ThemeBackColor)
            ApplyPlotZoomTransform(g, pPolar, captureVectors)

            If Not hasVisibleData Then
                g.DrawString("Set airfoil + alpha range and click ""Run Polar Sweep"".", tickFont, Brushes.Gray, New PointF(10, 10))
            Else
                Dim marginX As Single = 55
                Dim marginY As Single = 35
                Dim cellW As Single = (w - marginX * 3) / 2
                Dim cellH As Single = (h - marginY * 3) / 2

                DrawSubplot(g, tickFont, axisFont, marginX, marginY, cellW, cellH,
                            "CL vs alpha", _polarRuns, Function(pt) pt.Alpha, Function(pt) pt.CL, showLegend:=True)
                DrawSubplot(g, tickFont, axisFont, marginX * 2 + cellW, marginY, cellW, cellH,
                            "CD vs alpha", _polarRuns, Function(pt) pt.Alpha, Function(pt) pt.CD)
                DrawSubplot(g, tickFont, axisFont, marginX, marginY * 2 + cellH, cellW, cellH,
                            "Cm vs alpha", _polarRuns, Function(pt) pt.Alpha, Function(pt) pt.CM)
                DrawSubplot(g, tickFont, axisFont, marginX * 2 + cellW, marginY * 2 + cellH, cellW, cellH,
                            "CL vs CD (drag polar)", _polarRuns, Function(pt) pt.CD, Function(pt) pt.CL)
            End If

            If captureVectors Then
                svgOut = g.GetSvgContent()
                pdfOut = g.GetPdfContentStream()
            End If
        End Using

        SetPlotBitmap(pPolar, bmp)
        If captureVectors Then _polarSvg = svgOut : _polarPdf = pdfOut
    End Sub

    ''' <summary>
    ''' Draws one subplot for every visible run in the shared axis range so multiple
    ''' polar sweeps can be overlaid/compared, each in its own color (XfoilPolarRun.Color),
    ''' with an optional small color-swatch legend when more than one run is visible.
    ''' </summary>
    Private Sub DrawSubplot(g As SvgGraphics, tickFont As Font, axisFont As Font,
                             x As Single, y As Single, w As Single, h As Single,
                             title As String, runs As List(Of XfoilPolarRun),
                             xOf As Func(Of XfoilPolarPoint, Double), yOf As Func(Of XfoilPolarPoint, Double),
                             Optional showLegend As Boolean = False)
        Dim visibleRuns As New List(Of XfoilPolarRun)
        For Each r In runs
            If r.Visible AndAlso r.Points.Count > 0 Then visibleRuns.Add(r)
        Next
        If visibleRuns.Count = 0 Then Return

        Dim xMin As Double = Double.MaxValue, xMax As Double = Double.MinValue
        Dim yMin As Double = Double.MaxValue, yMax As Double = Double.MinValue
        For Each r In visibleRuns
            For Each pt In r.Points
                Dim xv = xOf(pt) : Dim yv = yOf(pt)
                xMin = Math.Min(xMin, xv) : xMax = Math.Max(xMax, xv)
                yMin = Math.Min(yMin, yv) : yMax = Math.Max(yMax, yv)
            Next
        Next
        If xMin = xMax Then xMin -= 1 : xMax += 1
        If yMin = yMax Then yMin -= 1 : yMax += 1
        Dim yPad = (yMax - yMin) * 0.1
        yMin -= yPad
        yMax += yPad

        Dim axisPen As New Pen(ThemeForeColor, 1)
        Dim gridPen As New Pen(ThemeGridColor, 1) With {.DashStyle = DashStyle.Dash}
        Dim fgBrush As New SolidBrush(ThemeForeColor)

        g.DrawRectangle(axisPen, x, y, w, h)
        g.DrawString(title, axisFont, fgBrush, New PointF(x, y - axisFont.Height - 2))

        If yMin < 0 AndAlso yMax > 0 Then
            Dim zeroY As Single = CSng(y + h - (0 - yMin) / (yMax - yMin) * h)
            g.DrawLine(gridPen, x, zeroY, x + w, zeroY)
        End If

        For Each r In visibleRuns
            Dim ordered As New List(Of XfoilPolarPoint)(r.Points)
            ordered.Sort(Function(a, b) a.Alpha.CompareTo(b.Alpha))

            Dim pen As New Pen(r.Color, 1.5F)
            Dim markerBrush As New SolidBrush(r.Color)
            Dim pts As New List(Of PointF)
            For Each pt In ordered
                Dim px As Single = CSng(x + (xOf(pt) - xMin) / (xMax - xMin) * w)
                Dim py As Single = CSng(y + h - (yOf(pt) - yMin) / (yMax - yMin) * h)
                pts.Add(New PointF(px, py))
            Next
            If pts.Count >= 2 Then g.DrawLines(pen, pts.ToArray())
            For Each pt In pts
                g.FillEllipse(markerBrush, pt.X - 2, pt.Y - 2, 4, 4)
            Next
        Next

        g.DrawString(xMin.ToString("0.00"), tickFont, fgBrush, New PointF(x, y + h + 2))
        Dim maxLabel = xMax.ToString("0.00")
        Dim maxLabelSize = g.MeasureString(maxLabel, tickFont)
        g.DrawString(maxLabel, tickFont, fgBrush, New PointF(x + w - maxLabelSize.Width, y + h + 2))

        Dim yMaxLabel = yMax.ToString("0.0000")
        Dim yMaxLabelSize = g.MeasureString(yMaxLabel, tickFont)
        g.DrawString(yMaxLabel, tickFont, fgBrush, New PointF(x - yMaxLabelSize.Width - 2, y))
        Dim yMinLabel = yMin.ToString("0.0000")
        Dim yMinLabelSize = g.MeasureString(yMinLabel, tickFont)
        g.DrawString(yMinLabel, tickFont, fgBrush, New PointF(x - yMinLabelSize.Width - 2, y + h - yMinLabelSize.Height))

        If showLegend AndAlso visibleRuns.Count > 1 Then
            Dim ly As Single = y + 4
            For Each r In visibleRuns
                g.FillRectangle(New SolidBrush(r.Color), x + 4, ly, 10, 10)
                g.DrawString(r.Label, tickFont, fgBrush, New PointF(x + 18, ly - 1))
                ly += 14
            Next
        End If
    End Sub

    ''' <summary>
    ''' Matches XFOIL's own native CPX plot exactly (confirmed against a screenshot of the
    ''' real thing): black background, white content, no border rectangle - just a single
    ''' Y axis with ticks every 0.5 and a horizontal Cp=0 line with small unlabeled ticks
    ''' (XFOIL doesn't label the x-axis at all). The Cp axis is a fixed -2.0..1.0 range,
    ''' widened outward in 0.5 steps only if the data actually exceeds it, rather than
    ''' auto-fitting tightly to the data the way the other plots here do. "XFOIL" is
    ''' tagged top-left and a parameter block (airfoil name, alpha, CL, CM, CDp) sits
    ''' top-right, same as the reference. The airfoil shape is drawn in its own band
    ''' below the Cp curve (not overlaid on the same axis) with a matching x mapping so
    ''' leading/trailing edge line up vertically with the curve above it.
    ''' </summary>
    Private Sub RenderCpPlot(Optional captureVectors As Boolean = False)
        If pCp Is Nothing OrElse pCp.Width <= 0 OrElse pCp.Height <= 0 Then Return
        Dim w = pCp.Width : Dim h = pCp.Height
        Dim bmp As New Bitmap(w, h)
        Dim tickFont = MakeTickFont()
        Dim tagFont = New Font("Consolas", 8.0F)
        Dim paramFont = New Font("Consolas", 9.5F)
        Dim titleFont = New Font("Consolas", 11.0F, FontStyle.Bold)
        Dim cpLabelFont = New Font("Consolas", 15.0F)
        Dim cpLabelSubFont = New Font("Consolas", 10.0F)
        Dim svgOut As String = "" : Dim pdfOut As String = ""
        Dim whiteBrush As New SolidBrush(ThemeForeColor)
        Dim whitePen As New Pen(ThemeForeColor, 1)
        Dim upperPen As New Pen(ThemeUpperColor, 1.5F)
        Dim lowerPen As New Pen(ThemeLowerColor, 1.5F)

        Using g As New SvgGraphics(w, h, Graphics.FromImage(bmp), captureVectors)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.Clear(ThemeBackColor)
            ApplyPlotZoomTransform(g, pCp, captureVectors)

            If _cpPoints.Count = 0 Then
                g.DrawString("Set a single alpha and click ""Run Point Analysis"" to compute the Cp distribution.", tickFont, whiteBrush, New PointF(10, 10))
            Else
                Dim marginX As Single = 65
                Dim topY As Single = 40
                Dim airfoilBandH As Single = 90
                Dim plotW As Single = w - marginX - 20
                Dim plotBottomY As Single = h - airfoilBandH

                Dim xMin As Double = Double.MaxValue, xMax As Double = Double.MinValue
                Dim cpMin As Double = Double.MaxValue, cpMax As Double = Double.MinValue
                For Each pt In _cpPoints
                    xMin = Math.Min(xMin, pt.X) : xMax = Math.Max(xMax, pt.X)
                    cpMin = Math.Min(cpMin, pt.Cp) : cpMax = Math.Max(cpMax, pt.Cp)
                Next
                If xMin = xMax Then xMin -= 1 : xMax += 1

                ' XFOIL's default Cp axis is a fixed -2.0..1.0 range, only widened (in 0.5
                ' steps) if the data doesn't fit - not auto-fit tightly like the other plots.
                Dim cpAxisMin As Double = -2.0
                Dim cpAxisMax As Double = 1.0
                Do While cpMin < cpAxisMin
                    cpAxisMin -= 0.5
                Loop
                Do While cpMax > cpAxisMax
                    cpAxisMax += 0.5
                Loop

                Dim mapX = Function(xv As Double) As Single
                               Return CSng(marginX + (xv - xMin) / (xMax - xMin) * plotW)
                           End Function
                Dim mapY = Function(cpv As Double) As Single
                               Return CSng(topY + (cpv - cpAxisMin) / (cpAxisMax - cpAxisMin) * (plotBottomY - topY))
                           End Function

                ' Y axis: single vertical line with ticks/labels every 0.5, no border box.
                g.DrawLine(whitePen, marginX, topY, marginX, plotBottomY)
                Dim cpTick = cpAxisMin
                Do While cpTick <= cpAxisMax + 0.001
                    Dim ty = mapY(cpTick)
                    g.DrawLine(whitePen, marginX - 4, ty, marginX, ty)
                    ' Guard against "-0.0" from floating-point drift landing just below zero.
                    Dim tickValue = If(Math.Abs(cpTick) < 0.001, 0.0, cpTick)
                    Dim lbl = tickValue.ToString("0.0", CultureInfo.InvariantCulture)
                    Dim lblSize = g.MeasureString(lbl, tickFont)
                    g.DrawString(lbl, tickFont, whiteBrush, New PointF(marginX - 8 - lblSize.Width, ty - lblSize.Height / 2))
                    cpTick += 0.5
                Loop

                ' Horizontal Cp=0 line spans the full plot width with small unlabeled ticks
                ' every 0.1 x/c - XFOIL doesn't label the x-axis at all.
                Dim zeroY = mapY(0.0)
                g.DrawLine(whitePen, marginX, zeroY, marginX + plotW, zeroY)
                For i = 0 To 10
                    Dim tx = mapX(xMin + (xMax - xMin) * i / 10.0)
                    g.DrawLine(whitePen, tx, zeroY - 3, tx, zeroY + 3)
                Next

                ' "Cp" axis label, left of the axis, vertically centered - "C" full size
                ' with a smaller subscript "p" offset down-right, same as the reference.
                ' Positioned in its own column further left than the tick labels so the
                ' two never collide regardless of how wide the widest tick label is.
                Dim cpLabelY = (topY + plotBottomY) / 2.0F - 10
                g.DrawString("C", cpLabelFont, whiteBrush, New PointF(6, cpLabelY))
                g.DrawString("p", cpLabelSubFont, whiteBrush, New PointF(6 + g.MeasureString("C", cpLabelFont).Width - 3, cpLabelY + 15))

                ' "XFOIL"/version tag, top-left corner of the axis.
                g.DrawString("XFOIL", tagFont, whiteBrush, New PointF(marginX + 2, topY - 32))
                g.DrawString("V 6.99", tagFont, whiteBrush, New PointF(marginX + 2, topY - 20))

                ' Parameter block, top-right: airfoil name + Re/alpha/CL/CM/CD/L/D/Ncrit,
                ' matching XFOIL's own CPX view.
                DrawParamBlock(g, titleFont, paramFont, tagFont, whiteBrush, w - 190, 12)

                ' Transition markers (from the last point-analysis run's console echo).
                If _lastTopXtr.HasValue OrElse _lastBotXtr.HasValue Then
                    Dim trPen As New Pen(Color.Gray, 1) With {.DashStyle = DashStyle.Dot}
                    If _lastTopXtr.HasValue Then
                        Dim tx = mapX(_lastTopXtr.Value)
                        g.DrawLine(trPen, tx, topY, tx, plotBottomY)
                    End If
                    If _lastBotXtr.HasValue Then
                        Dim bx = mapX(_lastBotXtr.Value)
                        g.DrawLine(trPen, bx, topY, bx, plotBottomY)
                    End If
                End If

                ' Cp curve, inverted: cpAxisMax (most positive) maps to the BOTTOM. Points
                ' trace the airfoil surface as a loop (TE -> upper -> LE -> lower -> TE);
                ' split at the index of minimum x (the leading edge) so the upper and lower
                ' surface halves can be colored separately.
                Dim cpSplitIdx = IndexOfMinX(_cpPoints)
                Dim cpUpperPts As New List(Of PointF)
                For i = 0 To cpSplitIdx
                    cpUpperPts.Add(New PointF(mapX(_cpPoints(i).X), mapY(_cpPoints(i).Cp)))
                Next
                If cpUpperPts.Count >= 2 Then g.DrawLines(upperPen, cpUpperPts.ToArray())
                Dim cpLowerPts As New List(Of PointF)
                For i = cpSplitIdx To _cpPoints.Count - 1
                    cpLowerPts.Add(New PointF(mapX(_cpPoints(i).X), mapY(_cpPoints(i).Cp)))
                Next
                If cpLowerPts.Count >= 2 Then g.DrawLines(lowerPen, cpLowerPts.ToArray())

                g.DrawString("upper", tickFont, New SolidBrush(ThemeUpperColor), New PointF(w - 90, topY - 32))
                g.DrawString("lower", tickFont, New SolidBrush(ThemeLowerColor), New PointF(w - 45, topY - 32))

                ' Airfoil shape, drawn in its own band below the Cp curve (not overlaid on
                ' it) with the same x mapping so LE/TE line up with the curve above, split
                ' upper/lower the same way as the Cp curve.
                If _airfoilCoords.Count >= 3 Then
                    Dim gXMin As Double = Double.MaxValue, gXMax As Double = Double.MinValue
                    Dim gYMin As Double = Double.MaxValue, gYMax As Double = Double.MinValue
                    For Each pt In _airfoilCoords
                        gXMin = Math.Min(gXMin, pt.X) : gXMax = Math.Max(gXMax, pt.X)
                        gYMin = Math.Min(gYMin, pt.Y) : gYMax = Math.Max(gYMax, pt.Y)
                    Next
                    If gYMin = gYMax Then gYMin -= 0.05 : gYMax += 0.05
                    Dim bandTop = plotBottomY + 15
                    Dim bandH = airfoilBandH - 25
                    Dim bandScale = Math.Min(plotW / (gXMax - gXMin), bandH / (gYMax - gYMin)) * 0.95
                    Dim shapeH = CSng((gYMax - gYMin) * bandScale)
                    Dim bandOriginY = bandTop + (bandH - shapeH) / 2.0F

                    Dim mapGeom = Function(pt As XfoilGeomPoint) As PointF
                                      Dim gx = CSng(mapX(gXMin) + (pt.X - gXMin) * bandScale)
                                      Dim gy = CSng(bandOriginY + shapeH - (pt.Y - gYMin) * bandScale)
                                      Return New PointF(gx, gy)
                                  End Function

                    Dim geomSplitIdx = IndexOfMinX(_airfoilCoords)
                    Dim geomUpperPts As New List(Of PointF)
                    For i = 0 To geomSplitIdx
                        geomUpperPts.Add(mapGeom(_airfoilCoords(i)))
                    Next
                    If geomUpperPts.Count >= 2 Then g.DrawLines(upperPen, geomUpperPts.ToArray())
                    Dim geomLowerPts As New List(Of PointF)
                    For i = geomSplitIdx To _airfoilCoords.Count - 1
                        geomLowerPts.Add(mapGeom(_airfoilCoords(i)))
                    Next
                    ' Close the loop back to the first (TE) point.
                    If geomUpperPts.Count > 0 Then geomLowerPts.Add(geomUpperPts(0))
                    If geomLowerPts.Count >= 2 Then g.DrawLines(lowerPen, geomLowerPts.ToArray())

                    ' Boundary-layer edge, drawn as a dashed line standing off the surface by
                    ' the local displacement thickness (Dstar) - the same overlay the real
                    ' xfoil.exe's own CPX view shows on its airfoil band.
                    If _blTop.Count >= 2 Then
                        Dim edgePts = ComputeBlEdge(_blTop).ConvertAll(Function(pt) mapGeom(pt))
                        g.DrawLines(New Pen(ThemeUpperColor, 1.0F) With {.DashStyle = DashStyle.Dash}, edgePts.ToArray())
                    End If
                    If _blBottom.Count >= 2 Then
                        Dim edgePts = ComputeBlEdge(_blBottom).ConvertAll(Function(pt) mapGeom(pt))
                        g.DrawLines(New Pen(ThemeLowerColor, 1.0F) With {.DashStyle = DashStyle.Dash}, edgePts.ToArray())
                    End If
                End If
            End If

            If captureVectors Then
                svgOut = g.GetSvgContent()
                pdfOut = g.GetPdfContentStream()
            End If
        End Using

        SetPlotBitmap(pCp, bmp)
        If captureVectors Then _cpSvg = svgOut : _cpPdf = pdfOut
    End Sub

    ''' <summary>
    ''' Offsets each boundary-layer point outward from the airfoil surface, along the
    ''' local surface normal, by its displacement thickness (Dstar) - producing the
    ''' "boundary-layer edge" curve XFOIL overlays on its own airfoil-band view. Points
    ''' are re-sorted by x/c first since the BL dump's row order isn't guaranteed to be
    ''' monotonic in x for either surface.
    ''' </summary>
    Private Function ComputeBlEdge(points As List(Of XfoilBLPoint)) As List(Of XfoilGeomPoint)
        Dim ordered As New List(Of XfoilBLPoint)(points)
        ordered.Sort(Function(a, b) a.X.CompareTo(b.X))

        Dim result As New List(Of XfoilGeomPoint)
        For i = 0 To ordered.Count - 1
            Dim tX As Double, tY As Double
            If ordered.Count = 1 Then
                tX = 1 : tY = 0
            ElseIf i = 0 Then
                tX = ordered(1).X - ordered(0).X : tY = ordered(1).Y - ordered(0).Y
            ElseIf i = ordered.Count - 1 Then
                tX = ordered(i).X - ordered(i - 1).X : tY = ordered(i).Y - ordered(i - 1).Y
            Else
                tX = ordered(i + 1).X - ordered(i - 1).X : tY = ordered(i + 1).Y - ordered(i - 1).Y
            End If
            Dim tLen = Math.Sqrt(tX * tX + tY * tY)
            If tLen < 0.0000001 Then tLen = 1
            Dim nx = -tY / tLen
            Dim ny = tX / tLen
            ' Orient the normal outward from the chord line: up for the upper surface,
            ' down for the lower.
            If (ordered(i).Y >= 0 AndAlso ny < 0) OrElse (ordered(i).Y < 0 AndAlso ny > 0) Then
                nx = -nx : ny = -ny
            End If
            result.Add(New XfoilGeomPoint With {
                .X = ordered(i).X + nx * ordered(i).Dstar,
                .Y = ordered(i).Y + ny * ordered(i).Dstar
            })
        Next
        Return result
    End Function

    ''' <summary>
    ''' Rounds a data range up to a "nice" axis tick step (1/2/5 x 10^n) targeting
    ''' roughly targetTicks gridlines - the standard nice-numbers approach used by most
    ''' charting libraries, so axis labels land on round values like 0.2/0.5/1.0 rather
    ''' than whatever the raw data range happens to be.
    ''' </summary>
    Private Function NiceStep(range As Double, targetTicks As Integer) As Double
        If range <= 0 Then Return 1
        Dim rawStep = range / targetTicks
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

    ''' <summary>
    ''' Draws the two-row parameter block spanning the full plot width at the top of the
    ''' BL plot, matching XFOIL's own VPLO output exactly: Ma/alpha/CL/top-transition on
    ''' row 1, Re/Ncrit/CD/bottom-transition on row 2, with the top-surface transition
    ''' tagged "T:" in yellow and the bottom-surface one tagged "B:" in cyan (XFOIL's own
    ''' convention for distinguishing the two sides).
    ''' </summary>
    Private Sub DrawBlParamBlock(g As SvgGraphics, titleFont As Font, paramFont As Font, subFont As Font,
                                  brush As SolidBrush, w As Integer)
        Dim airfoilName = txtAirfoil.Text.Trim()
        If Regex.IsMatch(airfoilName, "^\d{4,5}$") Then airfoilName = "NACA " & airfoilName
        g.DrawString(airfoilName.ToUpperInvariant(), titleFont, brush, New PointF(60, 6))

        Dim col1 As Single = 60
        Dim col2 As Single = w * 0.30F
        Dim col3 As Single = w * 0.50F
        Dim col4 As Single = w * 0.70F
        Dim row1Y As Single = 36
        Dim row2Y As Single = 62

        Dim DrawSub2 = Sub(mainText As String, subText As String, x As Single, y As Single, br As SolidBrush)
                           g.DrawString(mainText, paramFont, br, New PointF(x, y))
                           Dim tw = g.MeasureString(mainText, paramFont).Width
                           g.DrawString(subText, subFont, br, New PointF(x + tw - 1, y + 6))
                       End Sub
        Dim DrawRe = Sub(x As Single, y As Single)
                         g.DrawString("Re", paramFont, brush, New PointF(x, y))
                         g.DrawString("=", paramFont, brush, New PointF(x + 45, y))
                         If _lastPointRe.HasValue AndAlso _lastPointRe.Value > 0 Then
                             Dim reMain = (_lastPointRe.Value / 1000000.0).ToString("0.000", CultureInfo.InvariantCulture) & "×10"
                             g.DrawString(reMain, paramFont, brush, New PointF(x + 65, y))
                             Dim reW = g.MeasureString(reMain, paramFont).Width
                             g.DrawString("6", subFont, brush, New PointF(x + 65 + reW - 1, y - 3))
                         Else
                             g.DrawString("inviscid", paramFont, brush, New PointF(x + 65, y))
                         End If
                     End Sub
        Dim DrawXtr = Sub(tag As String, value As Double?, x As Single, y As Single, color As Color)
                          Dim br As New SolidBrush(color)
                          g.DrawString(tag, paramFont, br, New PointF(x, y))
                          g.DrawString("x", paramFont, br, New PointF(x + 24, y))
                          g.DrawString("tr", subFont, br, New PointF(x + 24 + g.MeasureString("x", paramFont).Width - 1, y + 6))
                          g.DrawString("/c =", paramFont, br, New PointF(x + 47, y))
                          g.DrawString(If(value.HasValue, value.Value.ToString("0.0000", CultureInfo.InvariantCulture), "--"), paramFont, br, New PointF(x + 98, y))
                      End Sub

        ' Row 1: Ma, alpha, CL, top-surface transition (yellow).
        g.DrawString("Ma", paramFont, brush, New PointF(col1, row1Y))
        g.DrawString("=", paramFont, brush, New PointF(col1 + 45, row1Y))
        g.DrawString(If(_lastPointMach.HasValue, _lastPointMach.Value.ToString("0.0000", CultureInfo.InvariantCulture), "--"), paramFont, brush, New PointF(col1 + 65, row1Y))

        g.DrawString("α", paramFont, brush, New PointF(col2, row1Y))
        g.DrawString("=", paramFont, brush, New PointF(col2 + 40, row1Y))
        g.DrawString(If(_lastPointAlpha.HasValue, _lastPointAlpha.Value.ToString("0.0000", CultureInfo.InvariantCulture) & "°", "--"), paramFont, brush, New PointF(col2 + 60, row1Y))

        DrawSub2("C", "L", col3, row1Y, brush)
        g.DrawString("=", paramFont, brush, New PointF(col3 + 30, row1Y))
        g.DrawString(If(_lastPointCL.HasValue, _lastPointCL.Value.ToString("0.0000", CultureInfo.InvariantCulture), "--"), paramFont, brush, New PointF(col3 + 50, row1Y))

        DrawXtr("T:", _lastTopXtr, col4, row1Y, Color.Yellow)

        ' Row 2: Re, Ncrit, CD, bottom-surface transition (cyan).
        DrawRe(col1, row2Y)

        DrawSub2("N", "cr", col2, row2Y, brush)
        g.DrawString("=", paramFont, brush, New PointF(col2 + 40, row2Y))
        g.DrawString(If(_lastPointNcrit.HasValue, _lastPointNcrit.Value.ToString("0.00", CultureInfo.InvariantCulture), "--"), paramFont, brush, New PointF(col2 + 60, row2Y))

        DrawSub2("C", "D", col3, row2Y, brush)
        g.DrawString("=", paramFont, brush, New PointF(col3 + 30, row2Y))
        g.DrawString(If(_lastPointCD.HasValue, _lastPointCD.Value.ToString("0.00000", CultureInfo.InvariantCulture), "--"), paramFont, brush, New PointF(col3 + 50, row2Y))

        DrawXtr("B:", _lastBotXtr, col4, row2Y, Color.Cyan)
    End Sub

    ''' <summary>
    ''' Returns the requested single-quantity BL value for one point; index is aligned
    ''' with _blQuantityNames / _blQuantityAxisMain / _blQuantityAxisSub and with
    ''' cmbBlQuantity.SelectedIndex. Not valid for the DT/DB dual-quantity indices
    ''' (BlQuantityIndexDT/DB) - RenderBlPlot handles those separately.
    ''' Re_theta (RT/RTL) is computed rather than read directly: XFOIL's dump file has no
    ''' Re_theta column, but Re_theta = Re_chord * (Ue/Vinf) * (Theta/c) - the same
    ''' relation XFOIL itself uses internally - and all three factors on the right ARE in
    ''' the dump, so this reconstructs it exactly rather than approximating it.
    ''' </summary>
    Private Function BlQuantityValue(index As Integer, pt As XfoilBLPoint) As Double
        Select Case index
            Case 0 : Return pt.H
            Case 1 : Return pt.Ue
            Case 2 : Return pt.Cf
            Case 5 : Return If(_lastPointRe.HasValue, _lastPointRe.Value, 0.0) * pt.Ue * pt.Theta
            Case 6
                Dim reTheta = If(_lastPointRe.HasValue, _lastPointRe.Value, 0.0) * pt.Ue * pt.Theta
                Return If(reTheta > 0, Math.Log10(reTheta), 0.0)
            Case Else : Return 0.0
        End Select
    End Function

    ''' <summary>
    ''' Renders one boundary-layer quantity at a time (whichever is selected in
    ''' cmbBlQuantity), full-size, styled to match the real xfoil.exe's own VPLO output
    ''' exactly: a bordered grid with dashed gridlines and labeled ticks on both axes (the
    ''' x-axis runs past x/c = 1 into the wake, since the BL dump includes wake stations),
    ''' a two-row Ma/alpha/CL/Re/Ncrit/CD/transition parameter block spanning the full
    ''' width, and colored upper/lower series.
    ''' </summary>
    Private Sub RenderBlPlot(Optional captureVectors As Boolean = False)
        If pBl Is Nothing OrElse pBl.Width <= 0 OrElse pBl.Height <= 0 Then Return
        Dim w = pBl.Width : Dim h = pBl.Height
        Dim bmp As New Bitmap(w, h)
        Dim tickFont = MakeTickFont()
        Dim tagFont = New Font("Consolas", 8.0F)
        Dim paramFont = New Font("Consolas", 9.5F)
        Dim titleFont = New Font("Consolas", 11.0F, FontStyle.Bold)
        Dim yLabelFont = New Font("Consolas", 15.0F)
        Dim yLabelSubFont = New Font("Consolas", 10.0F)
        Dim svgOut As String = "" : Dim pdfOut As String = ""
        Dim whiteBrush As New SolidBrush(ThemeForeColor)
        Dim whitePen As New Pen(ThemeForeColor, 1)
        Dim gridPen As New Pen(ThemeGridColor, 1) With {.DashStyle = DashStyle.Dash}

        Dim qIndex = If(cmbBlQuantity IsNot Nothing, cmbBlQuantity.SelectedIndex, 0)
        If qIndex < 0 Then qIndex = 0

        ' DT/DB are XFOIL's own combined Dstar+Theta-vs-x plot for a SINGLE surface -
        ' everything else here is a single quantity plotted across both surfaces.
        Dim isDual = (qIndex = BlQuantityIndexDT OrElse qIndex = BlQuantityIndexDB)
        Dim dualPts = If(qIndex = BlQuantityIndexDT, _blTop, _blBottom)

        Using g As New SvgGraphics(w, h, Graphics.FromImage(bmp), captureVectors)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.Clear(ThemeBackColor)
            ApplyPlotZoomTransform(g, pBl, captureVectors)

            Dim allPts As New List(Of XfoilBLPoint)
            If isDual Then
                allPts.AddRange(dualPts)
            Else
                allPts.AddRange(_blTop)
                allPts.AddRange(_blBottom)
            End If

            If allPts.Count = 0 Then
                g.DrawString("Run a viscous (Re > 0) point analysis to compute boundary-layer data.", tickFont, whiteBrush, New PointF(10, 10))
            Else
                Dim marginX As Single = 55
                Dim topY As Single = 92
                Dim bottomMargin As Single = 40
                Dim plotW As Single = w - marginX - 15
                Dim plotBottomY As Single = h - bottomMargin

                Dim xDataMin As Double = Double.MaxValue, xDataMax As Double = Double.MinValue
                Dim yDataMin As Double = Double.MaxValue, yDataMax As Double = Double.MinValue
                For Each pt In allPts
                    xDataMin = Math.Min(xDataMin, pt.X) : xDataMax = Math.Max(xDataMax, pt.X)
                    If isDual Then
                        yDataMin = Math.Min(yDataMin, Math.Min(pt.Dstar, pt.Theta))
                        yDataMax = Math.Max(yDataMax, Math.Max(pt.Dstar, pt.Theta))
                    Else
                        Dim yv = BlQuantityValue(qIndex, pt)
                        yDataMin = Math.Min(yDataMin, yv) : yDataMax = Math.Max(yDataMax, yv)
                    End If
                Next
                If xDataMin = xDataMax Then xDataMax += 1
                If yDataMin = yDataMax Then yDataMax += 1

                ' Nice round axis bounds (always including zero as a baseline), so ticks
                ' land on values like 0.2/1.0 instead of the raw data range.
                Dim xStep = NiceStep(xDataMax - Math.Min(0.0, xDataMin), 7)
                Dim xAxisMin = Math.Floor(Math.Min(0.0, xDataMin) / xStep) * xStep
                Dim xAxisMax = Math.Ceiling(xDataMax / xStep) * xStep
                Dim yStep = NiceStep(yDataMax - Math.Min(0.0, yDataMin), 6)
                Dim yAxisMin = Math.Floor(Math.Min(0.0, yDataMin) / yStep) * yStep
                Dim yAxisMax = Math.Ceiling(yDataMax / yStep) * yStep

                Dim mapX = Function(xv As Double) As Single
                               Return CSng(marginX + (xv - xAxisMin) / (xAxisMax - xAxisMin) * plotW)
                           End Function
                Dim mapY = Function(yv As Double) As Single
                               Return CSng(topY + (yAxisMax - yv) / (yAxisMax - yAxisMin) * (plotBottomY - topY))
                           End Function

                ' Dashed gridlines + labeled ticks on both axes, inside a solid border box.
                Dim xt = xAxisMin
                Do While xt <= xAxisMax + xStep * 0.001
                    Dim tx = mapX(xt)
                    g.DrawLine(gridPen, tx, topY, tx, plotBottomY)
                    Dim lbl = xt.ToString("0.0", CultureInfo.InvariantCulture)
                    Dim lblSize = g.MeasureString(lbl, tickFont)
                    g.DrawString(lbl, tickFont, whiteBrush, New PointF(tx - lblSize.Width / 2, plotBottomY + 4))
                    xt += xStep
                Loop
                Dim yt = yAxisMin
                Do While yt <= yAxisMax + yStep * 0.001
                    Dim ty = mapY(yt)
                    g.DrawLine(gridPen, marginX, ty, marginX + plotW, ty)
                    Dim lbl = yt.ToString("0.0", CultureInfo.InvariantCulture)
                    Dim lblSize = g.MeasureString(lbl, tickFont)
                    g.DrawString(lbl, tickFont, whiteBrush, New PointF(marginX - 8 - lblSize.Width, ty - lblSize.Height / 2))
                    yt += yStep
                Loop
                g.DrawRectangle(whitePen, marginX, topY, plotW, plotBottomY - topY)

                ' "X" axis label, bottom-right corner under the last tick.
                g.DrawString("X", tickFont, whiteBrush, New PointF(marginX + plotW - 6, plotBottomY + 20))

                If isDual Then
                    ' No single axis letter fits "Dstar and Theta together" - draw a small
                    ' two-color legend instead, top-left inside the axis box.
                    g.DrawString("d*", MakeAxisFont(), New SolidBrush(Color.Orange), New PointF(marginX + 6, topY + 4))
                    g.DrawString("θ", MakeAxisFont(), New SolidBrush(Color.MediumOrchid), New PointF(marginX + 36, topY + 4))
                Else
                    ' Quantity axis label, left of the axis, vertically centered - main
                    ' character full size with a smaller subscript, same style as the Cp
                    ' plot's "Cp" label (e.g. "H" + subscript "k" for the shape parameter).
                    Dim yLabelY = (topY + plotBottomY) / 2.0F - 10
                    g.DrawString(_blQuantityAxisMain(qIndex), yLabelFont, whiteBrush, New PointF(6, yLabelY))
                    If _blQuantityAxisSub(qIndex) <> "" Then
                        g.DrawString(_blQuantityAxisSub(qIndex), yLabelSubFont, whiteBrush,
                                     New PointF(6 + g.MeasureString(_blQuantityAxisMain(qIndex), yLabelFont).Width - 3, yLabelY + 15))
                    End If
                End If

                ' Two-row parameter block spanning the full width, matching XFOIL's own
                ' VPLO output (airfoil name, Ma/alpha/CL/top-transition, Re/Ncrit/CD/
                ' bottom-transition).
                DrawBlParamBlock(g, titleFont, paramFont, tagFont, whiteBrush, w)

                If isDual Then
                    DrawBlSeries(g, marginX, topY, plotW, plotBottomY - topY, xAxisMin, xAxisMax, yAxisMin, yAxisMax,
                                 dualPts, Function(pt) pt.Dstar, Color.Orange)
                    DrawBlSeries(g, marginX, topY, plotW, plotBottomY - topY, xAxisMin, xAxisMax, yAxisMin, yAxisMax,
                                 dualPts, Function(pt) pt.Theta, Color.MediumOrchid)
                Else
                    ' Top/bottom curves use the same yellow/cyan convention as the T:/B:
                    ' transition labels above (matching the real xfoil.exe's own VPLO colors).
                    DrawBlSeries(g, marginX, topY, plotW, plotBottomY - topY, xAxisMin, xAxisMax, yAxisMin, yAxisMax,
                                 _blTop, Function(pt) BlQuantityValue(qIndex, pt), Color.Yellow)
                    DrawBlSeries(g, marginX, topY, plotW, plotBottomY - topY, xAxisMin, xAxisMax, yAxisMin, yAxisMax,
                                 _blBottom, Function(pt) BlQuantityValue(qIndex, pt), Color.Cyan)
                End If
            End If

            If captureVectors Then
                svgOut = g.GetSvgContent()
                pdfOut = g.GetPdfContentStream()
            End If
        End Using

        SetPlotBitmap(pBl, bmp)
        If captureVectors Then _blSvg = svgOut : _blPdf = pdfOut
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

    ''' <summary>
    ''' Standalone airfoil-shape view: the same _airfoilCoords used for the Cp overlay,
    ''' drawn full-size with an aspect-ratio-correct (equal x/y scale) mapping so the
    ''' airfoil isn't visually stretched.
    ''' </summary>
    Private Sub RenderGeometryPlot(Optional captureVectors As Boolean = False)
        If pGeom Is Nothing OrElse pGeom.Width <= 0 OrElse pGeom.Height <= 0 Then Return
        Dim w = pGeom.Width : Dim h = pGeom.Height
        Dim bmp As New Bitmap(w, h)
        Dim tickFont = MakeTickFont() : Dim axisFont = MakeAxisFont()
        Dim svgOut As String = "" : Dim pdfOut As String = ""

        Using g As New SvgGraphics(w, h, Graphics.FromImage(bmp), captureVectors)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.Clear(ThemeBackColor)
            ApplyPlotZoomTransform(g, pGeom, captureVectors)

            If _airfoilCoords.Count < 3 Then
                g.DrawString("Enter an airfoil and click ""Load Airfoil"" (or run an analysis) to view its shape.", tickFont, Brushes.Gray, New PointF(10, 10))
            Else
                Dim marginX As Single = 20
                Dim marginY As Single = 35
                Dim plotW As Single = w - marginX * 2
                Dim plotH As Single = h - marginY * 2

                Dim xMin As Double = Double.MaxValue, xMax As Double = Double.MinValue
                Dim yMin As Double = Double.MaxValue, yMax As Double = Double.MinValue
                For Each pt In _airfoilCoords
                    xMin = Math.Min(xMin, pt.X) : xMax = Math.Max(xMax, pt.X)
                    yMin = Math.Min(yMin, pt.Y) : yMax = Math.Max(yMax, pt.Y)
                Next
                If xMin = xMax Then xMin -= 1 : xMax += 1
                If yMin = yMax Then yMin -= 1 : yMax += 1

                ' Equal scale on both axes (aspect-ratio-correct), fit to the smaller dimension,
                ' then center the shape in the remaining space.
                Dim scale = Math.Min(plotW / (xMax - xMin), plotH / (yMax - yMin)) * 0.9
                Dim shapeW = CSng((xMax - xMin) * scale)
                Dim shapeH = CSng((yMax - yMin) * scale)
                Dim originX = marginX + (plotW - shapeW) / 2.0F
                Dim originY = marginY + (plotH - shapeH) / 2.0F

                g.DrawString($"Airfoil geometry ({_airfoilCoords.Count} points)", axisFont, New SolidBrush(ThemeForeColor), New PointF(marginX, 8))
                g.DrawString("upper", tickFont, New SolidBrush(ThemeUpperColor), New PointF(w - 90, 10))
                g.DrawString("lower", tickFont, New SolidBrush(ThemeLowerColor), New PointF(w - 45, 10))

                Dim mapGeom = Function(pt As XfoilGeomPoint) As PointF
                                  Dim px = CSng(originX + (pt.X - xMin) * scale)
                                  ' Flip Y: airfoil y increases upward, screen y increases downward.
                                  Dim py = CSng(originY + shapeH - (pt.Y - yMin) * scale)
                                  Return New PointF(px, py)
                              End Function

                ' Split upper/lower the same way as the Cp plot's airfoil overlay.
                Dim splitIdx = IndexOfMinX(_airfoilCoords)
                Dim upperPts As New List(Of PointF)
                For i = 0 To splitIdx
                    upperPts.Add(mapGeom(_airfoilCoords(i)))
                Next
                If upperPts.Count >= 2 Then g.DrawLines(New Pen(ThemeUpperColor, 1.5F), upperPts.ToArray())
                Dim lowerPts As New List(Of PointF)
                For i = splitIdx To _airfoilCoords.Count - 1
                    lowerPts.Add(mapGeom(_airfoilCoords(i)))
                Next
                If upperPts.Count > 0 Then lowerPts.Add(upperPts(0)) ' close the loop back to TE
                If lowerPts.Count >= 2 Then g.DrawLines(New Pen(ThemeLowerColor, 1.5F), lowerPts.ToArray())
            End If

            If captureVectors Then
                svgOut = g.GetSvgContent()
                pdfOut = g.GetPdfContentStream()
            End If
        End Using

        SetPlotBitmap(pGeom, bmp)
        If captureVectors Then _geomSvg = svgOut : _geomPdf = pdfOut
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

Public Class XfoilGeomPoint
    Public X, Y As Double
End Class

Public Class XfoilBLPoint
    Public X, Y, Cf, H, Theta, Dstar, Ue As Double
End Class

''' <summary>
''' One completed polar sweep, shown as its own colored/labeled series so multiple
''' sweeps (different Re/Mach/airfoil) can be overlaid and compared in the Polar tab.
''' </summary>
Public Class XfoilPolarRun
    Public Label As String
    Public Color As Color
    Public Points As New List(Of XfoilPolarPoint)
    Public Visible As Boolean = True
End Class

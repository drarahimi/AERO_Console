Option Explicit On
Option Strict On

Imports System.ComponentModel
Imports System.IO
Imports System.IO.Compression
Imports System.Threading

Public Class frmMain
    Public p As Process
    Private Leaving As Boolean
    Private logtext As String = ""
    Private bt As Thread
    Public updatedpath As String = Path.Combine(Application.StartupPath, "update.exe")
    Public originalpath As String = Path.Combine(Application.StartupPath, "AERO_Console.exe")
    Public appUpdateNeeded As Boolean = False
    Public appUpdated As Boolean = False
    Public curApp As String = "avl"
    Public firstLoad As Boolean = False
    Public Shared systemFont As Font = New Font("Consolas", 12)
    Public fw As FileSystemWatcher = New FileSystemWatcher()
    ' Starts blank, not "test" - SetPlaceholder() deliberately bypasses txtName_TextChanged
    ' while showing the placeholder (so it doesn't stomp a real name mid-sync), which means
    ' this field is never touched by that path. A hardcoded "test" default left a fresh,
    ' untouched window internally thinking a project named "test" was active - showing the
    ' "using default 'test' project" warning - even though the visible box was just the
    ' empty placeholder. See the same fix on frmGeometry's projectName.
    Public projectName As String = ""
    Public Shared IsSyncingProject As Boolean = False
    Private cbEngine As ToolStripComboBox = Nothing
    Private btnClosePlot As ToolStripButton = Nothing
    Private ReadOnly _logBuffer As New Text.StringBuilder()
    Private ReadOnly _logBufferLock As New Object()
    Private WithEvents _logFlushTimer As New System.Windows.Forms.Timer With {.Interval = 75}
    ' Index into txtLog.Text where the in-progress, not-yet-submitted command starts.
    ' Everything before this is AVL's own output history and stays read-only.
    Private _promptStart As Integer = 0
    Private Const DESKTOPVERTRES As Integer = &H75
    Private Const DESKTOPHORZRES As Integer = &H76
    <Runtime.InteropServices.DllImport("gdi32.dll")> Private Shared Function GetDeviceCaps(ByVal hdc As IntPtr, ByVal nIndex As Integer) As Integer
    End Function
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

        If _promptStart >= txtLog.TextLength Then
            ' Nothing typed yet - plain append, same as a normal console.
            txtLog.AppendText(pending)
            _promptStart = txtLog.TextLength
            txtLog.SelectionStart = _promptStart
        Else
            ' The user has a command in progress. Splice the new output in
            ' before it instead of after, so it doesn't land in the middle
            ' of - or get appended after - what they're typing.
            Dim caretPos = txtLog.SelectionStart
            Dim caretInPrompt = caretPos >= _promptStart
            Dim relCaret = Math.Max(0, caretPos - _promptStart)

            txtLog.Select(_promptStart, 0)
            txtLog.SelectedText = pending
            _promptStart += pending.Length

            txtLog.SelectionStart = If(caretInPrompt, _promptStart + relCaret, caretPos)
            txtLog.SelectionLength = 0
        End If

        txtLog.ScrollToCaret()
    End Sub
    Public Sub loadConsole()
        ' 1. Cleanup old process if it exists
        If p IsNot Nothing Then
            Try
                If Not p.HasExited Then p.Kill()
                p.Dispose()
            Catch
            End Try
            p = Nothing
        End If

        ' 2. Setup New Process
        p = New Process()
        Dim startinfo = New ProcessStartInfo()
        Dim appPath As String = ""

        If curApp.ToLower() = "avl" Then
            appPath = Path.Combine(Application.StartupPath, "appdata", "avl.exe")
        ElseIf curApp.ToLower() = "xfoil" Then
            appPath = Path.Combine(Application.StartupPath, "appdata", "xfoil.exe")
        End If

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

        ' 3. Start the Process
        p.Start()
        If lblStatus IsNot Nothing Then
            lblStatus.Text = $"Status: Running {curApp.ToUpper()}"
        End If

        If p.HasExited Then
            AppMessageBox.Show("Process exited immediately! Exit Code: " & p.ExitCode)
            ' If this pops up, it means the EXE path is wrong, 
            ' or it's blocked by antivirus, or missing a DLL.
        End If

        ' 4. Start the Thread (No need to abort the old one, it died when we did p.Kill)
        bt = New Thread(AddressOf ReadThread)
        bt.IsBackground = True
        bt.Start()

        _promptStart = txtLog.TextLength
        txtLog.Focus()

    End Sub



    Public Sub findAVLs(path As String)
        ' 1. Thread Safety: Ensure this runs on the UI thread
        If Me.InvokeRequired Then
            Me.Invoke(Sub() findAVLs(path))
            Return
        End If

        ' 2. Get the new list of files
        Dim files() As String = Directory.GetFiles(path, "*.avl", SearchOption.TopDirectoryOnly)
        Dim newItems As New List(Of String)
        For Each FileName As String In files
            newItems.Add(System.IO.Path.GetFileNameWithoutExtension(FileName))
        Next

        ' 3. Define a helper action to update any given control
        ' (This allows us to use the exact same logic for 'Me' and other forms)
        Dim updateControl As Action(Of Object) =
        Sub(ctrl)
            ' Change 'ComboBox' to 'ListBox' if your control is a ListBox
            Dim box = TryCast(ctrl, ToolStripComboBox)
            If box Is Nothing Then Return

            ' A. Check if the items in the ComboBox are already identical to newItems
            Dim itemsChanged As Boolean = False
            If box.Items.Count <> newItems.Count Then
                itemsChanged = True
            Else
                For i As Integer = 0 To newItems.Count - 1
                    If Not box.Items(i).ToString().Equals(newItems(i)) Then
                        itemsChanged = True
                        Exit For
                    End If
                Next
            End If

            ' If items are unchanged, do nothing (completely prevents redraw/flicker)
            If Not itemsChanged Then Return

            ' B. Save the current selection
            Dim currentSelection As String = box.Text

            ' C. Update the list (using ComboBox.BeginUpdate/EndUpdate avoids drawing artifacts)
            box.ComboBox.BeginUpdate()
            box.Items.Clear()
            For Each item In newItems
                box.Items.Add(item)
            Next

            ' D. Restore selection only if the file still exists
            If box.Items.Contains(currentSelection) Then
                box.Text = currentSelection
            Else
                ' Optional: Select the first item or clear selection
                box.SelectedIndex = -1
            End If
            box.ComboBox.EndUpdate()
        End Sub

        ' 4. Update the control on THIS form
        updateControl(Me.txtName)

        ' 5. Find and update 'txtName' on other open frmGeometry forms
        For Each frm As Form In Application.OpenForms
            ' Check if the form is frmGeometry (and not the one we just updated)
            If TypeOf frm Is frmGeometry AndAlso frm IsNot Me Then
                Dim geoForm = DirectCast(frm, frmGeometry)
                ' Pass the other form's txtName control to our helper
                updateControl(geoForm.txtName)
            End If
        Next
    End Sub

    ' ===================== Drag-and-drop file import =====================
    ' Lets users drop .avl/.mass/.run/airfoil files, whole folders, or .zip
    ' archives onto the main window; everything gets flattened (no subfolder
    ' structure preserved) and copied straight into Application.StartupPath,
    ' which is where the app actually looks for project files. The existing
    ' FileSystemWatcher (fw) already picks up newly-created .avl/.mass/.run
    ' files and refreshes the project dropdown, so no manual refresh is needed.

    ' Runtime files that must never be clobbered by a careless drop (e.g.
    ' someone dragging in a whole folder that happens to contain other stuff).
    Private Shared ReadOnly DropImportExcludedExtensions As String() = {".exe", ".dll", ".pdb", ".config", ".json", ".manifest"}

    Private Sub InitializeDropZone()
        Dim pnlDropZone As New Panel()
        pnlDropZone.Dock = DockStyle.Fill
        pnlDropZone.BackColor = Color.FromArgb(45, 45, 48)
        pnlDropZone.BorderStyle = BorderStyle.FixedSingle
        pnlDropZone.AllowDrop = True
        pnlDropZone.Margin = New Padding(0, 5, 0, 0)

        Dim lblDropHint As New Label()
        lblDropHint.Text = "Drop .avl / .mass / .run / airfoil files, folders, or .zip archives here to copy them into the project folder"
        lblDropHint.Dock = DockStyle.Fill
        lblDropHint.TextAlign = ContentAlignment.MiddleCenter
        lblDropHint.ForeColor = Color.Gainsboro
        lblDropHint.AllowDrop = True
        pnlDropZone.Controls.Add(lblDropHint)

        AddHandler pnlDropZone.DragEnter, AddressOf DropZone_DragEnter
        AddHandler pnlDropZone.DragDrop, AddressOf DropZone_DragDrop
        AddHandler lblDropHint.DragEnter, AddressOf DropZone_DragEnter
        AddHandler lblDropHint.DragDrop, AddressOf DropZone_DragDrop

        ' Added as a new row of the EXISTING LayoutTable (rather than a
        ' Dock=Bottom sibling of it) so there's no docking-order ambiguity
        ' between this and StatusStrip1 - a table's row layout is unambiguous.
        ' Row 0 (txtLog, Percent 100) is defined in the designer; this adds
        ' row 1 for the drop zone.
        LayoutTable.RowCount = 2
        LayoutTable.RowStyles.Add(New RowStyle(SizeType.Absolute, 56.0F))
        LayoutTable.Controls.Add(pnlDropZone, 0, 1)
    End Sub

    Private Sub DropZone_DragEnter(sender As Object, e As DragEventArgs)
        e.Effect = If(e.Data.GetDataPresent(DataFormats.FileDrop), DragDropEffects.Copy, DragDropEffects.None)
    End Sub

    Private Sub DropZone_DragDrop(sender As Object, e As DragEventArgs)
        If Not e.Data.GetDataPresent(DataFormats.FileDrop) Then Return
        Dim paths = TryCast(e.Data.GetData(DataFormats.FileDrop), String())
        If paths Is Nothing OrElse paths.Length = 0 Then Return
        ImportDroppedPaths(paths)
    End Sub

    Private Sub ImportDroppedPaths(paths As String())
        Dim destDir = Application.StartupPath
        Dim tempExtractDirs As New List(Of String)
        Dim toCopy As New List(Of Tuple(Of String, String))   ' (sourceFile, destFileName)
        Dim usedNames As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        Try
            For Each droppedPath In paths
                Try
                    If Directory.Exists(droppedPath) Then
                        For Each f In Directory.GetFiles(droppedPath, "*", SearchOption.AllDirectories)
                            AddDropCandidate(f, toCopy, usedNames)
                        Next
                    ElseIf File.Exists(droppedPath) Then
                        If Path.GetExtension(droppedPath).Equals(".zip", StringComparison.OrdinalIgnoreCase) Then
                            Dim tempDir = Path.Combine(Path.GetTempPath(), "avlimport_" & Path.GetRandomFileName())
                            Directory.CreateDirectory(tempDir)
                            tempExtractDirs.Add(tempDir)
                            ZipFile.ExtractToDirectory(droppedPath, tempDir)
                            For Each f In Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories)
                                AddDropCandidate(f, toCopy, usedNames)
                            Next
                        Else
                            AddDropCandidate(droppedPath, toCopy, usedNames)
                        End If
                    End If
                Catch ex As Exception
                    AppMessageBox.Show($"Could not read ""{droppedPath}"":" & vbCrLf & ex.Message, "Import Files", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End Try
            Next

            If toCopy.Count = 0 Then
                AppMessageBox.Show("No usable files were found in what was dropped.", "Import Files", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim existingCount = 0
            For Each t In toCopy
                If File.Exists(Path.Combine(destDir, t.Item2)) Then existingCount += 1
            Next

            Dim msg = $"{toCopy.Count} file(s) will be copied to:" & vbCrLf & destDir
            If existingCount > 0 Then
                msg &= vbCrLf & vbCrLf & $"{existingCount} file(s) already exist there and will be overwritten."
            End If
            msg &= vbCrLf & vbCrLf & "Continue?"
            If AppMessageBox.Show(msg, "Import Files", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.No Then Return

            Dim copied = 0
            Dim errors As New List(Of String)
            For Each t In toCopy
                Try
                    File.Copy(t.Item1, Path.Combine(destDir, t.Item2), True)
                    copied += 1
                Catch ex As Exception
                    errors.Add($"{t.Item2}: {ex.Message}")
                End Try
            Next

            Dim summary = $"Copied {copied} of {toCopy.Count} file(s) to:" & vbCrLf & destDir
            If errors.Count > 0 Then
                summary &= vbCrLf & vbCrLf & "Errors:" & vbCrLf & String.Join(vbCrLf, errors)
            End If
            AppMessageBox.Show(summary, "Import Files", MessageBoxButtons.OK, If(errors.Count > 0, MessageBoxIcon.Warning, MessageBoxIcon.Information))
        Finally
            For Each d In tempExtractDirs
                Try
                    Directory.Delete(d, True)
                Catch
                End Try
            Next
        End Try
    End Sub

    ' Filters out runtime/junk files and flattens the destination name,
    ' auto-disambiguating (name_2.ext, name_3.ext, ...) if two different
    ' source files in the SAME drop would otherwise land on the same name.
    Private Sub AddDropCandidate(f As String, toCopy As List(Of Tuple(Of String, String)), usedNames As HashSet(Of String))
        Dim name = Path.GetFileName(f)
        If name.StartsWith("._") OrElse name.Equals(".DS_Store", StringComparison.OrdinalIgnoreCase) Then Return
        If f.Contains("__MACOSX") Then Return

        Dim ext = Path.GetExtension(f).ToLowerInvariant()
        If Array.IndexOf(DropImportExcludedExtensions, ext) >= 0 Then Return

        Dim destName = name
        If usedNames.Contains(destName) Then
            Dim baseName = Path.GetFileNameWithoutExtension(name)
            Dim n = 2
            Do While usedNames.Contains($"{baseName}_{n}{ext}")
                n += 1
            Loop
            destName = $"{baseName}_{n}{ext}"
        End If
        usedNames.Add(destName)
        toCopy.Add(Tuple.Create(f, destName))
    End Sub

    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        _logFlushTimer.Start()

        If Application.ExecutablePath.EndsWith("update.exe") Then

            File.Copy(updatedpath, originalpath, True)
            Dim fi As FileInfo = New FileInfo(originalpath)
            fi.Attributes = FileAttributes.Normal
            My.Settings.appUpdated = True
            My.Settings.Save()
            Me.Close()
        Else
            Try
                File.Delete(updatedpath)
            Catch
            End Try
        End If


        'Dim FileVer As String = Assembly.LoadFrom(Application.ExecutablePath).GetName.Version.ToString 'FileVersionInfo.GetVersionInfo(Application.ExecutablePath).FileVersion  'FileVersionInfo.GetVersionInfo(Application.ExecutablePath).SpecialBuild
        Me.Text = My.Application.Info.AssemblyName.ToString.Replace("_", " ") + " - " + My.Application.Info.Version.ToString

        Me.Width = CInt(Screen.PrimaryScreen.WorkingArea.Width * 0.8)
        Me.Height = CInt(Screen.PrimaryScreen.WorkingArea.Height * 0.8)
        Me.Left = CInt((Screen.PrimaryScreen.WorkingArea.Width - Me.Width) / 2)
        Me.Top = CInt((Screen.PrimaryScreen.WorkingArea.Height - Me.Height) / 2)

        ' 1. Configure the Watcher
        fw.Path = Application.StartupPath
        fw.NotifyFilter = NotifyFilters.FileName ' We only care if files are added/removed/renamed
        fw.Filter = "*.*" ' Watch all files, we will filter specific extensions in the code below
        fw.EnableRaisingEvents = True

        ' 2. Add Handlers for all relevant events
        ' Note: We point them all to the same update logic for simplicity
        AddHandler fw.Created, AddressOf OnFileChanged
        AddHandler fw.Deleted, AddressOf OnFileChanged
        AddHandler fw.Renamed, AddressOf OnFileRenamed

        Try
            Dim fi As FileInfo = New FileInfo(Path.Combine(Application.StartupPath, "appdata.zip"))
            Dim b() As Byte = My.Resources.appdata
            If fi.Exists Then fi.Delete()
            File.WriteAllBytes(fi.FullName, b)
            fi.Attributes = FileAttributes.Hidden

            txtLog.Dock = DockStyle.Fill

            Dim d = Path.Combine(Application.StartupPath, "appdata")
            If File.Exists(fi.FullName) Then
                If Directory.Exists(d) Then
                    Directory.Delete(d, True)
                End If
                ZipFile.ExtractToDirectory(fi.FullName, d)
                Dim DirectoryInfo As DirectoryInfo = New DirectoryInfo(d)
                DirectoryInfo.Attributes = FileAttributes.Hidden
                'Directory
                'End If
            End If
            fi.Delete()
            loadConsole()
        Catch ex As Exception
            AppMessageBox.Show("Error: " + ex.Message)
        End Try

        ' Initialize engine selection dynamically
        Dim lblEngine As New ToolStripLabel("Engine:")
        lblEngine.ForeColor = Color.DimGray
        
        cbEngine = New ToolStripComboBox("cbEngine")
        cbEngine.Items.AddRange(New Object() {"AVL", "XFOIL"})
        cbEngine.SelectedIndex = 0 ' AVL by default
        cbEngine.DropDownStyle = ComboBoxStyle.DropDownList
        AddHandler cbEngine.SelectedIndexChanged, AddressOf cbEngine_SelectedIndexChanged
        
        ' Find the index of btnGeometry to insert before it
        Dim insertIndex = ToolStrip2.Items.IndexOf(btnGeometry)
        If insertIndex >= 0 Then
            ToolStrip2.Items.Insert(insertIndex, lblEngine)
            ToolStrip2.Items.Insert(insertIndex + 1, cbEngine)
            ToolStrip2.Items.Insert(insertIndex + 2, New ToolStripSeparator())
        Else
            ToolStrip2.Items.Add(lblEngine)
            ToolStrip2.Items.Add(cbEngine)
            ToolStrip2.Items.Add(New ToolStripSeparator())
        End If

        ' Initialize Close Plot button dynamically
        btnClosePlot = New ToolStripButton("Close Plot")
        btnClosePlot.Name = "btnClosePlot"
        btnClosePlot.Visible = False
        AddHandler btnClosePlot.Click, AddressOf btnClosePlot_Click
        
        Dim designerIndex = ToolStrip2.Items.IndexOf(btnDesigner)
        If designerIndex >= 0 Then
            ToolStrip2.Items.Insert(designerIndex, btnClosePlot)
        Else
            ToolStrip2.Items.Add(btnClosePlot)
        End If

        ' Initialize warning label dynamically
        Dim lblWarning As New ToolStripLabel()
        lblWarning.Name = "lblWarning"
        lblWarning.ForeColor = Color.OrangeRed
        lblWarning.Font = New Font(ToolStrip1.Font, FontStyle.Bold)
        lblWarning.Text = "⚠️ Warning: Using default 'test' project. Changes will be overwritten!"
        ToolStrip1.Items.Add(lblWarning)

        ' Initialize placeholder behavior for txtName
        AddHandler txtName.ComboBox.GotFocus, Sub(s, ev) ClearPlaceholder()
        AddHandler txtName.ComboBox.LostFocus, Sub(s, ev) SetPlaceholder()
        SetPlaceholder()

        UpdateTitleAndButtons()
        UpdateProjectWarning()

        If (Not My.Settings.appFont Is Nothing) Then
            systemFont = My.Settings.appFont
        End If

        SetAllControlsFont(Me.Controls, systemFont)
        firstLoad = True
        findAVLs(Environment.CurrentDirectory)
        InitializeDropZone()

        ' Enable double-buffering recursively on all controls (including toolbars) to prevent hover/draw flicker
        EnableDoubleBuffering(Me)

        ' Set ComboBox FlatStyle and double-buffering directly
        Try
            Dim dbProp = GetType(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic Or System.Reflection.BindingFlags.Instance)
            If dbProp IsNot Nothing Then
                If txtName IsNot Nothing AndAlso txtName.ComboBox IsNot Nothing Then
                    dbProp.SetValue(txtName.ComboBox, True, Nothing)
                    txtName.ComboBox.FlatStyle = FlatStyle.Flat
                End If
                If cbEngine IsNot Nothing AndAlso cbEngine.ComboBox IsNot Nothing Then
                    dbProp.SetValue(cbEngine.ComboBox, True, Nothing)
                    cbEngine.ComboBox.FlatStyle = FlatStyle.Flat
                End If
            End If
        Catch
        End Try


        'Using g As Graphics = Graphics.FromHwnd(IntPtr.Zero)
        '    Dim hdc As IntPtr = g.GetHdc
        '    Dim TrueScreenSize As New Size(GetDeviceCaps(hdc, DESKTOPHORZRES), GetDeviceCaps(hdc, DESKTOPVERTRES))
        '    Dim sclX As Single = CSng(Math.Round((TrueScreenSize.Width / Screen.PrimaryScreen.Bounds.Width), 2))
        '    Dim sclY As Single = CSng(Math.Round((TrueScreenSize.Height / Screen.PrimaryScreen.Bounds.Height), 2))
        '    g.ReleaseHdc(hdc)

        '    'show the true screen size
        '    Dim DPIstr = "Screen Width:  " & TrueScreenSize.Width.ToString & vbLf &
        '                  "Screen Height: " & TrueScreenSize.Height.ToString & vbLf & vbLf &
        '                  "Scale X: " & sclX.ToString & vbLf &
        '                  "Scale Y: " & sclY.ToString
        '    If (sclX <> 1 Or sclY <> 1) Then
        '        AppMessageBox.Show($"Your displace scale factor is {sclX * 100}% in X and {sclY * 100}% in Y direction. Please note that your editor may experience display issues if your display scale factor is not at 100%. Please fix the scale factor before continuing." + vbNewLine + vbNewLine + "See this for help: https://bit.ly/3LzMotW")
        '    End If

        'End Using

        'frmGeometry.Show()


    End Sub

    ''' <summary>
    ''' Handles Created and Deleted events
    ''' </summary>
    Private Sub OnFileChanged(sender As Object, e As FileSystemEventArgs)
        If IsTargetFile(e.FullPath) Then
            RefreshFileList()
        End If
    End Sub

    ''' <summary>
    ''' Handles Renamed events. 
    ''' We need to check if the file was renamed TO a target type OR FROM a target type.
    ''' </summary>
    Private Sub OnFileRenamed(sender As Object, e As RenamedEventArgs)
        ' Update if the old name WAS a target, or the new name IS a target
        If IsTargetFile(e.FullPath) OrElse IsTargetFile(e.OldFullPath) Then
            RefreshFileList()
        End If
    End Sub

    ''' <summary>
    ''' Checks if the file ends in .avl, .mass, or .run
    ''' </summary>
    Private Function IsTargetFile(filePath As String) As Boolean
        Dim ext As String = IO.Path.GetExtension(filePath).ToLower()
        Return ext = ".avl" OrElse ext = ".mass" OrElse ext = ".run"
    End Function

    ''' <summary>
    ''' Updates the UI safely from the background thread
    ''' </summary>
    Private Sub RefreshFileList()
        ' We must use Invoke because FileSystemWatcher runs on a different thread
        If Me.InvokeRequired Then
            Me.Invoke(Sub() UpdateListBoxLogic())
        Else
            UpdateListBoxLogic()
        End If
    End Sub

    ''' <summary>
    ''' The actual logic to re-read the directory and fill your list
    ''' </summary>
    Private Sub UpdateListBoxLogic()
        findAVLs(Environment.CurrentDirectory)
    End Sub

    Private Sub AirplaneDesignToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AirplaneDesignToolStripMenuItem.Click
        frmGeometry.WindowState = FormWindowState.Maximized
        frmGeometry.Show()
    End Sub

    Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
        frmAbout.Show()
    End Sub

    Private Sub OpenCurrentDirectoryToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles OpenCurrentDirectoryToolStripMenuItem.Click
        Process.Start("explorer.exe", Application.StartupPath)
    End Sub

    ''' <summary>
    ''' Zips the bare-minimum set of files needed to launch AERO Console (the exe, its IL assembly,
    ''' the third-party FastColoredTextBox dependency, and the .NET runtime config/deps files) and drops
    ''' it on the Desktop, ready to attach to a GitHub Release. Deliberately an allow-list of runtime
    ''' file types directly in the install folder (non-recursive) so debug symbols (.pdb), API doc XML,
    ''' sample/working project files (test.avl/.mas/.run), the "appdata" runtime cache (rebuilt
    ''' automatically from an embedded resource plus the XFOIL auto-downloader on first run), and stray
    ''' build artifacts like the ClickOnce "publish" or self-contained "win-x64" subfolders never get swept in.
    ''' </summary>
    Private Sub PackageForReleaseToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles PackageForReleaseToolStripMenuItem.Click
        Dim oldCursor As Cursor = Me.Cursor
        Try
            Dim sourceDir As String = Application.StartupPath
            Dim versionStr As String = My.Application.Info.Version.ToString()
            Dim desktopDir As String = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            Dim zipPath As String = Path.Combine(desktopDir, $"AERO_Console_v{versionStr}.zip")

            If File.Exists(zipPath) Then
                Dim overwrite = AppMessageBox.Show($"""{Path.GetFileName(zipPath)}"" already exists on the Desktop. Overwrite it?",
                                                 "Package for GitHub Release", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                If overwrite <> DialogResult.Yes Then Return
                File.Delete(zipPath)
            End If

            ' Only these runtime file types are needed to launch the app; everything else (debug symbols,
            ' doc XML, sample project files, downloaded appdata, alternate build outputs) is left out.
            Dim allowedExtensions As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {".exe", ".dll", ".json", ".config"}
            Dim excludedFileNames As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {"update.exe"}

            Me.Cursor = Cursors.WaitCursor
            lblStatus.Text = "Status: Packaging release zip..."
            Application.DoEvents()

            Dim fileCount As Integer = 0
            Using zip As ZipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create)
                For Each filePath In Directory.EnumerateFiles(sourceDir, "*", SearchOption.TopDirectoryOnly)
                    Dim fileName As String = Path.GetFileName(filePath)

                    If Not allowedExtensions.Contains(Path.GetExtension(filePath)) Then Continue For
                    If excludedFileNames.Contains(fileName) Then Continue For

                    zip.CreateEntryFromFile(filePath, fileName, CompressionLevel.Optimal)
                    fileCount += 1
                Next
            End Using

            Me.Cursor = oldCursor
            lblStatus.Text = "Status: Release package created."

            Dim openFolder = AppMessageBox.Show($"Release package created ({fileCount} files):{Environment.NewLine}{zipPath}{Environment.NewLine}{Environment.NewLine}Open containing folder?",
                                              "Package for GitHub Release", MessageBoxButtons.YesNo, MessageBoxIcon.Information)
            If openFolder = DialogResult.Yes Then
                Process.Start("explorer.exe", $"/select,""{zipPath}""")
            End If
        Catch ex As Exception
            Me.Cursor = oldCursor
            AppMessageBox.Show("Failed to create release package: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Publishes a single, self-sufficient exe (.NET single-file publish, framework-dependent, ReadyToRun)
    ''' and drops it on the Desktop, ready to attach to a GitHub Release. Single-file publish already bundles
    ''' AERO_Console.dll and FastColoredTextBox.dll into the one exe (extracted to a runtime temp cache
    ''' automatically by .NET itself), so no separate "install to a folder" step is needed - the published
    ''' exe already runs standalone from anywhere a user puts it, same as double-clicking it today, just
    ''' without needing the sibling .dll/.json files a normal build/zip requires.
    ''' Requires the .NET SDK (this is a maintainer/dev-machine action, not something end users run).
    ''' </summary>
    Private Async Sub PackageStandaloneExeToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles PackageStandaloneExeToolStripMenuItem.Click
        Dim oldCursor As Cursor = Me.Cursor
        Try
            Dim projectPath As String = FindProjectFile()
            If projectPath Is Nothing Then
                AppMessageBox.Show("Could not locate AERO_Console.vbproj by walking up from the running exe's folder." & Environment.NewLine &
                                 "This feature must be run from a normal Debug/Release build inside the source repo.",
                                 "Package as Standalone Exe", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim versionStr As String = My.Application.Info.Version.ToString()
            Dim desktopDir As String = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            Dim destExePath As String = Path.Combine(desktopDir, $"AERO_Console_v{versionStr}.exe")

            If File.Exists(destExePath) Then
                Dim overwrite = AppMessageBox.Show($"""{Path.GetFileName(destExePath)}"" already exists on the Desktop. Overwrite it?",
                                                 "Package as Standalone Exe", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                If overwrite <> DialogResult.Yes Then Return
            End If

            Me.Cursor = Cursors.WaitCursor
            lblStatus.Text = "Status: Publishing standalone exe (this can take a minute)..."

            Dim projectDir As String = Path.GetDirectoryName(projectPath)
            Dim publishDir As String = Path.Combine(projectDir, "bin", "Release", "net10.0-windows", "publish", "win-x64")

            ' Arguments are added one-by-one via ArgumentList (not a single manually-quoted .Arguments
            ' string) so paths are escaped correctly. A hand-built "...\"" argument is misparsed by
            ' Windows' command-line rules - a backslash immediately before a closing quote escapes the
            ' quote itself instead of being a literal path separator, corrupting everything after it.
            ' Passed as explicit properties (matching My Project\PublishProfiles\FolderProfile.pubxml) rather
            ' than "-p:PublishProfile=FolderProfile" - the SDK's profile-name lookup does not resolve pubxml
            ' files under the VB "My Project" folder here, so it silently falls back to a non-single-file publish.
            ' (PublishDir is safe to override this way; BaseOutputPath/BaseIntermediateOutputPath are NOT -
            ' MSBuild only honors those reliably when set before the Sdk import, so overriding them via -p:
            ' on the command line produced duplicate generated AssemblyInfo files and a hard compile error.)
            Dim psi As New ProcessStartInfo("dotnet.exe") With {
                .WorkingDirectory = projectDir,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .UseShellExecute = False,
                .CreateNoWindow = True
            }
            psi.ArgumentList.Add("publish")
            psi.ArgumentList.Add(projectPath)
            psi.ArgumentList.Add("-c")
            psi.ArgumentList.Add("Release")
            psi.ArgumentList.Add($"-p:PublishDir={publishDir}\")
            psi.ArgumentList.Add("-p:SelfContained=false")
            psi.ArgumentList.Add("-p:RuntimeIdentifier=win-x64")
            psi.ArgumentList.Add("-p:PublishSingleFile=true")
            psi.ArgumentList.Add("-p:PublishReadyToRun=true")
            psi.ArgumentList.Add("--nologo")
            psi.ArgumentList.Add("-v:q")

            Dim output As String
            Dim exitCode As Integer
            Using proc As New Process()
                proc.StartInfo = psi
                proc.Start()
                Dim stdOutTask = proc.StandardOutput.ReadToEndAsync()
                Dim stdErrTask = proc.StandardError.ReadToEndAsync()
                Await proc.WaitForExitAsync()
                output = (Await stdOutTask) & Environment.NewLine & (Await stdErrTask)
                exitCode = proc.ExitCode
            End Using

            If exitCode <> 0 Then
                Me.Cursor = oldCursor
                lblStatus.Text = "Status: Publish failed."
                AppMessageBox.Show("dotnet publish failed:" & Environment.NewLine & output, "Package as Standalone Exe", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            Dim publishedExeCandidates = If(Directory.Exists(publishDir), Directory.GetFiles(publishDir, "*.exe"), Array.Empty(Of String)())
            If publishedExeCandidates.Length = 0 Then
                Me.Cursor = oldCursor
                lblStatus.Text = "Status: Publish output not found."
                AppMessageBox.Show($"Publish succeeded but no .exe was found in:{Environment.NewLine}{publishDir}",
                                 "Package as Standalone Exe", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If
            Dim publishedExePath As String = publishedExeCandidates(0)

            File.Copy(publishedExePath, destExePath, overwrite:=True)

            Me.Cursor = oldCursor
            lblStatus.Text = "Status: Standalone exe created."

            Dim openFolder = AppMessageBox.Show($"Standalone exe created:{Environment.NewLine}{destExePath}{Environment.NewLine}{Environment.NewLine}" &
                                              "It runs on its own from anywhere - no other files needed." & Environment.NewLine & Environment.NewLine &
                                              "Open containing folder?",
                                              "Package as Standalone Exe", MessageBoxButtons.YesNo, MessageBoxIcon.Information)
            If openFolder = DialogResult.Yes Then
                Process.Start("explorer.exe", $"/select,""{destExePath}""")
            End If
        Catch ex As Exception
            Me.Cursor = oldCursor
            AppMessageBox.Show("Failed to create standalone exe: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Walks up from the running exe's folder (e.g. AVL_Geometry\bin\Release\net10.0-windows\) looking
    ''' for the AERO_Console.vbproj that produced it, so packaging features can shell out to "dotnet publish".
    ''' </summary>
    Private Function FindProjectFile() As String
        Dim dir As New DirectoryInfo(Application.StartupPath)
        For i As Integer = 0 To 7
            If dir Is Nothing Then Exit For
            Dim candidate = Path.Combine(dir.FullName, "AERO_Console.vbproj")
            If File.Exists(candidate) Then Return candidate
            dir = dir.Parent
        Next
        Return Nothing
    End Function

    Private Sub RestartConsoleToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RestartConsoleToolStripMenuItem.Click
        If Not IsNothing(p) Then
            Try
                If Not p.HasExited Then p.Kill()
                p = Nothing
                txtLog.Text = ""
                _promptStart = 0
                If lblStatus IsNot Nothing Then lblStatus.Text = "Status: Restarting..."
                loadConsole()
            Catch
            End Try
        End If
    End Sub

    Private Sub frmMain_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        _logFlushTimer.Stop()

        For Each F As Form In Application.OpenForms
            F.Hide()
        Next
        'frmPrompt.Show()

        If Leaving = False Then
            Leaving = True
            Try
                Do While Not IsNothing(p) AndAlso (Not p.HasExited)
                    If Not IsNothing(p) Then
                        If Not p.HasExited Then p.Kill()
                        p = Nothing
                    End If
                Loop
            Catch ex As Exception
                AppMessageBox.Show("Error: " + ex.Message)
            End Try
            'e.Cancel = True
            'Else
            Dim d = Application.StartupPath + "\appdata"
            'Return
            If Directory.Exists(d) Then
                Do Until Not Directory.Exists(d)
                    Try
                        Directory.Delete(d, True)
                    Catch ex As Exception
                    End Try
                Loop
            End If

        End If
    End Sub

    Private Sub txtLog_KeyDown(sender As Object, e As KeyEventArgs) Handles txtLog.KeyDown
        ' Keys that only move the caret/selection or copy - safe to use anywhere,
        ' including up in the read-only history above the prompt.
        Dim isSafeInHistory =
            e.KeyCode = Keys.Left OrElse e.KeyCode = Keys.Right OrElse
            e.KeyCode = Keys.Up OrElse e.KeyCode = Keys.Down OrElse
            e.KeyCode = Keys.PageUp OrElse e.KeyCode = Keys.PageDown OrElse
            e.KeyCode = Keys.Home OrElse e.KeyCode = Keys.End OrElse
            (e.Control AndAlso (e.KeyCode = Keys.C OrElse e.KeyCode = Keys.A))

        ' Any other key (typing, paste, backspace...) always lands at the
        ' prompt - just like a real terminal, you can't edit past output.
        If Not isSafeInHistory AndAlso txtLog.SelectionStart < _promptStart Then
            txtLog.SelectionStart = txtLog.TextLength
            txtLog.SelectionLength = 0
        End If

        If e.KeyCode = Keys.Back AndAlso txtLog.SelectionLength = 0 AndAlso txtLog.SelectionStart <= _promptStart Then
            e.SuppressKeyPress = True
            Return
        End If

        If e.KeyCode = Keys.Delete AndAlso txtLog.SelectionStart < _promptStart Then
            e.SuppressKeyPress = True
            Return
        End If

        If e.KeyCode <> Keys.Return Then Return
        e.SuppressKeyPress = True

        If p Is Nothing OrElse p.HasExited Then Return

        Dim command = txtLog.Text.Substring(_promptStart)
        Try
            p.StandardInput.WriteLine(command)
            p.StandardInput.Flush()
        Catch ex As Exception
            Debug.WriteLine("Error sending command: " & ex.Message)
        End Try

        txtLog.AppendText(Environment.NewLine)
        _promptStart = txtLog.TextLength
        txtLog.SelectionStart = _promptStart
        txtLog.ScrollToCaret()
    End Sub

    Private Sub CheckForUpdatesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CheckForUpdatesToolStripMenuItem.Click
        frmUpdate.Show()
    End Sub

    Private Sub frmMain_Closed(sender As Object, e As EventArgs) Handles Me.Closed
    End Sub

    Private Sub btnDesigner_Click(sender As Object, e As EventArgs) Handles btnDesigner.Click
        AirplaneDesignToolStripMenuItem.PerformClick()
    End Sub

    Private Sub FontToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FontToolStripMenuItem.Click
        fd1.Font = txtLog.Font
        If (fd1.ShowDialog() = DialogResult.OK) Then
            'txtLog.Font = fd1.Font
            systemFont = fd1.Font
            My.Settings.appFont = systemFont
            My.Settings.Save()
            SetAllControlsFont(Me.Controls, systemFont)
        End If
    End Sub

    Public Shared Sub SetAllControlsFont(ctrls As Control.ControlCollection, font As Font)
        Debug.WriteLine("I am in the set font for controls")
        For Each ctrl As Control In ctrls

            If (Not ctrl.Controls Is Nothing) Then
                SetAllControlsFont(ctrl.Controls, font)
            End If
            Debug.WriteLine(ctrl.Name)
            ctrl.Font = font 'New Font("Impact", ctrl.Font.Size - 4)
        Next
    End Sub

    Private Sub AVLHelpToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AVLHelpToolStripMenuItem.Click
        frmHelp.Show()
        frmHelp.txt1.Text = frmGeometry.readLines(frmGeometry.help, 1, 2388)
    End Sub

    Private Sub btnGeometry_Click(sender As Object, e As EventArgs) Handles btnGeometry.Click
        If p Is Nothing OrElse p.HasExited Then Return
        Try
            If curApp.ToLower() = "avl" Then
                Dim f = $"{projectName}.avl"
                p.StandardInput.WriteLine($"load {f}")
            Else
                Dim cmd = ""
                If IsNumeric(projectName) AndAlso projectName.Length = 4 Then
                    cmd = $"naca {projectName}"
                ElseIf projectName.ToLower().StartsWith("naca") Then
                    cmd = projectName
                Else
                    cmd = $"load {projectName}.dat"
                End If
                p.StandardInput.WriteLine(cmd)
            End If
            p.StandardInput.Flush()
        Catch ex As Exception
            AppMessageBox.Show("Error: " + ex.Message)
        End Try
    End Sub

    Private Function GetPlaceholderText() As String
        If curApp.ToLower() = "avl" Then
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
        ' This form's own state (projectName/title/warning) must stay in sync with its own
        ' txtName text regardless of WHY that text changed - including when it was just set by
        ' a frmGeometry window syncing its project name into us (IsSyncingProject = True at that
        ' point). Only the re-broadcast loop below needs to skip during a sync, to avoid an
        ' infinite ping-pong between forms - otherwise this form's warning label (e.g. "using
        ' default 'test' project") kept showing the stale pre-sync project after switching
        ' projects from a Geometry window, since this whole handler used to bail out early.
        Dim txt = txtName.Text
        If txt = "Enter AVL Project (e.g. glider)" OrElse txt = "Enter NACA (e.g. 2412) or dat file" Then
            projectName = ""
        Else
            projectName = txt
        End If

        UpdateTitleAndButtons()
        UpdateProjectWarning()

        If IsSyncingProject Then Return

        ' Sync with open frmGeometry forms
        IsSyncingProject = True
        Try
            For Each frm As Form In Application.OpenForms
                If TypeOf frm Is frmGeometry Then
                    Dim geoForm = DirectCast(frm, frmGeometry)
                    If geoForm.txtName.Text <> txtName.Text Then
                        geoForm.txtName.Text = txtName.Text
                    End If
                End If
            Next
        Finally
            IsSyncingProject = False
        End Try
    End Sub

    Private Async Sub cbEngine_SelectedIndexChanged(sender As Object, e As EventArgs)
        Dim cb = DirectCast(sender, ToolStripComboBox)
        Dim selectedEngine = cb.SelectedItem.ToString().ToLower()
        
        If selectedEngine = "xfoil" Then
            Dim downloaded = Await DownloadXfoilAsync()
            If Not downloaded Then
                RemoveHandler cb.SelectedIndexChanged, AddressOf cbEngine_SelectedIndexChanged
                cb.SelectedIndex = 0 ' Fallback to AVL
                AddHandler cb.SelectedIndexChanged, AddressOf cbEngine_SelectedIndexChanged
                Return
            End If
        End If
        
        curApp = selectedEngine
        If lblStatus IsNot Nothing Then
            lblStatus.Text = $"Status: Switching to {selectedEngine.ToUpper()}..."
        End If
        
        ' Update placeholder if active
        If txtName.Text = "Enter AVL Project (e.g. glider)" OrElse txtName.Text = "Enter NACA (e.g. 2412) or dat file" Then
            RemoveHandler txtName.TextChanged, AddressOf txtName_TextChanged
            txtName.Text = GetPlaceholderText()
            txtName.ComboBox.ForeColor = Color.Gray
            AddHandler txtName.TextChanged, AddressOf txtName_TextChanged
        End If
        
        loadConsole()
        UpdateTitleAndButtons()
    End Sub

    ''' <summary>
    ''' Downloads a file with progress reported to the status-bar progress bar. The file is
    ''' written to a ".download" sibling of destPath first and only moved into place on success,
    ''' so a failed/cancelled download never leaves a half-written exe where the app expects one.
    ''' </summary>
    Private Async Function DownloadFileWithProgressAsync(url As String, destPath As String, displayName As String) As Task(Of Boolean)
        Dim oldStatus = If(lblStatus IsNot Nothing, lblStatus.Text, "")
        Dim tempPath = destPath & ".download"
        Try
            ShowDownloadProgress($"Status: Downloading {displayName}...")

            Using client As New System.Net.Http.HttpClient()
                Using response = Await client.GetAsync(url, System.Net.Http.HttpCompletionOption.ResponseHeadersRead)
                    response.EnsureSuccessStatusCode()
                    Dim totalBytes = response.Content.Headers.ContentLength

                    Using contentStream = Await response.Content.ReadAsStreamAsync()
                        Using fileStream As New FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None)
                            Dim buffer(81919) As Byte
                            Dim totalRead As Long = 0
                            Do
                                Dim bytesRead = Await contentStream.ReadAsync(buffer, 0, buffer.Length)
                                If bytesRead = 0 Then Exit Do
                                Await fileStream.WriteAsync(buffer, 0, bytesRead)
                                totalRead += bytesRead

                                If totalBytes.HasValue AndAlso totalBytes.Value > 0 Then
                                    Dim pct = CInt(totalRead * 100L / totalBytes.Value)
                                    UpdateDownloadProgress(pct, $"Status: Downloading {displayName}... {pct}%")
                                Else
                                    UpdateDownloadProgress(-1, $"Status: Downloading {displayName}... {totalRead \ 1024}KB")
                                End If
                            Loop
                        End Using
                    End Using
                End Using
            End Using

            Directory.CreateDirectory(Path.GetDirectoryName(destPath))
            File.Copy(tempPath, destPath, True)
            Return True
        Catch ex As Exception
            AppMessageBox.Show($"Failed to download {displayName}: {ex.Message}", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        Finally
            Try
                If File.Exists(tempPath) Then File.Delete(tempPath)
            Catch
            End Try
            HideDownloadProgress(oldStatus)
        End Try
    End Function

    Private Sub ShowDownloadProgress(statusText As String)
        If downloadProgressBar IsNot Nothing Then
            downloadProgressBar.Style = ProgressBarStyle.Marquee
            downloadProgressBar.Value = 0
            downloadProgressBar.Visible = True
        End If
        If lblStatus IsNot Nothing Then lblStatus.Text = statusText
    End Sub

    Private Sub UpdateDownloadProgress(percent As Integer, statusText As String)
        If downloadProgressBar IsNot Nothing Then
            If percent >= 0 Then
                downloadProgressBar.Style = ProgressBarStyle.Continuous
                downloadProgressBar.Value = Math.Min(100, Math.Max(0, percent))
            Else
                downloadProgressBar.Style = ProgressBarStyle.Marquee
            End If
        End If
        If lblStatus IsNot Nothing Then lblStatus.Text = statusText
    End Sub

    Private Sub HideDownloadProgress(restoreStatusText As String)
        If downloadProgressBar IsNot Nothing Then downloadProgressBar.Visible = False
        If lblStatus IsNot Nothing Then lblStatus.Text = restoreStatusText
    End Sub

    Private Sub OpenExternalLink(url As String)
        Try
            Process.Start(New ProcessStartInfo(url) With {.UseShellExecute = True})
        Catch ex As Exception
            AppMessageBox.Show("Could not open link: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub DownloadAvlPageToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DownloadAvlPageToolStripMenuItem.Click
        OpenExternalLink("https://web.mit.edu/drela/Public/web/avl/")
    End Sub

    Private Sub DownloadXfoilPageToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DownloadXfoilPageToolStripMenuItem.Click
        OpenExternalLink("https://web.mit.edu/drela/Public/web/xfoil/")
    End Sub

    Private Async Sub DownloadAvlToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DownloadAvlToolStripMenuItem.Click
        Dim destPath = Path.Combine(Application.StartupPath, "appdata", "avl.exe")

        If File.Exists(destPath) Then
            Dim resp = AppMessageBox.Show("AVL is already installed. Re-download the latest version and overwrite it?",
                                           "Download AVL", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If resp <> DialogResult.Yes Then Return
        End If

        DownloadAvlToolStripMenuItem.Enabled = False
        Try
            ' AVL ships as a bare .exe on MIT's site (no zip to extract), unlike XFOIL.
            If Await DownloadFileWithProgressAsync("https://web.mit.edu/drela/Public/web/avl/avl352.exe", destPath, "AVL") Then
                AppToast.Show("AVL downloaded and installed")
            End If
        Finally
            DownloadAvlToolStripMenuItem.Enabled = True
        End Try
    End Sub

    Private Async Sub DownloadXfoilToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DownloadXfoilToolStripMenuItem.Click
        DownloadXfoilToolStripMenuItem.Enabled = False
        Try
            Await DownloadXfoilAsync(forcePrompt:=True)
        Finally
            DownloadXfoilToolStripMenuItem.Enabled = True
        End Try
    End Sub

    ''' <summary>
    ''' forcePrompt:=False (the cbEngine auto-switch path) silently reuses an existing xfoil.exe.
    ''' forcePrompt:=True (the explicit "Download XFOIL..." menu item) always asks, even when
    ''' XFOIL is already installed, so the menu item is actually useful for grabbing a fresh copy.
    ''' </summary>
    Private Async Function DownloadXfoilAsync(Optional forcePrompt As Boolean = False) As Task(Of Boolean)
        Dim appDataDir = Path.Combine(Application.StartupPath, "appdata")
        Dim xfoilPath = Path.Combine(appDataDir, "xfoil.exe")
        Dim alreadyInstalled = File.Exists(xfoilPath)

        If alreadyInstalled AndAlso Not forcePrompt Then Return True

        Dim promptMsg = If(alreadyInstalled,
                            "XFOIL is already installed. Re-download the latest version from MIT's official repository and overwrite it?",
                            "XFOIL is not found in the appdata folder. Would you like to download it from MIT's official repository now?")
        Dim response = AppMessageBox.Show(promptMsg, "Download XFOIL", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        If response <> DialogResult.Yes Then Return alreadyInstalled

        Dim zipPath = Path.Combine(appDataDir, "XFOIL6.99.zip")
        Dim extractDir = Path.Combine(appDataDir, "xfoil_temp")

        If Not Await DownloadFileWithProgressAsync("https://web.mit.edu/drela/Public/web/xfoil/XFOIL6.99.zip", zipPath, "XFOIL") Then
            Return False
        End If

        Try
            If Directory.Exists(extractDir) Then Directory.Delete(extractDir, True)
            ZipFile.ExtractToDirectory(zipPath, extractDir)

            Dim foundFiles = Directory.GetFiles(extractDir, "xfoil.exe", SearchOption.AllDirectories)
            If foundFiles.Length = 0 Then
                AppMessageBox.Show("Downloaded XFOIL but couldn't find xfoil.exe inside the archive.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return False
            End If
            File.Copy(foundFiles(0), xfoilPath, True)

            AppToast.Show("XFOIL downloaded and installed")
            Return True
        Catch ex As Exception
            AppMessageBox.Show("Failed to install XFOIL: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        Finally
            Try
                File.Delete(zipPath)
                If Directory.Exists(extractDir) Then Directory.Delete(extractDir, True)
            Catch
            End Try
        End Try
    End Function

    Public Sub UpdateTitleAndButtons()
        Dim versionStr As String = My.Application.Info.Version.ToString
        Me.Text = $"AERO Console (v{versionStr}) - Project: <{projectName}>"
        
        If curApp.ToLower() = "avl" Then
            btnGeometry.Text = "Load Geometry"
            btnGeometry.ToolTipText = "Load geometry file (.avl) into AVL"
            btnMass.Text = "Load Mass"
            btnMass.ToolTipText = "Load mass file (.mass) into AVL"
            btnRun.Text = "Load Run"
            btnRun.ToolTipText = "Load run case file (.run) into AVL"
            btnDesigner.Visible = True
            If btnClosePlot IsNot Nothing Then btnClosePlot.Visible = False
        Else
            btnGeometry.Text = "Load Airfoil"
            btnGeometry.ToolTipText = "Load airfoil file (.dat) or NACA profile into XFOIL"
            btnMass.Text = "Init Polar"
            btnMass.ToolTipText = "Accumulate airfoil polar results (PACC)"
            btnRun.Text = "Run Alpha"
            btnRun.ToolTipText = "Calculate operational point at specified alpha (ALFA)"
            btnDesigner.Visible = False
            If btnClosePlot IsNot Nothing Then btnClosePlot.Visible = True
        End If

        Dim hasProject = Not String.IsNullOrEmpty(projectName)
        btnGeometry.Enabled = hasProject
        btnMass.Enabled = hasProject
        btnRun.Enabled = hasProject
        If Not hasProject Then
            Dim prompt = "Select or enter a project name above first"
            btnGeometry.ToolTipText = prompt
            btnMass.ToolTipText = prompt
            btnRun.ToolTipText = prompt
        End If
    End Sub

    Public Sub UpdateProjectWarning()
        Dim lbl = TryCast(ToolStrip1.Items("lblWarning"), ToolStripLabel)
        If lbl IsNot Nothing Then
            ' lbl.Text is a fixed string set once at creation ("Using default 'test'
            ' project...") - it was also being shown for an empty projectName, which is
            ' actively misleading (an empty name isn't "test"). Match frmGeometry's
            ' UpdateProjectWarning(): only the literal "test" case is what this message
            ' actually describes.
            lbl.Visible = Not String.IsNullOrEmpty(projectName) AndAlso projectName.ToLower() = "test"
        End If
    End Sub

    Private Sub btnMass_Click(sender As Object, e As EventArgs) Handles btnMass.Click
        If p Is Nothing OrElse p.HasExited Then Return
        Try
            If curApp.ToLower() = "avl" Then
                Dim f = $"{projectName}.mass"
                p.StandardInput.WriteLine($"mass {f}")
            Else
                p.StandardInput.WriteLine("oper")
                p.StandardInput.WriteLine("pacc")
                p.StandardInput.WriteLine($"{projectName}.pol")
                p.StandardInput.WriteLine("") ' default dump file
            End If
            p.StandardInput.Flush()
        Catch ex As Exception
            AppMessageBox.Show("Error: " + ex.Message)
        End Try

    End Sub

    Private Sub btnRun_Click(sender As Object, e As EventArgs) Handles btnRun.Click
        If p Is Nothing OrElse p.HasExited Then Return
        Try
            If curApp.ToLower() = "avl" Then
                Dim f = $"{projectName}.run"
                p.StandardInput.WriteLine($"case {f}")
            Else
                Dim alpha = InputBox("Enter Angle of Attack (alpha) for analysis:", "XFOIL Analysis", "5")
                If Not String.IsNullOrEmpty(alpha) Then
                    txtLog.AppendText(Environment.NewLine & "[AERO Console] Plot window opened. Click the 'Close Plot' button or press Enter in the command box to dismiss it." & Environment.NewLine)
                    txtLog.SelectionStart = txtLog.Text.Length
                    txtLog.ScrollToCaret()
                    
                    p.StandardInput.WriteLine("oper")
                    p.StandardInput.WriteLine($"alfa {alpha}")
                End If
            End If
            p.StandardInput.Flush()
        Catch ex As Exception
            AppMessageBox.Show("Error: " + ex.Message)
        End Try

    End Sub

    Private Sub btnClosePlot_Click(sender As Object, e As EventArgs)
        If p Is Nothing Then Return
        Try
            ' Send two newlines to exit plot/menu
            p.StandardInput.WriteLine()
            p.StandardInput.WriteLine()
            p.StandardInput.Flush()
            
            ' Wait for process to handle the exit commands
            Thread.Sleep(150)
            
            If p.HasExited Then
                loadConsole()
            End If
        Catch
        End Try
    End Sub

    ' Enable double-buffering on Form controls
    Private Sub EnableDoubleBuffering(ctrl As Control)
        Try
            Dim dbProp = GetType(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic Or System.Reflection.BindingFlags.Instance)
            If dbProp IsNot Nothing Then
                dbProp.SetValue(ctrl, True, Nothing)
            End If
        Catch
        End Try
        
        For Each child As Control In ctrl.Controls
            EnableDoubleBuffering(child)
        Next
    End Sub

End Class

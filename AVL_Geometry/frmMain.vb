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
    Public projectName As String = "test"
    Public isSyncing As Boolean = False
    Private cbEngine As ToolStripComboBox = Nothing
    Private btnClosePlot As ToolStripButton = Nothing
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

                Me.Invoke(Sub()
                              txtLog.AppendText(textChunk)
                              txtLog.SelectionStart = txtLog.Text.Length
                              txtLog.ScrollToCaret()
                          End Sub)
            Loop
        Catch
            ' Process exited or stream closed
        End Try
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

        If p.HasExited Then
            MsgBox("Process exited immediately! Exit Code: " & p.ExitCode)
            ' If this pops up, it means the EXE path is wrong, 
            ' or it's blocked by antivirus, or missing a DLL.
        End If

        ' 4. Start the Thread (No need to abort the old one, it died when we did p.Kill)
        bt = New Thread(AddressOf ReadThread)
        bt.IsBackground = True
        bt.Start()

    End Sub



    Private Sub txtCommand_TextChanged(sender As Object, e As EventArgs) Handles txtCommand.TextChanged

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

            ' A. Save the current selection
            Dim currentSelection As String = box.Text

            ' B. Update the list (using BeginUpdate prevents flickering)
            box.BeginUpdate()
            box.Items.Clear()
            For Each item In newItems
                box.Items.Add(item)
            Next

            ' C. Restore selection only if the file still exists
            If box.Items.Contains(currentSelection) Then
                box.Text = currentSelection
            Else
                ' Optional: Select the first item or clear selection
                box.SelectedIndex = -1
            End If
            box.EndUpdate()
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
    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load

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
            MsgBox("Error: " + ex.Message)
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
        Dim insertIndex = ToolStrip1.Items.IndexOf(btnGeometry)
        If insertIndex >= 0 Then
            ToolStrip1.Items.Insert(insertIndex, lblEngine)
            ToolStrip1.Items.Insert(insertIndex + 1, cbEngine)
            ToolStrip1.Items.Insert(insertIndex + 2, New ToolStripSeparator())
        Else
            ToolStrip1.Items.Add(lblEngine)
            ToolStrip1.Items.Add(cbEngine)
            ToolStrip1.Items.Add(New ToolStripSeparator())
        End If

        ' Initialize Close Plot button dynamically
        btnClosePlot = New ToolStripButton("Close Plot")
        btnClosePlot.Name = "btnClosePlot"
        btnClosePlot.Visible = False
        AddHandler btnClosePlot.Click, AddressOf btnClosePlot_Click
        
        Dim designerIndex = ToolStrip1.Items.IndexOf(btnDesigner)
        If designerIndex >= 0 Then
            ToolStrip1.Items.Insert(designerIndex, btnClosePlot)
        Else
            ToolStrip1.Items.Add(btnClosePlot)
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
        '        MsgBox($"Your displace scale factor is {sclX * 100}% in X and {sclY * 100}% in Y direction. Please note that your editor may experience display issues if your display scale factor is not at 100%. Please fix the scale factor before continuing." + vbNewLine + vbNewLine + "See this for help: https://bit.ly/3LzMotW")
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

    Private Sub RestartConsoleToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RestartConsoleToolStripMenuItem.Click
        If Not IsNothing(p) Then
            Try
                If Not p.HasExited Then p.Kill()
                p = Nothing
                txtLog.Text = ""
                loadConsole()
            Catch
            End Try
        End If
    End Sub

    Private Sub frmMain_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
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
                MsgBox("Error: " + ex.Message)
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

    Private Sub txtCommand_KeyDown(sender As Object, e As KeyEventArgs) Handles txtCommand.KeyDown
        ' 1. Safety Check: Make sure the process is actually running
        If p Is Nothing OrElse p.HasExited Then
            Return
        End If

        If e.KeyCode = Keys.Return Then
            e.SuppressKeyPress = True

            Try
                ' 2. Send the command
                p.StandardInput.WriteLine(txtCommand.Text)
                ' 3. CRITICAL: Force the text to be sent immediately
                p.StandardInput.Flush()

                txtCommand.Text = ""
                txtCommand.Select()
            Catch ex As Exception
                Console.WriteLine("Error sending command: " & ex.Message)
            End Try
        End If
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
            'txtCommand.Font = fd1.Font
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
            MsgBox("Error: " + ex.Message)
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
        If isSyncing Then Return
        
        Dim txt = txtName.Text
        If txt = "Enter AVL Project (e.g. glider)" OrElse txt = "Enter NACA (e.g. 2412) or dat file" Then
            projectName = ""
        Else
            projectName = txt
        End If
        
        UpdateTitleAndButtons()
        UpdateProjectWarning()

        ' Sync with open frmGeometry forms
        isSyncing = True
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
            isSyncing = False
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

    Private Async Function DownloadXfoilAsync() As Task(Of Boolean)
        Dim appDataDir = Path.Combine(Application.StartupPath, "appdata")
        Dim xfoilPath = Path.Combine(appDataDir, "xfoil.exe")
        
        If File.Exists(xfoilPath) Then Return True
        
        Dim response = MessageBox.Show("XFOIL is not found in the appdata folder. Would you like to download it from MIT's official repository now?",
                                       "Download XFOIL",
                                       MessageBoxButtons.YesNo,
                                       MessageBoxIcon.Question)
        If response <> DialogResult.Yes Then Return False
        
        If Not Directory.Exists(appDataDir) Then
            Directory.CreateDirectory(appDataDir)
        End If
        
        Dim zipPath = Path.Combine(appDataDir, "XFOIL6.99.zip")
        Dim extractDir = Path.Combine(appDataDir, "xfoil_temp")
        
        Try
            Dim oldText = Me.Text
            Me.Text = "AERO Console - Downloading XFOIL..."
            
            Using client As New System.Net.Http.HttpClient()
                Dim data = Await client.GetByteArrayAsync("https://web.mit.edu/drela/Public/web/xfoil/XFOIL6.99.zip")
                File.WriteAllBytes(zipPath, data)
            End Using
            
            Me.Text = "AERO Console - Extracting XFOIL..."
            
            If Directory.Exists(extractDir) Then
                Directory.Delete(extractDir, True)
            End If
            
            ZipFile.ExtractToDirectory(zipPath, extractDir)
            
            Dim foundFiles = Directory.GetFiles(extractDir, "xfoil.exe", SearchOption.AllDirectories)
            If foundFiles.Length > 0 Then
                File.Copy(foundFiles(0), xfoilPath, True)
            End If
            
            Try
                File.Delete(zipPath)
                Directory.Delete(extractDir, True)
            Catch
            End Try
            
            Me.Text = oldText
            MessageBox.Show("XFOIL has been successfully downloaded and installed!", "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return True
        Catch ex As Exception
            MessageBox.Show("Failed to download XFOIL: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
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
    End Sub

    Public Sub UpdateProjectWarning()
        Dim lbl = TryCast(ToolStrip1.Items("lblWarning"), ToolStripLabel)
        If lbl IsNot Nothing Then
            If String.IsNullOrEmpty(projectName) OrElse projectName.ToLower() = "test" Then
                lbl.Visible = True
            Else
                lbl.Visible = False
            End If
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
            MsgBox("Error: " + ex.Message)
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
            MsgBox("Error: " + ex.Message)
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

    Private Sub txtCommand_Enter(sender As Object, e As EventArgs) Handles txtCommand.Enter
        If (txtCommand.Text.Equals("Type your commands here...")) Then
            txtCommand.Text = ""
        End If

    End Sub

    Private Sub txtCommand_Leave(sender As Object, e As EventArgs) Handles txtCommand.Leave
        txtCommand.Text = "Type your commands here..."
    End Sub
End Class

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
    Dim projectName As String = "test"
    Private Const DESKTOPVERTRES As Integer = &H75
    Private Const DESKTOPHORZRES As Integer = &H76
    <Runtime.InteropServices.DllImport("gdi32.dll")> Private Shared Function GetDeviceCaps(ByVal hdc As IntPtr, ByVal nIndex As Integer) As Integer
    End Function
    Private Sub ReadThread()
        Try
            Do Until Leaving OrElse p Is Nothing OrElse p.HasExited
                Dim line As String = p.StandardOutput.ReadLine()

                If line Is Nothing Then Exit Do

                Me.Invoke(Sub()
                              txtLog.AppendText(line & vbCrLf)
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
            .RedirectStandardError = True
            .RedirectStandardOutput = True
            .RedirectStandardInput = True
            .UseShellExecute = False
            .CreateNoWindow = True
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

    Public Sub finAVLs(path As String)
        Dim files() As String
        files = Directory.GetFiles(path, "*.avl", SearchOption.TopDirectoryOnly)
        txtName.Items.Clear()
        For Each FileName As String In files
            txtName.Items.Add(System.IO.Path.GetFileNameWithoutExtension(FileName))
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

        Try
            Dim fi As FileInfo = New FileInfo(Path.Combine(Application.StartupPath, "appdata.dat"))
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

        If (Not My.Settings.appFont Is Nothing) Then
            systemFont = My.Settings.appFont
        End If

        SetAllControlsFont(Me.Controls, systemFont)
        firstLoad = True
        finAVLs(Environment.CurrentDirectory)


        Using g As Graphics = Graphics.FromHwnd(IntPtr.Zero)
            Dim hdc As IntPtr = g.GetHdc
            Dim TrueScreenSize As New Size(GetDeviceCaps(hdc, DESKTOPHORZRES), GetDeviceCaps(hdc, DESKTOPVERTRES))
            Dim sclX As Single = CSng(Math.Round((TrueScreenSize.Width / Screen.PrimaryScreen.Bounds.Width), 2))
            Dim sclY As Single = CSng(Math.Round((TrueScreenSize.Height / Screen.PrimaryScreen.Bounds.Height), 2))
            g.ReleaseHdc(hdc)

            'show the true screen size
            Dim DPIstr = "Screen Width:  " & TrueScreenSize.Width.ToString & vbLf &
                          "Screen Height: " & TrueScreenSize.Height.ToString & vbLf & vbLf &
                          "Scale X: " & sclX.ToString & vbLf &
                          "Scale Y: " & sclY.ToString
            If (sclX <> 1 Or sclY <> 1) Then
                MsgBox($"Your displace scale factor is {sclX * 100}% in X and {sclY * 100}% in Y direction. Please note that your editor may experience display issues if your display scale factor is not at 100%. Please fix the scale factor before continuing." + vbNewLine + vbNewLine + "See this for help: https://bit.ly/3LzMotW")
            End If

        End Using

        'frmGeometry.Show()


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
                p.StandardInput.WriteLine("") ' force prompt
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
        Try
            Dim f = $"{projectName}.avl"
            p.StandardInput.WriteLine($"load {f}")
        Catch ex As Exception
            MsgBox("Error: " + ex.Message)
        End Try
    End Sub

    Private Sub txtName_TextChanged(sender As Object, e As EventArgs) Handles txtName.TextChanged
        projectName = txtName.Text
        'Me.Text = $"AVL - Designer - working on <{projectName}>"
        Me.btnGeometry.Text = $"Load Geometry" ' ({projectName}.avl)"
        Me.btnMass.Text = $"Load Mass" ' ({projectName}.mass)"
        Me.btnRun.Text = $"Load Run" ' ({projectName}.run)"
    End Sub

    Private Sub btnMass_Click(sender As Object, e As EventArgs) Handles btnMass.Click
        Try
            Dim f = $"{projectName}.mass"
            p.StandardInput.WriteLine($"mass {f}")
        Catch ex As Exception
            MsgBox("Error: " + ex.Message)
        End Try

    End Sub

    Private Sub btnRun_Click(sender As Object, e As EventArgs) Handles btnRun.Click
        Try
            Dim f = $"{projectName}.run"
            p.StandardInput.WriteLine($"case {f}")
        Catch ex As Exception
            MsgBox("Error: " + ex.Message)
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

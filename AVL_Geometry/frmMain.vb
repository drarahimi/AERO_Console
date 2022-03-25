Imports System.IO.Compression
Imports System.IO
Imports System.Reflection
Imports System.ComponentModel

Public Class frmMain
    Public p As Process
    Private Leaving As Boolean
    Private logtext As String = ""
    Private bt As Threading.Thread
    Public updatedpath = Application.StartupPath & "\update.exe"
    Public originalpath = Application.StartupPath & "\AERO_Console.exe"
    Public appUpdateNeeded As Boolean = False
    Public appUpdated As Boolean = False
    Public curApp = "avl"
    Public firstLoad As Boolean = False
    Public Shared systemFont As Font = New Font("Consolas", 14)
    Dim projectName As String = "test"

    Private Sub ReadThread()
        Console.WriteLine("read thread called")
        Dim rLine As String
        Do Until Leaving
            If (p Is Nothing) Then
                Console.WriteLine("error in ReadThread: Process is null")
            End If
            Try
                rLine = p.StandardOutput.Read()
                logtext += (Chr(rLine))
                If p.StandardOutput.Peek = -1 Then
                    Me.Invoke(Sub() txtLog.AppendText(logtext))
                    logtext = ""
                End If
            Catch ex As Exception
                Console.WriteLine("error in ReadThread while reading input " & ex.Message)
            End Try
        Loop
        Try
            Console.WriteLine("closing form")
            Me.Invoke(Sub() frmMain_Closing(New Object, New CancelEventArgs(False)))
        Catch ex As Exception
            Console.WriteLine("error in ReadThread while closing form " & ex.Message)
        End Try
    End Sub
    Public Sub loadConsole()
        If (p Is Nothing) Then
            p = New Process()
            Dim startinfo = New ProcessStartInfo()

            If curApp.ToLower() = "avl" Then
                With startinfo
                    .FileName = Application.StartupPath & "\appdata\avl.exe"
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

                If Not IsNothing(bt) Then bt.Abort()
                bt = New Threading.Thread(AddressOf ReadThread)
                bt.IsBackground = True
                bt.Start()
                p.Start()


            ElseIf curApp.ToLower() = "xfoil" Then
                With startinfo
                    .FileName = Application.StartupPath & "\appdata\xfoil.exe"
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

                If Not IsNothing(bt) Then bt.Abort()
                bt = New Threading.Thread(AddressOf ReadThread)
                bt.IsBackground = True
                bt.Start()
                p.Start()
                'p.StandardInput.AutoFlush = True


            End If

        Else
            Try
                p.Close()
                While (Not p.HasExited)
                    Application.DoEvents()
                End While
                p = Nothing
                loadConsole()
            Catch ex As Exception
                Console.WriteLine("error while closing process: " + ex.Message)
            End Try
        End If

    End Sub



    Private Sub txtCommand_TextChanged(sender As Object, e As EventArgs) Handles txtCommand.TextChanged

    End Sub

    Public Sub finAVLs(path As String)
        Dim files() As String
        files = Directory.GetFiles(path, "*.avl", SearchOption.TopDirectoryOnly)
        txtName.Items.Clear()
        For Each FileName As String In files
            'Console.WriteLine(FileName)
            txtName.Items.Add(FileName.Split("\").Last.Replace(".avl", ""))
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
            Dim fi As FileInfo = New FileInfo(Application.StartupPath + "\appdata.dat")
            Dim b() As Byte = My.Resources.appdata
            If fi.Exists Then fi.Delete()
            File.WriteAllBytes(fi.FullName, b)
            fi.Attributes = FileAttributes.Hidden

            txtLog.Dock = DockStyle.Fill

            Dim d = Application.StartupPath + "\appdata"
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
        'frmGeometry.Show()
    End Sub

    Private Sub AirplaneDesignToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AirplaneDesignToolStripMenuItem.Click
        frmGeometry.Show()
    End Sub

    Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
        frmAbout.Show()
    End Sub

    Private Sub OpenCurrentDirectoryToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles OpenCurrentDirectoryToolStripMenuItem.Click
        Process.Start(Application.StartupPath)
    End Sub

    Private Sub RestartConsoleToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RestartConsoleToolStripMenuItem.Click
        If Not IsNothing(p) Then
            Try
                p.Kill()
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
                        p.Kill()
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
                        'Dim pinfo As Process = New Process
                        'Dim startinfo As ProcessStartInfo = New ProcessStartInfo()
                        'With startinfo
                        '    .FileName = "cmd.exe"
                        '    .Arguments = ""
                        '    .WorkingDirectory = Application.StartupPath
                        '    .UseShellExecute = False
                        '    .RedirectStandardInput = True
                        '    .CreateNoWindow = True
                        'End With
                        'pinfo.StartInfo = startinfo
                        'pinfo.Start()
                        'pinfo.WaitForExit()
                    End Try
                Loop
            End If

        End If
    End Sub

    Private Sub txtCommand_KeyDown(sender As Object, e As KeyEventArgs) Handles txtCommand.KeyDown
        If e.KeyCode = Keys.Return Then
            e.SuppressKeyPress = True
            p.StandardInput.WriteLine(txtCommand.Text)
            txtCommand.Text = ""
            txtCommand.Select()
        End If

    End Sub

    Private Sub CheckForUpdatesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CheckForUpdatesToolStripMenuItem.Click
        frmUpdate.Show()
    End Sub

    Private Sub frmMain_Closed(sender As Object, e As EventArgs) Handles Me.Closed
    End Sub

    Private Sub btnAVL_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub btnAVL_Click_1(sender As Object, e As EventArgs) Handles btnAVL.Click
        If Not firstLoad Then Exit Sub

        If btnAVL.Checked Then
            btnXFoil.Checked = False
            curApp = "avl"
            RestartConsoleToolStripMenuItem_Click(sender, e)
        Else
            btnXFoil.Checked = True
            curApp = "xfoil"
            RestartConsoleToolStripMenuItem_Click(sender, e)
        End If
    End Sub

    Private Sub btnAVL_CheckStateChanged(sender As Object, e As EventArgs) Handles btnAVL.CheckStateChanged

    End Sub

    Private Sub btnXFoil_Click(sender As Object, e As EventArgs) Handles btnXFoil.Click
        If Not firstLoad Then Exit Sub

        If btnXFoil.Checked Then
            btnAVL.Checked = False
            curApp = "xfoil"
            RestartConsoleToolStripMenuItem_Click(sender, e)
        Else
            btnAVL.Checked = True
            curApp = "avl"
            RestartConsoleToolStripMenuItem_Click(sender, e)
        End If

    End Sub


    Private Sub btnXFoil_CheckStateChanged(sender As Object, e As EventArgs) Handles btnXFoil.CheckStateChanged
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
            Dim f = Application.StartupPath + $"\{projectName}.avl"
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
            Dim f = Application.StartupPath + $"\{projectName}.mass"
            p.StandardInput.WriteLine($"mass {f}")
        Catch ex As Exception
            MsgBox("Error: " + ex.Message)
        End Try

    End Sub

    Private Sub btnRun_Click(sender As Object, e As EventArgs) Handles btnRun.Click
        Try
            Dim f = Application.StartupPath + $"\{projectName}.run"
            p.StandardInput.WriteLine($"case {f}")
        Catch ex As Exception
            MsgBox("Error: " + ex.Message)
        End Try

    End Sub
End Class

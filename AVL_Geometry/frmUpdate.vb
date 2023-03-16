Imports System.Net
Imports System.Reflection
Imports System.IO
Imports System.ComponentModel
Imports System.Runtime.CompilerServices

Public Class frmUpdate
    Dim mainurl = "https://github.com/drarahimi/AERO_Console/releases/latest"
    Dim url = "https://github.com/drarahimi/AERO_Console/releases/download/production/AERO_Console.exe" '"https://onedrive.live.com/download?cid=69DFBF70939557A5&resid=69DFBF70939557A5%21238401&authkey=AHrbgMu6AdSRKTc"
    Dim path As String
    Dim latestserver As String
    Dim isuptodate As Boolean
    Dim gitpath As String

    Private Sub downloadFile()

        lblStat.Text = "Checking for updates..."
        bg3.RunWorkerAsync()

    End Sub

    Private Sub downloadFile_ProgressChanged(ByVal sender As Object, ByVal e As DownloadProgressChangedEventArgs)
        Console.WriteLine(e.TotalBytesToReceive * 10 ^ -6 & " MB")
        lblStat.Text = e.ProgressPercentage & "%"
        Dim fi As FileInfo = New FileInfo(path)
        fi.Attributes = FileAttributes.Hidden
        Application.DoEvents()
    End Sub

    Private Sub downloadFile_Completed(ByVal sender As Object, ByVal e As System.ComponentModel.AsyncCompletedEventArgs)

        lblStat.Text = e.Cancelled
        'download completed
        lblStat.Text = "Download completed!"
        Application.DoEvents()
        System.Threading.Thread.Sleep(1000)

        lblStat.Text = "Restarting the app to finish update..."
        Application.DoEvents()

        System.Threading.Thread.Sleep(1000)

        My.Settings.appUpdateNeeded = True
        My.Settings.Save()
        frmMain.Close()
        'lblStat.Text = "Cleaning up..."
        'BackgroundWorker2.RunWorkerAsync()


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

    Private Sub frmUpdate_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim DownloadsFolderPath As String = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
        'path = DownloadsFolderPath & "\" & Application.ExecutablePath.Split("\").Last.Replace(".exe", "_u.exe")
        path = frmMain.updatedpath ' Application.ExecutablePath.Replace(".exe", "_u.exe")


        frmUpdate.SetAllControlsFont(Me.Controls, frmMain.systemFont)

        downloadFile()

        'curl()
    End Sub

    Private Sub BackgroundWorker2_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles bg2.DoWork
        While (File.Exists(path))
            Try
                IO.File.Delete(path)
            Catch
            End Try
        End While
    End Sub

    Private Sub BackgroundWorker2_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles bg2.RunWorkerCompleted
        System.Threading.Thread.Sleep(2000)
        Me.Dispose()
    End Sub

    Private Sub lblStat_Click(sender As Object, e As EventArgs) Handles lblStat.Click

    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click

    End Sub

    Private Sub bg3_DoWork(sender As Object, e As DoWorkEventArgs) Handles bg3.DoWork
        'downloadFile(path)

        Dim request As HttpWebRequest = DirectCast(HttpWebRequest.Create("https://github.com/drarahimi/AERO_Console/releases/latest/"), HttpWebRequest)
        request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/13.0.782.112 Safari/535.1"
        request.MaximumAutomaticRedirections = 1
        request.AllowAutoRedirect = True
        gitpath = request.GetResponse().ResponseUri.ToString()
        bg3.ReportProgress(0, gitpath)

        latestserver = gitpath.Split("/").Last()
        isuptodate = Application.ProductVersion >= latestserver

        Debug.WriteLine($"latest version is: {latestserver} and current version is {Application.ProductVersion}, which means the application is up-to-date: {isuptodate}")
        Debug.WriteLine($"gitpath: {gitpath}")



    End Sub
    Private Sub bg3_ProgressChanged(sender As Object, e As ProgressChangedEventArgs) Handles bg3.ProgressChanged
        lblStat.Text = e.UserState
        Application.DoEvents()
    End Sub
    Private Sub bg3_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles bg3.RunWorkerCompleted
        lblStat.Text = "local version: " & My.Application.Info.Version.ToString & " | server version: " & latestserver
        lblStat.Text &= vbNewLine & vbNewLine & "Update" & IIf(isuptodate, " is not ", " is ") & "required"

        Application.DoEvents()
        System.Threading.Thread.Sleep(2000)



        If (isuptodate) Then
            'MsgBox("Update is Not required")
            lblStat.Text = "Closing update window"
            Application.DoEvents()
            bg2.RunWorkerAsync()

        Else
            System.Threading.Thread.Sleep(1000)

            Dim srcPath As String = gitpath.Replace("tag", "download") + "/aero_console.exe"

            lblStat.Text = "Download started"

            Dim wClient As New System.Net.WebClient()
            wClient.Headers.Add("user-agent", "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.9.2.12) Gecko/20101026 Firefox/3.6.12 (.NET CLR 3.5.30729)")
            AddHandler wClient.DownloadProgressChanged, AddressOf downloadFile_ProgressChanged
            AddHandler wClient.DownloadFileCompleted, AddressOf downloadFile_Completed
            Application.DoEvents()

            Using wClient.OpenRead(srcPath)
                For Each s As String In wClient.ResponseHeaders
                    Console.WriteLine(s & ": " & wClient.ResponseHeaders.Item(s))
                Next
            End Using

            wClient.DownloadFileAsync(New System.Uri(srcPath), path)
        End If
    End Sub


End Class
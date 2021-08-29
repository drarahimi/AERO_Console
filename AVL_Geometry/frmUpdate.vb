Imports System.Net
Imports System.Reflection
Imports System.IO
Imports System.ComponentModel

Public Class frmUpdate

    Dim url = "https://onedrive.live.com/download?cid=69DFBF70939557A5&resid=69DFBF70939557A5%21238401&authkey=AHrbgMu6AdSRKTc"
    Dim path As String

    Private Sub downloadFile(ByVal srcPath As String, ByVal destPath As String)

        lblStat.Text = "Download started"

        Dim wClient As New System.Net.WebClient()
        'wClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)")
        wClient.Headers.Add("user-agent", "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.9.2.12) Gecko/20101026 Firefox/3.6.12 (.NET CLR 3.5.30729)")
        AddHandler wClient.DownloadProgressChanged, AddressOf downloadFile_ProgressChanged
        AddHandler wClient.DownloadFileCompleted, AddressOf downloadFile_Completed
        'My.Computer.Network.DownloadFile(New System.Uri(srcPath), destPath, Nothing, True, 500, True)
        Application.DoEvents()

        Using wClient.OpenRead(srcPath)
            For Each s As String In wClient.ResponseHeaders
                Console.WriteLine(s & ": " & wClient.ResponseHeaders.Item(s))
            Next
        End Using

        wClient.DownloadFileAsync(New System.Uri(srcPath), destPath)

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
        System.Threading.Thread.Sleep(2000)

        Dim FileVer As String = AssemblyName.GetAssemblyName(path).Version.ToString

        'AppDomain.Unload(dom)

        Dim req As Boolean = (String.Compare(My.Application.Info.Version.ToString, FileVer) < 0)
        lblStat.Text = "local version: " & My.Application.Info.Version.ToString & " | server version: " & FileVer
        lblStat.Text &= vbNewLine & vbNewLine & "Update" & IIf(req, " is ", " is not ") & "required"
        Application.DoEvents()

        System.Threading.Thread.Sleep(2000)

        lblStat.Text = IIf(req, "Restarting the app to finish update...", "Preparing for clean up...")
        Application.DoEvents()

        System.Threading.Thread.Sleep(2000)

        If req Then
            'System.Threading.Thread.Sleep(2000)
            My.Settings.appUpdateNeeded = True
            My.Settings.Save()
            frmMain.Close()
            'Application.Restart()
        Else
            lblStat.Text = "Cleaning up..."
            BackgroundWorker2.RunWorkerAsync()
        End If


    End Sub


    Private Sub frmUpdate_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim DownloadsFolderPath As String = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
        'path = DownloadsFolderPath & "\" & Application.ExecutablePath.Split("\").Last.Replace(".exe", "_u.exe")
        path = frmMain.updatedpath ' Application.ExecutablePath.Replace(".exe", "_u.exe")
        downloadFile(url, path)
    End Sub

    Private Sub BackgroundWorker2_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker2.DoWork
        While (File.Exists(path))
            Try
                IO.File.Delete(path)
            Catch
            End Try
        End While
    End Sub

    Private Sub BackgroundWorker2_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles BackgroundWorker2.RunWorkerCompleted
        System.Threading.Thread.Sleep(2000)
        Me.Dispose()
    End Sub

    Private Sub lblStat_Click(sender As Object, e As EventArgs) Handles lblStat.Click

    End Sub
End Class
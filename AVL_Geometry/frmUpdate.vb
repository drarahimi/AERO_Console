Imports System.Net.Http
Imports System.IO
Imports System.Reflection
Imports System.Threading.Tasks

Public Class frmUpdate

    ' Configuration
    Private Const GITHUB_BASE_URL As String = "https://github.com/drarahimi/AERO_Console/releases/latest"
    Private Const USER_AGENT As String = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AERO_Console_Updater"

    ' State
    Private _downloadPath As String
    Private _httpClient As HttpClient

    Public Sub New()
        InitializeComponent()
        ' Initialize HttpClient once to prevent socket exhaustion
        _httpClient = New HttpClient()
        _httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT)
    End Sub

    Private Async Sub frmUpdate_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' 1. Setup UI and Paths
        ApplyFontRecursive(Me.Controls, frmMain.systemFont)
        _downloadPath = frmMain.updatedpath

        ' 2. Clean up old updates if they exist
        CleanUpOldFiles(_downloadPath)

        ' 3. Start the process
        Await CheckAndDownloadUpdateAsync()
    End Sub

    Private Async Function CheckAndDownloadUpdateAsync() As Task
        Try
            lblStat.Text = "Checking for updates..."

            ' --- STEP 1: Get Redirect URL (Latest Version) ---
            Dim response = Await _httpClient.GetAsync(GITHUB_BASE_URL, HttpCompletionOption.ResponseHeadersRead)
            Dim finalUrl As String = response.RequestMessage.RequestUri.ToString()

            ' Extract version tag (assuming url ends in /tag/v1.2.3)
            Dim latestTag As String = finalUrl.Split("/"c).Last()
            ' Remove 'v' if present for cleaner parsing (e.g. v1.0 -> 1.0)
            Dim cleanVersionStr As String = latestTag.Replace("v", "").Trim()

            Dim serverVersion As Version = Nothing
            Dim localVersion As Version = My.Application.Info.Version

            ' Safely parse version
            Version.TryParse(cleanVersionStr, serverVersion)

            Debug.WriteLine($"Local: {localVersion} | Server: {serverVersion}")

            ' --- STEP 2: Compare Versions ---
            Dim isUpdateAvailable As Boolean = (serverVersion IsNot Nothing AndAlso serverVersion > localVersion)

            lblStat.Text = $"Local: {localVersion} | Server: {serverVersion}" & Environment.NewLine &
                           If(isUpdateAvailable, "Update found!", "You are up to date.")

            Await Task.Delay(1500) ' Short pause for user readability

            If Not isUpdateAvailable Then
                CloseFormSafe()
                Return
            End If

            ' --- STEP 3: Download ---
            ' Construct the direct download link based on your logic: replace 'tag' with 'download' + filename
            Dim downloadUrl As String = finalUrl.Replace("tag", "download") & "/AERO_Console.exe"

            lblStat.Text = "Downloading update..."
            Dim progressIndicator As New Progress(Of Integer)(Sub(p)
                                                                  lblStat.Text = $"Downloading... {p}%"
                                                                  ' If you have a ProgressBar, set it here: pbUpdate.Value = p
                                                              End Sub)

            Await DownloadFileAsync(downloadUrl, _downloadPath, progressIndicator)

            ' --- STEP 4: Apply Update ---
            lblStat.Text = "Download complete. Restarting..."
            Await Task.Delay(1000)

            ' Hide file attribute if needed
            Dim fi As New FileInfo(_downloadPath)
            fi.Attributes = FileAttributes.Hidden

            My.Settings.appUpdateNeeded = True
            My.Settings.Save()

            frmMain.Close() ' Triggers app shutdown logic

        Catch ex As Exception
            lblStat.Text = "Error: " & ex.Message
            Debug.WriteLine("Update Error: " & ex.ToString())
            'Await Task.Delay(3000)
            CloseFormSafe()
        End Try
    End Function

    ''' <summary>
    ''' Downloads a file with progress reporting using HttpClient.
    ''' </summary>
    Private Async Function DownloadFileAsync(url As String, destinationPath As String, progress As IProgress(Of Integer)) As Task
        Using response = Await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
            response.EnsureSuccessStatusCode()

            Dim totalBytes As Long = response.Content.Headers.ContentLength.GetValueOrDefault(-1L)
            Dim canReportProgress As Boolean = totalBytes <> -1

            Using contentStream = Await response.Content.ReadAsStreamAsync(),
                  fileStream = New FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, True)

                Dim totalRead As Long = 0
                Dim buffer(8191) As Byte
                Dim isMoreToRead As Boolean = True

                Do
                    Dim read As Integer = Await contentStream.ReadAsync(buffer, 0, buffer.Length)
                    If read = 0 Then
                        isMoreToRead = False
                    Else
                        Await fileStream.WriteAsync(buffer, 0, read)
                        totalRead += read
                        If canReportProgress Then
                            progress.Report(CInt((totalRead * 100) / totalBytes))
                        End If
                    End If
                Loop While isMoreToRead
            End Using
        End Using
    End Function

    Private Sub CleanUpOldFiles(filePath As String)
        If File.Exists(filePath) Then
            Try
                File.Delete(filePath)
            Catch ex As Exception
                Debug.WriteLine("Could not delete old temporary file: " & ex.Message)
            End Try
        End If
    End Sub

    Private Sub CloseFormSafe()
        If Not Me.IsDisposed Then Me.Close()
    End Sub

    ''' <summary>
    ''' Recursively sets fonts, cleaned up for recursion efficiency.
    ''' </summary>
    Public Shared Sub ApplyFontRecursive(ctrls As Control.ControlCollection, font As Font)
        For Each ctrl As Control In ctrls
            ctrl.Font = font
            If ctrl.HasChildren Then
                ApplyFontRecursive(ctrl.Controls, font)
            End If
        Next
    End Sub

    ' Cleanup HttpClient when form closes
    Private Sub frmUpdate_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed
        _httpClient?.Dispose()
    End Sub

End Class
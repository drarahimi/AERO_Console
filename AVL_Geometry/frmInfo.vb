Public Class frmInfo
    Private Sub frmInfo_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If e.CloseReason <> CloseReason.None Then
            frmMain.RestartConsoleToolStripMenuItem.PerformClick()
        End If
    End Sub

    Private Sub frmInfo_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        lb1.Dock = DockStyle.Fill
        Me.Icon = frmMain.Icon
        Me.Left = Screen.PrimaryScreen.WorkingArea.Width - Me.Width
    End Sub
End Class
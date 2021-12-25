Public Class frmInfo
    Private Sub frmInfo_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If e.CloseReason <> CloseReason.None Then
            frmMain.RestartConsoleToolStripMenuItem.PerformClick()
        End If
    End Sub
End Class
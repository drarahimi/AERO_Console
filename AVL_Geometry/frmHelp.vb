Public Class frmHelp
    Private Sub frmHelp_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        txt1.Dock = DockStyle.Fill
        frmMain.SetAllControlsFont(Me.Controls, My.Settings.appFont)
    End Sub
End Class
Public Class frmHelp
    Private Sub frmHelp_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        txt1.Dock = DockStyle.Fill
        If Not IsNothing(frmMain.systemFont) Then
            frmMain.SetAllControlsFont(Me.Controls, frmMain.systemFont)
        Else
            Dim newFont = New Font(FontFamily.GenericMonospace, frmMain.Font.Size)
            frmMain.SetAllControlsFont(Me.Controls, newFont)
        End If
    End Sub
End Class
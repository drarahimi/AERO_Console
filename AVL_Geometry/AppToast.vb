Option Explicit On
Option Strict On

Imports System.Drawing

''' <summary>
''' A small, non-blocking notification for things that don't need a decision or even
''' acknowledgment - "exported successfully", "project copied", etc. Fades in near the bottom-
''' right corner of the active window, stays a few seconds, fades out, and closes itself; the
''' user never has to click anything. Use AppMessageBox instead for anything that needs a
''' response (confirmations, errors, questions).
''' </summary>
Public NotInheritable Class AppToast
    Private Sub New()
    End Sub

    Public Shared Sub Show(message As String, Optional icon As MessageBoxIcon = MessageBoxIcon.Information, Optional durationMs As Integer = 2600)
        Dim owner = System.Windows.Forms.Form.ActiveForm
        Dim toast As New AppToastForm(message, icon, durationMs, owner)
        toast.Show()
    End Sub
End Class

Friend Class AppToastForm
    Inherits System.Windows.Forms.Form

    Private ReadOnly _showTimer As New System.Windows.Forms.Timer()
    Private ReadOnly _fadeTimer As New System.Windows.Forms.Timer()
    Private _fadingOut As Boolean = False

    Public Sub New(message As String, icon As MessageBoxIcon, durationMs As Integer, owner As System.Windows.Forms.Form)
        ' See the same fix on AppMessageBoxDialog: this form has no Designer-generated
        ' InitializeComponent, so AutoScaleMode.Font (the default) has no proper baseline to
        ' rescale against and can silently distort the hand-computed pixel layout below.
        AutoScaleMode = AutoScaleMode.None

        Dim isDark = My.Settings.DarkTheme

        FormBorderStyle = FormBorderStyle.None
        ShowInTaskbar = False
        StartPosition = FormStartPosition.Manual
        TopMost = True
        BackColor = If(isDark, Color.FromArgb(45, 45, 45), Color.FromArgb(250, 250, 250))
        Opacity = 0.0R

        Dim fg As Color = If(isDark, Color.White, Color.Black)
        Dim accent As Color
        Dim glyph As String
        Select Case icon
            Case MessageBoxIcon.Warning
                accent = Color.FromArgb(230, 150, 20) : glyph = "!"
            Case MessageBoxIcon.Error
                accent = Color.FromArgb(210, 60, 60) : glyph = ChrW(&HD7)
            Case Else
                accent = Color.FromArgb(60, 170, 90) : glyph = ChrW(&H2713) ' checkmark
        End Select

        Dim stripe As New System.Windows.Forms.Panel() With {.Dock = DockStyle.Left, .Width = 4, .BackColor = accent}
        Controls.Add(stripe)

        Dim iconLbl As New System.Windows.Forms.Label() With {
            .Text = glyph,
            .AutoSize = False,
            .Size = New Size(26, 26),
            .Location = New Point(14, 13),
            .TextAlign = ContentAlignment.MiddleCenter,
            .ForeColor = accent,
            .Font = New Font("Segoe UI", 13.0F, FontStyle.Bold)
        }
        Controls.Add(iconLbl)

        ' Explicitly measured rather than AutoSize=True - see the comment on AppMessageBoxDialog's
        ' message Label for why relying on a freshly-constructed AutoSize control's computed
        ' size can be wrong before it's been through a real layout pass.
        Dim msgFont As New Font("Segoe UI", 9.5F)
        Const maxTextWidth As Integer = 280
        Dim measured = System.Windows.Forms.TextRenderer.MeasureText(message, msgFont, New Size(maxTextWidth, Integer.MaxValue), System.Windows.Forms.TextFormatFlags.WordBreak)

        Dim msgLbl As New System.Windows.Forms.Label() With {
            .Text = message,
            .AutoSize = False,
            .Size = New Size(Math.Min(maxTextWidth, measured.Width + 4), measured.Height + 4),
            .Location = New Point(48, 15),
            .ForeColor = fg,
            .Font = msgFont
        }
        Controls.Add(msgLbl)

        Dim w = Math.Max(180, msgLbl.Right + 16)
        Dim h = Math.Max(52, msgLbl.Bottom + 14)
        ClientSize = New Size(w, h)

        Dim workArea = If(owner IsNot Nothing, Screen.FromControl(owner).WorkingArea, Screen.PrimaryScreen.WorkingArea)
        Location = New Point(workArea.Right - Width - 20, workArea.Bottom - Height - 20)

        AddHandler MyBase.Load, Sub(s, e) _fadeTimer.Start()

        _fadeTimer.Interval = 30
        AddHandler _fadeTimer.Tick, AddressOf FadeTick

        _showTimer.Interval = durationMs
        AddHandler _showTimer.Tick, Sub(s, e)
                                         _showTimer.Stop()
                                         _fadingOut = True
                                         _fadeTimer.Start()
                                     End Sub
    End Sub

    Private Sub FadeTick(sender As Object, e As EventArgs)
        If Not _fadingOut Then
            Opacity = Math.Min(1.0R, Opacity + 0.12R)
            If Opacity >= 1.0R Then
                _fadeTimer.Stop()
                _showTimer.Start()
            End If
        Else
            Opacity = Math.Max(0.0R, Opacity - 0.08R)
            If Opacity <= 0.0R Then
                _fadeTimer.Stop()
                Close()
            End If
        End If
    End Sub

    Protected Overrides Sub OnFormClosed(e As System.Windows.Forms.FormClosedEventArgs)
        _showTimer.Dispose()
        _fadeTimer.Dispose()
        MyBase.OnFormClosed(e)
    End Sub

    Protected Overrides Sub OnPaint(e As System.Windows.Forms.PaintEventArgs)
        MyBase.OnPaint(e)
        Using p As New Pen(Color.FromArgb(60, 0, 0, 0))
            e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1)
        End Using
    End Sub
End Class

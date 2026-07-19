Option Explicit On
Option Strict On

Imports System.Drawing

''' <summary>
''' Drop-in replacement for MessageBox.Show, styled to match the rest of the app (flat white/
''' WhiteSmoke buttons, Segoe UI, light/dark theme via the same My.Settings.DarkTheme flag every
''' other themed view in the app already reads) instead of the raw OS message box. Same call
''' signatures as MessageBox.Show, so existing call sites only need "MessageBox.Show(" swapped
''' for "AppMessageBox.Show(" - no argument changes required.
''' </summary>
Public NotInheritable Class AppMessageBox
    Private Sub New()
    End Sub

    Public Shared Function Show(text As String) As DialogResult
        Return ShowCore(text, "", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1)
    End Function

    Public Shared Function Show(text As String, caption As String) As DialogResult
        Return ShowCore(text, caption, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1)
    End Function

    Public Shared Function Show(text As String, caption As String, buttons As MessageBoxButtons) As DialogResult
        Return ShowCore(text, caption, buttons, MessageBoxIcon.None, MessageBoxDefaultButton.Button1)
    End Function

    Public Shared Function Show(text As String, caption As String, buttons As MessageBoxButtons, icon As MessageBoxIcon) As DialogResult
        Return ShowCore(text, caption, buttons, icon, MessageBoxDefaultButton.Button1)
    End Function

    Public Shared Function Show(text As String, caption As String, buttons As MessageBoxButtons, icon As MessageBoxIcon, defaultButton As MessageBoxDefaultButton) As DialogResult
        Return ShowCore(text, caption, buttons, icon, defaultButton)
    End Function

    Private Shared Function ShowCore(text As String, caption As String, buttons As MessageBoxButtons, icon As MessageBoxIcon, defaultButton As MessageBoxDefaultButton) As DialogResult
        Using dlg As New AppMessageBoxDialog(text, caption, buttons, icon, defaultButton)
            Dim owner = System.Windows.Forms.Form.ActiveForm
            If owner IsNot Nothing AndAlso owner IsNot dlg Then
                Return dlg.ShowDialog(owner)
            End If
            Return dlg.ShowDialog()
        End Using
    End Function
End Class

''' <summary>
''' The actual dialog behind AppMessageBox.Show - not meant to be used directly, use
''' AppMessageBox.Show instead. Built entirely in code (no Designer) to match how most of this
''' app's UI is constructed.
''' </summary>
Friend Class AppMessageBoxDialog
    Inherits System.Windows.Forms.Form

    Public Sub New(text As String, caption As String, buttons As MessageBoxButtons, icon As MessageBoxIcon, Optional defaultButton As MessageBoxDefaultButton = MessageBoxDefaultButton.Button1)
        ' Must be set before anything else: this form has no Designer-generated
        ' InitializeComponent, so WinForms never gets a proper design-time baseline for
        ' AutoScaleMode.Font (the default) to rescale against - left enabled, it can silently
        ' rescale/reposition every control after the constructor runs, distorting or
        ' collapsing the pixel layout computed below (this is what was making the message
        ' text disappear).
        AutoScaleMode = AutoScaleMode.None

        Dim isDark = My.Settings.DarkTheme
        Dim fg As Color = If(isDark, Color.White, Color.Black)
        Dim bg As Color = If(isDark, Color.FromArgb(32, 32, 32), Color.White)

        ' Me.Text, not Text - "Text" alone here would resolve to the "text" constructor
        ' parameter (VB is case-insensitive, and a parameter shadows an inherited member of
        ' the same name), silently overwriting the message with the caption instead of
        ' setting the form's title bar. This was the actual bug behind the message body
        ' appearing to show the caption instead of the real message.
        Me.Text = If(String.IsNullOrEmpty(caption), "AERO Console", caption)
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        ShowInTaskbar = False
        StartPosition = FormStartPosition.CenterParent
        Font = New Font("Segoe UI", 9.0F)
        BackColor = bg
        KeyPreview = True

        ' --- Icon (owner-drawn circle + glyph, no external image assets - consistent with
        ' the rest of the app's owner-drawn tooltips/gizmos rather than embedding bitmaps) ---
        Dim iconChar As String = ""
        Dim iconColor As Color = fg
        Select Case icon
            Case MessageBoxIcon.Information
                iconChar = "i" : iconColor = Color.FromArgb(0, 120, 215)
            Case MessageBoxIcon.Warning
                iconChar = "!" : iconColor = Color.FromArgb(230, 150, 20)
            Case MessageBoxIcon.Error
                iconChar = ChrW(&HD7) : iconColor = Color.FromArgb(210, 60, 60) ' multiplication sign, reads as a clean "x"
            Case MessageBoxIcon.Question
                iconChar = "?" : iconColor = Color.FromArgb(0, 120, 215)
        End Select

        Dim leftMargin As Integer = 20
        Dim textLeft As Integer = 20

        If Not String.IsNullOrEmpty(iconChar) Then
            Dim pic As New System.Windows.Forms.PictureBox() With {.Size = New Size(40, 40), .Location = New Point(leftMargin, 22)}
            AddHandler pic.Paint, Sub(s, e)
                                       e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                                       e.Graphics.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit
                                       Using b As New SolidBrush(iconColor)
                                           e.Graphics.FillEllipse(b, 0, 0, 40, 40)
                                       End Using
                                       Using f As New Font("Georgia", 17.0F, FontStyle.Bold)
                                           Dim sz = e.Graphics.MeasureString(iconChar, f)
                                           e.Graphics.DrawString(iconChar, f, Brushes.White, (40 - sz.Width) / 2.0F, (40 - sz.Height) / 2.0F - 1)
                                       End Using
                                   End Sub
            Controls.Add(pic)
            textLeft = pic.Right + 16
        End If

        ' --- Message text ---
        ' Explicitly measured (AutoSize=False, sized from TextRenderer.MeasureText) instead of
        ' relying on Label.AutoSize: reading a freshly-constructed AutoSize control's computed
        ' .Height/.Bottom immediately after construction can return a stale pre-layout value
        ' (the same class of bug fixed for the Polar tab's overlapping fields) - for a short
        ' message that stale value is close enough to go unnoticed, but for a long multi-
        ' paragraph message (e.g. the Dynamics tab's stability tips) it can be drastically too
        ' small, so the button row below ends up positioned over most of the actual text instead
        ' of below it.
        Dim msgFont As New Font("Segoe UI", 9.5F)
        Const maxTextWidth As Integer = 360
        Dim measured = System.Windows.Forms.TextRenderer.MeasureText(text, msgFont, New Size(maxTextWidth, Integer.MaxValue), System.Windows.Forms.TextFormatFlags.WordBreak)

        Dim lbl As New System.Windows.Forms.Label() With {
            .Text = text,
            .AutoSize = False,
            .Size = New Size(Math.Min(maxTextWidth, measured.Width + 4), measured.Height + 4),
            .Location = New Point(textLeft, 26),
            .ForeColor = fg,
            .Font = msgFont
        }
        Controls.Add(lbl)

        Dim bottomOfContent = Math.Max(lbl.Bottom, 22 + 40) + 24
        Dim rightOfContent = Math.Max(lbl.Right, textLeft) + leftMargin

        ' --- Buttons (flat, matching the app's established button styling) ---
        Dim btnPanel As New System.Windows.Forms.FlowLayoutPanel() With {
            .FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft,
            .Location = New Point(0, bottomOfContent),
            .Size = New Size(rightOfContent, 40),
            .WrapContents = False
        }

        Dim MakeButton = Function(label As String, result As DialogResult) As System.Windows.Forms.Button
                              Dim btn As New System.Windows.Forms.Button() With {
                                  .Text = label,
                                  .DialogResult = result,
                                  .Size = New Size(92, 28),
                                  .FlatStyle = FlatStyle.Flat,
                                  .BackColor = If(isDark, Color.FromArgb(60, 60, 60), Color.White),
                                  .ForeColor = fg,
                                  .Cursor = Cursors.Hand,
                                  .Margin = New Padding(8, 4, 0, 4)
                              }
                              btn.FlatAppearance.BorderColor = If(isDark, Color.FromArgb(90, 90, 90), Color.LightGray)
                              btn.FlatAppearance.BorderSize = 1
                              Return btn
                          End Function

        ' "logicalOrder" matches standard MessageBox semantics for MessageBoxDefaultButton
        ' (Button1/2/3 = first/second/third in this logical, not visual, order) - the
        ' FlowLayoutPanel then lays them out right-to-left as usual.
        Dim logicalOrder As New List(Of System.Windows.Forms.Button)
        Dim cancelBtn As System.Windows.Forms.Button = Nothing

        Select Case buttons
            Case MessageBoxButtons.OKCancel
                Dim ok = MakeButton("OK", DialogResult.OK)
                Dim cancel = MakeButton("Cancel", DialogResult.Cancel)
                logicalOrder.Add(ok) : logicalOrder.Add(cancel)
                cancelBtn = cancel
            Case MessageBoxButtons.YesNo
                Dim yes = MakeButton("Yes", DialogResult.Yes)
                Dim no = MakeButton("No", DialogResult.No)
                logicalOrder.Add(yes) : logicalOrder.Add(no)
                cancelBtn = no
            Case MessageBoxButtons.YesNoCancel
                Dim yes = MakeButton("Yes", DialogResult.Yes)
                Dim no = MakeButton("No", DialogResult.No)
                Dim cancel = MakeButton("Cancel", DialogResult.Cancel)
                logicalOrder.Add(yes) : logicalOrder.Add(no) : logicalOrder.Add(cancel)
                cancelBtn = cancel
            Case Else ' OK
                Dim ok = MakeButton("OK", DialogResult.OK)
                logicalOrder.Add(ok)
                cancelBtn = ok
        End Select

        ' Add to the panel in reverse logical order so FlowDirection.RightToLeft still puts
        ' the first logical button (Yes/OK) on the right, matching the native MessageBox.
        For i = logicalOrder.Count - 1 To 0 Step -1
            btnPanel.Controls.Add(logicalOrder(i))
        Next

        Dim defaultIndex = Math.Min(logicalOrder.Count - 1, Math.Max(0,
            CInt(defaultButton.ToString().Replace("Button", "")) - 1))
        Dim defaultBtn = logicalOrder(defaultIndex)
        AcceptButton = defaultBtn
        CancelButton = cancelBtn
        AddHandler Shown, Sub(s, e) defaultBtn.Focus()

        Controls.Add(btnPanel)
        'Dim CSize = New Size(Math.Max(rightOfContent, 280), bottomOfContent + 44)
        'MsgBox(caption + vbNewLine + text + vbNewLine + CSize.Width.ToString + "," + CSize.Height.ToString)
        ClientSize = New Size(Math.Max(rightOfContent, 280), bottomOfContent + 44)
        Size = ClientSize
    End Sub
End Class

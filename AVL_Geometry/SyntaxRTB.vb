Public Class SyntaxRTB
    Inherits System.Windows.Forms.RichTextBox

    Private Declare Function SendMessage Lib "user32" Alias "SendMessageA" _
       (ByVal hWnd As IntPtr, ByVal wMsg As Integer, ByVal wParam As Integer, _
        ByVal lParam As Integer) As Integer
    Private Declare Function LockWindowUpdate Lib "user32" _
        (ByVal hWnd As Integer) As Integer

    Private _SyntaxHighlight_CaseSensitive As Boolean = False
    Friend Words As New DataTable

    'Contains Windows Messages for the SendMessage API call
    Private Enum EditMessages
        LineIndex = 187
        LineFromChar = 201
        GetFirstVisibleLine = 206
        CharFromPos = 215
        PosFromChar = 1062
    End Enum

    Protected Overrides Sub OnTextChanged(ByVal e As System.EventArgs)
        MyBase.OnTextChanged(e)
        'Colorize()
    End Sub

    Public Sub New()
        Me.AcceptsTab = True
        Me.HideSelection = False
    End Sub

    Public Sub Colorize()

        'For i = 0 To Words.Count - 1
        colorWord()

        'Next

    End Sub

    Sub colorWord() ' by im4dbr0

        'On Error Resume Next

        Dim Keys As New List(Of String)
        With Keys
            .Add("IYsym".ToUpper)
            .Add("IZsym".ToUpper)
            .Add("Zsym".ToUpper)
            .Add("Mach".ToUpper)
            .Add("Sref".ToUpper)
            .Add("Cref".ToUpper)
            .Add("Bref".ToUpper)
            .Add("Xref".ToUpper)
            .Add("Yref".ToUpper)
            .Add("Zref".ToUpper)
            .Add("Nchordwise".ToUpper)
            .Add("Cspace".ToUpper)
            .Add("Nspanwise".ToUpper)
            .Add("Sspace".ToUpper)
            .Add("YDUPLICATE".ToUpper)
            .Add("ANGLE".ToUpper)
            .Add("Xle".ToUpper)
            .Add("Yle".ToUpper)
            .Add("Zle".ToUpper)
            .Add("Chord".ToUpper)
            .Add("Ainc".ToUpper)
            .Add("Nspanwise".ToUpper)
            .Add("Sspace".ToUpper)
            .Add("NACA".ToUpper)
            .Add("Cname".ToUpper)
            .Add("Cgain".ToUpper)
            .Add("Xhinge".ToUpper)
            .Add("HingeVec".ToUpper)
            .Add("SgnDup".ToUpper)
        End With

        Dim Funcs As New List(Of String)
        With Funcs
            .Add("SECTION".ToUpper)
            .Add("SURFACE".ToUpper)
            .Add("CONTROL".ToUpper)
        End With

        Dim Types As New List(Of String)
        With Types
            .Add("FLOAT")
            .Add("STRING")
            .Add("DOUBLE")
            .Add("INTEGER")
            .Add("INT32")
            .Add("CHAR")
            .Add("VARCHAR")
            .Add("NVARCHAR")
        End With

        Dim Containers As New List(Of String)
        With Containers
            .Add(")")
            .Add("(")
            .Add("[")
            .Add("]")
            .Add("{")
            .Add("}")
            .Add("'")
        End With

        Me.HideSelection = True
        Me.SuspendLayout()
        Me.Visible = False

        Dim si As Integer = Me.SelectionStart
        Dim se As Integer = Me.SelectionLength


        Dim allwords As List(Of String)
        Me.Text = Me.Text.Replace("  ", " ").Trim
        Me.Text = Me.Text.Replace(Chr(10), " " & Chr(10) & "").Trim


        allwords = Me.Text.Split(" ").ToList

        'Me.Text = ""
        Me.SelectAll()
        Me.SelectionColor = Drawing.Color.Black
        Me.SelectionFont = New Font(Me.Font, FontStyle.Regular)
        Me.DeselectAll()


        On Error Resume Next
        'Try


        With Keys
            For i = 0 To .Count - 1
                Dim cursori As Integer = 0
                While Me.Text.ToUpper.IndexOf(.Item(i), cursori) >= 0
                    Me.SelectionStart = Me.Text.ToUpper.IndexOf(.Item(i), cursori)
                    Me.SelectionLength = .Item(i).Length
                    Me.SelectionFont = New Font(Me.Font, FontStyle.Regular)
                    Me.SelectionColor = Color.Blue
                    'Me.SelectionBackColor = Color.Yellow
                    cursori = Me.SelectionStart + Me.SelectionLength + 1
                    If cursori > Me.Text.Length - 2 Then
                        Exit While
                    End If
                End While
            Next
        End With

        With Me
            Dim cursori As Integer = 0
            While Me.Text.ToUpper.IndexOf("@", cursori) >= 0
                Me.SelectionStart = Me.Text.ToUpper.IndexOf("@", cursori)
                If Me.Text.ToUpper.IndexOf(" ", Me.SelectionStart + 1) > 0 Then
                    Me.SelectionLength = Me.Text.ToUpper.IndexOf(" ", Me.SelectionStart + 1) - Me.SelectionStart
                Else
                    Me.SelectionLength = Me.Text.Length - Me.SelectionStart
                End If
                Me.SelectionFont = New Font(Me.Font, FontStyle.Regular)
                Me.SelectionColor = Color.DarkViolet
                cursori = Me.SelectionStart + Me.SelectionLength + 1
                If cursori > Me.Text.Length - 2 Then
                    Exit While
                End If
            End While
        End With

        With Containers

            For i = 0 To .Count - 1
                Dim cursori As Integer = 0
                While Me.Text.ToUpper.IndexOf(.Item(i), cursori) >= 0
                    Me.SelectionStart = Me.Text.ToUpper.IndexOf(.Item(i), cursori)
                    Me.SelectionLength = .Item(i).Length
                    Me.SelectionFont = New Font(Me.Font, FontStyle.Regular)
                    Me.SelectionColor = Color.Red
                    cursori = Me.SelectionStart + Me.SelectionLength + 1
                    If cursori > Me.Text.Length - 2 Then
                        Exit While
                    End If
                End While
            Next
        End With

        With Types
            For i = 0 To .Count - 1
                Dim cursori As Integer = 0
                While Me.Text.ToUpper.IndexOf(.Item(i), cursori) >= 0
                    Me.SelectionStart = Me.Text.ToUpper.IndexOf(.Item(i), cursori)
                    Me.SelectionLength = .Item(i).Length
                    Me.SelectionFont = New Font(Me.Font, FontStyle.Regular)
                    Me.SelectionColor = Color.DarkOrange
                    cursori = Me.SelectionStart + Me.SelectionLength + 1
                    If cursori > Me.Text.Length - 2 Then
                        Exit While
                    End If
                End While
            Next
        End With

        With Funcs
            For i = 0 To .Count - 1
                Dim cursori As Integer = 0
                While Me.Text.ToUpper.IndexOf(.Item(i), cursori) >= 0
                    Me.SelectionStart = Me.Text.ToUpper.IndexOf(.Item(i), cursori)
                    Me.SelectionLength = .Item(i).Length
                    Me.SelectionFont = New Font(Me.Font, FontStyle.Bold)
                    Me.SelectionColor = Color.Green
                    Me.SelectedText = .Item(i).ToUpper
                    cursori = Me.SelectionStart + Me.SelectionLength + 1
                    If cursori > Me.Text.Length - 2 Then
                        Exit While
                    End If
                End While
            Next
        End With

        'Catch ex As Exception
        'End Try


        If se > 0 Then
            Me.Select(si, se)
        Else
            Me.DeselectAll()
            Me.SelectionStart = si
        End If


        Me.ResumeLayout()
        Me.Visible = True

    End Sub


    Public Property CaseSensitive() As Boolean
        Get
            Return _SyntaxHighlight_CaseSensitive
        End Get
        Set(ByVal Value As Boolean)
            _SyntaxHighlight_CaseSensitive = Value
        End Set
    End Property

End Class

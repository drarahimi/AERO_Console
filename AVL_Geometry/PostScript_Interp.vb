Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports System.Drawing
Public Class PostScript_Interp

    Private m_fileName As String
    Private operand As Stack(Of String) = New Stack(Of String)()
    Private CustomOps As Dictionary(Of String, String) = New Dictionary(Of String, String)()
    Private data As List(Of String) = New List(Of String)()
    Private img As Image = New Bitmap(800, 500)
    Private currentPoint As PointF
    Private currentPen As Pen = New Pen(Color.Black)

    Public ReadOnly Property FileName As String
        Get
            Return m_fileName
        End Get
    End Property

    Public ReadOnly Property Rendered_Image As Image
        Get
            Return img
        End Get
    End Property

    Public Sub New()

        Using g = Graphics.FromImage(img)
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High
        End Using
    End Sub

    Public Sub Dispose()
        Me.currentPen.Dispose()
        Me.img.Dispose()
    End Sub

    Public Function Load(ByVal file As String) As Image
        m_fileName = file
        Dim finfo As FileInfo = New FileInfo(m_fileName)
        If Not finfo.Exists Then Return img
        Dim fs = New FileStream(m_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)

        Using sr = New StreamReader(fs)

            While Not sr.EndOfStream
                Dim Line As String = sr.ReadLine()
                Dim splits As String() = Line.Split(New String() {" ", vbTab}, StringSplitOptions.RemoveEmptyEntries)
                If splits.Length = 0 OrElse splits(0).Length = 0 Then Continue While

                Select Case splits(0).Substring(0, 1)
                    Case "/"
                        Dim key As String = splits(0).Substring(1, splits(0).Length - 1)
                        Dim value As String = ""

                        If splits(1).StartsWith("{") Then
                            If splits(1).Length > 1 Then value += splits(1).Substring(1, splits(1).Length - 1) & " "
                            Dim i As Integer = 2

                            While i < splits.Length AndAlso splits(i) <> "}"

                                If CustomOps.ContainsKey(splits(i)) Then
                                    value += CustomOps(splits(i))
                                Else
                                    value += splits(i) & " "
                                End If

                                i += 1
                            End While

                            If i = splits.Length AndAlso splits(i - 1) <> "}" Then
                                Line = sr.ReadLine()
                                splits = Line.Split(New String() {" ", vbTab}, StringSplitOptions.RemoveEmptyEntries)
                                i = 0

                                While i < splits.Length AndAlso splits(i) <> "}"

                                    If CustomOps.ContainsKey(splits(i)) Then
                                        value += CustomOps(splits(i))
                                    Else
                                        value += splits(i) & " "
                                    End If

                                    i += 1
                                End While
                            End If
                        End If

                        If Not CustomOps.ContainsKey(key) Then CustomOps.Add(key, value)
                    Case "%"

                        If splits(0).Length > 1 AndAlso splits(0).Substring(0, 2) = "%%" Then
                            Exit Select
                        Else
                            Exit Select
                        End If

                    Case Else
                        data.AddRange(splits)
                End Select
            End While
        End Using

        Execute()
        img.RotateFlip(RotateFlipType.RotateNoneFlipY)
        finfo = Nothing
        Return img
    End Function

    Private Sub Execute()
        For i As Integer = 0 To data.Count - 1

            If data(i) = "[" Then
                Dim fullArray As String = String.Empty

                While i < data.Count AndAlso data(i) <> "]"
                    fullArray += data(i) & " "
                    i += 1
                End While

                fullArray += data(i)
                operand.Push(fullArray)
            ElseIf data(i).Trim().Length = 0 OrElse data(i).StartsWith("%") Then
                Continue For
            Else
                Evaluate(data(i))
            End If
        Next
    End Sub

    Private Sub Evaluate(ByVal msg As String)
        If CustomOps.ContainsKey(msg) Then
            Dim splits As String() = CustomOps(msg).Split(New String() {" ", vbTab}, StringSplitOptions.RemoveEmptyEntries)

            For i As Integer = 0 To splits.Length - 1
                Evaluate(splits(i))
            Next
        Else

            Select Case msg.ToLower()
                Case "add"
                    Dim val2 As Double = Double.Parse(operand.Pop())
                    Dim val1 As Double = Double.Parse(operand.Pop())
                    Dim res As Double = val1 + val2
                    operand.Push(res.ToString())
                Case "sub"
                    Dim val2 As Double = Double.Parse(operand.Pop())
                    Dim val1 As Double = Double.Parse(operand.Pop())
                    Dim res As Double = val1 - val2
                    operand.Push(res.ToString())
                Case "div"
                    Dim val2 As Double = Double.Parse(operand.Pop())
                    Dim val1 As Double = Double.Parse(operand.Pop())
                    Dim res As Double = val1 / val2
                    operand.Push(res.ToString())
                Case "exch"
                    Dim val1 As String = operand.Pop()
                    Dim val2 As String = operand.Pop()
                    operand.Push(val1)
                    operand.Push(val2)
                Case "index"
                    Dim place As Integer = Integer.Parse(operand.Pop())
                    Dim vales As String() = New String(place + 1) {}

                    For i As Integer = 0 To vales.Length - 1
                        vales(i) = operand.Pop()
                    Next

                    For i As Integer = vales.Length - 1 To 0
                        operand.Push(vales(i))
                    Next

                    operand.Push(vales(place))
                Case "pop"
                    operand.Pop()
                Case "currentpoint"
                    operand.Push(currentPoint.X.ToString())
                    operand.Push(currentPoint.Y.ToString())
                Case "moveto"
                    Dim y As Single = Single.Parse(operand.Pop())
                    Dim x As Single = Single.Parse(operand.Pop())
                    currentPoint = New PointF(x, y)
                Case "rmoveto"
                    Dim dy As Single = Single.Parse(operand.Pop())
                    Dim dx As Single = Single.Parse(operand.Pop())
                    currentPoint = New PointF(currentPoint.X + dx, currentPoint.Y + dy)
                Case "lineto"
                    Dim y As Single = Single.Parse(operand.Pop())
                    Dim x As Single = Single.Parse(operand.Pop())

                    Using g = Graphics.FromImage(img)
                        g.DrawLine(currentPen, currentPoint.X, currentPoint.Y, x, y)
                    End Using

                    currentPoint = New PointF(x, y)
                Case "rlineto"
                    Dim dy As Single = Single.Parse(operand.Pop())
                    Dim dx As Single = Single.Parse(operand.Pop())

                    Using g = Graphics.FromImage(img)
                        g.DrawLine(currentPen, currentPoint.X, currentPoint.Y, currentPoint.X + dx, currentPoint.Y + dy)
                    End Using

                    currentPoint = New PointF(currentPoint.X + dx, currentPoint.Y + dy)
                Case "setgray"
                    Dim gray As Single = Single.Parse(operand.Pop())
                    gray = gray * 255
                    currentPen.Color = Color.FromArgb(CInt(gray), CInt(gray), CInt(gray))
                Case "setlinewidth"
                    Dim width As Single = Single.Parse(operand.Pop())
                    currentPen.Width = width
                Case "setrgbcolor"
                    Dim blue As Single = Single.Parse(operand.Pop()) * 255
                    Dim green As Single = Single.Parse(operand.Pop()) * 255
                    Dim red As Single = Single.Parse(operand.Pop()) * 255
                    currentPen.Color = Color.FromArgb(CInt(red), CInt(green), CInt(blue))
                Case "setdash"
                    Dim offset As Integer = Integer.Parse(operand.Pop())
                    Dim array As String = operand.Pop()
                    Dim splits As String() = array.Split(" "c)
                    Dim arrayvals As Single() = New Single(splits.Length - 2 - 1) {}

                    For i As Integer = 0 To splits.Length - 2 - 1
                        arrayvals(i) = Single.Parse(splits(i + 1))
                    Next

                    If arrayvals.Length = 0 Then
                        currentPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid
                    Else
                        currentPen.DashPattern = arrayvals
                        currentPen.DashOffset = offset
                    End If

                Case "translate"
                    Dim ty As Single = Single.Parse(operand.Pop())
                    Dim tx As Single = Single.Parse(operand.Pop())

                    Using g = Graphics.FromImage(img)
                        g.TranslateTransform(tx, ty)
                    End Using

                Case "rotate"
                    Dim deg_angle As Single = Single.Parse(operand.Pop())

                    Using g = Graphics.FromImage(img)
                        g.RotateTransform(deg_angle)
                    End Using

                Case "setlinejoin"
                    Dim type As Integer = Integer.Parse(operand.Pop())

                    Select Case type
                        Case 0
                            currentPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter
                        Case 1
                            currentPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round
                        Case 2
                            currentPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Bevel
                    End Select

                Case "gsave", "current", "context", "stroke", "closepath", "fill", "bind", "def"
                Case Else
                    operand.Push(msg)
            End Select
        End If
    End Sub



End Class

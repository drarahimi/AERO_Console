Imports FastColoredTextBoxNS
Imports System
Imports System.Drawing
Imports System.Drawing.Drawing2D

Friend Class EllipseStyle
    Inherits Style
    Dim lineColor As Color = Color.Red
    Dim linewidth As Integer = 1
    Sub New()
    End Sub
    Sub New(ByVal color As Color)
        lineColor = color
    End Sub
    Sub New(ByVal color As Color, ByVal width As Integer)
        lineColor = color
        linewidth = width
    End Sub
    Sub New(ByVal width As Integer)
        linewidth = width
    End Sub
    Public Overrides Sub Draw(gr As Graphics, position As Point, range As Range)
        Dim size As Size = Style.GetSizeOfRange(range)
        Dim rect As Rectangle = New Rectangle(position, size)
        rect.Inflate(1, 0)
        Dim path As GraphicsPath = Style.GetRoundedRectangle(rect, 7)
        gr.DrawPath(New Pen(lineColor, linewidth), path)
    End Sub
End Class
Imports System.Windows.Media.Media3D

Public Class Node
    Public Point As Point3D
    Public Surface As String
    Public X As Single
    Public Y As Single
    Public Z As Single
    Public Hovered As Boolean
    Public lineNumber As Integer
    Public type As NodeType
    Public mass As Single = 0

    Enum NodeType
        Geometry = 0
        Mass = 1
    End Enum

    Sub New()
        Me.Hovered = False
        Me.lineNumber = 0
    End Sub
    'Sub New(ByVal x As Single, ByVal y As Single, ByVal z As Single, ByVal surfacename As String, ByVal hovered As Boolean, ByVal linenumber As Integer, ByVal nodetype As NodeType, Optional mass As Single = 0)
    '    Point = New Point3D(x, y, z)
    '    Me.X = x
    '    Me.Y = y
    '    Me.Z = z
    '    Me.Surface = surfacename
    '    Me.Hovered = hovered
    '    Me.lineNumber = linenumber
    '    Me.type = nodetype
    '    Me.mass = mass
    'End Sub
    Sub New(ByVal p As Point3D, ByVal surfacename As String, ByVal hovered As Boolean, ByVal linenumber As Integer, ByVal nodetype As NodeType, Optional mass As Single = 0)
        Me.Point = p
        Me.X = p.X
        Me.Y = p.Y
        Me.Z = p.Z
        Me.Surface = surfacename
        Me.Hovered = hovered
        Me.lineNumber = linenumber
        Me.type = nodetype
        Me.mass = mass
    End Sub
    Sub New(ByVal x As Double, ByVal y As Double, ByVal z As Double, ByVal surfacename As String, ByVal hovered As Boolean, ByVal linenumber As Integer, ByVal nodetype As NodeType, Optional mass As Single = 0)
        Me.Point = New Point3D(x, y, z)
        Me.X = x
        Me.Y = y
        Me.Z = z
        Me.Surface = surfacename
        Me.Hovered = hovered
        Me.lineNumber = linenumber
        Me.type = nodetype
        Me.mass = mass
    End Sub


    ' x' = x*cos q - y*sin q
    ' y' = x*sin q + y*cos q
    ' z' = z
    '
    '          | cos q  sin q  0  0|
    ' Rz (q) = |-sin q  cos q  0  0|
    '          |     0      0  1  0|
    '          |     0      0  0  1|

    Public Function RotateZ(angle As Single) As Node
        Dim rad As Single, cosa As Single, sina As Single, Xn As Single, Yn As Single

        rad = angle * Math.PI / 180
        cosa = Math.Cos(rad)
        sina = Math.Sin(rad)
        Xn = Me.X * cosa - Me.Y * sina
        Yn = Me.X * sina + Me.Y * cosa
        Return New Node(New Point3D(Xn, Yn, Me.Z), Me.Surface, Me.Hovered, Me.lineNumber, Me.type)
    End Function

    ' y' = y*cos(q) - z*sin(q)
    ' z' = y*sin(q) + z*cos(q)
    ' x' = x
    '
    '         |1      0       0   0|
    ' Rx(q) = |0  cos(q)  sin(q)  0|
    '         |0 -sin(q)  cos(q)  0|
    '         |0      0       0   1|

    Public Function RotateX(angle As Single) As Node
        Dim rad As Single, cosa As Single, sina As Single, yn As Single, zn As Single

        rad = angle * Math.PI / 180
        cosa = Math.Cos(rad)
        sina = Math.Sin(rad)
        yn = Me.Y * cosa - Me.Z * sina
        zn = Me.Y * sina + Me.Z * cosa
        Return New Node(New Point3D(Me.X, yn, zn), Me.Surface, Me.Hovered, Me.lineNumber, Me.type)
    End Function

    ' z' = z*cos(q) - x*sin(q)
    ' x' = z*sin(q) + x*cos(q)
    ' y' = y
    '
    '         |cos(q)  0  -sin(q)  0|
    ' Ry(q) = |    0   1       0   0|
    '         |sin(q)  0   cos(q)  0|
    '         |    0   0       0   1|

    Public Function RotateY(angle As Single) As Node
        Dim rad As Single, cosa As Single, sina As Single, Xn As Single, Zn As Single

        rad = angle * Math.PI / 180
        cosa = Math.Cos(rad)
        sina = Math.Sin(rad)
        Zn = Me.Z * cosa - Me.X * sina
        Xn = Me.Z * sina + Me.X * cosa

        Return New Node(New Point3D(Xn, Me.Y, Zn), Me.Surface, Me.Hovered, Me.lineNumber, Me.type)
    End Function

    Public Function Project(viewWidth, viewHeight, viewDistance, Optional fov = 256) As Node

        Dim factor! = fov / (viewDistance + Me.Z)

        ' 2D point result (x, y)
        Dim p As New PointF
        p.X = Me.X * factor + viewWidth / 2
        p.Y = Me.Y * factor + viewHeight / 2


        Return New Node(New Point3D(p.X, p.Y, Me.Z), Me.Surface, Me.Hovered, Me.lineNumber, Me.type)
    End Function


End Class

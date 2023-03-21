Imports System

Namespace GeomLib
    ' the 3d point class
    Public Class Point3D
        ' data members - X, Y and Z coordinates
        Public X As Double
        Public Y As Double
        Public Z As Double

        ' constructor
        Public Sub New()
            X = 0.0
            Y = 0.0
            Z = 0.0
        End Sub

        ' parametrised constructor
        Public Sub New(ByVal xx As Double, ByVal yy As Double, ByVal zz As Double)
            X = xx
            Y = yy
            Z = zz
        End Sub

        ' copy constructor
        Public Sub New(ByVal Point As Point3D)
            X = Point.X
            Y = Point.Y
            Z = Point.Z
        End Sub

        ' to redefine the point variables
        Public Sub SetPoint(ByVal xx As Double, ByVal yy As Double, ByVal zz As Double)
            X = xx
            Y = yy
            Z = zz
        End Sub

        ' find the distance between this point and the parameter point
        Public Function DistanceTo(ByVal Point As Point3D) As Double
            Dim xval As Double = X - Point.X
            Dim yval As Double = Y - Point.Y
            Dim zval As Double = Z - Point.Z
            Return System.Math.Sqrt(xval * xval + yval * yval + zval * zval)
        End Function

        ' checks whether this point is equal to the parameter point
        Public Function IsEqualTo(ByVal Point As Point3D) As Boolean
            If X = Point.X And Y = Point.Y And Z = Point.Z Then
                Return True
            End If
            Return False
        End Function

        ' translate this point by the parameter vector
        Public Sub TranslateBy(ByVal Vec As Vector3D)
            If Vec.Length() > 1.0 Then
                X = X + Vec.X
                Y = Y + Vec.Y
                Z = Z + Vec.Z
            End If
        End Sub

        ' transform this point by the parameter matrix
        Public Sub TransformBy(ByVal Mat As Matrix3D)
            Dim xx As Double = 0, yy As Double = 0, zz As Double = 0
            xx = (X * Mat.matrix.GetValue(0, 0)) + (Y * Mat.matrix.GetValue(1, 0)) + _
                 (Z * Mat.matrix.GetValue(2, 0)) + (Mat.matrix.GetValue(0, 3))

            yy = (X * Mat.matrix.GetValue(0, 1)) + (Y * Mat.matrix.GetValue(1, 1)) + _
                 (Z * Mat.matrix.GetValue(2, 1)) + (Mat.matrix.GetValue(1, 3))

            zz = (X * Mat.matrix.GetValue(0, 2)) + (Y * Mat.matrix.GetValue(1, 2)) + _
                 (Z * Mat.matrix.GetValue(2, 2)) + (Mat.matrix.GetValue(2, 3))

            X = xx
            Y = yy
            Z = zz
        End Sub

        ' add this point with the parameter point
        ' and return the result
        Public Function Add(ByVal Point As Point3D) As Point3D
            Dim NewPoint = New Point3D
            NewPoint.X = X + Point.X
            NewPoint.Y = Y + Point.Y
            NewPoint.z = Z + Point.Z
            Return NewPoint
        End Function

        ' subtract this point with the parameter point
        ' and return the result
        Public Function Subtract(ByVal Point As Point3D) As Point3D
            Dim NewPoint = New Point3D
            NewPoint.X = X - Point.X
            NewPoint.Y = Y - Point.Y
            NewPoint.Y = Z - Point.Z
            Return NewPoint
        End Function

    End Class

End Namespace
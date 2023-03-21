Imports System

Namespace GeomLib
    ' the 3d vector class
    Public Class Vector3D
        ' data members - X, Y and Z values
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
        Public Sub New(ByVal Vec As Vector3D)
            X = Vec.X
            Y = Vec.Y
            Z = Vec.Z
        End Sub

        ' set vector from two points
        Public Sub New(ByVal StartPt As Point3D, ByVal EndPt As Point3D)
            X = EndPt.X - StartPt.X
            Y = EndPt.Y - StartPt.Y
            Z = EndPt.Z - StartPt.Z
        End Sub

        ' to redefine the vector variables
        Public Sub SetVector(ByVal xx As Double, ByVal yy As Double, ByVal zz As Double)
            X = xx
            Y = yy
            Z = zz
        End Sub

        ' dot product of this vector and the parameter vector
        Public Function DotProduct(ByVal Vec As Vector3D) As Double
            Return (X * Vec.X) + (Y * Vec.Y) + (Z * Vec.Z)
        End Function

        ' length of this vector
        Public Function Length() As Double
            Return System.Math.Sqrt(X * X + Y * Y + Z * Z)
        End Function

        ' find the angle between this vector and the parameter vector
        Public Function AngleTo(ByVal Vec As Vector3D) As Double
            Dim AdotB As Double = DotProduct(Vec)
            Dim ALstarBL As Double = Length() * Vec.Length()
            If ALstarBL = 0 Then
                Return 0.0
            End If
            Return System.Math.Acos(AdotB / ALstarBL)
        End Function

        ' find the unit vector and return it
        Public Function UnitVector() As Vector3D
            Dim Vec = New Vector3D
            Dim len As Double = Length()
            If len = 0.0 Then
                Vec.SetVec(0.0, 0.0, 0.0)
                Return Vec
            End If
            Vec.X = X / len
            Vec.Y = Y / len
            Vec.Z = Z / len
            Return Vec
        End Function

        ' checks whether this vector is codirectional to the parameter vector
        Public Function IsCodirectionalTo(ByVal Vec As Vector3D) As Boolean
            Dim Vec1 As Vector3D = UnitVector()
            Dim Vec2 As Vector3D = Vec.UnitVector()
            If Vec1.X = Vec2.X And Vec1.Y = Vec2.Y And Vec1.Z = Vec2.Z Then
                Return True
            End If
            Return False
        End Function

        ' checks whether this vector is equal to the parameter vector
        Public Function IsEqualTo(ByVal Vec As Vector3D) As Boolean
            If X = Vec.X And Y = Vec.Y And Z = Vec.Z Then
                Return True
            End If
            Return False
        End Function

        ' checks whether this vector is parallel to the parameter vector
        Public Function IsParallelTo(ByVal Vec As Vector3D) As Boolean
            Dim Vec1 As Vector3D = UnitVector()
            Dim Vec2 As Vector3D = Vec.UnitVector()
            If ((Vec1.X = Vec2.X And Vec1.Y = Vec2.Y And Vec1.Z = Vec2.Z) Or _
                (Vec1.X = -Vec2.X And Vec1.Y = -Vec2.Y And Vec1.Z = Vec2.Z)) Then
                Return True
            End If
            Return False
        End Function

        ' checks whether this vector is perpendicular to the parameter vector
        Public Function IsPerpendicularTo(ByVal Vec As Vector3D) As Boolean
            Dim Ang As Double = AngleTo(Vec)
            If Ang = (90 * System.Math.PI / 180.0) Then
                Return True
            End If
            Return False
        End Function

        ' checks whether this vector is X axis
        Public Function IsXAxis()
            If (X <> 0.0 And Y = 0.0 And Z = 0.0) Then
                Return True
            End If
            Return False
        End Function

        ' checks whether this vector is Y axis
        Public Function IsYAxis()
            If (X = 0.0 And Y <> 0.0 And Z = 0.0) Then
                Return True
            End If
            Return False
        End Function

        ' checks whether this vector is Z axis
        Public Function IsZAxis()
            If (X = 0.0 And Y = 0.0 And Z <> 0.0) Then
                Return True
            End If
            Return False
        End Function

        ' negate this vector
        Public Sub Negate()
            X = X * -1.0
            Y = Y * -1.0
            Z = Z * -1.0
        End Sub

        ' transform this vector with given matrix
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

        ' add this vector with the parameter vector
        ' and return the result
        Public Function Add(ByVal Vec As Vector3D) As Vector3D
            Dim NewVec = New Vector3D
            NewVec.X = X + Vec.X
            NewVec.Y = Y + Vec.Y
            NewVec.Z = Z + Vec.Z
            Return NewVec
        End Function

        ' subtract this vector with the parameter vector
        ' and return the result
        Public Function Subtract(ByVal Vec As Vector3D) As Vector3D
            Dim NewVec = New Vector3D
            NewVec.X = X - Vec.X
            NewVec.Y = Y - Vec.Y
            NewVec.Z = Z - Vec.Z
            Return NewVec
        End Function

    End Class

End Namespace
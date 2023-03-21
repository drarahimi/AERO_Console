Imports System

Namespace GeomLib
    ' the 2d point class
    Public Class Point2D
        ' data members - X and Y coordinates
        Public X As Double
        Public Y As Double

        ' constructor
        Public Sub New()
            X = 0.0
            Y = 0.0
        End Sub

        ' parametrised constructor
        Public Sub New(ByVal xx As Double, ByVal yy As Double)
            X = xx
            Y = yy
        End Sub

        ' copy constructor
        Public Sub New(ByVal Point As Point2D)
            X = Point.X
            Y = Point.Y
        End Sub

        ' to redefine the point variables
        Public Sub SetPoint(ByVal xx As Double, ByVal yy As Double)
            X = xx
            Y = yy
        End Sub

        ' find the distance between this point and the parameter point
        Public Function DistanceTo(ByVal Point As Point2D) As Double
            Dim xval As Double = X - Point.X
            Dim yval As Double = Y - Point.Y
            Return System.Math.Sqrt(xval * xval + yval * yval)
        End Function

        ' checks whether this point is equal to the parameter point
        Public Function IsEqualTo(ByVal Point As Point2D) As Boolean
            If X = Point.X And Y = Point.Y Then
                Return True
            End If
            Return False
        End Function

        ' translate this point by the parameter vector
        Public Sub TranslateBy(ByVal Vec As Vector2D)
            If Vec.Length() > 1.0 Then
                X = X + Vec.X
                Y = Y + Vec.Y
            End If
        End Sub

        ' transform this point by the parameter matrix
        Public Sub TransformBy(ByVal Mat As Matrix2D)
            Dim xx As Double = 0, yy As Double = 0
            xx = (X * Mat.matrix.GetValue(0, 0)) + (Y * Mat.matrix.GetValue(1, 0)) + _
                 (Mat.matrix.GetValue(0, 2))

            yy = (X * Mat.matrix.GetValue(0, 1)) + (Y * Mat.matrix.GetValue(1, 1)) + _
                 (Mat.matrix.GetValue(1, 2))

            X = xx
            Y = yy
        End Sub

        ' add this point with the parameter point
        ' and return the result
        Public Function Add(ByVal Point As Point2D) As Point2D
            Dim NewPoint = New Point2D
            NewPoint.X = X + Point.X
            NewPoint.Y = Y + Point.Y
            Return NewPoint
        End Function

        ' subtract this point with the parameter point
        ' and return the result
        Public Function Subtract(ByVal Point As Point2D) As Point2D
            Dim NewPoint = New Point2D
            NewPoint.X = X - Point.X
            NewPoint.Y = Y - Point.Y
            Return NewPoint
        End Function

    End Class

End Namespace
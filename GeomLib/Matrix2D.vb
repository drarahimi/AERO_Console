Imports System

Namespace GeomLib
    ' the 2d matrix class
    Public Class Matrix2D

        ' storing the members of the matrix as array
        Public matrix As Array

        ' constructor - create instance of the array variable and
        ' and fill with values of a idendity matrix
        Public Sub New()
            matrix = Array.CreateInstance(GetType(Double), 3, 3)
            Dim i As Integer
            For i = matrix.GetLowerBound(0) To matrix.GetUpperBound(0)
                Dim j As Integer
                For j = matrix.GetLowerBound(1) To matrix.GetUpperBound(1)
                    matrix.SetValue(0, i, j)
                Next j
            Next i
            matrix.SetValue(1, 0, 0)
            matrix.SetValue(1, 1, 1)
            matrix.SetValue(1, 2, 2)
        End Sub

        ' copy constructor
        Public Sub New(ByVal Mat As Matrix2D)
            matrix.CreateInstance(GetType(Double), 3, 3)
            Dim i As Integer
            For i = matrix.GetLowerBound(0) To matrix.GetUpperBound(0)
                Dim j As Integer
                For j = matrix.GetLowerBound(1) To matrix.GetUpperBound(1)
                    matrix.SetValue(Mat.matrix.GetValue(i, j), i, j)
                Next j
            Next i
        End Sub

        ' set the current matrix to identity matrix
        Public Sub SetToIdentity()
            Dim i As Integer
            For i = matrix.GetLowerBound(0) To matrix.GetUpperBound(0)
                Dim j As Integer
                For j = matrix.GetLowerBound(1) To matrix.GetUpperBound(1)
                    matrix.SetValue(0, i, j)
                Next j
            Next i
            matrix.SetValue(1, 0, 0)
            matrix.SetValue(1, 1, 1)
            matrix.SetValue(1, 2, 2)
        End Sub

        ' rotate the current matrix
        Public Sub SetToRotation(ByVal Angle As Double)
            Dim SinAng As Double = Math.Sin(Angle)
            Dim CosAng As Double = Math.Cos(Angle)
            matrix.SetValue(CosAng, 0, 0)
            matrix.SetValue(-SinAng, 0, 1)
            matrix.SetValue(SinAng, 1, 0)
            matrix.SetValue(CosAng, 1, 1)
        End Sub

        ' scale the current matrix
        Public Sub SetToScaling(ByVal ScaleFac As Double)
            matrix.SetValue(ScaleFac, 0, 0)
            matrix.SetValue(ScaleFac, 1, 1)
        End Sub

        ' translate the current matrix
        Public Sub SetToTranslation(ByVal TransVec As Vector2D)
            matrix.SetValue(TransVec.X, 0, 2)
            matrix.SetValue(TransVec.Y, 1, 2)
        End Sub

        ' multiply matrix 1 and 2 and return the resultant matrix
        Private Function Multiply(ByVal Mat1 As Matrix2D, ByVal Mat2 As Matrix2D) As Matrix2D
            Dim Mat = New Matrix2D
            Dim ii As Integer = 0
            Dim jj As Integer = 0
            Dim kk As Integer = 0
            Dim sum As Double = 0
            For ii = 0 To 2
                For jj = 0 To 2
                    sum = 0
                    For kk = 0 To 2
                        sum = sum + (Mat1.matrix.GetValue(ii, kk) * Mat2.matrix.GetValue(kk, jj))
                    Next kk
                    Mat.matrix.SetValue(sum, ii, jj)
                Next jj
            Next ii
            Return Mat
        End Function

        ' find the determinant of the current matrix
        Public Function Determinant() As Double
            Dim det As Double = 0.0
            det = (matrix.GetValue(0, 0) * (matrix.GetValue(1, 1) * matrix.GetValue(2, 2) _
                  - matrix.GetValue(2, 1) * matrix.GetValue(1, 2))) _
                  - (matrix.GetValue(0, 1) * (matrix.GetValue(1, 0) * matrix.GetValue(2, 2) _
                  - matrix.GetValue(2, 0) * matrix.GetValue(1, 2))) _
                  + (matrix.GetValue(0, 2) * (matrix.GetValue(1, 0) * matrix.GetValue(2, 1) _
                  - matrix.GetValue(2, 0) * matrix.GetValue(1, 1)))
            Return det
        End Function

        ' find the transpose
        Public Sub Transpose()
            Dim i As Integer = 0
            Dim j As Integer = 0
            Dim tmp As Double = 0.0

            For i = 1 To 2
                For j = 0 To i
                    tmp = matrix.GetValue(i, j)
                    matrix.SetValue(matrix.GetValue(j, i), i, j)
                    matrix.SetValue(tmp, j, i)
                Next j
            Next i
        End Sub

        ' find the cofactor matrix and return the same
        Public Function CoFactor() As Matrix2D
            Dim i, j, ii, jj, i1, j1 As Integer
            Dim det As Double
            Dim m = New Matrix2D
            Dim CMat = New Matrix2D

            For j = 0 To 2
                For i = 0 To 2
                    i1 = 0
                    For ii = 0 To 2
                        If ii <> i Then
                            j1 = 0
                            For jj = 0 To 2
                                If jj <> j Then
                                    m.matrix.SetValue(matrix.GetValue(ii, jj), i1, j1)
                                    j1 = +j1 + 1
                                End If
                            Next jj
                            i1 = i1 + 1
                        End If
                    Next ii
                    det = m.Determinant()
                    CMat.Matrix.SetValue(System.Math.Pow(-1.0, i + j + 2.0) * det, i, j)
                Next i
            Next j
            m = Nothing
            Return CMat
        End Function

        ' multiply the currnet matrix with the parameter
        Public Sub PreMultiplyBy(ByVal Mat As Matrix2D)
            Dim ThisMat = New Matrix2D
            Dim CMat = New Matrix2D
            ThisMat.matrix = matrix
            CMat = Multiply(Mat, ThisMat)
            matrix = CMat.matrix
            ThisMat = Nothing
            CMat = Nothing
        End Sub

        ' multiply the currnet matrix with the parameter
        Public Sub PostMultiplyBY(ByVal Mat As Matrix2D)
            Dim Thismat = New Matrix2D
            Dim CMat = New Matrix2D
            Thismat.matrix = matrix
            CMat = Multiply(Thismat, Mat)
            matrix = CMat.matrix
            Thismat = Nothing
            CMat = Nothing
        End Sub

        ' get the inverse matrix of the current matrix and return it
        Public Function GetInverse() As Matrix2D
            Dim NewMatrix As Matrix2D
            Dim det As Double = Determinant()
            det = 1 / det
            NewMatrix = CoFactor()
            NewMatrix.Transpose()

            For i As Integer = 0 To 2
                For j As Integer = 0 To 2
                    NewMatrix.matrix.SetValue(NewMatrix.matrix.GetValue(i, j) * det, i, j)
                Next j
            Next i
            Return NewMatrix
        End Function
        ' find the inverse matrix of the current matrix and set that as current matrix
        Public Sub Invert()
            Dim NewMatrix As Matrix2D
            Dim det As Double = Determinant()
            det = 1 / det
            NewMatrix = CoFactor()
            NewMatrix.Transpose()

            For i As Integer = 0 To 2
                For j As Integer = 0 To 2
                    matrix.SetValue(NewMatrix.matrix.GetValue(i, j) * det, i, j)
                Next j
            Next i
            NewMatrix = Nothing
        End Sub

    End Class

End Namespace

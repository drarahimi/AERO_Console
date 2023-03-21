Imports System

Namespace GeomLib
    ' the 3d matrix class
    Public Class Matrix3D
        ' storing the members of the matrix as array
        Public matrix As Array

        ' constructor - create instance of the array variable and
        ' and fill with values of a idendity matrix
        Public Sub New()
            matrix = Array.CreateInstance(GetType(Double), 4, 4)
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
            matrix.SetValue(1, 3, 3)
        End Sub

        ' copy constructor
        Public Sub New(ByVal Mat As Matrix3D)
            matrix.CreateInstance(GetType(Double), 4, 4)
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
            matrix.SetValue(1, 3, 3)
        End Sub

        ' rotate the current matrix along the axis specified
        Public Sub SetToRotation(ByVal Angle As Double, ByVal Axis As Vector3D)
            Dim SinAng As Double = Math.Sin(Angle)
            Dim CosAng As Double = Math.Cos(Angle)
            If Axis.IsZAxis() Then
                matrix.SetValue(CosAng, 0, 0)
                matrix.SetValue(-SinAng, 0, 1)
                matrix.SetValue(SinAng, 1, 0)
                matrix.SetValue(CosAng, 1, 1)
            End If
            If Axis.IsXAxis() Then
                matrix.SetValue(CosAng, 1, 1)
                matrix.SetValue(-SinAng, 1, 2)
                matrix.SetValue(SinAng, 2, 1)
                matrix.SetValue(CosAng, 2, 2)
            End If
            If Axis.IsYAxis() Then
                matrix.SetValue(CosAng, 0, 0)
                matrix.SetValue(SinAng, 0, 2)
                matrix.SetValue(-SinAng, 2, 0)
                matrix.SetValue(CosAng, 2, 2)
            End If
        End Sub

        ' scale the current matrix
        Public Sub SetToScaling(ByVal ScaleFac As Double)
            matrix.SetValue(ScaleFac, 0, 0)
            matrix.SetValue(ScaleFac, 1, 1)
            matrix.SetValue(ScaleFac, 2, 2)
        End Sub

        ' translate the current matrix
        Public Sub SetToTranslation(ByVal TransVec As Vector3D)
            matrix.SetValue(TransVec.X, 0, 3)
            matrix.SetValue(TransVec.Y, 1, 3)
            matrix.SetValue(TransVec.Z, 2, 3)
        End Sub

        ' multiply matrix 1 and 2 and return the resultant matrix
        Private Function Multiply(ByVal Mat1 As Matrix3D, ByVal Mat2 As Matrix3D) As Matrix3D
            Dim Mat = New Matrix3D
            Dim ii As Integer = 0
            Dim jj As Integer = 0
            Dim kk As Integer = 0
            Dim sum As Double = 0
            For ii = 0 To 3
                For jj = 0 To 3
                    sum = 0
                    For kk = 0 To 3
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
            Dim m = New Matrix2D

            For j1 As Integer = 0 To 3
                For i As Integer = 1 To 3
                    Dim j2 As Integer = 0
                    For j As Integer = 0 To 3
                        If j <> j1 Then
                            m.Matrix.SetValue(matrix.GetValue(i, j), i - 1, j2)
                            j2 = j2 + 1
                        End If
                    Next j
                Next i
                Dim d As Double = System.Math.Pow(-1.0, j1 + 2)
                d *= matrix.GetValue(0, j1) * m.Determinant()
                det += d
            Next j1
            m = Nothing
            Return det
        End Function

        ' find the transpose
        Public Sub Transpose()
            Dim i As Integer = 0
            Dim j As Integer = 0
            Dim tmp As Double = 0.0

            For i = 1 To 3
                For j = 0 To i
                    tmp = matrix.GetValue(i, j)
                    matrix.SetValue(matrix.GetValue(j, i), i, j)
                    matrix.SetValue(tmp, j, i)
                Next j
            Next i
        End Sub

        ' find the cofactor matrix and return the same
        Public Function CoFactor() As Matrix3D
            Dim i, j, ii, jj, i1, j1 As Integer
            Dim det As Double
            Dim m = New Matrix3D
            Dim CMat = New Matrix3D

            For j = 0 To 3
                For i = 0 To 3
                    i1 = 0
                    For ii = 0 To 3
                        If ii <> i Then
                            j1 = 0
                            For jj = 0 To 3
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
        Public Sub PreMultiplyBy(ByVal Mat As Matrix3D)
            Dim ThisMat = New Matrix3D
            Dim CMat = New Matrix3D
            ThisMat.matrix = matrix
            CMat = Multiply(Mat, ThisMat)
            matrix = CMat.matrix
            ThisMat = Nothing
            CMat = Nothing
        End Sub

        ' multiply the currnet matrix with the parameter
        Public Sub PostMultiplyBY(ByVal Mat As Matrix3D)
            Dim Thismat = New Matrix3D
            Dim CMat = New Matrix3D
            Thismat.matrix = matrix
            CMat = Multiply(Thismat, Mat)
            matrix = CMat.matrix
            Thismat = Nothing
            CMat = Nothing
        End Sub

        ' get the inverse matrix of the current matrix and return it
        Public Function GetInverse() As Matrix3D
            Dim NewMatrix As Matrix3D
            Dim det As Double = Determinant()
            det = 1 / det
            NewMatrix = CoFactor()
            NewMatrix.Transpose()

            For i As Integer = 0 To 3
                For j As Integer = 0 To 3
                    NewMatrix.matrix.SetValue(NewMatrix.matrix.GetValue(i, j) * det, i, j)
                Next j
            Next i
            Return NewMatrix
        End Function

        ' find the inverse matrix of the current matrix and set that as current matrix
        Public Sub Invert()
            Dim NewMatrix As Matrix3D
            Dim det As Double = Determinant()
            det = 1 / det
            NewMatrix = CoFactor()
            NewMatrix.Transpose()

            For i As Integer = 0 To 3
                For j As Integer = 0 To 3
                    matrix.SetValue(NewMatrix.matrix.GetValue(i, j) * det, i, j)
                Next j
            Next i
            NewMatrix = Nothing
        End Sub

    End Class

End Namespace
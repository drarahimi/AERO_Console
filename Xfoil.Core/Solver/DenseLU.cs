// Port of xsolve.f LUDCMP / BAKSUB: the full-matrix LU factorization and
// back-substitution XFOIL uses to solve the panel-influence system for the unit
// vorticity distributions. Faithful to the Fortran (implicit-pivoting Crout LU),
// 0-based. The NSIZ leading-dimension argument is irrelevant in C# and dropped.

namespace Xfoil.Core.Solver;

public static class DenseLU
{
    /// <summary>Port of LUDCMP. Factors the n x n matrix a in place into LU form and
    /// records the pivot permutation in indx. a and indx are consumed by BakSub.</summary>
    public static void Ludcmp(double[][] a, int n, int[] indx)
    {
        var vv = new double[n];
        for (int i = 0; i < n; i++)
        {
            double aamax = 0.0;
            for (int j = 0; j < n; j++) aamax = Math.Max(Math.Abs(a[i][j]), aamax);
            vv[i] = 1.0 / aamax;
        }

        for (int j = 0; j < n; j++)
        {
            for (int i = 0; i < j; i++)
            {
                double sum = a[i][j];
                for (int k = 0; k < i; k++) sum -= a[i][k] * a[k][j];
                a[i][j] = sum;
            }

            double aamax = 0.0;
            int imax = j;
            for (int i = j; i < n; i++)
            {
                double sum = a[i][j];
                for (int k = 0; k < j; k++) sum -= a[i][k] * a[k][j];
                a[i][j] = sum;
                double dum = vv[i] * Math.Abs(sum);
                if (dum >= aamax) { imax = i; aamax = dum; }
            }

            if (j != imax)
            {
                (a[imax], a[j]) = (a[j], a[imax]); // swap whole rows
                vv[imax] = vv[j];
            }

            indx[j] = imax;
            if (j != n - 1)
            {
                double dum = 1.0 / a[j][j];
                for (int i = j + 1; i < n; i++) a[i][j] *= dum;
            }
        }
    }

    /// <summary>Port of BAKSUB. Back-substitutes the LU factors (a, indx from Ludcmp)
    /// against right-hand side b in place; b is replaced by the solution.</summary>
    public static void Baksub(double[][] a, int n, int[] indx, double[] b)
    {
        int ii = -1;
        for (int i = 0; i < n; i++)
        {
            int ll = indx[i];
            double sum = b[ll];
            b[ll] = b[i];
            if (ii != -1)
                for (int j = ii; j < i; j++) sum -= a[i][j] * b[j];
            else if (sum != 0.0)
                ii = i;
            b[i] = sum;
        }
        for (int i = n - 1; i >= 0; i--)
        {
            double sum = b[i];
            if (i < n - 1)
                for (int j = i + 1; j < n; j++) sum -= a[i][j] * b[j];
            b[i] = sum / a[i][i];
        }
    }
}

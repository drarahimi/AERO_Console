// Standard partial-pivoting dense LU factorization/solve. Per the port plan's
// numeric-fidelity notes, this doesn't need to replicate matrix.f's LUDCMP
// pivoting choice exactly -- any correct partial-pivot LU converges to the
// same solution to within double-precision rounding for AVL's well-conditioned
// AIC systems.
//
// Port of packages/avl-core/src/solver/denseLU.ts.

namespace Avl.Core.Solver;

public sealed class LUFactorization
{
    public int N;
    /// <summary>Combined L/U matrix, row-major, n*n.</summary>
    public double[] Lu = Array.Empty<double>();
    /// <summary>Row permutation from partial pivoting.</summary>
    public int[] Piv = Array.Empty<int>();
}

public static class DenseLU
{
    public static LUFactorization LuFactor(double[] aRowMajor, int n)
    {
        var lu = (double[])aRowMajor.Clone();
        var piv = new int[n];
        for (int i = 0; i < n; i++) piv[i] = i;

        for (int k = 0; k < n; k++)
        {
            // partial pivot: find largest |lu[k..n-1][k]|
            double maxVal = Math.Abs(lu[k * n + k]);
            int maxRow = k;
            for (int i = k + 1; i < n; i++)
            {
                double v = Math.Abs(lu[i * n + k]);
                if (v > maxVal)
                {
                    maxVal = v;
                    maxRow = i;
                }
            }
            if (maxRow != k)
            {
                for (int j = 0; j < n; j++)
                {
                    double tmp = lu[k * n + j];
                    lu[k * n + j] = lu[maxRow * n + j];
                    lu[maxRow * n + j] = tmp;
                }
                int tmpP = piv[k];
                piv[k] = piv[maxRow];
                piv[maxRow] = tmpP;
            }

            double pivotVal = lu[k * n + k];
            if (pivotVal == 0) throw new InvalidOperationException($"luFactor: singular matrix at pivot {k}");

            for (int i = k + 1; i < n; i++)
            {
                double factor = lu[i * n + k] / pivotVal;
                lu[i * n + k] = factor;
                for (int j = k + 1; j < n; j++)
                {
                    lu[i * n + j] -= factor * lu[k * n + j];
                }
            }
        }

        return new LUFactorization { N = n, Lu = lu, Piv = piv };
    }

    /// <summary>Solves Ax = b given a factorization from LuFactor. Returns a new array.</summary>
    public static double[] LuSolve(LUFactorization fac, double[] b)
    {
        int n = fac.N;
        var lu = fac.Lu;
        var piv = fac.Piv;
        var x = new double[n];
        for (int i = 0; i < n; i++) x[i] = b[piv[i]];

        // forward substitution (L, unit diagonal)
        for (int i = 0; i < n; i++)
        {
            double sum = x[i];
            for (int j = 0; j < i; j++) sum -= lu[i * n + j] * x[j];
            x[i] = sum;
        }
        // back substitution (U)
        for (int i = n - 1; i >= 0; i--)
        {
            double sum = x[i];
            for (int j = i + 1; j < n; j++) sum -= lu[i * n + j] * x[j];
            x[i] = sum / lu[i * n + i];
        }
        return x;
    }
}

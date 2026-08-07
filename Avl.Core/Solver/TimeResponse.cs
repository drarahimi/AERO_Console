// Linearized time-domain response of the flight-dynamics system (the numerically
// meaningful core of the MODE menu's "TIME" feature). Propagates a small initial
// perturbation via modal superposition using the validated MODE eigensystem:
//
//     x(t) = Re( sum_k c_k * v_k * exp(lambda_k * t) ),   with  V c = x0.
//
// AVL's full TIME command integrates the *nonlinear* 6-DOF equations with an implicit
// backward-difference scheme (atime.f TDSET); this linear-modal form is exact for the
// small-perturbation regime the eigenmodes describe and reuses the same A matrix.
//
// Ported/derived from avl3.52 (no TypeScript predecessor).

using System.Numerics;

namespace Avl.Core.Solver;

public static class TimeResponse
{
    /// <summary>State time-history for an initial perturbation x0 (12-vector in the
    /// u,w,q,the,v,p,r,phi,x,y,z,psi order). Returns states[timeIndex][stateIndex].</summary>
    public static double[][] Simulate(ModeInputs m, double[] x0, IReadOnlyList<double> times)
    {
        var a = SysMat.Build(m);
        var es = Eigen.EigenSystem(a, 12);

        // V c = x0, V's columns are the eigenvectors.
        var v = new Complex[12, 12];
        for (int j = 0; j < 12; j++)
            for (int i = 0; i < 12; i++)
                v[i, j] = es.Vectors[j][i];
        var rhs = new Complex[12];
        for (int i = 0; i < 12; i++) rhs[i] = new Complex(x0[i], 0);
        var c = ComplexSolve(v, rhs);

        var result = new double[times.Count][];
        for (int ti = 0; ti < times.Count; ti++)
        {
            double t = times[ti];
            var x = new double[12];
            for (int k = 0; k < 12; k++)
            {
                var mode = c[k] * Complex.Exp(es.Values[k] * t);
                for (int i = 0; i < 12; i++) x[i] += (mode * es.Vectors[k][i]).Real;
            }
            result[ti] = x;
        }
        return result;
    }

    /// <summary>Complex Gaussian elimination with partial pivoting: solves A x = b.</summary>
    private static Complex[] ComplexSolve(Complex[,] aIn, Complex[] bIn)
    {
        int n = bIn.Length;
        var a = (Complex[,])aIn.Clone();
        var b = (Complex[])bIn.Clone();
        for (int k = 0; k < n; k++)
        {
            int piv = k;
            double max = a[k, k].Magnitude;
            for (int i = k + 1; i < n; i++)
                if (a[i, k].Magnitude > max) { max = a[i, k].Magnitude; piv = i; }
            if (piv != k)
            {
                for (int j = 0; j < n; j++) { (a[k, j], a[piv, j]) = (a[piv, j], a[k, j]); }
                (b[k], b[piv]) = (b[piv], b[k]);
            }
            for (int i = k + 1; i < n; i++)
            {
                var f = a[i, k] / a[k, k];
                for (int j = k; j < n; j++) a[i, j] -= f * a[k, j];
                b[i] -= f * b[k];
            }
        }
        var x = new Complex[n];
        for (int i = n - 1; i >= 0; i--)
        {
            var s = b[i];
            for (int j = i + 1; j < n; j++) s -= a[i, j] * x[j];
            x[i] = s / a[i, i];
        }
        return x;
    }
}

// Real general (non-symmetric) eigenvalue/eigenvector solver -- the piece AVL gets
// from EISPACK's RG. AVL links EISPACK; this is an independent, faithful port of the
// same routines (balanc -> elmhes -> eltran -> hqr2 -> balbak), 1-based to mirror the
// canonical EISPACK Fortran exactly.
//
// Eigenvalues() returns just the spectrum (used by fast paths / tests). EigenSystem()
// additionally returns eigenvectors (mode shapes for MODE), with complex conjugate
// pairs stored as adjacent real/imag columns exactly like EISPACK's z matrix.

using System.Numerics;

namespace Avl.Core.Solver;

public sealed class EigenResult
{
    public Complex[] Values = Array.Empty<Complex>();
    /// <summary>Vectors[j] is the eigenvector for Values[j] (length n).</summary>
    public Complex[][] Vectors = Array.Empty<Complex[]>();
}

public static class Eigen
{
    private const double Radix = 2.0;

    /// <summary>Eigenvalues of a real n*n matrix (row-major). Does not modify the input.</summary>
    public static Complex[] Eigenvalues(double[] aIn, int n) => Solve(aIn, n, false).Values;

    /// <summary>Eigenvalues and eigenvectors of a real n*n matrix (row-major).</summary>
    public static EigenResult EigenSystem(double[] aIn, int n) => Solve(aIn, n, true);

    private static EigenResult Solve(double[] aIn, int n, bool wantVectors)
    {
        // Balanc's iterative scaling loop only terminates for a finite matrix -- a NaN/Inf entry
        // (e.g. from a singular mass/inertia tensor upstream) makes its convergence test never
        // pass, spinning forever. Reject non-finite input up front so callers get a clean failure
        // instead of a frozen process.
        for (int idx = 0; idx < aIn.Length; idx++)
            if (double.IsNaN(aIn[idx]) || double.IsInfinity(aIn[idx]))
                throw new InvalidOperationException("Eigen: non-finite matrix entry");

        var a = new double[n + 1, n + 1];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                a[i + 1, j + 1] = aIn[i * n + j];

        var scale = new double[n + 1];
        var intv = new int[n + 1];
        Balanc(a, n, out int low, out int igh, scale);
        Elmhes(a, n, low, igh, intv);

        double[,]? z = null;
        if (wantVectors)
        {
            z = new double[n + 1, n + 1];
            Eltran(a, n, low, igh, intv, z);
        }

        var wr = new double[n + 1];
        var wi = new double[n + 1];
        int ierr = wantVectors ? Hqr2(a, n, low, igh, wr, wi, z!) : Hqr(a, n, low, igh, wr, wi);
        if (ierr != 0) throw new InvalidOperationException($"Eigen: QR failed to converge (ierr={ierr})");

        var result = new EigenResult { Values = new Complex[n] };
        for (int i = 0; i < n; i++) result.Values[i] = new Complex(wr[i + 1], wi[i + 1]);

        if (wantVectors)
        {
            Balbak(n, low, igh, scale, z!);
            result.Vectors = new Complex[n][];
            for (int j = 1; j <= n; j++)
            {
                var v = new Complex[n];
                if (wi[j] == 0.0)
                    for (int i = 1; i <= n; i++) v[i - 1] = new Complex(z![i, j], 0.0);
                else if (wi[j] > 0.0)
                    for (int i = 1; i <= n; i++) v[i - 1] = new Complex(z![i, j], z![i, j + 1]);
                else
                    for (int i = 1; i <= n; i++) v[i - 1] = new Complex(z![i, j - 1], -z![i, j]);
                result.Vectors[j - 1] = v;
            }
        }
        return result;
    }

    private static void Balanc(double[,] a, int n, out int low, out int igh, double[] scale)
    {
        double b2 = Radix * Radix;
        int k = 1, l = n;

        void Exc(int m, int j)
        {
            scale[m] = j;
            if (j == m) return;
            for (int i = 1; i <= l; i++) { double f = a[i, j]; a[i, j] = a[i, m]; a[i, m] = f; }
            for (int i = k; i <= n; i++) { double f = a[j, i]; a[j, i] = a[m, i]; a[m, i] = f; }
        }

        // search for rows isolating an eigenvalue and push them down
        bool cont = true;
        while (cont)
        {
            cont = false;
            for (int j = l; j >= 1; j--)
            {
                bool allZero = true;
                for (int i = 1; i <= l; i++) { if (i == j) continue; if (a[j, i] != 0.0) { allZero = false; break; } }
                if (allZero) { Exc(l, j); if (l == 1) { low = k; igh = l; return; } l--; cont = true; break; }
            }
        }

        // search for columns isolating an eigenvalue and push them left
        cont = true;
        while (cont)
        {
            cont = false;
            for (int j = k; j <= l; j++)
            {
                bool allZero = true;
                for (int i = k; i <= l; i++) { if (i == j) continue; if (a[i, j] != 0.0) { allZero = false; break; } }
                if (allZero) { Exc(k, j); k++; cont = true; break; }
            }
        }

        for (int i = k; i <= l; i++) scale[i] = 1.0;

        // iterative balancing
        bool noconv = true;
        while (noconv)
        {
            noconv = false;
            for (int i = k; i <= l; i++)
            {
                double c = 0.0, r = 0.0;
                for (int j = k; j <= l; j++) { if (j == i) continue; c += Math.Abs(a[j, i]); r += Math.Abs(a[i, j]); }
                if (c == 0.0 || r == 0.0) continue;
                double g = r / Radix, f = 1.0, s = c + r;
                while (c < g) { f *= Radix; c *= b2; }
                g = r * Radix;
                while (c >= g) { f /= Radix; c /= b2; }
                if ((c + r) / f >= 0.95 * s) continue;
                g = 1.0 / f;
                scale[i] *= f;
                noconv = true;
                for (int j = k; j <= n; j++) a[i, j] *= g;
                for (int j = 1; j <= l; j++) a[j, i] *= f;
            }
        }
        low = k; igh = l;
    }

    private static void Elmhes(double[,] a, int n, int low, int igh, int[] intv)
    {
        int la = igh - 1;
        for (int m = low + 1; m <= la; m++)
        {
            int mm1 = m - 1;
            double x = 0.0; int i0 = m;
            for (int j = m; j <= igh; j++)
                if (Math.Abs(a[j, mm1]) > Math.Abs(x)) { x = a[j, mm1]; i0 = j; }
            intv[m] = i0;
            if (i0 != m)
            {
                for (int j = mm1; j <= n; j++) { double t = a[i0, j]; a[i0, j] = a[m, j]; a[m, j] = t; }
                for (int j = 1; j <= igh; j++) { double t = a[j, i0]; a[j, i0] = a[j, m]; a[j, m] = t; }
            }
            if (x == 0.0) continue;
            for (int i = m + 1; i <= igh; i++)
            {
                double y = a[i, mm1];
                if (y == 0.0) continue;
                y /= x; a[i, mm1] = y;
                for (int j = m; j <= n; j++) a[i, j] -= y * a[m, j];
                for (int j = 1; j <= igh; j++) a[j, m] += y * a[j, i];
            }
        }
    }

    private static void Eltran(double[,] a, int n, int low, int igh, int[] intv, double[,] z)
    {
        for (int i = 1; i <= n; i++) { for (int j = 1; j <= n; j++) z[i, j] = 0.0; z[i, i] = 1.0; }
        for (int mp = igh - 1; mp >= low + 1; mp--)
        {
            for (int i = mp + 1; i <= igh; i++) z[i, mp] = a[i, mp - 1];
            int i0 = intv[mp];
            if (i0 == mp) continue;
            for (int j = mp; j <= igh; j++) { z[mp, j] = z[i0, j]; z[i0, j] = 0.0; }
            z[i0, mp] = 1.0;
        }
    }

    private static void Balbak(int n, int low, int igh, double[] scale, double[,] z)
    {
        for (int i = low; i <= igh; i++)
        {
            double s = scale[i];
            for (int j = 1; j <= n; j++) z[i, j] *= s;
        }
        for (int ii = 1; ii <= n; ii++)
        {
            int i = ii;
            if (i >= low && i <= igh) continue;
            if (i < low) i = low - ii;
            int k = (int)scale[i];
            if (k == i) continue;
            for (int j = 1; j <= n; j++) { double t = z[i, j]; z[i, j] = z[k, j]; z[k, j] = t; }
        }
    }

    private static double Sign(double a, double b) => b >= 0.0 ? Math.Abs(a) : -Math.Abs(a);

    // Eigenvalues-only QR (EISPACK hqr), kept for the fast path.
    private static int Hqr(double[,] h, int n, int low, int igh, double[] wr, double[] wi)
    {
        double norm = 0.0; int k0 = 1;
        for (int i = 1; i <= n; i++)
        {
            for (int j = k0; j <= n; j++) norm += Math.Abs(h[i, j]);
            k0 = i;
            if (i < low || i > igh) { wr[i] = h[i, i]; wi[i] = 0.0; }
        }
        int en = igh; double tt = 0.0;
        double p = 0, q = 0, r = 0, s, x, y, z, w;
        int ena, l, m;
        while (en >= low)
        {
            int its = 0; ena = en - 1;
            while (true)
            {
                for (l = en; l >= low + 1; l--)
                {
                    s = Math.Abs(h[l - 1, l - 1]) + Math.Abs(h[l, l]);
                    if (s == 0.0) s = norm;
                    if (Math.Abs(h[l, l - 1]) <= 1e-300 + s * 2.2204460492503131e-16) break;
                }
                x = h[en, en];
                if (l == en) { wr[en] = x + tt; wi[en] = 0.0; en--; break; }
                y = h[ena, ena]; w = h[en, ena] * h[ena, en];
                if (l == ena)
                {
                    p = 0.5 * (y - x); q = p * p + w; z = Math.Sqrt(Math.Abs(q)); x += tt;
                    if (q >= 0.0)
                    {
                        z = p + Sign(z, p);
                        wr[ena] = wr[en] = x + z;
                        if (z != 0.0) wr[en] = x - w / z;
                        wi[ena] = wi[en] = 0.0;
                    }
                    else { wr[ena] = wr[en] = x + p; wi[ena] = z; wi[en] = -z; }
                    en -= 2; break;
                }
                if (its == 30) return en;
                if (its == 10 || its == 20)
                {
                    tt += x; for (int i = low; i <= en; i++) h[i, i] -= x;
                    s = Math.Abs(h[en, ena]) + Math.Abs(h[ena, en - 2]);
                    x = y = 0.75 * s; w = -0.4375 * s * s;
                }
                its++;
                for (m = en - 2; m >= l; m--)
                {
                    z = h[m, m]; r = x - z; s = y - z;
                    p = (r * s - w) / h[m + 1, m] + h[m, m + 1];
                    q = h[m + 1, m + 1] - z - r - s; r = h[m + 2, m + 1];
                    s = Math.Abs(p) + Math.Abs(q) + Math.Abs(r); p /= s; q /= s; r /= s;
                    if (m == l) break;
                    double u = Math.Abs(h[m, m - 1]) * (Math.Abs(q) + Math.Abs(r));
                    double v = Math.Abs(p) * (Math.Abs(h[m - 1, m - 1]) + Math.Abs(z) + Math.Abs(h[m + 1, m + 1]));
                    if (u <= 2.2204460492503131e-16 * v) break;
                }
                for (int i = m + 2; i <= en; i++) { h[i, i - 2] = 0.0; if (i != m + 2) h[i, i - 3] = 0.0; }
                for (int kk = m; kk <= ena; kk++)
                {
                    bool notlast = kk != ena;
                    if (kk != m)
                    {
                        p = h[kk, kk - 1]; q = h[kk + 1, kk - 1]; r = notlast ? h[kk + 2, kk - 1] : 0.0;
                        x = Math.Abs(p) + Math.Abs(q) + Math.Abs(r);
                        if (x == 0.0) continue; p /= x; q /= x; r /= x;
                    }
                    s = Sign(Math.Sqrt(p * p + q * q + r * r), p);
                    if (kk == m) { if (l != m) h[kk, kk - 1] = -h[kk, kk - 1]; }
                    else h[kk, kk - 1] = -s * x;
                    p += s; x = p / s; y = q / s; z = r / s; q /= p; r /= p;
                    for (int j = kk; j <= en; j++)
                    {
                        p = h[kk, j] + q * h[kk + 1, j];
                        if (notlast) { p += r * h[kk + 2, j]; h[kk + 2, j] -= p * z; }
                        h[kk + 1, j] -= p * y; h[kk, j] -= p * x;
                    }
                    int jmax = Math.Min(en, kk + 3);
                    for (int i = l; i <= jmax; i++)
                    {
                        p = x * h[i, kk] + y * h[i, kk + 1];
                        if (notlast) { p += z * h[i, kk + 2]; h[i, kk + 2] -= p * r; }
                        h[i, kk + 1] -= p * q; h[i, kk] -= p;
                    }
                }
            }
        }
        return 0;
    }

    // Full QR with eigenvectors (EISPACK hqr2). h is destroyed; z holds the vectors.
    private static int Hqr2(double[,] h, int n, int low, int igh, double[] wr, double[] wi, double[,] z)
    {
        const double eps = 2.2204460492503131e-16;
        double norm = 0.0; int k0 = 1;
        for (int i = 1; i <= n; i++)
        {
            for (int j = k0; j <= n; j++) norm += Math.Abs(h[i, j]);
            k0 = i;
            if (i < low || i > igh) { wr[i] = h[i, i]; wi[i] = 0.0; }
        }

        int en = igh; double t = 0.0;
        double p = 0, q = 0, r = 0, s = 0, x = 0, y = 0, zz = 0, w = 0, ra = 0, sa = 0, vr = 0, vi = 0;
        int na, l, m, enm2;

        while (en >= low)
        {
            int its = 0; na = en - 1; enm2 = na - 1;
            while (true)
            {
                for (l = en; l >= low + 1; l--)
                {
                    s = Math.Abs(h[l - 1, l - 1]) + Math.Abs(h[l, l]);
                    if (s == 0.0) s = norm;
                    if (Math.Abs(h[l, l - 1]) <= eps * s) break;
                }
                x = h[en, en];
                if (l == en) { h[en, en] = wr[en] = x + t; wi[en] = 0.0; en--; break; }
                y = h[na, na]; w = h[en, na] * h[na, en];
                if (l == na)
                {
                    p = 0.5 * (y - x); q = p * p + w; zz = Math.Sqrt(Math.Abs(q));
                    h[en, en] = x + t; h[na, na] = y + t; x += t;
                    if (q >= 0.0)
                    {
                        zz = p + Sign(zz, p);
                        wr[na] = wr[en] = x + zz;
                        if (zz != 0.0) wr[en] = x - w / zz;
                        wi[na] = wi[en] = 0.0;
                        x = h[en, na]; s = Math.Abs(x) + Math.Abs(zz);
                        p = x / s; q = zz / s; r = Math.Sqrt(p * p + q * q); p /= r; q /= r;
                        for (int j = na; j <= n; j++) { zz = h[na, j]; h[na, j] = q * zz + p * h[en, j]; h[en, j] = q * h[en, j] - p * zz; }
                        for (int i = 1; i <= en; i++) { zz = h[i, na]; h[i, na] = q * zz + p * h[i, en]; h[i, en] = q * h[i, en] - p * zz; }
                        for (int i = low; i <= igh; i++) { zz = z[i, na]; z[i, na] = q * zz + p * z[i, en]; z[i, en] = q * z[i, en] - p * zz; }
                    }
                    else { wr[na] = wr[en] = x + p; wi[na] = zz; wi[en] = -zz; }
                    en -= 2; break;
                }
                if (its == 30) return en;
                if (its == 10 || its == 20)
                {
                    t += x; for (int i = low; i <= en; i++) h[i, i] -= x;
                    s = Math.Abs(h[en, na]) + Math.Abs(h[na, enm2]);
                    x = y = 0.75 * s; w = -0.4375 * s * s;
                }
                its++;
                for (m = enm2; m >= l; m--)
                {
                    zz = h[m, m]; r = x - zz; s = y - zz;
                    p = (r * s - w) / h[m + 1, m] + h[m, m + 1];
                    q = h[m + 1, m + 1] - zz - r - s; r = h[m + 2, m + 1];
                    s = Math.Abs(p) + Math.Abs(q) + Math.Abs(r); p /= s; q /= s; r /= s;
                    if (m == l) break;
                    double u = Math.Abs(h[m, m - 1]) * (Math.Abs(q) + Math.Abs(r));
                    double v = Math.Abs(p) * (Math.Abs(h[m - 1, m - 1]) + Math.Abs(zz) + Math.Abs(h[m + 1, m + 1]));
                    if (u <= eps * v) break;
                }
                for (int i = m + 2; i <= en; i++) { h[i, i - 2] = 0.0; if (i != m + 2) h[i, i - 3] = 0.0; }
                for (int kk = m; kk <= na; kk++)
                {
                    bool notlast = kk != na;
                    if (kk != m)
                    {
                        p = h[kk, kk - 1]; q = h[kk + 1, kk - 1]; r = notlast ? h[kk + 2, kk - 1] : 0.0;
                        x = Math.Abs(p) + Math.Abs(q) + Math.Abs(r);
                        if (x == 0.0) continue; p /= x; q /= x; r /= x;
                    }
                    s = Sign(Math.Sqrt(p * p + q * q + r * r), p);
                    if (kk == m) { if (l != m) h[kk, kk - 1] = -h[kk, kk - 1]; }
                    else h[kk, kk - 1] = -s * x;
                    p += s; x = p / s; y = q / s; zz = r / s; q /= p; r /= p;
                    for (int j = kk; j <= n; j++)
                    {
                        p = h[kk, j] + q * h[kk + 1, j];
                        if (notlast) { p += r * h[kk + 2, j]; h[kk + 2, j] -= p * zz; }
                        h[kk + 1, j] -= p * y; h[kk, j] -= p * x;
                    }
                    int jmax = Math.Min(en, kk + 3);
                    for (int i = 1; i <= jmax; i++)
                    {
                        p = x * h[i, kk] + y * h[i, kk + 1];
                        if (notlast) { p += zz * h[i, kk + 2]; h[i, kk + 2] -= p * r; }
                        h[i, kk + 1] -= p * q; h[i, kk] -= p;
                    }
                    for (int i = low; i <= igh; i++)
                    {
                        p = x * z[i, kk] + y * z[i, kk + 1];
                        if (notlast) { p += zz * z[i, kk + 2]; z[i, kk + 2] -= p * r; }
                        z[i, kk + 1] -= p * q; z[i, kk] -= p;
                    }
                }
            }
        }

        if (norm == 0.0) return 0;
        // back-substitution for eigenvectors of the upper-triangular form
        for (en = n; en >= 1; en--)
        {
            p = wr[en]; q = wi[en]; na = en - 1;
            if (q == 0.0)
            {
                m = en; h[en, en] = 1.0;
                for (int i = na; i >= 1; i--)
                {
                    w = h[i, i] - p; r = 0.0;
                    for (int j = m; j <= en; j++) r += h[i, j] * h[j, en];
                    if (wi[i] < 0.0) { zz = w; s = r; }
                    else
                    {
                        m = i;
                        if (wi[i] == 0.0) { double tmp = w; if (tmp == 0.0) tmp = eps * norm; h[i, en] = -r / tmp; }
                        else
                        {
                            x = h[i, i + 1]; y = h[i + 1, i];
                            q = (wr[i] - p) * (wr[i] - p) + wi[i] * wi[i];
                            double tv = (x * s - zz * r) / q; h[i, en] = tv;
                            if (Math.Abs(x) > Math.Abs(zz)) h[i + 1, en] = (-r - w * tv) / x;
                            else h[i + 1, en] = (-s - y * tv) / zz;
                        }
                    }
                }
            }
            else if (q < 0.0)
            {
                m = na;
                if (Math.Abs(h[en, na]) > Math.Abs(h[na, en])) { h[na, na] = q / h[en, na]; h[na, en] = -(h[en, en] - p) / h[en, na]; }
                else { Cdiv(0.0, -h[na, en], h[na, na] - p, q, out double cr, out double ci); h[na, na] = cr; h[na, en] = ci; }
                h[en, na] = 0.0; h[en, en] = 1.0;
                for (int i = na - 1; i >= 1; i--)
                {
                    w = h[i, i] - p; ra = 0.0; sa = 0.0;
                    for (int j = m; j <= en; j++) { ra += h[i, j] * h[j, na]; sa += h[i, j] * h[j, en]; }
                    if (wi[i] < 0.0) { zz = w; r = ra; s = sa; continue; }
                    m = i;
                    if (wi[i] == 0.0) { Cdiv(-ra, -sa, w, q, out double cr, out double ci); h[i, na] = cr; h[i, en] = ci; }
                    else
                    {
                        x = h[i, i + 1]; y = h[i + 1, i];
                        vr = (wr[i] - p) * (wr[i] - p) + wi[i] * wi[i] - q * q;
                        vi = (wr[i] - p) * 2.0 * q;
                        if (vr == 0.0 && vi == 0.0) vr = eps * norm * (Math.Abs(w) + Math.Abs(q) + Math.Abs(x) + Math.Abs(y) + Math.Abs(zz));
                        Cdiv(x * r - zz * ra + q * sa, x * s - zz * sa - q * ra, vr, vi, out double cr, out double ci);
                        h[i, na] = cr; h[i, en] = ci;
                        if (Math.Abs(x) > Math.Abs(zz) + Math.Abs(q))
                        {
                            h[i + 1, na] = (-ra - w * h[i, na] + q * h[i, en]) / x;
                            h[i + 1, en] = (-sa - w * h[i, en] - q * h[i, na]) / x;
                        }
                        else
                        {
                            Cdiv(-r - y * h[i, na], -s - y * h[i, en], zz, q, out double cr2, out double ci2);
                            h[i + 1, na] = cr2; h[i + 1, en] = ci2;
                        }
                    }
                }
            }
        }
        // vectors of isolated roots
        for (int i = 1; i <= n; i++)
        {
            if (i >= low && i <= igh) continue;
            for (int j = i; j <= n; j++) z[i, j] = h[i, j];
        }
        // multiply by transformation matrix to get vectors of original matrix
        for (int j = n; j >= low; j--)
        {
            m = Math.Min(j, igh);
            for (int i = low; i <= igh; i++)
            {
                zz = 0.0;
                for (int k = low; k <= m; k++) zz += z[i, k] * h[k, j];
                z[i, j] = zz;
            }
        }
        return 0;
    }

    private static void Cdiv(double ar, double ai, double br, double bi, out double cr, out double ci)
    {
        if (Math.Abs(br) >= Math.Abs(bi))
        {
            double s = bi / br, d = br + bi * s;
            cr = (ar + ai * s) / d; ci = (ai - ar * s) / d;
        }
        else
        {
            double s = br / bi, d = br * s + bi;
            cr = (ar * s + ai) / d; ci = (ai * s - ar) / d;
        }
    }
}

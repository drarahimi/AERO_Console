// Port of xsolve.f BLSOLV: the custom block solver for XFOIL's coupled
// viscous-inviscid Newton system. Each station contributes 3 unknowns
// (dCtau, dTheta, dm) and 3 equations. The system is block-bidiagonal in the BL
// equations (diagonal block A, sub-diagonal block B, plus a wake-junction block Z),
// but DENSE in the mass-defect coupling to edge velocity (the VM columns), because Ue
// at one station depends on the mass defect m at every station via the DIJ influence.
// Two right-hand sides are solved together (the residual R and the Re-influence S),
// held in VDEL(:, 0..1, :). On return VDEL holds the Newton deltas.
//
// Data layout mirrors XFOIL.INC (0-based here): VA/VB/VDEL are [3,2,NSYS];
// VZ is [3,2]; VM is [3,NSYS,NSYS] indexed [k, L(station coupled), iv(equation)].
// VACCEL thresholds skip negligible dense couplings; set them to 0 for an exact solve.

namespace Xfoil.Core.Solver.Bl;

public sealed class BlSolve
{
    private readonly int _n;                 // NSYS
    private readonly double[,,] _va, _vb, _vdel; // [3,2,NSYS]
    private readonly double[,] _vz;          // [3,2]
    private readonly double[,,] _vm;         // [3,NSYS,NSYS]
    private readonly int _ivte1;             // station index whose row eliminates VZ (or <0 for none)
    private readonly int _ivz;               // target station for the VZ elimination
    private readonly double _vacc1, _vacc2, _vacc3;

    public BlSolve(int nsys, double[,,] va, double[,,] vb, double[,] vz, double[,,] vm, double[,,] vdel,
                   int ivte1 = -1, int ivz = -1, double vacc1 = 0, double vacc2 = 0, double vacc3 = 0)
    {
        _n = nsys; _va = va; _vb = vb; _vz = vz; _vm = vm; _vdel = vdel;
        _ivte1 = ivte1; _ivz = ivz;
        _vacc1 = vacc1; _vacc2 = vacc2; _vacc3 = vacc3;
    }

    /// <summary>Solves in place; VDEL is overwritten with the solution.</summary>
    public void Solve()
    {
        int n = _n;
        var va = _va; var vb = _vb; var vz = _vz; var vm = _vm; var vdel = _vdel;

        for (int iv = 0; iv < n; iv++)
        {
            int ivp = iv + 1;

            // ---- invert the VA(iv) diagonal block ----
            // normalize first row
            double pivot = 1.0 / va[0, 0, iv];
            va[0, 1, iv] *= pivot;
            for (int l = iv; l < n; l++) vm[0, l, iv] *= pivot;
            vdel[0, 0, iv] *= pivot;
            vdel[0, 1, iv] *= pivot;

            // eliminate lower first column
            for (int k = 1; k < 3; k++)
            {
                double vtmp = va[k, 0, iv];
                va[k, 1, iv] -= vtmp * va[0, 1, iv];
                for (int l = iv; l < n; l++) vm[k, l, iv] -= vtmp * vm[0, l, iv];
                vdel[k, 0, iv] -= vtmp * vdel[0, 0, iv];
                vdel[k, 1, iv] -= vtmp * vdel[0, 1, iv];
            }

            // normalize second row
            pivot = 1.0 / va[1, 1, iv];
            for (int l = iv; l < n; l++) vm[1, l, iv] *= pivot;
            vdel[1, 0, iv] *= pivot;
            vdel[1, 1, iv] *= pivot;

            // eliminate lower second column (row 3)
            {
                double vtmp = va[2, 1, iv];
                for (int l = iv; l < n; l++) vm[2, l, iv] -= vtmp * vm[1, l, iv];
                vdel[2, 0, iv] -= vtmp * vdel[1, 0, iv];
                vdel[2, 1, iv] -= vtmp * vdel[1, 1, iv];
            }

            // normalize third row (its diagonal lives in VM[.,iv,iv])
            pivot = 1.0 / vm[2, iv, iv];
            for (int l = ivp; l < n; l++) vm[2, l, iv] *= pivot;
            vdel[2, 0, iv] *= pivot;
            vdel[2, 1, iv] *= pivot;

            // eliminate upper third column
            double vtmp1 = vm[0, iv, iv];
            double vtmp2 = vm[1, iv, iv];
            for (int l = ivp; l < n; l++)
            {
                vm[0, l, iv] -= vtmp1 * vm[2, l, iv];
                vm[1, l, iv] -= vtmp2 * vm[2, l, iv];
            }
            vdel[0, 0, iv] -= vtmp1 * vdel[2, 0, iv];
            vdel[1, 0, iv] -= vtmp2 * vdel[2, 0, iv];
            vdel[0, 1, iv] -= vtmp1 * vdel[2, 1, iv];
            vdel[1, 1, iv] -= vtmp2 * vdel[2, 1, iv];

            // eliminate upper second column
            {
                double vtmp = va[0, 1, iv];
                for (int l = ivp; l < n; l++) vm[0, l, iv] -= vtmp * vm[1, l, iv];
                vdel[0, 0, iv] -= vtmp * vdel[1, 0, iv];
                vdel[0, 1, iv] -= vtmp * vdel[1, 1, iv];
            }

            if (iv == n - 1) continue;

            // ---- eliminate VB(iv+1) block ----
            for (int k = 0; k < 3; k++)
            {
                double b1 = vb[k, 0, ivp];
                double b2 = vb[k, 1, ivp];
                double b3 = vm[k, iv, ivp];
                for (int l = ivp; l < n; l++)
                    vm[k, l, ivp] -= b1 * vm[0, l, iv] + b2 * vm[1, l, iv] + b3 * vm[2, l, iv];
                vdel[k, 0, ivp] -= b1 * vdel[0, 0, iv] + b2 * vdel[1, 0, iv] + b3 * vdel[2, 0, iv];
                vdel[k, 1, ivp] -= b1 * vdel[0, 1, iv] + b2 * vdel[1, 1, iv] + b3 * vdel[2, 1, iv];
            }

            // ---- eliminate the wake-junction VZ block ----
            if (iv == _ivte1 && _ivz >= 0)
            {
                int ivz = _ivz;
                for (int k = 0; k < 3; k++)
                {
                    double z1 = vz[k, 0];
                    double z2 = vz[k, 1];
                    for (int l = ivp; l < n; l++)
                        vm[k, l, ivz] -= z1 * vm[0, l, iv] + z2 * vm[1, l, iv];
                    vdel[k, 0, ivz] -= z1 * vdel[0, 0, iv] + z2 * vdel[1, 0, iv];
                    vdel[k, 1, ivz] -= z1 * vdel[0, 1, iv] + z2 * vdel[1, 1, iv];
                }
            }

            if (ivp == n - 1) continue;

            // ---- eliminate the lower dense VM column ----
            for (int kv = iv + 2; kv < n; kv++)
            {
                double t1 = vm[0, iv, kv];
                double t2 = vm[1, iv, kv];
                double t3 = vm[2, iv, kv];

                if (Math.Abs(t1) > _vacc1)
                {
                    for (int l = ivp; l < n; l++) vm[0, l, kv] -= t1 * vm[2, l, iv];
                    vdel[0, 0, kv] -= t1 * vdel[2, 0, iv];
                    vdel[0, 1, kv] -= t1 * vdel[2, 1, iv];
                }
                if (Math.Abs(t2) > _vacc2)
                {
                    for (int l = ivp; l < n; l++) vm[1, l, kv] -= t2 * vm[2, l, iv];
                    vdel[1, 0, kv] -= t2 * vdel[2, 0, iv];
                    vdel[1, 1, kv] -= t2 * vdel[2, 1, iv];
                }
                if (Math.Abs(t3) > _vacc3)
                {
                    for (int l = ivp; l < n; l++) vm[2, l, kv] -= t3 * vm[2, l, iv];
                    vdel[2, 0, kv] -= t3 * vdel[2, 0, iv];
                    vdel[2, 1, kv] -= t3 * vdel[2, 1, iv];
                }
            }
        }

        // ---- back-substitution over the dense VM columns ----
        for (int iv = n - 1; iv >= 1; iv--)
        {
            double t = vdel[2, 0, iv];
            for (int kv = iv - 1; kv >= 0; kv--)
            {
                vdel[0, 0, kv] -= vm[0, iv, kv] * t;
                vdel[1, 0, kv] -= vm[1, iv, kv] * t;
                vdel[2, 0, kv] -= vm[2, iv, kv] * t;
            }
            t = vdel[2, 1, iv];
            for (int kv = iv - 1; kv >= 0; kv--)
            {
                vdel[0, 1, kv] -= vm[0, iv, kv] * t;
                vdel[1, 1, kv] -= vm[1, iv, kv] * t;
                vdel[2, 1, kv] -= vm[2, iv, kv] * t;
            }
        }
    }
}

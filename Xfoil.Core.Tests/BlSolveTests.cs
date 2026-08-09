using Xfoil.Core.Solver;
using Xfoil.Core.Solver.Bl;
using Xunit;

namespace Xfoil.Core.Tests;

/// <summary>Validates the BLSOLV block-elimination solver by building a random system
/// with exactly its sparsity structure (bidiagonal in Ctau/Theta, dense in the mass
/// defect m, two simultaneous RHS), solving it both with BlSolve and with a general
/// dense LU (DenseLU), and confirming the solutions agree.</summary>
public class BlSolveTests
{
    [Fact]
    public void Solve_MatchesDenseLU_OnStructuredRandomSystem()
    {
        const int n = 6;         // stations
        int dim = 3 * n;         // 3 unknowns (dCtau, dTheta, dm) per station
        var rnd = new Random(1234);

        var va = new double[3, 2, n];
        var vb = new double[3, 2, n];
        var vz = new double[3, 2];
        var vm = new double[3, n, n];
        var vdel = new double[3, 2, n];

        // Diagonal block: strongly diagonally dominant so BLSOLV's no-pivot order is stable.
        for (int iv = 0; iv < n; iv++)
        {
            // 3x3 diagonal block D: columns 0,1 -> VA; column 2 (m) -> VM[.,iv,iv].
            for (int k = 0; k < 3; k++)
            {
                va[k, 0, iv] = 0.3 * (rnd.NextDouble() - 0.5);
                va[k, 1, iv] = 0.3 * (rnd.NextDouble() - 0.5);
                vm[k, iv, iv] = 0.3 * (rnd.NextDouble() - 0.5);
            }
            va[0, 0, iv] += 8.0;   // diagonal of the block (Ctau eqn / dCtau)
            va[1, 1, iv] += 8.0;   // (Theta eqn / dTheta)
            vm[2, iv, iv] += 8.0;  // (m eqn / dm)

            // sub-diagonal Ctau/Theta coupling to previous station (none at iv=0)
            if (iv >= 1)
                for (int k = 0; k < 3; k++)
                {
                    vb[k, 0, iv] = 0.2 * (rnd.NextDouble() - 0.5);
                    vb[k, 1, iv] = 0.2 * (rnd.NextDouble() - 0.5);
                }

            // dense mass-defect coupling to other stations
            for (int l = 0; l < n; l++)
                if (l != iv)
                    for (int k = 0; k < 3; k++)
                        vm[k, l, iv] = 0.05 * (rnd.NextDouble() - 0.5);

            // two RHS columns
            for (int k = 0; k < 3; k++)
            {
                vdel[k, 0, iv] = rnd.NextDouble() - 0.5;
                vdel[k, 1, iv] = rnd.NextDouble() - 0.5;
            }
        }

        // Build the equivalent dense system A (dim x dim) and the two RHS.
        var a = new double[dim][];
        for (int i = 0; i < dim; i++) a[i] = new double[dim];
        var b1 = new double[dim];
        var b2 = new double[dim];
        for (int iv = 0; iv < n; iv++)
            for (int k = 0; k < 3; k++)
            {
                int r = 3 * iv + k;
                a[r][3 * iv + 0] += va[k, 0, iv];
                a[r][3 * iv + 1] += va[k, 1, iv];
                for (int l = 0; l < n; l++) a[r][3 * l + 2] += vm[k, l, iv];
                if (iv >= 1)
                {
                    a[r][3 * (iv - 1) + 0] += vb[k, 0, iv];
                    a[r][3 * (iv - 1) + 1] += vb[k, 1, iv];
                }
                b1[r] = vdel[k, 0, iv];
                b2[r] = vdel[k, 1, iv];
            }

        // Reference solve via general dense LU.
        var indx = new int[dim];
        DenseLU.Ludcmp(a, dim, indx);
        DenseLU.Baksub(a, dim, indx, b1);
        DenseLU.Baksub(a, dim, indx, b2);

        // BLSOLV solve (in place, overwrites vdel).
        new BlSolve(n, va, vb, vz, vm, vdel).Solve();

        double maxErr = 0.0;
        for (int iv = 0; iv < n; iv++)
            for (int k = 0; k < 3; k++)
            {
                maxErr = Math.Max(maxErr, Math.Abs(vdel[k, 0, iv] - b1[3 * iv + k]));
                maxErr = Math.Max(maxErr, Math.Abs(vdel[k, 1, iv] - b2[3 * iv + k]));
            }
        Assert.True(maxErr < 1e-10, $"BLSOLV vs dense LU max error {maxErr:E3}");
    }
}

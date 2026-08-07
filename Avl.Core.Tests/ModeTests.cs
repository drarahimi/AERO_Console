using System.IO;
using System.Numerics;
using Avl.Core.Model;
using Avl.Core.Solver;
using Xunit;

namespace Avl.Core.Tests;

/// <summary>MODE (eigenvalue / dynamic-mode) analysis vs the reference exe. allegro +
/// allegro.mass at v=10 m/s, alpha=0: the five classic modes (short period, roll,
/// phugoid, dutch roll, spiral). Validates SysMat (12x12 dynamics matrix, including
/// apparent mass) + the Eigen solver end-to-end.</summary>
public class ModeTests
{
    private static Geometry LoadAllegro() =>
        AvlInputParser.ParseAvlGeometry(TestPaths.ReadRun("allegro.avl"), new ParseOptions
        {
            ResolveAirfoilFile = name =>
            {
                var p = Path.Combine(TestPaths.RunsDir, name);
                return File.Exists(p) ? File.ReadAllText(p) : null;
            },
        });

    private static bool Matches(Complex[] evals, double re, double im, double tol = 5e-3) =>
        evals.Any(e => Math.Abs(e.Real - re) < tol && Math.Abs(Math.Abs(e.Imaginary) - Math.Abs(im)) < tol);

    [Fact]
    public void Allegro_DynamicModes_MatchReference()
    {
        var geo = LoadAllegro();
        // allegro.mass, real units: CG (geom), inertia (kg-m^2), Lunit=0.0254, mass 0.514 kg.
        var cg = new double[] { 3.438, 0.0, 0.4883 };
        var ctx = SolveCase.BuildSolveContext(geo, new SolveContextOverrides { Xyzref = cg });
        var res = SolveCase.EvaluateCase(ctx, 0, 0, new double[] { 0, 0, 0 }, new double[geo.ControlNames.Count]);
        var d = BodyDerivsCalc.ComputeBodyAxisDerivs(geo, ctx, res.Alfa, res.Vinf, res.Wrot, res.Delcon);
        var (amass, ainer) = SysMat.AppMass(geo, ctx.EncResult, 0.0254);

        var inertia = new double[3, 3];
        inertia[0, 0] = 0.06392; inertia[1, 1] = 0.01966; inertia[2, 2] = 0.08279;
        inertia[0, 2] = inertia[2, 0] = -0.0007208;

        double[][] cfU =
        {
            new[]{ d.Cx.U, d.Cx.V, d.Cx.W, d.Cx.P, d.Cx.Q, d.Cx.R },
            new[]{ d.Cy.U, d.Cy.V, d.Cy.W, d.Cy.P, d.Cy.Q, d.Cy.R },
            new[]{ d.Cz.U, d.Cz.V, d.Cz.W, d.Cz.P, d.Cz.Q, d.Cz.R },
        };
        double[][] cmU =
        {
            new[]{ d.Cl.U, d.Cl.V, d.Cl.W, d.Cl.P, d.Cl.Q, d.Cl.R },
            new[]{ d.Cm.U, d.Cm.V, d.Cm.W, d.Cm.P, d.Cm.Q, d.Cm.R },
            new[]{ d.Cn.U, d.Cn.V, d.Cn.W, d.Cn.P, d.Cn.Q, d.Cn.R },
        };

        var m = new ModeInputs
        {
            Sref = geo.Sref, Cref = geo.Cref, Bref = geo.Bref,
            Vee = 10, Rho = 1.225, Gee = 9.81, Unitl = 0.0254,
            Mass = 0.514, Inertia = inertia,
            AlfaRad = 0, PhiDeg = 0, TheDeg = 0, PsiDeg = 0,
            Vinf = res.Vinf, Wrot = res.Wrot,
            Cftot = res.Totals.CfTot, Cmtot = res.Totals.CmTot, CftotU = cfU, CmtotU = cmU,
            Amass = amass, Ainer = ainer,
        };

        var evals = SysMat.Eigenvalues(m);

        // Reference avl3.51-32.exe MODE eigenvalues:
        Assert.True(Matches(evals, -18.5613, 7.67633), "short period");
        Assert.True(Matches(evals, -32.9966, 0.0), "roll subsidence");
        Assert.True(Matches(evals, -0.132448, 1.16043), "phugoid");
        Assert.True(Matches(evals, -2.00914, 6.17487), "dutch roll");
        Assert.True(Matches(evals, -0.124620, 0.0), "spiral");
    }

    [Fact]
    public void Allegro_RollModeShape_MatchesReference()
    {
        var geo = LoadAllegro();
        var cg = new double[] { 3.438, 0.0, 0.4883 };
        var ctx = SolveCase.BuildSolveContext(geo, new SolveContextOverrides { Xyzref = cg });
        var res = SolveCase.EvaluateCase(ctx, 0, 0, new double[] { 0, 0, 0 }, new double[geo.ControlNames.Count]);
        var d = BodyDerivsCalc.ComputeBodyAxisDerivs(geo, ctx, res.Alfa, res.Vinf, res.Wrot, res.Delcon);
        var (amass, ainer) = SysMat.AppMass(geo, ctx.EncResult, 0.0254);
        var inertia = new double[3, 3];
        inertia[0, 0] = 0.06392; inertia[1, 1] = 0.01966; inertia[2, 2] = 0.08279; inertia[0, 2] = inertia[2, 0] = -0.0007208;
        double[][] cfU = { new[] { d.Cx.U, d.Cx.V, d.Cx.W, d.Cx.P, d.Cx.Q, d.Cx.R }, new[] { d.Cy.U, d.Cy.V, d.Cy.W, d.Cy.P, d.Cy.Q, d.Cy.R }, new[] { d.Cz.U, d.Cz.V, d.Cz.W, d.Cz.P, d.Cz.Q, d.Cz.R } };
        double[][] cmU = { new[] { d.Cl.U, d.Cl.V, d.Cl.W, d.Cl.P, d.Cl.Q, d.Cl.R }, new[] { d.Cm.U, d.Cm.V, d.Cm.W, d.Cm.P, d.Cm.Q, d.Cm.R }, new[] { d.Cn.U, d.Cn.V, d.Cn.W, d.Cn.P, d.Cn.Q, d.Cn.R } };
        var m = new ModeInputs
        {
            Sref = geo.Sref, Cref = geo.Cref, Bref = geo.Bref, Vee = 10, Rho = 1.225, Gee = 9.81, Unitl = 0.0254,
            Mass = 0.514, Inertia = inertia, AlfaRad = 0, PhiDeg = 0, TheDeg = 0, PsiDeg = 0,
            Vinf = res.Vinf, Wrot = res.Wrot, Cftot = res.Totals.CfTot, Cmtot = res.Totals.CmTot, CftotU = cfU, CmtotU = cmU,
            Amass = amass, Ainer = ainer,
        };

        var sys = Eigen.EigenSystem(SysMat.Build(m), 12);
        // State order: u,w,q,the,v,p,r,phi,x,y,z,psi -> p is index 5.
        int roll = Enumerable.Range(0, 12).First(j => Math.Abs(sys.Values[j].Imaginary) < 1e-6 && Math.Abs(sys.Values[j].Real + 33) < 2);
        var v = sys.Vectors[roll];
        var p = v[5];

        // Reference exe roll mode shape (ratios to the dominant p component).
        Assert.Equal(-0.0857, (v[4] / p).Real, 3); // v/p
        Assert.Equal(0.0758, (v[6] / p).Real, 3);  // r/p
        Assert.Equal(0.0303, (v[7] / p).Real, 3);  // phi/p
    }

    private static ModeInputs BuildAllegroMode(out EigenResult sys)
    {
        var geo = LoadAllegro();
        var cg = new double[] { 3.438, 0.0, 0.4883 };
        var ctx = SolveCase.BuildSolveContext(geo, new SolveContextOverrides { Xyzref = cg });
        var res = SolveCase.EvaluateCase(ctx, 0, 0, new double[] { 0, 0, 0 }, new double[geo.ControlNames.Count]);
        var d = BodyDerivsCalc.ComputeBodyAxisDerivs(geo, ctx, res.Alfa, res.Vinf, res.Wrot, res.Delcon);
        var (amass, ainer) = SysMat.AppMass(geo, ctx.EncResult, 0.0254);
        var inertia = new double[3, 3];
        inertia[0, 0] = 0.06392; inertia[1, 1] = 0.01966; inertia[2, 2] = 0.08279; inertia[0, 2] = inertia[2, 0] = -0.0007208;
        double[][] cfU = { new[] { d.Cx.U, d.Cx.V, d.Cx.W, d.Cx.P, d.Cx.Q, d.Cx.R }, new[] { d.Cy.U, d.Cy.V, d.Cy.W, d.Cy.P, d.Cy.Q, d.Cy.R }, new[] { d.Cz.U, d.Cz.V, d.Cz.W, d.Cz.P, d.Cz.Q, d.Cz.R } };
        double[][] cmU = { new[] { d.Cl.U, d.Cl.V, d.Cl.W, d.Cl.P, d.Cl.Q, d.Cl.R }, new[] { d.Cm.U, d.Cm.V, d.Cm.W, d.Cm.P, d.Cm.Q, d.Cm.R }, new[] { d.Cn.U, d.Cn.V, d.Cn.W, d.Cn.P, d.Cn.Q, d.Cn.R } };
        var m = new ModeInputs
        {
            Sref = geo.Sref, Cref = geo.Cref, Bref = geo.Bref, Vee = 10, Rho = 1.225, Gee = 9.81, Unitl = 0.0254,
            Mass = 0.514, Inertia = inertia, AlfaRad = 0, PhiDeg = 0, TheDeg = 0, PsiDeg = 0,
            Vinf = res.Vinf, Wrot = res.Wrot, Cftot = res.Totals.CfTot, Cmtot = res.Totals.CmTot, CftotU = cfU, CmtotU = cmU,
            Amass = amass, Ainer = ainer,
        };
        sys = Eigen.EigenSystem(SysMat.Build(m), 12);
        return m;
    }

    [Fact]
    public void TimeResponse_PureModeDecaysAtEigenvalueRate()
    {
        var m = BuildAllegroMode(out var sys);
        // Seed a pure real-mode initial condition (the roll subsidence, lambda~-33).
        int roll = Enumerable.Range(0, 12).First(j => Math.Abs(sys.Values[j].Imaginary) < 1e-6 && Math.Abs(sys.Values[j].Real + 33) < 2);
        double lambda = sys.Values[roll].Real;
        var x0 = sys.Vectors[roll].Select(c => c.Real).ToArray();

        var t = new[] { 0.0, 0.02, 0.05 };
        var hist = TimeResponse.Simulate(m, x0, t);

        // At t=0 the initial condition is recovered; a pure mode decays as exp(lambda t).
        for (int i = 0; i < 12; i++) Assert.Equal(x0[i], hist[0][i], 6);
        for (int ti = 1; ti < t.Length; ti++)
        {
            double factor = Math.Exp(lambda * t[ti]);
            for (int i = 0; i < 12; i++)
                Assert.Equal(x0[i] * factor, hist[ti][i], 5);
        }
    }

    [Fact]
    public void Allegro_ApparentMass_MatchesReference()
    {
        var geo = LoadAllegro();
        var ctx = SolveCase.BuildSolveContext(geo, new SolveContextOverrides { Xyzref = new double[] { 3.438, 0, 0.4883 } });
        var (amass, ainer) = SysMat.AppMass(geo, ctx.EncResult, 0.0254);

        // MSHO apparent mass/inertia (x rho), kg / kg-m^2.
        Assert.Equal(0.0, amass[0, 0] * 1.225, 4);
        Assert.Equal(0.003854, amass[1, 1] * 1.225, 4);
        Assert.Equal(0.05907, amass[2, 2] * 1.225, 4);
        Assert.Equal(0.01453, ainer[0, 0] * 1.225, 4);
        Assert.Equal(0.001893, ainer[1, 1] * 1.225, 4);
        Assert.Equal(0.001252, ainer[2, 2] * 1.225, 4);
    }
}
